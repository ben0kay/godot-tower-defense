// Cached merged grid-edge segments, including internal holes; rebuilt only when power terrain changes.
using Godot;
using System.Collections.Generic;

internal sealed class IslandPowerBoundaryCache
{
    internal readonly Dictionary<int, Vector2[]> Lines = new();
    internal int Revision = -1;
    internal void Rebuild(PowerGridManager power)
    {
        Lines.Clear();
        var pieces = new Dictionary<int, List<Vector2>>();
        int width = power.Grid.Config.WidthCells, height = power.Grid.Config.HeightCells;
        float size = power.Grid.Config.CellSize;
        // Merge adjacent collinear edges into one retained segment instead of drawing every cell.
        for (int axis = 0; axis < 2; axis++)
        {
            int across = axis == 0 ? width : height, rows = axis == 0 ? height : width;
            for (int row = 0; row <= rows; row++)
            {
                int current = 0, start = 0;
                for (int at = 0; at <= across; at++)
                {
                    int region = 0;
                    if (at < across)
                    {
                        Vector2I cell = axis == 0 ? new(at, row) : new(row, at);
                        int a = power.RegionAt(cell), b = power.RegionAt(cell + (axis == 0 ? Vector2I.Up : Vector2I.Left));
                        if (a != b) region = a > 0 ? a : b;
                    }
                    if (region == current) continue;
                    if (current > 0)
                    {
                        if (!pieces.TryGetValue(current, out var points)) pieces[current] = points = new();
                        points.Add((axis == 0 ? new Vector2(start, row) : new Vector2(row, start)) * size);
                        points.Add((axis == 0 ? new Vector2(at, row) : new Vector2(row, at)) * size);
                    }
                    current = region; start = at;
                }
            }
        }
        foreach (var pair in pieces) Lines[pair.Key] = pair.Value.ToArray();
        Revision = power.Revision;
    }
}
