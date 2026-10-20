// Immutable deployment input to Stage04; no Resource references, I/O or per-frame progression lookups.
public readonly record struct TowerResearchBonuses(double DamagePercent = 0, double ProjectileSpreadReductionFraction = 0, bool UnlockHomingProjectiles = false);
