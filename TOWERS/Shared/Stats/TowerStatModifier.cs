// Future modifier data shape only; no modifier application or timed-effect gameplay in Phase 3A.
public enum TowerStatId { Range, FireRate, Damage, ProjectileSpeed, RotationSpeed, AimTolerance, SearchInterval }
public enum TowerStatOperation { Flat, AdditivePercent, Multiplier }
public readonly record struct TowerStatModifier(TowerStatId Stat, TowerStatOperation Operation, float Amount, string SourceId);
