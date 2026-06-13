"""Validation and a reference generator for the ``.particles`` document."""
from __future__ import annotations

from . import jsonlike

MODULES = [
    "main", "emission", "shape", "velocity_over_lifetime", "limit_velocity",
    "force_over_lifetime", "color_over_lifetime", "size_over_lifetime",
    "rotation_over_lifetime", "noise", "collision", "sub_emitters",
    "texture_sheet", "trails", "lights", "renderer",
]
CURVE_MODES = {"Constant", "Curve", "RandomBetweenConstants", "RandomBetweenCurves"}


class ParticleError(Exception):
    pass


def load(path: str) -> dict:
    return jsonlike.load(path)


def _validate_curve(c: dict, path: str, errors: list[str]) -> None:
    if not isinstance(c, dict):
        errors.append(f"{path}: curve must be an object")
        return
    if c.get("mode") not in CURVE_MODES:
        errors.append(f"{path}.mode invalid: {c.get('mode')!r}")
    for key in ("curve_min", "curve_max"):
        if key in c and not isinstance(c[key], list):
            errors.append(f"{path}.{key} must be a keyframe array")


def validate(doc: dict) -> list[str]:
    errors: list[str] = []
    if doc.get("type") != "ParticleSystem":
        errors.append(f"not a ParticleSystem (type={doc.get('type')!r})")
    if "main" not in doc:
        errors.append("missing 'main' module")

    main = doc.get("main", {})
    if main.get("max_particles", 0) <= 0:
        errors.append("main.max_particles must be > 0")
    for ck in ("start_lifetime", "start_speed", "start_size"):
        if ck in main:
            _validate_curve(main[ck], f"main.{ck}", errors)

    emission = doc.get("emission", {})
    if emission:
        _validate_curve(emission.get("rate_over_time", {}), "emission.rate_over_time", errors)
        for i, b in enumerate(emission.get("bursts", [])):
            if "count" in b:
                _validate_curve(b["count"], f"emission.bursts[{i}].count", errors)

    for m in MODULES:
        if m in doc and m not in ("main", "renderer") and "enabled" not in doc[m]:
            errors.append(f"module '{m}' missing 'enabled' flag")
    return errors


def module_summary(doc: dict) -> dict:
    active = [m for m in MODULES
             if m not in ("main", "renderer") and doc.get(m, {}).get("enabled")]
    return {
        "name": doc.get("name"),
        "max_particles": doc.get("main", {}).get("max_particles"),
        "active_modules": active,
        "active_count": len(active),
    }


def fire_preset(name: str = "Fire") -> dict:
    """Mirror of C# ParticleSystemDef.FirePreset() in document form."""
    def curve_range(a: float, b: float) -> dict:
        return {"mode": "RandomBetweenConstants", "constant_min": a, "constant_max": b,
                "multiplier": 1.0, "curve_min": [], "curve_max": []}

    def curve_const(v: float) -> dict:
        return {"mode": "Constant", "constant_min": 0.0, "constant_max": v,
                "multiplier": 1.0, "curve_min": [], "curve_max": []}

    def gradient(c0, c1):
        return {"colors": [{"t": 0, "color": c0}, {"t": 1, "color": c1}],
                "alphas": [{"t": 0, "a": c0[3]}, {"t": 1, "a": c1[3]}]}

    return {
        "format": 2, "type": "ParticleSystem", "name": name,
        "main": {
            "duration": 5.0, "looping": True, "prewarm": False, "start_delay": 0.0,
            "start_lifetime": curve_range(0.8, 1.4), "start_speed": curve_range(1.5, 3.0),
            "start_size": curve_range(0.4, 0.9), "start_rotation": curve_const(0.0),
            "start_color": gradient([1.0, 0.85, 0.3, 1.0], [1.0, 0.35, 0.05, 1.0]),
            "gravity_modifier": curve_const(0.0), "simulation_space": "Local",
            "simulation_speed": 1.0, "max_particles": 600,
        },
        "emission": {"enabled": True, "rate_over_time": curve_const(120.0),
                     "rate_over_distance": curve_const(0.0), "bursts": [
                         {"time": 0.0, "count": curve_const(40), "cycles": 1, "interval": 0.01, "probability": 1.0}]},
        "shape": {"enabled": True, "shape": "Cone", "radius": 0.4, "radius_thickness": 1.0,
                  "angle_degrees": 12.0, "arc_degrees": 360.0, "position": [0, 0, 0],
                  "rotation": [0, 0, 0], "scale": [1, 1, 1], "randomize_direction": 0.1,
                  "align_to_direction": False},
        "velocity_over_lifetime": {"enabled": False},
        "limit_velocity": {"enabled": False},
        "force_over_lifetime": {"enabled": False},
        "color_over_lifetime": {"enabled": True,
                                "gradient": gradient([1, 1, 1, 1], [1, 0.2, 0.0, 0.0])},
        "size_over_lifetime": {"enabled": True, "size": {
            "mode": "Curve", "constant_min": 0, "constant_max": 1, "multiplier": 1,
            "curve_min": [], "curve_max": [{"t": 0, "v": 1, "in": 0, "out": -1},
                                           {"t": 1, "v": 0, "in": -1, "out": 0}]}},
        "rotation_over_lifetime": {"enabled": False},
        "noise": {"enabled": True, "strength": curve_const(1.5), "frequency": 0.5,
                  "scroll_speed": [0, 0.5, 0], "octaves": 2, "octave_multiplier": 0.5,
                  "octave_scale": 2.0, "damping": 1.0},
        "collision": {"enabled": False},
        "sub_emitters": {"enabled": True, "emitters": [
            {"trigger": "Death", "system": "res://smoke.particles", "probability": 0.5}]},
        "texture_sheet": {"enabled": False},
        "trails": {"enabled": False},
        "lights": {"enabled": True, "ratio": 0.1, "color": [1, 0.6, 0.2, 1],
                   "range": 5.0, "intensity": 1.5},
        "renderer": {"mode": "Billboard", "material": None, "mesh": None,
                     "sort_mode": "ByDistance", "length_scale": 2.0, "velocity_scale": 0.0,
                     "min_particle_size": 0.0, "max_particle_size": 0.5},
        "active_module_count": 5,
    }
