#!/usr/bin/env python3
"""Repair AssetRipper/decompiler artifacts in the recovered 4.1.0 scripts.

Artifact #1 — `RaycastHit.collider.GetComponent<Collider>()`
------------------------------------------------------------
Unity 4 had `Component.collider`. The decompiler rewrites every `.collider`
access as `.GetComponent<Collider>()`, which is correct for a Component but
**wrong** for `RaycastHit`/`Collision`, where `.collider` is already a
`Collider`. The result:

* `hit.collider.GetComponent<Collider>()` throws a `NullReferenceException`
  whenever the cast misses (`hit.collider == null`) instead of yielding null —
  e.g. `vp_FPController.FixedMove()` spams NREs every FixedUpdate and ground
  detection dies, so the player has no physics and flies away on jump;
* when the hit object carries several colliders, `GetComponent<Collider>()`
  returns the *first* one, not the one that was hit, so tag checks
  (`PlayerSkin`, `DamageObject`, `IgnoreDecal`, …) and NGUI's UI raycasts hit
  the wrong collider.

The fix restores the original expression: `hit.collider`.

Only receivers whose declared type is `RaycastHit`, `RaycastHit[]`,
`ControllerColliderHit` or `Collision` are touched; genuine
`component.GetComponent<Collider>()` conversions are left alone.

    python3 tools/fix_decompiler_artifacts.py --report
    python3 tools/fix_decompiler_artifacts.py --apply
"""

from __future__ import annotations

import argparse
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
SCRIPTS = ROOT / "client" / "Assets" / "Scripts"

HIT_TYPES = ("RaycastHit", "ControllerColliderHit", "Collision")
CALL_RE = re.compile(r"([A-Za-z_][A-Za-z0-9_]*(?:\s*\[[^\]]*\])?(?:\.[A-Za-z_][A-Za-z0-9_]*)*)\.collider\.GetComponent<Collider>\(\)")


def declared_types(text):
    """Map local/field/parameter name -> declared type for the hit types."""
    types = {}
    for hit_type in HIT_TYPES:
        pattern = re.compile(
            r"\b%s\b(?:\s*\[\s*\])?\s+([A-Za-z_][A-Za-z0-9_]*)\s*(?:[;=,)]|in\b)" % hit_type)
        for m in pattern.finditer(text):
            types[m.group(1)] = hit_type
        # `out RaycastHit hit` and `foreach (RaycastHit hit in ...)`
        for m in re.finditer(r"\bout\s+%s\s+([A-Za-z_][A-Za-z0-9_]*)" % hit_type, text):
            types[m.group(1)] = hit_type
    return types


def base_name(expression):
    """`array[j]` -> `array`, `m_Hit` -> `m_Hit`, `a.b` -> last component."""
    expression = expression.strip()
    expression = re.sub(r"\s*\[[^\]]*\]$", "", expression)
    return expression.split(".")[-1]


def process(path, apply):
    text = path.read_text(encoding="utf-8")
    types = declared_types(text)
    hits = []

    def repl(m):
        receiver = m.group(1)
        name = base_name(receiver)
        kind = types.get(name)
        line = text[: m.start()].count("\n") + 1
        if kind is None:
            hits.append((line, receiver, None))
            return m.group(0)
        hits.append((line, receiver, kind))
        return receiver + ".collider"

    new_text = CALL_RE.sub(repl, text)
    if apply and new_text != text:
        path.write_text(new_text, encoding="utf-8")
    return hits, new_text != text


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--apply", action="store_true")
    parser.add_argument("--report", action="store_true")
    args = parser.parse_args(argv)

    total, fixed, unknown = 0, 0, 0
    for path in sorted(SCRIPTS.rglob("*.cs")):
        hits, changed = process(path, args.apply)
        if not hits:
            continue
        for line, receiver, kind in hits:
            total += 1
            if kind is None:
                unknown += 1
            else:
                fixed += 1
            if args.report or kind is None:
                print("%-52s :%-5d %-22s %s" % (
                    path.relative_to(ROOT), line, receiver + ".collider",
                    kind or "LEFT ALONE (type unknown)"))

    print("\n%d `.collider.GetComponent<Collider>()` sites: %d on hit structs%s, %d left alone"
          % (total, fixed, " (rewritten)" if args.apply else " (would rewrite)", unknown))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
