// Stateless instant-disc selection and linear base-damage falloff; no weapon balances or visuals.
using Godot;
using System;
using System.Collections.Generic;

public static class AreaDamage
{
    // =========================================================
    // Validate every supplied area value before any recipient can be mutated.
    public static void Validate(float radius, float edgeFraction)
    {
        if (!float.IsFinite(radius) || radius < 0) throw new ArgumentOutOfRangeException(nameof(radius));
        if (!float.IsFinite(edgeFraction) || edgeFraction < 0 || edgeFraction > 1)
            throw new ArgumentOutOfRangeException(nameof(edgeFraction));
    }

    // =========================================================
    // Zero radius hits only the exact center; outside distance returns a zero-amount payload.
    public static DamageInfo AtDistance(DamageInfo source, float distance, float radius, float edgeFraction)
    {
        Validate(radius, edgeFraction);
        if (!float.IsFinite(distance) || distance < 0) throw new ArgumentOutOfRangeException(nameof(distance));
        double scale = distance > radius ? 0 : radius == 0 ? 1
            : 1 - (1 - (double)edgeFraction) * Math.Clamp((double)distance / radius, 0, 1);
        return new DamageInfo((float)(source.Amount * scale), source.Type, source.ShieldCoefficientOverride,
            source.HullCoefficientOverride, source.ShieldPenetration);
    }

    // =========================================================
    // One scan per impact, then dispatch each snapshotted hit once despite kill/removal callbacks.
    // EnemyRegistry guarantees uniqueness. The return value counts eligible dispatched hits, including zero damage.
    public static int ApplyInstant(Vector2 impact, float radius, DamageInfo source, float edgeFraction, EnemyRegistry registry)
    {
        Validate(radius, edgeFraction);
        if (!float.IsFinite(impact.X) || !float.IsFinite(impact.Y)) throw new ArgumentOutOfRangeException(nameof(impact));
        if (!GodotObject.IsInstanceValid(registry)) throw new ArgumentNullException(nameof(registry));
        List<(Enemy Enemy, DamageInfo Hit)> recipients = new();
        double radiusSquared = (double)radius * radius;
        foreach (Enemy enemy in registry.Living)
        {
            if (!Eligible(enemy, registry)) continue;
            Vector2 position = enemy.GlobalPosition;
            double dx = (double)position.X - impact.X, dy = (double)position.Y - impact.Y;
            double squared = dx * dx + dy * dy;
            if (!double.IsFinite(squared) || squared > radiusSquared) continue;
            recipients.Add((enemy, AtDistance(source, (float)Math.Sqrt(squared), radius, edgeFraction)));
        }
        int applied = 0;
        foreach (var recipient in recipients)
        {
            if (!Eligible(recipient.Enemy, registry)) continue;
            recipient.Enemy.ApplyDamage(recipient.Hit); applied++;
        }
        return applied;
    }

    // =========================================================
    // Native validity, scene state and membership can change during an earlier target's death callback.
    private static bool Eligible(Enemy enemy, EnemyRegistry registry) => GodotObject.IsInstanceValid(registry)
        && GodotObject.IsInstanceValid(enemy) && enemy.IsAlive && enemy.IsInsideTree()
        && !enemy.IsQueuedForDeletion() && registry.Contains(enemy);
}
