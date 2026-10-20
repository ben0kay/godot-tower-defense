// Stack-owned spatial visitor for physical projectile contact; no per-bullet enemy scans, result buffers or physics bodies.
using Godot;
using System;

public static class ProjectileEnemyContact
{
    #region Swept Contact
    // =========================================================
    // Conservative circle broadphase reuses the world point index; exact swept circles choose the earliest live contact.
    public static Enemy First(EnemyRegistry registry, Vector2 from, Vector2 to, out double fraction)
    {
        fraction = 0;
        if (!GodotObject.IsInstanceValid(registry) || !registry.IsInsideTree() || registry.IsQueuedForDeletion()
            || !from.IsFinite() || !to.IsFinite()) return null;
        Vector2 center = new((float)(((double)from.X + to.X) * .5), (float)(((double)from.Y + to.Y) * .5));
        double dx = (double)to.X - from.X, dy = (double)to.Y - from.Y;
        double radius = Math.Sqrt(dx * dx + dy * dy) * .5 + registry.ProjectileRadiusPadding + 3;
        // Cover float narrowing at the broadphase center without changing exact collision geometry.
        radius += Math.Max(Math.Abs(center.X), Math.Abs(center.Y)) * 1e-6 + .001;
        if (radius > float.MaxValue) return null;
        Visitor visitor = new() { From = from, To = to, Best = double.PositiveInfinity, Order = ulong.MaxValue };
        registry.VisitEnemiesInRadius(center, (float)radius, ref visitor);
        fraction = visitor.Best; return visitor.Target;
    }
    private struct Visitor : EnemySpatialIndex.INeighborVisitor
    {
        public Vector2 From, To;
        public Enemy Target;
        public double Best;
        public ulong Order;
        // =========================================================
        // Hidden enemies remain physical recipients; target-selection stealth filtering is intentionally not used here.
        public void Accept(EnemySpatialIndex.Neighbor neighbor)
        {
            Enemy enemy = neighbor.Enemy;
            if (!GodotObject.IsInstanceValid(enemy) || !enemy.IsAlive || enemy.IsQueuedForDeletion()
                || !float.IsFinite(enemy.Definition.RadiusPixels) || enemy.Definition.RadiusPixels < 0
                || !SegmentCircle(From, To, neighbor.Position, enemy.Definition.RadiusPixels + 3, out double entry)) return;
            if (entry < Best || (entry == Best && neighbor.Order < Order))
            { Target = enemy; Best = entry; Order = neighbor.Order; }
        }
    }
    // =========================================================
    // Inclusive entry fraction handles tangencies, starts inside and high-speed traversal without endpoint tunnelling.
    public static bool SegmentCircle(Vector2 from, Vector2 to, Vector2 center, float radius, out double fraction)
    {
        fraction = 0;
        if (!from.IsFinite() || !to.IsFinite() || !center.IsFinite() || !float.IsFinite(radius) || radius < 0) return false;
        double x = (double)from.X - center.X, y = (double)from.Y - center.Y;
        double r2 = (double)radius * radius, c = x * x + y * y - r2;
        if (c <= 0) return true;
        double dx = (double)to.X - from.X, dy = (double)to.Y - from.Y, a = dx * dx + dy * dy;
        if (a == 0) return false;
        double b = x * dx + y * dy, discriminant = b * b - a * c;
        if (discriminant < 0) return false;
        fraction = (-b - Math.Sqrt(discriminant)) / a;
        return fraction >= 0 && fraction <= 1;
    }
    #endregion
}
