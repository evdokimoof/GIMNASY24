"""Validation and generation of the full ``.terrain`` document.

The terrain file is intentionally large — it carries the complete heightmap,
per-layer splat weights, a hole mask, baked normals and an LOD-chunk manifest,
just like exporting a 3D model. This module validates such files and can
generate one procedurally (handy for examples and tests) in the exact shape the
C# ``TerrainIO`` reads.
"""
from __future__ import annotations

import math
import random

from . import jsonlike

REQUIRED_TOP = ["format", "type", "dimensions", "heightmap"]


class TerrainError(Exception):
    pass


def load(path: str) -> dict:
    return jsonlike.load(path)


def validate(doc: dict) -> list[str]:
    errors: list[str] = []
    if doc.get("type") != "Terrain":
        errors.append(f"not a Terrain (type={doc.get('type')!r})")
    for key in REQUIRED_TOP:
        if key not in doc:
            errors.append(f"missing top-level field '{key}'")

    dims = doc.get("dimensions", {})
    res = dims.get("resolution")
    if not isinstance(res, int) or res < 2:
        errors.append("dimensions.resolution must be an int >= 2")
        return errors

    n = res * res
    hm = doc.get("heightmap", {})
    data = hm.get("data")
    if not isinstance(data, list):
        errors.append("heightmap.data must be an array")
    elif len(data) != n:
        errors.append(f"heightmap.data has {len(data)} samples, expected {n} ({res}x{res})")

    for s in doc.get("splatmaps", []):
        sd = s.get("data", [])
        if len(sd) != n:
            errors.append(f"splatmap layer {s.get('layer')} has {len(sd)} samples, expected {n}")

    normals = doc.get("normals")
    if normals is not None:
        nd = normals.get("data", [])
        if len(nd) != n * 3:
            errors.append(f"normals.data has {len(nd)} floats, expected {n * 3}")

    holes = doc.get("holes", {}).get("data", [])
    for h in holes:
        if not isinstance(h, int) or h < 0 or h >= n:
            errors.append(f"hole index {h} out of range [0,{n})")
            break
    return errors


# --- procedural generation -------------------------------------------------

def _value_noise(perm: list[int], x: float, y: float) -> float:
    xi, yi = int(math.floor(x)) & 255, int(math.floor(y)) & 255
    xf, yf = x - math.floor(x), y - math.floor(y)

    def fade(t: float) -> float:
        return t * t * t * (t * (t * 6 - 15) + 10)

    def lerp(a: float, b: float, t: float) -> float:
        return a + (b - a) * t

    aa = perm[(perm[xi] + yi) & 255] / 255.0
    ba = perm[(perm[(xi + 1) & 255] + yi) & 255] / 255.0
    ab = perm[(perm[xi] + yi + 1) & 255] / 255.0
    bb = perm[(perm[(xi + 1) & 255] + yi + 1) & 255] / 255.0
    u, v = fade(xf), fade(yf)
    return lerp(lerp(aa, ba, u), lerp(ab, bb, u), v)


def generate(name: str, resolution: int = 65, cell_size: float = 2.0,
             max_height: float = 80.0, seed: int = 1337,
             octaves: int = 6, base_frequency: float = 0.015,
             include_normals: bool = True) -> dict:
    rng = random.Random(seed)
    perm = list(range(256))
    rng.shuffle(perm)

    n = resolution * resolution
    heights = [0.0] * n

    def fbm(x: float, y: float) -> float:
        total, freq, amp, norm = 0.0, base_frequency, 1.0, 0.0
        for _ in range(octaves):
            total += _value_noise(perm, x * freq, y * freq) * amp
            norm += amp
            amp *= 0.5
            freq *= 2.0
        return total / norm if norm else 0.0

    for z in range(resolution):
        for x in range(resolution):
            heights[z * resolution + x] = round(fbm(x * cell_size, z * cell_size) * max_height, 4)

    def height(x: int, z: int) -> float:
        x = min(max(x, 0), resolution - 1)
        z = min(max(z, 0), resolution - 1)
        return heights[z * resolution + x]

    def slope_deg(x: int, z: int) -> float:
        hl, hr = height(x - 1, z), height(x + 1, z)
        hd, hu = height(x, z - 1), height(x, z + 1)
        ny = 2.0 * cell_size
        length = math.sqrt((hl - hr) ** 2 + ny ** 2 + (hd - hu) ** 2)
        return math.degrees(math.acos(max(-1.0, min(1.0, ny / length))))

    # Three auto-placed layers: grass (flat low), rock (steep), snow (high).
    layers = [
        {"name": "grass", "height": (0, max_height * 0.55), "slope": (0, 35)},
        {"name": "rock", "height": (0, max_height), "slope": (32, 90)},
        {"name": "snow", "height": (max_height * 0.6, max_height), "slope": (0, 60)},
    ]
    splat = [[0.0] * n for _ in layers]
    for z in range(resolution):
        for x in range(resolution):
            i = z * resolution + x
            h, s = height(x, z), slope_deg(x, z)
            weights = []
            for ly in layers:
                inside = ly["height"][0] <= h <= ly["height"][1] and ly["slope"][0] <= s <= ly["slope"][1]
                weights.append(1.0 if inside else 0.0)
            total = sum(weights) or 1.0
            for li, w in enumerate(weights):
                splat[li][i] = round((w / total) if sum(weights) else (1.0 if li == 0 else 0.0), 4)

    doc = {
        "format": 2, "type": "Terrain", "name": name,
        "dimensions": {"resolution": resolution, "cell_size": cell_size,
                       "max_height": max_height, "world_size": (resolution - 1) * cell_size},
        "water": {"enabled": True, "level": round(max_height * 0.18, 3),
                  "color": [0.1, 0.35, 0.5, 0.85]},
        "generation": {"seed": seed, "octaves": octaves, "lacunarity": 2.0, "gain": 0.5,
                       "base_frequency": base_frequency, "ridged": 0.0, "domain_warp": 0.0},
        "layers": [
            {"type": "TerrainLayer", "properties": {
                "LayerName": ly["name"], "HeightMin": ly["height"][0], "HeightMax": ly["height"][1],
                "SlopeMinDegrees": ly["slope"][0], "SlopeMaxDegrees": ly["slope"][1],
                "UvTiling": [12, 12], "Roughness": 0.85}}
            for ly in layers
        ],
        "heightmap": {"encoding": "f32-array", "width": resolution, "height": resolution, "data": heights},
        "splatmaps": [{"layer": li, "encoding": "f32-array", "data": splat[li]} for li in range(len(layers))],
        "holes": {"encoding": "indices", "data": []},
    }

    if include_normals:
        normals: list[float] = []
        for z in range(resolution):
            for x in range(resolution):
                hl, hr = height(x - 1, z), height(x + 1, z)
                hd, hu = height(x, z - 1), height(x, z + 1)
                nx, ny, nz = hl - hr, 2.0 * cell_size, hd - hu
                length = math.sqrt(nx * nx + ny * ny + nz * nz) or 1.0
                normals += [round(nx / length, 4), round(ny / length, 4), round(nz / length, 4)]
        doc["normals"] = {"encoding": "vec3-array", "data": normals}

    hmin, hmax = min(heights), max(heights)
    quads = (resolution - 1) * (resolution - 1)
    doc["stats"] = {
        "min_height": round(hmin, 4), "max_height": round(hmax, 4),
        "mean_height": round(sum(heights) / n, 4),
        "max_slope_degrees": round(max(slope_deg(x, z) for z in range(resolution) for x in range(resolution)), 2),
        "hole_count": 0, "vertex_count": n, "triangle_count": quads * 2,
    }
    return doc
