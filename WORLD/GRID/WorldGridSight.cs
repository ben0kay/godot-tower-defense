// Allocation-free weapon sight shared by friendly towers and enemy mounts over explicit cached terrain metadata, independent of navigation/art.
using Godot;
using System;

public partial class WorldGrid
{
    public int TowerSightBlockerCount { get; private set; }

    // =========================================================
    // Traverse only cells touched by this segment. Corner contact conservatively checks both sides.
    // No-blocker worlds return before transforms/traversal; placement occupancy never blocks sight.
    public bool HasWeaponLineOfSight(Vector2 from, Vector2 to)
    {
        if (!float.IsFinite(from.X) || !float.IsFinite(from.Y)
            || !float.IsFinite(to.X) || !float.IsFinite(to.Y)) return false;
        if (TowerSightBlockerCount == 0) return true;
        Vector2 start = ToLocal(from) / Config.CellSize, end = ToLocal(to) / Config.CellSize;
        int x = Mathf.FloorToInt(start.X), y = Mathf.FloorToInt(start.Y);
        int endX = Mathf.FloorToInt(end.X), endY = Mathf.FloorToInt(end.Y);
        if (!IsInsideMap(new(x, y)) || !IsInsideMap(new(endX, endY))) return false;
        double dx = (double)end.X - start.X, dy = (double)end.Y - start.Y;
        int stepX = Math.Sign(dx), stepY = Math.Sign(dy);
        bool onVertical = stepX == 0 && start.X == MathF.Floor(start.X);
        bool onHorizontal = stepY == 0 && start.Y == MathF.Floor(start.Y);
        double deltaX = stepX == 0 ? double.PositiveInfinity : 1 / Math.Abs(dx);
        double deltaY = stepY == 0 ? double.PositiveInfinity : 1 / Math.Abs(dy);
        double nextX = stepX == 0 ? double.PositiveInfinity : ((stepX > 0 ? x + 1 : x) - start.X) / dx;
        double nextY = stepY == 0 ? double.PositiveInfinity : ((stepY > 0 ? y + 1 : y) - start.Y) / dy;
        int budget = Config.WidthCells + Config.HeightCells + 2;
        while (budget-- > 0)
        {
            if (SightBlockedAt(x, y) || (onVertical && SightBlockedAt(x - 1, y))
                || (onHorizontal && SightBlockedAt(x, y - 1))
                || (onVertical && onHorizontal && SightBlockedAt(x - 1, y - 1))) return false;
            if (x == endX && y == endY) return true;
            if (Math.Abs(nextX - nextY) <= 1e-10)
            {
                if (SightBlockedAt(x + stepX, y) || SightBlockedAt(x, y + stepY)) return false;
                x += stepX; y += stepY; nextX += deltaX; nextY += deltaY;
            }
            else if (nextX < nextY) { x += stepX; nextX += deltaX; }
            else { y += stepY; nextY += deltaY; }
        }
        return false;
    }

    // =========================================================
    // Existing callers/assets can retain the old name; there is only one terrain cache and traversal.
    public bool HasTowerLineOfSight(Vector2 from, Vector2 to) => HasWeaponLineOfSight(from, to);
    // =========================================================
    // Map bounds are handled by the segment query; off-map neighbours have no authored obstruction.
    private bool SightBlockedAt(int x, int y) => x >= 0 && y >= 0
        && x < Config.WidthCells && y < Config.HeightCells
        && _towerSightBlocked[y * Config.WidthCells + x];
}
