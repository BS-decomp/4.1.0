#!/usr/bin/env python3
"""Extract the original shader assets from the 4.1.0 APK.

Unity 4.7 stores a *compiled* shader as one serialized string: the full
ShaderLab text (properties, tags, render state, every pass) with the GLES /
GLES3 programs inlined as `SubProgram "gles " { "!!GLES ..." }` blocks. That
string is exactly the ground truth needed to rebuild the shaders AssetRipper
could only stub out (`//DummyShaderTextExporter`).

The serialized layout around each shader is:

    <int32 len><m_Name bytes><pad to 4><int32 len><script bytes><pad to 4>

so the script length is read from the file instead of guessed, and the result
is verified to be balanced ShaderLab.

    python3 tools/extract_apk_shaders.py --out tools/shader-extract
"""

from __future__ import annotations

import argparse
import json
import re
import shutil
import struct
import sys
import tempfile
import zipfile
from collections import defaultdict
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
DATA_PREFIX = "assets/bin/Data/"
SHADER_START = re.compile(rb'Shader "([^"\n]{1,120})" \{')
INVALID = '<>:"|?*\\/'


def unpack_data(apk_path, workdir):
    """Unpack assets/bin/Data and rejoin Unity's `.splitN` files."""
    with zipfile.ZipFile(apk_path) as zf:
        for info in zf.infolist():
            if not info.filename.startswith(DATA_PREFIX) or info.is_dir():
                continue
            target = workdir / Path(info.filename).name
            with zf.open(info) as src, open(target, "wb") as dst:
                shutil.copyfileobj(src, dst)
    groups = defaultdict(list)
    for path in list(workdir.iterdir()):
        m = re.match(r"(.+)\.split(\d+)$", path.name)
        if m:
            groups[m.group(1)].append((int(m.group(2)), path))
    for base, parts in groups.items():
        parts.sort()
        with open(workdir / base, "wb") as dst:
            for _index, part in parts:
                dst.write(part.read_bytes())
            for _index, part in parts:
                part.unlink()
    return sorted(p for p in workdir.iterdir() if p.is_file())


def read_prefixed_string(data, end_offset):
    """Read the length-prefixed string whose payload ends at `end_offset`."""
    length = struct.unpack_from("<I", data, end_offset - 4)[0]
    return length


def preceding_name(data, script_start):
    """m_Name sits right before the script string; recover it when possible."""
    window = data[max(0, script_start - 160) : script_start - 4]
    matches = list(re.finditer(rb"[\x20-\x7e]{3,80}", window))
    return matches[-1].group(0).decode("ascii") if matches else ""


def balanced(text):
    depth = 0
    in_string = False
    for ch in text:
        if ch == '"':
            in_string = not in_string
        elif not in_string:
            if ch == "{":
                depth += 1
            elif ch == "}":
                depth -= 1
                if depth == 0:
                    return True
                if depth < 0:
                    return False
    return depth == 0


def sanitize(name):
    return "".join("-" if c in INVALID else c for c in name).strip()


def extract(files):
    shaders = {}
    for path in files:
        try:
            data = path.read_bytes()
        except OSError:
            continue
        for m in SHADER_START.finditer(data):
            start = m.start()
            if start < 4:
                continue
            length = read_prefixed_string(data, start)
            if not (16 <= length <= 4_000_000) or start + length > len(data):
                continue
            blob = data[start : start + length]
            try:
                text = blob.decode("utf-8")
            except UnicodeDecodeError:
                continue
            if not text.rstrip().endswith("}") or not balanced(text):
                continue
            shader_name = m.group(1).decode("utf-8")
            asset_name = preceding_name(data, start)
            previous = shaders.get(shader_name)
            if previous and len(previous["text"]) >= len(text):
                continue
            shaders[shader_name] = {
                "shader_name": shader_name,
                "asset_name": asset_name,
                "source_file": path.name,
                "offset": start,
                "length": length,
                "text": text,
            }
    return shaders


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--apk", default=None)
    parser.add_argument("--out", default=str(ROOT / "tools" / "shader-extract"))
    args = parser.parse_args(argv)

    sys.path.insert(0, str(Path(__file__).resolve().parent))
    import bs_crypto  # noqa: E402  (only for default_apk)

    apk = Path(args.apk) if args.apk else bs_crypto.default_apk(ROOT)
    out_dir = Path(args.out)
    out_dir.mkdir(parents=True, exist_ok=True)

    with tempfile.TemporaryDirectory(prefix="bs-apk-") as tmp:
        files = unpack_data(apk, Path(tmp))
        print("unpacked %d serialized files from %s" % (len(files), apk.name))
        shaders = extract(files)

    index = []
    for name, entry in sorted(shaders.items()):
        target = out_dir / (sanitize(name) + ".shaderlab")
        target.write_text(entry["text"], encoding="utf-8")
        index.append(
            {
                "shader_name": name,
                "asset_name": entry["asset_name"],
                "source_file": entry["source_file"],
                "bytes": entry["length"],
                "file": target.name,
                "subprograms": entry["text"].count('SubProgram "'),
                "passes": len(re.findall(r"^\s*Pass \{", entry["text"], re.M)),
            }
        )
    (out_dir / "index.json").write_text(json.dumps(index, indent=1, ensure_ascii=False) + "\n", encoding="utf-8")

    print("extracted %d shaders -> %s" % (len(index), out_dir))
    for item in index:
        print("  %-42s %6d bytes  %2d passes  %2d subprograms  (%s)" % (
            item["shader_name"], item["bytes"], item["passes"], item["subprograms"], item["source_file"]))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
