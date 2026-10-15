// Small shared table validation/XP publication helper; combat, UI, wallet and progression stay separate.
using Godot;
using System;
using System.Collections.Generic;

public static class TowerRankController
{
    private static TowerMasterConfig _defaults;
    public static TowerMasterConfig DefaultRules => _defaults ??= GD.Load<TowerMasterConfig>("res://TOWERS/DefaultTowerConfig.tres");
    public static IReadOnlyList<TowerRankDefinition> StandardOnly { get; } = Array.AsReadOnly(new[] { new TowerRankDefinition(TowerRank.Standard, 0, 0, 0, 0) });

    // =========================================================
    // Validate once when a type's static table initializes; no per-hit table/resource validation scans.
    public static IReadOnlyList<TowerRankDefinition> Define(params TowerRankDefinition[] rows)
    {
        if (rows == null || rows.Length != 5) throw new ArgumentException("Ranks require all five named rows.");
        double previous = -1;
        for (int i = 0; i < rows.Length; i++)
        {
            var row = rows[i];
            if (row.Rank != (TowerRank)i || !double.IsFinite(row.TotalXp) || row.TotalXp <= previous
                || (i == 0 && row.TotalXp != 0) || !ValidBonus(row.DamageBonus) || !ValidBonus(row.RangeBonus) || !ValidBonus(row.HpBonus))
                throw new ArgumentException("Rank rows must have ordered names, increasing finite XP and nonnegative finite bonuses.");
            previous = row.TotalXp;
        }
        return Array.AsReadOnly((TowerRankDefinition[])rows.Clone());
    }

    // =========================================================
    // Nonfinite/negative Inspector multiplier edits disable that reward instead of corrupting progression.
    internal static double SafeMultiplier(float value) => float.IsFinite(value) && value >= 0 ? value : 0;
    // =========================================================
    // Keep unused HP metadata valid without creating a disconnected health system.
    private static bool ValidBonus(float value) => float.IsFinite(value) && value >= 0;

    // =========================================================
    // No processing loop: an actual attributed damage/kill calls this once with its combined XP award.
    public static void AwardExperience(TowerBase tower, double amount)
    {
        if (!GodotObject.IsInstanceValid(tower) || !tower.IsInsideTree() || tower.IsQueuedForDeletion()
            || !double.IsFinite(amount) || amount <= 0 || tower.AuthoredConfig == null) return;
        tower.ReceiveExperience(amount);
    }
}
