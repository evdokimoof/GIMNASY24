using Gimnasy.Core.Math;

namespace Gimnasy.Core.Particles;

/// <summary>A single control point of an <see cref="AnimationCurve"/> with
/// Hermite tangents for smooth interpolation.</summary>
public struct Keyframe
{
    public float Time;
    public float Value;
    public float InTangent;
    public float OutTangent;

    public Keyframe(float time, float value, float inTangent = 0f, float outTangent = 0f)
    {
        Time = time; Value = value; InTangent = inTangent; OutTangent = outTangent;
    }
}

/// <summary>
/// A piecewise cubic-Hermite curve over normalised time [0,1]. Used everywhere
/// a value changes over a particle's lifetime (size, alpha, force, rotation…).
/// </summary>
public sealed class AnimationCurve
{
    public List<Keyframe> Keys { get; } = new();

    public static AnimationCurve Constant(float value) =>
        new AnimationCurve().With(new Keyframe(0, value)).With(new Keyframe(1, value));

    public static AnimationCurve Linear(float from, float to) =>
        new AnimationCurve().With(new Keyframe(0, from, to - from, to - from))
                            .With(new Keyframe(1, to, to - from, to - from));

    public static AnimationCurve EaseInOut(float from, float to) =>
        new AnimationCurve().With(new Keyframe(0, from)).With(new Keyframe(1, to));

    public AnimationCurve With(Keyframe k) { AddKey(k); return this; }

    public void AddKey(Keyframe k)
    {
        int i = Keys.FindIndex(e => e.Time > k.Time);
        if (i < 0) Keys.Add(k); else Keys.Insert(i, k);
    }

    public float Evaluate(float t)
    {
        if (Keys.Count == 0) return 0f;
        if (Keys.Count == 1 || t <= Keys[0].Time) return Keys[0].Value;
        if (t >= Keys[^1].Time) return Keys[^1].Value;

        int i = 0;
        while (i < Keys.Count - 1 && Keys[i + 1].Time < t) i++;
        Keyframe a = Keys[i], b = Keys[i + 1];
        float dt = b.Time - a.Time;
        if (dt <= Mathf.Epsilon) return b.Value;
        float u = (t - a.Time) / dt;

        // Cubic Hermite spline.
        float u2 = u * u, u3 = u2 * u;
        float h00 = 2 * u3 - 3 * u2 + 1;
        float h10 = u3 - 2 * u2 + u;
        float h01 = -2 * u3 + 3 * u2;
        float h11 = u3 - u2;
        return h00 * a.Value + h10 * dt * a.OutTangent + h01 * b.Value + h11 * dt * b.InTangent;
    }
}

/// <summary>How a scalar particle parameter is sourced.</summary>
public enum MinMaxMode { Constant, Curve, RandomBetweenConstants, RandomBetweenCurves }

/// <summary>A scalar that may be a constant, a curve over lifetime, or a random
/// range — the "MinMaxCurve" concept from Unity's particle system.</summary>
public sealed class MinMaxCurve
{
    public MinMaxMode Mode { get; set; } = MinMaxMode.Constant;
    public float ConstantMin { get; set; }
    public float ConstantMax { get; set; }
    public AnimationCurve CurveMin { get; set; } = AnimationCurve.Constant(0);
    public AnimationCurve CurveMax { get; set; } = AnimationCurve.Constant(1);
    public float Multiplier { get; set; } = 1f;

    public static MinMaxCurve Const(float v) => new() { Mode = MinMaxMode.Constant, ConstantMax = v };
    public static MinMaxCurve Range(float a, float b) =>
        new() { Mode = MinMaxMode.RandomBetweenConstants, ConstantMin = a, ConstantMax = b };

    /// <summary><paramref name="t"/> = normalised lifetime, <paramref name="rng"/> = stable 0..1 roll.</summary>
    public float Evaluate(float t, float rng) => Mode switch
    {
        MinMaxMode.Constant => ConstantMax * Multiplier,
        MinMaxMode.Curve => CurveMax.Evaluate(t) * Multiplier,
        MinMaxMode.RandomBetweenConstants => Mathf.Lerp(ConstantMin, ConstantMax, rng) * Multiplier,
        MinMaxMode.RandomBetweenCurves => Mathf.Lerp(CurveMin.Evaluate(t), CurveMax.Evaluate(t), rng) * Multiplier,
        _ => 0f,
    };
}

public struct GradientColorKey { public Color Color; public float Time;
    public GradientColorKey(Color c, float t) { Color = c; Time = t; } }
public struct GradientAlphaKey { public float Alpha; public float Time;
    public GradientAlphaKey(float a, float t) { Alpha = a; Time = t; } }

/// <summary>A colour gradient over normalised time with independent colour and
/// alpha keys — used by Color-over-Lifetime and start-colour.</summary>
public sealed class ColorGradient
{
    public List<GradientColorKey> ColorKeys { get; } = new();
    public List<GradientAlphaKey> AlphaKeys { get; } = new();

    public static ColorGradient TwoColor(Color a, Color b)
    {
        var g = new ColorGradient();
        g.ColorKeys.Add(new GradientColorKey(a, 0)); g.ColorKeys.Add(new GradientColorKey(b, 1));
        g.AlphaKeys.Add(new GradientAlphaKey(a.A, 0)); g.AlphaKeys.Add(new GradientAlphaKey(b.A, 1));
        return g;
    }

    public Color Evaluate(float t)
    {
        Color rgb = SampleColor(t);
        float alpha = SampleAlpha(t);
        return new Color(rgb.R, rgb.G, rgb.B, alpha);
    }

    private Color SampleColor(float t)
    {
        if (ColorKeys.Count == 0) return Color.White;
        if (t <= ColorKeys[0].Time) return ColorKeys[0].Color;
        for (int i = 0; i < ColorKeys.Count - 1; i++)
        {
            if (t > ColorKeys[i + 1].Time) continue;
            float span = ColorKeys[i + 1].Time - ColorKeys[i].Time;
            float u = span <= Mathf.Epsilon ? 0f : (t - ColorKeys[i].Time) / span;
            return ColorKeys[i].Color.Lerp(ColorKeys[i + 1].Color, u);
        }
        return ColorKeys[^1].Color;
    }

    private float SampleAlpha(float t)
    {
        if (AlphaKeys.Count == 0) return 1f;
        if (t <= AlphaKeys[0].Time) return AlphaKeys[0].Alpha;
        for (int i = 0; i < AlphaKeys.Count - 1; i++)
        {
            if (t > AlphaKeys[i + 1].Time) continue;
            float span = AlphaKeys[i + 1].Time - AlphaKeys[i].Time;
            float u = span <= Mathf.Epsilon ? 0f : (t - AlphaKeys[i].Time) / span;
            return Mathf.Lerp(AlphaKeys[i].Alpha, AlphaKeys[i + 1].Alpha, u);
        }
        return AlphaKeys[^1].Alpha;
    }
}
