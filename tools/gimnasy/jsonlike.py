"""Reader/writer for the engine's JSON-like asset syntax.

The on-disk format is valid JSON, but to keep ``.scen``/``.material`` files
hand-editable we also accept ``//`` and ``/* */`` comments and trailing commas
on read — matching the C# ``JsonLike`` reader exactly.
"""
from __future__ import annotations

import json
from typing import Any


def _strip(text: str) -> str:
    """Remove comments and trailing commas while preserving string contents."""
    out: list[str] = []
    i, n = 0, len(text)
    in_str = False
    while i < n:
        c = text[i]
        if in_str:
            out.append(c)
            if c == "\\" and i + 1 < n:       # keep escape pairs intact
                out.append(text[i + 1])
                i += 2
                continue
            if c == '"':
                in_str = False
            i += 1
            continue
        if c == '"':
            in_str = True
            out.append(c)
            i += 1
            continue
        if c == "/" and i + 1 < n and text[i + 1] == "/":
            while i < n and text[i] != "\n":
                i += 1
            continue
        if c == "/" and i + 1 < n and text[i + 1] == "*":
            i += 2
            while i + 1 < n and not (text[i] == "*" and text[i + 1] == "/"):
                i += 1
            i += 2
            continue
        out.append(c)
        i += 1
    return _drop_trailing_commas("".join(out))


def _drop_trailing_commas(text: str) -> str:
    result: list[str] = []
    i, n = 0, len(text)
    in_str = False
    while i < n:
        c = text[i]
        if in_str:
            result.append(c)
            if c == "\\" and i + 1 < n:
                result.append(text[i + 1])
                i += 2
                continue
            if c == '"':
                in_str = False
            i += 1
            continue
        if c == '"':
            in_str = True
            result.append(c)
            i += 1
            continue
        if c == ",":
            j = i + 1
            while j < n and text[j] in " \t\r\n":
                j += 1
            if j < n and text[j] in "]}":
                i += 1               # skip the trailing comma
                continue
        result.append(c)
        i += 1
    return "".join(result)


def loads(text: str) -> Any:
    return json.loads(_strip(text))


def load(path: str) -> Any:
    with open(path, "r", encoding="utf-8") as fh:
        return loads(fh.read())


def dumps(value: Any, header: str | None = None) -> str:
    body = json.dumps(value, indent=2, ensure_ascii=False)
    return (f"// {header}\n{body}\n") if header else (body + "\n")


def dump(value: Any, path: str, header: str | None = None) -> None:
    with open(path, "w", encoding="utf-8") as fh:
        fh.write(dumps(value, header))
