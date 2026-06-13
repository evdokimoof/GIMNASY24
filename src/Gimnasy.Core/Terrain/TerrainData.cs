using Gimnasy.Core.Math;
using Gimnasy.Core.Object;
using Gimnasy.Core.Resources;

namespace Gimnasy.Core.Terrain;

/// <summary>Sculpt operations, modelled on Roblox Studio's terrain tools.</summary>
public enum TerrainBrush
{
    Add,        // raise toward the cursor
    Subtract,   // lower
    Grow,       // raise proportional to existing height
    Erode,      // lower proportional to existing height
    Smooth,     // average with neighbours
    Flatten,    // pull toward a target plane height
    Paint,      // change splat weights of the active layer
    SeaLevel,   // set to a fixed level
    Noise,      // add fractal noise detail
    Hole,       // carve a hole (caves / overhang cut-outs)
    Fill,       // remove a hole
}

/// <summary>
/// Professional height-field terrain storage. Beyond a single heightmap it
/// keeps: per-layer splat weights (texture blending), a hole mask (carved
/// gaps), baked per-vertex normals, a generation descriptor and rich statistics
/// — everything a <see cref="TerrainMeshBuilder"/> needs to emit a full 3D mesh
/// and everything <see cref="TerrainIO"/> serialises to the large JSON terrain
/// document. The grid is <see cref="Resolution"/> × <see cref="Resolution"/>
/// samples spaced <see cref="CellSize"/> apart.
/// </summary>
[RegisteredType("TerrainData", "3D/Terrain")]
public sealed class TerrainData : Resource
{
    public override string ResourceType => "TerrainData";

    [Export] public int Resolution { get; set; } = 129;          // (2^n)+1 for seamless LOD
    [Export] public float CellSize { get; set; } = 1f;
    [Export] public float MaxHeight { get; set; } = 100f;

    // ---- Water -----------------------------------------------------------
    [Export] public bool WaterEnabled { get; set; }
    [Export] public float WaterLevel { get; set; } = 0f;
    [Export] public Color WaterColor { get; set; } = new Color(0.1f, 0.35f, 0.5f, 0.85f);

    // ---- Generation descriptor (kept so the terrain can be regenerated) ---
    [Export] public int Seed { get; set; } = 1337;
    [Export] public int Octaves { get; set; } = 6;
    [Export] public float Lacunarity { get; set; } = 2f;
    [Export] public float Gain { get; set; } = 0.5f;
    [Export] public float BaseFrequency { get; set; } = 0.01f;
    [Export("range:0,1")] public float Ridged { get; set; }
    [Export] public float DomainWarp { get; set; }

    /// <summary>Material/texture layers; their auto-placement rules drive the splatmaps.</summary>
    public List<TerrainLayer> Layers { get; } = new();

    private float[] _heights = Array.Empty<float>();
    private float[][] _splat = Array.Empty<float[]>();   // [layer][cell] weight in [0,1]
    private bool[] _holes = Array.Empty<bool>();
    private Vector3[] _normals = Array.Empty<Vector3>(); // baked per-vertex normals

    public int LayerCount => System.Math.Max(1, Layers.Count);
    public float[] Heights => EnsureAllocated()._heights;
    public bool[] Holes => EnsureAllocated()._holes;
    public Vector3[] BakedNormals => EnsureAllocated()._normals;
    public float WorldSize => (Resolution - 1) * CellSize;
    public int SampleCount => Resolution * Resolution;

    public float[] SplatLayer(int layer) => EnsureAllocated()._splat[Mathf.Clamp(layer, 0, _splat.Length - 1)];

    // ---- Bulk load (used by TerrainIO when deserialising) ----------------

    public void LoadHeights(float[] src)
    {
        EnsureAllocated();
        Array.Copy(src, _heights, System.Math.Min(src.Length, _heights.Length));
    }

    public void LoadSplatLayer(int layer, float[] src)
    {
        EnsureAllocated();
        if (layer < 0 || layer >= _splat.Length) return;
        Array.Copy(src, _splat[layer], System.Math.Min(src.Length, _splat[layer].Length));
    }

    public void LoadHoles(bool[] src)
    {
        EnsureAllocated();
        Array.Copy(src, _holes, System.Math.Min(src.Length, _holes.Length));
    }

    private TerrainData EnsureAllocated()
    {
        int n = Resolution * Resolution;
        if (_heights.Length != n) _heights = new float[n];
        if (_holes.Length != n) _holes = new bool[n];
        if (_normals.Length != n) _normals = new Vector3[n];
        int layers = LayerCount;
        if (_splat.Length != layers || (_splat.Length > 0 && _splat[0].Length != n))
        {
            _splat = new float[layers][];
            for (int i = 0; i < layers; i++)
            {
                _splat[i] = new float[n];
                if (i == 0) Array.Fill(_splat[i], 1f); // default fully base layer
            }
        }
        return this;
    }

    private int Index(int x, int z) => z * Resolution + x;
    private static int Clamp(int v, int max) => v < 0 ? 0 : (v > max ? max : v);

    // ---- Height access ----------------------------------------------------

    public float GetHeight(int x, int z) =>
        EnsureAllocated()._heights[Index(Clamp(x, Resolution - 1), Clamp(z, Resolution - 1))];

    public void SetHeight(int x, int z, float h)
    {
        if (x < 0 || z < 0 || x >= Resolution || z >= Resolution) return;
        EnsureAllocated()._heights[Index(x, z)] = Mathf.Clamp(h, 0, MaxHeight);
    }

    public bool IsHole(int x, int z) =>
        EnsureAllocated()._holes[Index(Clamp(x, Resolution - 1), Clamp(z, Resolution - 1))];

    public float SampleHeight(float localX, float localZ)
    {
        float fx = localX / CellSize, fz = localZ / CellSize;
        int x0 = (int)Mathf.Floor(fx), z0 = (int)Mathf.Floor(fz);
        float tx = fx - x0, tz = fz - z0;
        float h00 = GetHeight(x0, z0), h10 = GetHeight(x0 + 1, z0);
        float h01 = GetHeight(x0, z0 + 1), h11 = GetHeight(x0 + 1, z0 + 1);
        return Mathf.Lerp(Mathf.Lerp(h00, h10, tx), Mathf.Lerp(h01, h11, tx), tz);
    }

    public Vector3 SampleNormal(int x, int z)
    {
        float hl = GetHeight(x - 1, z), hr = GetHeight(x + 1, z);
        float hd = GetHeight(x, z - 1), hu = GetHeight(x, z + 1);
        return new Vector3(hl - hr, 2f * CellSize, hd - hu).Normalized;
    }

    /// <summary>Slope at a sample in degrees (0 = flat, 90 = vertical cliff).</summary>
    public float SlopeDegrees(int x, int z)
    {
        Vector3 n = SampleNormal(x, z);
        return Mathf.RadToDeg(Mathf.Acos(Mathf.Clamp(n.Y, -1f, 1f)));
    }

    // ---- Splat (texture weights) -----------------------------------------

    public float GetSplat(int layer, int x, int z) =>
        EnsureAllocated()._splat[Clamp(layer, _splat.Length - 1)][Index(Clamp(x, Resolution - 1), Clamp(z, Resolution - 1))];

    public void SetSplat(int layer, int x, int z, float weight)
    {
        EnsureAllocated();
        if (x < 0 || z < 0 || x >= Resolution || z >= Resolution || layer < 0 || layer >= _splat.Length) return;
        _splat[layer][Index(x, z)] = Mathf.Clamp01(weight);
        NormalizeSplatAt(x, z);
    }

    private void NormalizeSplatAt(int x, int z)
    {
        int i = Index(x, z);
        float sum = 0f;
        for (int l = 0; l < _splat.Length; l++) sum += _splat[l][i];
        if (sum <= Mathf.Epsilon) { _splat[0][i] = 1f; return; }
        for (int l = 0; l < _splat.Length; l++) _splat[l][i] /= sum;
    }

    /// <summary>Recompute every splat weight from the layers' auto-placement
    /// rules (height + slope). This is the auto-texturing pass.</summary>
    public void RecomputeSplatFromRules()
    {
        EnsureAllocated();
        if (Layers.Count == 0) return;
        for (int z = 0; z < Resolution; z++)
        for (int x = 0; x < Resolution; x++)
        {
            int i = Index(x, z);
            float height = _heights[i];
            float slope = SlopeDegrees(x, z);
            float sum = 0f;
            for (int l = 0; l < Layers.Count; l++)
            {
                float w = Layers[l].WeightAt(height, slope);
                _splat[l][i] = w;
                sum += w;
            }
            if (sum <= Mathf.Epsilon) { _splat[0][i] = 1f; sum = 1f; }
            for (int l = 0; l < Layers.Count; l++) _splat[l][i] /= sum;
        }
    }

    // ---- Normal baking ----------------------------------------------------

    public void BakeNormals()
    {
        EnsureAllocated();
        for (int z = 0; z < Resolution; z++)
        for (int x = 0; x < Resolution; x++)
            _normals[Index(x, z)] = SampleNormal(x, z);
    }

    // ---- Generation -------------------------------------------------------

    /// <summary>Generate the heightmap from the stored noise descriptor, then
    /// bake normals and auto-texture from layer rules. The high-level "make a
    /// landscape" call.</summary>
    public void Generate()
    {
        EnsureAllocated();
        var noise = new TerrainNoise(Seed)
        {
            Octaves = Octaves, Lacunarity = Lacunarity, Gain = Gain,
            BaseFrequency = BaseFrequency, Ridged = Ridged, DomainWarp = DomainWarp,
        };
        for (int z = 0; z < Resolution; z++)
        for (int x = 0; x < Resolution; x++)
            _heights[Index(x, z)] = noise.Sample(x * CellSize, z * CellSize) * MaxHeight;
        BakeNormals();
        RecomputeSplatFromRules();
    }

    /// <summary>Back-compat fractal fill used by simpler call sites.</summary>
    public void GenerateFractal(int seed = 1337, float amplitude = 0.5f, int octaves = 5)
    {
        Seed = seed; Octaves = octaves;
        var noise = new TerrainNoise(seed) { Octaves = octaves, BaseFrequency = BaseFrequency };
        EnsureAllocated();
        for (int z = 0; z < Resolution; z++)
        for (int x = 0; x < Resolution; x++)
            _heights[Index(x, z)] = noise.Sample(x * CellSize, z * CellSize) * MaxHeight * amplitude;
        BakeNormals();
    }

    // ---- Sculpting --------------------------------------------------------

    public int Sculpt(TerrainBrush brush, Vector2 localXZ, float radius, float strength,
        float targetHeight = 0f, int paintLayer = 0, float falloff = 0.5f)
    {
        EnsureAllocated();
        int cx = (int)Mathf.Round(localXZ.X / CellSize);
        int cz = (int)Mathf.Round(localXZ.Y / CellSize);
        int cells = Mathf.Max(1, (int)Mathf.Ceil(radius / CellSize));
        int touched = 0;
        var noise = brush == TerrainBrush.Noise ? new TerrainNoise(Seed) { BaseFrequency = BaseFrequency * 4f } : null;

        for (int dz = -cells; dz <= cells; dz++)
        for (int dx = -cells; dx <= cells; dx++)
        {
            int x = cx + dx, z = cz + dz;
            if (x < 0 || z < 0 || x >= Resolution || z >= Resolution) continue;
            float dist = new Vector2(dx, dz).Length * CellSize;
            if (dist > radius) continue;

            float w = Weight(dist, radius, falloff);
            ApplyBrush(brush, x, z, w * strength, targetHeight, paintLayer, noise);
            touched++;
        }
        // Keep normals coherent in the edited region.
        if (brush is not (TerrainBrush.Paint or TerrainBrush.Hole or TerrainBrush.Fill))
            BakeRegionNormals(cx, cz, cells + 1);
        return touched;
    }

    private void ApplyBrush(TerrainBrush brush, int x, int z, float amount, float target,
        int layer, TerrainNoise? noise)
    {
        int i = Index(x, z);
        switch (brush)
        {
            case TerrainBrush.Add: _heights[i] = Mathf.Clamp(_heights[i] + amount, 0, MaxHeight); break;
            case TerrainBrush.Subtract: _heights[i] = Mathf.Clamp(_heights[i] - amount, 0, MaxHeight); break;
            case TerrainBrush.Grow: _heights[i] = Mathf.Clamp(_heights[i] + amount * (0.2f + _heights[i] / MaxHeight), 0, MaxHeight); break;
            case TerrainBrush.Erode: _heights[i] = Mathf.Clamp(_heights[i] - amount * (0.2f + _heights[i] / MaxHeight), 0, MaxHeight); break;
            case TerrainBrush.Smooth: _heights[i] = Mathf.Lerp(_heights[i], NeighbourAverage(x, z), Mathf.Clamp01(amount)); break;
            case TerrainBrush.Flatten: _heights[i] = Mathf.MoveToward(_heights[i], target, amount); break;
            case TerrainBrush.SeaLevel: _heights[i] = target; break;
            case TerrainBrush.Noise: _heights[i] = Mathf.Clamp(_heights[i] + (noise!.Sample(x * CellSize, z * CellSize) - 0.5f) * amount * 2f, 0, MaxHeight); break;
            case TerrainBrush.Hole: _holes[i] = true; break;
            case TerrainBrush.Fill: _holes[i] = false; break;
            case TerrainBrush.Paint: PaintSplat(i, layer, Mathf.Clamp01(amount)); break;
        }
    }

    private void PaintSplat(int i, int layer, float amount)
    {
        if (layer < 0 || layer >= _splat.Length) return;
        // Increase target layer, decay others, then renormalise.
        _splat[layer][i] = Mathf.Clamp01(_splat[layer][i] + amount);
        float sum = 0f;
        for (int l = 0; l < _splat.Length; l++) sum += _splat[l][i];
        if (sum > Mathf.Epsilon)
            for (int l = 0; l < _splat.Length; l++) _splat[l][i] /= sum;
    }

    private void BakeRegionNormals(int cx, int cz, int radiusCells)
    {
        for (int dz = -radiusCells; dz <= radiusCells; dz++)
        for (int dx = -radiusCells; dx <= radiusCells; dx++)
        {
            int x = cx + dx, z = cz + dz;
            if (x < 0 || z < 0 || x >= Resolution || z >= Resolution) continue;
            _normals[Index(x, z)] = SampleNormal(x, z);
        }
    }

    private float NeighbourAverage(int x, int z)
    {
        float sum = 0; int count = 0;
        for (int dz = -1; dz <= 1; dz++)
        for (int dx = -1; dx <= 1; dx++) { sum += GetHeight(x + dx, z + dz); count++; }
        return sum / count;
    }

    private static float Weight(float dist, float radius, float falloff)
    {
        float t = Mathf.Clamp01(1f - dist / radius);
        float hard = t > 0f ? 1f : 0f;
        return Mathf.Lerp(hard, Mathf.SmoothStep(0f, 1f, t), Mathf.Clamp01(falloff));
    }

    // ---- Statistics -------------------------------------------------------

    public TerrainStats ComputeStats()
    {
        EnsureAllocated();
        float min = float.MaxValue, max = float.MinValue, sum = 0f, maxSlope = 0f;
        int holes = 0;
        for (int z = 0; z < Resolution; z++)
        for (int x = 0; x < Resolution; x++)
        {
            float h = _heights[Index(x, z)];
            min = Mathf.Min(min, h); max = Mathf.Max(max, h); sum += h;
            maxSlope = Mathf.Max(maxSlope, SlopeDegrees(x, z));
            if (_holes[Index(x, z)]) holes++;
        }
        int quads = (Resolution - 1) * (Resolution - 1);
        return new TerrainStats
        {
            MinHeight = min, MaxHeight = max, MeanHeight = sum / SampleCount,
            MaxSlopeDegrees = maxSlope, HoleCount = holes,
            VertexCount = SampleCount, TriangleCount = quads * 2,
        };
    }
}

/// <summary>Aggregate metrics describing a generated terrain.</summary>
public readonly record struct TerrainStats
{
    public float MinHeight { get; init; }
    public float MaxHeight { get; init; }
    public float MeanHeight { get; init; }
    public float MaxSlopeDegrees { get; init; }
    public int HoleCount { get; init; }
    public int VertexCount { get; init; }
    public int TriangleCount { get; init; }
}
