using System.Diagnostics;
using Gimnasy.Core.Input;
using Gimnasy.Core.Math;
using Gimnasy.Core.Scene;
using Gimnasy.Runtime.Render;

namespace Gimnasy.Runtime;

/// <summary>
/// The game loop. Physics runs at a fixed timestep (accumulator pattern) for
/// determinism, while <c>_Process</c> and rendering run once per displayed
/// frame. This is the same decoupling Godot/Unity use.
/// </summary>
public sealed class MainLoop
{
    private readonly SceneTree _tree;
    private readonly IRenderingServer _renderer;

    public double PhysicsTickRate { get; set; } = 60.0;
    public Color ClearColor { get; set; } = new Color(0.12f, 0.12f, 0.15f);
    public ulong FrameCount { get; private set; }

    public MainLoop(SceneTree tree, IRenderingServer renderer)
    {
        _tree = tree;
        _renderer = renderer;
    }

    /// <summary>Run for a fixed number of frames (headless / test / CI use).</summary>
    public void RunFrames(int frames, double simulatedDelta = 1.0 / 60.0)
    {
        for (int i = 0; i < frames; i++)
            Step(simulatedDelta);
    }

    /// <summary>Run in real time until <paramref name="shouldQuit"/> returns true.</summary>
    public void Run(Func<bool> shouldQuit)
    {
        var clock = Stopwatch.StartNew();
        double last = clock.Elapsed.TotalSeconds;
        double accumulator = 0;
        double fixedDelta = 1.0 / PhysicsTickRate;

        while (!shouldQuit())
        {
            double now = clock.Elapsed.TotalSeconds;
            double frameDelta = Math.Min(now - last, 0.25); // clamp spiral-of-death
            last = now;
            accumulator += frameDelta;

            while (accumulator >= fixedDelta)
            {
                _tree.PhysicsProcess(fixedDelta);
                accumulator -= fixedDelta;
            }

            RenderFrame(_tree, frameDelta);
            InputServer.EndFrame();
            FrameCount++;
        }
    }

    private void Step(double delta)
    {
        _tree.PhysicsProcess(delta);
        RenderFrame(_tree, delta);
        InputServer.EndFrame();
        FrameCount++;
    }

    private void RenderFrame(SceneTree tree, double delta)
    {
        tree.Process(delta);
        _renderer.BeginFrame(ClearColor);
        SceneRenderer.Render(tree.Root, _renderer);
        _renderer.EndFrame();
    }
}
