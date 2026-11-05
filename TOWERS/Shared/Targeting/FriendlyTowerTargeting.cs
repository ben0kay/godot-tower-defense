// Reusable damaged-friendly selection through the existing tower index; no healing or scheduling ownership.
using Godot;
using System;
using System.Collections.Generic;

public static class FriendlyTowerTargeting
{
    // =========================================================
    // Default friendly policy is LowestIntegrity; the caller owns/reuses scratch and staggers acquisition.
    public static TowerBase Find(TowerRegistry registry, TowerBase healer, float range, List<TowerBase> scratch)
    {
        if (scratch == null) throw new ArgumentNullException(nameof(scratch));
        scratch.Clear();
        if (!CanSearch(registry, healer, range)) return null;
        Vector2 origin = healer.GlobalPosition;
        registry.GetNearby(origin, range, scratch);
        TowerBase best = null; double score = double.PositiveInfinity; long order = long.MaxValue;
        try
        {
            foreach (TowerBase tower in scratch)
            {
                if (!ValidTarget(registry, healer, tower, origin, range)) continue;
                double integrity = (double)tower.Health / tower.MaxHealth;
                long serial = registry.RegistrationOrder(tower);
                if (integrity < score || (integrity == score && serial < order))
                { best = tower; score = integrity; order = serial; }
            }
            return best;
        }
        finally { scratch.Clear(); } // Retain capacity, release target references between queries/unloads.
    }

    // =========================================================
    // O(1) lock revalidation never reacquires or enumerates nearby towers; caller stops repair/beam on false.
    public static bool Valid(TowerRegistry registry, TowerBase healer, TowerBase target, float range) =>
        CanSearch(registry, healer, range) && ValidTarget(registry, healer, target, healer.GlobalPosition, range);

    // =========================================================
    // No cross-world searches, detached previews or disabled/busy healer; pause remains the caller's clock gate.
    private static bool CanSearch(TowerRegistry registry, TowerBase healer, float range) =>
        GodotObject.IsInstanceValid(registry) && registry.IsInsideTree() && !registry.IsQueuedForDeletion()
        && float.IsFinite(range) && range >= 0 && registry.Contains(healer)
        && !healer.IsBusy && healer.CanOperate && healer.GlobalPosition.IsFinite();

    // =========================================================
    // Target disables do not affect hull eligibility. Broadphase footprint overlap still requires root-in-range.
    private static bool ValidTarget(TowerRegistry registry, TowerBase healer, TowerBase target, Vector2 origin, float range) =>
        target != healer && registry.Contains(target) && !target.IsBusy
        && float.IsFinite(target.Health) && float.IsFinite(target.MaxHealth)
        && target.Health > 0 && target.Health < target.MaxHealth && target.GlobalPosition.IsFinite()
        && DistanceSquared(origin, target.GlobalPosition) <= (double)range * range;

    // =========================================================
    // Inclusive world-root radius with double intermediates prevents finite float-square overflow.
    private static double DistanceSquared(Vector2 a, Vector2 b)
    { double x = (double)a.X - b.X, y = (double)a.Y - b.Y; return x * x + y * y; }
}
