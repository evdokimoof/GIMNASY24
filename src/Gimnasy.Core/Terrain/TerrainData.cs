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
    Paint,      // change material only
    SeaLevel,   // set to a fixed level
}

/// <summary>
/// Height-field terrain storage with painting layers. This is the data a
/// <c>Terrain3D</c> node renders and the brush tools mutate. The grid is
/// <see cref="Resolution"/> × <see cref="Resolution"/> samples spaced
/// <see cref="CellSize"/> apart, giving editor-driven, Roblox-style sculpting.
/// </summary>
[RegisteredType("TerrainData", "Resource")]
public sealed class TerrainData : Resource
{
    public override string ResourceType => "TerrainData";

    [Export] public int Resolution { get; set; } = 129;          // (2^n)+1 for LOD seams
    [Export] public float CellSize { get; set; } = 1f;
    [Export] public float MaxHeight { get; set; } = 100f;
    [Export] public int MaterialLayerCount { get; set; } = 4;

    private float[] _heights = Array.Empty<float>();
    private byte[] _materials = Array.Empty<byte>();

    public float[] Heights => EnsureAllocated()._heights;
    public byte[] Materials => EnsureAllocated()._materials;
    public float WorldSize => (Resolution - 1) * CellSize;

    private TerrainData EnsureAllocated()
    {
        int n = Resolution * Resolution;
        if (_heights.Length != n) _heights = new float[n];
        if (_materials.Length != n) _materials = new byte[n];
        return this;
    }

    private int Index(int x, int z) => z * Resolution + x;
    private static int Clamp(int v, int max) => v < 0 ? 0 : (v > max ? max : v);

    public float GetHeight(int x, int z) =>
        EnsureAllocated()._heights[Index(Clamp(x, Resolution - 1), Clamp(z, Resolution - 1))];

    public void SetHeight(int x, int z, float h)
    {
        if (x < 0 || z < 0 || x >= Resolution || z >= Resolution) return;
        EnsureAllocated()._heights[Index(x, z)] = Mathf.Clamp(h, 0, MaxHeight);
    }

    public byte GetMaterial(int x, int z) =>
        EnsureAllocated()._materials[Index(Clamp(x, Resolution - 1), Clamp(z, Resolution - 1))];

    /// <summary>Bilinearly sample the height at a local-space XZ position.</summary>
    public float SampleHeight(float localX, float localZ)
    {
        float fx = localX / CellSize, fz = localZ / CellSize;
        int x0 = (int)Mathf.Floor(fx), z0 = (int)Mathf.Floor(fz);
        float tx = fx - x0, tz = fz - z0;
        float h00 = GetHeight(x0, z0), h10 = GetHeight(x0 + 1, z0);
        float h01 = GetHeight(x0, z0 + 1), h11 = GetHeight(x0 + 1, z0 + 1);
        return Mathf.Lerp(Mathf.Lerp(h00, h10, tx), Mathf.Lerp(h01, h11, tx), tz);
    }

    /// <summary>Approximate surface normal from neighbouring heights.</summary>
    public Vector3 SampleNormal(int x, int z)
    {
        float hl = GetHeight(x - 1, z), hr = GetHeight(x + 1, z);
        float hd = GetHeight(x, z - 1), hu = GetHeight(x, z + 1);
        return new Vector3(hl - hr, 2f * CellSize, hd - hu).Normalized;
    }

    /// <summary>
    /// Apply a brush at a local-space position. <paramref name="strength"/> is
    /// in world units per call; <paramref name="falloff"/> 0..1 softens edges.
    /// Returns the number of samples touched.
    /// </summary>
    public int Sculpt(TerrainBrush brush, Vector2 localXZ, float radius, float strength,
        float targetHeight = 0f, byte paintMaterial = 0, float falloff = 0.5f)
    {
        EnsureAllocated();
        int cx = (int)Mathf.Round(localXZ.X / CellSize);
        int cz = (int)Mathf.Round(localXZ.Y / CellSize);
        int cells = Mathf.Max(1, (int)Mathf.Ceil(radius / CellSize));
        int touched = 0;

        for (int dz = -cells; dz <= cells; dz++)
        for (int dx = -cells; dx <= cells; dx++)
        {
            int x = cx + dx, z = cz + dz;
            if (x < 0 || z < 0 || x >= Resolution || z >= Resolution) continue;
            float dist = new Vector2(dx, dz).Length * CellSize;
            if (dist > radius) continue;

            float w = Weight(dist, radius, falloff);
            ApplyBrush(brush, x, z, w * strength, targetHeight, paintMaterial);
            touched++;
        }
        return touched;
    }

    private void ApplyBrush(TerrainBrush brush, int x, int z, float amount, float target, byte material)
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
            case TerrainBrush.Paint: _materials[i] = material; break;
        }
    }

    private float NeighbourAverage(int x, int z)
    {
        float sum = 0; int count = 0;
        for (int dz = -1; dz <= 1; dz++)
        for (int dx = -1; dx <= 1; dx++)
        {
            sum += GetHeight(x + dx, z + dz);
            count++;
        }
        return sum / count;
    }

    private static float Weight(float dist, float radius, float falloff)
    {
        // t = 1 at the centre, 0 at the edge.
        float t = Mathf.Clamp01(1f - dist / radius);
        // Blend between a hard brush (1 everywhere inside) and a soft smoothstep
        // brush according to falloff (0 = hard, 1 = fully feathered).
        float hard = t > 0f ? 1f : 0f;
        return Mathf.Lerp(hard, Mathf.SmoothStep(0f, 1f, t), Mathf.Clamp01(falloff));
    }

    /// <summary>Fill the terrain with fractal value noise — a quick base island.</summary>
    public void GenerateFractal(int seed = 1337, float amplitude = 0.5f, int octaves = 5)
    {
        EnsureAllocated();
        var rng = new Random(seed);
        float[] perm = new float[256];
        for (int i = 0; i < perm.Length; i++) perm[i] = (float)rng.NextDouble();

        float Noise(float x, float z)
        {
            int xi = (int)x & 255, zi = (int)z & 255;
            return perm[(xi + zi * 57) & 255];
        }

        for (int z = 0; z < Resolution; z++)
        for (int x = 0; x < Resolution; x++)
        {
            float total = 0f, freq = 1f, amp = amplitude, max = 0f;
            for (int o = 0; o < octaves; o++)
            {
                total += Noise(x * 0.05f * freq, z * 0.05f * freq) * amp;
                max += amp;
                amp *= 0.5f; freq *= 2f;
            }
            _heights[Index(x, z)] = (total / max) * MaxHeight * amplitude;
        }
    }
}
