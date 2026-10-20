// Named earned ranks and explicit current-rank totals; HP bonuses apply to maximum tower health.
public enum TowerRank { Standard, Veteran, Enhanced, Superior, Legendary }
public readonly record struct TowerRankDefinition(TowerRank Rank, double TotalXp, float DamageBonus,
    float RangeBonus, float HpBonus, float ProjectileSpreadReductionFraction = 0);
