using Gimnasy.Core.Math;
using Gimnasy.Core.Object;

namespace Gimnasy.Core.Resources;

public enum CullMode { Back, Front, Disabled }
public enum ShadingMode { Unshaded, PerPixel, PerVertex }
public enum BlendMode { Mix, Add, Subtract, Multiply }
public enum TransparencyMode { Opaque, AlphaBlend, AlphaScissor }

/// <summary>Base for all materials. Saved to <c>.material</c> files.</summary>
public abstract class Material : Resource
{
    [Export] public BlendMode Blend { get; set; } = BlendMode.Mix;
    [Export(Category = "Rendering")] public int RenderPriority { get; set; }
}

/// <summary>The standard physically based 3D material (albedo/metallic/rough).</summary>
[RegisteredType("StandardMaterial3D", "Material")]
public sealed class StandardMaterial3D : Material
{
    public override string ResourceType => "StandardMaterial3D";

    [Export(Category = "Albedo")] public Color AlbedoColor { get; set; } = Color.White;
    [Export("file:*.png,*.jpg", Category = "Albedo")] public Texture2D? AlbedoTexture { get; set; }

    [Export("range:0,1", Category = "Metallic")] public float Metallic { get; set; } = 0f;
    [Export("range:0,1", Category = "Metallic")] public float Specular { get; set; } = 0.5f;
    [Export("range:0,1", Category = "Roughness")] public float Roughness { get; set; } = 1f;

    [Export(Category = "Emission")] public bool EmissionEnabled { get; set; }
    [Export(Category = "Emission")] public Color Emission { get; set; } = Color.Black;
    [Export("range:0,16", Category = "Emission")] public float EmissionEnergy { get; set; } = 1f;

    [Export("range:0,1", Category = "NormalMap")] public float NormalScale { get; set; } = 1f;
    [Export(Category = "NormalMap")] public Texture2D? NormalTexture { get; set; }

    [Export(Category = "Transparency")] public TransparencyMode Transparency { get; set; } = TransparencyMode.Opaque;
    [Export(Category = "Shading")] public ShadingMode Shading { get; set; } = ShadingMode.PerPixel;
    [Export(Category = "Culling")] public CullMode Cull { get; set; } = CullMode.Back;
    [Export(Category = "Rendering")] public bool DepthTest { get; set; } = true;
}

/// <summary>A material backed by a custom shader program (GLSL-like source).</summary>
[RegisteredType("ShaderMaterial", "Material")]
public sealed class ShaderMaterial : Material
{
    public override string ResourceType => "ShaderMaterial";
    [Export("file:*.gsl")] public string? ShaderPath { get; set; }
    [Export] public string ShaderCode { get; set; } = string.Empty;
}

/// <summary>Material for 2D canvas items (sprites, UI).</summary>
[RegisteredType("CanvasItemMaterial", "Material")]
public sealed class CanvasItemMaterial : Material
{
    public override string ResourceType => "CanvasItemMaterial";
    [Export] public Color Modulate { get; set; } = Color.White;
    [Export] public bool ParticlesAnimation { get; set; }
}

/// <summary>Physics material controlling friction and bounciness.</summary>
[RegisteredType("PhysicsMaterial", "Material")]
public sealed class PhysicsMaterial : Resource
{
    public override string ResourceType => "PhysicsMaterial";
    [Export("range:0,1")] public float Friction { get; set; } = 1f;
    [Export("range:0,1")] public float Bounce { get; set; } = 0f;
    [Export] public bool Absorbent { get; set; }
    [Export] public bool Rough { get; set; }
}
