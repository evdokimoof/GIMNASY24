using Gimnasy.Core.Math;
using Gimnasy.Core.Scene;

namespace Gimnasy.Runtime.Render;

/// <summary>
/// Backend-agnostic rendering interface. The scene is walked once per frame and
/// drawable nodes issue commands through this surface. Concrete backends:
///   • <see cref="NullRenderingServer"/> — headless (CI, servers, tests).
///   • An OpenGL/Vulkan backend (roadmap) implementing the same contract.
/// Keeping the contract small is what lets the engine run the same game code on
/// every platform.
/// </summary>
public interface IRenderingServer
{
    string Name { get; }
    int DrawCallsLastFrame { get; }

    void Initialize(int width, int height, string title);
    void BeginFrame(Color clearColor);

    void DrawSprite(Texture2DHandle texture, Transform2D transform, Color modulate);
    void DrawRect(Rect2 rect, Color color, bool filled = true);
    void DrawText(string text, Vector2 position, int size, Color color);
    void DrawMesh(MeshHandle mesh, Transform3D transform, MaterialHandle material);

    void EndFrame();
    void Shutdown();
}

// Opaque GPU resource handles. Backends map these to real textures/buffers.
public readonly record struct Texture2DHandle(int Id);
public readonly record struct MeshHandle(int Id);
public readonly record struct MaterialHandle(int Id);
