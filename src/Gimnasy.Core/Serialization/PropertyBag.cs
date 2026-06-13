using System.Text.Json;
using Gimnasy.Core.Object;

namespace Gimnasy.Core.Serialization;

/// <summary>
/// Reads and writes the <c>[Export]</c> properties of any registered object as
/// a plain <c>Dictionary&lt;string, object?&gt;</c>. Only values that differ
/// from a fresh instance's defaults are captured, keeping scene files small.
/// </summary>
public static class PropertyBag
{
    public static Dictionary<string, object?> Capture(object target)
    {
        var result = new Dictionary<string, object?>();
        var desc = ClassDb.Get(target.GetType());
        if (desc is null) return result;

        object? defaults = TryCreateDefaults(target.GetType());

        foreach (var prop in desc.Properties)
        {
            object? value = prop.GetValue(target);
            if (defaults is not null)
            {
                object? def = prop.GetValue(defaults);
                if (Equals(value, def)) continue; // skip unchanged
            }
            result[prop.Name] = VariantConverter.ToVariant(value);
        }
        return result;
    }

    public static void Apply(object target, JsonElement properties)
    {
        var desc = ClassDb.Get(target.GetType());
        if (desc is null) return;
        foreach (var jp in properties.EnumerateObject())
        {
            var prop = FindProp(desc.Properties, jp.Name);
            if (prop is null) continue;
            object? converted = VariantConverter.FromVariant(jp.Value, prop.Type);
            if (converted is not null || IsNullable(prop.Type))
                prop.SetValue(target, converted);
        }
    }

    private static ExportedProperty? FindProp(IReadOnlyList<ExportedProperty> props, string name)
    {
        foreach (var p in props) if (p.Name == name) return p;
        return null;
    }

    private static object? TryCreateDefaults(Type t)
    {
        try { return Activator.CreateInstance(t); }
        catch { return null; }
    }

    private static bool IsNullable(Type t) =>
        !t.IsValueType || Nullable.GetUnderlyingType(t) is not null;
}
