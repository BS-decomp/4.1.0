#!/usr/bin/env python3
"""Factual audit of the recovered 4.1.0 project (no Unity required).

Reports, per scene and project-wide:

* renderers still pointing at a `Combined Mesh (root: scene)` (static batching);
* `m_Script` GUIDs that no asset in the project provides (missing MonoBehaviour);
* materials whose shader asset is an AssetRipper placeholder
  (`//DummyShaderTextExporter`) — these render wrong even though they compile;
* lightmap references that do not resolve to a texture in the project;
* scenes missing from `ProjectSettings/EditorBuildSettings.asset`.

    python3 tools/audit_project_410.py --report docs/audit-410.json
"""

from __future__ import annotations

import argparse
import json
import re
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))

from static_batch_lib import build_guid_map, iter_scenes  # noqa: E402

ROOT = Path(__file__).resolve().parent.parent
PROJECT = ROOT / "client"
BUILTIN_GUIDS = {"0000000000000000e000000000000000", "0000000000000000f000000000000000"}
DUMMY_MARKERS = ("DummyShaderTextExporter", "Shader created for shader asset")

MESH_RE = re.compile(r"m_Mesh: \{fileID: \d+, guid: ([0-9a-f]{32})")
SCRIPT_RE = re.compile(r"m_Script: \{fileID: -?\d+, guid: ([0-9a-f]{32})")
MATERIAL_RE = re.compile(r"- \{fileID: 2100000, guid: ([0-9a-f]{32})")
LIGHTMAP_RE = re.compile(r"m_Lightmap(?:Far|Near|)?: \{fileID: \d+, guid: ([0-9a-f]{32})")


def shader_of_material(path):
    try:
        text = path.read_text(encoding="utf-8", errors="replace")
    except OSError:
        return None
    m = re.search(r"m_Shader: \{fileID: \d+, guid: ([0-9a-f]{32})", text)
    return m.group(1) if m else None


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--report", help="write the full JSON report here")
    args = parser.parse_args(argv)

    guid_map = build_guid_map(PROJECT)
    dummy_shaders = {}
    for guid, path in guid_map.items():
        if path.suffix == ".shader":
            try:
                head = path.read_text(encoding="utf-8", errors="replace")[:4000]
            except OSError:
                continue
            if any(marker in head for marker in DUMMY_MARKERS):
                dummy_shaders[guid] = path.name

    build_settings = (PROJECT / "ProjectSettings" / "EditorBuildSettings.asset").read_text(encoding="utf-8")
    in_build = set(re.findall(r"path: (.+\.unity)", build_settings))

    scenes = []
    totals = {
        "scenes": 0,
        "combined_mesh_renderers": 0,
        "missing_scripts": 0,
        "materials_with_dummy_shader": 0,
        "missing_lightmaps": 0,
        "scenes_not_in_build": 0,
    }
    dummy_usage = {}

    for scene in iter_scenes(PROJECT):
        text = scene.read_text(encoding="utf-8", errors="replace")
        rel = scene.relative_to(PROJECT).as_posix()
        entry = {"scene": rel, "name": scene.stem}

        combined = 0
        for guid in MESH_RE.findall(text):
            path = guid_map.get(guid)
            if path and path.name.startswith("Combined Mesh"):
                combined += 1
        entry["combined_mesh_renderers"] = combined

        missing = sorted({g for g in SCRIPT_RE.findall(text) if g not in guid_map and g not in BUILTIN_GUIDS})
        entry["missing_scripts"] = missing

        dummies = {}
        for guid in set(MATERIAL_RE.findall(text)):
            path = guid_map.get(guid)
            if path is None or path.suffix != ".mat":
                continue
            shader_guid = shader_of_material(path)
            if shader_guid in dummy_shaders:
                dummies[path.name] = dummy_shaders[shader_guid]
                dummy_usage.setdefault(dummy_shaders[shader_guid], set()).add(scene.stem)
        entry["materials_with_dummy_shader"] = dummies

        lightmaps = set(LIGHTMAP_RE.findall(text))
        missing_lm = sorted(g for g in lightmaps if g not in guid_map and g not in BUILTIN_GUIDS)
        entry["lightmaps"] = len(lightmaps)
        entry["missing_lightmaps"] = missing_lm

        entry["in_build_settings"] = ("Assets/" + rel.split("Assets/", 1)[-1]) in in_build

        totals["scenes"] += 1
        totals["combined_mesh_renderers"] += combined
        totals["missing_scripts"] += len(missing)
        totals["materials_with_dummy_shader"] += len(dummies)
        totals["missing_lightmaps"] += len(missing_lm)
        totals["scenes_not_in_build"] += 0 if entry["in_build_settings"] else 1
        scenes.append(entry)

    print("scenes                       : %d" % totals["scenes"])
    print("renderers on combined meshes : %d" % totals["combined_mesh_renderers"])
    print("missing script GUIDs         : %d" % totals["missing_scripts"])
    print("placeholder shader assets    : %d of %d shaders" % (
        len(dummy_shaders), sum(1 for p in guid_map.values() if p.suffix == ".shader")))
    print("materials on placeholders    : %d (scene references)" % totals["materials_with_dummy_shader"])
    print("missing lightmap textures    : %d" % totals["missing_lightmaps"])
    print("scenes not in Build Settings : %d" % totals["scenes_not_in_build"])
    if dummy_usage:
        print("\nplaceholder shaders actually used by maps:")
        for shader, maps in sorted(dummy_usage.items(), key=lambda kv: -len(kv[1])):
            print("  %-46s %2d maps (%s%s)" % (
                shader, len(maps), ", ".join(sorted(maps)[:4]), ", ..." if len(maps) > 4 else ""))

    if args.report:
        Path(args.report).write_text(
            json.dumps(
                {
                    "totals": totals,
                    "placeholder_shaders": sorted(dummy_shaders.values()),
                    "placeholder_usage": {k: sorted(v) for k, v in dummy_usage.items()},
                    "scenes": scenes,
                },
                indent=1,
                ensure_ascii=False,
            )
            + "\n",
            encoding="utf-8",
        )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
