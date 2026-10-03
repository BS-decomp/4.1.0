#!/usr/bin/env python3
"""Restore the real scene names of the 4.1.0 export.

The shipped game stores every scene under a DES-encrypted, base64 name
(`SXZtRDEyM0ExMt6xab3TLQVn.unity`). The key material is recomputed from the
ground-truth APK by `tools/bs_crypto.py` — see that module for the exact chain
taken from `Utils`/`UIFontControl`.

What `--apply` does:

* renames `<encrypted>.unity` and its `.meta` to `<Plain>.unity`
  (GUIDs are untouched, so every reference in the project survives);
* renames the sibling folder that AssetRipper created for the scene's lightmaps;
* rewrites the paths in `ProjectSettings/EditorBuildSettings.asset`;
* rewrites the scene paths inside the static-batch manifests and report;
* writes the full mapping to `docs/scene-names-410.json`.

Runtime note: `LevelManager.LoadLevel()` encrypts the name before calling
`Application.LoadLevel()`, and the key depends on byte sizes of files inside the
APK, which do not exist in a Unity project. The reconstruction therefore loads
scenes by their plain names; see `docs/scene-names-410.md`.

    python3 tools/recover_scene_names.py            # show the mapping
    python3 tools/recover_scene_names.py --apply
    python3 tools/recover_scene_names.py --verify   # check nothing is stale
"""

from __future__ import annotations

import argparse
import json
import re
import subprocess
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))

import bs_crypto as crypto  # noqa: E402

ROOT = Path(__file__).resolve().parent.parent
PROJECT = ROOT / "client"
LEVELS = PROJECT / "Assets" / "Levels"
BUILD_SETTINGS = PROJECT / "ProjectSettings" / "EditorBuildSettings.asset"
MAPPING_FILE = ROOT / "docs" / "scene-names-410.json"
REFERENCE_FILES = [
    BUILD_SETTINGS,
    ROOT / "docs" / "static-batching-report.json",
]
REFERENCE_GLOBS = [ROOT / "tools" / "static-batch-manifests"]
INVALID = '<>:"|?*\\'


def safe_name(name):
    """Unity/Windows-safe file name; `/` was already escaped as `#` on disk."""
    cleaned = "".join("_" if c in INVALID else c for c in name).strip()
    return cleaned.replace("/", "#")


def collect(key):
    entries = []
    for scene in sorted(LEVELS.rglob("*.unity")):
        stem = scene.stem
        try:
            plain = crypto.decrypt_name(stem, key)
        except Exception as exc:  # noqa: BLE001 - reported, never guessed
            entries.append({"file": str(scene.relative_to(PROJECT)), "encrypted": stem,
                            "plain": None, "error": str(exc)})
            continue
        entries.append(
            {
                "file": str(scene.relative_to(PROJECT)).replace("\\", "/"),
                "encrypted": stem,
                "plain": plain,
                "target": safe_name(plain),
                "map_folder": scene.parent.name,
            }
        )
    return entries


def git_mv(src, dst):
    result = subprocess.run(
        ["git", "mv", "--", str(src), str(dst)], cwd=ROOT, capture_output=True, text=True
    )
    if result.returncode != 0:
        src.rename(dst)


def apply_rename(entries):
    changed = []
    for entry in entries:
        if not entry.get("plain"):
            continue
        scene = PROJECT / entry["file"]
        target = scene.with_name(entry["target"] + ".unity")
        if scene == target:
            continue
        if target.exists():
            raise RuntimeError("target already exists: %s" % target)
        git_mv(scene, target)
        meta = scene.with_name(scene.name + ".meta")
        if meta.exists():
            git_mv(meta, target.with_name(target.name + ".meta"))
        # AssetRipper puts the scene's lightmaps in a folder named like the scene.
        folder = scene.with_suffix("")
        if folder.is_dir():
            git_mv(folder, folder.with_name(entry["target"]))
        # De-batched meshes are grouped per map label, which for the three
        # root-level scenes is the (encrypted) scene stem.
        debatched = PROJECT / "Assets" / "Mesh" / "Debatched" / entry["encrypted"]
        if debatched.is_dir():
            git_mv(debatched, debatched.with_name(entry["target"]))
            meta = debatched.with_name(debatched.name + ".meta")
            if meta.exists():
                git_mv(meta, debatched.with_name(entry["target"] + ".meta"))
        changed.append((entry["encrypted"], entry["target"], entry["plain"]))
    return changed


def rewrite_references(changed):
    targets = []
    for path in REFERENCE_FILES:
        if path.exists():
            targets.append(path)
    for folder in REFERENCE_GLOBS:
        if folder.exists():
            targets.extend(sorted(folder.glob("*.json")))
    touched = []
    for path in targets:
        text = path.read_text(encoding="utf-8")
        original = text
        for encrypted, target, _plain in changed:
            text = text.replace(encrypted, target)
        if text != original:
            path.write_text(text, encoding="utf-8")
            touched.append(str(path.relative_to(ROOT)))
    for folder in REFERENCE_GLOBS:
        for encrypted, target, _plain in changed:
            manifest = folder / (encrypted + ".json")
            if manifest.exists():
                git_mv(manifest, folder / (target + ".json"))
                touched.append(str((folder / (target + ".json")).relative_to(ROOT)))
    return touched


def verify(entries, key):
    """Works before and after --apply: encrypted stems must decrypt, plain stems
    must be present in the recorded mapping and re-encrypt to the original."""
    problems = []
    recorded = {}
    if MAPPING_FILE.exists():
        data = json.loads(MAPPING_FILE.read_text(encoding="utf-8"))
        recorded = {e["plain"]: e["encrypted"] for e in data["scenes"] if e.get("plain")}
    for entry in entries:
        scene = PROJECT / entry["file"]
        stem = scene.stem
        if entry.get("plain") is not None:
            if stem != entry["target"]:
                problems.append("%s: not renamed yet (expected %s)" % (stem, entry["target"]))
            continue
        # Already renamed: the stem is a plain name.
        plain = stem.replace("#", "/")
        if plain not in recorded:
            problems.append("%s: plain name is not in docs/scene-names-410.json" % stem)
        elif crypto.encrypt_name(plain, key) != recorded[plain]:
            problems.append("%s: does not re-encrypt to the recorded name" % stem)
    if BUILD_SETTINGS.exists():
        text = BUILD_SETTINGS.read_text(encoding="utf-8")
        for path in re.findall(r"path: (.+\.unity)", text):
            if not (PROJECT / path).exists():
                problems.append("EditorBuildSettings points at a missing scene: %s" % path)
        listed = len(re.findall(r"path: .+\.unity", text))
        if listed != len(entries):
            problems.append(
                "EditorBuildSettings lists %d scenes, project has %d" % (listed, len(entries))
            )
    # Every still-encrypted name must encrypt back to its own file name.
    for entry in entries:
        if entry.get("plain") and crypto.encrypt_name(entry["plain"], key) != entry["encrypted"]:
            problems.append("%s: re-encryption mismatch" % entry["plain"])
    return problems


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--apk", default=None)
    parser.add_argument("--apply", action="store_true", help="rename the scenes and fix references")
    parser.add_argument("--verify", action="store_true", help="check the project is consistent")
    parser.add_argument("--font-size", type=int, default=0, help="UIFontControl.fontSize (0 = full)")
    args = parser.parse_args(argv)

    apk = Path(args.apk) if args.apk else crypto.default_apk(ROOT)
    password, sizes = crypto.password_from_apk(apk, args.font_size)
    key = crypto.derive_key(password)
    crypto.self_test(key)
    print("APK            : %s" % apk.name)
    print("classes.dex    : %d bytes" % sizes["classes.dex"])
    print("Assembly-CSharp: %d bytes" % sizes["Assembly-CSharp.dll"])
    print("Utils.test     : %s" % password)
    print("DES key        : %s (PBKDF2-HMAC-SHA1, salt 'IvmD123A12', 555 iterations)" % key.hex())
    print()

    entries = collect(key)
    mapping = {
        "source_apk": apk.name,
        "password_inputs": sizes,
        "password": password,
        "des_key": key.hex(),
        "scenes": [
            {k: e[k] for k in ("encrypted", "plain", "file") if k in e} for e in entries
        ],
    }

    if args.verify:
        problems = verify(entries, key)
        print("scenes: %d" % len(entries))
        if problems:
            print("PROBLEMS (%d):" % len(problems))
            for p in problems[:40]:
                print("  - " + p)
            return 1
        print("OK")
        return 0

    for entry in entries:
        print("%-40s -> %s" % (entry["encrypted"], entry.get("plain") or entry.get("error")))

    if args.apply:
        changed = apply_rename(entries)
        touched = rewrite_references(changed)
        for entry in entries:
            if entry.get("target"):
                entry["file"] = str(
                    (PROJECT / entry["file"]).with_name(entry["target"] + ".unity").relative_to(PROJECT)
                ).replace("\\", "/")
        mapping["scenes"] = [
            {k: e[k] for k in ("encrypted", "plain", "file") if k in e} for e in entries
        ]
        MAPPING_FILE.parent.mkdir(parents=True, exist_ok=True)
        MAPPING_FILE.write_text(json.dumps(mapping, indent=1, ensure_ascii=False) + "\n", encoding="utf-8")
        print()
        print("renamed %d scenes, updated %d reference files" % (len(changed), len(touched)))
        for t in touched[:10]:
            print("  * " + t)
        print("mapping written to %s" % MAPPING_FILE.relative_to(ROOT))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
