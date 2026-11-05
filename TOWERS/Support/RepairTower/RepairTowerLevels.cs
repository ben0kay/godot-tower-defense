// Three complete absolute paid tiers for Restoration Tower; support repair is not negative damage.
using System.Collections.Generic;

public static class RepairTowerLevels
{
    // =========================================================
    // Each purchased level owns its final stats, payment and construction time.
    // Range and repair throughput grow modestly, and max hull gains 5% per tier.
    public static List<TowerLevel> Create() => new()
    {
        new(1, 350, new TowerStatBlock(320, 0, 0, 0, 210, 12, .3f,
            MaxHealth: 1000, Repair: new(16)), 5),
        new(2, 225, new TowerStatBlock(344, 0, 0, 0, 210, 12, .3f,
            MaxHealth: 1050, Repair: new(20)), 6),
        new(3, 325, new TowerStatBlock(368, 0, 0, 0, 210, 12, .3f,
            MaxHealth: 1102.5f, Repair: new(24)), 7)
    };
}
