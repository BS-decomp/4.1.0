#!/usr/bin/env python3
"""Render a quick top-down PNG of a scene's mesh geometry (no Unity required).

This is a review aid for geometry work such as the static-batch repair: it
rasterises every MeshRenderer's triangles in world space, looking straight down,
and shades them by height.  If a map's walls and floors are missing or scattered,
it is immediately visible here instead of only inside the editor.

    python3 tools/preview_scene_topdown.py --scene Bust --out /tmp/bust.png
"""

from __future__ import annotations

import argparse
import re
import struct
import sys
import zlib
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))

from static_batch_lib import (  # noqa: E402
    CLASS_MESH_FILTER,
    CLASS_MESH_RENDERER,
    SceneGraph,
    UnsupportedLayout,
    build_guid_map,
    field_text,
    iter_scenes,
    load_mesh,
    parse_document,
    parse_ptr,
    transform_point,
)

ROOT = Path(__file__).resolve().parent.parent


def field_block(body, name):
    m = re.search(r"^(\s*)" + re.escape(name) + r":\s*$", body, re.M)
    if not m:
        return ""
    indent = len(m.group(1))
    lines = []
    for line in body[m.end():].split("\n")[1:]:
        if line.strip() and (len(line) - len(line.lstrip())) <= indent and not line.lstrip().startswith("- "):
            break
        lines.append(line)
    return "\n".join(lines)


def scene_label(scene_path, project_root):
    rel = scene_path.relative_to(Path(project_root) / "Assets" / "Levels")
    return rel.parent.name if rel.parent != Path(".") else scene_path.stem


def write_png(path, width, height, pixels):
    """pixels: bytearray of RGB triples, row-major."""
    raw = bytearray()
    for y in range(height):
        raw.append(0)
        raw.extend(pixels[y * width * 3 : (y + 1) * width * 3])

    def chunk(tag, data):
        body = tag + data
        return struct.pack(">I", len(data)) + body + struct.pack(">I", zlib.crc32(body) & 0xFFFFFFFF)

    png = b"\x89PNG\r\n\x1a\n"
    png += chunk(b"IHDR", struct.pack(">IIBBBBB", width, height, 8, 2, 0, 0, 0))
    png += chunk(b"IDAT", zlib.compress(bytes(raw), 9))
    png += chunk(b"IEND", b"")
    Path(path).write_bytes(png)


def collect_triangles(scene_path, project, guid_map):
    doc = parse_document(scene_path.read_text(encoding="utf-8"))
    graph = SceneGraph(doc)
    cache = {}
    tris = []
    for obj in doc.objects:
        if obj.class_id != CLASS_MESH_RENDERER:
            continue
        if field_text(obj.body, "m_Enabled", "1") != "1":
            continue
        go_id, _ = parse_ptr(field_text(obj.body, "m_GameObject"))
        mf_id = graph.component(go_id, CLASS_MESH_FILTER)
        if mf_id is None or go_id not in graph.transform_of_go:
            continue
        _fid, guid = parse_ptr(field_text(graph.objects[mf_id].body, "m_Mesh"))
        if not guid or guid not in guid_map:
            continue
        if guid not in cache:
            try:
                cache[guid] = load_mesh(guid_map[guid])
            except (UnsupportedLayout, OSError):
                cache[guid] = None
        mesh = cache[guid]
        if mesh is None:
            continue
        matrix = graph.world_matrix(graph.transform_of_go[go_id])
        points = [
            transform_point(matrix, struct.unpack_from("<3f", mesh.raw, v * mesh.stride))
            for v in range(mesh.vertex_count)
        ]
        # Unity draws submesh i with material i: that is exactly why a batched
        # renderer left on a combined mesh shows submesh 0 (a barrel, on Bust).
        material_count = len(re.findall(r"^  - \{fileID: \d+", field_block(obj.body, "m_Materials"), re.M)) or 1
        for s_index in range(min(material_count, len(mesh.submeshes))):
            idx = mesh.submesh_indices(s_index)
            for t in range(0, len(idx) - 2, 3):
                try:
                    tris.append((points[idx[t]], points[idx[t + 1]], points[idx[t + 2]]))
                except IndexError:
                    pass
    return tris


def render(tris, size=900, margin=20):
    if not tris:
        raise SystemExit("no geometry found")
    # Frame on the bulk of the geometry: maps contain huge helpers (moon,
    # skybox cards) thousands of units away that would otherwise shrink the
    # playable area to a few pixels.  Interquartile clipping keeps the level,
    # then the frame is refitted to whatever survived the clip.
    def robust_range(values, factor=1.5):
        values = sorted(values)
        n = len(values)
        q1, q3 = values[n // 4], values[(3 * n) // 4]
        spread = max(q3 - q1, 1e-3)
        return q1 - factor * spread, q3 + factor * spread

    rough_x = robust_range([(a[0] + b[0] + c[0]) / 3 for a, b, c in tris])
    rough_z = robust_range([(a[2] + b[2] + c[2]) / 3 for a, b, c in tris])
    kept = [
        tri
        for tri in tris
        if rough_x[0] <= (tri[0][0] + tri[1][0] + tri[2][0]) / 3 <= rough_x[1]
        and rough_z[0] <= (tri[0][2] + tri[1][2] + tri[2][2]) / 3 <= rough_z[1]
    ]
    tris = kept or tris
    xs = [p[0] for tri in tris for p in tri]
    zs = [p[2] for tri in tris for p in tri]
    ys = [p[1] for tri in tris for p in tri]
    lo_x, hi_x, lo_z, hi_z = min(xs), max(xs), min(zs), max(zs)
    lo_y, hi_y = min(ys), max(ys)
    pad = 0.03 * max(hi_x - lo_x, hi_z - lo_z, 1e-3)
    lo_x, hi_x, lo_z, hi_z = lo_x - pad, hi_x + pad, lo_z - pad, hi_z + pad
    span = max(hi_x - lo_x, hi_z - lo_z, 1e-3)
    scale = (size - 2 * margin) / span

    # Shade by height, but normalise on the 5..95 percentile of triangle tops so
    # a single tall helper does not flatten the whole map to one colour.
    tops = sorted(max(p[1] for p in tri) for tri in tris)
    shade_lo = tops[int(len(tops) * 0.05)]
    shade_hi = tops[min(len(tops) - 1, int(len(tops) * 0.95))]
    if shade_hi <= shade_lo:
        shade_lo, shade_hi = lo_y, max(hi_y, lo_y + 1e-3)

    pixels = bytearray([24] * (size * size * 3))
    height_buffer = [float("-inf")] * (size * size)

    off_x = (size - 2 * margin - (hi_x - lo_x) * scale) / 2
    off_z = (size - 2 * margin - (hi_z - lo_z) * scale) / 2

    def to_pixel(p):
        px = margin + off_x + (p[0] - lo_x) * scale
        py = margin + off_z + (hi_z - p[2]) * scale
        return px, py

    for tri in tris:
        pts = [to_pixel(p) for p in tri]
        top = max(p[1] for p in tri)
        shade = min(1.0, max(0.0, (top - shade_lo) / (shade_hi - shade_lo)))
        shade = 0.15 + 0.85 * shade
        colour = (
            int(40 + 200 * shade),
            int(55 + 185 * shade),
            int(70 + 150 * shade),
        )
        min_x = max(0, int(min(p[0] for p in pts)))
        max_x = min(size - 1, int(max(p[0] for p in pts)) + 1)
        min_y = max(0, int(min(p[1] for p in pts)))
        max_y = min(size - 1, int(max(p[1] for p in pts)) + 1)
        if min_x > max_x or min_y > max_y:
            continue
        (x0, y0), (x1, y1), (x2, y2) = pts
        area = (x1 - x0) * (y2 - y0) - (x2 - x0) * (y1 - y0)
        if abs(area) < 1e-9:
            continue
        for py in range(min_y, max_y + 1):
            for px in range(min_x, max_x + 1):
                cx, cy = px + 0.5, py + 0.5
                w0 = ((x1 - x0) * (cy - y0) - (cx - x0) * (y1 - y0)) / area
                w1 = ((cx - x0) * (y2 - y0) - (x2 - x0) * (cy - y0)) / area
                if w0 < 0 or w1 < 0 or w0 + w1 > 1:
                    continue
                offset = py * size + px
                if top <= height_buffer[offset]:
                    continue
                height_buffer[offset] = top
                base = offset * 3
                pixels[base : base + 3] = bytes(colour)
    return size, size, pixels


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--project", default=str(ROOT / "client"))
    parser.add_argument("--scene", required=True, help="map label, e.g. Bust")
    parser.add_argument("--out", required=True, help="output PNG path")
    parser.add_argument("--size", type=int, default=900)
    args = parser.parse_args(argv)

    project = Path(args.project).resolve()
    scenes = [s for s in iter_scenes(project) if scene_label(s, project) == args.scene]
    if not scenes:
        parser.error("no scene labelled %r" % args.scene)
    guid_map = build_guid_map(project)
    tris = collect_triangles(scenes[0], project, guid_map)
    width, height, pixels = render(tris, size=args.size)
    write_png(args.out, width, height, pixels)
    print("%s: %d triangles -> %s" % (args.scene, len(tris), args.out))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
