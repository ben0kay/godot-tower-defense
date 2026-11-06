// Complete absolute Corruptor purchased tiers; all launch damage/status numbers are authored here.
using System.Collections.Generic;
public static class CorruptorTowerLevels
{
    public static List<TowerLevel> Create() => new()
    {
        Row(1, 300, 4, 8, 3, 8, .6f, 256, 1000),
        Row(2, 225, 5, 10, 4, 9, .65f, 272, 1050),
        Row(3, 325, 6, 12, 5, 10, .7f, 288, 1102.5f)
    };
    // =========================================================
    // Energy hull hit uses explicit 1:1 hull response and full shield bypass; DoT has its own identity.
    private static TowerLevel Row(int level, int credits, double work, int hit, float dps, float duration, float rate, float range, float health) =>
        new(level, credits, new TowerStatBlock(range, rate, hit, 700, 216, 4, .15f,
            ShieldPenetration: 1, Weapon: new(DamageType.Energy, false, 1, true, 1),
            Ammunition: new(600, 1), MaxHealth: health, Handling: new(.5f), Corruption: new(dps, duration)), work);
}
