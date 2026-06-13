using Gimnasy.Core.Math;
using Gimnasy.Core.Object;
using Gimnasy.Core.Resources;

namespace Gimnasy.Nodes;

// ===========================================================================
//  Visual-effects (VFX) node catalog: smoke, fire, water, clouds, particles,
//  trails and screen effects. Each is a self-contained, serializable node with
//  artist-facing parameters; a GPU backend consumes these to simulate/render.
// ===========================================================================

public enum ParticleBlend { Additive, AlphaBlend, PremultAlpha, Subtract }
public enum EmissionShape { Point, Sphere, Box, Cone, Ring, Mesh }

/// <summary>Shared base for emitter-style effects (the data a particle GPU
/// system needs to drive a simulation).</summary>
public abstract class VfxEmitter3D : Node3D
{
    [Export] public bool Emitting { get; set; } = true;
    [Export("range:0,100000")] public int Amount { get; set; } = 256;
    [Export("range:0.01,600")] public float Lifetime { get; set; } = 2f;
    [Export("range:0,1")] public float Explosiveness { get; set; }
    [Export("range:0.01,10")] public float SpeedScale { get; set; } = 1f;
    [Export] public EmissionShape Shape { get; set; } = EmissionShape.Sphere;
    [Export] public Vector3 ShapeExtents { get; set; } = Vector3.One;
    [Export] public ParticleBlend Blend { get; set; } = ParticleBlend.AlphaBlend;
    [Export] public Gradient? ColorRamp { get; set; }
    [Export] public Material? Material { get; set; }
}

[RegisteredType("Smoke3D", "VFX")]
public sealed class Smoke3D : VfxEmitter3D
{
    [Export("range:0,1")] public float Density { get; set; } = 0.6f;
    [Export] public Color SmokeColor { get; set; } = new Color(0.4f, 0.4f, 0.42f, 0.8f);
    [Export] public Vector3 RiseVelocity { get; set; } = new Vector3(0, 1.2f, 0);
    [Export("range:0,10")] public float Turbulence { get; set; } = 1.5f;
    [Export("range:0,8")] public float Dissipation { get; set; } = 1f;
    [Export("range:0,4")] public float BillowScale { get; set; } = 1f;

    public Smoke3D() { Blend = ParticleBlend.AlphaBlend; Lifetime = 4f; }
}

[RegisteredType("Fire3D", "VFX")]
public sealed class Fire3D : VfxEmitter3D
{
    [Export("range:0,16")] public float Intensity { get; set; } = 2f;
    [Export] public Color CoreColor { get; set; } = new Color(1f, 0.9f, 0.4f);
    [Export] public Color TipColor { get; set; } = new Color(1f, 0.3f, 0.05f);
    [Export("range:0,8")] public float FlameHeight { get; set; } = 2f;
    [Export] public bool EmitsLight { get; set; } = true;
    [Export] public bool EmitsSmoke { get; set; } = true;

    public Fire3D() { Blend = ParticleBlend.Additive; Lifetime = 1.2f; }
}

[RegisteredType("WaterVolume3D", "VFX")]
public sealed class WaterVolume3D : Node3D
{
    [Export] public Vector3 Size { get; set; } = new Vector3(20, 4, 20);
    [Export] public Color ShallowColor { get; set; } = new Color(0.2f, 0.6f, 0.7f, 0.7f);
    [Export] public Color DeepColor { get; set; } = new Color(0.05f, 0.2f, 0.35f, 0.95f);
    [Export("range:0,4")] public float WaveAmplitude { get; set; } = 0.3f;
    [Export("range:0,8")] public float WaveSpeed { get; set; } = 1f;
    [Export("range:0,8")] public float WaveScale { get; set; } = 2f;
    [Export("range:0,1")] public float Transparency { get; set; } = 0.6f;
    [Export("range:0,4")] public float RefractionStrength { get; set; } = 1f;
    [Export("range:0,1")] public float FoamAmount { get; set; } = 0.3f;
    [Export] public bool Buoyancy { get; set; } = true;

    /// <summary>Gerstner-style surface height sample, usable for buoyancy and
    /// for placing objects on the water at runtime.</summary>
    public float SampleHeight(Vector2 xz, float time)
    {
        float h = 0f;
        h += Mathf.Sin((xz.X * 0.5f + time * WaveSpeed) / Mathf.Max(0.01f, WaveScale)) * WaveAmplitude;
        h += Mathf.Cos((xz.Y * 0.4f + time * WaveSpeed * 0.8f) / Mathf.Max(0.01f, WaveScale)) * WaveAmplitude * 0.6f;
        return Position.Y + h;
    }
}

[RegisteredType("CloudLayer3D", "VFX")]
public sealed class CloudLayer3D : Node3D
{
    [Export("range:0,1")] public float Coverage { get; set; } = 0.5f;
    [Export("range:0,1")] public float Density { get; set; } = 0.4f;
    [Export] public float Altitude { get; set; } = 200f;
    [Export] public float Thickness { get; set; } = 80f;
    [Export] public Color Tint { get; set; } = Color.White;
    [Export] public Vector2 WindDirection { get; set; } = new Vector2(1, 0);
    [Export("range:0,50")] public float WindSpeed { get; set; } = 4f;
    [Export("range:1,8")] public int NoiseOctaves { get; set; } = 4;
}

[RegisteredType("VolumetricFog3D", "VFX")]
public sealed class VolumetricFog3D : Node3D
{
    [Export] public Vector3 Size { get; set; } = new Vector3(20, 10, 20);
    [Export("range:0,1")] public float Density { get; set; } = 0.2f;
    [Export] public Color Albedo { get; set; } = Color.White;
    [Export("range:0,16")] public float Emission { get; set; }
    [Export("range:-1,1")] public float Anisotropy { get; set; } = 0.2f;
}

[RegisteredType("Explosion3D", "VFX")]
public sealed class Explosion3D : VfxEmitter3D
{
    [Export("range:0,100")] public float Radius { get; set; } = 4f;
    [Export("range:0,200")] public float Force { get; set; } = 30f;
    [Export] public bool Shockwave { get; set; } = true;
    [Export] public bool Debris { get; set; } = true;
    [Export] public bool CameraShake { get; set; } = true;

    public Explosion3D() { Explosiveness = 1f; Blend = ParticleBlend.Additive; }
}

[RegisteredType("Trail3D", "VFX")]
public sealed class Trail3D : Node3D
{
    [Export("range:0,32")] public float Width { get; set; } = 0.5f;
    [Export("range:0,16")] public float Lifetime { get; set; } = 0.5f;
    [Export] public Gradient? ColorOverLifetime { get; set; }
    [Export] public bool FaceCamera { get; set; } = true;
    [Export("range:2,256")] public int MaxPoints { get; set; } = 64;
}

[RegisteredType("Trail2D", "VFX")]
public sealed class Trail2D : Node2D
{
    [Export("range:0,256")] public float Width { get; set; } = 8f;
    [Export("range:0,16")] public float Lifetime { get; set; } = 0.5f;
    [Export] public Color StartColor { get; set; } = Color.White;
    [Export] public Color EndColor { get; set; } = Color.Transparent;
}

[RegisteredType("Lightning3D", "VFX")]
public sealed class Lightning3D : Node3D
{
    [Export] public Vector3 Target { get; set; } = new Vector3(0, -10, 0);
    [Export("range:0,16")] public int Segments { get; set; } = 8;
    [Export("range:0,8")] public float Jaggedness { get; set; } = 1.5f;
    [Export] public Color Color { get; set; } = new Color(0.7f, 0.85f, 1f);
    [Export("range:0,16")] public float GlowEnergy { get; set; } = 4f;
}

[RegisteredType("Splash3D", "VFX")]
public sealed class Splash3D : VfxEmitter3D
{
    [Export("range:0,32")] public float Spread { get; set; } = 2f;
    [Export] public Color DropletColor { get; set; } = new Color(0.6f, 0.8f, 0.9f, 0.8f);

    public Splash3D() { Explosiveness = 0.8f; Lifetime = 0.8f; }
}

[RegisteredType("WeatherSystem", "VFX")]
public sealed class WeatherSystem : Gimnasy.Core.Scene.Node
{
    public enum WeatherKind { Clear, Rain, Snow, Storm, Fog, Sandstorm }
    [Export] public WeatherKind Kind { get; set; } = WeatherKind.Clear;
    [Export("range:0,1")] public float Intensity { get; set; } = 0.5f;
    [Export("range:0,100")] public float WindStrength { get; set; } = 5f;
    [Export] public bool AffectsParticles { get; set; } = true;
}

// ---- Screen / post-process effects ----------------------------------------

[RegisteredType("ScreenEffect", "VFX/PostProcess")]
public sealed class ScreenEffect : Gimnasy.Core.Scene.Node
{
    [Export("range:0,2")] public float Bloom { get; set; }
    [Export("range:0,1")] public float Vignette { get; set; }
    [Export("range:0,1")] public float ChromaticAberration { get; set; }
    [Export("range:0,1")] public float MotionBlur { get; set; }
    [Export("range:0,1")] public float FilmGrain { get; set; }
    [Export] public Gradient? ColorGrading { get; set; }
}
