namespace Gimnasy.Core.Math;

/// <summary>2D vector of single-precision floats.</summary>
public readonly struct Vector2 : IEquatable<Vector2>
{
    public readonly float X;
    public readonly float Y;

    public Vector2(float x, float y) { X = x; Y = y; }
    public Vector2(float v) { X = v; Y = v; }

    public static readonly Vector2 Zero = new(0, 0);
    public static readonly Vector2 One = new(1, 1);
    public static readonly Vector2 Up = new(0, -1);     // screen space: up is -Y
    public static readonly Vector2 Down = new(0, 1);
    public static readonly Vector2 Left = new(-1, 0);
    public static readonly Vector2 Right = new(1, 0);

    public float LengthSquared => X * X + Y * Y;
    public float Length => Mathf.Sqrt(LengthSquared);
    public float Angle => Mathf.Atan2(Y, X);
    public Vector2 Normalized => Length is var l && l > Mathf.Epsilon ? this / l : Zero;
    public Vector2 Abs => new(Mathf.Abs(X), Mathf.Abs(Y));
    public Vector2 Orthogonal => new(-Y, X);

    public float Dot(Vector2 b) => X * b.X + Y * b.Y;
    public float Cross(Vector2 b) => X * b.Y - Y * b.X;
    public float DistanceTo(Vector2 b) => (b - this).Length;
    public float DistanceSquaredTo(Vector2 b) => (b - this).LengthSquared;
    public float AngleTo(Vector2 b) => Mathf.Atan2(Cross(b), Dot(b));

    public Vector2 Lerp(Vector2 b, float t) => new(Mathf.Lerp(X, b.X, t), Mathf.Lerp(Y, b.Y, t));
    public Vector2 Rotated(float radians)
    {
        float c = Mathf.Cos(radians), s = Mathf.Sin(radians);
        return new(X * c - Y * s, X * s + Y * c);
    }
    public Vector2 MoveToward(Vector2 to, float delta)
    {
        Vector2 d = to - this; float len = d.Length;
        return len <= delta || len < Mathf.Epsilon ? to : this + d / len * delta;
    }

    public static Vector2 operator +(Vector2 a, Vector2 b) => new(a.X + b.X, a.Y + b.Y);
    public static Vector2 operator -(Vector2 a, Vector2 b) => new(a.X - b.X, a.Y - b.Y);
    public static Vector2 operator -(Vector2 a) => new(-a.X, -a.Y);
    public static Vector2 operator *(Vector2 a, float s) => new(a.X * s, a.Y * s);
    public static Vector2 operator *(float s, Vector2 a) => a * s;
    public static Vector2 operator *(Vector2 a, Vector2 b) => new(a.X * b.X, a.Y * b.Y);
    public static Vector2 operator /(Vector2 a, float s) => new(a.X / s, a.Y / s);

    public bool Equals(Vector2 o) => Mathf.Approximately(X, o.X) && Mathf.Approximately(Y, o.Y);
    public override bool Equals(object? o) => o is Vector2 v && Equals(v);
    public override int GetHashCode() => HashCode.Combine(X, Y);
    public override string ToString() => $"({X}, {Y})";
}
