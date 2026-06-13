using Gimnasy.Core.Math;
using Gimnasy.Core.Object;
using Gimnasy.Core.Resources;

namespace Gimnasy.Nodes;

// ===========================================================================
//  2D physics nodes.
// ===========================================================================

public abstract class CollisionObject2D : Node2D
{
    [Export] public int CollisionLayer { get; set; } = 1;
    [Export] public int CollisionMask { get; set; } = 1;
    [Export] public bool Disabled { get; set; }
}

[RegisteredType("Area2D", "2D/Physics")]
[Signal("body_entered")]
[Signal("body_exited")]
[Signal("area_entered")]
public sealed class Area2D : CollisionObject2D
{
    [Export] public bool Monitoring { get; set; } = true;
    [Export] public bool Monitorable { get; set; } = true;
    [Export("range:-100,100")] public float GravityOverride { get; set; }
    [Export] public int Priority { get; set; }
}

[RegisteredType("StaticBody2D", "2D/Physics")]
public sealed class StaticBody2D : CollisionObject2D
{
    [Export] public PhysicsMaterial? PhysicsMaterial { get; set; }
    [Export] public Vector2 ConstantLinearVelocity { get; set; }
    [Export] public float ConstantAngularVelocity { get; set; }
}

[RegisteredType("RigidBody2D", "2D/Physics")]
[Signal("body_entered")]
[Signal("sleeping_state_changed")]
public sealed class RigidBody2D : CollisionObject2D
{
    [Export("range:0.01,1000")] public float Mass { get; set; } = 1f;
    [Export("range:0,100")] public float GravityScale { get; set; } = 1f;
    [Export("range:0,1")] public float LinearDamp { get; set; }
    [Export("range:0,1")] public float AngularDamp { get; set; }
    [Export] public Vector2 LinearVelocity { get; set; }
    [Export] public float AngularVelocity { get; set; }
    [Export] public bool Freeze { get; set; }
    [Export] public bool ContactMonitor { get; set; }
}

[RegisteredType("CharacterBody2D", "2D/Physics")]
public sealed class CharacterBody2D : CollisionObject2D
{
    [Export] public Vector2 Velocity { get; set; }
    [Export("range:0,90")] public float FloorMaxAngleDegrees { get; set; } = 45f;
    [Export] public Vector2 UpDirection { get; set; } = Vector2.Up;
    [Export] public float FloorSnapLength { get; set; } = 1f;

    public bool IsOnFloor { get; private set; }
    public bool IsOnWall { get; private set; }
    public bool IsOnCeiling { get; private set; }

    /// <summary>
    /// Integrate <see cref="Velocity"/> over a frame and resolve against the
    /// supplied solid colliders, sliding along surfaces. Returns the resulting
    /// position. This is the kinematic-body core used by platformer characters.
    /// </summary>
    public Vector2 MoveAndSlide(double delta, IReadOnlyList<Rect2> solids, Vector2 selfHalfExtents)
    {
        IsOnFloor = IsOnWall = IsOnCeiling = false;
        Vector2 motion = Velocity * (float)delta;
        Vector2 pos = Position;
        float maxCos = Mathf.Cos(Mathf.DegToRad(FloorMaxAngleDegrees));

        // Resolve X then Y (axis-separated sweep) for stable platformer feel.
        pos = ResolveAxis(pos, new Vector2(motion.X, 0), solids, selfHalfExtents, isVertical: false, maxCos);
        pos = ResolveAxis(pos, new Vector2(0, motion.Y), solids, selfHalfExtents, isVertical: true, maxCos);
        Position = pos;
        return pos;
    }

    private Vector2 ResolveAxis(Vector2 pos, Vector2 motion, IReadOnlyList<Rect2> solids,
        Vector2 half, bool isVertical, float maxCos)
    {
        Vector2 next = pos + motion;
        var self = new Rect2(next - half, half * 2f);
        foreach (var s in solids)
        {
            if (!self.Intersects(s)) continue;
            if (isVertical)
            {
                if (motion.Y > 0) { next = new Vector2(next.X, s.Position.Y - half.Y); IsOnFloor = true; Velocity = new Vector2(Velocity.X, 0); }
                else if (motion.Y < 0) { next = new Vector2(next.X, s.End.Y + half.Y); IsOnCeiling = true; Velocity = new Vector2(Velocity.X, 0); }
            }
            else
            {
                if (motion.X > 0) next = new Vector2(s.Position.X - half.X, next.Y);
                else if (motion.X < 0) next = new Vector2(s.End.X + half.X, next.Y);
                IsOnWall = true;
                Velocity = new Vector2(0, Velocity.Y);
            }
            self = new Rect2(next - half, half * 2f);
        }
        return next;
    }
}

[RegisteredType("AnimatableBody2D", "2D/Physics")]
public sealed class AnimatableBody2D : CollisionObject2D
{
    [Export] public bool SyncToPhysics { get; set; } = true;
}

[RegisteredType("CollisionShape2D", "2D/Physics")]
public sealed class CollisionShape2D : Node2D
{
    [Export] public Shape2D? Shape { get; set; }
    [Export] public bool Disabled { get; set; }
    [Export] public bool OneWayCollision { get; set; }
    [Export] public Color DebugColor { get; set; } = new Color(0, 0.6f, 0.7f, 0.4f);
}

[RegisteredType("CollisionPolygon2D", "2D/Physics")]
public sealed class CollisionPolygon2D : Node2D
{
    [Export] public bool Disabled { get; set; }
    [Export] public bool OneWayCollision { get; set; }
    [Export] public float OneWayCollisionMargin { get; set; } = 1f;
}
