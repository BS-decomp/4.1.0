#!/usr/bin/env python3
"""Check that every scene's baked lightmaps are present, bound and usable.

Checks per scene:

1. the original Unity 4 `LightmapSettings.m_Lightmaps` list (ground truth from
   the export) still exists and every texture it names is in the project;
2. a `BSLegacyLightmaps` binder is present and references **exactly** those
   textures, in the same order (index 0 must stay index 0 — renderers address
   lightmaps by `m_LightmapIndex`);
3. every `m_LightmapIndex` used by renderers is inside that range;
4. the materials of lightmapped renderers use a shader that can sample a
   lightmap (`LIGHTMAP_ON` / `unity_Lightmap`), otherwise the map renders flat
   no matter how well the textures are bound.

    python3 tools/verify_lightmaps.py
"""

from __future__ import annotations

import argparse
import re
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))

from static_batch_lib import build_guid_map, iter_scenes  # noqa: E402

ROOT = Path(__file__).resolve().parent.parent
PROJECT = ROOT / "client"
LEGACY_RE = re.compile(r"- m_Lightmap: \{fileID: \d+, guid: ([0-9a-f]{32}), type: \d+\}")
BINDER_RE = re.compile(
    r"lightmapsFar:\n((?:  - \{fileID: \d+, guid: [0-9a-f]{32}, type: \d+\}\n)*)")
REF_RE = re.compile(r"guid: ([0-9a-f]{32})")
INDEX_RE = re.compile(r"m_LightmapIndex: (\d+)")
MATERIAL_RE = re.compile(r"- \{fileID: 2100000, guid: ([0-9a-f]{32})")


def legacy_list(text):
    start = text.find("  m_Lightmaps:")
    if start < 0:
        return []
    end = text.find("\n  m_LightmapsMode:", start)
    return LEGACY_RE.findall(text[start:end if end > 0 else start + 4000])


def binder_field(text, name):
    m = re.search(r"  %s:\n((?:  - [^\n]*\n)*)" % name, text)
    if not m or not m.group(1).strip():
        return []
    return [line.strip()[2:] for line in m.group(1).rstrip("\n").split("\n")]


def binder_renderers(text):
    """(renderer keys, indices, scale offsets) recorded by the binder."""
    names = binder_field(text, "renderNames")
    positions = binder_field(text, "renderPositions")
    meshes = binder_field(text, "renderMeshes")
    indices = binder_field(text, "lightmapIndices")
    scales = binder_field(text, "lightmapScaleOffsets")
    keys = list(zip(names, positions, meshes))
    return len(names), len(indices), len(scales), len(set(keys)), len(positions), len(meshes)


def binder_list(text):
    m = BINDER_RE.search(text)
    return REF_RE.findall(m.group(1)) if m else None


def lightmap_capable(shader_text):
    # Either the engine path (LIGHTMAP_ON / unity_Lightmap) or the recovery
    # path (_BSLightmap + _BSLightmapST through a MaterialPropertyBlock) can
    # sample a lightmap; both count.
    return ("LIGHTMAP_ON" in shader_text or "unity_Lightmap" in shader_text
            or ("_BSLightmap" in shader_text and "_BSLightmapST" in shader_text))


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--quiet", action="store_true")
    args = parser.parse_args(argv)

    guid_map = build_guid_map(PROJECT)
    shader_cache = {}
    failures = []
    stats = {"scenes": 0, "with_lightmaps": 0, "bound": 0, "textures": 0, "flat_materials": 0}

    for scene in iter_scenes(PROJECT):
        text = scene.read_text(encoding="utf-8", errors="replace")
        stats["scenes"] += 1
        legacy = legacy_list(text)
        if not legacy:
            continue
        stats["with_lightmaps"] += 1

        for guid in legacy:
            if guid not in guid_map:
                failures.append("%s: baked lightmap %s is missing from the project" % (scene.stem, guid))
            else:
                stats["textures"] += 1

        bound = binder_list(text)
        if bound is None:
            failures.append("%s: no BSLegacyLightmaps binder — Unity 2021 ignores the Unity 4 block, "
                            "run tools/install_lightmap_binder.py --apply" % scene.stem)
        elif bound != legacy:
            failures.append("%s: binder list %s != original %s" % (scene.stem, bound, legacy))
        else:
            stats["bound"] += 1

        # the binder must restore every renderer Unity would otherwise reset
        baked = sum(1 for i in INDEX_RE.findall(text) if int(i) < 65534)
        refs, idx, st, unique, positions, meshes = binder_renderers(text)
        if bound is not None and not (refs == idx == st == positions == meshes == baked):
            failures.append("%s: binder covers names %d / positions %d / meshes %d / indices %d / "
                            "offsets %d, scene has %d baked renderers"
                            % (scene.stem, refs, positions, meshes, idx, st, baked))
        elif unique != refs:
            failures.append("%s: %d of %d binder keys are ambiguous (same name, position and mesh) — "
                            "those renderers cannot be matched reliably" % (scene.stem, refs - unique, refs))
        else:
            stats["renderers"] = stats.get("renderers", 0) + refs

        indices = {int(i) for i in INDEX_RE.findall(text)}
        # 65535 = no lightmap, 65534 = lightmapped but unassigned (Unity 5+ sentinels)
        bad = sorted(i for i in indices if i < 65534 and i >= len(legacy))
        if bad:
            failures.append("%s: renderers use lightmap index %s but only %d lightmap(s) exist"
                            % (scene.stem, bad[:5], len(legacy)))

        flat = set()
        for guid in set(MATERIAL_RE.findall(text)):
            path = guid_map.get(guid)
            if path is None or path.suffix != ".mat":
                continue
            m = re.search(r"m_Shader: \{fileID: \d+, guid: ([0-9a-f]{32})",
                          path.read_text(encoding="utf-8", errors="replace"))
            if not m:
                continue
            shader_path = guid_map.get(m.group(1))
            if shader_path is None or shader_path.suffix != ".shader":
                continue
            if shader_path not in shader_cache:
                shader_cache[shader_path] = lightmap_capable(
                    shader_path.read_text(encoding="utf-8", errors="replace"))
            if not shader_cache[shader_path] and shader_path.name == "Mobile-Lightmap-Unlit.shader":
                flat.add(shader_path.name)
        if flat:
            stats["flat_materials"] += len(flat)
            failures.append("%s: lightmap shader(s) cannot sample a lightmap: %s"
                            % (scene.stem, ", ".join(sorted(flat))))

        if not args.quiet:
            print("%-26s lightmaps %d  binder %s" % (
                scene.stem, len(legacy), "ok" if bound == legacy else "MISSING"))

    print("\nscenes %d | with lightmaps %d | correctly bound %d | textures resolved %d | "
          "renderers restored %d"
          % (stats["scenes"], stats["with_lightmaps"], stats["bound"], stats["textures"],
             stats.get("renderers", 0)))
    if failures:
        print("FAILURES (%d):" % len(failures))
        for f in failures[:40]:
            print("  - " + f)
        return 1
    print("OK")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
