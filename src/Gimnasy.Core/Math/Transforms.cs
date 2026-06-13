namespace Gimnasy.Core.Math;

/// <summary>2D affine transform (2x3 matrix): two basis columns + origin.</summary>
public readonly struct Transform2D
{
    public readonly Vector2 X;      // basis column 0
    public readonly Vector2 Y;      // basis column 1
    public readonly Vector2 Origin;

    public Transform2D(Vector2 x, Vector2 y, Vector2 origin) { X = x; Y = y; Origin = origin; }

    public static readonly Transform2D Identity = new(new Vector2(1, 0), new Vector2(0, 1), Vector2.Zero);

    public static Transform2D FromTRS(Vector2 position, float rotation, Vector2 scale)
    {
        float c = Mathf.Cos(rotation), s = Mathf.Sin(rotation);
        Vector2 x = new Vector2(c, s) * scale.X;
        Vector2 y = new Vector2(-s, c) * scale.Y;
        return new Transform2D(x, y, position);
    }

    public float Rotation => Mathf.Atan2(X.Y, X.X);
    public Vector2 Scale => new(X.Length, Y.Length);

    public Vector2 BasisXform(Vector2 v) => X * v.X + Y * v.Y;
    public Vector2 Xform(Vector2 v) => BasisXform(v) + Origin;

    public static Transform2D operator *(Transform2D a, Transform2D b) => new(
        a.BasisXform(b.X), a.BasisXform(b.Y), a.Xform(b.Origin));

    public override string ToString() => $"[X{X}, Y{Y}, O{Origin}]";
}

/// <summary>3x3 rotation/scale matrix (the rotational part of a 3D transform).</summary>
public readonly struct Basis
{
    public readonly Vector3 X, Y, Z; // columns

    public Basis(Vector3 x, Vector3 y, Vector3 z) { X = x; Y = y; Z = z; }

    public static readonly Basis Identity = new(Vector3.Right, Vector3.Up, Vector3.Back);

    public static Basis FromQuaternion(Quaternion q)
    {
        q = q.Normalized;
        float xx = q.X * q.X, yy = q.Y * q.Y, zz = q.Z * q.Z;
        float xy = q.X * q.Y, xz = q.X * q.Z, yz = q.Y * q.Z;
        float wx = q.W * q.X, wy = q.W * q.Y, wz = q.W * q.Z;
        return new Basis(
            new Vector3(1 - 2 * (yy + zz), 2 * (xy + wz), 2 * (xz - wy)),
            new Vector3(2 * (xy - wz), 1 - 2 * (xx + zz), 2 * (yz + wx)),
            new Vector3(2 * (xz + wy), 2 * (yz - wx), 1 - 2 * (xx + yy)));
    }

    public static Basis Scaled(Vector3 scale) =>
        new(Vector3.Right * scale.X, Vector3.Up * scale.Y, Vector3.Back * scale.Z);

    public Vector3 Xform(Vector3 v) => X * v.X + Y * v.Y + Z * v.Z;

    public static Basis operator *(Basis a, Basis b) =>
        new(a.Xform(b.X), a.Xform(b.Y), a.Xform(b.Z));
}

/// <summary>3D affine transform: a Basis plus an origin.</summary>
public readonly struct Transform3D
{
    public readonly Basis Basis;
    public readonly Vector3 Origin;

    public Transform3D(Basis basis, Vector3 origin) { Basis = basis; Origin = origin; }

    public static readonly Transform3D Identity = new(Basis.Identity, Vector3.Zero);

    public static Transform3D FromTRS(Vector3 position, Quaternion rotation, Vector3 scale)
    {
        Basis b = Basis.FromQuaternion(rotation) * Basis.Scaled(scale);
        return new Transform3D(b, position);
    }

    public Vector3 Xform(Vector3 v) => Basis.Xform(v) + Origin;
    public Vector3 BasisXform(Vector3 v) => Basis.Xform(v);

    public static Transform3D operator *(Transform3D a, Transform3D b) =>
        new(a.Basis * b.Basis, a.Xform(b.Origin));

    public override string ToString() => $"[Basis(...), O{Origin}]";
}
