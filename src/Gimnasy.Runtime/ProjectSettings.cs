using System.Text.Json;
using Gimnasy.Core.Serialization;

namespace Gimnasy.Runtime;

/// <summary>
/// Parsed <c>project.gimnasy</c> file — the manifest that points the runtime at
/// the main scene, window settings and the compiled script assembly.
/// </summary>
public sealed class ProjectSettings
{
    public string Name { get; set; } = "Untitled";
    public string MainScene { get; set; } = "res://main.scen";
    public string? ScriptAssembly { get; set; }
    public int WindowWidth { get; set; } = 1280;
    public int WindowHeight { get; set; } = 720;
    public double PhysicsTickRate { get; set; } = 60;
    public string ProjectRoot { get; set; } = ".";

    public static ProjectSettings Load(string path)
    {
        string text = File.ReadAllText(path);
        using var doc = JsonLike.Parse(text);
        var root = doc.RootElement;
        var s = new ProjectSettings { ProjectRoot = Path.GetDirectoryName(Path.GetFullPath(path)) ?? "." };

        string? Str(string k) => root.TryGetProperty(k, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;
        int? Int(string k) => root.TryGetProperty(k, out var v) && v.ValueKind == JsonValueKind.Number ? v.GetInt32() : null;

        s.Name = Str("name") ?? s.Name;
        s.MainScene = Str("main_scene") ?? s.MainScene;
        s.ScriptAssembly = Str("script_assembly");
        s.WindowWidth = Int("window_width") ?? s.WindowWidth;
        s.WindowHeight = Int("window_height") ?? s.WindowHeight;
        if (root.TryGetProperty("physics_tick_rate", out var ptr) && ptr.ValueKind == JsonValueKind.Number)
            s.PhysicsTickRate = ptr.GetDouble();
        return s;
    }
}
