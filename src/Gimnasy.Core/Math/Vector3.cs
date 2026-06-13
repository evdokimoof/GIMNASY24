namespace Gimnasy.Core.Math;

/// <summary>3D vector of single-precision floats. The engine uses a
/// right-handed, Y-up coordinate system (the convention adopted by most
/// modern 3D engines).</summary>
public readonly struct Vector3 : IEquatable<Vector3>
{
    public readonly float X;
    public readonly float Y;
    public readonly float Z;

    public Vector3(float x, float y, float z) { X = x; Y = y; Z = z; }
    public Vector3(float v) { X = v; Y = v; Z = v; }

    public static readonly Vector3 Zero = new(0, 0, 0);
    public static readonly Vector3 One = new(1, 1, 1);
    public static readonly Vector3 Up = new(0, 1, 0);
    public static readonly Vector3 Down = new(0, -1, 0);
    public static readonly Vector3 Left = new(-1, 0, 0);
    public static readonly Vector3 Right = new(1, 0, 0);
    public static readonly Vector3 Forward = new(0, 0, -1);
    public static readonly Vector3 Back = new(0, 0, 1);

    public float LengthSquared => X * X + Y * Y + Z * Z;
    public float Length => Mathf.Sqrt(LengthSquared);
    public Vector3 Normalized => Length is var l && l > Mathf.Epsilon ? this / l : Zero;
    public Vector3 Abs => new(Mathf.Abs(X), Mathf.Abs(Y), Mathf.Abs(Z));

    public float Dot(Vector3 b) => X * b.X + Y * b.Y + Z * b.Z;
    public Vector3 Cross(Vector3 b) => new(
        Y * b.Z - Z * b.Y,
        Z * b.X - X * b.Z,
        X * b.Y - Y * b.X);
    public float DistanceTo(Vector3 b) => (b - this).Length;
    public float DistanceSquaredTo(Vector3 b) => (b - this).LengthSquared;
    public float AngleTo(Vector3 b) => Mathf.Acos(Normalized.Dot(b.Normalized));

    public Vector3 Lerp(Vector3 b, float t) =>
        new(Mathf.Lerp(X, b.X, t), Mathf.Lerp(Y, b.Y, t), Mathf.Lerp(Z, b.Z, t));

    public Vector3 MoveToward(Vector3 to, float delta)
    {
        Vector3 d = to - this; float len = d.Length;
        return len <= delta || len < Mathf.Epsilon ? to : this + d / len * delta;
    }

    public Vector3 Reflect(Vector3 normal) => this - 2f * Dot(normal) * normal;
    public Vector3 Project(Vector3 onto) => onto * (Dot(onto) / onto.LengthSquared);
    public Vector3 Slide(Vector3 normal) => this - normal * Dot(normal);

    public static Vector3 operator +(Vector3 a, Vector3 b) => new(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
    public static Vector3 operator -(Vector3 a, Vector3 b) => new(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
    public static Vector3 operator -(Vector3 a) => new(-a.X, -a.Y, -a.Z);
    public static Vector3 operator *(Vector3 a, float s) => new(a.X * s, a.Y * s, a.Z * s);
    public static Vector3 operator *(float s, Vector3 a) => a * s;
    public static Vector3 operator *(Vector3 a, Vector3 b) => new(a.X * b.X, a.Y * b.Y, a.Z * b.Z);
    public static Vector3 operator /(Vector3 a, float s) => new(a.X / s, a.Y / s, a.Z / s);

    public bool Equals(Vector3 o) =>
        Mathf.Approximately(X, o.X) && Mathf.Approximately(Y, o.Y) && Mathf.Approximately(Z, o.Z);
    public override bool Equals(object? o) => o is Vector3 v && Equals(v);
    public override int GetHashCode() => HashCode.Combine(X, Y, Z);
    public override string ToString() => $"({X}, {Y}, {Z})";
}

/// <summary>4D vector — used for shader params, colors and homogeneous coords.</summary>
public readonly struct Vector4 : IEquatable<Vector4>
{
    public readonly float X, Y, Z, W;
    public Vector4(float x, float y, float z, float w) { X = x; Y = y; Z = z; W = w; }
    public static readonly Vector4 Zero = new(0, 0, 0, 0);
    public static readonly Vector4 One = new(1, 1, 1, 1);
    public float Dot(Vector4 b) => X * b.X + Y * b.Y + Z * b.Z + W * b.W;
    public static Vector4 operator +(Vector4 a, Vector4 b) => new(a.X + b.X, a.Y + b.Y, a.Z + b.Z, a.W + b.W);
    public static Vector4 operator *(Vector4 a, float s) => new(a.X * s, a.Y * s, a.Z * s, a.W * s);
    public bool Equals(Vector4 o) => Mathf.Approximately(X, o.X) && Mathf.Approximately(Y, o.Y)
        && Mathf.Approximately(Z, o.Z) && Mathf.Approximately(W, o.W);
    public override bool Equals(object? o) => o is Vector4 v && Equals(v);
    public override int GetHashCode() => HashCode.Combine(X, Y, Z, W);
    public override string ToString() => $"({X}, {Y}, {Z}, {W})";
}

/// <summary>Integer 2D vector (tile coords, pixel sizes, grid indices).</summary>
public readonly struct Vector2I : IEquatable<Vector2I>
{
    public readonly int X, Y;
    public Vector2I(int x, int y) { X = x; Y = y; }
    public static readonly Vector2I Zero = new(0, 0);
    public static readonly Vector2I One = new(1, 1);
    public Vector2 ToVector2() => new(X, Y);
    public static Vector2I operator +(Vector2I a, Vector2I b) => new(a.X + b.X, a.Y + b.Y);
    public static Vector2I operator -(Vector2I a, Vector2I b) => new(a.X - b.X, a.Y - b.Y);
    public bool Equals(Vector2I o) => X == o.X && Y == o.Y;
    public override bool Equals(object? o) => o is Vector2I v && Equals(v);
    public override int GetHashCode() => HashCode.Combine(X, Y);
    public override string ToString() => $"({X}, {Y})";
}
