"""Validation and reference compilation of ``.shadergraph`` files.

This mirrors the C# ``ShaderGraphCompiler`` closely enough to validate graphs
and emit equivalent GLSL from the Python toolchain (useful in CI where the .NET
SDK may be unavailable). Every value is a ``vec4``; the Output node reads the
components it needs.
"""
from __future__ import annotations

from . import jsonlike

# input ports per node type (Output handled specially)
NODE_INPUTS: dict[str, list[str]] = {
    "Float": [], "Vector3": [], "Color": [], "Time": [], "UV": [], "Normal": [],
    "Add": ["a", "b"], "Subtract": ["a", "b"], "Multiply": ["a", "b"], "Divide": ["a", "b"],
    "Mix": ["a", "b", "t"], "Dot": ["a", "b"], "Cross": ["a", "b"],
    "Normalize": ["a"], "Length": ["a"], "Sin": ["a"], "Cos": ["a"],
    "Power": ["a", "b"], "Clamp01": ["a"], "OneMinus": ["a"],
    "Fresnel": ["power"], "Noise": ["uv"], "TextureSample": ["uv"],
}
OUTPUT_PORTS = ["albedo", "metallic", "roughness", "emission", "alpha"]


class ShaderGraphError(Exception):
    pass


def load(path: str) -> dict:
    return jsonlike.load(path)


def _fmt(values: list[float]) -> str:
    n = len(values)
    v = [f"{x:g}" for x in values]
    if n == 1:
        return f"vec4({v[0]})"
    if n == 2:
        return f"vec4({v[0]}, {v[1]}, 0.0, 0.0)"
    if n == 3:
        return f"vec4({v[0]}, {v[1]}, {v[2]}, 0.0)"
    return f"vec4({v[0]}, {v[1]}, {v[2]}, {v[3]})"


def validate(graph: dict) -> list[str]:
    errors: list[str] = []
    if graph.get("type") != "ShaderGraph":
        errors.append(f"not a ShaderGraph (type={graph.get('type')!r})")

    nodes = {n["id"]: n for n in graph.get("nodes", [])}
    if len(nodes) != len(graph.get("nodes", [])):
        errors.append("duplicate node id")

    outputs = [n for n in nodes.values() if n["type"] == "Output"]
    if len(outputs) != 1:
        errors.append(f"expected exactly one Output node, found {len(outputs)}")

    for n in nodes.values():
        if n["type"] != "Output" and n["type"] not in NODE_INPUTS:
            errors.append(f"unknown node type '{n['type']}' (id={n['id']})")

    for c in graph.get("connections", []):
        if c["from"] not in nodes:
            errors.append(f"connection from unknown node '{c['from']}'")
        if c["to"] not in nodes:
            errors.append(f"connection to unknown node '{c['to']}'")

    # cycle detection via DFS from the output node
    if outputs:
        errors.extend(_detect_cycles(nodes, graph.get("connections", []), outputs[0]["id"]))
    return errors


def _incoming(connections: list[dict], node_id: str, port: str) -> dict | None:
    for c in connections:
        if c["to"] == node_id and c["to_port"] == port:
            return c
    return None


def _detect_cycles(nodes: dict, connections: list[dict], start: str) -> list[str]:
    errors: list[str] = []
    visiting: set[str] = set()
    done: set[str] = set()

    def visit(nid: str) -> None:
        if nid in done:
            return
        if nid in visiting:
            errors.append(f"cycle detected at node '{nid}'")
            return
        visiting.add(nid)
        node = nodes.get(nid)
        ports = OUTPUT_PORTS if node and node["type"] == "Output" else NODE_INPUTS.get(node["type"], []) if node else []
        for port in ports:
            c = _incoming(connections, nid, port)
            if c:
                visit(c["from"])
        visiting.discard(nid)
        done.add(nid)

    visit(start)
    return errors


def compile_glsl(graph: dict) -> tuple[str, list[str]]:
    errors = validate(graph)
    if errors:
        return "", errors

    nodes = {n["id"]: n for n in graph["nodes"]}
    connections = graph.get("connections", [])
    statements: list[str] = []
    emitted: dict[str, str] = {}

    def var(nid: str) -> str:
        return "n_" + "".join(c if c.isalnum() else "_" for c in nid)

    def default_for(port: str) -> str:
        return {"uv": "vec4(v_uv, 0.0, 0.0)", "t": "vec4(0.5)", "power": "vec4(5.0)"}.get(port, "vec4(0.0)")

    def resolve(nid: str, port: str) -> str:
        c = _incoming(connections, nid, port)
        if c:
            return emit(c["from"])
        node = nodes[nid]
        if "params" in node and port in node["params"]:
            return _fmt(node["params"][port])
        return default_for(port)

    def emit(nid: str) -> str:
        if nid in emitted:
            return emitted[nid]
        node = nodes[nid]
        ins = {p: resolve(nid, p) for p in NODE_INPUTS[node["type"]]}
        expr = _emit_expr(node, ins)
        v = var(nid)
        statements.append(f"    vec4 {v} = {expr};")
        emitted[nid] = v
        return v

    out = next(n for n in nodes.values() if n["type"] == "Output")
    albedo = resolve(out["id"], "albedo")
    metallic = resolve(out["id"], "metallic")
    roughness = resolve(out["id"], "roughness")
    emission = resolve(out["id"], "emission")
    alpha = resolve(out["id"], "alpha")

    samplers = sorted({"".join(c if c.isalnum() else "_" for c in n["id"])
                       for n in nodes.values() if n["type"] == "TextureSample"})

    lines = ["#version 330 core",
             "// Generated by Gimnasy shadergraph_format (python) — matches the C# compiler.",
             "in vec2 v_uv;", "in vec3 v_normal;", "in vec3 v_view;", "uniform float u_time;"]
    lines += [f"uniform sampler2D tex_{s};" for s in samplers]
    lines += ["out vec4 FragColor;", "", "void main() {"]
    lines += statements
    lines += [f"    vec3 ALBEDO = ({albedo}).xyz;",
              f"    float METALLIC = ({metallic}).x;",
              f"    float ROUGHNESS = ({roughness}).x;",
              f"    vec3 EMISSION = ({emission}).xyz;",
              f"    float ALPHA = ({alpha}).x;"]
    if graph.get("unshaded"):
        lines.append("    FragColor = vec4(ALBEDO + EMISSION, ALPHA);")
    else:
        lines += ["    vec3 L = normalize(vec3(0.4, 0.8, 0.5));",
                  "    float ndl = max(dot(normalize(v_normal), L), 0.0);",
                  "    vec3 lit = ALBEDO * (0.25 + 0.75 * ndl) * (1.0 - 0.5 * METALLIC);",
                  "    FragColor = vec4(lit + EMISSION, ALPHA);"]
    lines.append("}")
    return "\n".join(lines) + "\n", []


def _emit_expr(node: dict, ins: dict[str, str]) -> str:
    t = node["type"]
    params = node.get("params", {})
    if t == "Float":
        return _fmt(params.get("value", [0.0])[:1])
    if t == "Vector3":
        return _fmt(params.get("value", [0.0, 0.0, 0.0])[:3])
    if t == "Color":
        return _fmt(params.get("value", [1.0, 1.0, 1.0, 1.0])[:4])
    if t == "Time":
        return "vec4(u_time)"
    if t == "UV":
        return "vec4(v_uv, 0.0, 0.0)"
    if t == "Normal":
        return "vec4(normalize(v_normal), 0.0)"
    if t == "Add":
        return f"({ins['a']} + {ins['b']})"
    if t == "Subtract":
        return f"({ins['a']} - {ins['b']})"
    if t == "Multiply":
        return f"({ins['a']} * {ins['b']})"
    if t == "Divide":
        return f"({ins['a']} / {ins['b']})"
    if t == "Mix":
        return f"mix({ins['a']}, {ins['b']}, {ins['t']}.x)"
    if t == "Dot":
        return f"vec4(dot({ins['a']}.xyz, {ins['b']}.xyz))"
    if t == "Cross":
        return f"vec4(cross({ins['a']}.xyz, {ins['b']}.xyz), 0.0)"
    if t == "Normalize":
        return f"vec4(normalize({ins['a']}.xyz), 0.0)"
    if t == "Length":
        return f"vec4(length({ins['a']}.xyz))"
    if t == "Sin":
        return f"sin({ins['a']})"
    if t == "Cos":
        return f"cos({ins['a']})"
    if t == "Power":
        return f"pow(max({ins['a']}, vec4(0.0)), {ins['b']})"
    if t == "Clamp01":
        return f"clamp({ins['a']}, 0.0, 1.0)"
    if t == "OneMinus":
        return f"(vec4(1.0) - {ins['a']})"
    if t == "Fresnel":
        return f"vec4(pow(1.0 - max(dot(normalize(v_normal), normalize(v_view)), 0.0), max({ins['power']}.x, 0.0001)))"
    if t == "Noise":
        return f"vec4(fract(sin(dot({ins['uv']}.xy, vec2(12.9898, 78.233))) * 43758.5453))"
    if t == "TextureSample":
        sid = "".join(c if c.isalnum() else "_" for c in node["id"])
        return f"texture(tex_{sid}, {ins['uv']}.xy)"
    return "vec4(0.0)"
