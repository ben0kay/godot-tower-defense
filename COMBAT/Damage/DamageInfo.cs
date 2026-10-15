// Immutable base hit; defense coefficients are applied exactly once by DamageResolver.
using System;
public readonly record struct DamageInfo
{
    public float Amount { get; }
    public DamageType Type { get; }
    public float? ShieldCoefficientOverride { get; }
    public float? HullCoefficientOverride { get; }
    public float ShieldPenetration { get; }
    // =========================================================
    // Reject malformed payloads before any target state changes; zero is a valid no-op.
    public DamageInfo(float amount, DamageType type, float? shieldCoefficientOverride = null,
        float? hullCoefficientOverride = null, float shieldPenetration = 0)
    {
        if (!float.IsFinite(amount) || amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
        if (!Enum.IsDefined(type)) throw new ArgumentOutOfRangeException(nameof(type));
        ValidateCoefficient(shieldCoefficientOverride, nameof(shieldCoefficientOverride));
        ValidateCoefficient(hullCoefficientOverride, nameof(hullCoefficientOverride));
        if (!float.IsFinite(shieldPenetration) || shieldPenetration < 0 || shieldPenetration > 1)
            throw new ArgumentOutOfRangeException(nameof(shieldPenetration));
        Amount = amount; Type = type;
        ShieldCoefficientOverride = shieldCoefficientOverride; HullCoefficientOverride = hullCoefficientOverride;
        ShieldPenetration = shieldPenetration;
    }
    // =========================================================
    // Null inherits the type default; an explicit zero remains a real override.
    private static void ValidateCoefficient(float? value, string name)
    {
        if (value.HasValue && (!float.IsFinite(value.Value) || value.Value < 0))
            throw new ArgumentOutOfRangeException(name);
    }
}
