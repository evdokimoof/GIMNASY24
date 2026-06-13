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
\tarchiveVersion = 1;
\tclasses = {{
\t}};
\tobjectVersion = 56;
\tobjects = {{

/* Begin PBXAggregateTarget section */
\t\t1A00000000000000000001 /* {name} */ = {{
\t\t\tisa = PBXAggregateTarget;
\t\t\tbuildConfigurationList = 1A00000000000000000010 /* Build configuration list for {name} */;
\t\t\tbuildPhases = (
\t\t\t\t1A00000000000000000040 /* Build with dotnet */,
\t\t\t);
\t\t\tdependencies = (
\t\t\t);
\t\t\tname = "{name}";
\t\t\tproductName = "{name}";
\t\t}};
/* End PBXAggregateTarget section */

/* Begin PBXProject section */
\t\t1A00000000000000000002 /* Project object */ = {{
\t\t\tisa = PBXProject;
\t\t\tattributes = {{
\t\t\t\tLastUpgradeCheck = 1500;
\t\t\t\tORGANIZATIONNAME = "Gimnasy";
\t\t\t\tTargetAttributes = {{
\t\t\t\t\t1A00000000000000000001 = {{ CreatedOnToolsVersion = "15.0"; }};
\t\t\t\t}};
\t\t\t}};
\t\t\tbuildConfigurationList = 1A00000000000000000011 /* Build configuration list for PBXProject */;
\t\t\tcompatibilityVersion = "Xcode 14.0";
\t\t\tdevelopmentRegion = en;
\t\t\thasScannedForEncodings = 0;
\t\t\tknownRegions = ( en, Base );
\t\t\tmainGroup = 1A00000000000000000003;
\t\t\tprojectDirPath = "";
\t\t\tprojectRoot = "";
\t\t\ttargets = (
\t\t\t\t1A00000000000000000001 /* {name} */,
\t\t\t);
\t\t}};
/* End PBXProject section */

/* Begin PBXGroup section */
\t\t1A00000000000000000003 = {{
\t\t\tisa = PBXGroup;
\t\t\tchildren = (
\t\t\t);
\t\t\tsourceTree = "<group>";
\t\t}};
/* End PBXGroup section */

/* Begin PBXShellScriptBuildPhase section */
\t\t1A00000000000000000040 /* Build with dotnet */ = {{
\t\t\tisa = PBXShellScriptBuildPhase;
\t\t\talwaysOutOfDate = 1;
\t\t\tbuildActionMask = 2147483647;
\t\t\tfiles = (
\t\t\t);
\t\t\tinputFileListPaths = (
\t\t\t);
\t\t\tinputPaths = (
\t\t\t);
\t\t\tname = "Build with dotnet";
\t\t\toutputFileListPaths = (
\t\t\t);
\t\t\toutputPaths = (
\t\t\t);
\t\t\trunOnlyForDeploymentPostprocessing = 0;
\t\t\tshellPath = /bin/sh;
\t\t\tshellScript = "{shell}";
\t\t}};
/* End PBXShellScriptBuildPhase section */

/* Begin XCBuildConfiguration section */
\t\t1A00000000000000000020 /* Debug */ = {{
\t\t\tisa = XCBuildConfiguration;
\t\t\tbuildSettings = {{
\t\t\t\tCODE_SIGNING_ALLOWED = NO;
\t\t\t\tGIMNASY_PROJECT = "{project}";
\t\t\t\tGIMNASY_RID = "{rid}";
\t\t\t\tPRODUCT_NAME = "{name}";
\t\t\t}};
\t\t\tname = Debug;
\t\t}};
\t\t1A00000000000000000021 /* Release */ = {{
\t\t\tisa = XCBuildConfiguration;
\t\t\tbuildSettings = {{
\t\t\t\tCODE_SIGNING_ALLOWED = NO;
\t\t\t\tGIMNASY_PROJECT = "{project}";
\t\t\t\tGIMNASY_RID = "{rid}";
\t\t\t\tPRODUCT_NAME = "{name}";
\t\t\t}};
\t\t\tname = Release;
\t\t}};
\t\t1A00000000000000000030 /* Debug */ = {{
\t\t\tisa = XCBuildConfiguration;
\t\t\tbuildSettings = {{
\t\t\t\tPRODUCT_NAME = "{name}";
\t\t\t\tSDKROOT = macosx;
\t\t\t}};
\t\t\tname = Debug;
\t\t}};
\t\t1A00000000000000000031 /* Release */ = {{
\t\t\tisa = XCBuildConfiguration;
\t\t\tbuildSettings = {{
\t\t\t\tPRODUCT_NAME = "{name}";
\t\t\t\tSDKROOT = macosx;
\t\t\t}};
\t\t\tname = Release;
\t\t}};
/* End XCBuildConfiguration section */

/* Begin XCConfigurationList section */
\t\t1A00000000000000000010 /* Build configuration list for {name} */ = {{
\t\t\tisa = XCConfigurationList;
\t\t\tbuildConfigurations = (
\t\t\t\t1A00000000000000000020 /* Debug */,
\t\t\t\t1A00000000000000000021 /* Release */,
\t\t\t);
\t\t\tdefaultConfigurationIsVisible = 0;
\t\t\tdefaultConfigurationName = Release;
\t\t}};
\t\t1A00000000000000000011 /* Build configuration list for PBXProject */ = {{
\t\t\tisa = XCConfigurationList;
\t\t\tbuildConfigurations = (
\t\t\t\t1A00000000000000000030 /* Debug */,
\t\t\t\t1A00000000000000000031 /* Release */,
\t\t\t);
\t\t\tdefaultConfigurationIsVisible = 0;
\t\t\tdefaultConfigurationName = Release;
\t\t}};
/* End XCConfigurationList section */
\t}};
\trootObject = 1A00000000000000000002 /* Project object */;
}}
"""

# Shell run by the Aggregate target. Locates the .NET SDK across the usual macOS
# install locations, then publishes the runtime straight into the .app bundle.
BUILD_SHELL = r"""set -e
echo "Gimnasy: building $PRODUCT_NAME ($GIMNASY_RID / $CONFIGURATION)"
DOTNET="$(command -v dotnet || true)"
if [ -z "$DOTNET" ]; then
  for p in "$HOME/.dotnet/dotnet" /opt/homebrew/bin/dotnet /usr/local/bin/dotnet /usr/local/share/dotnet/dotnet; do
    if [ -x "$p" ]; then DOTNET="$p"; break; fi
  done
fi
if [ -z "$DOTNET" ]; then
  echo "error: .NET 8 SDK not found. Install it from https://dotnet.microsoft.com/download"
  exit 1
fi
echo "Gimnasy: using $DOTNET"
"$DOTNET" publish "$GIMNASY_PROJECT" -c "$CONFIGURATION" -r "$GIMNASY_RID" --self-contained true -p:PublishSingleFile=false -o "$SRCROOT/$PRODUCT_NAME.app/Contents/MacOS"
echo "Gimnasy: published into $SRCROOT/$PRODUCT_NAME.app/Contents/MacOS"
"""

WORKSPACE_DATA = """<?xml version="1.0" encoding="UTF-8"?>
<Workspace version="1.0">
   <FileRef location="self:">
   </FileRef>
</Workspace>
"""

SCHEME = """<?xml version="1.0" encoding="UTF-8"?>
<Scheme LastUpgradeVersion="1500" version="1.7">
   <BuildAction parallelizeBuildables="YES" buildImplicitDependencies="YES">
      <BuildActionEntries>
         <BuildActionEntry buildForTesting="YES" buildForRunning="YES" buildForProfiling="YES" buildForArchiving="YES" buildForAnalyzing="YES">
            <BuildableReference
               BuildableIdentifier="primary"
               BlueprintIdentifier="1A00000000000000000001"
               BuildableName="{name}"
               BlueprintName="{name}"
               ReferencedContainer="container:{name}.xcodeproj">
            </BuildableReference>
         </BuildActionEntry>
      </BuildActionEntries>
   </BuildAction>
   <LaunchAction buildConfiguration="Release" selectedDebuggerIdentifier="" selectedLauncherIdentifier="Xcode.IDEFoundation.Launcher.PosixSpawn" launchStyle="0" useCustomWorkingDirectory="NO" ignoresPersistentStateOnLaunch="NO" debugDocumentVersioning="YES" allowLocationSimulation="YES">
      <PathRunnable runnableDebuggingMode="0" FilePath="$(SRCROOT)/{name}.app">
      </PathRunnable>
   </LaunchAction>
   <ProfileAction buildConfiguration="Release" shouldUseLaunchSchemeArgsEnv="YES" savedToolIdentifier="" useCustomWorkingDirectory="NO" debugDocumentVersioning="YES">
   </ProfileAction>
   <AnalyzeAction buildConfiguration="Debug"></AnalyzeAction>
   <ArchiveAction buildConfiguration="Release" revealArchiveInOrganizer="YES"></ArchiveAction>
</Scheme>
"""


def _escape_pbx_string(s: str) -> str:
    """Escape a shell script for embedding in a quoted pbxproj string value."""
    return s.replace("\\", "\\\\").replace('"', '\\"').replace("\n", "\\n").replace("\t", "\\t")


def generate_macos_bundle(name: str, out_dir: str, project: str = RUNTIME_PROJECT,
                          rid: str = "osx-arm64") -> dict:
    ident = "".join(c for c in name.lower() if c.isalnum()) or "game"

    # .app bundle skeleton (Xcode publishes the binary into Contents/MacOS).
    app = os.path.join(out_dir, f"{name}.app", "Contents")
    os.makedirs(os.path.join(app, "MacOS"), exist_ok=True)
    os.makedirs(os.path.join(app, "Resources"), exist_ok=True)
    with open(os.path.join(app, "Info.plist"), "w", encoding="utf-8") as fh:
        fh.write(INFO_PLIST.format(name=name, ident=ident))

    # .xcodeproj with project file, workspace and a shared scheme.
    xcodeproj = os.path.join(out_dir, f"{name}.xcodeproj")
    os.makedirs(xcodeproj, exist_ok=True)
    shell = _escape_pbx_string(BUILD_SHELL)
    with open(os.path.join(xcodeproj, "project.pbxproj"), "w", encoding="utf-8") as fh:
        fh.write(PBXPROJ.format(name=name, project=project, rid=rid, shell=shell))

    workspace = os.path.join(xcodeproj, "project.xcworkspace")
    os.makedirs(workspace, exist_ok=True)
    with open(os.path.join(workspace, "contents.xcworkspacedata"), "w", encoding="utf-8") as fh:
        fh.write(WORKSPACE_DATA)

    schemes = os.path.join(xcodeproj, "xcshareddata", "xcschemes")
    os.makedirs(schemes, exist_ok=True)
    with open(os.path.join(schemes, f"{name}.xcscheme"), "w", encoding="utf-8") as fh:
        fh.write(SCHEME.format(name=name))

    return {
        "app_bundle": f"{out_dir}/{name}.app",
        "xcodeproj": xcodeproj,
        "scheme": os.path.join(schemes, f"{name}.xcscheme"),
    }


def package_all(name: str, out_dir: str = "build") -> dict:
    scripts = generate_build_scripts(out_dir)
    mac = generate_macos_bundle(name, out_dir)
    return {
        "scripts": scripts,
        "macos": mac,
        "commands": {key: publish_command(rid) for key, rid in TARGETS.items()},
    }

