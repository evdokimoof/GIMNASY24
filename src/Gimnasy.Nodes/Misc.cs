using Gimnasy.Core.Math;
using Gimnasy.Core.Object;
using Gimnasy.Core.Resources;
using Gimnasy.Core.Scene;

namespace Gimnasy.Nodes;

// ===========================================================================
//  Audio, animation, timing and utility nodes.
// ===========================================================================

[RegisteredType("AudioStreamPlayer", "Audio")]
[Signal("finished")]
public sealed class AudioStreamPlayer : Node
{
    [Export] public AudioStream? Stream { get; set; }
    [Export("range:-80,24")] public float VolumeDb { get; set; }
    [Export("range:0.01,4")] public float PitchScale { get; set; } = 1f;
    [Export] public bool Autoplay { get; set; }
    [Export] public bool Playing { get; set; }
    [Export] public string Bus { get; set; } = "Master";
}

[RegisteredType("AudioStreamPlayer2D", "Audio")]
[Signal("finished")]
public sealed class AudioStreamPlayer2D : Node2D
{
    [Export] public AudioStream? Stream { get; set; }
    [Export("range:-80,24")] public float VolumeDb { get; set; }
    [Export] public float MaxDistance { get; set; } = 2000f;
    [Export] public bool Autoplay { get; set; }
}

[RegisteredType("AudioStreamPlayer3D", "Audio")]
[Signal("finished")]
public sealed class AudioStreamPlayer3D : Node3D
{
    [Export] public AudioStream? Stream { get; set; }
    [Export("range:-80,24")] public float VolumeDb { get; set; }
    [Export] public float MaxDistance { get; set; } = 0f;
    [Export("range:0,10")] public float UnitSize { get; set; } = 1f;
    [Export] public bool Autoplay { get; set; }
}

[RegisteredType("AudioListener2D", "Audio")]
public sealed class AudioListener2D : Node2D
{
    [Export] public bool Current { get; set; } = true;
}

[RegisteredType("AudioListener3D", "Audio")]
public sealed class AudioListener3D : Node3D
{
    [Export] public bool Current { get; set; } = true;
}

// ---- Animation ------------------------------------------------------------

[RegisteredType("AnimationPlayer", "Animation")]
[Signal("animation_finished")]
[Signal("animation_started")]
public sealed class AnimationPlayer : Node
{
    [Export] public string CurrentAnimation { get; set; } = string.Empty;
    [Export] public string Autoplay { get; set; } = string.Empty;
    [Export("range:-4,4")] public float SpeedScale { get; set; } = 1f;
    [Export] public bool Active { get; set; } = true;
}

[RegisteredType("AnimationTree", "Animation")]
public sealed class AnimationTree : Node
{
    [Export] public bool Active { get; set; } = true;
    [Export] public string AnimPlayerPath { get; set; } = string.Empty;
}

[RegisteredType("Tween", "Animation")]
[Signal("finished")]
public sealed class Tween : Node
{
    public enum EaseType { In, Out, InOut, OutIn }
    public enum TransType { Linear, Sine, Quad, Cubic, Quart, Expo, Elastic, Bounce, Back }
    [Export] public bool Loops { get; set; }
    [Export("range:0.1,10")] public float SpeedScale { get; set; } = 1f;
    [Export] public EaseType DefaultEase { get; set; } = EaseType.InOut;
    [Export] public TransType DefaultTrans { get; set; } = TransType.Linear;
}

// ---- Timing / utility -----------------------------------------------------

[RegisteredType("Timer", "Utility")]
[Signal("timeout")]
public sealed class Timer : Node
{
    [Export("range:0.001,4096")] public float WaitTime { get; set; } = 1f;
    [Export] public bool OneShot { get; set; }
    [Export] public bool Autostart { get; set; }
    [Export] public bool Paused { get; set; }

    public double TimeLeft { get; private set; }
    private bool _running;

    public void Start(float? time = null)
    {
        WaitTime = time ?? WaitTime;
        TimeLeft = WaitTime;
        _running = true;
    }

    public void Stop() { _running = false; TimeLeft = 0; }

    public override void _Ready() { if (Autostart) Start(); }

    public override void _Process(double delta)
    {
        if (!_running || Paused) return;
        TimeLeft -= delta;
        if (TimeLeft <= 0)
        {
            EmitSignal("timeout");
            if (OneShot) _running = false;
            else TimeLeft += WaitTime;
        }
    }
}

[RegisteredType("HTTPRequest", "Networking")]
[Signal("request_completed")]
public sealed class HttpRequest : Node
{
    [Export] public int TimeoutSeconds { get; set; }
    [Export] public bool UseThreads { get; set; } = true;
    [Export] public string DownloadFile { get; set; } = string.Empty;
}

[RegisteredType("MultiplayerSpawner", "Networking")]
public sealed class MultiplayerSpawner : Node
{
    [Export] public string SpawnPath { get; set; } = string.Empty;
    [Export] public int SpawnLimit { get; set; }
}

[RegisteredType("MultiplayerSynchronizer", "Networking")]
public sealed class MultiplayerSynchronizer : Node
{
    [Export] public float ReplicationInterval { get; set; }
    [Export] public bool PublicVisibility { get; set; } = true;
}

[RegisteredType("NavigationRegion2D", "Navigation")]
public sealed class NavigationRegion2D : Node2D
{
    [Export] public bool Enabled { get; set; } = true;
    [Export] public int NavigationLayers { get; set; } = 1;
}

[RegisteredType("NavigationRegion3D", "Navigation")]
public sealed class NavigationRegion3D : Node3D
{
    [Export] public bool Enabled { get; set; } = true;
    [Export] public int NavigationLayers { get; set; } = 1;
}

[RegisteredType("NavigationAgent2D", "Navigation")]
public sealed class NavigationAgent2D : Node2D
{
    [Export] public float PathDesiredDistance { get; set; } = 10f;
    [Export] public float TargetDesiredDistance { get; set; } = 10f;
    [Export] public bool AvoidanceEnabled { get; set; }
}

[RegisteredType("NavigationAgent3D", "Navigation")]
public sealed class NavigationAgent3D : Node3D
{
    [Export] public float PathDesiredDistance { get; set; } = 1f;
    [Export] public float Radius { get; set; } = 0.5f;
    [Export] public bool AvoidanceEnabled { get; set; }
}

[RegisteredType("SubViewport", "Rendering")]
public sealed class SubViewport : Node
{
    [Export] public Vector2I Size { get; set; } = new Vector2I(512, 512);
    [Export] public bool TransparentBg { get; set; }
    [Export] public bool Disable3D { get; set; }
}

[RegisteredType("ResourcePreloader", "Utility")]
public sealed class ResourcePreloader : Node
{
    [Export] public int ResourceCount { get; set; }
}

[RegisteredType("ShaderGlobals", "Rendering")]
public sealed class ShaderGlobals : Node { }
