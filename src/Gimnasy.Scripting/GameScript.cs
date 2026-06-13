using Gimnasy.Core.Input;
using Gimnasy.Core.Math;
using Gimnasy.Core.Scene;
using Gimnasy.Nodes;

namespace Gimnasy.Scripting;

/// <summary>
/// Base class for gameplay scripts written in C#. Derive from this (or from a
/// specific node like <see cref="Node2D"/>/<see cref="Node3D"/>) and override
/// the lifecycle methods. It bundles convenient accessors so script code reads
/// like Godot's GDScript: <c>Input.IsActionPressed("jump")</c>,
/// <c>GetNode&lt;Sprite2D&gt;("Sprite")</c>, <c>Emit("died")</c>.
/// </summary>
public abstract class GameScript : Node2D
{
    /// <summary>Shortcut to the global input state.</summary>
    protected static class Input
    {
        public static bool IsActionPressed(string a) => InputServer.IsActionPressed(a);
        public static bool IsActionJustPressed(string a) => InputServer.IsActionJustPressed(a);
        public static bool IsActionJustReleased(string a) => InputServer.IsActionJustReleased(a);
        public static float GetAxis(string n, string p) => InputServer.GetAxis(n, p);
        public static Vector2 GetVector(string l, string r, string u, string d) => InputServer.GetVector(l, r, u, d);
        public static Vector2 MousePosition => InputServer.MousePosition;
    }

    /// <summary>Emit a named signal.</summary>
    protected void Emit(string signal, params object?[] args) => EmitSignal(signal, args);

    /// <summary>Free this node at the end of the frame.</summary>
    protected void Free() => QueueFree();
}
