using Gimnasy.Core.Math;
using Gimnasy.Core.Object;
using Gimnasy.Core.Resources;

namespace Gimnasy.Core.Terrain;

/// <summary>
/// A texture/material layer painted onto the terrain via splat weights, with
/// automatic placement rules by height and slope (so rock appears on cliffs,
/// snow on peaks, sand near the water line — the way professional terrain
/// systems auto-texture a landscape).
/// </summary>
[RegisteredType("TerrainLayer", "3D/Terrain")]
public sealed class TerrainLayer : Resource
{
    public override string ResourceType => "TerrainLayer";

    [Export] public string LayerName { get; set; } = "Layer";
    [Export("file:*.png,*.jpg")] public string? AlbedoTexture { get; set; }
    [Export("file:*.png")] public string? NormalTexture { get; set; }
    [Export("file:*.png")] public string? RoughnessTexture { get; set; }
    [Export("file:*.png")] public string? HeightTexture { get; set; }
    [Export] public Color Tint { get; set; } = Color.White;

    [Export] public Vector2 UvTiling { get; set; } = new Vector2(10, 10);
    [Export] public Vector2 UvOffset { get; set; }
    [Export("range:0,1")] public float Metallic { get; set; }
    [Export("range:0,1")] public float Roughness { get; set; } = 0.85f;
    [Export("range:0,4")] public float NormalStrength { get; set; } = 1f;

    /// <summary>Triplanar projection avoids stretching on steep slopes.</summary>
    [Export] public bool Triplanar { get; set; }
    [Export("range:0,1")] public float HeightBlend { get; set; } = 0.5f;

    // ---- Automatic placement rules ---------------------------------------
    [Export] public bool AutoPlace { get; set; } = true;
    [Export] public float HeightMin { get; set; } = 0f;
    [Export] public float HeightMax { get; set; } = 1000f;
    [Export("range:0,90")] public float SlopeMinDegrees { get; set; } = 0f;
    [Export("range:0,90")] public float SlopeMaxDegrees { get; set; } = 90f;
    [Export("range:0,200")] public float HeightFalloff { get; set; } = 4f;
    [Export("range:0,90")] public float SlopeFalloff { get; set; } = 8f;

    /// <summary>Weight in [0,1] this layer contributes at a sample given its
    /// world height and slope (degrees), using soft falloffs at the edges.</summary>
    public float WeightAt(float height, float slopeDegrees)
    {
        if (!AutoPlace) return 0f;
        float h = Band(height, HeightMin, HeightMax, HeightFalloff);
        float s = Band(slopeDegrees, SlopeMinDegrees, SlopeMaxDegrees, SlopeFalloff);
        return h * s;
    }

    private static float Band(float v, float min, float max, float falloff)
    {
        if (falloff <= Mathf.Epsilon)
            return v >= min && v <= max ? 1f : 0f;
        float lower = Mathf.Clamp01((v - (min - falloff)) / falloff);
        float upper = Mathf.Clamp01(((max + falloff) - v) / falloff);
        return Mathf.Clamp01(Mathf.Min(lower, upper));
    }
}
