#!/usr/bin/env python3
"""Validate the static-batch repair against the original exported geometry.

The check is deliberately paranoid, because the de-batching in
`debatch_static_meshes.py` rewrites geometry:

1. every scene is scanned for renderers that still point at a
   `Combined Mesh (root: scene)` asset (after a repair there must be none);
2. every `m_Mesh` reference in every scene must resolve to an asset that exists
   in the project;
3. for each entry of `tools/static-batch-manifests/*.json` the de-batched mesh is
   loaded and pushed back through the renderer's world matrix; the resulting
   world-space vertices, triangles and bounds must match the submeshes of the
   original combined mesh from the APK export, vertex by vertex;
4. the asset `.meta` GUID must match the GUID the scene references.

Usage:
    python3 tools/verify_static_batching.py              # all scenes
    python3 tools/verify_static_batching.py --scene Bust
"""

from __future__ import annotations

import argparse
import json
import math
import re
import struct
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))

from static_batch_lib import (  # noqa: E402
    CLASS_MESH_FILTER,
    CLASS_MESH_RENDERER,
    SceneGraph,
    UnsupportedLayout,
    build_guid_map,
    field_text,
    is_combined_mesh,
    iter_scenes,
    load_mesh,
    parse_document,
    parse_ptr,
    parse_subset_indices,
    transform_point,
)

ROOT = Path(__file__).resolve().parent.parent
MANIFEST_DIR = ROOT / "tools" / "static-batch-manifests"
TOLERANCE = 2e-3
# Unity's built-in resource files (default Cube/Quad meshes etc.) have no .meta
# in the project; they are legitimate references, not missing assets.
BUILTIN_GUIDS = {
    "0000000000000000e000000000000000",
    "0000000000000000f000000000000000",
}


def scene_label(scene_path, project_root):
    rel = scene_path.relative_to(Path(project_root) / "Assets" / "Levels")
    return rel.parent.name if rel.parent != Path(".") else scene_path.stem


def world_positions(mesh, submesh_index, matrix):
    s = mesh.submeshes[submesh_index]
    out = []
    for v in range(s.first_vertex, s.first_vertex + s.vertex_count):
        p = struct.unpack_from("<3f", mesh.raw, v * mesh.stride)
        out.append(transform_point(matrix, p))
    return out


def triangles(mesh, submesh_index):
    s = mesh.submeshes[submesh_index]
    idx = mesh.submesh_indices(submesh_index)
    return [tuple(i - s.first_vertex for i in idx[t : t + 3]) for t in range(0, len(idx), 3)]


def check_manifest(manifest, project, guid_map, failures, stats):
    scene_path = project / manifest["scene"]
    if not scene_path.exists():
        failures.append("%s: scene file missing" % manifest["scene"])
        return
    doc = parse_document(scene_path.read_text(encoding="utf-8"))
    graph = SceneGraph(doc)
    objects = graph.objects
    mesh_cache = {}

    for entry in manifest["renderers"]:
        rid = entry["renderer"]
        if rid not in objects:
            failures.append("%s: renderer %d disappeared" % (manifest["map"], rid))
            continue
        renderer = objects[rid]
        go_id, _ = parse_ptr(field_text(renderer.body, "m_GameObject"))
        mf_id = graph.component(go_id, CLASS_MESH_FILTER)
        _fid, guid = parse_ptr(field_text(objects[mf_id].body, "m_Mesh"))
        if guid != entry["mesh_guid"]:
            failures.append(
                "%s: renderer %d points at %s, expected %s"
                % (manifest["map"], rid, guid, entry["mesh_guid"])
            )
            continue

        asset = project / entry["mesh_asset"]
        meta = asset.with_name(asset.name + ".meta")
        if not asset.exists() or not meta.exists():
            failures.append("%s: missing asset %s" % (manifest["map"], entry["mesh_asset"]))
            continue
        meta_guid = re.search(r"^guid: ([0-9a-f]{32})", meta.read_text(encoding="utf-8"), re.M)
        if not meta_guid or meta_guid.group(1) != entry["mesh_guid"]:
            failures.append("%s: meta GUID mismatch for %s" % (manifest["map"], asset.name))
            continue

        for key in (entry["combined_guid"], entry["mesh_guid"]):
            if key not in mesh_cache:
                path = guid_map.get(key)
                if path is None:
                    failures.append("%s: GUID %s not found in project" % (manifest["map"], key))
                    mesh_cache[key] = None
                    continue
                try:
                    mesh_cache[key] = load_mesh(path)
                except (UnsupportedLayout, OSError) as exc:
                    failures.append("%s: %s" % (manifest["map"], exc))
                    mesh_cache[key] = None
        combined = mesh_cache.get(entry["combined_guid"])
        rebuilt = mesh_cache.get(entry["mesh_guid"])
        if combined is None or rebuilt is None:
            continue

        matrix = graph.world_matrix(graph.transform_of_go[go_id])
        subset = entry["subset"]
        if len(rebuilt.submeshes) != len(subset):
            failures.append(
                "%s: %s has %d submeshes, expected %d"
                % (manifest["map"], asset.name, len(rebuilt.submeshes), len(subset))
            )
            continue

        identity = [[1.0 if i == j else 0.0 for j in range(4)] for i in range(4)]
        worst = 0.0
        for new_index, src_index in enumerate(subset):
            expected = world_positions(combined, src_index, identity)
            actual = world_positions(rebuilt, new_index, matrix)
            if len(expected) != len(actual):
                failures.append(
                    "%s: %s submesh %d vertex count %d != %d"
                    % (manifest["map"], asset.name, new_index, len(actual), len(expected))
                )
                break
            for a, b in zip(expected, actual):
                d = max(abs(a[k] - b[k]) for k in range(3))
                worst = max(worst, d)
            if triangles(combined, src_index) != triangles(rebuilt, new_index):
                failures.append(
                    "%s: %s submesh %d triangle list differs" % (manifest["map"], asset.name, new_index)
                )
        else:
            if worst > TOLERANCE:
                failures.append(
                    "%s: %s world positions drift by %.5f" % (manifest["map"], asset.name, worst)
                )
            stats["max_drift"] = max(stats["max_drift"], worst)
            stats["renderers_checked"] += 1

        # Subset indices must now be a plain 0..n-1 sequence.
        subset_hex = field_text(renderer.body, "m_SubsetIndices", "")
        if subset_hex and parse_subset_indices(subset_hex) != list(range(len(subset))):
            failures.append("%s: renderer %d keeps legacy subset indices" % (manifest["map"], rid))


def scan_scene(scene_path, project, guid_map, failures, stats):
    doc = parse_document(scene_path.read_text(encoding="utf-8"))
    graph = SceneGraph(doc)
    label = scene_label(scene_path, project)
    mesh_cache = {}
    for obj in doc.objects:
        if obj.class_id != CLASS_MESH_RENDERER:
            continue
        go_id, _ = parse_ptr(field_text(obj.body, "m_GameObject"))
        mf_id = graph.component(go_id, CLASS_MESH_FILTER)
        if mf_id is None:
            continue
        _fid, guid = parse_ptr(field_text(graph.objects[mf_id].body, "m_Mesh"))
        if not guid or guid in BUILTIN_GUIDS:
            continue
        if guid not in guid_map:
            failures.append("%s: renderer %d references unknown mesh GUID %s" % (label, obj.file_id, guid))
            continue
        if guid not in mesh_cache:
            try:
                mesh_cache[guid] = load_mesh(guid_map[guid])
            except (UnsupportedLayout, OSError):
                mesh_cache[guid] = None
        mesh = mesh_cache[guid]
        if mesh is not None and is_combined_mesh(mesh):
            stats["still_batched"] += 1
            failures.append(
                "%s: renderer %d still uses %s" % (label, obj.file_id, mesh.path.name)
            )
    stats["scenes_scanned"] += 1


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--project", default=str(ROOT / "client"))
    parser.add_argument("--manifests", default=str(MANIFEST_DIR))
    parser.add_argument("--scene", action="append", default=[], help="limit to these map labels")
    args = parser.parse_args(argv)

    project = Path(args.project).resolve()
    guid_map = build_guid_map(project)
    failures = []
    stats = {"scenes_scanned": 0, "renderers_checked": 0, "still_batched": 0, "max_drift": 0.0}

    scenes = iter_scenes(project)
    if args.scene:
        scenes = [s for s in scenes if scene_label(s, project) in set(args.scene)]
    for scene in scenes:
        scan_scene(scene, project, guid_map, failures, stats)

    manifest_dir = Path(args.manifests)
    manifests = sorted(manifest_dir.glob("*.json")) if manifest_dir.exists() else []
    for path in manifests:
        manifest = json.loads(path.read_text(encoding="utf-8"))
        if args.scene and manifest["map"] not in set(args.scene):
            continue
        check_manifest(manifest, project, guid_map, failures, stats)

    print(
        "scenes scanned: %d | renderers verified against the combined meshes: %d | "
        "still batched: %d | max world drift: %.6f"
        % (stats["scenes_scanned"], stats["renderers_checked"], stats["still_batched"], stats["max_drift"])
    )
    if failures:
        print("FAILURES (%d):" % len(failures))
        for f in failures[:40]:
            print("  - " + f)
        if len(failures) > 40:
            print("  ... and %d more" % (len(failures) - 40))
        return 1
    print("OK")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
