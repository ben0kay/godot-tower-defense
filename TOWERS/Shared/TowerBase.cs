// Lightweight placed-tower progression and effective snapshots; no mandatory targeting/firing loop.
using Godot;
using System;

public abstract partial class TowerBase : Node2D
{
    public int UpgradeLevel { get; private set; } = 1;
    public TowerFinalStats FinalStats { get; protected set; }
    public event Action StatsChanged;
    public abstract Godot.Collections.Array<TowerUpgradeTier> UpgradeTiers { get; }
    public TowerUpgradeTier NextUpgrade => UpgradeTiers != null && UpgradeLevel - 1 < UpgradeTiers.Count
        ? UpgradeTiers[UpgradeLevel - 1] : null;

    // =========================================================
    // Derived tower definitions supply their inputs; preview and application use the same resolver.
    public abstract TowerFinalStats ResolveLevel(int level);

    // =========================================================
    // Publish once after committed changes; wallet/controller retain payment ownership.
    internal void NotifyStatsChanged() => StatsChanged?.Invoke();

    // =========================================================
    // Commit pre-resolved data silently so observers see a fully paid upgrade, never partial state.
    internal bool CommitUpgrade(int expectedLevel, TowerFinalStats final)
    {
        if (!IsInsideTree() || UpgradeLevel != expectedLevel || final == null) return false;
        UpgradeLevel++; FinalStats = final;
        return true;
    }
}
