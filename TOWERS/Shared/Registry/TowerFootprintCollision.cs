// Inclusive swept point-versus-rectangle geometry; double arithmetic prevents fast bullets tunnelling through footprints.
using Godot;
using System;

public static class TowerFootprintCollision
{
    #region Segment Geometry
    // =========================================================
    // Slab clipping returns the first contact fraction, including a start inside, tangencies and zero-length segments.
    public static bool SegmentRect(Vector2 from, Vector2 to, Rect2 rect, out double fraction)
    {
        fraction = 0;
        if (!from.IsFinite() || !to.IsFinite() || !rect.Position.IsFinite() || !rect.End.IsFinite()
            || rect.Size.X < 0 || rect.Size.Y < 0) return false;
        double enter = 0, exit = 1;
        if (!Axis(from.X, (double)to.X - from.X, rect.Position.X, rect.End.X, ref enter, ref exit)
            || !Axis(from.Y, (double)to.Y - from.Y, rect.Position.Y, rect.End.Y, ref enter, ref exit)) return false;
        fraction = enter; return true;
    }
    // =========================================================
    // Exact parallel segments only intersect when their fixed coordinate lies inside the inclusive slab.
    private static bool Axis(double origin, double direction, double low, double high, ref double enter, ref double exit)
    {
        if (direction == 0) return origin >= low && origin <= high;
        double a = (low - origin) / direction, b = (high - origin) / direction;
        enter = Math.Max(enter, Math.Min(a, b)); exit = Math.Min(exit, Math.Max(a, b));
        return enter <= exit;
    }
    #endregion
}
