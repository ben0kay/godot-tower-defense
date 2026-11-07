// Common firing presentation adapter: one renderer, one launch position, no gameplay ownership.
using Godot;
public static class WeaponVisuals
{
    // =========================================================
    // Empty styles remain compatible; renderers have no idle processing and are owned by the rotating pivot.
    public static WeaponVisualRenderer Attach(Node2D pivot)
    { var renderer = new WeaponVisualRenderer { Name = "WeaponVisuals" }; pivot.AddChild(renderer); return renderer; }
    // =========================================================
    // A funded tower shot and flash sample the same barrel before cosmetic recoil.
    internal static void FireFriendly(Node2D projectiles, Marker2D muzzle, IProjectileVisualConfig config,
        WeaponVisualRenderer renderer, Enemy target, float speed, DamageInfo damage, StatusEffectPayload? effect,
        ProjectileHandlingStats? handling, bool shieldRicochet, int hullHitSparkCount)
    {
        ProjectileLaunch.FireFriendly(projectiles, target, speed, damage, muzzle.GlobalPosition, -muzzle.GlobalTransform.Y,
            effect, config?.ProjectileVisual, handling, shieldRicochet, hullHitSparkCount);
        renderer.FlashAt(muzzle, config?.MuzzleFlash);
    }
}
