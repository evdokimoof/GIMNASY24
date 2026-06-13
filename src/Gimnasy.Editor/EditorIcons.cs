using System.Text.Json;

namespace Gimnasy.Editor;

/// <summary>
/// Resolves editor icons (toolbar actions and asset file-types) from the
/// shipped <c>assets/editor/icons</c> set, driven by <c>manifest.json</c>.
/// The asset browser uses <see cref="ForFile"/> to pick a file's thumbnail and
/// the toolbar uses <see cref="ForAction"/>.
/// </summary>
public sealed class EditorIcons
{
    private readonly Dictionary<string, string> _byExtension = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string> _byAction = new(StringComparer.OrdinalIgnoreCase);
    private string _fallback = "icons8-сломанное-изображение-50.png";

    public string IconsRoot { get; }

    public EditorIcons(string iconsRoot) { IconsRoot = iconsRoot; }

    public static EditorIcons Load(string iconsRoot)
    {
        var icons = new EditorIcons(iconsRoot);
        string manifest = Path.Combine(iconsRoot, "manifest.json");
        if (!File.Exists(manifest)) return icons;

        using var doc = JsonDocument.Parse(File.ReadAllText(manifest));
        var root = doc.RootElement;
        if (root.TryGetProperty("fallback", out var fb)) icons._fallback = fb.GetString() ?? icons._fallback;
        if (root.TryGetProperty("by_extension", out var ext))
            foreach (var p in ext.EnumerateObject()) icons._byExtension[p.Name] = p.Value.GetString()!;
        if (root.TryGetProperty("by_action", out var act))
            foreach (var p in act.EnumerateObject()) icons._byAction[p.Name] = p.Value.GetString()!;
        return icons;
    }

    public string ForFile(string fileName)
    {
        string ext = Path.GetExtension(fileName).TrimStart('.');
        return Path.Combine(IconsRoot, _byExtension.TryGetValue(ext, out var i) ? i : _fallback);
    }

    public string ForAction(string action) =>
        Path.Combine(IconsRoot, _byAction.TryGetValue(action, out var i) ? i : _fallback);

    public int ExtensionCount => _byExtension.Count;
    public int ActionCount => _byAction.Count;
}
