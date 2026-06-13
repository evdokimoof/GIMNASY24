using Gimnasy.Core.Input;
using Gimnasy.Core.Scene;
using Gimnasy.Core.Serialization;
using Gimnasy.Runtime.Render;
using Gimnasy.Scripting;

namespace Gimnasy.Runtime;

/// <summary>
/// Boots a project: registers types, loads the script assembly and the main
/// scene, then drives the <see cref="MainLoop"/>. This is the single entry the
/// platform players (desktop exe, etc.) wrap.
/// </summary>
public sealed class Application
{
    public ProjectSettings Settings { get; }
    public IRenderingServer Renderer { get; }
    public SceneTree Tree { get; }

    public Application(ProjectSettings settings, IRenderingServer? renderer = null)
    {
        Settings = settings;
        Renderer = renderer ?? new NullRenderingServer();

        ScriptHost.Initialize();
        ResourceLoader.ProjectRoot = settings.ProjectRoot;
        InputMap.LoadDefaults();

        if (!string.IsNullOrEmpty(settings.ScriptAssembly))
        {
            string dll = ResourceLoader.ResolvePath(settings.ScriptAssembly);
            if (File.Exists(dll)) ScriptHost.LoadScriptAssembly(dll);
            else Console.WriteLine($"[warn] script assembly not found: {dll}");
        }

        Tree = new SceneTree();
    }

    /// <summary>Load the configured main scene into the tree.</summary>
    public void LoadMainScene()
    {
        string scenePath = ResourceLoader.ResolvePath(Settings.MainScene);
        if (!File.Exists(scenePath))
            throw new FileNotFoundException($"Main scene not found: {scenePath}");
        var root = SceneSerializer.Load(scenePath);
        Tree.ChangeScene(root);
        Console.WriteLine($"[app] loaded scene '{root.Name}' ({CountNodes(root)} nodes)");
    }

    public MainLoop CreateLoop()
    {
        Renderer.Initialize(Settings.WindowWidth, Settings.WindowHeight, Settings.Name);
        return new MainLoop(Tree, Renderer) { PhysicsTickRate = Settings.PhysicsTickRate };
    }

    private static int CountNodes(Node n)
    {
        int c = 1;
        foreach (var child in n.Children) c += CountNodes(child);
        return c;
    }
}
