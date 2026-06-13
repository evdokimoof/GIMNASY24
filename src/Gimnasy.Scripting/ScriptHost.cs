using System.Reflection;
using Gimnasy.Core;
using Gimnasy.Core.Object;

namespace Gimnasy.Scripting;

/// <summary>
/// Loads compiled game assemblies and registers the script node types they
/// contain. The recommended workflow compiles the game's <c>scripts/*.cs</c>
/// into a single DLL (see the Python <c>build</c> command); this host then
/// loads it so scenes referencing those scripts can be instantiated.
///
/// A hot-reload, Roslyn-based source compiler is on the roadmap and would slot
/// in behind the same <see cref="LoadScriptAssembly"/> surface.
/// </summary>
public static class ScriptHost
{
    private static readonly List<Assembly> _loaded = new();

    public static IReadOnlyList<Assembly> LoadedAssemblies => _loaded;

    /// <summary>Register the built-in node library so scripts can subclass it.</summary>
    public static void Initialize()
    {
        CoreModule.Initialize(typeof(Nodes.Node2D).Assembly, typeof(GameScript).Assembly);
    }

    /// <summary>Load a compiled game DLL and register every node type in it.</summary>
    public static Assembly LoadScriptAssembly(string dllPath)
    {
        var asm = Assembly.LoadFrom(System.IO.Path.GetFullPath(dllPath));
        ClassDb.RegisterAssembly(asm);
        _loaded.Add(asm);
        return asm;
    }

    /// <summary>Register an already-loaded assembly (e.g. the game's own).</summary>
    public static void Register(Assembly assembly)
    {
        ClassDb.RegisterAssembly(assembly);
        if (!_loaded.Contains(assembly)) _loaded.Add(assembly);
    }
}
