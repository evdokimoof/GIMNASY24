"""Tests for the .shadergraph validator / reference GLSL compiler."""
import os
import sys
import unittest

sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

from gimnasy import shadergraph_format as sg  # noqa: E402


def graph(nodes, connections):
    return {"format": 1, "type": "ShaderGraph", "name": "T", "unshaded": False,
            "nodes": nodes, "connections": connections}


class ShaderGraphTests(unittest.TestCase):
    def test_minimal_valid_graph_compiles(self):
        g = graph(
            [{"id": "output", "type": "Output", "pos": [0, 0]},
             {"id": "c", "type": "Color", "pos": [0, 0], "params": {"value": [1, 0, 0, 1]}}],
            [{"from": "c", "to": "output", "to_port": "albedo"}],
        )
        self.assertEqual(sg.validate(g), [])
        glsl, errors = sg.compile_glsl(g)
        self.assertEqual(errors, [])
        self.assertIn("FragColor", glsl)
        self.assertIn("vec4(1, 0, 0, 1)", glsl)

    def test_unknown_node_type(self):
        g = graph(
            [{"id": "output", "type": "Output", "pos": [0, 0]},
             {"id": "x", "type": "Bogus", "pos": [0, 0]}],
            [],
        )
        self.assertTrue(any("unknown node type" in e for e in sg.validate(g)))

    def test_requires_single_output(self):
        g = graph([{"id": "a", "type": "Color", "pos": [0, 0]}], [])
        self.assertTrue(any("Output" in e for e in sg.validate(g)))

    def test_cycle_detection(self):
        g = graph(
            [{"id": "output", "type": "Output", "pos": [0, 0]},
             {"id": "a", "type": "Add", "pos": [0, 0]},
             {"id": "b", "type": "Add", "pos": [0, 0]}],
            [{"from": "a", "to": "b", "to_port": "a"},
             {"from": "b", "to": "a", "to_port": "a"},
             {"from": "b", "to": "output", "to_port": "albedo"}],
        )
        self.assertTrue(any("cycle" in e for e in sg.validate(g)))

    def test_texture_sample_declares_uniform(self):
        g = graph(
            [{"id": "output", "type": "Output", "pos": [0, 0]},
             {"id": "tex", "type": "TextureSample", "pos": [0, 0]}],
            [{"from": "tex", "to": "output", "to_port": "albedo"}],
        )
        glsl, errors = sg.compile_glsl(g)
        self.assertEqual(errors, [])
        self.assertIn("uniform sampler2D tex_tex;", glsl)

    def test_example_water_graph(self):
        path = os.path.join(os.path.dirname(os.path.dirname(os.path.dirname(
            os.path.abspath(__file__)))), "examples", "platformer3d", "water.shadergraph")
        if not os.path.exists(path):
            self.skipTest("example missing")
        g = sg.load(path)
        self.assertEqual(sg.validate(g), [])
        glsl, errors = sg.compile_glsl(g)
        self.assertEqual(errors, [])
        self.assertIn("mix(", glsl)


if __name__ == "__main__":
    unittest.main(verbosity=2)
