// Single deterministic shield/base-spillover/hull calculation; no gameplay events or visuals.
using Godot;
using System;
public readonly record struct DamageResult(float Shield, float Health);
public static class DamageResolver
{
    private static DamageResponseProfile _default;
    public static DamageResponseProfile DefaultProfile => _default ??= LoadDefault();
    // =========================================================
    // Resolve the shared authored profile once rather than loading a resource on each hit.
    private static DamageResponseProfile LoadDefault()
    {
        var profile = GD.Load<DamageResponseProfile>("res://COMBAT/Damage/DefaultDamageProfile.tres");
        if (profile == null) throw new InvalidOperationException("Default damage profile is missing.");
        profile.Validate(); return profile;
    }
    // =========================================================
    // Typed hits remain base units until each defense layer consumes its own share.
    public static DamageResult Resolve(DamageInfo hit, float shield, float health, DamageResponseProfile profile = null)
    {
        var coefficients = (profile ?? DefaultProfile).Coefficients(hit.Type);
        return ResolveBase(hit.Amount, shield, health, coefficients.Shield, coefficients.Hull);
    }
    // =========================================================
    // Legacy callers explicitly use 1:1 behavior, independent of authored type coefficients.
    public static DamageResult ResolveLegacy(float amount, float shield, float health) => ResolveBase(amount, shield, health, 1, 1);
    // =========================================================
    // Double intermediates prevent overflow; a zero shield coefficient blocks without bypass.
    private static DamageResult ResolveBase(float amount, float shield, float health, float shieldScale, float hullScale)
    {
        if (!float.IsFinite(amount) || amount < 0 || !float.IsFinite(shield) || shield < 0 || !float.IsFinite(health) || health < 0)
            throw new ArgumentOutOfRangeException(nameof(amount), "Damage/state must be finite and nonnegative.");
        double remaining = amount, nextShield = shield;
        if (shield > 0)
        {
            if (shieldScale == 0) return new(shield, health);
            double consumed = Math.Min(remaining, (double)shield / shieldScale);
            nextShield = Math.Max(0, shield - consumed * shieldScale);
            remaining -= consumed;
        }
        return new((float)nextShield, (float)Math.Max(0, health - remaining * hullScale));
    }
}
