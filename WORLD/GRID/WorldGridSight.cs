// Allocation-free weapon sight shared by friendly towers and enemy mounts over explicit cached terrain metadata, independent of navigation/art.
using Godot;
using System;

public partial class WorldGrid
{
    private int[] _sightBlockerPrefix;
    private const double CornerTolerance = 1e-10;

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
        // Near an endpoint corner, the traversal can consume an extra boundary in its tie branch.
        // Defer to that traversal whenever either moving axis could cross near the endpoint.
        if (_sightBlockerPrefix != null && EndpointClearOfBoundary(end.X, endX, dx)
            && EndpointClearOfBoundary(end.Y, endY, dy) && SightCorridorEmpty(x, y, endX, endY)) return true;
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
            if (Math.Abs(nextX - nextY) <= CornerTolerance)
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
    // Reuse the allocation across terrain edits and publish it before terrain callbacks.
    private void RebuildSightBlockerPrefix()
    {
        long stride = (long)Config.WidthCells + 1;
        long length = stride * ((long)Config.HeightCells + 1);
        if (TowerSightBlockerCount == 0 || length > Array.MaxLength)
        {
            _sightBlockerPrefix = null;
            return;
        }

        int width = Config.WidthCells, pitch = (int)stride;
        if (_sightBlockerPrefix?.Length != length) _sightBlockerPrefix = new int[(int)length];
        int[] prefix = _sightBlockerPrefix;
        Array.Clear(prefix, 0, pitch);
        for (int y = 0; y < Config.HeightCells; y++)
        {
            int row = (y + 1) * pitch, terrainRow = y * width, blocked = 0;
            prefix[row] = 0;
            for (int x = 0; x < width; x++)
            {
                if (_towerSightBlocked[terrainRow + x]) blocked++;
                prefix[row + x + 1] = blocked + prefix[row - pitch + x + 1];
            }
        }
    }

    private static bool EndpointClearOfBoundary(float coordinate, int cell, double delta)
    {
        if (delta == 0) return true;
        double fraction = (double)coordinate - cell;
        // Leave room for traversal tie tolerance and accumulated floating-point rounding.
        double margin = 10 * CornerTolerance * Math.Max(1, Math.Abs(delta));
        return fraction > margin && 1 - fraction > margin;
    }

    private bool SightCorridorEmpty(int x, int y, int endX, int endY)
    {
        int left = Math.Max(0, Math.Min(x, endX) - 1);
        int top = Math.Max(0, Math.Min(y, endY) - 1);
        int right = Math.Min(Config.WidthCells, Math.Max(x, endX) + 2);
        int bottom = Math.Min(Config.HeightCells, Math.Max(y, endY) + 2);
        int stride = Config.WidthCells + 1;
        int[] prefix = _sightBlockerPrefix;
        int topRow = top * stride, bottomRow = bottom * stride;
        return prefix[bottomRow + right] - prefix[bottomRow + left]
            == prefix[topRow + right] - prefix[topRow + left];
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
