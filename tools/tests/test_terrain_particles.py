"""Tests for the .terrain and .particles documents."""
import os
import sys
import unittest

sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

from gimnasy import particles_format, terrain_format  # noqa: E402

EXAMPLES = os.path.join(os.path.dirname(os.path.dirname(os.path.dirname(
    os.path.abspath(__file__)))), "examples", "platformer3d")


class TerrainTests(unittest.TestCase):
    def test_generate_is_valid_and_complete(self):
        doc = terrain_format.generate("T", resolution=33)
        self.assertEqual(terrain_format.validate(doc), [])
        n = 33 * 33
        self.assertEqual(len(doc["heightmap"]["data"]), n)
        self.assertEqual(len(doc["normals"]["data"]), n * 3)
        self.assertTrue(len(doc["splatmaps"]) >= 1)
        for s in doc["splatmaps"]:
            self.assertEqual(len(s["data"]), n)
        self.assertEqual(doc["stats"]["vertex_count"], n)

    def test_splat_weights_normalised(self):
        doc = terrain_format.generate("T", resolution=17)
        n = 17 * 17
        for i in range(n):
            total = sum(s["data"][i] for s in doc["splatmaps"])
            self.assertAlmostEqual(total, 1.0, places=2)

    def test_detects_wrong_sample_count(self):
        doc = terrain_format.generate("T", resolution=17)
        doc["heightmap"]["data"] = doc["heightmap"]["data"][:-5]
        self.assertTrue(any("heightmap.data" in e for e in terrain_format.validate(doc)))

    def test_detects_bad_hole_index(self):
        doc = terrain_format.generate("T", resolution=17)
        doc["holes"]["data"] = [999999]
        self.assertTrue(any("hole index" in e for e in terrain_format.validate(doc)))

    def test_example_island(self):
        path = os.path.join(EXAMPLES, "island.terrain")
        if not os.path.exists(path):
            self.skipTest("example missing")
        doc = terrain_format.load(path)
        self.assertEqual(terrain_format.validate(doc), [])
        self.assertGreater(os.path.getsize(path), 100_000)  # genuinely large


class ParticleTests(unittest.TestCase):
    def test_fire_preset_valid(self):
        doc = particles_format.fire_preset()
        self.assertEqual(particles_format.validate(doc), [])
        summary = particles_format.module_summary(doc)
        self.assertGreaterEqual(summary["active_count"], 4)
        self.assertEqual(summary["max_particles"], 600)

    def test_requires_main(self):
        self.assertTrue(any("main" in e for e in particles_format.validate({"type": "ParticleSystem"})))

    def test_detects_bad_curve_mode(self):
        doc = particles_format.fire_preset()
        doc["main"]["start_speed"]["mode"] = "Nonsense"
        self.assertTrue(any("mode invalid" in e for e in particles_format.validate(doc)))

    def test_module_missing_enabled(self):
        doc = particles_format.fire_preset()
        del doc["noise"]["enabled"]
        self.assertTrue(any("noise" in e for e in particles_format.validate(doc)))

    def test_example_fire(self):
        path = os.path.join(EXAMPLES, "fire.particles")
        if not os.path.exists(path):
            self.skipTest("example missing")
        doc = particles_format.load(path)
        self.assertEqual(particles_format.validate(doc), [])


if __name__ == "__main__":
    unittest.main(verbosity=2)
