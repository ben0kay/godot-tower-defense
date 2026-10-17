// One lazy clock per affected tower, not one node per effect or a scan over all placed towers.
using Godot;
using System;
using System.Collections.Generic;

public partial class TowerConditions : Node
{
    private struct Entry { public long Token; public TowerDisableReason Reason; public ulong Source; public double? Remaining; }
    private readonly List<Entry> _entries = new();
    private TowerBase _owner;
    private long _next;
    private TowerDisableHandle _overheat;
    private HeatStats? _stats;
    private double _notice;
    private bool _heatDirty;
    public float Heat { get; private set; }
    public bool IsOverheated => _overheat.Token != 0;
    public int Count => _entries.Count;
    public HeatStats? Stats => _stats;

    // =========================================================
    // Priority runs timers/cooling before ordinary weapon physics. Pause inheritance freezes the clock.
    internal void Initialize(TowerBase owner) { _owner = owner; ProcessPhysicsPriority = -100; SetProcess(false); SetPhysicsProcess(false); }
    // =========================================================
    // Indexed read avoids allocating status snapshots; presentation may query after a change event.
    public TowerDisableReason ReasonAt(int index) => _entries[index].Reason;
    // =========================================================
    // Source zero creates an independent instance; a nonzero source refreshes its same-reason application.
    internal TowerDisableHandle Apply(TowerDisableReason reason, double? duration, ulong source)
    {
        if (!Enum.IsDefined(reason)) throw new ArgumentOutOfRangeException(nameof(reason));
        if (duration.HasValue && (!double.IsFinite(duration.Value) || duration <= 0))
            throw new ArgumentOutOfRangeException(nameof(duration), "Disable reason and duration must be valid; null duration means condition-based.");
        if (source != 0)
            for (int i = 0; i < _entries.Count; i++)
                if (_entries[i].Reason == reason && _entries[i].Source == source)
                { Entry item = _entries[i]; item.Remaining = duration; _entries[i] = item; Wake(); return new(_owner.GetInstanceId(), item.Token); }
        long token = ++_next;
        _entries.Add(new() { Token = token, Reason = reason, Source = source, Remaining = duration });
        TowerDisableHandle handle = new(_owner.GetInstanceId(), token);
        Wake(); _owner.PublishDisableChanged(); return handle;
    }
    // =========================================================
    // Removing one application never clears another EMP or a separate reason.
    internal bool Remove(TowerDisableHandle handle)
    {
        if (handle.TowerId != _owner.GetInstanceId() || handle.Token == 0) return false;
        for (int i = 0; i < _entries.Count; i++) if (_entries[i].Token == handle.Token)
        {
            _entries.RemoveAt(i); if (_overheat == handle) _overheat = default;
            Wake(); _owner.PublishDisableChanged(); return true;
        }
        return false;
    }
    // =========================================================
    // Source lifetime is explicit: timed pulses persist after source death; sustained effects can remove by source.
    internal void RemoveSource(ulong source)
    {
        if (source == 0) return;
        bool changed = false;
        for (int i = _entries.Count - 1; i >= 0; i--) if (_entries[i].Source == source) { _entries.RemoveAt(i); changed = true; }
        if (changed) { Wake(); _owner.PublishDisableChanged(); }
    }
    // =========================================================
    // Purchases/rank changes preserve absolute heat and latch; capability removal clears only its own cause.
    internal void Synchronize(HeatStats? stats)
    {
        _stats = HeatStats.Sanitize(stats);
        Heat = _stats.HasValue ? Math.Min(Heat, _stats.Value.Capacity) : 0;
        Reconcile(); Wake(); if (Alive()) _owner.PublishHeatChanged();
    }
    // =========================================================
    // Only committed launches/pulses or funded damaging beam time call this; never seeking/cooldown failures.
    internal void Charge(int shots, double beamSeconds)
    {
        if (!_stats.HasValue) return;
        HeatStats s = _stats.Value;
        double amount = Math.Max(0, shots) * (double)s.PerShot + Math.Max(0, beamSeconds) * s.PerBeamSecond;
        if (!double.IsFinite(amount) || amount <= 0) return;
        Heat = (float)Math.Min(s.Capacity, Heat + amount);
        // Repeated fractional beam steps must not stall just below capacity after float rounding.
        if (s.Capacity - Heat <= s.Capacity * .000001f) Heat = s.Capacity;
        _heatDirty = true;
        Reconcile(); Wake(); if (Alive()) _owner.PublishHeatChanged();
    }
    // =========================================================
    // One overheat handle belongs to this capability; cooling must not remove an unrelated disable.
    private void Reconcile()
    {
        if (IsOverheated && (!_stats.HasValue || Heat <= _stats.Value.Capacity * _stats.Value.RestartBelowFraction))
            Remove(_overheat);
        if (Alive() && !IsOverheated && _stats.HasValue && Heat >= _stats.Value.Capacity)
        {
            // Install the latch before publishing so callbacks see coherent state.
            long token = ++_next; _overheat = new(_owner.GetInstanceId(), token);
            _entries.Add(new() { Token = token, Reason = TowerDisableReason.Overheated });
            _owner.PublishDisableChanged();
        }
    }
    // =========================================================
    // The same explicit clock serves tests and normal engine processing; no stat rebuilding on ticks.
    public void Advance(double delta)
    {
        if (!double.IsFinite(delta) || delta <= 0 || !GodotObject.IsInstanceValid(_owner) || !_owner.IsInsideTree()
            || _owner.IsQueuedForDeletion() || _owner.GetTree().Paused) return;
        bool removed = false;
        for (int i = _entries.Count - 1; i >= 0; i--)
        {
            Entry entry = _entries[i]; if (!entry.Remaining.HasValue) continue;
            entry.Remaining -= delta;
            if (entry.Remaining <= 0) { _entries.RemoveAt(i); removed = true; } else _entries[i] = entry;
        }
        if (removed) _owner.PublishDisableChanged();
        if (!Alive()) return;
        if (_stats.HasValue && Heat > 0)
        {
            float next = (float)Math.Max(0, Heat - _stats.Value.CoolingPerSecond * delta);
            _heatDirty |= next != Heat; Heat = next; Reconcile();
            if (!Alive()) return;
        }
        _notice += delta;
        if (_heatDirty && (_notice >= .1 || Heat == 0))
        { _heatDirty = false; _notice = 0; _owner.PublishHeatChanged(); }
        Wake();
    }
    // =========================================================
    // Event callbacks may remove a tower synchronously.
    private bool Alive() => GodotObject.IsInstanceValid(_owner) && _owner.IsInsideTree() && !_owner.IsQueuedForDeletion();
    // =========================================================
    // No clock is needed for cold towers or exclusively condition-based disabling.
    private void Wake()
    {
        bool active = Heat > 0 && _stats.HasValue && _stats.Value.CoolingPerSecond > 0;
        for (int i = 0; i < _entries.Count && !active; i++) active = _entries[i].Remaining.HasValue;
        if (GodotObject.IsInstanceValid(this) && Alive()) SetPhysicsProcess(active);
    }
    // =========================================================
    // Engine time, not wall time, determines expiration and cooling.
    public override void _PhysicsProcess(double delta) => Advance(delta);
    // =========================================================
    // Unload retains no source Nodes, subscriptions or callbacks.
    internal void Clear() { _entries.Clear(); _overheat = default; Heat = 0; _owner = null; SetPhysicsProcess(false); }
}
