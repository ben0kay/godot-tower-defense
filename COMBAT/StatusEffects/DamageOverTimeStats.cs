// Immutable reusable periodic-damage capability; tower snapshots carry values, never mutable runtime clocks.
using System;
public readonly record struct DamageOverTimeStats(float DamagePerSecond, float DurationSeconds)
{
    public double TotalDamage => (double)DamagePerSecond * DurationSeconds;
    public static DamageOverTimeStats? Sanitize(DamageOverTimeStats? value) => value is { } stats
        && float.IsFinite(stats.DamagePerSecond) && stats.DamagePerSecond > 0
        && float.IsFinite(stats.DurationSeconds) && stats.DurationSeconds > 0 ? stats : null;
}

public static class StatusEffectRules
{
    // Extend this family mapping for future DoT identities; the runtime/tick implementation stays shared.
    public static bool IsDamageOverTime(StatusEffectId id) => id == StatusEffectId.Corruption;
    public const double DamageTickSeconds = 1;
}
