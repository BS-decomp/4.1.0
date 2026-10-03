#!/usr/bin/env python3
"""Check the rebuilt shaders against the ground truth extracted from the APK.

For every shader asset in `client/Assets/Shader` that has a counterpart in
`tools/shader-extract`:

* the declared shader name must match;
* the property list must match exactly — name, label, type and default value
  (this is what materials bind to, so a single typo silently breaks them);
* the render state of the first pass (Blend / ZWrite / Cull / ColorMask /
  Offset / LightMode) must match, except for the shaders whose passes Unity
  generates from a surface program (listed below with the reason);
* no AssetRipper placeholder may be left.

    python3 tools/verify_shaders.py
"""

from __future__ import annotations

import argparse
import json
import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
SHADER_DIR = ROOT / "client" / "Assets" / "Shader"
EXTRACT_DIR = ROOT / "tools" / "shader-extract"
DUMMY_MARKERS = ("DummyShaderTextExporter", "Shader created for shader asset")

# Shaders whose passes Unity generates from `#pragma surface`; the pass state in
# the APK is the compiler's output, not something we can or should hand-write.
SURFACE_GENERATED = {
    "Mobile/VertexLit (Only Directional Lights)": "surface Lambert noforwardadd",
    "ProBuilder/Diffuse Vertex Color": "surface Lambert",
}
# Fixed-function state that has no modern equivalent and is reproduced
# differently (documented in the shader files themselves).
STATE_EXCEPTIONS = {
    # The legacy Vertex / VertexLM / VertexLMRGBM pass family cannot be
    # reproduced: `unity_LightmapMatrix` and the fixed-function combiners are
    # gone, and a CG pass tagged "Vertex" never receives the LIGHTMAP_ON
    # keyword. Both shaders are folded into one ForwardBase pass instead.
    "Mobile/Unlit (Supports Lightmap)": {"LightMode"},
    "Mobile/VertexLit": {"LightMode", "Offset"},
    "ProBuilder/Unlit Solid Color": {"AlphaTest"},   # -> clip()
    "ProBuilder/UnlitVertexColor": {"AlphaTest"},    # -> clip()
    "Particles/Additive": {"AlphaTest"},             # -> clip()
}
STATE_KEYS = ("Blend", "ZWrite", "Cull", "ColorMask", "Offset", "AlphaTest", "ZTest")

PROPERTY_RE = re.compile(r"^\s*([_A-Za-z][\w]*)\s*\(\s*\"([^\"]*)\"\s*,\s*([^)]+)\)\s*=\s*(.+?)\s*$", re.M)


def properties(text):
    block = re.search(r"Properties\s*\{(.*?)\n\}", text, re.S)
    if not block:
        return []
    out = []
    for m in PROPERTY_RE.finditer(block.group(1)):
        name, label, kind, default = m.groups()
        kind = re.sub(r"\s+", "", kind)
        default = default.strip().rstrip("{}").strip()
        default = re.sub(r"\s+", "", default)
        out.append((name, label, kind, default))
    return out


def first_pass_block(text):
    """Return the text of the first Pass of the first SubShader."""
    sub = re.search(r"SubShader\s*\{", text)
    if not sub:
        return ""
    rest = text[sub.end():]
    start = re.search(r"\bPass\s*\{", rest)
    if not start:
        return rest
    i = sub.end() + start.end()
    depth = 1
    out = []
    while i < len(text) and depth > 0:
        ch = text[i]
        if ch == "{":
            depth += 1
        elif ch == "}":
            depth -= 1
            if depth == 0:
                break
        out.append(ch)
        i += 1
    return "".join(out)


def first_pass_state(text):
    """Collect render-state tokens of the first Pass of the first SubShader."""
    body = first_pass_block(text)
    if not body:
        return {}
    state = {}
    for key in STATE_KEYS:
        m = re.search(r"^\s*%s\s+([^\n/]+)$" % key, body, re.M | re.I)
        if m:
            state[key] = re.sub(r"\s+", " ", m.group(1).strip()).rstrip()
    lm = re.search(r'"?(?:LIGHTMODE|LightMode)"?\s*=\s*"([^"]+)"', body)
    if lm:
        state["LightMode"] = lm.group(1).lower()
    return state


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--quiet", action="store_true")
    args = parser.parse_args(argv)

    if not EXTRACT_DIR.exists():
        print("no ground truth: run tools/extract_apk_shaders.py first")
        return 1
    index = {item["shader_name"]: item for item in
             json.loads((EXTRACT_DIR / "index.json").read_text(encoding="utf-8"))}

    failures = []
    checked = 0
    placeholders = 0
    for path in sorted(SHADER_DIR.glob("*.shader")):
        text = path.read_text(encoding="utf-8", errors="replace")
        if any(marker in text for marker in DUMMY_MARKERS):
            placeholders += 1
            failures.append("%s: still an AssetRipper placeholder" % path.name)
            continue
        declared = re.search(r'^Shader\s+"([^"]+)"', text, re.M)
        if not declared:
            failures.append("%s: no shader declaration" % path.name)
            continue
        name = declared.group(1)
        if name not in index:
            if not args.quiet:
                print("skip  %-46s (not in the APK extract)" % path.name)
            continue
        ground = (EXTRACT_DIR / index[name]["file"]).read_text(encoding="utf-8")
        checked += 1

        want_props = properties(ground)
        have_props = properties(text)
        if want_props != have_props:
            only_want = [p for p in want_props if p not in have_props]
            only_have = [p for p in have_props if p not in want_props]
            failures.append("%s: properties differ\n      APK : %s\n      file: %s" % (
                path.name, only_want or "-", only_have or "-"))

        if name not in SURFACE_GENERATED:
            want_state = first_pass_state(ground)
            have_state = first_pass_state(text)
            exceptions = STATE_EXCEPTIONS.get(name, set())
            for key, value in want_state.items():
                if key in exceptions:
                    continue
                got = have_state.get(key)
                if got is None:
                    failures.append("%s: pass state %s %r missing" % (path.name, key, value))
                elif got.lower() != value.lower():
                    failures.append("%s: pass state %s is %r, APK has %r" % (path.name, key, got, value))

        if not args.quiet:
            note = " (surface: %s)" % SURFACE_GENERATED[name] if name in SURFACE_GENERATED else ""
            print("ok    %-46s %-44s %d properties%s" % (path.name, name, len(have_props), note))

    print("\nshaders checked against the APK: %d | placeholders left: %d" % (checked, placeholders))
    if failures:
        print("FAILURES (%d):" % len(failures))
        for f in failures:
            print("  - " + f)
        return 1
    print("OK")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
