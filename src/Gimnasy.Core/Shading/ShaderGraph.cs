using Gimnasy.Core.Math;
using Gimnasy.Core.Object;
using Gimnasy.Core.Resources;

namespace Gimnasy.Core.Shading;

/// <summary>One node in a shader graph (a constant, a math op, a texture…).</summary>
public sealed class ShaderGraphNode
{
    public required string Id { get; init; }
    public required string Type { get; init; }
    public Vector2 EditorPosition { get; set; }
    /// <summary>Default/literal values for inputs that have no incoming wire,
    /// plus node-specific settings (e.g. a texture name).</summary>
    public Dictionary<string, float[]> Params { get; init; } = new();
    public Dictionary<string, string> StringParams { get; init; } = new();
}

/// <summary>A wire from one node's output port to another node's input port.</summary>
public sealed class ShaderGraphConnection
{
    public required string FromNode { get; init; }
    public string FromPort { get; init; } = "out";
    public required string ToNode { get; init; }
    public required string ToPort { get; init; }
}

/// <summary>
/// A visual shader graph: a set of nodes and the wires between them, compiled to
/// shader source by <see cref="ShaderGraphCompiler"/>. This backs the editor's
/// node-based material editor (à la Godot's VisualShader / UE Material editor).
/// </summary>
[RegisteredType("ShaderGraph", "Material")]
public sealed class ShaderGraph : Resource
{
    public override string ResourceType => "ShaderGraph";

    [Export] public string GraphName { get; set; } = "Untitled";
    [Export] public bool Unshaded { get; set; }

    public List<ShaderGraphNode> Nodes { get; } = new();
    public List<ShaderGraphConnection> Connections { get; } = new();

    public ShaderGraphNode AddNode(string id, string type)
    {
        var node = new ShaderGraphNode { Id = id, Type = type };
        Nodes.Add(node);
        return node;
    }

    public void Connect(string fromNode, string toNode, string toPort, string fromPort = "out")
    {
        Connections.Add(new ShaderGraphConnection
        {
            FromNode = fromNode, FromPort = fromPort, ToNode = toNode, ToPort = toPort
        });
    }

    public ShaderGraphNode? Find(string id) => Nodes.Find(n => n.Id == id);

    /// <summary>The connection feeding a given input port, if any.</summary>
    public ShaderGraphConnection? IncomingTo(string nodeId, string port) =>
        Connections.Find(c => c.ToNode == nodeId && c.ToPort == port);

    /// <summary>Ensure there is exactly one Output node and return it.</summary>
    public ShaderGraphNode GetOrCreateOutput()
    {
        var existing = Nodes.Find(n => n.Type == "Output");
        if (existing is not null) return existing;
        return AddNode("output", "Output");
    }
}
