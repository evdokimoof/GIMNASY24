using System.Text.Json;
using Gimnasy.Core.Math;
using Gimnasy.Core.Resources;

namespace Gimnasy.Core.Serialization;

/// <summary>
/// Converts engine property values to and from the plain object graph
/// (numbers, strings, bools, lists, dictionaries) that is then written as the
/// JSON-like <c>.scen</c>/<c>.material</c> syntax. Math types serialize as
/// compact arrays, e.g. a Vector3 becomes <c>[1, 2, 3]</c>.
/// </summary>
public static class VariantConverter
{
    public static object? ToVariant(object? value) => value switch
    {
        null => null,
        bool or string => value,
        int i => (double)i,
        long l => (double)l,
        float f => (double)f,
        double => value,
        Enum e => e.ToString(),
        Vector2 v => new List<object> { v.X, v.Y },
        Vector2I v => new List<object> { v.X, v.Y },
        Vector3 v => new List<object> { v.X, v.Y, v.Z },
        Vector4 v => new List<object> { v.X, v.Y, v.Z, v.W },
        Quaternion q => new List<object> { q.X, q.Y, q.Z, q.W },
        Color c => new List<object> { c.R, c.G, c.B, c.A },
        Resource r => ResourceRefToVariant(r),
        _ => value.ToString()
    };

    private static object ResourceRefToVariant(Resource r)
    {
        // External resource → reference by path; otherwise inline.
        if (!string.IsNullOrEmpty(r.ResourcePath))
            return new Dictionary<string, object?> { ["$res"] = r.ResourcePath };
        var dict = new Dictionary<string, object?> { ["$type"] = r.ResourceType };
        dict["properties"] = PropertyBag.Capture(r);
        return dict;
    }

    public static object? FromVariant(JsonElement el, Type targetType)
    {
        Type t = Nullable.GetUnderlyingType(targetType) ?? targetType;

        if (t == typeof(bool)) return el.GetBoolean();
        if (t == typeof(string)) return el.GetString();
        if (t == typeof(int)) return el.GetInt32();
        if (t == typeof(long)) return el.GetInt64();
        if (t == typeof(float)) return (float)el.GetDouble();
        if (t == typeof(double)) return el.GetDouble();
        if (t.IsEnum) return Enum.Parse(t, el.GetString() ?? "0", ignoreCase: true);

        if (t == typeof(Vector2)) { var a = Arr(el); return new Vector2(a[0], a[1]); }
        if (t == typeof(Vector2I)) { var a = Arr(el); return new Vector2I((int)a[0], (int)a[1]); }
        if (t == typeof(Vector3)) { var a = Arr(el); return new Vector3(a[0], a[1], a[2]); }
        if (t == typeof(Vector4)) { var a = Arr(el); return new Vector4(a[0], a[1], a[2], a[3]); }
        if (t == typeof(Quaternion)) { var a = Arr(el); return new Quaternion(a[0], a[1], a[2], a[3]); }
        if (t == typeof(Color)) { var a = Arr(el); return new Color(a[0], a[1], a[2], a.Length > 3 ? a[3] : 1f); }

        if (typeof(Resource).IsAssignableFrom(t))
            return ResourceRefFromVariant(el, t);

        // Fallback: leave as raw string.
        return el.ValueKind == JsonValueKind.String ? el.GetString() : el.GetRawText();
    }

    private static object? ResourceRefFromVariant(JsonElement el, Type t)
    {
        if (el.ValueKind == JsonValueKind.Object && el.TryGetProperty("$res", out var path))
            return ResourceLoader.Load(path.GetString()!);
        if (el.ValueKind == JsonValueKind.Object && el.TryGetProperty("$type", out var typeEl))
        {
            var res = ResourceLoader.CreateByType(typeEl.GetString()!);
            if (res is not null && el.TryGetProperty("properties", out var props))
                PropertyBag.Apply(res, props);
            return res;
        }
        return null;
    }

    private static float[] Arr(JsonElement el)
    {
        var list = new List<float>();
        foreach (var item in el.EnumerateArray()) list.Add((float)item.GetDouble());
        return list.ToArray();
    }
}
