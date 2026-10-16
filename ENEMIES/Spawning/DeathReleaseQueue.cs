// One world-owned, budgeted death-release scheduler; no work while empty and no timers on living carriers.
using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics;

public partial class DeathReleaseQueue : Node
{
    private SpawnManager _spawns;
    private readonly List<DeathReleaseJob> _jobs = new();
    private int _cursor;
    private uint _ordinal;
    public int PendingJobs => _jobs.Count;
    public int PendingChildren { get; private set; }
    public int SuccessfulChildren { get; private set; }
    public int SkippedChildren { get; private set; }
    public int LastFrameAttempts { get; private set; }
    public double PeakReleaseMilliseconds { get; private set; }

    // =========================================================
    // Created once by the spawning service, after its registry/path/catalog dependencies exist.
    public override void _Ready() { _spawns = (SpawnManager)GetParent(); SetProcess(false); SetPhysicsProcess(false); }
    // =========================================================
    // Synchronous pre-removal transaction, including the final wave-owned carrier and spawn-entry hazard kills.
    internal void Enqueue(Enemy parent, DeathSpawnConfig config)
    {
        if (!IsInsideTree() || IsQueuedForDeletion()) return;
        if (config?.IsValid != true) { GD.PushWarning("DeathSpawn rejected invalid release settings."); return; }
        if (parent.SpawnGeneration >= config.MaxSpawnGenerations) return;
        PackedScene scene = _spawns.Catalog.Find(config.ChildEnemyId); EnemyDefinition definition = _spawns.Catalog.DefinitionFor(config.ChildEnemyId);
        if (scene == null || definition == null) { GD.PushWarning("DeathSpawn unknown child ID: " + config.ChildEnemyId); return; }
        if (_jobs.Count >= Mathf.Clamp(_spawns.Paths.Config.MaxPendingDeathReleaseJobs, 1, 4096))
        { SkippedChildren += config.ChildCount; GD.PushWarning("DeathSpawn pending-job safety limit reached; release skipped."); return; }
        DeathSpawnReservation reservation = _spawns.WaveOwner?.ReserveDeathChildren(parent, config.ChildCount);
        uint seed = CorridorPreference.Hash(unchecked((uint)_spawns.Paths.Config.DeathReleaseSeed + ++_ordinal * 0x9e3779b9u));
        try { _jobs.Add(new(parent, config, scene, definition, seed, reservation)); }
        catch { reservation?.Cancel(); throw; }
        PendingChildren += config.ChildCount;
        SetPhysicsProcess(true);
    }
    // =========================================================
    // No catch-up burst: each job attempts at most one child/tick, and all jobs share a fair creation budget.
    public override void _PhysicsProcess(double delta) => Advance(delta, Engine.GetPhysicsFrames());

    // =========================================================
    // Explicit frame stamp prevents an already-active queue releasing another carrier in its death tick.
    internal void Advance(double delta, ulong physicsFrame)
    {
        LastFrameAttempts = 0;
        if (GetTree().Paused || !double.IsFinite(delta) || delta < 0 || _jobs.Count == 0) return;
        long started = Stopwatch.GetTimestamp();
        foreach (DeathReleaseJob job in _jobs) job.Delay = Math.Max(0, job.Delay - delta);
        int examined = _jobs.Count, limit = Mathf.Clamp(_spawns.Paths.Config.MaxDeathSpawnAttemptsPerFrame, 1, 16);
        while (examined-- > 0 && _jobs.Count > 0 && LastFrameAttempts < limit)
        {
            _cursor %= _jobs.Count; DeathReleaseJob job = _jobs[_cursor];
            if (job.Reservation != null && !job.Reservation.Active) { RemoveAt(_cursor); continue; }
            if (physicsFrame < job.FirstPhysicsFrame || job.Delay > 0.0000001) { _cursor++; continue; }
            LastFrameAttempts++;
            Enemy child = _spawns.SpawnDeathChild(job);
            if (child == null) SkippedChildren++; else SuccessfulChildren++;
            if (job.Cancelled) continue; // A child lifecycle callback may have reconfigured/unloaded its wave.
            // Entry hazards may have killed a successfully created child; its ownership was already attached.
            job.Index++; PendingChildren--; job.Reservation?.Retire(); job.Delay = job.Interval;
            if (job.Cancelled) continue; // Retiring the slot publishes events, which may cancel this job.
            if (job.Index == job.Count) RemoveAt(_cursor); else _cursor++;
        }
        PeakReleaseMilliseconds = Math.Max(PeakReleaseMilliseconds, Stopwatch.GetElapsedTime(started).TotalMilliseconds);
        if (_jobs.Count == 0 && GodotObject.IsInstanceValid(this) && IsInsideTree()) SetPhysicsProcess(false);
    }
    // =========================================================
    // Wave reconfiguration cancels only that wave's jobs; debug/unowned jobs stay independent.
    internal void CancelWave(WaveManager wave)
    {
        for (int i = _jobs.Count - 1; i >= 0; i--) if (_jobs[i].Reservation?.Owner == wave) RemoveAt(i);
        if (_jobs.Count == 0) SetPhysicsProcess(false);
    }
    private void RemoveAt(int index)
    { DeathReleaseJob job = _jobs[index]; job.Cancelled = true; PendingChildren -= job.Count - job.Index; _jobs.RemoveAt(index); }
    // =========================================================
    // No pending references/timers survive a battlefield unload.
    public override void _ExitTree()
    {
        DeathReleaseJob[] retired = _jobs.ToArray(); _jobs.Clear(); PendingChildren = 0;
        foreach (DeathReleaseJob job in retired) { job.Cancelled = true; job.Reservation?.Cancel(); }
    }
}
