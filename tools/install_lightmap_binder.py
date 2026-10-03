#!/usr/bin/env python3
"""Re-bind the Unity 4 lightmaps of every exported scene.

Unity 5 moved the baked lightmap array out of `LightmapSettings` and into the
LightingData asset, so the Unity 4 block the export still carries

    m_Lightmaps:
    - m_Lightmap: {fileID: 2800000, guid: ..., type: 3}
      m_IndirectLightmap: {fileID: 0}

is ignored by Unity 2021: `LightmapSettings.lightmaps` stays empty, LIGHTMAP_ON
is never set and the maps render flat. The renderers themselves are fine — all
9353 of them still have `m_LightmapIndex` and `m_LightmapTilingOffset`.

This tool reads the original block (ground truth), then adds one small
`BSLegacyLightmaps` object to the scene that re-binds those exact textures at
load time, in their original order. The legacy block is left untouched so the
export can always be re-read.

    python3 tools/install_lightmap_binder.py --report
    python3 tools/install_lightmap_binder.py --apply
    python3 tools/verify_lightmaps.py
"""

from __future__ import annotations

import argparse
import hashlib
import json
import re
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))

from static_batch_lib import build_guid_map, iter_scenes  # noqa: E402

ROOT = Path(__file__).resolve().parent.parent
PROJECT = ROOT / "client"
SCRIPT_PATH = PROJECT / "Assets" / "Scripts" / "Recovery" / "BSLegacyLightmaps.cs"
MANIFEST = ROOT / "docs" / "lightmaps-410.json"
OBJECT_NAME = "BS Legacy Lightmaps"

LIGHTMAP_RE = re.compile(
    r"- m_Lightmap: \{fileID: (\d+), guid: ([0-9a-f]{32}), type: (\d+)\}"
)
BLOCK_RE = re.compile(r"^--- !u!(\d+) &(\d+)", re.M)
RENDERER_RE = re.compile(
    r"^--- !u!(?:23|137) &(\d+).*?\n(.*?)(?=^--- !u!|\Z)", re.M | re.S)
LM_INDEX_RE = re.compile(r"^\s*m_LightmapIndex: (\d+)$", re.M)
LM_ST_RE = re.compile(
    r"^\s*m_LightmapTilingOffset: \{x: ([-\d.eE+]+), y: ([-\d.eE+]+), z: ([-\d.eE+]+), w: ([-\d.eE+]+)\}$", re.M)


def script_guid():
    """Deterministic GUID for the recovery script, written into its .meta."""
    meta = SCRIPT_PATH.with_name(SCRIPT_PATH.name + ".meta")
    if meta.exists():
        m = re.search(r"^guid: ([0-9a-f]{32})", meta.read_text(encoding="utf-8"), re.M)
        if m:
            return m.group(1)
    guid = hashlib.md5(b"BS-decomp/4.1.0 BSLegacyLightmaps").hexdigest()
    meta.write_text(
        "fileFormatVersion: 2\nguid: %s\nMonoImporter:\n  externalObjects: {}\n"
        "  serializedVersion: 2\n  defaultReferences: []\n  executionOrder: 0\n"
        "  icon: {instanceID: 0}\n  userData:\n  assetBundleName:\n  assetBundleVariant:\n" % guid,
        encoding="utf-8",
    )
    return guid


def scene_lightmaps(text):
    """The legacy lightmap list of a scene, in order."""
    start = text.find("  m_Lightmaps:")
    if start < 0:
        return []
    end = text.find("\n  m_LightmapsMode:", start)
    if end < 0:
        end = start + 4000
    return [(int(f), g, int(t)) for f, g, t in LIGHTMAP_RE.findall(text[start:end])]


def lightmapped_renderers(text, guid_map):
    """(name, world position, index, scaleOffset) of every baked renderer.

    Unity drops scene-local `{fileID: N}` references when it imports these
    Unity 4 scenes, so the binder identifies renderers by name + world position
    instead — stable, and independent of how Unity renumbers the file.
    """
    from static_batch_lib import parse_document, SceneGraph, field_text, parse_ptr

    doc = parse_document(text)
    graph = SceneGraph(doc)
    out = []
    for obj in doc.objects:
        if obj.class_id not in (23, 137):
            continue
        index = LM_INDEX_RE.search(obj.body)
        if index is None:
            continue
        value = int(index.group(1))
        if value >= 65534:        # 65535 = none, 65534 = unassigned
            continue
        st = LM_ST_RE.search(obj.body)
        scale = tuple(float(x) for x in st.groups()) if st else (1.0, 1.0, 0.0, 0.0)
        go_id, _ = parse_ptr(field_text(obj.body, "m_GameObject"))
        transform_id = graph.transform_of_go.get(go_id)
        if transform_id is None:
            continue
        matrix = graph.world_matrix(transform_id)
        position = (matrix[0][3], matrix[1][3], matrix[2][3])
        out.append((graph.go_name(go_id), position, value, scale, mesh_name(graph, go_id, guid_map)))
    return out


_MESH_NAMES = {}


def mesh_name(graph, go_id, guid_map):
    """m_Name of the mesh the renderer draws — the de-batched meshes are unique
    per renderer, which disambiguates objects that share a name and a position."""
    mf_id = graph.component(go_id, 33)
    if mf_id is None:
        return ""
    ptr = re.search(r"m_Mesh: \{fileID: \d+, guid: ([0-9a-f]{32})", graph.objects[mf_id].body)
    if ptr is None:
        return ""
    guid = ptr.group(1)
    if guid not in _MESH_NAMES:
        path = guid_map.get(guid)
        name = ""
        if path is not None and path.exists():
            head = path.read_text(encoding="utf-8", errors="replace")[:4000]
            m = re.search(r"^  m_Name: (.*)$", head, re.M)
            if m:
                name = m.group(1).strip().strip("'\"")
        _MESH_NAMES[guid] = name
    return _MESH_NAMES[guid]


def free_ids(text, count):
    used = {int(m.group(2)) for m in BLOCK_RE.finditer(text)}
    ids = []
    candidate = 990000001
    while len(ids) < count:
        if candidate not in used:
            ids.append(candidate)
            used.add(candidate)
        candidate += 1
    return ids


def number(value):
    """Compact, culture-independent float for Unity YAML."""
    text = "%.6g" % value
    return text if text not in ("-0", "-0.0") else "0"


def yaml_string(value):
    if value == "" or any(c in value for c in ":#{}[],&*?|-<>=!%@`\"'") or value != value.strip():
        return '"%s"' % value.replace("\\", "\\\\").replace('"', '\\"')
    return value


def build_blocks(go_id, tr_id, mb_id, guid, lightmaps, renderers):
    refs = "\n".join(
        "  - {fileID: %d, guid: %s, type: %d}" % (f, g, t) for f, g, t in lightmaps
    )
    names = "\n".join("  - %s" % yaml_string(r[0]) for r in renderers)
    positions = "\n".join(
        "  - {x: %s, y: %s, z: %s}" % tuple(number(v) for v in r[1]) for r in renderers)
    indices = "\n".join("  - %d" % r[2] for r in renderers)
    scales = "\n".join(
        "  - {x: %s, y: %s, z: %s, w: %s}" % tuple(number(v) for v in r[3]) for r in renderers)
    meshes = "\n".join("  - %s" % yaml_string(r[4]) for r in renderers)
    if renderers:
        renderer_block = ("  renderNames:\n%s\n  renderPositions:\n%s\n  renderMeshes:\n%s\n"
                          "  lightmapIndices:\n%s\n  lightmapScaleOffsets:\n%s\n"
                          % (names, positions, meshes, indices, scales))
    else:
        renderer_block = ("  renderNames: []\n  renderPositions: []\n  renderMeshes: []\n"
                          "  lightmapIndices: []\n  lightmapScaleOffsets: []\n")
    return (
        "--- !u!1 &%d\n"
        "GameObject:\n"
        "  m_ObjectHideFlags: 0\n"
        "  m_PrefabParentObject: {fileID: 0}\n"
        "  m_PrefabInternal: {fileID: 0}\n"
        "  serializedVersion: 4\n"
        "  m_Component:\n"
        "  - 4: {fileID: %d}\n"
        "  - 114: {fileID: %d}\n"
        "  m_Layer: 0\n"
        "  m_Name: %s\n"
        "  m_TagString: Untagged\n"
        "  m_Icon: {fileID: 0}\n"
        "  m_NavMeshLayer: 0\n"
        "  m_StaticEditorFlags: 0\n"
        "  m_IsActive: 1\n"
        "--- !u!4 &%d\n"
        "Transform:\n"
        "  m_ObjectHideFlags: 0\n"
        "  m_PrefabParentObject: {fileID: 0}\n"
        "  m_PrefabInternal: {fileID: 0}\n"
        "  m_GameObject: {fileID: %d}\n"
        "  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}\n"
        "  m_LocalPosition: {x: 0, y: 0, z: 0}\n"
        "  m_LocalScale: {x: 1, y: 1, z: 1}\n"
        "  m_Children: []\n"
        "  m_Father: {fileID: 0}\n"
        "  m_RootOrder: 0\n"
        "  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}\n"
        "--- !u!114 &%d\n"
        "MonoBehaviour:\n"
        "  m_ObjectHideFlags: 0\n"
        "  m_PrefabParentObject: {fileID: 0}\n"
        "  m_PrefabInternal: {fileID: 0}\n"
        "  m_GameObject: {fileID: %d}\n"
        "  m_Enabled: 1\n"
        "  m_EditorHideFlags: 0\n"
        "  m_Script: {fileID: 11500000, guid: %s, type: 3}\n"
        "  m_Name: \n"
        "  m_EditorClassIdentifier: \n"
        "  lightmapsFar:\n%s\n"
        "  lightmapsNear: []\n"
        "%s"
        % (go_id, tr_id, mb_id, OBJECT_NAME, tr_id, go_id, mb_id, go_id, guid, refs, renderer_block)
    )


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--apply", action="store_true")
    parser.add_argument("--report", action="store_true")
    args = parser.parse_args(argv)

    guid = script_guid()
    guid_map = build_guid_map(PROJECT)
    manifest = {"script_guid": guid, "scenes": []}
    installed = skipped = missing = 0

    for scene in iter_scenes(PROJECT):
        text = scene.read_text(encoding="utf-8")
        lightmaps = scene_lightmaps(text)
        rel = scene.relative_to(PROJECT).as_posix()
        if not lightmaps:
            if args.report:
                print("%-56s no lightmaps" % scene.stem)
            continue

        resolved = []
        for fid, g, kind in lightmaps:
            path = guid_map.get(g)
            if path is None:
                missing += 1
                print("  ! %s: lightmap texture %s is missing from the project" % (scene.stem, g))
            resolved.append({"fileID": fid, "guid": g, "type": kind,
                             "asset": path.relative_to(PROJECT).as_posix() if path else None})

        renderers = lightmapped_renderers(text, guid_map)
        has_binder = ("m_Script: {fileID: 11500000, guid: %s" % guid) in text
        manifest["scenes"].append({"scene": rel, "name": scene.stem, "lightmaps": resolved,
                                   "lightmapped_renderers": len(renderers), "binder": True})

        if has_binder:
            skipped += 1
            if args.report:
                print("%-56s already bound (%d)" % (scene.stem, len(lightmaps)))
            continue

        if args.report:
            print("%-56s %d lightmap(s), %d renderer(s) -> will bind"
                  % (scene.stem, len(lightmaps), len(renderers)))
        if args.apply:
            go_id, tr_id, mb_id = free_ids(text, 3)
            block = build_blocks(go_id, tr_id, mb_id, guid, lightmaps, renderers)
            if not text.endswith("\n"):
                text += "\n"
            scene.write_text(text + block, encoding="utf-8")
        installed += 1

    MANIFEST.parent.mkdir(parents=True, exist_ok=True)
    MANIFEST.write_text(json.dumps(manifest, indent=1, ensure_ascii=False) + "\n", encoding="utf-8")

    print("\nscenes with lightmaps: %d | %s: %d | already bound: %d | missing textures: %d"
          % (len(manifest["scenes"]), "bound" if args.apply else "to bind", installed, skipped, missing))
    print("manifest: %s" % MANIFEST.relative_to(ROOT))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
