namespace Gimnasy.Core.Math;

/// <summary>Axis-aligned 2D rectangle (position = top-left corner).</summary>
public readonly struct Rect2
{
    public readonly Vector2 Position;
    public readonly Vector2 Size;

    public Rect2(Vector2 position, Vector2 size) { Position = position; Size = size; }
    public Rect2(float x, float y, float w, float h) : this(new Vector2(x, y), new Vector2(w, h)) { }

    public Vector2 End => Position + Size;
    public Vector2 Center => Position + Size * 0.5f;
    public float Area => Size.X * Size.Y;

    public bool HasPoint(Vector2 p) =>
        p.X >= Position.X && p.Y >= Position.Y && p.X < End.X && p.Y < End.Y;

    public bool Intersects(Rect2 b) =>
        Position.X < b.End.X && End.X > b.Position.X &&
        Position.Y < b.End.Y && End.Y > b.Position.Y;

    public Rect2 Merge(Rect2 b)
    {
        Vector2 min = new(Mathf.Min(Position.X, b.Position.X), Mathf.Min(Position.Y, b.Position.Y));
        Vector2 max = new(Mathf.Max(End.X, b.End.X), Mathf.Max(End.Y, b.End.Y));
        return new Rect2(min, max - min);
    }

    public override string ToString() => $"Rect2({Position}, {Size})";
}

/// <summary>Axis-aligned bounding box in 3D.</summary>
public readonly struct Aabb
{
    public readonly Vector3 Position; // minimum corner
    public readonly Vector3 Size;

    public Aabb(Vector3 position, Vector3 size) { Position = position; Size = size; }

    public Vector3 End => Position + Size;
    public Vector3 Center => Position + Size * 0.5f;
    public float Volume => Size.X * Size.Y * Size.Z;

    public bool HasPoint(Vector3 p) =>
        p.X >= Position.X && p.X <= End.X &&
        p.Y >= Position.Y && p.Y <= End.Y &&
        p.Z >= Position.Z && p.Z <= End.Z;

    public bool Intersects(Aabb b) =>
        Position.X < b.End.X && End.X > b.Position.X &&
        Position.Y < b.End.Y && End.Y > b.Position.Y &&
        Position.Z < b.End.Z && End.Z > b.Position.Z;

    public Aabb Merge(Aabb b)
    {
        Vector3 min = new(Mathf.Min(Position.X, b.Position.X), Mathf.Min(Position.Y, b.Position.Y), Mathf.Min(Position.Z, b.Position.Z));
        Vector3 max = new(Mathf.Max(End.X, b.End.X), Mathf.Max(End.Y, b.End.Y), Mathf.Max(End.Z, b.End.Z));
        return new Aabb(min, max - min);
    }

    public override string ToString() => $"AABB({Position}, {Size})";
}
