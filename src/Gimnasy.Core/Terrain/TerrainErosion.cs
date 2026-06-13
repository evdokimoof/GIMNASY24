using Gimnasy.Core.Math;

namespace Gimnasy.Core.Terrain;

/// <summary>Tunable parameters for the droplet-based hydraulic erosion pass.</summary>
public sealed class HydraulicErosionSettings
{
    public int Droplets { get; set; } = 70_000;
    public int MaxLifetime { get; set; } = 30;
    public float Inertia { get; set; } = 0.05f;        // 0 = water follows gradient exactly
    public float SedimentCapacityFactor { get; set; } = 4f;
    public float MinSedimentCapacity { get; set; } = 0.01f;
    public float ErodeSpeed { get; set; } = 0.3f;
    public float DepositSpeed { get; set; } = 0.3f;
    public float EvaporateSpeed { get; set; } = 0.01f;
    public float Gravity { get; set; } = 4f;
    public float InitialWater { get; set; } = 1f;
    public float InitialSpeed { get; set; } = 1f;
    public int ErosionRadius { get; set; } = 3;
    public int Seed { get; set; } = 1;
}

/// <summary>
/// Physically-inspired terrain erosion. The hydraulic pass simulates thousands
/// of rain droplets that pick up and deposit sediment as they flow downhill,
/// carving valleys and building up deltas — the standard technique used by
/// World Machine / Gaea-style tools. A thermal pass relaxes slopes past the
/// talus angle, producing natural scree. Both operate in place on the
/// height-field and re-bake normals afterwards.
/// </summary>
public static class TerrainErosion
{
    public static void Hydraulic(TerrainData data, HydraulicErosionSettings settings)
    {
        int res = data.Resolution;
        float[] map = data.Heights; // shared backing array — eroded in place
        var rng = new Random(settings.Seed);
        var (weights, offsets) = BuildBrush(res, settings.ErosionRadius);

        for (int iteration = 0; iteration < settings.Droplets; iteration++)
        {
            float posX = (float)rng.NextDouble() * (res - 1);
            float posY = (float)rng.NextDouble() * (res - 1);
            float dirX = 0, dirY = 0;
            float speed = settings.InitialSpeed;
            float water = settings.InitialWater;
            float sediment = 0;

            for (int life = 0; life < settings.MaxLifetime; life++)
            {
                int nodeX = (int)posX;
                int nodeY = (int)posY;
                int dropletIndex = nodeY * res + nodeX;
                float cellOffsetX = posX - nodeX;
                float cellOffsetY = posY - nodeY;

                var (height, gradX, gradY) = HeightAndGradient(map, res, posX, posY);

                // Update direction with inertia, then move one cell.
                dirX = dirX * settings.Inertia - gradX * (1 - settings.Inertia);
                dirY = dirY * settings.Inertia - gradY * (1 - settings.Inertia);
                float len = Mathf.Sqrt(dirX * dirX + dirY * dirY);
                if (len > Mathf.Epsilon) { dirX /= len; dirY /= len; }
                posX += dirX;
                posY += dirY;

                if ((dirX == 0 && dirY == 0) || posX < 0 || posX >= res - 1 || posY < 0 || posY >= res - 1)
                    break;

                float newHeight = HeightAndGradient(map, res, posX, posY).height;
                float deltaHeight = newHeight - height;

                float capacity = Mathf.Max(-deltaHeight * speed * water * settings.SedimentCapacityFactor,
                    settings.MinSedimentCapacity);

                if (sediment > capacity || deltaHeight > 0)
                {
                    // Deposit: either fill an uphill pit or drop the surplus.
                    float deposit = deltaHeight > 0
                        ? Mathf.Min(deltaHeight, sediment)
                        : (sediment - capacity) * settings.DepositSpeed;
                    sediment -= deposit;
                    map[dropletIndex] += deposit * (1 - cellOffsetX) * (1 - cellOffsetY);
                    map[dropletIndex + 1] += deposit * cellOffsetX * (1 - cellOffsetY);
                    map[dropletIndex + res] += deposit * (1 - cellOffsetX) * cellOffsetY;
                    map[dropletIndex + res + 1] += deposit * cellOffsetX * cellOffsetY;
                }
                else
                {
                    // Erode using the radial brush (never more than the drop below).
                    float erode = Mathf.Min((capacity - sediment) * settings.ErodeSpeed, -deltaHeight);
                    for (int i = 0; i < offsets.Length; i++)
                    {
                        int idx = dropletIndex + offsets[i];
                        if (idx < 0 || idx >= map.Length) continue;
                        float weighted = erode * weights[i];
                        float removed = map[idx] < weighted ? map[idx] : weighted;
                        map[idx] -= removed;
                        sediment += removed;
                    }
                }

                speed = Mathf.Sqrt(Mathf.Max(0, speed * speed + deltaHeight * settings.Gravity));
                water *= 1 - settings.EvaporateSpeed;
            }
        }

        ClampHeights(map, data.MaxHeight);
        data.BakeNormals();
    }

    /// <summary>Thermal erosion: move material from slopes steeper than the
    /// talus angle to lower neighbours over several iterations.</summary>
    public static void Thermal(TerrainData data, int iterations = 30, float talusDegrees = 35f, float strength = 0.5f)
    {
        int res = data.Resolution;
        float[] map = data.Heights;
        float talus = Mathf.Tan(Mathf.DegToRad(talusDegrees)) * data.CellSize;
        var delta = new float[map.Length];

        for (int it = 0; it < iterations; it++)
        {
            Array.Clear(delta, 0, delta.Length);
            for (int z = 1; z < res - 1; z++)
            for (int x = 1; x < res - 1; x++)
            {
                int i = z * res + x;
                float h = map[i];
                float maxDiff = 0f; int lowest = -1;
                for (int dz = -1; dz <= 1; dz++)
                for (int dx = -1; dx <= 1; dx++)
                {
                    if (dx == 0 && dz == 0) continue;
                    int ni = (z + dz) * res + (x + dx);
                    float diff = h - map[ni];
                    if (diff > maxDiff) { maxDiff = diff; lowest = ni; }
                }
                if (lowest >= 0 && maxDiff > talus)
                {
                    float move = (maxDiff - talus) * 0.5f * strength;
                    delta[i] -= move;
                    delta[lowest] += move;
                }
            }
            for (int i = 0; i < map.Length; i++) map[i] += delta[i];
        }

        ClampHeights(map, data.MaxHeight);
        data.BakeNormals();
    }

    private static (float height, float gradX, float gradY) HeightAndGradient(float[] map, int res, float posX, float posY)
    {
        int x = (int)posX, y = (int)posY;
        float fx = posX - x, fy = posY - y;
        int i = y * res + x;
        float nw = map[i], ne = map[i + 1], sw = map[i + res], se = map[i + res + 1];

        float gradX = (ne - nw) * (1 - fy) + (se - sw) * fy;
        float gradY = (sw - nw) * (1 - fx) + (se - ne) * fx;
        float height = nw * (1 - fx) * (1 - fy) + ne * fx * (1 - fy)
                     + sw * (1 - fx) * fy + se * fx * fy;
        return (height, gradX, gradY);
    }

    private static (float[] weights, int[] offsets) BuildBrush(int res, int radius)
    {
        var weights = new List<float>();
        var offsets = new List<int>();
        float weightSum = 0f;
        for (int dz = -radius; dz <= radius; dz++)
        for (int dx = -radius; dx <= radius; dx++)
        {
            float sqrDst = dx * dx + dz * dz;
            if (sqrDst > radius * radius) continue;
            float w = 1f - Mathf.Sqrt(sqrDst) / radius;
            weightSum += w;
            weights.Add(w);
            offsets.Add(dz * res + dx);
        }
        for (int i = 0; i < weights.Count; i++) weights[i] /= weightSum;
        return (weights.ToArray(), offsets.ToArray());
    }

    private static void ClampHeights(float[] map, float maxHeight)
    {
        for (int i = 0; i < map.Length; i++) map[i] = Mathf.Clamp(map[i], 0, maxHeight);
    }
}
