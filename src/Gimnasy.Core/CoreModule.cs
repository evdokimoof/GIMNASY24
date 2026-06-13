using System.Reflection;
using Gimnasy.Core.Object;

namespace Gimnasy.Core;

/// <summary>
/// Entry point that wires up the core type registry. Call
/// <see cref="Initialize"/> once at startup (the runtime does this for you)
/// before loading any scene or resource.
/// </summary>
public static class CoreModule
{
    private static bool _initialized;

    public static void Initialize(params Assembly[] extraAssemblies)
    {
        if (_initialized) return;
        ClassDb.RegisterAssembly(typeof(CoreModule).Assembly);
        foreach (var asm in extraAssemblies)
            ClassDb.RegisterAssembly(asm);
        _initialized = true;
    }

    /// <summary>Register a game/script assembly so its node types load in scenes.</summary>
    public static void RegisterGameAssembly(Assembly assembly) =>
        ClassDb.RegisterAssembly(assembly);
}
