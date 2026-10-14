// Immutable base hit; defense coefficients are applied exactly once by DamageResolver.
using System;
public readonly record struct DamageInfo
{
    public float Amount { get; }
    public DamageType Type { get; }
    // =========================================================
    // Reject malformed payloads before any target state changes; zero is a valid no-op.
    public DamageInfo(float amount, DamageType type)
    {
        if (!float.IsFinite(amount) || amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
        if (!Enum.IsDefined(type)) throw new ArgumentOutOfRangeException(nameof(type));
        Amount = amount; Type = type;
    }
}
