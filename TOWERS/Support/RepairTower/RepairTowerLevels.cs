// RT1 complete absolute Level 1 row; purchased tiers and support XP are separate future passes.
using System.Collections.Generic;

public static class RepairTowerLevels
{
    // =========================================================
    // Shared level recipes own payment/work; Repair is an independent optional stat, never negative damage.
    public static List<TowerLevel> Create() => new()
    {
        new(1, 350, new TowerStatBlock(320, 0, 0, 0, 210, 12, .3f, MaxHealth: 1000, Repair: new(16)), 5)
    };
}
