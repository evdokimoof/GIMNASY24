using Gimnasy.Core.Math;

namespace Gimnasy.Core.Particles;

/// <summary>One live particle (struct kept in a pooled array for cache-friendly stepping).</summary>
public struct Particle
{
    public bool Alive;
    public Vector3 Position;
    public Vector3 Velocity;
    public float Age;
    public float Lifetime;
    public float StartSize;
    public float Size;
    public float Rotation;          // degrees
    public Color StartColor;
    public Color Color;
    public uint Seed;               // stable per-particle random
}

/// <summary>
/// CPU reference simulator that steps a <see cref="ParticleSystemDef"/> through
/// all of its enabled modules. It runs headless (no GPU) so VFX behaviour is
/// fully testable; a GPU backend would mirror this maths in a compute shader.
/// </summary>
public sealed class ParticleSimulator
{
    private readonly ParticleSystemDef _def;
    private readonly Particle[] _pool;
    private readonly Random _rng;
    private float _emitAccumulator;
    private readonly bool[] _burstFired;

    public float Time { get; private set; }
    public int AliveCount { get; private set; }
    public IReadOnlyList<Particle> Particles => _pool;

    public ParticleSimulator(ParticleSystemDef def, int seed = 12345)
    {
        _def = def;
        _pool = new Particle[def.Main.MaxParticles];
        _rng = new Random(seed);
        _burstFired = new bool[def.Emission.Bursts.Count];
    }

    private float Rng01() => (float)_rng.NextDouble();
    private static float Hash01(uint s)
    {
        s ^= 2747636419u; s *= 2654435769u; s ^= s >> 16; s *= 2654435769u; s ^= s >> 16;
        return (s & 0xFFFFFF) / (float)0x1000000;
    }

    /// <summary>Advance the simulation by <paramref name="dt"/> seconds.</summary>
    public void Step(float dt)
    {
        dt *= _def.Main.SimulationSpeed;
        Time += dt;
        if (_def.Main.Looping && Time > _def.Main.Duration)
        {
            Time %= _def.Main.Duration;
            Array.Clear(_burstFired, 0, _burstFired.Length);
        }

        Emit(dt);
        Integrate(dt);
    }

    private void Emit(float dt)
    {
        if (!_def.Emission.Enabled) return;

        float rate = _def.Emission.RateOverTime.Evaluate(Time / _def.Main.Duration, Rng01());
        _emitAccumulator += rate * dt;
        while (_emitAccumulator >= 1f) { Spawn(); _emitAccumulator -= 1f; }

        for (int b = 0; b < _def.Emission.Bursts.Count; b++)
        {
            var burst = _def.Emission.Bursts[b];
            if (_burstFired[b] || Time < burst.Time) continue;
            if (Rng01() <= burst.Probability)
            {
                int count = (int)burst.Count.Evaluate(0f, Rng01());
                for (int i = 0; i < count; i++) Spawn();
            }
            _burstFired[b] = true;
        }
    }

    private void Spawn()
    {
        for (int i = 0; i < _pool.Length; i++)
        {
            if (_pool[i].Alive) continue;
            ref Particle p = ref _pool[i];
            p.Alive = true;
            p.Age = 0;
            p.Seed = (uint)_rng.Next();
            p.Lifetime = Mathf.Max(0.01f, _def.Main.StartLifetime.Evaluate(0f, Rng01()));
            float speed = _def.Main.StartSpeed.Evaluate(0f, Rng01());
            var (pos, dir) = SampleShape();
            p.Position = pos;
            p.Velocity = dir * speed;
            p.StartSize = _def.Main.StartSize.Evaluate(0f, Rng01());
            p.Size = p.StartSize;
            p.Rotation = _def.Main.StartRotation.Evaluate(0f, Rng01());
            p.StartColor = _def.Main.StartColor.Evaluate(Rng01());
            p.Color = p.StartColor;
            return;
        }
    }

    private (Vector3 pos, Vector3 dir) SampleShape()
    {
        var s = _def.Shape;
        if (!s.Enabled) return (Vector3.Zero, Vector3.Up);

        switch (s.Shape)
        {
            case EmitterShape.Sphere:
            {
                Vector3 d = RandomUnitVector();
                float r = s.Radius * Mathf.Lerp(1f - s.RadiusThickness, 1f, Rng01());
                return (s.Position + d * r, d);
            }
            case EmitterShape.Hemisphere:
            {
                Vector3 d = RandomUnitVector(); d = new Vector3(d.X, Mathf.Abs(d.Y), d.Z);
                return (s.Position + d * s.Radius, d);
            }
            case EmitterShape.Box:
            {
                Vector3 half = s.Scale * 0.5f;
                var pos = new Vector3((Rng01() * 2 - 1) * half.X, (Rng01() * 2 - 1) * half.Y, (Rng01() * 2 - 1) * half.Z);
                return (s.Position + pos, Vector3.Up);
            }
            case EmitterShape.Circle:
            {
                float a = Mathf.DegToRad(s.ArcDegrees) * Rng01();
                var d = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                return (s.Position + d * s.Radius, d);
            }
            case EmitterShape.Cone:
            default:
            {
                float a = Mathf.DegToRad(s.AngleDegrees);
                float theta = Mathf.DegToRad(s.ArcDegrees) * Rng01();
                float rr = s.Radius * Mathf.Sqrt(Rng01());
                var basePos = new Vector3(Mathf.Cos(theta) * rr, 0, Mathf.Sin(theta) * rr);
                var dir = new Vector3(Mathf.Cos(theta) * Mathf.Sin(a), Mathf.Cos(a), Mathf.Sin(theta) * Mathf.Sin(a)).Normalized;
                dir = dir.Lerp(RandomUnitVector(), s.RandomizeDirection).Normalized;
                return (s.Position + basePos, dir);
            }
        }
    }

    private Vector3 RandomUnitVector()
    {
        float z = Rng01() * 2f - 1f;
        float a = Rng01() * Mathf.Tau;
        float r = Mathf.Sqrt(Mathf.Max(0f, 1f - z * z));
        return new Vector3(r * Mathf.Cos(a), z, r * Mathf.Sin(a));
    }

    private void Integrate(float dt)
    {
        int alive = 0;
        for (int i = 0; i < _pool.Length; i++)
        {
            ref Particle p = ref _pool[i];
            if (!p.Alive) continue;

            p.Age += dt;
            if (p.Age >= p.Lifetime) { p.Alive = false; continue; }
            float t = p.Age / p.Lifetime;
            float roll = Hash01(p.Seed);

            // Gravity.
            float gravity = _def.Main.GravityModifier.Evaluate(t, roll);
            p.Velocity += new Vector3(0, -9.81f, 0) * gravity * dt;

            // Velocity over lifetime.
            if (_def.VelocityOverLifetime.Enabled)
            {
                var v = _def.VelocityOverLifetime;
                p.Velocity += new Vector3(v.X.Evaluate(t, roll), v.Y.Evaluate(t, roll), v.Z.Evaluate(t, roll)) * dt;
                p.Velocity *= v.SpeedModifier.Evaluate(t, roll);
            }

            // Constant force.
            if (_def.Force.Enabled)
            {
                var f = _def.Force;
                p.Velocity += new Vector3(f.X.Evaluate(t, roll), f.Y.Evaluate(t, roll), f.Z.Evaluate(t, roll)) * dt;
            }

            // Turbulence noise.
            if (_def.Noise.Enabled)
            {
                float strength = _def.Noise.Strength.Evaluate(t, roll);
                Vector3 n = CurlNoise(p.Position * _def.Noise.Frequency + _def.Noise.ScrollSpeed * Time);
                p.Velocity += n * strength * dt;
            }

            // Velocity limiting / drag.
            if (_def.LimitVelocity.Enabled)
            {
                float limit = _def.LimitVelocity.Speed.Evaluate(t, roll);
                float spd = p.Velocity.Length;
                if (spd > limit && spd > Mathf.Epsilon)
                    p.Velocity *= Mathf.Lerp(1f, limit / spd, _def.LimitVelocity.Dampen);
                p.Velocity *= Mathf.Max(0f, 1f - _def.LimitVelocity.Drag * dt);
            }

            p.Position += p.Velocity * dt;

            // Plane collisions (headless approximation).
            if (_def.Collision.Enabled && _def.Collision.Type == CollisionType.Planes)
                foreach (float planeY in _def.Collision.PlaneHeights)
                    if (p.Position.Y < planeY && p.Velocity.Y < 0)
                    {
                        p.Position = new Vector3(p.Position.X, planeY, p.Position.Z);
                        p.Velocity = new Vector3(p.Velocity.X, -p.Velocity.Y * _def.Collision.Bounce, p.Velocity.Z) * (1f - _def.Collision.Dampen);
                        p.Age += _def.Collision.LifetimeLoss * p.Lifetime;
                    }

            // Rotation.
            if (_def.RotationOverLifetime.Enabled)
                p.Rotation += _def.RotationOverLifetime.AngularVelocity.Evaluate(t, roll) * dt;

            // Size over lifetime.
            p.Size = _def.SizeOverLifetime.Enabled
                ? p.StartSize * _def.SizeOverLifetime.Size.Evaluate(t, roll)
                : p.StartSize;

            // Colour over lifetime.
            p.Color = _def.ColorOverLifetime.Enabled
                ? p.StartColor * _def.ColorOverLifetime.Gradient.Evaluate(t)
                : p.StartColor;

            alive++;
        }
        AliveCount = alive;
    }

    /// <summary>Cheap divergence-free-ish curl noise from gradient samples.</summary>
    private static Vector3 CurlNoise(Vector3 p)
    {
        const float e = 0.1f;
        float n1 = Snoise(p.Y + 31.4f, p.Z), n2 = Snoise(p.Y - e + 31.4f, p.Z);
        float x = (n1 - n2) / e;
        float n3 = Snoise(p.Z + 7.1f, p.X), n4 = Snoise(p.Z - e + 7.1f, p.X);
        float y = (n3 - n4) / e;
        float n5 = Snoise(p.X + 11.7f, p.Y), n6 = Snoise(p.X - e + 11.7f, p.Y);
        float z = (n5 - n6) / e;
        return new Vector3(x, y, z).Normalized;
    }

    private static float Snoise(float x, float y) =>
        Mathf.Sin(x * 12.9898f + y * 78.233f) * 0.5f;
}
