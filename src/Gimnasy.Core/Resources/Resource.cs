using Gimnasy.Core.Object;

namespace Gimnasy.Core.Resources;

/// <summary>
/// Base class for shared, serializable assets (materials, meshes, textures,
/// audio streams…). Resources are referenced by path (<c>res://…</c>) and may
/// be saved to disk independently of any scene.
/// </summary>
public abstract class Resource : GObject
{
    /// <summary>Project-relative path this resource was loaded from, if any.</summary>
    public string? ResourcePath { get; set; }

    /// <summary>The type name used when this resource is serialized.</summary>
    public abstract string ResourceType { get; }
}
