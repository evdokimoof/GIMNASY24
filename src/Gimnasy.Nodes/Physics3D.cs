using Gimnasy.Core.Math;
using Gimnasy.Core.Object;
using Gimnasy.Core.Resources;

namespace Gimnasy.Nodes;

// ===========================================================================
//  3D physics nodes.
// ===========================================================================

public abstract class CollisionObject3D : Node3D
{
    [Export] public int CollisionLayer { get; set; } = 1;
    [Export] public int CollisionMask { get; set; } = 1;
}

[RegisteredType("Area3D", "3D/Physics")]
[Signal("body_entered")]
[Signal("body_exited")]
public sealed class Area3D : CollisionObject3D
{
    [Export] public bool Monitoring { get; set; } = true;
    [Export("range:-100,100")] public float GravityOverride { get; set; }
}

[RegisteredType("StaticBody3D", "3D/Physics")]
public sealed class StaticBody3D : CollisionObject3D
{
    [Export] public PhysicsMaterial? PhysicsMaterial { get; set; }
}

[RegisteredType("RigidBody3D", "3D/Physics")]
[Signal("body_entered")]
public sealed class RigidBody3D : CollisionObject3D
{
    [Export("range:0.01,1000")] public float Mass { get; set; } = 1f;
    [Export("range:0,100")] public float GravityScale { get; set; } = 1f;
    [Export] public Vector3 LinearVelocity { get; set; }
    [Export] public Vector3 AngularVelocity { get; set; }
    [Export("range:0,1")] public float LinearDamp { get; set; }
    [Export] public bool Freeze { get; set; }
}

[RegisteredType("CharacterBody3D", "3D/Physics")]
public sealed class CharacterBody3D : CollisionObject3D
{
    [Export] public Vector3 Velocity { get; set; }
    [Export("range:0,90")] public float FloorMaxAngleDegrees { get; set; } = 45f;
    [Export] public Vector3 UpDirection { get; set; } = Vector3.Up;
    [Export("range:0,100")] public float Speed { get; set; } = 5f;
    [Export("range:0,50")] public float JumpVelocity { get; set; } = 4.5f;

    public bool IsOnFloor { get; private set; }

    /// <summary>Simple ground-plane kinematic integration (y = 0 floor).
    /// A full broad-phase solver is on the roadmap; this keeps demos playable.</summary>
    public Vector3 MoveAndSlide(double delta, float floorHeight = 0f)
    {
        Vector3 next = Position + Velocity * (float)delta;
        if (next.Y <= floorHeight)
        {
            next = new Vector3(next.X, floorHeight, next.Z);
            Velocity = new Vector3(Velocity.X, Mathf.Max(0, Velocity.Y), Velocity.Z);
            IsOnFloor = true;
        }
        else IsOnFloor = false;
        Position = next;
        return next;
    }
}

[RegisteredType("AnimatableBody3D", "3D/Physics")]
public sealed class AnimatableBody3D : CollisionObject3D
{
    [Export] public bool SyncToPhysics { get; set; } = true;
}

[RegisteredType("CollisionShape3D", "3D/Physics")]
public sealed class CollisionShape3D : Node3D
{
    [Export] public Shape3D? Shape { get; set; }
    [Export] public bool Disabled { get; set; }
}

[RegisteredType("VehicleBody3D", "3D/Physics")]
public sealed class VehicleBody3D : CollisionObject3D
{
    [Export("range:0,1")] public float EngineForce { get; set; }
    [Export("range:0,1")] public float Brake { get; set; }
    [Export("range:-1,1")] public float Steering { get; set; }
    [Export] public float Mass { get; set; } = 40f;
}

[RegisteredType("SpringArm3D", "3D/Physics")]
public sealed class SpringArm3D : Node3D
{
    [Export] public float SpringLength { get; set; } = 1f;
    [Export("range:0,1")] public float Margin { get; set; } = 0.01f;
    [Export] public int CollisionMask { get; set; } = 1;
}

[RegisteredType("Joint3D", "3D/Physics")]
public sealed class Joint3D : Node3D
{
    [Export] public string NodeA { get; set; } = string.Empty;
    [Export] public string NodeB { get; set; } = string.Empty;
}
