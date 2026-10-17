// Immutable launch-time status values; permanent Slow needs no timer or live tower reference.
using System;

public readonly record struct StatusEffectPayload
{
    public StatusEffectId Id { get; }
    public float Strength { get; }
    // =========================================================
    // Reject malformed or unsupported effects before they reach an enemy.
    public StatusEffectPayload(StatusEffectId id, float strength)
    {
        if (!Enum.IsDefined(id)) throw new ArgumentOutOfRangeException(nameof(id));
        if (!float.IsFinite(strength) || strength <= 0 || strength >= 1)
            throw new ArgumentOutOfRangeException(nameof(strength));
        Id = id; Strength = strength;
    }
}
