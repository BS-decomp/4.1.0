#!/usr/bin/env python3
"""Port the Unity 4 lightmap index sentinels to the modern ones.

`Renderer.lightmapIndex` is serialised as `m_LightmapIndex`. Unity 4 used a
byte-sized range, where

    255  = this renderer has no lightmap
    254  = lightmapped, but no index assigned yet (dynamic / not baked)

Unity 5 widened the field and moved the sentinels to

    65535 = no lightmap
    65534 = lightmapped, index not assigned

A Unity 4 scene opened in 2021 therefore asks for lightmap **255** on more than
five thousand renderers: an index far outside the single baked lightmap each map
has. That is both a flood of invalid lookups and the reason objects can pick up
the wrong lighting after the lightmaps are re-bound.

This tool converts only those two sentinel values and never touches a real
index (0 in this export).

    python3 tools/fix_lightmap_indices.py --report
    python3 tools/fix_lightmap_indices.py --apply
"""

from __future__ import annotations

import argparse
import re
import sys
from collections import Counter
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))

from static_batch_lib import iter_scenes  # noqa: E402

ROOT = Path(__file__).resolve().parent.parent
PROJECT = ROOT / "client"
MAPPING = {255: 65535, 254: 65534}
INDEX_RE = re.compile(r"^(\s*m_LightmapIndex: )(\d+)$", re.M)


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--apply", action="store_true")
    parser.add_argument("--report", action="store_true")
    args = parser.parse_args(argv)

    before = Counter()
    after = Counter()
    touched = 0

    for scene in iter_scenes(PROJECT):
        text = scene.read_text(encoding="utf-8")
        changed = [0]

        def repl(m):
            value = int(m.group(2))
            before[value] += 1
            new = MAPPING.get(value, value)
            after[new] += 1
            if new != value:
                changed[0] += 1
            return m.group(1) + str(new)

        new_text = INDEX_RE.sub(repl, text)
        if changed[0]:
            touched += 1
            if args.report:
                print("%-26s %d sentinel(s)" % (scene.stem, changed[0]))
            if args.apply:
                scene.write_text(new_text, encoding="utf-8")

    print("\nbefore: %s" % dict(sorted(before.items())))
    print("after : %s%s" % (dict(sorted(after.items())), "" if args.apply else " (dry run)"))
    print("scenes touched: %d" % touched)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
