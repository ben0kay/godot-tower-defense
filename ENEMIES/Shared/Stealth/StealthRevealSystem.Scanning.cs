// Capped heading refresh: diff one stable source against nearby enemies using retained query/membership buffers.
using Godot;

public partial class StealthRevealSystem
{
    // =========================================================
    // Require a real unit vector, never let malformed facing accidentally create radial coverage.
    private static bool ValidHeading(Vector2 heading) => heading.IsFinite() && Mathf.Abs(heading.LengthSquared() - 1) <= .001f;
    // =========================================================
    // Inclusive radius/cone; the epsilon handles coincident centres, not a passive detection bubble.
    private static bool InsideCone(Vector2 point, Area area)
    {
        Vector2 delta = point - area.Center;
        float squared = delta.LengthSquared();
        return squared <= area.RadiusSquared && (squared <= .000001f
            || delta.Dot(area.Forward) >= Mathf.Sqrt(squared) * area.CosineHalfAngle);
    }
    // =========================================================
    // Stationary enemies are sampled too. Old members absent from the query are explicitly removed.
    private void RefreshArea(Area area)
    {
        _registry.GetEnemiesInRadius(area.Center, area.Radius, _snapshot);
        _seen.Clear(); LastCandidates = _snapshot.Count;
        foreach (Enemy enemy in _snapshot)
            if (Eligible(enemy) && InsideCone(enemy.GlobalPosition, area)) _seen.Add(enemy);
        _expired.Clear();
        foreach (Enemy enemy in area.Enemies) if (!_seen.Contains(enemy)) _expired.Add(enemy);
        foreach (Enemy enemy in _expired) RemoveMembership(enemy, area);
        foreach (Enemy enemy in _snapshot) if (_seen.Contains(enemy)) AddMembership(enemy, area);
        _snapshot.Clear(); _expired.Clear(); _seen.Clear();
    }
}
