using Gimnasy.Core.Input;
using Gimnasy.Core.Math;
using Gimnasy.Core.Object;
using Gimnasy.Nodes;

namespace Game;

/// <summary>
/// A third-person character controller. Because it is a registered node type,
/// scenes can reference it by the name "Player" and the editor lists it in the
/// "Add Node" dialog — this is how C# scripting works in Gimnasy: scripts are
/// node subclasses compiled into the game assembly.
/// </summary>
[RegisteredType("Player", "Game")]
[Signal("jumped")]
public sealed class Player : CharacterBody3D
{
    [Export] public float Gravity { get; set; } = 18f;
    [Export] public float MouseSensitivity { get; set; } = 0.005f;

    public override void _Ready()
    {
        AddToGroup("players");
        Console.WriteLine($"Player ready at {Position}");
    }

    public override void _PhysicsProcess(double delta)
    {
        Vector3 velocity = Velocity;

        // Gravity.
        if (!IsOnFloor)
            velocity = new Vector3(velocity.X, velocity.Y - Gravity * (float)delta, velocity.Z);

        // Jump.
        if (IsOnFloor && InputServer.IsActionJustPressed("jump"))
        {
            velocity = new Vector3(velocity.X, JumpVelocity, velocity.Z);
            EmitSignal("jumped");
        }

        // Planar movement from the input map.
        Vector2 input = InputServer.GetVector("ui_left", "ui_right", "ui_up", "ui_down");
        Vector3 dir = new Vector3(input.X, 0, input.Y).Normalized;
        velocity = new Vector3(dir.X * Speed, velocity.Y, dir.Z * Speed);

        Velocity = velocity;
        MoveAndSlide(delta);
    }
}
