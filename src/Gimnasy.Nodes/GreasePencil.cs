using Gimnasy.Core.Math;
using Gimnasy.Core.Object;
using Gimnasy.Core.Resources;

namespace Gimnasy.Nodes;

// ===========================================================================
//  Grease Pencil — hand-drawn strokes in 2D/3D space, inspired by Blender's
//  Grease Pencil. Strokes live on layers; layers can hold keyframes so the
//  drawing can be animated frame-by-frame.
// ===========================================================================

/// <summary>A single freehand stroke: an ordered list of points with pressure.</summary>
[RegisteredType("GreasePencilStroke", "GreasePencil")]
public sealed class GreasePencilStroke : Resource
{
    public override string ResourceType => "GreasePencilStroke";

    public List<Vector3> Points { get; } = new();
    public List<float> Pressure { get; } = new();

    [Export] public Color Color { get; set; } = Color.Black;
    [Export("range:0.1,64")] public float Width { get; set; } = 2f;
    [Export] public bool Filled { get; set; }
    [Export] public Color FillColor { get; set; } = Color.Transparent;
    [Export] public bool Cyclic { get; set; }

    public void AddPoint(Vector3 p, float pressure = 1f)
    {
        Points.Add(p);
        Pressure.Add(Mathf.Clamp01(pressure));
    }

    /// <summary>Ramer–Douglas–Peucker simplification to reduce point count.</summary>
    public void Simplify(float tolerance = 0.01f)
    {
        if (Points.Count < 3) return;
        var keep = new bool[Points.Count];
        keep[0] = keep[^1] = true;
        Rdp(0, Points.Count - 1, tolerance, keep);

        var pts = new List<Vector3>();
        var pres = new List<float>();
        for (int i = 0; i < Points.Count; i++)
            if (keep[i]) { pts.Add(Points[i]); pres.Add(Pressure[i]); }
        Points.Clear(); Points.AddRange(pts);
        Pressure.Clear(); Pressure.AddRange(pres);
    }

    private void Rdp(int first, int last, float tol, bool[] keep)
    {
        float maxDist = 0f; int index = -1;
        Vector3 a = Points[first], b = Points[last];
        for (int i = first + 1; i < last; i++)
        {
            float d = PointLineDistance(Points[i], a, b);
            if (d > maxDist) { maxDist = d; index = i; }
        }
        if (maxDist > tol && index != -1)
        {
            keep[index] = true;
            Rdp(first, index, tol, keep);
            Rdp(index, last, tol, keep);
        }
    }

    private static float PointLineDistance(Vector3 p, Vector3 a, Vector3 b)
    {
        Vector3 ab = b - a;
        float len2 = ab.LengthSquared;
        if (len2 < Mathf.Epsilon) return p.DistanceTo(a);
        float t = Mathf.Clamp01((p - a).Dot(ab) / len2);
        return p.DistanceTo(a + ab * t);
    }
}

/// <summary>A drawing layer holding strokes (optionally per keyframe).</summary>
[RegisteredType("GreasePencilLayer", "GreasePencil")]
public sealed class GreasePencilLayer : Resource
{
    public override string ResourceType => "GreasePencilLayer";

    public List<GreasePencilStroke> Strokes { get; } = new();

    [Export] public string LayerName { get; set; } = "Layer";
    [Export] public bool Visible { get; set; } = true;
    [Export] public bool Locked { get; set; }
    [Export("range:0,1")] public float Opacity { get; set; } = 1f;
    [Export] public Color TintColor { get; set; } = Color.White;
}

/// <summary>The Grease Pencil container node — a stack of drawing layers.</summary>
[RegisteredType("GreasePencil3D", "GreasePencil")]
public sealed class GreasePencil3D : Node3D
{
    public List<GreasePencilLayer> Layers { get; } = new();

    [Export] public int ActiveLayer { get; set; }
    [Export] public bool DepthOrdered { get; set; } = true;
    [Export("range:0.1,64")] public float StrokeThickness { get; set; } = 2f;

    private GreasePencilStroke? _current;

    public GreasePencilLayer GetOrCreateActiveLayer()
    {
        while (Layers.Count <= ActiveLayer) Layers.Add(new GreasePencilLayer { LayerName = $"Layer {Layers.Count + 1}" });
        return Layers[ActiveLayer];
    }

    public void BeginStroke(Color color, float width)
    {
        _current = new GreasePencilStroke { Color = color, Width = width };
    }

    public void StrokeTo(Vector3 point, float pressure = 1f) => _current?.AddPoint(point, pressure);

    public GreasePencilStroke? EndStroke(bool simplify = true)
    {
        if (_current is null) return null;
        if (simplify) _current.Simplify();
        GetOrCreateActiveLayer().Strokes.Add(_current);
        var done = _current;
        _current = null;
        return done;
    }

    public int TotalStrokes()
    {
        int c = 0;
        foreach (var l in Layers) c += l.Strokes.Count;
        return c;
    }
}

/// <summary>2D variant of Grease Pencil for screen-space / canvas drawing.</summary>
[RegisteredType("GreasePencil2D", "GreasePencil")]
public sealed class GreasePencil2D : Node2D
{
    public List<GreasePencilLayer> Layers { get; } = new();
    [Export] public int ActiveLayer { get; set; }
    [Export("range:0.1,128")] public float StrokeThickness { get; set; } = 4f;
}
