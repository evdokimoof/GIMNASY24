"""Validation of ``.material`` resource files."""
from __future__ import annotations

from . import jsonlike

KNOWN_MATERIALS = {
    "StandardMaterial3D",
    "ShaderMaterial",
    "CanvasItemMaterial",
    "PhysicsMaterial",
}


class MaterialError(Exception):
    pass


def load(path: str) -> dict:
    with open(path, "r", encoding="utf-8") as fh:
        return jsonlike.loads(fh.read())


def validate(path: str) -> list[str]:
    errors: list[str] = []
    try:
        data = load(path)
    except Exception as exc:  # noqa: BLE001 - surface parse errors as validation
        return [f"parse error: {exc}"]

    if "type" not in data:
        errors.append("missing 'type' field")
    elif data["type"] not in KNOWN_MATERIALS:
        errors.append(f"unknown material type '{data['type']}'")

    if "properties" in data and not isinstance(data["properties"], dict):
        errors.append("'properties' must be an object")
    return errors
