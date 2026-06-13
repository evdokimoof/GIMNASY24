using System.Reflection;

namespace Gimnasy.Core.Object;

/// <summary>Metadata about one exported property of a registered type.</summary>
public sealed class ExportedProperty
{
    public required string Name { get; init; }
    public required Type Type { get; init; }
    public required PropertyInfo Info { get; init; }
    public string? Hint { get; init; }
    public string? Category { get; init; }

    public object? GetValue(object target) => Info.GetValue(target);
    public void SetValue(object target, object? value) => Info.SetValue(target, value);
}

/// <summary>Description of a registered engine type (node / resource).</summary>
public sealed class TypeDescriptor
{
    public required string TypeName { get; init; }
    public required string Category { get; init; }
    public required Type ClrType { get; init; }
    public required IReadOnlyList<ExportedProperty> Properties { get; init; }

    public object CreateInstance() =>
        Activator.CreateInstance(ClrType)
        ?? throw new InvalidOperationException($"Cannot instantiate {TypeName}");
}

/// <summary>
/// Central registry of all engine types — the equivalent of Godot's ClassDB.
/// Types tagged with <see cref="RegisteredTypeAttribute"/> are discovered by
/// reflection and become available for scene (de)serialization, the editor's
/// "Add Node" dialog, and scripting.
/// </summary>
public static class ClassDb
{
    private static readonly Dictionary<string, TypeDescriptor> _byName = new();
    private static readonly Dictionary<Type, TypeDescriptor> _byType = new();

    public static IReadOnlyDictionary<string, TypeDescriptor> All => _byName;
    public static int Count => _byName.Count;

    /// <summary>Scan an assembly and register every tagged type it contains.</summary>
    public static void RegisterAssembly(Assembly assembly)
    {
        foreach (var type in assembly.GetTypes())
        {
            var attr = type.GetCustomAttribute<RegisteredTypeAttribute>();
            if (attr is null || type.IsAbstract) continue;
            Register(type, attr.TypeName, attr.Category);
        }
    }

    public static TypeDescriptor Register(Type type, string typeName, string category)
    {
        var props = new List<ExportedProperty>();
        // Walk the full hierarchy so inherited [Export] properties are included.
        for (var t = type; t is not null && t != typeof(object); t = t.BaseType)
        {
            foreach (var pi in t.GetProperties(BindingFlags.Public | BindingFlags.Instance
                         | BindingFlags.DeclaredOnly))
            {
                var ex = pi.GetCustomAttribute<ExportAttribute>();
                if (ex is null || !pi.CanRead || !pi.CanWrite) continue;
                if (props.Exists(p => p.Name == pi.Name)) continue;
                props.Add(new ExportedProperty
                {
                    Name = pi.Name, Type = pi.PropertyType, Info = pi,
                    Hint = ex.Hint, Category = ex.Category
                });
            }
        }

        var desc = new TypeDescriptor
        {
            TypeName = typeName, Category = category, ClrType = type, Properties = props
        };
        _byName[typeName] = desc;
        _byType[type] = desc;
        return desc;
    }

    public static TypeDescriptor? Get(string typeName) =>
        _byName.TryGetValue(typeName, out var d) ? d : null;

    public static TypeDescriptor? Get(Type type) =>
        _byType.TryGetValue(type, out var d) ? d : null;

    public static bool IsRegistered(string typeName) => _byName.ContainsKey(typeName);

    public static object Instantiate(string typeName) =>
        Get(typeName)?.CreateInstance()
        ?? throw new KeyNotFoundException($"Type '{typeName}' is not registered in ClassDB.");

    public static IEnumerable<TypeDescriptor> InCategory(string category)
    {
        foreach (var d in _byName.Values)
            if (d.Category == category) yield return d;
    }
}
