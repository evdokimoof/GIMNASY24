using Gimnasy.Core.Object;

namespace Gimnasy.Core.Scene;

/// <summary>
/// Owns the active node tree and drives the per-frame update. The runtime calls
/// <see cref="Process"/> and <see cref="PhysicsProcess"/>; everything else
/// (groups, deferred frees, pausing) is bookkeeping mirrored from Godot.
/// </summary>
public sealed class SceneTree
{
    private readonly Dictionary<string, HashSet<Node>> _groups = new();
    private readonly List<Node> _toFree = new();

    public Node Root { get; }
    public bool Paused { get; set; }
    public double TimeScale { get; set; } = 1.0;
    public ulong Frame { get; private set; }

    public SceneTree()
    {
        Root = new Node { Name = "root" };
        Root.PropagateEnterTree(this);
    }

    /// <summary>Replace the current scene with the given root node.</summary>
    public void ChangeScene(Node newSceneRoot)
    {
        foreach (var child in Root.Children.ToArray())
            Root.RemoveChild(child);
        Root.AddChild(newSceneRoot);
    }

    public void Process(double delta)
    {
        if (!Paused) Root.PropagateProcess(delta * TimeScale);
        else PropagateProcessAlways(Root, delta * TimeScale);
        FlushFrees();
        Frame++;
    }

    public void PhysicsProcess(double delta)
    {
        if (!Paused) Root.PropagatePhysics(delta * TimeScale);
        FlushFrees();
    }

    public void DispatchInput(InputEventInfo e) => Root.PropagateInput(e);

    private static void PropagateProcessAlways(Node node, double delta)
    {
        if (node.ProcessMode is ProcessMode.Always or ProcessMode.WhenPaused)
            node._Process(delta);
        foreach (var c in node.Children.ToArray()) PropagateProcessAlways(c, delta);
    }

    // ---- Groups -------------------------------------------------------------

    internal void RegisterInGroup(string group, Node node)
    {
        if (!_groups.TryGetValue(group, out var set))
            _groups[group] = set = new HashSet<Node>();
        set.Add(node);
    }

    internal void UnregisterFromGroup(string group, Node node)
    {
        if (_groups.TryGetValue(group, out var set)) set.Remove(node);
    }

    public IReadOnlyCollection<Node> GetNodesInGroup(string group) =>
        _groups.TryGetValue(group, out var set) ? set : Array.Empty<Node>();

    public void CallGroup(string group, Action<Node> action)
    {
        foreach (var n in GetNodesInGroup(group).ToArray()) action(n);
    }

    // ---- Deferred free ------------------------------------------------------

    internal void QueueFree(Node node) { if (!_toFree.Contains(node)) _toFree.Add(node); }

    private void FlushFrees()
    {
        if (_toFree.Count == 0) return;
        foreach (var n in _toFree) n.Parent?.RemoveChild(n);
        _toFree.Clear();
    }
}
