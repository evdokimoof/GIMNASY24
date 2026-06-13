using System.Globalization;

namespace Gimnasy.Core.Shading;

/// <summary>Definition of a shader-graph node type: its input ports and how it
/// emits a GLSL expression. Every value flowing through the graph is a
/// <c>vec4</c>, which keeps the compiler simple and type-safe (scalars are
/// broadcast, vectors are padded) — downstream nodes pick the components they
/// need via swizzles.</summary>
public sealed class ShaderNodeDef
{
    public required string[] Inputs { get; init; }
    /// <summary>ins: resolved vec4 expression per input port.</summary>
    public required Func<IReadOnlyDictionary<string, string>, ShaderGraphNode, string> Emit { get; init; }
}

/// <summary>The built-in palette of shader-graph nodes shown in the editor.</summary>
public static class ShaderNodeLibrary
{
    private static string F(float v) => v.ToString("0.0###", CultureInfo.InvariantCulture);

    private static float[] P(ShaderGraphNode n, string key, params float[] fallback) =>
        n.Params.TryGetValue(key, out var v) ? v : fallback;

    private static string Sanitize(string id) =>
        new string(id.Select(c => char.IsLetterOrDigit(c) ? c : '_').ToArray());

    public static readonly IReadOnlyDictionary<string, ShaderNodeDef> Defs =
        new Dictionary<string, ShaderNodeDef>
        {
            ["Float"] = new() { Inputs = Array.Empty<string>(),
                Emit = (_, n) => { var v = P(n, "value", 0); return $"vec4({F(v[0])})"; } },

            ["Vector3"] = new() { Inputs = Array.Empty<string>(),
                Emit = (_, n) => { var v = P(n, "value", 0, 0, 0); return $"vec4({F(v[0])}, {F(v[1])}, {F(v[2])}, 0.0)"; } },

            ["Color"] = new() { Inputs = Array.Empty<string>(),
                Emit = (_, n) => { var v = P(n, "value", 1, 1, 1, 1); return $"vec4({F(v[0])}, {F(v[1])}, {F(v[2])}, {F(v[3])})"; } },

            ["Time"] = new() { Inputs = Array.Empty<string>(), Emit = (_, _) => "vec4(u_time)" },
            ["UV"] = new() { Inputs = Array.Empty<string>(), Emit = (_, _) => "vec4(v_uv, 0.0, 0.0)" },
            ["Normal"] = new() { Inputs = Array.Empty<string>(), Emit = (_, _) => "vec4(normalize(v_normal), 0.0)" },

            ["Add"] = new() { Inputs = new[] { "a", "b" }, Emit = (i, _) => $"({i["a"]} + {i["b"]})" },
            ["Subtract"] = new() { Inputs = new[] { "a", "b" }, Emit = (i, _) => $"({i["a"]} - {i["b"]})" },
            ["Multiply"] = new() { Inputs = new[] { "a", "b" }, Emit = (i, _) => $"({i["a"]} * {i["b"]})" },
            ["Divide"] = new() { Inputs = new[] { "a", "b" }, Emit = (i, _) => $"({i["a"]} / {i["b"]})" },

            ["Mix"] = new() { Inputs = new[] { "a", "b", "t" },
                Emit = (i, _) => $"mix({i["a"]}, {i["b"]}, {i["t"]}.x)" },

            ["Dot"] = new() { Inputs = new[] { "a", "b" },
                Emit = (i, _) => $"vec4(dot({i["a"]}.xyz, {i["b"]}.xyz))" },
            ["Cross"] = new() { Inputs = new[] { "a", "b" },
                Emit = (i, _) => $"vec4(cross({i["a"]}.xyz, {i["b"]}.xyz), 0.0)" },
            ["Normalize"] = new() { Inputs = new[] { "a" },
                Emit = (i, _) => $"vec4(normalize({i["a"]}.xyz), 0.0)" },
            ["Length"] = new() { Inputs = new[] { "a" },
                Emit = (i, _) => $"vec4(length({i["a"]}.xyz))" },

            ["Sin"] = new() { Inputs = new[] { "a" }, Emit = (i, _) => $"sin({i["a"]})" },
            ["Cos"] = new() { Inputs = new[] { "a" }, Emit = (i, _) => $"cos({i["a"]})" },
            ["Power"] = new() { Inputs = new[] { "a", "b" }, Emit = (i, _) => $"pow(max({i["a"]}, vec4(0.0)), {i["b"]})" },
            ["Clamp01"] = new() { Inputs = new[] { "a" }, Emit = (i, _) => $"clamp({i["a"]}, 0.0, 1.0)" },
            ["OneMinus"] = new() { Inputs = new[] { "a" }, Emit = (i, _) => $"(vec4(1.0) - {i["a"]})" },

            ["Fresnel"] = new() { Inputs = new[] { "power" },
                Emit = (i, _) => $"vec4(pow(1.0 - max(dot(normalize(v_normal), normalize(v_view)), 0.0), max({i["power"]}.x, 0.0001)))" },

            ["Noise"] = new() { Inputs = new[] { "uv" },
                Emit = (i, _) => $"vec4(fract(sin(dot({i["uv"]}.xy, vec2(12.9898, 78.233))) * 43758.5453))" },

            ["TextureSample"] = new() { Inputs = new[] { "uv" },
                Emit = (i, n) => $"texture(tex_{Sanitize(n.Id)}, {i["uv"]}.xy)" },
        };

    public static bool IsKnown(string type) => type == "Output" || Defs.ContainsKey(type);

    public static IEnumerable<string> AllTypes => Defs.Keys.Append("Output");

    public static IEnumerable<string> SamplerNodeIds(ShaderGraph graph) =>
        graph.Nodes.Where(n => n.Type == "TextureSample").Select(n => Sanitize(n.Id));
}
