namespace Gimnasy.Core.Object;

/// <summary>A late-bound, string-named signal connection.</summary>
public sealed class SignalConnection
{
    public required string SignalName { get; init; }
    public required Action<object?[]> Callback { get; init; }
    public object? Target { get; init; }
    public bool OneShot { get; init; }
}

/// <summary>
/// Base for everything that can emit signals and carry a name. Signals are
/// string-named (engine-wide convention) so they can be authored in the
/// editor and serialized as connections in scene files.
/// </summary>
public abstract class GObject
{
    private readonly Dictionary<string, List<SignalConnection>> _connections = new();
    private static int _idCounter;

    public int InstanceId { get; } = ++_idCounter;
    public string Name { get; set; } = string.Empty;

    /// <summary>Connect a handler to a named signal.</summary>
    public SignalConnection Connect(string signal, Action<object?[]> handler,
        object? target = null, bool oneShot = false)
    {
        var conn = new SignalConnection
        {
            SignalName = signal, Callback = handler, Target = target, OneShot = oneShot
        };
        if (!_connections.TryGetValue(signal, out var list))
            _connections[signal] = list = new List<SignalConnection>();
        list.Add(conn);
        return conn;
    }

    public void Disconnect(SignalConnection connection)
    {
        if (_connections.TryGetValue(connection.SignalName, out var list))
            list.Remove(connection);
    }

    public bool IsConnected(string signal) =>
        _connections.TryGetValue(signal, out var list) && list.Count > 0;

    /// <summary>Emit a signal to all connected handlers.</summary>
    public void EmitSignal(string signal, params object?[] args)
    {
        if (!_connections.TryGetValue(signal, out var list) || list.Count == 0) return;
        // Snapshot so handlers may connect/disconnect during emission.
        var snapshot = list.ToArray();
        foreach (var c in snapshot)
        {
            c.Callback(args);
            if (c.OneShot) list.Remove(c);
        }
    }

    public override string ToString() => $"{GetType().Name}#{InstanceId}({Name})";
}
