using Gimnasy.Core.Object;

namespace Gimnasy.Core.Scene;

/// <summary>Controls when a node receives process/physics callbacks.</summary>
public enum ProcessMode { Inherit, Pausable, WhenPaused, Always, Disabled }

/// <summary>
/// The fundamental scene-graph element. Everything in a scene is a Node or a
/// subclass. Nodes form a tree; the tree is walked every frame to deliver the
/// <c>_Process</c> / <c>_PhysicsProcess</c> callbacks, exactly like Godot.
/// </summary>
[RegisteredType("Node", "Node")]
public class Node : GObject
{
    private readonly List<Node> _children = new();
    private readonly HashSet<string> _groups = new();

    public Node? Parent { get; private set; }
    public SceneTree? Tree { get; internal set; }
    public bool IsInsideTree => Tree is not null;

    /// <summary>The node that "owns" this one for scene saving. Children whose
    /// owner is the scene root are persisted; instanced-scene internals are not.</summary>
    public Node? Owner { get; set; }

    /// <summary>Optional <c>res://…</c> path of a C# script attached to this node.
    /// Serialized verbatim; resolved by the scripting host at load time.</summary>
    public string? ScriptPath { get; set; }

    [Export] public ProcessMode ProcessMode { get; set; } = ProcessMode.Inherit;

    public IReadOnlyList<Node> Children => _children;
    public IReadOnlyCollection<string> Groups => _groups;

    // ---- Tree manipulation --------------------------------------------------

    public void AddChild(Node child, bool keepOwner = false)
    {
        if (child.Parent is not null)
            throw new InvalidOperationException($"{child.Name} already has a parent.");
        child.Parent = this;
        if (string.IsNullOrEmpty(child.Name))
            child.Name = child.GetType().Name;
        child.Name = EnsureUniqueChildName(child.Name);
        _children.Add(child);
        if (!keepOwner) child.Owner ??= Owner ?? this;
        if (IsInsideTree) child.PropagateEnterTree(Tree!);
    }

    public void RemoveChild(Node child)
    {
        if (!_children.Remove(child)) return;
        if (child.IsInsideTree) child.PropagateExitTree();
        child.Parent = null;
    }

    /// <summary>Detach from the parent and free the whole subtree.</summary>
    public void QueueFree() => Tree?.QueueFree(this);

    private string EnsureUniqueChildName(string name)
    {
        bool Taken(string n) => _children.Exists(c => c.Name == n);
        if (!Taken(name)) return name;
        for (int i = 2; ; i++)
            if (!Taken($"{name}{i}")) return $"{name}{i}";
    }

    // ---- Lookup -------------------------------------------------------------

    public Node? GetNodeOrNull(NodePath path)
    {
        Node? current = path.IsAbsolute ? Tree?.Root : this;
        foreach (var seg in path.Segments)
        {
            if (current is null) return null;
            current = seg switch
            {
                "." => current,
                ".." => current.Parent,
                _ => current._children.Find(c => c.Name == seg)
            };
        }
        return current;
    }

    public Node GetNode(NodePath path) =>
        GetNodeOrNull(path) ?? throw new KeyNotFoundException($"Node not found: {path}");

    public T GetNode<T>(NodePath path) where T : Node => (T)GetNode(path);

    public T? FindChild<T>() where T : Node
    {
        foreach (var c in _children)
        {
            if (c is T t) return t;
            var nested = c.FindChild<T>();
            if (nested is not null) return nested;
        }
        return null;
    }

    public string GetPath()
    {
        if (Parent is null) return "/" + Name;
        return Parent.GetPath() + "/" + Name;
    }

    // ---- Groups -------------------------------------------------------------

    public void AddToGroup(string group)
    {
        if (_groups.Add(group)) Tree?.RegisterInGroup(group, this);
    }

    public void RemoveFromGroup(string group)
    {
        if (_groups.Remove(group)) Tree?.UnregisterFromGroup(group, this);
    }

    public bool IsInGroup(string group) => _groups.Contains(group);

    // ---- Lifecycle (override in subclasses) ---------------------------------

    public virtual void _EnterTree() { }
    public virtual void _Ready() { }
    public virtual void _Process(double delta) { }
    public virtual void _PhysicsProcess(double delta) { }
    public virtual void _ExitTree() { }
    public virtual void _Input(InputEventInfo e) { }

    // ---- Internal traversal -------------------------------------------------

    internal void PropagateEnterTree(SceneTree tree)
    {
        Tree = tree;
        foreach (var g in _groups) tree.RegisterInGroup(g, this);
        _EnterTree();
        EmitSignal("tree_entered");
        foreach (var c in _children.ToArray()) c.PropagateEnterTree(tree);
        _Ready();
        EmitSignal("ready");
    }

    internal void PropagateExitTree()
    {
        foreach (var c in _children.ToArray()) c.PropagateExitTree();
        EmitSignal("tree_exiting");
        _ExitTree();
        if (Tree is not null)
            foreach (var g in _groups) Tree.UnregisterFromGroup(g, this);
        Tree = null;
    }

    internal void PropagateProcess(double delta)
    {
        if (ProcessMode != ProcessMode.Disabled) _Process(delta);
        foreach (var c in _children.ToArray()) c.PropagateProcess(delta);
    }

    internal void PropagatePhysics(double delta)
    {
        if (ProcessMode != ProcessMode.Disabled) _PhysicsProcess(delta);
        foreach (var c in _children.ToArray()) c.PropagatePhysics(delta);
    }

    internal void PropagateInput(InputEventInfo e)
    {
        _Input(e);
        foreach (var c in _children.ToArray()) c.PropagateInput(e);
    }
}

/// <summary>Lightweight input event passed to <c>_Input</c>.</summary>
public sealed class InputEventInfo
{
    public string Action { get; init; } = string.Empty;
    public bool Pressed { get; init; }
    public string Kind { get; init; } = "action"; // action | key | mouse | motion
    public float[] Args { get; init; } = Array.Empty<float>();
}
