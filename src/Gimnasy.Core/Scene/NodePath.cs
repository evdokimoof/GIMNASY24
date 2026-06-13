namespace Gimnasy.Core.Scene;

/// <summary>
/// A path to a node relative to another node, e.g. <c>"Player/Sprite"</c> or
/// the absolute <c>"/root/Main/Player"</c>. Mirrors Godot's NodePath.
/// </summary>
public readonly struct NodePath : IEquatable<NodePath>
{
    public string Raw { get; }
    public bool IsAbsolute => Raw.StartsWith('/');

    public NodePath(string raw) { Raw = raw ?? string.Empty; }

    public string[] Segments =>
        Raw.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);

    public static implicit operator NodePath(string s) => new(s);
    public bool Equals(NodePath o) => Raw == o.Raw;
    public override bool Equals(object? o) => o is NodePath p && Equals(p);
    public override int GetHashCode() => Raw.GetHashCode();
    public override string ToString() => Raw;
}
