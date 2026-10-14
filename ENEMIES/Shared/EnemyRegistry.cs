// Per-world living enemy list, shared by targeting, spawning and placement checks.
using Godot;
using System.Collections.Generic;

public partial class EnemyRegistry : Node
{
    private readonly List<Enemy> _living = new();
    public IReadOnlyList<Enemy> Living => _living;
    public int KilledCount { get; private set; }
    public int ArrivedCount { get; private set; }

    // =========================================================
    // Register once; enemies unregister immediately on death or arrival.
    public void Register(Enemy enemy) { if (!_living.Contains(enemy)) _living.Add(enemy); }

    // =========================================================
    // Exit-tree cleanup is idempotent and does not count an outcome twice.
    public void Unregister(Enemy enemy) => _living.Remove(enemy);

    // =========================================================
    // Keep simple debug totals without adding an economy or objective-health system.
    public void RecordOutcome(bool arrived)
    {
        if (arrived) ArrivedCount++; else KilledCount++;
    }
}
