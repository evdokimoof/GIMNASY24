using System.Text.Json;
using Gimnasy.Core.Object;
using Gimnasy.Core.Scene;

namespace Gimnasy.Core.Serialization;

/// <summary>
/// Saves and loads node trees in the <c>.scen</c> format. The format is a flat
/// list of nodes — each records its type, its parent's path and its changed
/// properties — which is compact, diff-friendly and order-independent.
/// </summary>
public static class SceneSerializer
{
    public static string Serialize(Node root)
    {
        var nodes = new List<object> { };
        CollectNode(root, root, nodes);

        var dict = new Dictionary<string, object?>
        {
            ["format"] = 1,
            ["type"] = "PackedScene",
            ["root"] = root.Name,
            ["nodes"] = nodes
        };
        return JsonLike.Write(dict, "Gimnasy Scene — edit with care");
    }

    public static void Save(Node root, string path)
    {
        System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(System.IO.Path.GetFullPath(path))!);
        System.IO.File.WriteAllText(path, Serialize(root));
    }

    private static void CollectNode(Node node, Node root, List<object> sink)
    {
        var desc = ClassDb.Get(node.GetType());
        var entry = new Dictionary<string, object?>
        {
            ["name"] = node.Name,
            ["type"] = desc?.TypeName ?? node.GetType().Name,
            ["parent"] = node == root ? null : RelativePath(root, node.Parent!),
            ["properties"] = PropertyBag.Capture(node)
        };
        if (!string.IsNullOrEmpty(node.ScriptPath)) entry["script"] = node.ScriptPath;
        sink.Add(entry);

        foreach (var child in node.Children)
            CollectNode(child, root, sink);
    }

    private static string RelativePath(Node root, Node node)
    {
        if (node == root) return ".";
        var stack = new Stack<string>();
        for (var n = node; n is not null && n != root; n = n.Parent)
            stack.Push(n.Name);
        return string.Join('/', stack);
    }

    public static Node Deserialize(string text)
    {
        using var doc = JsonLike.Parse(text);
        var root = doc.RootElement;
        var nodesEl = root.GetProperty("nodes");

        Node? sceneRoot = null;
        var byPath = new Dictionary<string, Node>();

        foreach (var nodeEl in nodesEl.EnumerateArray())
        {
            string name = nodeEl.GetProperty("name").GetString()!;
            string type = nodeEl.GetProperty("type").GetString()!;
            var node = (Node)ClassDb.Instantiate(type);
            node.Name = name;

            if (nodeEl.TryGetProperty("script", out var script))
                node.ScriptPath = script.GetString();
            if (nodeEl.TryGetProperty("properties", out var props))
                PropertyBag.Apply(node, props);

            JsonElement parentEl = nodeEl.TryGetProperty("parent", out var p) ? p : default;
            if (parentEl.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
            {
                sceneRoot = node;
                byPath["."] = node;
            }
            else
            {
                string parentPath = parentEl.GetString()!;
                if (!byPath.TryGetValue(parentPath, out var parent))
                    throw new InvalidOperationException($"Parent '{parentPath}' not found for node '{name}'. Is the scene ordered root-first?");
                parent.AddChild(node);
                node.Owner = sceneRoot;
                string thisPath = parentPath == "." ? name : parentPath + "/" + name;
                byPath[thisPath] = node;
            }
        }

        return sceneRoot ?? throw new InvalidOperationException("Scene has no root node.");
    }

    public static Node Load(string path) => Deserialize(System.IO.File.ReadAllText(path));
}
