using Gimnasy.Core.Object;
using Gimnasy.Runtime;
using Gimnasy.Runtime.Render;
using Gimnasy.Scripting;

// ---------------------------------------------------------------------------
//  Gimnasy command-line player / runtime.
//
//  Usage:
//    gimnasy run <project.gimnasy> [--frames N] [--verbose]
//    gimnasy info                       list every registered type
// ---------------------------------------------------------------------------

if (args.Length == 0)
{
    PrintUsage();
    return 0;
}

switch (args[0])
{
    case "run":
        return RunProject(args);
    case "info":
        return PrintInfo();
    case "--version" or "version":
        Console.WriteLine("Gimnasy Engine 0.1.0");
        return 0;
    default:
        PrintUsage();
        return 1;
}

static int RunProject(string[] args)
{
    if (args.Length < 2) { Console.Error.WriteLine("error: expected a project path"); return 1; }
    string projectPath = args[1];
    int frames = GetIntFlag(args, "--frames", 0);
    bool verbose = Array.Exists(args, a => a == "--verbose");

    var settings = ProjectSettings.Load(projectPath);
    var renderer = new NullRenderingServer { Verbose = verbose };
    var app = new Application(settings, renderer);
    app.LoadMainScene();

    var loop = app.CreateLoop();
    if (frames > 0)
    {
        Console.WriteLine($"[app] running {frames} frames (headless)…");
        loop.RunFrames(frames);
        Console.WriteLine($"[app] done. draw calls last frame: {renderer.DrawCallsLastFrame}");
    }
    else
    {
        Console.WriteLine("[app] running until Ctrl+C…");
        bool quit = false;
        Console.CancelKeyPress += (_, e) => { e.Cancel = true; quit = true; };
        loop.Run(() => quit);
    }
    return 0;
}

static int PrintInfo()
{
    ScriptHost.Initialize();
    Console.WriteLine($"Gimnasy Engine — {ClassDb.Count} registered types\n");
    foreach (var group in ClassDb.All.Values
                 .GroupBy(t => t.Category).OrderBy(g => g.Key))
    {
        Console.WriteLine($"[{group.Key}]");
        foreach (var t in group.OrderBy(t => t.TypeName))
            Console.WriteLine($"  {t.TypeName}  ({t.Properties.Count} exported props)");
        Console.WriteLine();
    }
    return 0;
}

static void PrintUsage()
{
    Console.WriteLine("""
        Gimnasy Engine 0.1.0
        Usage:
          gimnasy run <project.gimnasy> [--frames N] [--verbose]
          gimnasy info
          gimnasy version
        """);
}

static int GetIntFlag(string[] args, string flag, int fallback)
{
    int i = Array.IndexOf(args, flag);
    return i >= 0 && i + 1 < args.Length && int.TryParse(args[i + 1], out int v) ? v : fallback;
}
