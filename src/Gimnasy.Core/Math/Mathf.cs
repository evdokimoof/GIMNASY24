namespace Gimnasy.Core.Math;

/// <summary>
/// Scalar math helpers used across the engine. Mirrors the most common
/// helpers found in mature engines (clamp, lerp, smoothstep, angle wrap).
/// </summary>
public static class Mathf
{
    public const float Pi = 3.1415927f;
    public const float Tau = 6.2831855f;
    public const float Epsilon = 1e-6f;
    public const float Deg2Rad = Pi / 180f;
    public const float Rad2Deg = 180f / Pi;

    public static float Abs(float v) => System.MathF.Abs(v);
    public static float Sqrt(float v) => System.MathF.Sqrt(v);
    public static float Sin(float v) => System.MathF.Sin(v);
    public static float Cos(float v) => System.MathF.Cos(v);
    public static float Tan(float v) => System.MathF.Tan(v);
    public static float Atan2(float y, float x) => System.MathF.Atan2(y, x);
    public static float Acos(float v) => System.MathF.Acos(Clamp(v, -1f, 1f));
    public static float Pow(float a, float b) => System.MathF.Pow(a, b);
    public static float Floor(float v) => System.MathF.Floor(v);
    public static float Ceil(float v) => System.MathF.Ceiling(v);
    public static float Round(float v) => System.MathF.Round(v);
    public static float Sign(float v) => v > 0f ? 1f : (v < 0f ? -1f : 0f);

    public static float Min(float a, float b) => a < b ? a : b;
    public static float Max(float a, float b) => a > b ? a : b;

    public static float Clamp(float v, float min, float max) => v < min ? min : (v > max ? max : v);
    public static int Clamp(int v, int min, int max) => v < min ? min : (v > max ? max : v);
    public static float Clamp01(float v) => Clamp(v, 0f, 1f);

    public static float Lerp(float a, float b, float t) => a + (b - a) * t;
    public static float InverseLerp(float a, float b, float v) =>
        Approximately(a, b) ? 0f : Clamp01((v - a) / (b - a));

    public static float MoveToward(float from, float to, float delta)
    {
        if (Abs(to - from) <= delta) return to;
        return from + Sign(to - from) * delta;
    }

    public static float SmoothStep(float from, float to, float t)
    {
        t = Clamp01(InverseLerp(from, to, t));
        return t * t * (3f - 2f * t);
    }

    public static float DegToRad(float deg) => deg * Deg2Rad;
    public static float RadToDeg(float rad) => rad * Rad2Deg;

    /// <summary>Wrap an angle into the [-PI, PI] range.</summary>
    public static float WrapAngle(float radians)
    {
        radians = radians % Tau;
        if (radians < -Pi) radians += Tau;
        else if (radians > Pi) radians -= Tau;
        return radians;
    }

    public static float LerpAngle(float a, float b, float t)
    {
        float delta = WrapAngle(b - a);
        return a + delta * Clamp01(t);
    }

    public static bool Approximately(float a, float b, float tolerance = Epsilon) =>
        Abs(a - b) <= tolerance * Max(1f, Max(Abs(a), Abs(b)));
}
