// N4-only analytic swept-circle supplement prevents grazing corners between existing clearance samples.
using Godot;
using System;

public static class CorridorClearance
{
    // =========================================================
    // Preserve existing grid-corner rules, then check the entire swept disc against nearby solid rectangles.
    public static bool ConnectionClear(NavigationGrid nav, Vector2 from, Vector2 to, float radius)
    {
        if (!nav.ConnectionClear(from, to, radius)) return false;
        Vector2 a = nav.Grid.ToLocal(from), b = nav.Grid.ToLocal(to);
        float size = nav.Grid.Config.CellSize;
        int left = Mathf.FloorToInt((Mathf.Min(a.X, b.X) - radius) / size), right = Mathf.FloorToInt((Mathf.Max(a.X, b.X) + radius) / size);
        int top = Mathf.FloorToInt((Mathf.Min(a.Y, b.Y) - radius) / size), bottom = Mathf.FloorToInt((Mathf.Max(a.Y, b.Y) + radius) / size);
        for (int y = top; y <= bottom; y++) for (int x = left; x <= right; x++)
            if (nav.Grid.BlocksEnemies(new(x, y)) && DistanceSquared(a, b, x * size, y * size, size) <= (double)radius * radius)
                return false;
        return true;
    }
    // =========================================================
    // Exact segment/rectangle distance: intersection, endpoint projections and rectangle-corner projections.
    private static double DistanceSquared(Vector2 a, Vector2 b, double x, double y, double size)
    {
        double enter = 0, exit = 1;
        if (Clip(a.X, b.X - a.X, x, x + size, ref enter, ref exit) &&
            Clip(a.Y, b.Y - a.Y, y, y + size, ref enter, ref exit)) return 0;
        double result = Math.Min(PointBox(a, x, y, size), PointBox(b, x, y, size));
        result = Math.Min(result, PointSegment(x, y, a, b)); result = Math.Min(result, PointSegment(x + size, y, a, b));
        result = Math.Min(result, PointSegment(x, y + size, a, b)); return Math.Min(result, PointSegment(x + size, y + size, a, b));
    }
    // =========================================================
    // Slab interval intersection handles parallel lines without dividing by zero.
    private static bool Clip(double origin, double direction, double low, double high, ref double enter, ref double exit)
    {
        if (Math.Abs(direction) < 1e-12) return origin >= low && origin <= high;
        double first = (low - origin) / direction, second = (high - origin) / direction;
        if (first > second) (first, second) = (second, first);
        enter = Math.Max(enter, first); exit = Math.Min(exit, second); return enter <= exit;
    }
    // =========================================================
    // Endpoint distance to the closest point on a solid cell.
    private static double PointBox(Vector2 point, double x, double y, double size)
    { double dx = point.X - Math.Clamp(point.X, x, x + size), dy = point.Y - Math.Clamp(point.Y, y, y + size); return dx * dx + dy * dy; }
    // =========================================================
    // Double intermediates retain precision at near-tangent corners and zero-length segments.
    private static double PointSegment(double x, double y, Vector2 a, Vector2 b)
    {
        double dx = (double)b.X - a.X, dy = (double)b.Y - a.Y, length = dx * dx + dy * dy;
        double t = length > 0 ? Math.Clamp(((x - a.X) * dx + (y - a.Y) * dy) / length, 0, 1) : 0;
        double px = x - (a.X + t * dx), py = y - (a.Y + t * dy); return px * px + py * py;
    }
}
