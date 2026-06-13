using Gimnasy.Core.Math;

namespace Gimnasy.Core.Particles;

public enum SimulationSpace { Local, World }

/// <summary>Core spawn parameters shared by every particle.</summary>
public sealed class MainModule
{
    public float Duration { get; set; } = 5f;
    public bool Looping { get; set; } = true;
    public bool Prewarm { get; set; }
    public float StartDelay { get; set; }
    public MinMaxCurve StartLifetime { get; set; } = MinMaxCurve.Const(5f);
    public MinMaxCurve StartSpeed { get; set; } = MinMaxCurve.Const(5f);
    public MinMaxCurve StartSize { get; set; } = MinMaxCurve.Const(1f);
    public MinMaxCurve StartRotation { get; set; } = MinMaxCurve.Const(0f);
    public ColorGradient StartColor { get; set; } = ColorGradient.TwoColor(Color.White, Color.White);
    public MinMaxCurve GravityModifier { get; set; } = MinMaxCurve.Const(0f);
    public SimulationSpace SimulationSpace { get; set; } = SimulationSpace.Local;
    public float SimulationSpeed { get; set; } = 1f;
    public int MaxParticles { get; set; } = 1000;
}

public struct Burst
{
    public float Time;
    public MinMaxCurve Count;
    public int Cycles;
    public float Interval;
    public float Probability;
    public Burst(float time, MinMaxCurve count, int cycles = 1, float interval = 0.01f, float probability = 1f)
    { Time = time; Count = count; Cycles = cycles; Interval = interval; Probability = probability; }
}

public sealed class EmissionModule
{
    public bool Enabled { get; set; } = true;
    public MinMaxCurve RateOverTime { get; set; } = MinMaxCurve.Const(10f);
    public MinMaxCurve RateOverDistance { get; set; } = MinMaxCurve.Const(0f);
    public List<Burst> Bursts { get; } = new();
}

public enum EmitterShape { Sphere, Hemisphere, Cone, Box, Circle, Edge, Mesh }

public sealed class ShapeModule
{
    public bool Enabled { get; set; } = true;
    public EmitterShape Shape { get; set; } = EmitterShape.Cone;
    public float Radius { get; set; } = 1f;
    public float RadiusThickness { get; set; } = 1f;       // 0 = surface only, 1 = full volume
    public float AngleDegrees { get; set; } = 25f;          // cone half-angle
    public float ArcDegrees { get; set; } = 360f;
    public Vector3 Position { get; set; }
    public Vector3 Rotation { get; set; }
    public Vector3 Scale { get; set; } = Vector3.One;
    public float RandomizeDirection { get; set; }
    public bool AlignToDirection { get; set; }
}

public sealed class VelocityOverLifetimeModule
{
    public bool Enabled { get; set; }
    public MinMaxCurve X { get; set; } = MinMaxCurve.Const(0f);
    public MinMaxCurve Y { get; set; } = MinMaxCurve.Const(0f);
    public MinMaxCurve Z { get; set; } = MinMaxCurve.Const(0f);
    public MinMaxCurve OrbitalY { get; set; } = MinMaxCurve.Const(0f);
    public MinMaxCurve SpeedModifier { get; set; } = MinMaxCurve.Const(1f);
    public SimulationSpace Space { get; set; } = SimulationSpace.Local;
}

public sealed class LimitVelocityOverLifetimeModule
{
    public bool Enabled { get; set; }
    public MinMaxCurve Speed { get; set; } = MinMaxCurve.Const(1f);
    public float Dampen { get; set; } = 0.5f;
    public float Drag { get; set; }
}

public sealed class ForceOverLifetimeModule
{
    public bool Enabled { get; set; }
    public MinMaxCurve X { get; set; } = MinMaxCurve.Const(0f);
    public MinMaxCurve Y { get; set; } = MinMaxCurve.Const(0f);
    public MinMaxCurve Z { get; set; } = MinMaxCurve.Const(0f);
    public SimulationSpace Space { get; set; } = SimulationSpace.Local;
}

public sealed class ColorOverLifetimeModule
{
    public bool Enabled { get; set; }
    public ColorGradient Gradient { get; set; } = ColorGradient.TwoColor(Color.White, Color.Transparent);
}

public sealed class SizeOverLifetimeModule
{
    public bool Enabled { get; set; }
    public MinMaxCurve Size { get; set; } = new() { Mode = MinMaxMode.Curve, CurveMax = AnimationCurve.Linear(1f, 0f) };
}

public sealed class RotationOverLifetimeModule
{
    public bool Enabled { get; set; }
    public MinMaxCurve AngularVelocity { get; set; } = MinMaxCurve.Const(45f); // degrees/sec
}

/// <summary>Curl/turbulence noise field that perturbs particle motion.</summary>
public sealed class NoiseModule
{
    public bool Enabled { get; set; }
    public MinMaxCurve Strength { get; set; } = MinMaxCurve.Const(1f);
    public float Frequency { get; set; } = 0.5f;
    public Vector3 ScrollSpeed { get; set; }
    public int Octaves { get; set; } = 1;
    public float OctaveMultiplier { get; set; } = 0.5f;
    public float OctaveScale { get; set; } = 2f;
    public float Damping { get; set; } = 1f;
}

public enum CollisionType { Planes, World }

public sealed class CollisionModule
{
    public bool Enabled { get; set; }
    public CollisionType Type { get; set; } = CollisionType.World;
    public float Dampen { get; set; } = 0.5f;
    public float Bounce { get; set; } = 0.5f;
    public float LifetimeLoss { get; set; }
    public float MinKillSpeed { get; set; }
    public float RadiusScale { get; set; } = 1f;
    public List<float> PlaneHeights { get; } = new();   // simple Y-planes for the headless sim
}

public enum SubEmitterTrigger { Birth, Collision, Death }

public struct SubEmitter
{
    public SubEmitterTrigger Trigger;
    public string SystemPath;
    public float Probability;
    public SubEmitter(SubEmitterTrigger trigger, string systemPath, float probability = 1f)
    { Trigger = trigger; SystemPath = systemPath; Probability = probability; }
}

public sealed class SubEmittersModule
{
    public bool Enabled { get; set; }
    public List<SubEmitter> Emitters { get; } = new();
}

public sealed class TextureSheetAnimationModule
{
    public bool Enabled { get; set; }
    public int TilesX { get; set; } = 1;
    public int TilesY { get; set; } = 1;
    public MinMaxCurve FrameOverTime { get; set; } = new() { Mode = MinMaxMode.Curve, CurveMax = AnimationCurve.Linear(0f, 1f) };
    public int StartFrame { get; set; }
    public int Cycles { get; set; } = 1;
}

public sealed class TrailsModule
{
    public bool Enabled { get; set; }
    public float Ratio { get; set; } = 1f;
    public MinMaxCurve Lifetime { get; set; } = MinMaxCurve.Const(0.4f);
    public MinMaxCurve WidthOverTrail { get; set; } = MinMaxCurve.Const(1f);
    public ColorGradient ColorOverLifetime { get; set; } = ColorGradient.TwoColor(Color.White, Color.Transparent);
    public bool DieWithParticles { get; set; } = true;
}

public sealed class LightsModule
{
    public bool Enabled { get; set; }
    public float Ratio { get; set; } = 0.1f;
    public Color Color { get; set; } = Color.White;
    public float Range { get; set; } = 5f;
    public float Intensity { get; set; } = 1f;
}

public enum RenderMode { Billboard, StretchedBillboard, HorizontalBillboard, VerticalBillboard, Mesh }
public enum ParticleSortMode { None, ByDistance, OldestFirst, YoungestFirst }

public sealed class RendererModule
{
    public RenderMode Mode { get; set; } = RenderMode.Billboard;
    public string? Material { get; set; }
    public string? Mesh { get; set; }
    public ParticleSortMode SortMode { get; set; } = ParticleSortMode.ByDistance;
    public float LengthScale { get; set; } = 2f;        // for stretched billboards
    public float VelocityScale { get; set; }
    public float MinParticleSize { get; set; }
    public float MaxParticleSize { get; set; } = 0.5f;
}
