// Reusable upgrade transaction through the existing session wallet; stat stages never charge money.
using Godot;
using System;

public static class TowerUpgradeController
{
    // =========================================================
    // Validate all tiers/operations before resolving or paying; duplicate/missing level steps fail.
    public static void Validate(Godot.Collections.Array<TowerUpgradeTier> tiers)
    {
        if (tiers == null) throw new ArgumentException("Upgrade tiers are missing.");
        for (int i = 0; i < tiers.Count; i++)
        {
            TowerUpgradeTier tier = tiers[i];
            if (tier == null || tier.Level != i + 2 || tier.Costs == null || tier.Modifiers == null)
                throw new ArgumentException("Upgrade tiers must form consecutive levels starting at 2.");
            foreach (TowerUpgradeModifier modifier in tier.Modifiers)
                if (modifier == null || !Enum.IsDefined(modifier.Stat) || !Enum.IsDefined(modifier.Operation)
                    || !float.IsFinite(modifier.Amount) || (modifier.Operation == TowerStatOperation.Multiplier && modifier.Amount < 0))
                    throw new ArgumentException("Upgrade contains an invalid stat operation.");
        }
    }

    // =========================================================
    // An expected current level protects stale requests from purchasing the same tier twice.
    public static bool TryUpgrade(TowerBase tower, EconomyManager wallet, TowerMasterConfig rules,
        int expectedLevel, out string failure)
    {
        failure = "";
        if (!GodotObject.IsInstanceValid(tower) || !tower.IsInsideTree() || wallet == null || rules?.AllowUpgrades != true)
        { failure = "Upgrades are unavailable."; return false; }
        if (tower.UpgradeLevel != expectedLevel) { failure = "Tower level changed; select the current upgrade."; return false; }
        TowerUpgradeTier next;
        TowerFinalStats preview;
        try
        {
            Validate(tower.UpgradeTiers); next = tower.NextUpgrade;
            if (next == null) { failure = "Maximum level reached."; return false; }
            preview = tower.ResolveLevel(expectedLevel + 1);
        }
        catch (Exception exception) { failure = exception.Message; return false; }
        if (!wallet.TryPurchase(next.Costs, () => tower.NextUpgrade == next && tower.CommitUpgrade(expectedLevel, preview)))
        { failure = "Insufficient resources or tower changed; no upgrade purchased."; return false; }
        tower.NotifyStatsChanged();
        return true;
    }
}
