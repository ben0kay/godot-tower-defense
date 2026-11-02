// Reusable immediate-target damage path; delegates all defense, XP and kill rules to Enemy.
using Godot;

public static class WeaponHitDelivery
{
    // =========================================================
    // A valid locked target takes one authoritative hit without allocating a projectile or raycast.
    public static void ApplyInstant(Enemy target, DamageInfo hit, StatusEffectPayload? effect = null)
    {
        if (!GodotObject.IsInstanceValid(target) || !target.IsAlive || !target.IsInsideTree() || target.IsQueuedForDeletion()) return;
        EnemyRegistry registry = target.Registry;
        if (!GodotObject.IsInstanceValid(registry) || !registry.IsInsideTree() || !registry.Contains(target)) return;
        target.ApplyDamage(hit);
        if (effect.HasValue && GodotObject.IsInstanceValid(target) && target.IsAlive && !target.IsQueuedForDeletion())
            target.ApplyStatus(effect.Value);
    }
}
