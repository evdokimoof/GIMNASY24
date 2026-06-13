using Gimnasy.Core.Math;

namespace Gimnasy.Runtime.Render;

/// <summary>
/// A headless renderer that records but does not display draw commands. It lets
/// the full engine — scene tree, scripts, physics, signals — run on machines
/// with no GPU (CI, dedicated servers, automated tests), and is the reference
/// implementation a real GPU backend is validated against.
/// </summary>
public sealed class NullRenderingServer : IRenderingServer
{
    private int _drawCalls;
    public string Name => "Null (headless)";
    public int DrawCallsLastFrame { get; private set; }

    public bool Verbose { get; set; }

    public void Initialize(int width, int height, string title)
    {
        if (Verbose) Console.WriteLine($"[render] init {width}x{height} '{title}' backend={Name}");
    }

    public void BeginFrame(Color clearColor) => _drawCalls = 0;

    public void DrawSprite(Texture2DHandle texture, Transform2D transform, Color modulate) => _drawCalls++;
    public void DrawRect(Rect2 rect, Color color, bool filled = true) => _drawCalls++;
    public void DrawText(string text, Vector2 position, int size, Color color) => _drawCalls++;
    public void DrawMesh(MeshHandle mesh, Transform3D transform, MaterialHandle material) => _drawCalls++;

    public void EndFrame() => DrawCallsLastFrame = _drawCalls;
    public void Shutdown() { if (Verbose) Console.WriteLine("[render] shutdown"); }
}
