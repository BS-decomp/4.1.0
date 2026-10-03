#!/usr/bin/env python3
"""Undo Unity 4 build-time static batching in the recovered 4.1.0 scenes.

Why this exists
---------------
The 4.1.0 APK ships *built* scenes, so Unity 4.7.2f1 had already merged every
`static` renderer into per-scene `Combined Mesh (root: scene)` assets.  In that
layout:

* every batched `MeshFilter` points at the shared combined mesh;
* the combined mesh vertices are stored in **static-batch-root space** (the
  batch root is the scene root, i.e. world space, for this export);
* each `MeshRenderer` selects its own geometry through the Unity 4 field
  `m_SubsetIndices` (little-endian uint32 submesh indices).

Unity 5+ replaced `m_SubsetIndices` with `m_StaticBatchInfo`, so when the export
is opened in a modern editor every batched renderer silently falls back to
submesh 0 of the combined mesh and draws it *again* through its own transform.
On `Bust` submesh 0 happens to be a barrel - hence the scene full of barrels
hovering where walls and floors should be.

What this tool does
-------------------
For every batched renderer it writes a real, standalone mesh asset:

* the submeshes listed in `m_SubsetIndices`, in material order;
* vertices moved from batch-root space into the renderer's own local space
  (`inverse(worldMatrix) * v`), so the existing Transform, colliders and script
  references stay untouched and the geometry lands back on its original world
  position;
* normals/tangents rotated accordingly (byte-identical copy when the renderer
  has no rotation/scale, which is the common case in these maps);
* recomputed submesh and mesh AABBs.

The scene is then patched minimally: `m_Mesh` of the MeshFilter (and of a
MeshCollider on the same GameObject when it used the combined mesh), plus
`m_SubsetIndices` rewritten as a plain 0..n-1 sequence.  Nothing else in the
scene is touched, no component is deleted and the original combined meshes stay
in the project as ground-truth evidence for `verify_static_batching.py`.

Usage
-----
    python3 tools/debatch_static_meshes.py --list
    python3 tools/debatch_static_meshes.py --scene Bust --dry-run
    python3 tools/debatch_static_meshes.py --all --report docs/static-batching-report.json
"""

from __future__ import annotations

import argparse
import hashlib
import json
import re
import struct
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))

from static_batch_lib import (  # noqa: E402
    CH_NORMAL,
    CH_TANGENT,
    CH_VERTEX,
    CLASS_MESH_COLLIDER,
    CLASS_MESH_FILTER,
    CLASS_MESH_RENDERER,
    SceneGraph,
    UnsupportedLayout,
    build_guid_map,
    field_text,
    format_subset_indices,
    is_combined_mesh,
    is_identity_linear,
    iter_scenes,
    load_mesh,
    mat_inverse,
    parse_document,
    parse_ptr,
    parse_subset_indices,
    transform_normal_world_to_local,
    transform_point,
    transform_direction,
    unity_float,
)

ROOT = Path(__file__).resolve().parent.parent
GUID_NAMESPACE = "BS-decomp/4.1.0 static-batch de-batching v1"
META_TEMPLATE = (
    "fileFormatVersion: 2\n"
    "guid: {guid}\n"
    "licenseType: Free\n"
    "NativeFormatImporter:\n"
    "  name:\n"
    "  userData:\n"
)


def stable_guid(*parts):
    digest = hashlib.md5("|".join([GUID_NAMESPACE, *map(str, parts)]).encode()).hexdigest()
    return digest


def sanitize(name):
    cleaned = re.sub(r"[^A-Za-z0-9 _.-]", "_", name).strip() or "Object"
    return cleaned[:48]


def scene_label(scene_path, project_root):
    """Human-readable map label: the export keeps obfuscated scene file names."""
    rel = scene_path.relative_to(Path(project_root) / "Assets" / "Levels")
    return rel.parent.name if rel.parent != Path(".") else scene_path.stem


# --------------------------------------------------------------------------- #
# Mesh building
# --------------------------------------------------------------------------- #


def build_local_mesh(combined, subset, world_matrix, mesh_name):
    """Return the text of a standalone Unity 4 Mesh asset for one renderer."""
    inverse = mat_inverse(world_matrix)
    linear_identity = is_identity_linear(world_matrix)

    channels = combined.channels
    stride = combined.stride
    pos_channel = channels[CH_VERTEX] if len(channels) > CH_VERTEX else None
    if not pos_channel or pos_channel[2] != 0 or pos_channel[3] != 3 or pos_channel[1] != 0:
        raise UnsupportedLayout("unexpected position channel %r" % (pos_channel,))
    normal_channel = channels[CH_NORMAL] if len(channels) > CH_NORMAL else None
    tangent_channel = channels[CH_TANGENT] if len(channels) > CH_TANGENT else None

    out_vertices = bytearray()
    out_indices = []
    submesh_meta = []
    vertex_cursor = 0
    index_cursor = 0

    for submesh_index in subset:
        src = combined.submeshes[submesh_index]
        if src.topology != 0:
            raise UnsupportedLayout("submesh topology %d is not triangles" % src.topology)
        block = combined.raw[
            src.first_vertex * stride : (src.first_vertex + src.vertex_count) * stride
        ]
        if len(block) != src.vertex_count * stride:
            raise UnsupportedLayout("submesh vertex range outside the vertex buffer")
        block = bytearray(block)

        for v in range(src.vertex_count):
            px, py, pz = struct.unpack_from("<3f", block, v * stride)
            struct.pack_into("<3f", block, v * stride, *transform_point(inverse, (px, py, pz)))
            if linear_identity:
                continue
            if normal_channel and normal_channel[3] >= 3:
                comps = list(_read(block, stride, normal_channel, v))
                comps[:3] = transform_normal_world_to_local(world_matrix, comps[:3])
                _write(block, stride, normal_channel, v, comps)
            if tangent_channel and tangent_channel[3] >= 3:
                comps = list(_read(block, stride, tangent_channel, v))
                direction = transform_direction(inverse, comps[:3])
                length = sum(c * c for c in direction) ** 0.5
                if length:
                    comps[:3] = [c / length for c in direction]
                _write(block, stride, tangent_channel, v, comps)

        indices = combined.submesh_indices(submesh_index)
        lo, hi = src.first_vertex, src.first_vertex + src.vertex_count
        if indices and (min(indices) < lo or max(indices) >= hi):
            raise UnsupportedLayout("submesh indices leave the declared vertex range")
        out_indices.extend(i - lo + vertex_cursor for i in indices)

        centre, extent = _bounds(block, stride, src.vertex_count)
        submesh_meta.append(
            {
                "firstByte": index_cursor * 2,
                "indexCount": len(indices),
                "topology": 0,
                "firstVertex": vertex_cursor,
                "vertexCount": src.vertex_count,
                "center": centre,
                "extent": extent,
            }
        )
        out_vertices.extend(block)
        vertex_cursor += src.vertex_count
        index_cursor += len(indices)

    if vertex_cursor > 65535:
        raise UnsupportedLayout("de-batched mesh exceeds the 16-bit index range")

    mesh_centre, mesh_extent = _bounds(out_vertices, stride, vertex_cursor)
    return _render_mesh_asset(
        combined,
        mesh_name,
        submesh_meta,
        struct.pack("<%dH" % len(out_indices), *out_indices),
        bytes(out_vertices),
        vertex_cursor,
        mesh_centre,
        mesh_extent,
    )


def _read(buffer, stride, channel, index):
    from static_batch_lib import decode_channel

    return decode_channel(buffer, stride, channel, index)


def _write(buffer, stride, channel, index, values):
    from static_batch_lib import encode_channel

    encode_channel(buffer, stride, channel, index, values)


def _bounds(buffer, stride, count):
    if not count:
        return (0.0, 0.0, 0.0), (0.0, 0.0, 0.0)
    lo = [float("inf")] * 3
    hi = [float("-inf")] * 3
    for v in range(count):
        p = struct.unpack_from("<3f", buffer, v * stride)
        for a in range(3):
            lo[a] = min(lo[a], p[a])
            hi[a] = max(hi[a], p[a])
    centre = tuple((lo[a] + hi[a]) / 2 for a in range(3))
    extent = tuple((hi[a] - lo[a]) / 2 for a in range(3))
    return centre, extent


def _vec(name, values, indent):
    return "%s%s: {x: %s, y: %s, z: %s}\n" % (
        " " * indent,
        name,
        unity_float(values[0]),
        unity_float(values[1]),
        unity_float(values[2]),
    )


def _render_mesh_asset(combined, mesh_name, submeshes, index_bytes, raw, vertex_count, centre, extent):
    text = combined.text

    submesh_text = []
    for s in submeshes:
        submesh_text.append(
            "  - serializedVersion: 2\n"
            "    firstByte: %d\n"
            "    indexCount: %d\n"
            "    topology: %d\n"
            "    firstVertex: %d\n"
            "    vertexCount: %d\n"
            "    localAABB:\n" % (
                s["firstByte"], s["indexCount"], s["topology"], s["firstVertex"], s["vertexCount"],
            )
        )
        submesh_text.append(_vec("m_Center", s["center"], 6))
        submesh_text.append(_vec("m_Extent", s["extent"], 6))
    submesh_text = "".join(submesh_text)

    head, rest = text.split("  m_SubMeshes:\n", 1)
    _old_subs, tail = rest.split("  m_Shapes:", 1)
    text = head + "  m_SubMeshes:\n" + submesh_text + "  m_Shapes:" + tail

    text = re.sub(r"^  m_Name: .*$", "  m_Name: " + mesh_name, text, count=1, flags=re.M)
    text = re.sub(r"^  m_IndexBuffer: .*$", "  m_IndexBuffer: " + index_bytes.hex(), text, count=1, flags=re.M)
    text = re.sub(r"^    m_VertexCount: .*$", "    m_VertexCount: %d" % vertex_count, text, count=1, flags=re.M)
    text = re.sub(r"^    m_DataSize: .*$", "    m_DataSize: %d" % len(raw), text, count=1, flags=re.M)
    text = re.sub(r"^    _typelessdata: .*$", "    _typelessdata: " + raw.hex(), text, count=1, flags=re.M)

    head, rest = text.split("  m_LocalAABB:\n", 1)
    rest_lines = rest.split("\n")
    if not rest_lines[0].strip().startswith("m_Center") or not rest_lines[1].strip().startswith("m_Extent"):
        raise UnsupportedLayout("unexpected m_LocalAABB layout")
    text = head + "  m_LocalAABB:\n" + _vec("m_Center", centre, 4) + _vec("m_Extent", extent, 4) + "\n".join(rest_lines[2:])
    return text


# --------------------------------------------------------------------------- #
# Scene repair
# --------------------------------------------------------------------------- #


def repair_scene(scene_path, project_root, out_root, guid_map, dry_run=False, manifest_dir=None):
    text = scene_path.read_text(encoding="utf-8")
    doc = parse_document(text)
    graph = SceneGraph(doc)
    label = scene_label(scene_path, project_root)

    result = {
        "scene": str(scene_path.relative_to(project_root)),
        "map": label,
        "renderers_total": 0,
        "renderers_batched": 0,
        "renderers_fixed": 0,
        "colliders_fixed": 0,
        "meshes_written": 0,
        "combined_meshes": [],
        "warnings": [],
    }
    manifest = {
        "scene": scene_path.relative_to(project_root).as_posix(),
        "map": label,
        "generated_by": "tools/debatch_static_meshes.py",
        "renderers": [],
    }

    mesh_cache = {}
    scene_out_dir = out_root / label
    produced = {}
    scene_guid_seed = scene_path.relative_to(project_root).as_posix()

    for obj in doc.objects:
        if obj.class_id != CLASS_MESH_RENDERER:
            continue
        result["renderers_total"] += 1
        go_id, _ = parse_ptr(field_text(obj.body, "m_GameObject"))
        mf_id = graph.component(go_id, CLASS_MESH_FILTER)
        if mf_id is None:
            continue
        mf = graph.objects[mf_id]
        _fid, guid = parse_ptr(field_text(mf.body, "m_Mesh"))
        if not guid or guid not in guid_map:
            continue
        mesh_path = guid_map[guid]
        if guid not in mesh_cache:
            try:
                mesh_cache[guid] = load_mesh(mesh_path)
            except (UnsupportedLayout, OSError) as exc:
                mesh_cache[guid] = None
                # Only combined meshes matter here; other unreadable layouts
                # (e.g. compressed prop meshes) are simply not batched geometry.
                if mesh_path.name.startswith("Combined Mesh"):
                    result["warnings"].append("cannot read %s: %s" % (mesh_path.name, exc))
        combined = mesh_cache[guid]
        if combined is None or not is_combined_mesh(combined):
            continue

        result["renderers_batched"] += 1
        if combined.name not in result["combined_meshes"]:
            result["combined_meshes"].append(mesh_path.name)

        subset_hex = field_text(obj.body, "m_SubsetIndices", "")
        if not subset_hex:
            result["warnings"].append(
                "renderer %d uses %s without m_SubsetIndices - left untouched"
                % (obj.file_id, mesh_path.name)
            )
            continue
        subset = parse_subset_indices(subset_hex)
        if any(i >= len(combined.submeshes) for i in subset):
            result["warnings"].append(
                "renderer %d references submeshes outside %s" % (obj.file_id, mesh_path.name)
            )
            continue
        material_count = len(re.findall(r"- \{fileID: \d+, guid: [0-9a-f]{32}, type: \d+\}",
                                        field_block(obj.body, "m_Materials")))
        if material_count and material_count != len(subset):
            result["warnings"].append(
                "renderer %d has %d materials but %d subset indices"
                % (obj.file_id, material_count, len(subset))
            )

        transform_id = graph.transform_of_go.get(go_id)
        if transform_id is None:
            result["warnings"].append("renderer %d has no Transform" % obj.file_id)
            continue
        world = graph.world_matrix(transform_id)
        go_name = graph.go_name(go_id)
        mesh_name = "%s_%d" % (sanitize(go_name), obj.file_id)
        try:
            asset_text = build_local_mesh(combined, subset, world, mesh_name)
        except UnsupportedLayout as exc:
            result["warnings"].append("renderer %d (%s): %s" % (obj.file_id, go_name, exc))
            continue

        asset_rel = Path(label) / (mesh_name + ".asset")
        new_guid = stable_guid(scene_guid_seed, obj.file_id, mesh_name)
        produced[asset_rel] = (asset_text, new_guid)
        manifest["renderers"].append(
            {
                "renderer": obj.file_id,
                "gameObject": go_name,
                "combined_mesh": mesh_path.name,
                "combined_guid": guid,
                "subset": subset,
                "mesh_guid": new_guid,
                "mesh_asset": (out_root.relative_to(project_root) / asset_rel).as_posix(),
                "vertexCount": sum(combined.submeshes[i].vertex_count for i in subset),
                "indexCount": sum(combined.submeshes[i].index_count for i in subset),
            }
        )

        obj.body = re.sub(
            r"^(  m_SubsetIndices: ).*$",
            lambda m: m.group(1) + format_subset_indices(list(range(len(subset)))),
            obj.body,
            count=1,
            flags=re.M,
        )
        mf.body = replace_mesh_ref(mf.body, new_guid)
        result["renderers_fixed"] += 1

        mc_id = graph.component(go_id, CLASS_MESH_COLLIDER)
        if mc_id is not None:
            mc = graph.objects[mc_id]
            _cfid, cguid = parse_ptr(field_text(mc.body, "m_Mesh", "{fileID: 0}"))
            if cguid == guid:
                mc.body = replace_mesh_ref(mc.body, new_guid)
                result["colliders_fixed"] += 1

    if not produced:
        return result

    if not dry_run:
        scene_out_dir.mkdir(parents=True, exist_ok=True)
        if manifest_dir is not None:
            manifest_dir.mkdir(parents=True, exist_ok=True)
            (manifest_dir / (label + ".json")).write_text(
                json.dumps(manifest, indent=1, ensure_ascii=False) + "\n", encoding="utf-8"
            )
        for rel, (asset_text, guid) in produced.items():
            target = out_root / rel
            target.parent.mkdir(parents=True, exist_ok=True)
            target.write_text(asset_text, encoding="utf-8")
            target.with_name(target.name + ".meta").write_text(
                META_TEMPLATE.format(guid=guid), encoding="utf-8"
            )
        scene_path.write_text(doc.dumps(), encoding="utf-8")
    result["meshes_written"] = len(produced)
    return result


def field_block(body, name):
    """Return the indented block that follows `name:` (used for m_Materials)."""
    m = re.search(r"^(\s*)" + re.escape(name) + r":\s*$", body, re.M)
    if not m:
        return ""
    start = m.end()
    indent = len(m.group(1))
    lines = []
    for line in body[start:].split("\n")[1:]:
        if line.strip() and (len(line) - len(line.lstrip())) <= indent and not line.lstrip().startswith("- "):
            break
        lines.append(line)
    return "\n".join(lines)


def replace_mesh_ref(body, guid):
    return re.sub(
        r"^(\s*m_Mesh: )\{fileID: \d+, guid: [0-9a-f]{32}, type: \d+\}$",
        lambda m: m.group(1) + "{fileID: 4300000, guid: %s, type: 2}" % guid,
        body,
        count=1,
        flags=re.M,
    )


# --------------------------------------------------------------------------- #


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--project", default=str(ROOT / "client"), help="Unity project root (default: client/)")
    parser.add_argument("--out-dir", default="Assets/Mesh/Debatched", help="where de-batched meshes are written")
    parser.add_argument("--scene", action="append", default=[], help="map label or scene path (repeatable)")
    parser.add_argument("--all", action="store_true", help="process every scene under Assets/Levels")
    parser.add_argument("--list", action="store_true", help="only list scenes and their map labels")
    parser.add_argument("--dry-run", action="store_true", help="report without writing anything")
    parser.add_argument("--report", help="write a JSON report to this path")
    parser.add_argument(
        "--manifest-dir",
        default=str(ROOT / "tools" / "static-batch-manifests"),
        help="where per-map manifests for verify_static_batching.py are written",
    )
    args = parser.parse_args(argv)

    project = Path(args.project).resolve()
    scenes = iter_scenes(project)
    if args.list:
        for s in scenes:
            print("%-22s %s" % (scene_label(s, project), s.relative_to(project)))
        return 0

    if args.scene:
        wanted = set(args.scene)
        selected = [s for s in scenes if scene_label(s, project) in wanted or str(s).endswith(tuple(wanted))]
        missing = wanted - {scene_label(s, project) for s in selected}
        if missing and not selected:
            parser.error("no scene matches %s" % ", ".join(sorted(missing)))
    elif args.all:
        selected = scenes
    else:
        parser.error("pass --scene NAME, --all or --list")

    guid_map = build_guid_map(project)
    out_root = project / args.out_dir
    reports = []
    for scene in selected:
        report = repair_scene(
            scene,
            project,
            out_root,
            guid_map,
            dry_run=args.dry_run,
            manifest_dir=Path(args.manifest_dir),
        )
        reports.append(report)
        print(
            "%-22s renderers=%-4d batched=%-4d fixed=%-4d meshes=%-4d colliders=%-3d %s"
            % (
                report["map"],
                report["renderers_total"],
                report["renderers_batched"],
                report["renderers_fixed"],
                report["meshes_written"],
                report["colliders_fixed"],
                "WARN:%d" % len(report["warnings"]) if report["warnings"] else "",
            )
        )
        for w in report["warnings"][:5]:
            print("    ! " + w)

    totals = {
        "scenes": len(reports),
        "renderers_batched": sum(r["renderers_batched"] for r in reports),
        "renderers_fixed": sum(r["renderers_fixed"] for r in reports),
        "meshes_written": sum(r["meshes_written"] for r in reports),
        "colliders_fixed": sum(r["colliders_fixed"] for r in reports),
        "warnings": sum(len(r["warnings"]) for r in reports),
    }
    print("TOTAL %s%s" % (totals, " (dry run)" if args.dry_run else ""))
    if args.report:
        Path(args.report).write_text(
            json.dumps({"totals": totals, "scenes": reports}, indent=2, ensure_ascii=False) + "\n",
            encoding="utf-8",
        )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
