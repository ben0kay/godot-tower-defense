// Optional projectile capability; spread is a launch half-angle, separate from aiming permission and rotation speed.
using Godot;
using System;

public enum ProjectileGuidance { Straight, Homing }
public readonly record struct ProjectileHandlingStats(float SpreadDegrees = 0,
    ProjectileGuidance Guidance = ProjectileGuidance.Straight, bool CanUnlockHoming = true)
{
    #region Handling Safety
    // =========================================================
    // Immutable pipeline/launch copies stay finite; absent handling leaves non-projectile capabilities untouched.
    public static ProjectileHandlingStats Sanitize(ProjectileHandlingStats stats) => stats with
    {
        SpreadDegrees = float.IsFinite(stats.SpreadDegrees) ? Mathf.Clamp(stats.SpreadDegrees, 0, 180) : 0,
        Guidance = Enum.IsDefined(stats.Guidance) ? stats.Guidance : ProjectileGuidance.Straight
    };
    // =========================================================
    // Rank totals are fractions, never cumulative reductions from previous tiers.
    public static float Fraction(float value) => float.IsFinite(value) ? Mathf.Clamp(value, 0, 1) : 0;
    #endregion
}
