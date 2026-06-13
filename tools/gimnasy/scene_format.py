"""Validation and inspection of ``.scen`` scene files."""
from __future__ import annotations

from dataclasses import dataclass, field
from typing import Any

from . import jsonlike


@dataclass
class SceneNode:
    name: str
    type: str
    parent: str | None
    properties: dict[str, Any] = field(default_factory=dict)
    script: str | None = None


@dataclass
class Scene:
    root: str
    nodes: list[SceneNode]

    @property
    def node_count(self) -> int:
        return len(self.nodes)


class SceneError(Exception):
    pass


def parse(text: str) -> Scene:
    data = jsonlike.loads(text)
    if data.get("type") != "PackedScene":
        raise SceneError(f"not a PackedScene (type={data.get('type')!r})")

    nodes: list[SceneNode] = []
    for raw in data.get("nodes", []):
        nodes.append(
            SceneNode(
                name=raw["name"],
                type=raw["type"],
                parent=raw.get("parent"),
                properties=raw.get("properties", {}) or {},
                script=raw.get("script"),
            )
        )
    return Scene(root=data.get("root", ""), nodes=nodes)


def load(path: str) -> Scene:
    with open(path, "r", encoding="utf-8") as fh:
        return parse(fh.read())


def validate(scene: Scene) -> list[str]:
    """Return a list of human-readable problems (empty == valid)."""
    errors: list[str] = []
    seen_paths: set[str] = set()
    roots = [n for n in scene.nodes if n.parent is None]

    if len(roots) != 1:
        errors.append(f"expected exactly one root node, found {len(roots)}")

    seen_paths.add(".")
    for node in scene.nodes:
        if node.parent is None:
            continue
        if node.parent not in seen_paths:
            errors.append(
                f"node '{node.name}': parent '{node.parent}' appears after the "
                f"child or does not exist (scenes must be ordered root-first)"
            )
        path = node.name if node.parent == "." else f"{node.parent}/{node.name}"
        if path in seen_paths:
            errors.append(f"duplicate node path '{path}'")
        seen_paths.add(path)

    for node in scene.nodes:
        if not node.name:
            errors.append("a node has an empty name")
        if not node.type:
            errors.append(f"node '{node.name}' has no type")
    return errors
