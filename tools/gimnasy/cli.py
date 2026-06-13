"""Gimnasy command-line toolchain entry point.

Run with::

    python3 -m gimnasy.cli <command> ...

Commands: validate, gen-icons, import, package, new-project, info.
"""
from __future__ import annotations

import argparse
import os
import sys

from . import (
    __version__,
    icon_manifest,
    importer,
    material_format,
    packaging,
    particles_format,
    scene_format,
    shadergraph_format,
    terrain_format,
)
from . import jsonlike


def cmd_validate(args: argparse.Namespace) -> int:
    exts = (".scen", ".material", ".shadergraph", ".terrain", ".particles")
    targets: list[str] = []
    if os.path.isdir(args.path):
        for root, _dirs, files in os.walk(args.path):
            for f in files:
                if f.endswith(exts):
                    targets.append(os.path.join(root, f))
    else:
        targets.append(args.path)

    failures = 0
    for path in sorted(targets):
        label = ""
        if path.endswith(".scen"):
            try:
                scene = scene_format.load(path)
                errors = scene_format.validate(scene)
                if not errors:
                    label = f"{scene.node_count} nodes"
            except Exception as exc:  # noqa: BLE001
                errors = [f"parse error: {exc}"]
        elif path.endswith(".shadergraph"):
            try:
                graph = shadergraph_format.load(path)
                errors = shadergraph_format.validate(graph)
                if not errors:
                    label = f"{len(graph.get('nodes', []))} nodes"
            except Exception as exc:  # noqa: BLE001
                errors = [f"parse error: {exc}"]
        elif path.endswith(".terrain"):
            try:
                doc = terrain_format.load(path)
                errors = terrain_format.validate(doc)
                if not errors:
                    res = doc["dimensions"]["resolution"]
                    label = f"{res}x{res}, {doc.get('stats', {}).get('triangle_count', '?')} tris"
            except Exception as exc:  # noqa: BLE001
                errors = [f"parse error: {exc}"]
        elif path.endswith(".particles"):
            try:
                doc = particles_format.load(path)
                errors = particles_format.validate(doc)
                if not errors:
                    label = f"{particles_format.module_summary(doc)['active_count']} active modules"
            except Exception as exc:  # noqa: BLE001
                errors = [f"parse error: {exc}"]
        else:
            errors = material_format.validate(path)

        if errors:
            failures += 1
            print(f"✗ {path}")
            for e in errors:
                print(f"    - {e}")
        else:
            print(f"✓ {path} {label}".rstrip())

    print(f"\n{len(targets) - failures}/{len(targets)} files valid.")
    return 1 if failures else 0


def cmd_gen_icons(args: argparse.Namespace) -> int:
    out = icon_manifest.generate(args.icons_dir)
    manifest = icon_manifest.build(args.icons_dir)
    print(f"wrote {out}")
    print(f"  {manifest['icon_count']} icons, "
          f"{len(manifest['by_extension'])} extension mappings, "
          f"{len(manifest['by_action'])} action mappings")
    return 0


def cmd_import(args: argparse.Namespace) -> int:
    if os.path.isdir(args.src):
        results = importer.import_folder(args.src, args.project)
    else:
        results = [importer.import_asset(args.src, args.project)]
    for r in results:
        print(f"imported {r['source']}  ({r['type']}, {r['size_bytes']} bytes)")
    return 0


def cmd_package(args: argparse.Namespace) -> int:
    result = packaging.package_all(args.name, args.out)
    print("Generated build scripts:")
    for s in result["scripts"]:
        print(f"  {s}")
    print(f"macOS app bundle: {result['macos']['app_bundle']}")
    print(f"macOS Xcode project: {result['macos']['xcodeproj']}")
    print("\nPublish commands (run where the .NET 8 SDK is installed):")
    for key, cmd in result["commands"].items():
        print(f"  [{key}] {cmd}")
    return 0


def cmd_new_project(args: argparse.Namespace) -> int:
    from . import jsonlike

    os.makedirs(args.dir, exist_ok=True)
    os.makedirs(os.path.join(args.dir, "assets"), exist_ok=True)
    os.makedirs(os.path.join(args.dir, "scripts"), exist_ok=True)

    project = {
        "format": 1,
        "name": args.name,
        "main_scene": "res://main.scen",
        "window_width": 1280,
        "window_height": 720,
        "physics_tick_rate": 60,
    }
    jsonlike.dump(project, os.path.join(args.dir, "project.gimnasy"),
                  header="Gimnasy project manifest")

    scene = {
        "format": 1,
        "type": "PackedScene",
        "root": "Main",
        "nodes": [{"name": "Main", "type": "Node2D", "parent": None, "properties": {}}],
    }
    jsonlike.dump(scene, os.path.join(args.dir, "main.scen"),
                  header="Gimnasy Scene")
    print(f"created project '{args.name}' in {args.dir}/")
    return 0


def cmd_shader(args: argparse.Namespace) -> int:
    graph = shadergraph_format.load(args.graph)
    glsl, errors = shadergraph_format.compile_glsl(graph)
    if errors:
        print("shader compile errors:")
        for e in errors:
            print(f"  - {e}")
        return 1
    if args.out:
        with open(args.out, "w", encoding="utf-8") as fh:
            fh.write(glsl)
        print(f"wrote {args.out}")
    else:
        print(glsl)
    return 0


def cmd_gen_terrain(args: argparse.Namespace) -> int:
    doc = terrain_format.generate(args.name, resolution=args.res)
    jsonlike.dump(doc, args.out, header="Gimnasy Terrain — full landscape document")
    size_kb = os.path.getsize(args.out) // 1024
    print(f"wrote {args.out} ({size_kb} KB, {args.res}x{args.res}, "
          f"{doc['stats']['triangle_count']} triangles)")
    return 0


def cmd_gen_particles(args: argparse.Namespace) -> int:
    doc = particles_format.fire_preset(args.name)
    jsonlike.dump(doc, args.out, header="Gimnasy Particle System — full module document")
    print(f"wrote {args.out} ({particles_format.module_summary(doc)['active_count']} active modules)")
    return 0


def cmd_info(_args: argparse.Namespace) -> int:
    print(f"Gimnasy toolchain {__version__} (python {sys.version.split()[0]})")
    print("Supported import formats:")
    print(f"  images: {sorted(importer.IMAGE)}")
    print(f"  audio:  {sorted(importer.AUDIO)}")
    print(f"  video:  {sorted(importer.VIDEO)}")
    print(f"  models: {sorted(importer.MODEL)}")
    return 0


def build_parser() -> argparse.ArgumentParser:
    p = argparse.ArgumentParser(prog="gimnasy", description="Gimnasy Engine toolchain")
    p.add_argument("--version", action="version", version=f"%(prog)s {__version__}")
    sub = p.add_subparsers(dest="command", required=True)

    v = sub.add_parser("validate", help="validate .scen/.material files")
    v.add_argument("path")
    v.set_defaults(func=cmd_validate)

    g = sub.add_parser("gen-icons", help="generate the editor icon manifest")
    g.add_argument("icons_dir")
    g.set_defaults(func=cmd_gen_icons)

    i = sub.add_parser("import", help="import media/3D assets into a project")
    i.add_argument("src")
    i.add_argument("project")
    i.set_defaults(func=cmd_import)

    pk = sub.add_parser("package", help="generate cross-platform build artifacts")
    pk.add_argument("name")
    pk.add_argument("--out", default="build")
    pk.set_defaults(func=cmd_package)

    n = sub.add_parser("new-project", help="scaffold a new project")
    n.add_argument("dir")
    n.add_argument("--name", default="MyGame")
    n.set_defaults(func=cmd_new_project)

    sh = sub.add_parser("shader", help="compile a .shadergraph to GLSL")
    sh.add_argument("graph")
    sh.add_argument("--out", default=None)
    sh.set_defaults(func=cmd_shader)

    gt = sub.add_parser("gen-terrain", help="generate a full .terrain document")
    gt.add_argument("out")
    gt.add_argument("--res", type=int, default=65)
    gt.add_argument("--name", default="Island")
    gt.set_defaults(func=cmd_gen_terrain)

    gp = sub.add_parser("gen-particles", help="generate a .particles fire preset")
    gp.add_argument("out")
    gp.add_argument("--name", default="Fire")
    gp.set_defaults(func=cmd_gen_particles)

    inf = sub.add_parser("info", help="show toolchain info")
    inf.set_defaults(func=cmd_info)
    return p


def main(argv: list[str] | None = None) -> int:
    parser = build_parser()
    args = parser.parse_args(argv)
    return args.func(args)


if __name__ == "__main__":
    raise SystemExit(main())
