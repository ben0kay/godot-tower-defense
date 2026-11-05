// Cached multi-source conductive distances: linear BFS locally, positive-hop Dijkstra for linked islands.
using System;
using System.Collections.Generic;

public sealed class PowerDistanceMap
{
    public readonly record struct Anchor(int X, int Y, int Width, int Height);
    public readonly record struct Link(Anchor A, Anchor B, int Cost);
    private readonly Dictionary<int, List<(int Cell, int Cost)>> _links = new();
    private readonly Stack<List<(int Cell, int Cost)>> _linkLists = new();
    private readonly PriorityQueue<int, int> _frontier = new();
    private int _width, _height;
    private int[] _distance = Array.Empty<int>(), _queue = Array.Empty<int>();
    public int ReachedCells { get; private set; }
    // =========================================================
    // Seed ALL valid footprint cells at zero; overlapping sources enter the queue only once, independent of order.
    public void Rebuild(int width, int height, ReadOnlySpan<int> regions, IReadOnlyList<Anchor> anchors, IReadOnlyList<Link> links = null)
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
        if (links != null && links.Count > 0)
        { RebuildLinked(regions, links, tail); return; }
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
    // =========================================================
    // Dirty-only multi-source Dijkstra: unit conductive segments plus positive configured link hops.
    private void RebuildLinked(ReadOnlySpan<int> regions, IReadOnlyList<Link> links, int seeds)
    {
        foreach (var list in _links.Values) { list.Clear(); _linkLists.Push(list); } _links.Clear(); _frontier.Clear();
        foreach (var link in links)
        {
            if (link.Cost < 1 || !Valid(link.A, regions) || !Valid(link.B, regions)) continue;
            // The first footprint cell is the deterministic electrical port (both current types are 1x1).
            // Validate the full footprint above, but never expand a link into a quadratic all-cell cross-product.
            int a = link.A.Y * _width + link.A.X, b = link.B.Y * _width + link.B.X;
            AddLink(a, b, link.Cost); AddLink(b, a, link.Cost);
        }
        for (int i = 0; i < seeds; i++) _frontier.Enqueue(_queue[i], 0);
        ReachedCells = seeds;
        while (_frontier.TryDequeue(out int cell, out int cost))
        {
            if (_distance[cell] != cost) continue;
            int x = cell % _width;
            if (x > 0 && regions[cell - 1] == regions[cell]) Relax(cell - 1, cost, 1);
            if (x + 1 < _width && regions[cell + 1] == regions[cell]) Relax(cell + 1, cost, 1);
            if (cell >= _width && regions[cell - _width] == regions[cell]) Relax(cell - _width, cost, 1);
            if (cell + _width < regions.Length && regions[cell + _width] == regions[cell]) Relax(cell + _width, cost, 1);
            if (_links.TryGetValue(cell, out var edges)) foreach (var edge in edges) Relax(edge.Cell, cost, edge.Cost);
        }
    }
    private void AddLink(int from, int to, int cost)
    {
        if (!_links.TryGetValue(from, out var list)) { list = _linkLists.Count > 0 ? _linkLists.Pop() : new(); _links[from] = list; }
        list.Add((to, cost));
    }
    private void Relax(int cell, int from, int hop)
    {
        long cost = (long)from + hop;
        if (cost >= int.MaxValue || _distance[cell] >= 0 && _distance[cell] <= cost) return;
        if (_distance[cell] < 0) ReachedCells++;
        _distance[cell] = (int)cost; _frontier.Enqueue(cell, (int)cost);
    }
    public int DistanceAt(int x, int y) => x >= 0 && y >= 0 && x < _width && y < _height ? _distance[y * _width + x] : -1;
}
