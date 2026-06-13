namespace Gimnasy.Core.Object;

/// <summary>
/// Marks a property as serialized and editable in the inspector — the
/// engine's equivalent of Godot's <c>[Export]</c>. Only properties carrying
/// this attribute are written to <c>.scen</c>/<c>.material</c> files.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class ExportAttribute : Attribute
{
    /// <summary>Optional UI hint (e.g. "range:0,1", "file:*.png", "enum:A,B").</summary>
    public string? Hint { get; }
    /// <summary>Inspector category/group this property is shown under.</summary>
    public string? Category { get; set; }

    public ExportAttribute(string? hint = null) { Hint = hint; }
}

/// <summary>Declares a signal that a class emits, for editor/tooling discovery.</summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class SignalAttribute : Attribute
{
    public string Name { get; }
    public SignalAttribute(string name) { Name = name; }
}

/// <summary>Registers a class under a friendly type name used in scene files.</summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class RegisteredTypeAttribute : Attribute
{
    public string TypeName { get; }
    public string Category { get; }
    public RegisteredTypeAttribute(string typeName, string category = "Node")
    {
        TypeName = typeName;
        Category = category;
    }
}
