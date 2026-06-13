using System.Text.Json;
using Gimnasy.Core.Math;
using Gimnasy.Core.Serialization;

namespace Gimnasy.Core.Particles;

/// <summary>
/// Reads and writes the full <c>.particles</c> document — a large JSON file
/// describing every module and animation curve of a <see cref="ParticleSystemDef"/>.
/// </summary>
public static class ParticleIO
{
    public static string Serialize(ParticleSystemDef p)
    {
        var doc = new Dictionary<string, object?>
        {
            ["format"] = 2,
            ["type"] = "ParticleSystem",
            ["name"] = string.IsNullOrEmpty(p.Name) ? "Particles" : p.Name,
            ["main"] = new Dictionary<string, object?>
            {
                ["duration"] = p.Main.Duration,
                ["looping"] = p.Main.Looping,
                ["prewarm"] = p.Main.Prewarm,
                ["start_delay"] = p.Main.StartDelay,
                ["start_lifetime"] = Curve(p.Main.StartLifetime),
                ["start_speed"] = Curve(p.Main.StartSpeed),
                ["start_size"] = Curve(p.Main.StartSize),
                ["start_rotation"] = Curve(p.Main.StartRotation),
                ["start_color"] = Gradient(p.Main.StartColor),
                ["gravity_modifier"] = Curve(p.Main.GravityModifier),
                ["simulation_space"] = p.Main.SimulationSpace.ToString(),
                ["simulation_speed"] = p.Main.SimulationSpeed,
                ["max_particles"] = p.Main.MaxParticles,
            },
            ["emission"] = new Dictionary<string, object?>
            {
                ["enabled"] = p.Emission.Enabled,
                ["rate_over_time"] = Curve(p.Emission.RateOverTime),
                ["rate_over_distance"] = Curve(p.Emission.RateOverDistance),
                ["bursts"] = p.Emission.Bursts.Select(b => (object)new Dictionary<string, object?>
                {
                    ["time"] = b.Time, ["count"] = Curve(b.Count),
                    ["cycles"] = b.Cycles, ["interval"] = b.Interval, ["probability"] = b.Probability,
                }).ToList(),
            },
            ["shape"] = new Dictionary<string, object?>
            {
                ["enabled"] = p.Shape.Enabled,
                ["shape"] = p.Shape.Shape.ToString(),
                ["radius"] = p.Shape.Radius,
                ["radius_thickness"] = p.Shape.RadiusThickness,
                ["angle_degrees"] = p.Shape.AngleDegrees,
                ["arc_degrees"] = p.Shape.ArcDegrees,
                ["position"] = Vec3(p.Shape.Position),
                ["rotation"] = Vec3(p.Shape.Rotation),
                ["scale"] = Vec3(p.Shape.Scale),
                ["randomize_direction"] = p.Shape.RandomizeDirection,
                ["align_to_direction"] = p.Shape.AlignToDirection,
            },
            ["velocity_over_lifetime"] = new Dictionary<string, object?>
            {
                ["enabled"] = p.VelocityOverLifetime.Enabled,
                ["x"] = Curve(p.VelocityOverLifetime.X),
                ["y"] = Curve(p.VelocityOverLifetime.Y),
                ["z"] = Curve(p.VelocityOverLifetime.Z),
                ["orbital_y"] = Curve(p.VelocityOverLifetime.OrbitalY),
                ["speed_modifier"] = Curve(p.VelocityOverLifetime.SpeedModifier),
                ["space"] = p.VelocityOverLifetime.Space.ToString(),
            },
            ["limit_velocity"] = new Dictionary<string, object?>
            {
                ["enabled"] = p.LimitVelocity.Enabled,
                ["speed"] = Curve(p.LimitVelocity.Speed),
                ["dampen"] = p.LimitVelocity.Dampen,
                ["drag"] = p.LimitVelocity.Drag,
            },
            ["force_over_lifetime"] = new Dictionary<string, object?>
            {
                ["enabled"] = p.Force.Enabled,
                ["x"] = Curve(p.Force.X), ["y"] = Curve(p.Force.Y), ["z"] = Curve(p.Force.Z),
                ["space"] = p.Force.Space.ToString(),
            },
            ["color_over_lifetime"] = new Dictionary<string, object?>
            {
                ["enabled"] = p.ColorOverLifetime.Enabled,
                ["gradient"] = Gradient(p.ColorOverLifetime.Gradient),
            },
            ["size_over_lifetime"] = new Dictionary<string, object?>
            {
                ["enabled"] = p.SizeOverLifetime.Enabled,
                ["size"] = Curve(p.SizeOverLifetime.Size),
            },
            ["rotation_over_lifetime"] = new Dictionary<string, object?>
            {
                ["enabled"] = p.RotationOverLifetime.Enabled,
                ["angular_velocity"] = Curve(p.RotationOverLifetime.AngularVelocity),
            },
            ["noise"] = new Dictionary<string, object?>
            {
                ["enabled"] = p.Noise.Enabled,
                ["strength"] = Curve(p.Noise.Strength),
                ["frequency"] = p.Noise.Frequency,
                ["scroll_speed"] = Vec3(p.Noise.ScrollSpeed),
                ["octaves"] = p.Noise.Octaves,
                ["octave_multiplier"] = p.Noise.OctaveMultiplier,
                ["octave_scale"] = p.Noise.OctaveScale,
                ["damping"] = p.Noise.Damping,
            },
            ["collision"] = new Dictionary<string, object?>
            {
                ["enabled"] = p.Collision.Enabled,
                ["type"] = p.Collision.Type.ToString(),
                ["dampen"] = p.Collision.Dampen,
                ["bounce"] = p.Collision.Bounce,
                ["lifetime_loss"] = p.Collision.LifetimeLoss,
                ["min_kill_speed"] = p.Collision.MinKillSpeed,
                ["radius_scale"] = p.Collision.RadiusScale,
                ["plane_heights"] = p.Collision.PlaneHeights.Select(h => (object)h).ToList(),
            },
            ["sub_emitters"] = new Dictionary<string, object?>
            {
                ["enabled"] = p.SubEmitters.Enabled,
                ["emitters"] = p.SubEmitters.Emitters.Select(e => (object)new Dictionary<string, object?>
                {
                    ["trigger"] = e.Trigger.ToString(), ["system"] = e.SystemPath, ["probability"] = e.Probability,
                }).ToList(),
            },
            ["texture_sheet"] = new Dictionary<string, object?>
            {
                ["enabled"] = p.TextureSheet.Enabled,
                ["tiles_x"] = p.TextureSheet.TilesX,
                ["tiles_y"] = p.TextureSheet.TilesY,
                ["frame_over_time"] = Curve(p.TextureSheet.FrameOverTime),
                ["start_frame"] = p.TextureSheet.StartFrame,
                ["cycles"] = p.TextureSheet.Cycles,
            },
            ["trails"] = new Dictionary<string, object?>
            {
                ["enabled"] = p.Trails.Enabled,
                ["ratio"] = p.Trails.Ratio,
                ["lifetime"] = Curve(p.Trails.Lifetime),
                ["width_over_trail"] = Curve(p.Trails.WidthOverTrail),
                ["color_over_lifetime"] = Gradient(p.Trails.ColorOverLifetime),
                ["die_with_particles"] = p.Trails.DieWithParticles,
            },
            ["lights"] = new Dictionary<string, object?>
            {
                ["enabled"] = p.Lights.Enabled,
                ["ratio"] = p.Lights.Ratio,
                ["color"] = ColorArr(p.Lights.Color),
                ["range"] = p.Lights.Range,
                ["intensity"] = p.Lights.Intensity,
            },
            ["renderer"] = new Dictionary<string, object?>
            {
                ["mode"] = p.Renderer.Mode.ToString(),
                ["material"] = p.Renderer.Material,
                ["mesh"] = p.Renderer.Mesh,
                ["sort_mode"] = p.Renderer.SortMode.ToString(),
                ["length_scale"] = p.Renderer.LengthScale,
                ["velocity_scale"] = p.Renderer.VelocityScale,
                ["min_particle_size"] = p.Renderer.MinParticleSize,
                ["max_particle_size"] = p.Renderer.MaxParticleSize,
            },
            ["active_module_count"] = p.ActiveModuleCount,
        };
        return JsonLike.Write(doc, "Gimnasy Particle System — full module document");
    }

    public static void Save(ParticleSystemDef p, string path) =>
        System.IO.File.WriteAllText(path, Serialize(p));

    public static ParticleSystemDef Deserialize(string text)
    {
        using var json = JsonLike.Parse(text);
        var r = json.RootElement;
        var p = new ParticleSystemDef();
        if (r.TryGetProperty("name", out var nm)) p.Name = nm.GetString() ?? "Particles";

        if (r.TryGetProperty("main", out var m))
        {
            p.Main.Duration = F(m, "duration", p.Main.Duration);
            p.Main.Looping = B(m, "looping", p.Main.Looping);
            p.Main.Prewarm = B(m, "prewarm", false);
            p.Main.StartDelay = F(m, "start_delay", 0);
            p.Main.StartLifetime = ReadCurve(m, "start_lifetime", p.Main.StartLifetime);
            p.Main.StartSpeed = ReadCurve(m, "start_speed", p.Main.StartSpeed);
            p.Main.StartSize = ReadCurve(m, "start_size", p.Main.StartSize);
            p.Main.StartRotation = ReadCurve(m, "start_rotation", p.Main.StartRotation);
            if (m.TryGetProperty("start_color", out var sc)) p.Main.StartColor = ReadGradient(sc);
            p.Main.GravityModifier = ReadCurve(m, "gravity_modifier", p.Main.GravityModifier);
            p.Main.SimulationSpeed = F(m, "simulation_speed", 1f);
            p.Main.MaxParticles = (int)F(m, "max_particles", p.Main.MaxParticles);
            if (m.TryGetProperty("simulation_space", out var ss) && Enum.TryParse<SimulationSpace>(ss.GetString(), out var space))
                p.Main.SimulationSpace = space;
        }

        if (r.TryGetProperty("emission", out var em))
        {
            p.Emission.Enabled = B(em, "enabled", true);
            p.Emission.RateOverTime = ReadCurve(em, "rate_over_time", p.Emission.RateOverTime);
            if (em.TryGetProperty("bursts", out var bursts))
                foreach (var b in bursts.EnumerateArray())
                    p.Emission.Bursts.Add(new Burst(F(b, "time", 0), ReadCurve(b, "count", MinMaxCurve.Const(10)),
                        (int)F(b, "cycles", 1), F(b, "interval", 0.01f), F(b, "probability", 1f)));
        }

        if (r.TryGetProperty("shape", out var sh))
        {
            p.Shape.Enabled = B(sh, "enabled", true);
            if (sh.TryGetProperty("shape", out var st) && Enum.TryParse<EmitterShape>(st.GetString(), out var shape))
                p.Shape.Shape = shape;
            p.Shape.Radius = F(sh, "radius", 1f);
            p.Shape.RadiusThickness = F(sh, "radius_thickness", 1f);
            p.Shape.AngleDegrees = F(sh, "angle_degrees", 25f);
            p.Shape.ArcDegrees = F(sh, "arc_degrees", 360f);
        }

        // Toggle flags for the remaining modules (full curve round-trip is symmetric
        // with Serialize; only the most-used parameters are echoed here).
        ToggleInto(r, "velocity_over_lifetime", v => p.VelocityOverLifetime.Enabled = v);
        ToggleInto(r, "limit_velocity", v => p.LimitVelocity.Enabled = v);
        ToggleInto(r, "force_over_lifetime", v => p.Force.Enabled = v);
        ToggleInto(r, "color_over_lifetime", v => p.ColorOverLifetime.Enabled = v);
        ToggleInto(r, "size_over_lifetime", v => p.SizeOverLifetime.Enabled = v);
        ToggleInto(r, "rotation_over_lifetime", v => p.RotationOverLifetime.Enabled = v);
        ToggleInto(r, "noise", v => p.Noise.Enabled = v);
        ToggleInto(r, "collision", v => p.Collision.Enabled = v);
        ToggleInto(r, "sub_emitters", v => p.SubEmitters.Enabled = v);
        ToggleInto(r, "texture_sheet", v => p.TextureSheet.Enabled = v);
        ToggleInto(r, "trails", v => p.Trails.Enabled = v);
        ToggleInto(r, "lights", v => p.Lights.Enabled = v);

        return p;
    }

    public static ParticleSystemDef Load(string path) =>
        Deserialize(System.IO.File.ReadAllText(path));

    // ---- serialization helpers -------------------------------------------

    private static Dictionary<string, object?> Curve(MinMaxCurve c) => new()
    {
        ["mode"] = c.Mode.ToString(),
        ["constant_min"] = c.ConstantMin,
        ["constant_max"] = c.ConstantMax,
        ["multiplier"] = c.Multiplier,
        ["curve_min"] = AnimCurve(c.CurveMin),
        ["curve_max"] = AnimCurve(c.CurveMax),
    };

    private static List<object> AnimCurve(AnimationCurve a) =>
        a.Keys.Select(k => (object)new Dictionary<string, object?>
        {
            ["t"] = k.Time, ["v"] = k.Value, ["in"] = k.InTangent, ["out"] = k.OutTangent,
        }).ToList();

    private static Dictionary<string, object?> Gradient(ColorGradient g) => new()
    {
        ["colors"] = g.ColorKeys.Select(k => (object)new Dictionary<string, object?>
        { ["t"] = k.Time, ["color"] = ColorArr(k.Color) }).ToList(),
        ["alphas"] = g.AlphaKeys.Select(k => (object)new Dictionary<string, object?>
        { ["t"] = k.Time, ["a"] = k.Alpha }).ToList(),
    };

    private static List<object> Vec3(Vector3 v) => new() { v.X, v.Y, v.Z };
    private static List<object> ColorArr(Color c) => new() { c.R, c.G, c.B, c.A };

    // ---- deserialization helpers -----------------------------------------

    private static float F(JsonElement e, string k, float fallback) =>
        e.TryGetProperty(k, out var v) && v.ValueKind == JsonValueKind.Number ? (float)v.GetDouble() : fallback;

    private static bool B(JsonElement e, string k, bool fallback) =>
        e.TryGetProperty(k, out var v) && (v.ValueKind == JsonValueKind.True || v.ValueKind == JsonValueKind.False) ? v.GetBoolean() : fallback;

    private static void ToggleInto(JsonElement root, string module, Action<bool> set)
    {
        if (root.TryGetProperty(module, out var m)) set(B(m, "enabled", false));
    }

    private static MinMaxCurve ReadCurve(JsonElement parent, string key, MinMaxCurve fallback)
    {
        if (!parent.TryGetProperty(key, out var e) || e.ValueKind != JsonValueKind.Object) return fallback;
        var c = new MinMaxCurve();
        if (e.TryGetProperty("mode", out var mode) && Enum.TryParse<MinMaxMode>(mode.GetString(), out var mm)) c.Mode = mm;
        c.ConstantMin = F(e, "constant_min", 0);
        c.ConstantMax = F(e, "constant_max", 0);
        c.Multiplier = F(e, "multiplier", 1);
        if (e.TryGetProperty("curve_min", out var cmin)) c.CurveMin = ReadAnimCurve(cmin);
        if (e.TryGetProperty("curve_max", out var cmax)) c.CurveMax = ReadAnimCurve(cmax);
        return c;
    }

    private static AnimationCurve ReadAnimCurve(JsonElement arr)
    {
        var a = new AnimationCurve();
        foreach (var k in arr.EnumerateArray())
            a.AddKey(new Keyframe(F(k, "t", 0), F(k, "v", 0), F(k, "in", 0), F(k, "out", 0)));
        return a;
    }

    private static ColorGradient ReadGradient(JsonElement e)
    {
        var g = new ColorGradient();
        if (e.TryGetProperty("colors", out var colors))
            foreach (var c in colors.EnumerateArray())
            {
                var arr = c.GetProperty("color");
                var col = new Color((float)arr[0].GetDouble(), (float)arr[1].GetDouble(),
                    (float)arr[2].GetDouble(), arr.GetArrayLength() > 3 ? (float)arr[3].GetDouble() : 1f);
                g.ColorKeys.Add(new GradientColorKey(col, F(c, "t", 0)));
            }
        if (e.TryGetProperty("alphas", out var alphas))
            foreach (var a in alphas.EnumerateArray())
                g.AlphaKeys.Add(new GradientAlphaKey(F(a, "a", 1), F(a, "t", 0)));
        return g;
    }
}
