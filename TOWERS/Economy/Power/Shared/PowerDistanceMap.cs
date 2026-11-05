// Pure multi-source conductive-distance field; one iterative BFS and two retained int arrays per map size.
using System;
using System.Collections.Generic;

public sealed class PowerDistanceMap
{
    public readonly record struct Anchor(int X, int Y, int Width, int Height);
    private int _width, _height;
    private int[] _distance = Array.Empty<int>(), _queue = Array.Empty<int>();
    public int ReachedCells { get; private set; }
    // =========================================================
    // Seed ALL valid footprint cells at zero; overlapping sources enter the queue only once, independent of order.
    public void Rebuild(int width, int height, ReadOnlySpan<int> regions, IReadOnlyList<Anchor> anchors)
    {
        if (width <= 0 || height <= 0 || (long)width * height != regions.Length)
            throw new ArgumentException("Distance field must match the island snapshot.");
        if (anchors == null) throw new ArgumentNullException(nameof(anchors));
        int count = regions.Length;
        if (_distance.Length != count) { _distance = new int[count]; _queue = new int[count]; }
        Array.Fill(_distance, -1); _width = width; _height = height;
        int tail = 0;
        for (int i = 0; i < anchors.Count; i++)
        {
            Anchor anchor = anchors[i];
            if (!Valid(anchor, regions)) continue;
            for (int y = anchor.Y; y < anchor.Y + anchor.Height; y++) for (int x = anchor.X; x < anchor.X + anchor.Width; x++)
            { int cell = y * width + x; if (_distance[cell] < 0) { _distance[cell] = 0; _queue[tail++] = cell; } }
        }
        int head = 0;
        while (head < tail)
        {
            int cell = _queue[head++], x = cell % width;
            if (x > 0) Visit(cell - 1, cell, regions, ref tail);
            if (x + 1 < width) Visit(cell + 1, cell, regions, ref tail);
            if (cell >= width) Visit(cell - width, cell, regions, ref tail);
            if (cell + width < count) Visit(cell + width, cell, regions, ref tail);
        }
        ReachedCells = tail;
    }
    // =========================================================
    // A source may not bridge holes, borders or distinct islands through a partially valid footprint.
    private bool Valid(Anchor a, ReadOnlySpan<int> regions)
    {
        if (a.X < 0 || a.Y < 0 || a.Width <= 0 || a.Height <= 0 || a.X >= _width || a.Y >= _height
            || a.Width > _width - a.X || a.Height > _height - a.Y) return false;
        int region = regions[a.Y * _width + a.X]; if (region == 0) return false;
        for (int y = a.Y; y < a.Y + a.Height; y++) for (int x = a.X; x < a.X + a.Width; x++)
            if (regions[y * _width + x] != region) return false;
        return true;
    }
    // =========================================================
    // Mark on enqueue: each reachable cell is processed once, no diagonals, row wrapping, recursion or AStar.
    private void Visit(int next, int from, ReadOnlySpan<int> regions, ref int tail)
    {
        if (_distance[next] >= 0 || regions[next] == 0 || regions[next] != regions[from]) return;
        _distance[next] = _distance[from] + 1; _queue[tail++] = next;
    }
    public int DistanceAt(int x, int y) => x >= 0 && y >= 0 && x < _width && y < _height ? _distance[y * _width + x] : -1;
}
