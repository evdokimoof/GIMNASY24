using Gimnasy.Core.Math;
using Gimnasy.Core.Object;
using Gimnasy.Core.Resources;

namespace Gimnasy.Nodes;

// ===========================================================================
//  2D node catalog. Rendering nodes, cameras, paths, particles and tilemaps.
// ===========================================================================

[RegisteredType("Sprite2D", "2D")]
public class Sprite2D : Node2D
{
    [Export] public Texture2D? Texture { get; set; }
    [Export] public bool Centered { get; set; } = true;
    [Export] public Vector2 Offset { get; set; }
    [Export] public bool FlipH { get; set; }
    [Export] public bool FlipV { get; set; }
    [Export] public int Hframes { get; set; } = 1;
    [Export] public int Vframes { get; set; } = 1;
    [Export] public int Frame { get; set; }
    [Export] public bool RegionEnabled { get; set; }
    [Export] public Rect2 RegionRect { get; set; }
}

[RegisteredType("AnimatedSprite2D", "2D")]
public sealed class AnimatedSprite2D : Node2D
{
    [Export] public string Animation { get; set; } = "default";
    [Export] public int Frame { get; set; }
    [Export("range:0,10")] public float SpeedScale { get; set; } = 1f;
    [Export] public bool Playing { get; set; }
    [Export] public bool Centered { get; set; } = true;
}

[RegisteredType("Camera2D", "2D")]
public sealed class Camera2D : Node2D
{
    [Export] public bool Current { get; set; } = true;
    [Export] public Vector2 Zoom { get; set; } = Vector2.One;
    [Export] public Vector2 Offset { get; set; }
    [Export("range:0,30")] public float PositionSmoothingSpeed { get; set; } = 5f;
    [Export] public bool PositionSmoothingEnabled { get; set; }
    [Export] public float LimitLeft { get; set; } = -10000000;
    [Export] public float LimitTop { get; set; } = -10000000;
    [Export] public float LimitRight { get; set; } = 10000000;
    [Export] public float LimitBottom { get; set; } = 10000000;
}

[RegisteredType("Marker2D", "2D")]
public sealed class Marker2D : Node2D
{
    [Export] public float GizmoExtents { get; set; } = 10f;
}

[RegisteredType("Line2D", "2D")]
public sealed class Line2D : Node2D
{
    [Export] public float Width { get; set; } = 10f;
    [Export] public Color DefaultColor { get; set; } = Color.White;
    [Export] public bool Closed { get; set; }
    [Export] public int JointMode { get; set; }
}

[RegisteredType("Polygon2D", "2D")]
public sealed class Polygon2D : Node2D
{
    [Export] public Color Color { get; set; } = Color.White;
    [Export] public Texture2D? Texture { get; set; }
    [Export] public Vector2 TextureOffset { get; set; }
}

[RegisteredType("PointLight2D", "2D")]
public sealed class PointLight2D : Node2D
{
    [Export] public Color Color { get; set; } = Color.White;
    [Export("range:0,16")] public float Energy { get; set; } = 1f;
    [Export] public Texture2D? Texture { get; set; }
    [Export] public float TextureScale { get; set; } = 1f;
}

[RegisteredType("DirectionalLight2D", "2D")]
public sealed class DirectionalLight2D : Node2D
{
    [Export] public Color Color { get; set; } = Color.White;
    [Export("range:0,16")] public float Energy { get; set; } = 1f;
    [Export] public float MaxDistance { get; set; } = 10000f;
}

[RegisteredType("LightOccluder2D", "2D")]
public sealed class LightOccluder2D : Node2D
{
    [Export] public int OccluderLightMask { get; set; } = 1;
    [Export] public bool SdfCollision { get; set; } = true;
}

[RegisteredType("Path2D", "2D")]
public sealed class Path2D : Node2D
{
    [Export] public Curve? Curve { get; set; }
}

[RegisteredType("PathFollow2D", "2D")]
public sealed class PathFollow2D : Node2D
{
    [Export] public float Progress { get; set; }
    [Export("range:0,1")] public float ProgressRatio { get; set; }
    [Export] public bool Loop { get; set; } = true;
    [Export] public bool Rotates { get; set; } = true;
}

[RegisteredType("RayCast2D", "2D")]
public sealed class RayCast2D : Node2D
{
    [Export] public bool Enabled { get; set; } = true;
    [Export] public Vector2 TargetPosition { get; set; } = new Vector2(0, 50);
    [Export] public int CollisionMask { get; set; } = 1;
    [Export] public bool CollideWithAreas { get; set; }
    [Export] public bool CollideWithBodies { get; set; } = true;
}

[RegisteredType("TileMap", "2D")]
public sealed class TileMap : Node2D
{
    [Export] public Vector2I TileSize { get; set; } = new Vector2I(16, 16);
    [Export] public int RenderingQuadrantSize { get; set; } = 16;
    [Export("file:*.tres")] public string? TileSet { get; set; }
}

[RegisteredType("ParallaxBackground", "2D")]
public sealed class ParallaxBackground : Gimnasy.Core.Scene.Node
{
    [Export] public Vector2 ScrollOffset { get; set; }
    [Export] public Vector2 ScrollBaseScale { get; set; } = Vector2.One;
}

[RegisteredType("ParallaxLayer", "2D")]
public sealed class ParallaxLayer : Node2D
{
    [Export] public Vector2 MotionScale { get; set; } = Vector2.One;
    [Export] public Vector2 MotionOffset { get; set; }
    [Export] public Vector2 MotionMirroring { get; set; }
}

[RegisteredType("GPUParticles2D", "2D")]
public sealed class GpuParticles2D : Node2D
{
    [Export] public bool Emitting { get; set; } = true;
    [Export] public int Amount { get; set; } = 8;
    [Export] public float Lifetime { get; set; } = 1f;
    [Export] public bool OneShot { get; set; }
    [Export("range:0,1")] public float Explosiveness { get; set; }
    [Export] public Texture2D? Texture { get; set; }
}

[RegisteredType("CPUParticles2D", "2D")]
public sealed class CpuParticles2D : Node2D
{
    [Export] public bool Emitting { get; set; } = true;
    [Export] public int Amount { get; set; } = 8;
    [Export] public float Lifetime { get; set; } = 1f;
    [Export] public Vector2 Gravity { get; set; } = new Vector2(0, 98);
}

[RegisteredType("BackBufferCopy", "2D")]
public sealed class BackBufferCopy : Node2D
{
    [Export] public int CopyMode { get; set; } = 1;
    [Export] public Rect2 Rect { get; set; } = new Rect2(-100, -100, 200, 200);
}

[RegisteredType("RemoteTransform2D", "2D")]
public sealed class RemoteTransform2D : Node2D
{
    [Export] public string RemotePath { get; set; } = string.Empty;
    [Export] public bool UseGlobalCoordinates { get; set; } = true;
}
