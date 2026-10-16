// One world-owned active-job clock. Idle buildings never enter the queue; callbacks may remove/start other jobs safely.
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class ConstructionWorkManager : Node
{
    private readonly record struct Job(TowerBase Tower, ulong Version);
    private readonly Dictionary<TowerBase, ulong> _jobs = new();
    private List<Job> _waiting = new(), _batch = new();
    private ulong _version;
    private bool _advancing, _shutdown;
    public int ActiveCount => _jobs.Count;
    public int LastJobsVisited { get; private set; }

    // =========================================================
    // A world without active construction has no timer callback.
    public override void _Ready() { SetProcess(false); SetPhysicsProcess(_jobs.Count > 0); }

    // =========================================================
    // Unique registration is allowed before the tower enters the tree; paid placement owns rollback.
    internal void Register(TowerBase tower)
    {
        if (_shutdown || !IsInsideTree() || IsQueuedForDeletion()) throw new InvalidOperationException("Construction manager is unavailable.");
        if (_jobs.ContainsKey(tower)) throw new InvalidOperationException("Tower already has active work.");
        ulong version = ++_version;
        _jobs.Add(tower, version); _waiting.Add(new(tower, version)); SetPhysicsProcess(true);
    }

    // =========================================================
    // In-flight entries become harmless tombstones; no mutation of an enumerated collection or per-frame copies.
    internal void Unregister(TowerBase tower)
    {
        _jobs.Remove(tower);
        if (_jobs.Count != 0) return;
        SetPhysicsProcess(false); _waiting.Clear(); if (!_advancing) _batch.Clear();
    }

    // =========================================================
    // Godot supplies unpaused game time; explicit advances share exactly the same pause and validity rules.
    public override void _PhysicsProcess(double delta) => Advance(delta);
    public void Advance(double delta)
    {
        if (_advancing) return;
        LastJobsVisited = 0;
        if (_shutdown || !IsInsideTree() || GetTree().Paused || !double.IsFinite(delta) || delta <= 0) return;
        (_waiting, _batch) = (_batch, _waiting); _advancing = true;
        try
        {
            foreach (Job job in _batch)
            {
                if (!_jobs.TryGetValue(job.Tower, out ulong version) || version != job.Version) continue;
                if (!GodotObject.IsInstanceValid(job.Tower) || !job.Tower.IsInsideTree() || job.Tower.IsQueuedForDeletion())
                { Unregister(job.Tower); continue; }
                LastJobsVisited++;
                job.Tower.AdvanceWork(this, delta);
                if (_jobs.TryGetValue(job.Tower, out version) && version == job.Version) _waiting.Add(job);
            }
        }
        finally { _batch.Clear(); _advancing = false; if (GodotObject.IsInstanceValid(this)) SetPhysicsProcess(!_shutdown && _jobs.Count > 0); }
    }

    // =========================================================
    // Teardown drops strong references and cancels source clocks without activating unfinished structures.
    public override void _ExitTree()
    {
        _shutdown = true;
        foreach (TowerBase tower in _jobs.Keys.ToArray()) if (GodotObject.IsInstanceValid(tower)) tower.DetachConstruction(this);
        _jobs.Clear(); _waiting.Clear(); if (!_advancing) _batch.Clear(); SetPhysicsProcess(false);
    }
}
