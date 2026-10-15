// Immutable weapon response travels with the selected level and cached combat snapshot.
public readonly record struct WeaponDamageStats(DamageType Type, bool OverrideShield, float ShieldCoefficient,
    bool OverrideHull, float HullCoefficient)
{
    // =========================================================
    // Preserve optional absolute overrides and penetration in the shared damage resolver.
    public DamageInfo CreateHit(float amount, float penetration, DamageSource source = null) => new(amount, Type,
        OverrideShield ? ShieldCoefficient : null, OverrideHull ? HullCoefficient : null, penetration, source);
}
