// Immutable resolution inputs; future stage sources remain empty until their systems exist.
using System.Collections.Immutable;
public readonly record struct TowerStatContext(TowerStatBlock BaseStats,
    ImmutableArray<TowerStatModifier> UpgradeModifiers = default);
