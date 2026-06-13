using Gimnasy.Core.Resources;

namespace Gimnasy.Core.Serialization;

/// <summary>
/// Loads resources by project path (<c>res://…</c>) and instantiates resource
/// types by name. A small cache makes repeated loads of the same asset cheap.
/// </summary>
public static class ResourceLoader
{
    private static readonly Dictionary<string, Resource> _cache = new();
    private static readonly Dictionary<string, Func<Resource>> _factories = new();

    /// <summary>Project root used to resolve <c>res://</c> paths to disk.</summary>
    public static string ProjectRoot { get; set; } = ".";

    /// <summary>Register a factory so a resource type can be created by name.</summary>
    public static void RegisterType(string typeName, Func<Resource> factory) =>
        _factories[typeName] = factory;

    public static Resource? CreateByType(string typeName)
    {
        if (_factories.TryGetValue(typeName, out var f)) return f();
        // Fall back to the reflection-driven ClassDB registration.
        var desc = Object.ClassDb.Get(typeName);
        return desc?.CreateInstance() as Resource;
    }

    public static string ResolvePath(string resPath)
    {
        if (resPath.StartsWith("res://"))
            return System.IO.Path.Combine(ProjectRoot, resPath["res://".Length..]);
        return resPath;
    }

    public static Resource? Load(string resPath)
    {
        if (_cache.TryGetValue(resPath, out var cached)) return cached;

        string diskPath = ResolvePath(resPath);
        if (!System.IO.File.Exists(diskPath)) return null;

        string text = System.IO.File.ReadAllText(diskPath);
        using var doc = JsonLike.Parse(text);
        var root = doc.RootElement;
        string type = root.GetProperty("type").GetString()!;

        var res = CreateByType(type);
        if (res is null) return null;
        res.ResourcePath = resPath;
        if (root.TryGetProperty("properties", out var props))
            PropertyBag.Apply(res, props);

        _cache[resPath] = res;
        return res;
    }

    public static void Save(Resource resource, string resPath)
    {
        var dict = new Dictionary<string, object?>
        {
            ["format"] = 1,
            ["type"] = resource.ResourceType,
            ["properties"] = PropertyBag.Capture(resource)
        };
        string text = JsonLike.Write(dict, $"Gimnasy Resource — {resource.ResourceType}");
        string diskPath = ResolvePath(resPath);
        System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(diskPath)!);
        System.IO.File.WriteAllText(diskPath, text);
        resource.ResourcePath = resPath;
        _cache[resPath] = resource;
    }

    public static void ClearCache() => _cache.Clear();
}
