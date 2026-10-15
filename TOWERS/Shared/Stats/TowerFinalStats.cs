// Immutable per-instance effective combat values; never a substitute for saved progression inputs.
public sealed record TowerFinalStats(float RangePixels, float ShotsPerSecond, int DamagePerShot, float ProjectileSpeed, float RotationSpeedDegrees, float AimToleranceDegrees, float TargetSearchInterval, BeamStats? Beam = null, float ShieldPenetration = 0, AreaBlastStats? Area = null);
