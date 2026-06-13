using System.Text.Json;
using Gimnasy.Core.Math;
using Gimnasy.Core.Serialization;

namespace Gimnasy.Core.Shading;

/// <summary>Reads/writes shader graphs in the <c>.shadergraph</c> JSON-like
/// format, alongside <c>.scen</c> and <c>.material</c>.</summary>
public static class ShaderGraphIO
{
    public static string Serialize(ShaderGraph graph)
    {
        var nodes = new List<object>();
        foreach (var n in graph.Nodes)
        {
            var entry = new Dictionary<string, object?>
            {
                ["id"] = n.Id,
                ["type"] = n.Type,
                ["pos"] = new List<object> { n.EditorPosition.X, n.EditorPosition.Y },
            };
            if (n.Params.Count > 0)
            {
                var p = new Dictionary<string, object?>();
                foreach (var kv in n.Params)
                    p[kv.Key] = kv.Value.Select(f => (object)(double)f).ToList();
                entry["params"] = p;
            }
            if (n.StringParams.Count > 0)
                entry["strings"] = n.StringParams.ToDictionary(k => k.Key, v => (object?)v.Value);
            nodes.Add(entry);
        }

        var connections = graph.Connections.Select(c => (object)new Dictionary<string, object?>
        {
            ["from"] = c.FromNode, ["from_port"] = c.FromPort,
            ["to"] = c.ToNode, ["to_port"] = c.ToPort,
        }).ToList();

        var dict = new Dictionary<string, object?>
        {
            ["format"] = 1,
            ["type"] = "ShaderGraph",
            ["name"] = graph.GraphName,
            ["unshaded"] = graph.Unshaded,
            ["nodes"] = nodes,
            ["connections"] = connections,
        };
        return JsonLike.Write(dict, "Gimnasy Shader Graph");
    }

    public static void Save(ShaderGraph graph, string path) =>
        System.IO.File.WriteAllText(path, Serialize(graph));

    public static ShaderGraph Deserialize(string text)
    {
        using var doc = JsonLike.Parse(text);
        var root = doc.RootElement;
        var graph = new ShaderGraph
        {
            GraphName = root.TryGetProperty("name", out var nm) ? nm.GetString() ?? "Untitled" : "Untitled",
            Unshaded = root.TryGetProperty("unshaded", out var us) && us.GetBoolean(),
        };

        if (root.TryGetProperty("nodes", out var nodes))
            foreach (var ne in nodes.EnumerateArray())
            {
                var node = graph.AddNode(ne.GetProperty("id").GetString()!, ne.GetProperty("type").GetString()!);
                if (ne.TryGetProperty("pos", out var pos) && pos.GetArrayLength() >= 2)
                    node.EditorPosition = new Vector2((float)pos[0].GetDouble(), (float)pos[1].GetDouble());
                if (ne.TryGetProperty("params", out var pr))
                    foreach (var p in pr.EnumerateObject())
                        node.Params[p.Name] = p.Value.EnumerateArray().Select(e => (float)e.GetDouble()).ToArray();
                if (ne.TryGetProperty("strings", out var st))
                    foreach (var s in st.EnumerateObject())
                        node.StringParams[s.Name] = s.Value.GetString() ?? "";
            }

        if (root.TryGetProperty("connections", out var conns))
            foreach (var ce in conns.EnumerateArray())
                graph.Connect(
                    ce.GetProperty("from").GetString()!,
                    ce.GetProperty("to").GetString()!,
                    ce.GetProperty("to_port").GetString()!,
                    ce.TryGetProperty("from_port", out var fp) ? fp.GetString()! : "out");

        return graph;
    }

    public static ShaderGraph Load(string path) =>
        Deserialize(System.IO.File.ReadAllText(path));
}
