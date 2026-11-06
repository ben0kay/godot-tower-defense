// One active-only world clock. Retained versioned snapshots tolerate lethal ticks and nested lifecycle callbacks.
using Godot;
using System.Collections.Generic;
public partial class TimedStatusScheduler : Node
{
    private readonly record struct Entry(Enemy Enemy, ulong Version);
    private readonly List<Entry> _active = new(), _step = new();
    private readonly Dictionary<Enemy, int> _indices = new();
    private ulong _version;
    private bool _advancing;
    public int ActiveCount => _active.Count;
    // =========================================================
    // No timed status means no scheduler allocation or processing in this world.
    public static TimedStatusScheduler For(EnemyRegistry registry)
    {
        var clock = registry.GetNodeOrNull<TimedStatusScheduler>("TimedStatuses");
        if (clock == null) { clock = new() { Name = "TimedStatuses" }; registry.AddChild(clock); clock.SetProcess(false); clock.SetPhysicsProcess(false); }
        return clock;
    }
    public void Register(Enemy enemy)
    {
        if (!_indices.ContainsKey(enemy)) { _indices.Add(enemy, _active.Count); _active.Add(new(enemy, ++_version)); }
        SetPhysicsProcess(true);
    }
    // =========================================================
    // Swap-removal is O(1). Version tags distinguish the same native enemy after reinitialization.
    public void Unregister(Enemy enemy)
    {
        if (!_indices.Remove(enemy, out int index)) return;
        int last = _active.Count - 1;
        if (index != last) { _active[index] = _active[last]; _indices[_active[index].Enemy] = index; }
        _active.RemoveAt(last); if (_active.Count == 0) SetPhysicsProcess(false);
    }
    private bool Contains(Entry entry) => _indices.TryGetValue(entry.Enemy, out int index) && _active[index].Version == entry.Version;
    // =========================================================
    // Newly registered effects wait for the next step. Removed enemies and stale generations are skipped.
    public override void _PhysicsProcess(double delta)
    {
        if (_advancing || !IsInsideTree() || GetTree().Paused || !double.IsFinite(delta) || delta <= 0) return;
        _advancing = true; _step.Clear(); _step.AddRange(_active);
        try
        {
            for (int i = 0; i < _step.Count; i++)
            {
                Entry entry = _step[i]; if (!Contains(entry)) continue;
                bool timed = GodotObject.IsInstanceValid(entry.Enemy) && entry.Enemy.AdvanceStatuses(delta);
                if (!timed && Contains(entry)) Unregister(entry.Enemy);
            }
        }
        finally { _step.Clear(); _advancing = false; if (GodotObject.IsInstanceValid(this) && IsInsideTree()) SetPhysicsProcess(_active.Count > 0); }
    }
    public override void _ExitTree() { _active.Clear(); _step.Clear(); _indices.Clear(); }
}
