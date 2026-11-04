// Reusable immediate-target damage path; delegates all defense, XP and kill rules to Enemy.
using Godot;

public static class WeaponHitDelivery
{
    // =========================================================
    // A valid locked target takes one authoritative hit without allocating a projectile or raycast.
    public static void ApplyInstant(Enemy target, DamageInfo hit, StatusEffectPayload? effect = null,
        HullHitSparkController sparks = null, Vector2? muzzlePosition = null, int hullHitSparkCount = 0)
    {
        if (!GodotObject.IsInstanceValid(target) || !target.IsAlive || !target.IsInsideTree() || target.IsQueuedForDeletion()) return;
        EnemyRegistry registry = target.Registry;
        if (!GodotObject.IsInstanceValid(registry) || !registry.IsInsideTree() || !registry.Contains(target)) return;
        float hullBefore = target.Health;
        Vector2 center = target.GlobalPosition;
        Vector2 incoming = (center - (muzzlePosition ?? center + Vector2.Left)).Normalized();
        if (incoming.LengthSquared() < .0001f) incoming = Vector2.Right;
        float radius = Mathf.Max(2f, target.Definition?.RadiusPixels ?? 8f);
        Vector2 contact = center - incoming * radius;
        target.ApplyDamage(hit);
        // Instant delivery has no collision point; approximate the front hull along the muzzle ray.
        if (hit.Type == DamageType.Kinetic && hullHitSparkCount > 0 &&
            GodotObject.IsInstanceValid(target) && target.Health < hullBefore &&
            GodotObject.IsInstanceValid(sparks))
            sparks.Spawn(contact, incoming, hullHitSparkCount);
        if (effect.HasValue && GodotObject.IsInstanceValid(target) && target.IsAlive && !target.IsQueuedForDeletion())
            target.ApplyStatus(effect.Value);
    }
}
