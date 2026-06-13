using Gimnasy.Core.Math;
using Gimnasy.Core.Object;
using Gimnasy.Core.Scene;

namespace Gimnasy.Nodes;

// ===========================================================================
//  Shared base node types. These mirror the spatial roots of a Godot project:
//  CanvasItem → Node2D / Control for 2D, and Node3D for 3D.
// ===========================================================================

/// <summary>Anything drawable in 2D (the base of Node2D and Control).</summary>
public abstract class CanvasItem : Node
{
    [Export] public bool Visible { get; set; } = true;
    [Export] public Color Modulate { get; set; } = Color.White;
    [Export] public Color SelfModulate { get; set; } = Color.White;
    [Export] public int ZIndex { get; set; }
    [Export] public bool ZAsRelative { get; set; } = true;

    public bool IsVisibleInTree()
    {
        for (Node? n = this; n is not null; n = n.Parent)
            if (n is CanvasItem c && !c.Visible) return false;
        return true;
    }
}

/// <summary>Base for all 2D spatial nodes.</summary>
[RegisteredType("Node2D", "2D")]
public class Node2D : CanvasItem
{
    [Export] public Vector2 Position { get; set; } = Vector2.Zero;
    [Export("range:-360,360")] public float RotationDegrees { get; set; }
    [Export] public Vector2 Scale { get; set; } = Vector2.One;
    [Export] public int ZLayer { get; set; }

    public float Rotation
    {
        get => Mathf.DegToRad(RotationDegrees);
        set => RotationDegrees = Mathf.RadToDeg(value);
    }

    public Transform2D Transform => Transform2D.FromTRS(Position, Rotation, Scale);

    public Transform2D GlobalTransform =>
        Parent is Node2D p ? p.GlobalTransform * Transform : Transform;

    public Vector2 GlobalPosition => GlobalTransform.Origin;

    public void Translate(Vector2 offset) => Position += offset.Rotated(Rotation);
    public void LookAt(Vector2 target) => Rotation = (target - GlobalPosition).Angle;
}

/// <summary>Base for all 3D spatial nodes.</summary>
[RegisteredType("Node3D", "3D")]
public class Node3D : Node
{
    [Export] public Vector3 Position { get; set; } = Vector3.Zero;
    /// <summary>Euler rotation in degrees (YXZ order).</summary>
    [Export] public Vector3 RotationDegrees { get; set; } = Vector3.Zero;
    [Export] public Vector3 Scale { get; set; } = Vector3.One;
    [Export] public bool Visible { get; set; } = true;

    public Vector3 Rotation
    {
        get => new(Mathf.DegToRad(RotationDegrees.X), Mathf.DegToRad(RotationDegrees.Y), Mathf.DegToRad(RotationDegrees.Z));
        set => RotationDegrees = new Vector3(Mathf.RadToDeg(value.X), Mathf.RadToDeg(value.Y), Mathf.RadToDeg(value.Z));
    }

    public Quaternion Quaternion => Quaternion.FromEuler(Rotation);
    public Transform3D Transform => Transform3D.FromTRS(Position, Quaternion, Scale);

    public Transform3D GlobalTransform =>
        Parent is Node3D p ? p.GlobalTransform * Transform : Transform;

    public Vector3 GlobalPosition => GlobalTransform.Origin;

    public void Translate(Vector3 offset) => Position += offset;
    public void RotateY(float radians) => RotationDegrees += new Vector3(0, Mathf.RadToDeg(radians), 0);

    public bool IsVisibleInTree()
    {
        for (Node? n = this; n is not null; n = n.Parent)
            if (n is Node3D s && !s.Visible) return false;
        return true;
    }
}

/// <summary>A bare grouping/utility node that is neither 2D nor 3D specific.</summary>
[RegisteredType("CanvasLayer", "2D")]
public sealed class CanvasLayer : Node
{
    [Export] public int Layer { get; set; } = 1;
    [Export] public Vector2 Offset { get; set; }
    [Export] public bool FollowViewport { get; set; }
}
