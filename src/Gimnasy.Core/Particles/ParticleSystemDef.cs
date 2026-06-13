using Gimnasy.Core.Object;
using Gimnasy.Core.Resources;

namespace Gimnasy.Core.Particles;

/// <summary>
/// A complete particle-system definition: the aggregate of every module
/// (emission, shape, motion, colour, size, rotation, noise, collision,
/// sub-emitters, texture-sheet, trails, lights, renderer). Mirrors the depth of
/// Unity's Shuriken / Godot's process material so artists can author genuinely
/// complex VFX. Serialised in full by <see cref="ParticleIO"/>.
/// </summary>
[RegisteredType("ParticleSystem", "VFX")]
public sealed class ParticleSystemDef : Resource
{
    public override string ResourceType => "ParticleSystem";

    public MainModule Main { get; set; } = new();
    public EmissionModule Emission { get; set; } = new();
    public ShapeModule Shape { get; set; } = new();
    public VelocityOverLifetimeModule VelocityOverLifetime { get; set; } = new();
    public LimitVelocityOverLifetimeModule LimitVelocity { get; set; } = new();
    public ForceOverLifetimeModule Force { get; set; } = new();
    public ColorOverLifetimeModule ColorOverLifetime { get; set; } = new();
    public SizeOverLifetimeModule SizeOverLifetime { get; set; } = new();
    public RotationOverLifetimeModule RotationOverLifetime { get; set; } = new();
    public NoiseModule Noise { get; set; } = new();
    public CollisionModule Collision { get; set; } = new();
    public SubEmittersModule SubEmitters { get; set; } = new();
    public TextureSheetAnimationModule TextureSheet { get; set; } = new();
    public TrailsModule Trails { get; set; } = new();
    public LightsModule Lights { get; set; } = new();
    public RendererModule Renderer { get; set; } = new();

    /// <summary>How many optional modules are switched on (for inspector summary).</summary>
    public int ActiveModuleCount
    {
        get
        {
            int n = 0;
            if (Emission.Enabled) n++;
            if (Shape.Enabled) n++;
            if (VelocityOverLifetime.Enabled) n++;
            if (LimitVelocity.Enabled) n++;
            if (Force.Enabled) n++;
            if (ColorOverLifetime.Enabled) n++;
            if (SizeOverLifetime.Enabled) n++;
            if (RotationOverLifetime.Enabled) n++;
            if (Noise.Enabled) n++;
            if (Collision.Enabled) n++;
            if (SubEmitters.Enabled) n++;
            if (TextureSheet.Enabled) n++;
            if (Trails.Enabled) n++;
            if (Lights.Enabled) n++;
            return n;
        }
    }

    /// <summary>A ready-made fire preset showing many modules cooperating.</summary>
    public static ParticleSystemDef FirePreset()
    {
        var p = new ParticleSystemDef { Name = "Fire" };
        p.Main.StartLifetime = MinMaxCurve.Range(0.8f, 1.4f);
        p.Main.StartSpeed = MinMaxCurve.Range(1.5f, 3f);
        p.Main.StartSize = MinMaxCurve.Range(0.4f, 0.9f);
        p.Main.MaxParticles = 600;
        p.Main.StartColor = ColorGradient.TwoColor(new Color(1f, 0.85f, 0.3f), new Color(1f, 0.35f, 0.05f));
        p.Emission.RateOverTime = MinMaxCurve.Const(120f);
        p.Shape.Shape = EmitterShape.Cone;
        p.Shape.AngleDegrees = 12f;
        p.Shape.Radius = 0.4f;
        p.ColorOverLifetime.Enabled = true;
        p.SizeOverLifetime.Enabled = true;
        p.Noise.Enabled = true;
        p.Noise.Strength = MinMaxCurve.Const(1.5f);
        p.Lights.Enabled = true;
        p.Renderer.Mode = RenderMode.Billboard;
        return p;
    }
}
