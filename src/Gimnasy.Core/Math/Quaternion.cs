namespace Gimnasy.Core.Math;

/// <summary>Unit quaternion used for 3D rotations (avoids gimbal lock).</summary>
public readonly struct Quaternion : IEquatable<Quaternion>
{
    public readonly float X, Y, Z, W;

    public Quaternion(float x, float y, float z, float w) { X = x; Y = y; Z = z; W = w; }

    public static readonly Quaternion Identity = new(0, 0, 0, 1);

    public float LengthSquared => X * X + Y * Y + Z * Z + W * W;
    public float Length => Mathf.Sqrt(LengthSquared);

    public Quaternion Normalized
    {
        get
        {
            float l = Length;
            return l < Mathf.Epsilon ? Identity : new Quaternion(X / l, Y / l, Z / l, W / l);
        }
    }

    public Quaternion Conjugate => new(-X, -Y, -Z, W);
    public Quaternion Inverse => Conjugate.Scaled(1f / LengthSquared);
    private Quaternion Scaled(float s) => new(X * s, Y * s, Z * s, W * s);

    public static Quaternion FromAxisAngle(Vector3 axis, float radians)
    {
        Vector3 n = axis.Normalized;
        float half = radians * 0.5f;
        float s = Mathf.Sin(half);
        return new Quaternion(n.X * s, n.Y * s, n.Z * s, Mathf.Cos(half));
    }

    /// <summary>Build from Euler angles (radians) in YXZ order (yaw, pitch, roll).</summary>
    public static Quaternion FromEuler(Vector3 euler)
    {
        float cy = Mathf.Cos(euler.Y * 0.5f), sy = Mathf.Sin(euler.Y * 0.5f);
        float cx = Mathf.Cos(euler.X * 0.5f), sx = Mathf.Sin(euler.X * 0.5f);
        float cz = Mathf.Cos(euler.Z * 0.5f), sz = Mathf.Sin(euler.Z * 0.5f);
        // q = Qy * Qx * Qz
        return (new Quaternion(0, sy, 0, cy)
              * new Quaternion(sx, 0, 0, cx)
              * new Quaternion(0, 0, sz, cz)).Normalized;
    }

    public Vector3 ToEuler()
    {
        float sinp = 2f * (W * X - Y * Z);
        float pitch = Mathf.Abs(sinp) >= 1f ? Mathf.Sign(sinp) * (Mathf.Pi / 2f) : System.MathF.Asin(sinp);
        float yaw = Mathf.Atan2(2f * (W * Y + Z * X), 1f - 2f * (X * X + Y * Y));
        float roll = Mathf.Atan2(2f * (W * Z + X * Y), 1f - 2f * (Z * Z + X * X));
        return new Vector3(pitch, yaw, roll);
    }

    public Vector3 Rotate(Vector3 v)
    {
        Vector3 u = new(X, Y, Z);
        return 2f * u.Dot(v) * u + (W * W - u.Dot(u)) * v + 2f * W * u.Cross(v);
    }

    public float Dot(Quaternion b) => X * b.X + Y * b.Y + Z * b.Z + W * b.W;

    public Quaternion Slerp(Quaternion to, float t)
    {
        float dot = Dot(to);
        Quaternion target = to;
        if (dot < 0f) { target = new Quaternion(-to.X, -to.Y, -to.Z, -to.W); dot = -dot; }
        if (dot > 0.9995f)
            return new Quaternion(
                Mathf.Lerp(X, target.X, t), Mathf.Lerp(Y, target.Y, t),
                Mathf.Lerp(Z, target.Z, t), Mathf.Lerp(W, target.W, t)).Normalized;
        float theta0 = Mathf.Acos(dot);
        float theta = theta0 * t;
        float sinTheta = Mathf.Sin(theta), sinTheta0 = Mathf.Sin(theta0);
        float s0 = Mathf.Cos(theta) - dot * sinTheta / sinTheta0;
        float s1 = sinTheta / sinTheta0;
        return new Quaternion(
            X * s0 + target.X * s1, Y * s0 + target.Y * s1,
            Z * s0 + target.Z * s1, W * s0 + target.W * s1);
    }

    public static Quaternion operator *(Quaternion a, Quaternion b) => new(
        a.W * b.X + a.X * b.W + a.Y * b.Z - a.Z * b.Y,
        a.W * b.Y - a.X * b.Z + a.Y * b.W + a.Z * b.X,
        a.W * b.Z + a.X * b.Y - a.Y * b.X + a.Z * b.W,
        a.W * b.W - a.X * b.X - a.Y * b.Y - a.Z * b.Z);

    public bool Equals(Quaternion o) => Mathf.Approximately(X, o.X) && Mathf.Approximately(Y, o.Y)
        && Mathf.Approximately(Z, o.Z) && Mathf.Approximately(W, o.W);
    public override bool Equals(object? o) => o is Quaternion q && Equals(q);
    public override int GetHashCode() => HashCode.Combine(X, Y, Z, W);
    public override string ToString() => $"({X}, {Y}, {Z}, {W})";
}
