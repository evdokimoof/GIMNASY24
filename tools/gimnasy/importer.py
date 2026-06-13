"""Asset importer: bring external media and 3D files into a project.

Recognises the common media and 3D formats, copies the source into the
project's ``assets`` folder and writes a ``.import`` sidecar describing how the
runtime should load it. Images additionally get a ``.tres`` Texture2D resource.
"""
from __future__ import annotations

import os
import shutil

from . import jsonlike

IMAGE = {"png", "jpg", "jpeg", "webp", "gif", "bmp", "tga"}
AUDIO = {"wav", "ogg", "mp3", "aac", "wma", "flac"}
VIDEO = {"mov", "mp4", "ogv", "webm"}
MODEL = {"obj", "fbx", "gltf", "glb", "stl", "dae"}
FONT = {"ttf", "otf"}


def classify(path: str) -> str:
    ext = os.path.splitext(path)[1].lower().lstrip(".")
    if ext in IMAGE:
        return "Texture2D"
    if ext in AUDIO:
        return "AudioStream"
    if ext in VIDEO:
        return "VideoStream"
    if ext in MODEL:
        return "ArrayMesh"
    if ext in FONT:
        return "Font"
    return "Resource"


def import_asset(src: str, project_root: str, subdir: str = "assets") -> dict:
    if not os.path.isfile(src):
        raise FileNotFoundError(src)

    kind = classify(src)
    dest_dir = os.path.join(project_root, subdir)
    os.makedirs(dest_dir, exist_ok=True)
    dest = os.path.join(dest_dir, os.path.basename(src))
    shutil.copy2(src, dest)

    res_path = f"res://{subdir}/{os.path.basename(src)}"
    info = {
        "format": 1,
        "type": kind,
        "source": res_path,
        "size_bytes": os.path.getsize(dest),
    }
    jsonlike.dump(info, dest + ".import", header=f"Gimnasy import — {kind}")

    # For images, also emit a ready-to-reference Texture2D resource.
    if kind == "Texture2D":
        tres = {
            "format": 1,
            "type": "Texture2D",
            "properties": {"SourceFile": res_path, "GenerateMipmaps": True},
        }
        jsonlike.dump(tres, os.path.splitext(dest)[0] + ".tres",
                      header="Gimnasy Resource — Texture2D")

    return info


def import_folder(src_dir: str, project_root: str) -> list[dict]:
    results = []
    for entry in sorted(os.listdir(src_dir)):
        full = os.path.join(src_dir, entry)
        if os.path.isfile(full):
            results.append(import_asset(full, project_root))
    return results
