using Gimnasy.Core.Math;
using Gimnasy.Core.Object;

namespace Gimnasy.Core.Resources;

// ---------------------------------------------------------------------------
// Texture resources
// ---------------------------------------------------------------------------

public abstract class Texture : Resource { }

[RegisteredType("Texture2D", "Texture")]
public sealed class Texture2D : Texture
{
    public override string ResourceType => "Texture2D";
    [Export("file:*.png,*.jpg,*.webp")] public string? SourceFile { get; set; }
    [Export] public int Width { get; set; }
    [Export] public int Height { get; set; }
    [Export] public bool GenerateMipmaps { get; set; } = true;
    [Export] public bool Filter { get; set; } = true;
}

[RegisteredType("AtlasTexture", "Texture")]
public sealed class AtlasTexture : Texture
{
    public override string ResourceType => "AtlasTexture";
    [Export] public Texture2D? Atlas { get; set; }
    [Export] public Rect2 Region { get; set; }
}

[RegisteredType("CubemapTexture", "Texture")]
public sealed class CubemapTexture : Texture
{
    public override string ResourceType => "CubemapTexture";
    [Export] public int Size { get; set; } = 512;
}

[RegisteredType("Texture3D", "Texture")]
public sealed class Texture3D : Texture
{
    public override string ResourceType => "Texture3D";
    [Export] public Vector3 Dimensions { get; set; } = new Vector3(32, 32, 32);
}

[RegisteredType("ViewportTexture", "Texture")]
public sealed class ViewportTexture : Texture
{
    public override string ResourceType => "ViewportTexture";
    [Export] public string ViewportPath { get; set; } = string.Empty;
}

// ---------------------------------------------------------------------------
// Mesh resources
// ---------------------------------------------------------------------------

public abstract class Mesh : Resource
{
    [Export] public Material? SurfaceMaterial { get; set; }
}

[RegisteredType("ArrayMesh", "Mesh")]
public sealed class ArrayMesh : Mesh
{
    public override string ResourceType => "ArrayMesh";
    [Export("file:*.obj,*.gltf,*.glb")] public string? SourceFile { get; set; }
    [Export] public int SurfaceCount { get; set; }
}

public abstract class PrimitiveMesh : Mesh { }

[RegisteredType("BoxMesh", "Mesh")]
public sealed class BoxMesh : PrimitiveMesh
{
    public override string ResourceType => "BoxMesh";
    [Export] public Vector3 Size { get; set; } = Vector3.One;
}

[RegisteredType("SphereMesh", "Mesh")]
public sealed class SphereMesh : PrimitiveMesh
{
    public override string ResourceType => "SphereMesh";
    [Export] public float Radius { get; set; } = 0.5f;
    [Export] public float Height { get; set; } = 1f;
    [Export] public int RadialSegments { get; set; } = 64;
    [Export] public int Rings { get; set; } = 32;
}

[RegisteredType("CylinderMesh", "Mesh")]
public sealed class CylinderMesh : PrimitiveMesh
{
    public override string ResourceType => "CylinderMesh";
    [Export] public float TopRadius { get; set; } = 0.5f;
    [Export] public float BottomRadius { get; set; } = 0.5f;
    [Export] public float Height { get; set; } = 2f;
    [Export] public int RadialSegments { get; set; } = 64;
}

[RegisteredType("CapsuleMesh", "Mesh")]
public sealed class CapsuleMesh : PrimitiveMesh
{
    public override string ResourceType => "CapsuleMesh";
    [Export] public float Radius { get; set; } = 0.5f;
    [Export] public float Height { get; set; } = 2f;
}

[RegisteredType("PlaneMesh", "Mesh")]
public sealed class PlaneMesh : PrimitiveMesh
{
    public override string ResourceType => "PlaneMesh";
    [Export] public Vector2 Size { get; set; } = new Vector2(2, 2);
    [Export] public int SubdivideWidth { get; set; }
    [Export] public int SubdivideDepth { get; set; }
}

[RegisteredType("PrismMesh", "Mesh")]
public sealed class PrismMesh : PrimitiveMesh
{
    public override string ResourceType => "PrismMesh";
    [Export] public Vector3 Size { get; set; } = Vector3.One;
}

[RegisteredType("TorusMesh", "Mesh")]
public sealed class TorusMesh : PrimitiveMesh
{
    public override string ResourceType => "TorusMesh";
    [Export] public float InnerRadius { get; set; } = 0.5f;
    [Export] public float OuterRadius { get; set; } = 1f;
}

// ---------------------------------------------------------------------------
// Shapes (collision)
// ---------------------------------------------------------------------------

public abstract class Shape2D : Resource { }
public abstract class Shape3D : Resource { }

[RegisteredType("RectangleShape2D", "Shape")]
public sealed class RectangleShape2D : Shape2D
{
    public override string ResourceType => "RectangleShape2D";
    [Export] public Vector2 Size { get; set; } = new Vector2(20, 20);
}

[RegisteredType("CircleShape2D", "Shape")]
public sealed class CircleShape2D : Shape2D
{
    public override string ResourceType => "CircleShape2D";
    [Export] public float Radius { get; set; } = 10f;
}

[RegisteredType("CapsuleShape2D", "Shape")]
public sealed class CapsuleShape2D : Shape2D
{
    public override string ResourceType => "CapsuleShape2D";
    [Export] public float Radius { get; set; } = 10f;
    [Export] public float Height { get; set; } = 20f;
}

[RegisteredType("BoxShape3D", "Shape")]
public sealed class BoxShape3D : Shape3D
{
    public override string ResourceType => "BoxShape3D";
    [Export] public Vector3 Size { get; set; } = Vector3.One;
}

[RegisteredType("SphereShape3D", "Shape")]
public sealed class SphereShape3D : Shape3D
{
    public override string ResourceType => "SphereShape3D";
    [Export] public float Radius { get; set; } = 0.5f;
}

[RegisteredType("CapsuleShape3D", "Shape")]
public sealed class CapsuleShape3D : Shape3D
{
    public override string ResourceType => "CapsuleShape3D";
    [Export] public float Radius { get; set; } = 0.5f;
    [Export] public float Height { get; set; } = 2f;
}

// ---------------------------------------------------------------------------
// Audio / fonts / curves
// ---------------------------------------------------------------------------

[RegisteredType("AudioStream", "Audio")]
public sealed class AudioStream : Resource
{
    public override string ResourceType => "AudioStream";
    [Export("file:*.wav,*.ogg,*.mp3")] public string? SourceFile { get; set; }
    [Export] public bool Loop { get; set; }
    [Export] public double LengthSeconds { get; set; }
}

[RegisteredType("Font", "GUI")]
public sealed class Font : Resource
{
    public override string ResourceType => "Font";
    [Export("file:*.ttf,*.otf")] public string? SourceFile { get; set; }
    [Export] public int BaseSize { get; set; } = 16;
    [Export] public bool Antialiased { get; set; } = true;
}

[RegisteredType("Curve", "Math")]
public sealed class Curve : Resource
{
    public override string ResourceType => "Curve";
    [Export] public float MinValue { get; set; } = 0f;
    [Export] public float MaxValue { get; set; } = 1f;
}

[RegisteredType("Gradient", "Math")]
public sealed class Gradient : Resource
{
    public override string ResourceType => "Gradient";
    [Export] public Color From { get; set; } = Color.Black;
    [Export] public Color To { get; set; } = Color.White;
}
