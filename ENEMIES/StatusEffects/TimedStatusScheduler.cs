// One lazy world-owned clock visits only enemies with timed effects; pause follows the battlefield.
using Godot;
using System.Collections.Generic;
public partial class TimedStatusScheduler : Node
{
    #region Active Scheduling
    private readonly List<Enemy> _active = new();
    public int ActiveCount => _active.Count;
    // =========================================================
    // Registry ownership isolates simultaneous worlds and releases the scheduler on world unload.
    public static TimedStatusScheduler For(EnemyRegistry registry)
    {
        var clock = registry.GetNodeOrNull<TimedStatusScheduler>("TimedStatuses");
        if (clock == null) { clock = new() { Name = "TimedStatuses" }; registry.AddChild(clock); clock.SetPhysicsProcess(false); }
        return clock;
    }
    // =========================================================
    // Each enemy registers once; buffers retain capacity and idle clocks do not process.
    public void Register(Enemy enemy)
    { if (!_active.Contains(enemy)) _active.Add(enemy); SetPhysicsProcess(true); }
    // =========================================================
    // Lifecycle removal is immediate, even while paused.
    public void Unregister(Enemy enemy)
    { _active.Remove(enemy); if (_active.Count == 0) SetPhysicsProcess(false); }
    // =========================================================
    // Reverse removal keeps iteration safe without temporary lists or per-enemy timers.
    public override void _PhysicsProcess(double delta)
    {
        if (!double.IsFinite(delta) || delta <= 0) return;
        for (int i = _active.Count - 1; i >= 0; i--)
            if (!GodotObject.IsInstanceValid(_active[i]) || !_active[i].AdvanceStatuses((float)delta)) _active.RemoveAt(i);
        if (_active.Count == 0) SetPhysicsProcess(false);
    }
    #endregion
}
