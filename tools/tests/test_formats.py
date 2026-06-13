"""Unit tests for the Gimnasy Python toolchain.

Run from the ``tools`` directory::

    python3 -m unittest discover -s tests
"""
import os
import sys
import tempfile
import unittest

sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

from gimnasy import icon_manifest, importer, jsonlike, material_format, packaging, scene_format  # noqa: E402


class JsonLikeTests(unittest.TestCase):
    def test_round_trip(self):
        value = {"a": 1, "b": [1, 2, 3], "c": {"nested": True}, "s": "hi"}
        text = jsonlike.dumps(value)
        self.assertEqual(jsonlike.loads(text), value)

    def test_comments_and_trailing_commas(self):
        text = """
        // a comment
        {
            "a": 1, /* inline */
            "b": [1, 2, 3,],
        }
        """
        self.assertEqual(jsonlike.loads(text), {"a": 1, "b": [1, 2, 3]})

    def test_comment_chars_inside_strings_preserved(self):
        text = '{"url": "http://example.com", "note": "a, b,"}'
        self.assertEqual(jsonlike.loads(text), {"url": "http://example.com", "note": "a, b,"})


class SceneTests(unittest.TestCase):
    VALID = {
        "format": 1, "type": "PackedScene", "root": "Main",
        "nodes": [
            {"name": "Main", "type": "Node2D", "parent": None, "properties": {}},
            {"name": "Player", "type": "Sprite2D", "parent": ".", "properties": {"Position": [10, 20]}},
            {"name": "Cam", "type": "Camera2D", "parent": "Player", "properties": {}},
        ],
    }

    def test_valid_scene(self):
        scene = scene_format.parse(jsonlike.dumps(self.VALID))
        self.assertEqual(scene.node_count, 3)
        self.assertEqual(scene_format.validate(scene), [])

    def test_detects_bad_parent_order(self):
        broken = dict(self.VALID)
        broken["nodes"] = [
            {"name": "Main", "type": "Node2D", "parent": None, "properties": {}},
            {"name": "Cam", "type": "Camera2D", "parent": "Player", "properties": {}},  # parent later
            {"name": "Player", "type": "Sprite2D", "parent": ".", "properties": {}},
        ]
        errors = scene_format.validate(scene_format.parse(jsonlike.dumps(broken)))
        self.assertTrue(any("Player" in e for e in errors))

    def test_detects_two_roots(self):
        broken = dict(self.VALID)
        broken["nodes"] = [
            {"name": "A", "type": "Node2D", "parent": None, "properties": {}},
            {"name": "B", "type": "Node2D", "parent": None, "properties": {}},
        ]
        errors = scene_format.validate(scene_format.parse(jsonlike.dumps(broken)))
        self.assertTrue(any("root" in e for e in errors))


class MaterialTests(unittest.TestCase):
    def test_valid_material(self):
        with tempfile.NamedTemporaryFile("w", suffix=".material", delete=False) as fh:
            fh.write(jsonlike.dumps({"format": 1, "type": "StandardMaterial3D",
                                     "properties": {"AlbedoColor": [1, 0, 0, 1]}}))
            path = fh.name
        self.addCleanup(os.unlink, path)
        self.assertEqual(material_format.validate(path), [])

    def test_unknown_type(self):
        with tempfile.NamedTemporaryFile("w", suffix=".material", delete=False) as fh:
            fh.write(jsonlike.dumps({"format": 1, "type": "Nope", "properties": {}}))
            path = fh.name
        self.addCleanup(os.unlink, path)
        self.assertTrue(material_format.validate(path))


class ImporterTests(unittest.TestCase):
    def test_classify(self):
        self.assertEqual(importer.classify("a.png"), "Texture2D")
        self.assertEqual(importer.classify("a.ogg"), "AudioStream")
        self.assertEqual(importer.classify("a.fbx"), "ArrayMesh")
        self.assertEqual(importer.classify("a.glb"), "ArrayMesh")
        self.assertEqual(importer.classify("a.ttf"), "Font")

    def test_import_image_creates_resource(self):
        with tempfile.TemporaryDirectory() as d:
            src = os.path.join(d, "hero.png")
            with open(src, "wb") as fh:
                fh.write(b"\x89PNG\r\n\x1a\n fake")
            proj = os.path.join(d, "proj")
            os.makedirs(proj)
            info = importer.import_asset(src, proj)
            self.assertEqual(info["type"], "Texture2D")
            self.assertTrue(os.path.exists(os.path.join(proj, "assets", "hero.png")))
            self.assertTrue(os.path.exists(os.path.join(proj, "assets", "hero.png.import")))
            self.assertTrue(os.path.exists(os.path.join(proj, "assets", "hero.tres")))


class PackagingTests(unittest.TestCase):
    def test_publish_commands(self):
        cmd = packaging.publish_command("win-x64")
        self.assertIn("dotnet publish", cmd)
        self.assertIn("win-x64", cmd)
        self.assertIn("PublishSingleFile=true", cmd)

    def test_package_all_generates_files(self):
        with tempfile.TemporaryDirectory() as d:
            result = packaging.package_all("MyGame", d)
            self.assertEqual(len(result["scripts"]), 3)
            xcodeproj = result["macos"]["xcodeproj"]
            self.assertTrue(os.path.exists(os.path.join(xcodeproj, "project.pbxproj")))
            self.assertTrue(os.path.exists(os.path.join(
                xcodeproj, "project.xcworkspace", "contents.xcworkspacedata")))
            self.assertTrue(os.path.exists(os.path.join(
                xcodeproj, "xcshareddata", "xcschemes", "MyGame.xcscheme")))
            self.assertTrue(os.path.exists(os.path.join(d, "MyGame.app", "Contents", "Info.plist")))

    def test_pbxproj_is_balanced(self):
        with tempfile.TemporaryDirectory() as d:
            packaging.generate_macos_bundle("Demo", d, rid="osx-x64")
            pbx = open(os.path.join(d, "Demo.xcodeproj", "project.pbxproj"), encoding="utf-8").read()
            self.assertEqual(pbx.count("{"), pbx.count("}"))
            self.assertIn("PBXAggregateTarget", pbx)        # reliable modern target
            self.assertIn("PBXShellScriptBuildPhase", pbx)
            self.assertNotIn("PBXLegacyTarget", pbx)         # avoid the Xcode inconsistency bug
            self.assertIn("command -v dotnet", pbx)          # auto-detect the SDK
            self.assertIn("osx-x64", pbx)


class IconManifestTests(unittest.TestCase):
    def test_build_against_shipped_icons(self):
        icons_dir = os.path.join(os.path.dirname(os.path.dirname(os.path.dirname(
            os.path.abspath(__file__)))), "assets", "editor", "icons")
        if not os.path.isdir(icons_dir):
            self.skipTest("icons not present")
        manifest = icon_manifest.build(icons_dir)
        self.assertGreater(manifest["icon_count"], 0)
        self.assertIn("png", manifest["by_extension"])
        # stem-matching: the png mapping must be the real png icon, not any file
        self.assertIn("png", manifest["by_extension"]["png"])


if __name__ == "__main__":
    unittest.main(verbosity=2)
