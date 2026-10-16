// Reusable upgrade transaction through the existing session wallet; stat stages never charge money.
using Godot;
using System;

public static class TowerUpgradeController
{
    // =========================================================
    // Validate complete consecutive rows before resolving or paying; missing level steps fail.
    public static void Validate(System.Collections.Generic.IReadOnlyList<TowerLevel> levels)
    {
        if (levels == null) throw new ArgumentException("Tower levels are missing.");
        for (int i = 0; i < levels.Count; i++)
        {
            TowerLevel level = levels[i];
            if (level != null) TowerConfig.ValidateWorkSeconds(level.WorkSeconds);
            if (level == null || level.Level != i + 1 || level.Costs == null)
                throw new ArgumentException("Tower levels must form consecutive complete rows starting at 1.");
        }
    }

    // =========================================================
    // An expected current level protects stale requests from purchasing the same tier twice.
    public static bool TryUpgrade(TowerBase tower, EconomyManager wallet, TowerMasterConfig rules,
        int expectedLevel, out string failure)
    {
        failure = "";
        if (!GodotObject.IsInstanceValid(tower) || !tower.IsInsideTree() || tower.IsQueuedForDeletion() || tower.GetTree().Paused || !GodotObject.IsInstanceValid(wallet) || !wallet.IsInsideTree() || rules?.AllowUpgrades != true)
        { failure = "Upgrades are unavailable."; return false; }
        if (tower.IsBusy) { failure = "Building is busy; wait for current work to finish."; return false; }
        if (tower.UpgradeLevel != expectedLevel) { failure = "Tower level changed; select the current upgrade."; return false; }
        TowerLevel next;
        TowerFinalStats preview;
        try
        {
            Validate(tower.Levels); next = tower.NextUpgrade;
            if (next == null) { failure = "Maximum level reached."; return false; }
            preview = tower.ResolveLevel(expectedLevel + 1);
        }
        catch (Exception exception) { failure = exception.Message; return false; }
        bool instant = next.WorkSeconds == 0;
        if (!wallet.TryPurchase(next.Costs, () => tower.NextUpgrade == next && tower.BeginUpgrade(expectedLevel, next, preview)))
        { failure = "Insufficient resources or tower changed; no upgrade purchased."; return false; }
        if (GodotObject.IsInstanceValid(tower)) tower.PublishUpgradePurchase(instant);
        return true;
    }
}
