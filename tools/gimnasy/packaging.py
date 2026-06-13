"""Cross-platform packaging.

Generates the build scripts and project files needed to ship a Gimnasy game on
every desktop OS:

* Windows — a self-contained single-file ``.exe`` (``dotnet publish``).
* Linux   — a self-contained executable.
* macOS   — a ``.app`` bundle plus an ``.xcodeproj`` (External Build System)
            so the game can be opened, built and signed from Xcode.
"""
from __future__ import annotations

import os
import stat

RUNTIME_PROJECT = "src/Gimnasy.Runtime/Gimnasy.Runtime.csproj"

TARGETS = {
    "windows": "win-x64",
    "linux": "linux-x64",
    "macos": "osx-arm64",
    "macos-intel": "osx-x64",
}


def publish_command(rid: str, project: str = RUNTIME_PROJECT, out: str = "build") -> str:
    return (
        f"dotnet publish {project} -c Release -r {rid} "
        f"--self-contained true "
        f"-p:PublishSingleFile=true -p:PublishTrimmed=false "
        f"-o {out}/{rid}"
    )


def _write_script(path: str, body: str) -> None:
    os.makedirs(os.path.dirname(os.path.abspath(path)), exist_ok=True)
    with open(path, "w", encoding="utf-8") as fh:
        fh.write(body)
    os.chmod(path, os.stat(path).st_mode | stat.S_IEXEC | stat.S_IRGRP | stat.S_IXGRP)


def generate_build_scripts(out_dir: str = "build") -> list[str]:
    written = []

    bat = "@echo off\r\nREM Build the Windows executable.\r\n" \
          + publish_command(TARGETS["windows"]).replace("/", "\\") + "\r\n"
    p = os.path.join(out_dir, "build_windows.bat")
    _write_script(p, bat)
    written.append(p)

    for name, key in (("build_linux.sh", "linux"), ("build_macos.sh", "macos")):
        sh = "#!/usr/bin/env bash\nset -euo pipefail\n" + publish_command(TARGETS[key]) + "\n"
        p = os.path.join(out_dir, name)
        _write_script(p, sh)
        written.append(p)
    return written


# --- macOS app bundle + Xcode project --------------------------------------

INFO_PLIST = """<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
  <key>CFBundleName</key><string>{name}</string>
  <key>CFBundleDisplayName</key><string>{name}</string>
  <key>CFBundleIdentifier</key><string>com.gimnasy.{ident}</string>
  <key>CFBundleVersion</key><string>0.1.0</string>
  <key>CFBundleShortVersionString</key><string>0.1.0</string>
  <key>CFBundlePackageType</key><string>APPL</string>
  <key>CFBundleExecutable</key><string>gimnasy</string>
  <key>LSMinimumSystemVersion</key><string>11.0</string>
  <key>NSHighResolutionCapable</key><true/>
</dict>
</plist>
"""

PBXPROJ = """// !$*UTF8*$!
{{
  archiveVersion = 1;
  objectVersion = 56;
  objects = {{
    /* External build target that delegates to dotnet publish */
    1A0000000000000000000001 /* {name} */ = {{
      isa = PBXLegacyTarget;
      buildArgumentsString = "publish ../../{project} -c Release -r osx-arm64 --self-contained true -o ./build/macos";
      buildConfigurationList = 1A0000000000000000000010;
      buildToolPath = "/usr/local/share/dotnet/dotnet";
      buildWorkingDirectory = "$(SRCROOT)";
      name = "{name}";
      passBuildSettingsInEnvironment = 1;
      productName = "{name}";
    }};
    1A0000000000000000000002 /* Project */ = {{
      isa = PBXProject;
      attributes = {{ LastUpgradeCheck = 1500; }};
      buildConfigurationList = 1A0000000000000000000011;
      compatibilityVersion = "Xcode 14.0";
      mainGroup = 1A0000000000000000000003;
      targets = ( 1A0000000000000000000001 );
    }};
    1A0000000000000000000003 = {{ isa = PBXGroup; children = (); sourceTree = "<group>"; }};
    1A0000000000000000000010 = {{ isa = XCConfigurationList; buildConfigurations = ( 1A0000000000000000000020 ); }};
    1A0000000000000000000011 = {{ isa = XCConfigurationList; buildConfigurations = ( 1A0000000000000000000021 ); }};
    1A0000000000000000000020 = {{ isa = XCBuildConfiguration; name = Release; buildSettings = {{ PRODUCT_NAME = "{name}"; }}; }};
    1A0000000000000000000021 = {{ isa = XCBuildConfiguration; name = Release; buildSettings = {{}}; }};
  }};
  rootObject = 1A0000000000000000000002;
}}
"""


def generate_macos_bundle(name: str, out_dir: str, project: str = RUNTIME_PROJECT) -> dict:
    ident = "".join(c for c in name.lower() if c.isalnum()) or "game"

    app = os.path.join(out_dir, f"{name}.app", "Contents")
    os.makedirs(os.path.join(app, "MacOS"), exist_ok=True)
    os.makedirs(os.path.join(app, "Resources"), exist_ok=True)
    with open(os.path.join(app, "Info.plist"), "w", encoding="utf-8") as fh:
        fh.write(INFO_PLIST.format(name=name, ident=ident))

    xcodeproj = os.path.join(out_dir, f"{name}.xcodeproj")
    os.makedirs(xcodeproj, exist_ok=True)
    with open(os.path.join(xcodeproj, "project.pbxproj"), "w", encoding="utf-8") as fh:
        fh.write(PBXPROJ.format(name=name, project=project))

    return {"app_bundle": f"{out_dir}/{name}.app", "xcodeproj": xcodeproj}


def package_all(name: str, out_dir: str = "build") -> dict:
    scripts = generate_build_scripts(out_dir)
    mac = generate_macos_bundle(name, out_dir)
    return {
        "scripts": scripts,
        "macos": mac,
        "commands": {key: publish_command(rid) for key, rid in TARGETS.items()},
    }
