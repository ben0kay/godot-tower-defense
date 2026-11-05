// Cached four-neighbour terrain islands; pure data, reusable flood-fill buffers and no gameplay processing.
using System;
using System.Collections.Generic;

public sealed class PowerIslandMap
{
    private int _width, _height;
    private int[] _regions = Array.Empty<int>(), _queue = Array.Empty<int>();
    private readonly List<int> _sizes = new();
    internal int Width => _width;
    internal int Height => _height;
    internal ReadOnlySpan<int> Regions => _regions;
    public int IslandCount => _sizes.Count;
    public int ConductingCellCount { get; private set; }

    // =========================================================
    // Explicit terrain changes only: O(map cells) time, two reusable int arrays, no recursion or per-cell objects.
    public void Rebuild(int width, int height, ReadOnlySpan<bool> conducting)
    {
        if (width <= 0 || height <= 0 || (long)width * height != conducting.Length)
            throw new ArgumentException("Power island dimensions must match the conductivity cache.");
        int count = conducting.Length;
        if (_regions.Length != count) { _regions = new int[count]; _queue = new int[count]; }
        else Array.Clear(_regions);
        _width = width; _height = height; _sizes.Clear(); ConductingCellCount = 0;
        for (int seed = 0; seed < count; seed++)
        {
            if (!conducting[seed] || _regions[seed] != 0) continue;
            int region = _sizes.Count + 1, head = 0, tail = 0;
            Visit(seed, region, conducting, ref tail);
            while (head < tail)
            {
                int cell = _queue[head++], x = cell % width;
                if (x > 0) Visit(cell - 1, region, conducting, ref tail);
                if (x + 1 < width) Visit(cell + 1, region, conducting, ref tail);
                if (cell >= width) Visit(cell - width, region, conducting, ref tail);
                if (cell + width < count) Visit(cell + width, region, conducting, ref tail);
            }
            _sizes.Add(tail); ConductingCellCount += tail;
        }
    }

    // =========================================================
    // Mark on enqueue so every conducting cell occupies the queue at most once; diagonal contacts never join.
    private void Visit(int cell, int region, ReadOnlySpan<bool> conducting, ref int tail)
    {
        if (!conducting[cell] || _regions[cell] != 0) return;
        _regions[cell] = region; _queue[tail++] = cell;
    }

    // =========================================================
    // Zero denotes space/nonconducting/outside; positive IDs are deterministic for this snapshot, never save IDs.
    public int RegionAt(int x, int y) => x >= 0 && y >= 0 && x < _width && y < _height ? _regions[y * _width + x] : 0;
    public int CellCount(int region) => region > 0 && region <= _sizes.Count ? _sizes[region - 1] : 0;

    // =========================================================
    // Multi-cell structures must lie wholly on one conducting island; never bridge empty terrain by their artwork.
    public int RegionForFootprint(int x, int y, int width, int height)
    {
        if (x < 0 || y < 0 || width <= 0 || height <= 0 || x >= _width || y >= _height ||
            width > _width - x || height > _height - y) return 0;
        int region = RegionAt(x, y); if (region == 0) return 0;
        for (int row = y; row < y + height; row++) for (int column = x; column < x + width; column++)
            if (RegionAt(column, row) != region) return 0;
        return region;
    }
}
