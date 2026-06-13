namespace Gimnasy.Core.Math;

/// <summary>Linear RGBA color with components normally in the [0,1] range.</summary>
public readonly struct Color : IEquatable<Color>
{
    public readonly float R, G, B, A;

    public Color(float r, float g, float b, float a = 1f) { R = r; G = g; B = b; A = a; }

    public static readonly Color White = new(1, 1, 1);
    public static readonly Color Black = new(0, 0, 0);
    public static readonly Color Transparent = new(0, 0, 0, 0);
    public static readonly Color Red = new(1, 0, 0);
    public static readonly Color Green = new(0, 1, 0);
    public static readonly Color Blue = new(0, 0, 1);
    public static readonly Color Yellow = new(1, 1, 0);
    public static readonly Color Cyan = new(0, 1, 1);
    public static readonly Color Magenta = new(1, 0, 1);
    public static readonly Color Gray = new(0.5f, 0.5f, 0.5f);

    public Color Lerp(Color b, float t) =>
        new(Mathf.Lerp(R, b.R, t), Mathf.Lerp(G, b.G, t), Mathf.Lerp(B, b.B, t), Mathf.Lerp(A, b.A, t));

    public Color WithAlpha(float a) => new(R, G, B, a);

    /// <summary>Parse "#RRGGBB" or "#RRGGBBAA" (with or without leading '#').</summary>
    public static Color FromHtml(string hex)
    {
        hex = hex.TrimStart('#');
        byte ParseByte(int i) => Convert.ToByte(hex.Substring(i, 2), 16);
        float r = ParseByte(0) / 255f, g = ParseByte(2) / 255f, b = ParseByte(4) / 255f;
        float a = hex.Length >= 8 ? ParseByte(6) / 255f : 1f;
        return new Color(r, g, b, a);
    }

    public string ToHtml(bool withAlpha = true)
    {
        byte To(float v) => (byte)Mathf.Clamp((int)(v * 255f + 0.5f), 0, 255);
        string s = $"{To(R):X2}{To(G):X2}{To(B):X2}";
        return withAlpha ? s + $"{To(A):X2}" : s;
    }

    public static Color operator *(Color c, float s) => new(c.R * s, c.G * s, c.B * s, c.A * s);
    public static Color operator *(Color a, Color b) => new(a.R * b.R, a.G * b.G, a.B * b.B, a.A * b.A);
    public static Color operator +(Color a, Color b) => new(a.R + b.R, a.G + b.G, a.B + b.B, a.A + b.A);

    public bool Equals(Color o) => Mathf.Approximately(R, o.R) && Mathf.Approximately(G, o.G)
        && Mathf.Approximately(B, o.B) && Mathf.Approximately(A, o.A);
    public override bool Equals(object? o) => o is Color c && Equals(c);
    public override int GetHashCode() => HashCode.Combine(R, G, B, A);
    public override string ToString() => $"Color({R}, {G}, {B}, {A})";
}
