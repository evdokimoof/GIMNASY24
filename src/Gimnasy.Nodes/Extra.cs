using Gimnasy.Core.Math;
using Gimnasy.Core.Object;
using Gimnasy.Core.Resources;

namespace Gimnasy.Nodes;

// ===========================================================================
//  Additional commonly used visual nodes.
// ===========================================================================

[RegisteredType("Sprite3D", "3D")]
public sealed class Sprite3D : Node3D
{
    [Export] public Texture2D? Texture { get; set; }
    [Export] public bool Billboard { get; set; }
    [Export("range:0.001,128")] public float PixelSize { get; set; } = 0.01f;
    [Export] public Color Modulate { get; set; } = Color.White;
    [Export] public bool DoubleSided { get; set; } = true;
}

[RegisteredType("AnimatedSprite3D", "3D")]
public sealed class AnimatedSprite3D : Node3D
{
    [Export] public string Animation { get; set; } = "default";
    [Export] public int Frame { get; set; }
    [Export] public bool Playing { get; set; }
    [Export] public bool Billboard { get; set; }
}

[RegisteredType("Label3D", "3D")]
public sealed class Label3D : Node3D
{
    [Export] public string Text { get; set; } = string.Empty;
    [Export] public Font? Font { get; set; }
    [Export] public int FontSize { get; set; } = 32;
    [Export] public Color Modulate { get; set; } = Color.White;
    [Export] public bool Billboard { get; set; }
    [Export("range:0.001,128")] public float PixelSize { get; set; } = 0.005f;
}

[RegisteredType("MeshInstance2D", "2D")]
public sealed class MeshInstance2D : Node2D
{
    [Export] public Mesh? Mesh { get; set; }
    [Export] public Texture2D? Texture { get; set; }
}

[RegisteredType("CanvasModulate", "2D")]
public sealed class CanvasModulate : Node2D
{
    [Export] public Color Color { get; set; } = Color.White;
}

[RegisteredType("TouchScreenButton", "2D")]
[Signal("pressed")]
[Signal("released")]
public sealed class TouchScreenButton : Node2D
{
    [Export] public Texture2D? TextureNormal { get; set; }
    [Export] public Texture2D? TexturePressed { get; set; }
    [Export] public string Action { get; set; } = string.Empty;
}

[RegisteredType("VisibleOnScreenNotifier2D", "2D")]
[Signal("screen_entered")]
[Signal("screen_exited")]
public sealed class VisibleOnScreenNotifier2D : Node2D
{
    [Export] public Rect2 Rect { get; set; } = new Rect2(-10, -10, 20, 20);
}

[RegisteredType("VisibleOnScreenNotifier3D", "3D")]
[Signal("screen_entered")]
public sealed class VisibleOnScreenNotifier3D : Node3D
{
    [Export] public Aabb Aabb { get; set; } = new Aabb(new Vector3(-1, -1, -1), new Vector3(2, 2, 2));
}

[RegisteredType("Bone2D", "2D")]
public sealed class Bone2D : Node2D
{
    [Export] public float Length { get; set; } = 16f;
    [Export("range:-360,360")] public float BoneAngle { get; set; }
}

[RegisteredType("LightmapGI", "3D")]
public sealed class LightmapGI : Node3D
{
    [Export("range:0,8")] public float Energy { get; set; } = 1f;
    [Export] public int Quality { get; set; } = 1;
}

[RegisteredType("FogVolume", "3D")]
public sealed class FogVolume : Node3D
{
    [Export] public Vector3 Size { get; set; } = new Vector3(2, 2, 2);
    [Export("range:0,1")] public float Density { get; set; } = 1f;
    [Export] public Color Albedo { get; set; } = Color.White;
}
