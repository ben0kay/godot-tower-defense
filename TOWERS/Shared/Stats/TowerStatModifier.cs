// Typed modifiers consumed by purchased tiers; projectile and optional beam capabilities share stages.
public enum TowerStatId { Range, FireRate, Damage, ProjectileSpeed, RotationSpeed, AimTolerance, SearchInterval, BeamDps, AreaRadius, AreaEdgeFraction }
public enum TowerStatOperation { Flat, AdditivePercent, Multiplier }
public readonly record struct TowerStatModifier(TowerStatId Stat, TowerStatOperation Operation, float Amount, string SourceId);
