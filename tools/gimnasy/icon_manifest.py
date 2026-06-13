"""Generate ``manifest.json`` for the editor icon set.

The shipped icons (from icons8) have descriptive Russian/English file names.
This builds a stable mapping from file extensions and editor actions to icon
files by matching keywords, so the editor never hard-codes a file name.
"""
from __future__ import annotations

import os
import unicodedata

from . import jsonlike


def _nfc(s: str) -> str:
    # macOS-produced archives store filenames decomposed (NFD); normalise so
    # NFC keywords match. The on-disk name is kept verbatim in the manifest.
    return unicodedata.normalize("NFC", s)

# extension -> list of keyword candidates (first matching file wins)
EXTENSION_KEYWORDS: dict[str, list[str]] = {
    "png": ["png"], "jpg": ["jpg"], "jpeg": ["jpg"], "gif": ["gif"],
    "webp": ["webp"], "eps": ["eps"], "pdf": ["pdf"], "txt": ["txt"],
    "mp3": ["mp3", "музыка"], "ogg": ["ogg"], "aac": ["aac"], "wma": ["wma"],
    "wav": ["wav", "ogg"], "mov": ["mov"], "mp4": ["mov"],
    "dll": ["dll"], "html": ["html"], "css": ["css"], "js": ["js", "react"],
    "java": ["java"], "py": ["python"], "cs": ["символьный", "кодовый", "code"],
    "json": ["конфигурация", "config"], "log": ["журнал", "log"],
    "fbx": ["fbx"], "obj": ["символьный"], "stl": ["stl"], "gltf": ["fbx"],
    "glb": ["fbx"], "zip": ["открыть-архив", "архив"],
    "scen": ["документ"], "material": ["конфигурация", "редактирование"],
}

# editor action -> keyword candidates
ACTION_KEYWORDS: dict[str, list[str]] = {
    "open": ["открыть-архив", "открыть"], "save": ["icons8-save-50", "save"],
    "save_as": ["save-as", "сохранить"], "new": ["создать-новый", "создать"],
    "settings": ["settings", "настройка"], "search": ["search"],
    "filter": ["filter"], "copy": ["скопировать", "copy"], "cut": ["cut"],
    "home": ["home"], "logout": ["logout"], "delete": ["удалить"],
    "edit": ["редактирование-файла", "редактир"], "project": ["project-setup", "настройка-проекта"],
    "encrypt": ["шифрование"], "restore": ["восстановить", "восстановление"],
    "export": ["экспорт"], "check": ["проверить"],
}


def _index(icon_dir: str) -> list[str]:
    return [f for f in os.listdir(icon_dir) if f.lower().endswith(".png")]


def _match(files: list[str], keywords: list[str]) -> str | None:
    # Compare against the filename stem so the ".png" extension shared by every
    # icon never accidentally satisfies a keyword like "png".
    for kw in keywords:
        needle = _nfc(kw.lower())
        for f in files:
            stem = _nfc(os.path.splitext(f)[0].lower())
            if needle in stem:
                return f
    return None


def build(icon_dir: str) -> dict:
    files = _index(icon_dir)
    fallback = _match(files, ["сломанное", "broken"]) or (files[0] if files else "")

    by_ext = {}
    for ext, kws in EXTENSION_KEYWORDS.items():
        hit = _match(files, kws)
        if hit:
            by_ext[ext] = hit

    by_action = {}
    for action, kws in ACTION_KEYWORDS.items():
        hit = _match(files, kws)
        if hit:
            by_action[action] = hit

    return {
        "format": 1,
        "icon_count": len(files),
        "fallback": fallback,
        "by_extension": by_ext,
        "by_action": by_action,
    }


def generate(icon_dir: str) -> str:
    manifest = build(icon_dir)
    out_path = os.path.join(icon_dir, "manifest.json")
    jsonlike.dump(manifest, out_path, header="Gimnasy editor icon manifest (generated)")
    return out_path
