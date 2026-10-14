// Per-world living enemy list, shared by targeting, spawning and placement checks.
using Godot;
using System;
using System.Collections.Generic;

public partial class EnemyRegistry : Node
{
    private readonly List<Enemy> _living = new();
    public IReadOnlyList<Enemy> Living => _living;
    public int KilledCount { get; private set; }
    public int ArrivedCount { get; private set; }
    public event Action<Enemy> EnemyKilled;
    public event Action<Enemy> EnemyArrived;

    // =========================================================
    // Register once; enemies unregister immediately on death or arrival.
    public void Register(Enemy enemy) { if (!_living.Contains(enemy)) _living.Add(enemy); }

    // =========================================================
    // Exit-tree cleanup is idempotent and does not count an outcome twice.
    public void Unregister(Enemy enemy) => _living.Remove(enemy);

    // =========================================================
    // Emit kill-only notifications after an enemy leaves the living registry.
    // Enemy.Finish is guarded against repeated damage, so each ship pays at most once.
    public void RecordOutcome(bool arrived, Enemy enemy)
    {
        if (arrived) { ArrivedCount++; EnemyArrived?.Invoke(enemy); return; }
        KilledCount++;
        EnemyKilled?.Invoke(enemy);
    }
}
