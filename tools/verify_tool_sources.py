#!/usr/bin/env python3
"""Sanity-check the C# the recovery tools generate or patch.

Catches the two mistakes that have actually broken the editor so far:

* a string literal left open on a line (CS1010 "Newline in constant") — this
  happens when a patch script writes `\n` into C# source instead of `\\n`;
* unbalanced braces/parentheses/brackets outside of strings and comments.

It is not a compiler, but it runs anywhere and catches the damage class that
cost two round trips.

    python3 tools/verify_tool_sources.py
"""

from __future__ import annotations

import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
TARGETS = [
    ROOT / "client" / "Assets" / "BSPlaytest",
    ROOT / "client" / "Assets" / "Editor",
    ROOT / "client" / "Assets" / "Scripts" / "Recovery",
]


def strip_code(text):
    out = []
    i = 0
    n = len(text)
    while i < n:
        c = text[i]
        if c == '"':
            i += 1
            while i < n:
                if text[i] == "\\":
                    i += 2
                    continue
                if text[i] == '"':
                    i += 1
                    break
                i += 1
            out.append("S")
            continue
        if c == "'":
            i += 1
            while i < n:
                if text[i] == "\\":
                    i += 2
                    continue
                if text[i] == "'":
                    i += 1
                    break
                i += 1
            out.append("C")
            continue
        if text.startswith("//", i):
            j = text.find("\n", i)
            i = j if j >= 0 else n
            continue
        if text.startswith("/*", i):
            j = text.find("*/", i)
            i = (j + 2) if j >= 0 else n
            continue
        out.append(c)
        i += 1
    return "".join(out)


def open_string_lines(text):
    bad = []
    for number, line in enumerate(text.split("\n"), 1):
        if line.strip().startswith("//"):
            continue
        count = 0
        i = 0
        while i < len(line):
            if line[i] == "\\":
                i += 2
                continue
            if line[i] == '"':
                count += 1
            elif line.startswith("//", i) and count % 2 == 0:
                break
            i += 1
        if count % 2 == 1:
            bad.append((number, line.strip()[:100]))
    return bad


def main():
    failures = []
    checked = 0
    for folder in TARGETS:
        if not folder.exists():
            continue
        for path in sorted(folder.rglob("*.cs")):
            checked += 1
            text = path.read_text(encoding="utf-8")
            for number, line in open_string_lines(text):
                failures.append("%s:%d string literal left open -> %s"
                                % (path.relative_to(ROOT), number, line))
            stripped = strip_code(text)
            for name, (a, b) in {"braces": ("{", "}"), "parens": ("(", ")"), "brackets": ("[", "]")}.items():
                delta = stripped.count(a) - stripped.count(b)
                if delta:
                    failures.append("%s: %s unbalanced by %+d" % (path.relative_to(ROOT), name, delta))

    print("C# files checked: %d" % checked)
    if failures:
        print("FAILURES (%d):" % len(failures))
        for f in failures:
            print("  - " + f)
        return 1
    print("OK")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
