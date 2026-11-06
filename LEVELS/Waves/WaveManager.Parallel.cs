// Parallel wave batches share the manager's frame budget, clock, RNG and enemy ownership.
using System;

public partial class WaveManager
{
    private sealed class AttackGroup
    {
        public WaveBatch Batch;
        public WaveEnemyEntry[] Sequence;
        public int EntryIndex;
        public double Due;
        public bool WaitingForPortal, Blocked;
        public string Message = "";
        public bool Complete => EntryIndex >= Batch.Count;
    }
    private AttackGroup[] _parallelGroups;
    private double _parallelClock;
    private int _parallelCursor;

    // =========================================================
    // Allocate counters once; compose each group lazily on its first budgeted attempt.
    private void PrepareParallel()
    {
        var batches = _waves[_waveIndex].Batches;
        _parallelGroups = new AttackGroup[batches.Length];
        _parallelClock = 0; _parallelCursor = 0; _sequence = null;
        for (int i = 0; i < batches.Length; i++)
            _parallelGroups[i] = new AttackGroup { Batch = batches[i], Due = batches[i].StartOffsetSeconds };
    }

    // =========================================================
    // Round-robin ready groups: failures cost an attempt, but cannot monopolize the frame.
    // Due times reset from now, so missed intervals never build a catch-up backlog.
    private void AdvanceParallel(double delta, int limit)
    {
        _parallelClock += Math.Max(0, delta);
        bool changed = false;
        for (int attempt = 0; attempt < limit && !_outcomeStopped; attempt++)
        {
            AttackGroup group = null;
            for (int visited = 0; visited < _parallelGroups.Length; visited++)
            {
                var candidate = _parallelGroups[_parallelCursor];
                _parallelCursor = (_parallelCursor + 1) % _parallelGroups.Length;
                if (!candidate.Complete && !candidate.WaitingForPortal && candidate.Due <= _parallelClock + 0.0000001)
                { group = candidate; break; }
            }
            if (group == null) break;
            group.Sequence ??= WaveCatalog.Compose(group.Batch, _random);
            WaveEnemyEntry entry = group.Sequence[group.EntryIndex];
            Enemy ship;
            _acceptingSpawn = true;
            try
            {
                ship = _spawns.SpawnWave(_scenes[entry.EnemyId], _random, entry.Portal,
                    entry.SpawnWithShield, entry.SpawnWithStealth,
                    entry.Source ?? group.Batch.Source ?? _waves[_waveIndex].Source);
            }
            finally { _acceptingSpawn = false; }
            changed = true;
            if (ship == null)
            {
                group.WaitingForPortal = _spawns.WaitingForPortal;
                group.Blocked = !group.WaitingForPortal;
                group.Message = _spawns.LastMessage;
                group.Due = _parallelClock + Math.Max(0.1, _spawns.Paths.Config.WaveSpawnRetrySeconds);
                continue;
            }
            PlannedRemaining--; group.EntryIndex++;
            group.Blocked = false; group.Message = "";
            group.Due = _parallelClock + group.Batch.Interval;
            if (group.Complete) group.Sequence = null;
        }
        if (_outcomeStopped) return;
        RefreshParallelState(changed);
    }

    // =========================================================
    // Sleep only if every unfinished group is waiting for portal readiness.
    // A future start offset or blocked retry keeps the single pausable clock awake.
    private void RefreshParallelState(bool changed)
    {
        bool unfinished = false, awake = false, blocked = false;
        string message = "";
        foreach (var group in _parallelGroups)
        {
            if (group.Complete) continue;
            unfinished = true;
            awake |= !group.WaitingForPortal;
            blocked |= group.Blocked;
            if (message.Length == 0 && group.Message.Length > 0) message = group.Message;
        }
        var state = !unfinished ? WaveState.WaitingForClear : blocked ? WaveState.SpawnBlocked : WaveState.Spawning;
        changed |= State != state || StatusMessage != message;
        State = state; StatusMessage = message;
        SetPhysicsProcess(unfinished && awake);
        if (changed) Publish();
        if (!unfinished) CompleteIfClear();
    }

    // =========================================================
    // A readiness event retries retained entries under the normal next-frame budget.
    private void WakeParallelPortals()
    {
        if (State != WaveState.Spawning && State != WaveState.SpawnBlocked) return;
        bool changed = false;
        foreach (var group in _parallelGroups)
        {
            if (!group.WaitingForPortal || group.Complete) continue;
            group.WaitingForPortal = false; group.Due = _parallelClock;
            group.Message = ""; changed = true;
        }
        if (changed) RefreshParallelState(true);
    }
}
