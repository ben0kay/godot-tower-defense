// Named earned ranks and explicit current-rank totals; HP is reserved until real tower health exists.
public enum TowerRank { Standard, Veteran, Enhanced, Superior, Legendary }
public readonly record struct TowerRankDefinition(TowerRank Rank, double TotalXp, float DamageBonus,
    float RangeBonus, float HpBonus);
