using Gimnasy.Core.Math;

namespace Gimnasy.Core.Input;

/// <summary>One physical binding for an action (a key, button or axis).</summary>
public readonly struct InputBinding
{
    public string Device { get; init; }   // "key", "mouse", "pad"
    public string Code { get; init; }      // e.g. "Space", "Left", "A"
    public InputBinding(string device, string code) { Device = device; Code = code; }
}

/// <summary>
/// Named action → bindings map (Godot's InputMap). Game code queries actions
/// rather than raw keys, so controls are rebindable and platform-agnostic.
/// </summary>
public static class InputMap
{
    private static readonly Dictionary<string, List<InputBinding>> _actions = new();

    public static void AddAction(string action)
    {
        if (!_actions.ContainsKey(action)) _actions[action] = new List<InputBinding>();
    }

    public static void Bind(string action, string device, string code)
    {
        AddAction(action);
        _actions[action].Add(new InputBinding(device, code));
    }

    public static IReadOnlyList<InputBinding> GetBindings(string action) =>
        _actions.TryGetValue(action, out var l) ? l : Array.Empty<InputBinding>();

    public static IEnumerable<string> Actions => _actions.Keys;
    public static bool HasAction(string action) => _actions.ContainsKey(action);

    /// <summary>Set up the conventional default actions for a fresh project.</summary>
    public static void LoadDefaults()
    {
        Bind("ui_left", "key", "Left"); Bind("ui_left", "key", "A");
        Bind("ui_right", "key", "Right"); Bind("ui_right", "key", "D");
        Bind("ui_up", "key", "Up"); Bind("ui_up", "key", "W");
        Bind("ui_down", "key", "Down"); Bind("ui_down", "key", "S");
        Bind("ui_accept", "key", "Enter"); Bind("ui_accept", "key", "Space");
        Bind("ui_cancel", "key", "Escape");
        Bind("jump", "key", "Space");
    }
}

/// <summary>
/// The global input state. A platform backend feeds it raw key/mouse events
/// each frame; gameplay code reads actions and the mouse via the static API.
/// </summary>
public static class InputServer
{
    private static readonly HashSet<string> _down = new();         // currently held
    private static readonly HashSet<string> _justPressed = new();  // pressed this frame
    private static readonly HashSet<string> _justReleased = new(); // released this frame

    public static Vector2 MousePosition { get; private set; }
    public static Vector2 MouseDelta { get; private set; }

    // ---- Backend feed (called by the platform layer) -----------------------

    public static void FeedKey(string code, bool pressed)
    {
        string id = "key:" + code;
        if (pressed) { if (_down.Add(id)) _justPressed.Add(id); }
        else if (_down.Remove(id)) _justReleased.Add(id);
    }

    public static void FeedMouse(Vector2 position)
    {
        MouseDelta = position - MousePosition;
        MousePosition = position;
    }

    /// <summary>Clears the per-frame edges; the runtime calls this each frame end.</summary>
    public static void EndFrame() { _justPressed.Clear(); _justReleased.Clear(); MouseDelta = Vector2.Zero; }

    // ---- Query API (used by game scripts) ----------------------------------

    public static bool IsActionPressed(string action) =>
        AnyBinding(action, id => _down.Contains(id));

    public static bool IsActionJustPressed(string action) =>
        AnyBinding(action, id => _justPressed.Contains(id));

    public static bool IsActionJustReleased(string action) =>
        AnyBinding(action, id => _justReleased.Contains(id));

    public static float GetActionStrength(string action) => IsActionPressed(action) ? 1f : 0f;

    /// <summary>Composite axis, e.g. GetAxis("ui_left","ui_right") → [-1,1].</summary>
    public static float GetAxis(string negative, string positive) =>
        GetActionStrength(positive) - GetActionStrength(negative);

    public static Vector2 GetVector(string left, string right, string up, string down) =>
        new Vector2(GetAxis(left, right), GetAxis(up, down)).Length > 1f
            ? new Vector2(GetAxis(left, right), GetAxis(up, down)).Normalized
            : new Vector2(GetAxis(left, right), GetAxis(up, down));

    private static bool AnyBinding(string action, Func<string, bool> test)
    {
        foreach (var b in InputMap.GetBindings(action))
            if (test(b.Device + ":" + b.Code)) return true;
        return false;
    }
}
