using Gimnasy.Core.Math;

namespace Gimnasy.Core.Terrain;

/// <summary>
/// Procedural noise field used to author base terrain shapes. Implements a
/// gradient (Perlin-style) noise with fractal Brownian motion, ridged and
/// billow variants and domain warping — the building blocks professional
/// terrain tools expose for natural-looking landscapes.
/// </summary>
public sealed class TerrainNoise
{
    private readonly int[] _perm = new int[512];

    public int Seed { get; }
    public int Octaves { get; set; } = 6;
    public float Lacunarity { get; set; } = 2.0f;   // frequency multiplier per octave
    public float Gain { get; set; } = 0.5f;          // amplitude multiplier per octave
    public float BaseFrequency { get; set; } = 0.01f;
    public float Ridged { get; set; } = 0f;          // 0 = fbm, 1 = fully ridged
    public float DomainWarp { get; set; } = 0f;       // warp strength in world units

    public TerrainNoise(int seed = 1337)
    {
        Seed = seed;
        var rng = new Random(seed);
        var p = new int[256];
        for (int i = 0; i < 256; i++) p[i] = i;
        for (int i = 255; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (p[i], p[j]) = (p[j], p[i]);
        }
        for (int i = 0; i < 512; i++) _perm[i] = p[i & 255];
    }

    private static float Fade(float t) => t * t * t * (t * (t * 6f - 15f) + 10f);

    private static float Grad(int hash, float x, float y)
    {
        switch (hash & 7)
        {
            case 0: return x + y;
            case 1: return -x + y;
            case 2: return x - y;
            case 3: return -x - y;
            case 4: return x;
            case 5: return -x;
            case 6: return y;
            default: return -y;
        }
    }

    /// <summary>Single-octave gradient noise in roughly [-1, 1].</summary>
    public float Noise2D(float x, float y)
    {
        int xi = (int)Mathf.Floor(x) & 255;
        int yi = (int)Mathf.Floor(y) & 255;
        float xf = x - Mathf.Floor(x);
        float yf = y - Mathf.Floor(y);
        float u = Fade(xf), v = Fade(yf);

        int aa = _perm[_perm[xi] + yi];
        int ab = _perm[_perm[xi] + yi + 1];
        int ba = _perm[_perm[xi + 1] + yi];
        int bb = _perm[_perm[xi + 1] + yi + 1];

        float x1 = Mathf.Lerp(Grad(aa, xf, yf), Grad(ba, xf - 1, yf), u);
        float x2 = Mathf.Lerp(Grad(ab, xf, yf - 1), Grad(bb, xf - 1, yf - 1), u);
        return Mathf.Lerp(x1, x2, v);
    }

    /// <summary>Fractal sample in [0, 1], blending fbm and ridged by <see cref="Ridged"/>.</summary>
    public float Sample(float worldX, float worldY)
    {
        if (DomainWarp > 0f)
        {
            float wx = Noise2D(worldX * BaseFrequency + 5.2f, worldY * BaseFrequency + 1.3f);
            float wy = Noise2D(worldX * BaseFrequency + 9.7f, worldY * BaseFrequency + 4.8f);
            worldX += wx * DomainWarp;
            worldY += wy * DomainWarp;
        }

        float freq = BaseFrequency, amp = 1f, sum = 0f, norm = 0f;
        for (int o = 0; o < Octaves; o++)
        {
            float n = Noise2D(worldX * freq, worldY * freq);     // [-1,1]
            float fbm = n * 0.5f + 0.5f;                         // [0,1]
            float ridge = 1f - Mathf.Abs(n);                    // [0,1], sharp ridges
            float sample = Mathf.Lerp(fbm, ridge * ridge, Mathf.Clamp01(Ridged));
            sum += sample * amp;
            norm += amp;
            amp *= Gain;
            freq *= Lacunarity;
        }
        return norm > 0f ? sum / norm : 0f;
    }
}
