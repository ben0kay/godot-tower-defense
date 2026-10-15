// World-owned point buckets and temporary swept bounds; no physics shapes or per-frame world rebuild.
using Godot;
using System;
using System.Collections.Generic;

internal sealed class EnemySpatialIndex
{
    internal sealed class Entry
    {
        public Enemy Enemy;
        public Vector2 Position;
        public ulong Order, Registered, Stamp;
        public readonly List<Motion> History = new();
        public int HistoryStart;
        public readonly List<(double X, double Y)> SweepCells = new();
        public bool Wide, HasSweep;
        public (double X, double Y) SweepLow, SweepHigh;
    }
    internal readonly record struct Motion(ulong Revision, Vector2 From, Vector2 To);
    internal readonly record struct Neighbor(Enemy Enemy, Vector2 Position, ulong Order);
    internal readonly record struct Sweep(Enemy Enemy, Vector2 Start, Vector2 End, ulong Order);
    private readonly Dictionary<Enemy, Entry> _entries = new();
    private readonly Dictionary<(double X, double Y), List<Entry>> _points = new(), _sweeps = new();
    private readonly Stack<List<Entry>> _buckets = new();
    private readonly HashSet<Entry> _wide = new(), _dirty = new();
    private readonly Queue<(Entry Entry, ulong Revision)> _changes = new();
    private readonly Dictionary<ulong, ulong> _readers = new();
    private ulong _revision, _order, _reader, _stamp;
    public float CellSize { get; }
    public int CandidatesExamined { get; private set; }

    // =========================================================
    // Cell size is a setup parameter, independent of weapon balance.
    public EnemySpatialIndex(float cellSize)
    {
        if (!float.IsFinite(cellSize) || cellSize <= 0) throw new ArgumentOutOfRangeException(nameof(cellSize));
        CellSize = cellSize;
    }

    // =========================================================
    // A new registration gets a new stable order, matching the authoritative living list.
    public void Add(Enemy enemy)
    {
        if (_entries.ContainsKey(enemy)) return;
        Entry entry = new() { Enemy = enemy, Position = enemy.GlobalPosition, Order = ++_order, Registered = ++_revision };
        _entries.Add(enemy, entry);
        if (entry.Position.IsFinite()) Insert(_points, Cell(entry.Position), entry);
    }

    // =========================================================
    // Remove both representations immediately, including stale movement history.
    public void Remove(Enemy enemy)
    {
        if (!_entries.Remove(enemy, out Entry entry)) return;
        if (entry.Position.IsFinite()) Remove(_points, Cell(entry.Position), entry);
        RemoveSweep(entry); entry.History.Clear(); entry.HistoryStart = 0;
    }

    // =========================================================
    // Called by actual transform changes, not an all-enemies maintenance scan.
    public void Move(Enemy enemy, Vector2 position)
    {
        if (!_entries.TryGetValue(enemy, out Entry entry) || entry.Position == position) return;
        Vector2 before = entry.Position;
        if (_readers.Count > 0)
        {
            ulong revision = ++_revision;
            entry.History.Add(new(revision, before, position)); _changes.Enqueue((entry, revision));
        }
        if (before.IsFinite() && (!position.IsFinite() || Cell(before) != Cell(position))) Remove(_points, Cell(before), entry);
        if (position.IsFinite() && (!before.IsFinite() || Cell(before) != Cell(position))) Insert(_points, Cell(position), entry);
        entry.Position = position;
        if (_readers.Count > 0) UpdateSweep(entry);
    }

    // =========================================================
    // Exact inclusive center-distance query. Invalid input rejects before changing the caller's list.
    public void Circle(Vector2 center, float radius, List<Enemy> results)
    {
        Validate(center, radius); results.Clear(); CandidatesExamined = 0;
        CircleVisitor visitor = new() { Center = center, Squared = (double)radius * radius, Results = results };
        Visit(_points, center, radius, ref visitor); CandidatesExamined = visitor.Count;
    }

    // =========================================================
    // Combat callers reuse cached position/order snapshots instead of reading native transforms twice.
    public void Neighbors(Vector2 center, float radius, List<Neighbor> results)
    {
        Validate(center, radius); results.Clear();
        NeighborVisitor visitor = new() { Center = center, Squared = (double)radius * radius, Results = results };
        Visit(_points, center, radius, ref visitor); CandidatesExamined = visitor.Count;
    }
    private struct NeighborVisitor : IVisitor
    {
        public Vector2 Center;
        public double Squared;
        public List<Neighbor> Results;
        public int Count;
        // =========================================================
        // The caller applies weapon-specific scores or falloff after this exact circle selection.
        public void Accept(Entry entry)
        { Count++; if (DistanceSquared(entry.Position, Center) <= Squared && entry.Enemy.IsAlive) Results.Add(new(entry.Enemy, entry.Position, entry.Order)); }
    }
    internal interface INeighborVisitor { void Accept(Neighbor neighbor); }
    // =========================================================
    // Stack-owned visitors let targeting score candidates directly, with no intermediate result list.
    public void VisitNeighbors<T>(Vector2 center, float radius, ref T visitor) where T : struct, INeighborVisitor
    {
        Validate(center, radius);
        DirectVisitor<T> query = new() { Center = center, Squared = (double)radius * radius, Visitor = visitor };
        Visit(_points, center, radius, ref query); visitor = query.Visitor; CandidatesExamined = query.Count;
    }
    private struct DirectVisitor<T> : IVisitor where T : struct, INeighborVisitor
    {
        public Vector2 Center;
        public double Squared;
        public T Visitor;
        public int Count;
        // =========================================================
        // Broad/narrow phase remains shared; the caller owns target scoring, never the enemy index.
        public void Accept(Entry entry)
        { Count++; if (DistanceSquared(entry.Position, Center) <= Squared && entry.Enemy.IsAlive) Visitor.Accept(new(entry.Enemy, entry.Position, entry.Order)); }
    }
    private interface IVisitor { void Accept(Entry entry); }
    private struct CircleVisitor : IVisitor
    {
        public Vector2 Center;
        public double Squared;
        public List<Enemy> Results;
        public int Count;
        // =========================================================
        // Membership owns native lifetime; dead enemies are synchronously removed.
        public void Accept(Entry entry)
        { Count++; if (DistanceSquared(entry.Position, Center) <= Squared && entry.Enemy.IsAlive) Results.Add(entry.Enemy); }
    }
    private struct SweepVisitor : IVisitor
    {
        public Vector2 Center;
        public float From, To;
        public ulong Cursor, Stamp;
        public List<Sweep> Results;
        public int Count;
        // =========================================================
        // Filter an inclusive annulus conservatively using the sample segment's bounding box.
        public void Accept(Entry entry)
        {
            if (entry.Stamp == Stamp) return;
            entry.Stamp = Stamp; Count++;
            if (!entry.Enemy.IsAlive) return;
            Vector2 start = entry.Position;
            if (entry.Registered <= Cursor)
                for (int i = entry.HistoryStart; i < entry.History.Count; i++)
                    if (entry.History[i].Revision > Cursor) { start = entry.History[i].From; break; }
            Vector2 end = entry.Position;
            if (!end.IsFinite()) return;
            if (!start.IsFinite()) start = end; // The old emitter drops nonfinite previous samples.
            // Conservative numerical padding preserves Crossing's tolerant roots near tick endpoints.
            double padding = Math.Max(To, Math.Max(MaxComponent(start), Math.Max(MaxComponent(end), MaxComponent(Center)))) * 1e-6;
            double minX = Math.Min(start.X, end.X) - Center.X - padding, maxX = Math.Max(start.X, end.X) - Center.X + padding;
            double minY = Math.Min(start.Y, end.Y) - Center.Y - padding, maxY = Math.Max(start.Y, end.Y) - Center.Y + padding;
            double dx = minX > 0 ? minX : maxX < 0 ? maxX : 0, dy = minY > 0 ? minY : maxY < 0 ? maxY : 0;
            double farX = Math.Max(Math.Abs(minX), Math.Abs(maxX)), farY = Math.Max(Math.Abs(minY), Math.Abs(maxY));
            if (dx * dx + dy * dy > (double)To * To || farX * farX + farY * farY < (double)From * From) return;
            Results.Add(new(entry.Enemy, start, end, entry.Order));
        }
    }

    // =========================================================
    // Begin at the current revision without seeding every wave from every living enemy.
    public ulong BeginSweep() { ulong reader = ++_reader; _readers.Add(reader, _revision); return reader; }

    // =========================================================
    // Query conservative annulus bounds, then return exact old/current samples to the unchanged solver.
    public void Swept(ulong reader, Vector2 center, float from, float to, List<Sweep> results)
    {
        Validate(center, to);
        if (!_readers.TryGetValue(reader, out ulong cursor)) throw new InvalidOperationException("Unknown wave cursor.");
        results.Clear(); CandidatesExamined = 0;
        SweepVisitor visitor = new() { Center = center, From = from, To = to, Cursor = cursor, Stamp = ++_stamp, Results = results };
        double padding = Math.Max(to, MaxComponent(center)) * 1e-6;
        float broadTo = (float)Math.Min(float.MaxValue, to + padding), broadFrom = (float)Math.Max(0, from - padding);
        Visit(_points, center, broadTo, ref visitor, broadFrom);
        Visit(_sweeps, center, broadTo, ref visitor, broadFrom);
        foreach (Entry entry in _wide) visitor.Accept(entry);
        CandidatesExamined = visitor.Count;
        results.Sort(static (a, b) => a.Order.CompareTo(b.Order));
        _readers[reader] = _revision;
        Prune();
    }

    // =========================================================
    // Cancelled/finished waves release their retention cursor even during scene teardown.
    public void EndSweep(ulong reader) { if (_readers.Remove(reader)) Prune(); }
    // =========================================================
    // Retain only motion newer than the slowest active wave; cleanup cost is amortized per change.
    private void Prune()
    {
        ulong minimum = _revision;
        foreach (ulong cursor in _readers.Values) minimum = Math.Min(minimum, cursor);
        while (_changes.TryPeek(out var change) && change.Revision <= minimum)
        {
            _changes.Dequeue(); Entry entry = change.Entry;
            if (entry.HistoryStart < entry.History.Count && entry.History[entry.HistoryStart].Revision == change.Revision)
            { entry.HistoryStart++; _dirty.Add(entry); }
        }
        foreach (Entry entry in _dirty)
        {
            if (entry.HistoryStart == entry.History.Count) { entry.History.Clear(); entry.HistoryStart = 0; }
            else if (entry.HistoryStart > 64)
            { entry.History.RemoveRange(0, entry.HistoryStart); entry.HistoryStart = 0; }
            UpdateSweep(entry);
        }
        _dirty.Clear();
    }

    // =========================================================
    // Cover the entire retained motion envelope, including net chords across multiple movement steps.
    // Huge teleports use a small overflow set rather than allocating millions of grid cells.
    private void UpdateSweep(Entry entry)
    {
        if (entry.HistoryStart >= entry.History.Count || !_entries.ContainsKey(entry.Enemy)) { RemoveSweep(entry); return; }
        Vector2 min = entry.Position, max = min;
        for (int i = entry.HistoryStart; i < entry.History.Count; i++)
        {
            Motion motion = entry.History[i];
            if (!motion.From.IsFinite() || !motion.To.IsFinite())
            { if (!entry.Wide) { RemoveSweep(entry); entry.Wide = true; _wide.Add(entry); } return; }
            min = min.Min(motion.From).Min(motion.To); max = max.Max(motion.From).Max(motion.To);
        }
        float padding = (float)(Math.Max(MaxComponent(min), MaxComponent(max)) * 1e-6);
        min -= Vector2.One * padding; max += Vector2.One * padding;
        if (!min.IsFinite() || !max.IsFinite())
        { if (!entry.Wide) { RemoveSweep(entry); entry.Wide = true; _wide.Add(entry); } return; }
        var low = Cell(min); var high = Cell(max);
        double width = high.X - low.X + 1, height = high.Y - low.Y + 1;
        if (width * height > 64 || Math.Abs(low.X) > int.MaxValue || Math.Abs(low.Y) > int.MaxValue)
        { if (!entry.Wide) { RemoveSweep(entry); entry.Wide = true; _wide.Add(entry); } return; }
        if (entry.HasSweep && entry.SweepLow == low && entry.SweepHigh == high) return;
        RemoveSweep(entry); entry.HasSweep = true; entry.SweepLow = low; entry.SweepHigh = high;
        for (int y = 0; y < height; y++) for (int x = 0; x < width; x++)
        { var cell = (low.X + x, low.Y + y); Insert(_sweeps, cell, entry); entry.SweepCells.Add(cell); }
    }

    // =========================================================
    // Recycle empty buckets; normal movement does not allocate fresh bucket lists each frame.
    private void RemoveSweep(Entry entry)
    {
        foreach (var cell in entry.SweepCells) Remove(_sweeps, cell, entry);
        entry.SweepCells.Clear(); entry.HasSweep = false; if (entry.Wide) { _wide.Remove(entry); entry.Wide = false; }
    }
    // =========================================================
    // Iterate nearby cells, or occupied cells for exceptionally large/remote queries; never truncate results.
    private void Visit<T>(Dictionary<(double X, double Y), List<Entry>> grid, Vector2 center, float radius, ref T visitor, float innerRadius = 0) where T : struct, IVisitor
    {
        // Conservative arithmetic padding covers cancellation at extreme finite float coordinates.
        double padding = (Math.Max(Math.Abs((double)center.X), Math.Abs((double)center.Y)) + radius) * 1e-15;
        double left = Math.Floor(((double)center.X - radius - padding) / CellSize), top = Math.Floor(((double)center.Y - radius - padding) / CellSize);
        double right = Math.Floor(((double)center.X + radius + padding) / CellSize), bottom = Math.Floor(((double)center.Y + radius + padding) / CellSize);
        double width = right - left + 1, height = bottom - top + 1;
        if (width * height > grid.Count || Math.Abs(left) > int.MaxValue || Math.Abs(top) > int.MaxValue)
        {
            foreach (var pair in grid)
                if (pair.Key.X >= left && pair.Key.X <= right && pair.Key.Y >= top && pair.Key.Y <= bottom
                    && IntersectsCell(pair.Key, center, radius, innerRadius, padding))
                    foreach (Entry entry in pair.Value) visitor.Accept(entry);
        }
        else for (int y = 0; y < height; y++) for (int x = 0; x < width; x++)
            if (IntersectsCell((left + x, top + y), center, radius, innerRadius, padding)
                && grid.TryGetValue((left + x, top + y), out List<Entry> bucket)) foreach (Entry entry in bucket) visitor.Accept(entry);
    }
    // =========================================================
    // Reject cells outside the disc or wholly behind the swept annulus before visiting their enemies.
    private bool IntersectsCell((double X, double Y) cell, Vector2 center, float outer, float inner, double padding)
    {
        double x0 = cell.X * CellSize - center.X, y0 = cell.Y * CellSize - center.Y;
        double x1 = x0 + CellSize, y1 = y0 + CellSize;
        double dx = Math.Max(0, Math.Max(x0, -x1)), dy = Math.Max(0, Math.Max(y0, -y1));
        double padded = outer + padding;
        if (dx * dx + dy * dy > padded * padded) return false;
        double fx = Math.Max(Math.Abs(x0), Math.Abs(x1)), fy = Math.Max(Math.Abs(y0), Math.Abs(y1));
        return inner <= 0 || fx * fx + fy * fy >= (double)inner * inner;
    }
    // =========================================================
    // Point and swept channels belong to the same world index and share bucket storage.
    private void Insert(Dictionary<(double X, double Y), List<Entry>> grid, (double X, double Y) cell, Entry entry)
    {
        if (!grid.TryGetValue(cell, out List<Entry> bucket)) { bucket = _buckets.Count > 0 ? _buckets.Pop() : new(); grid.Add(cell, bucket); }
        bucket.Add(entry);
    }
    // =========================================================
    // Swap-remove does not change gameplay order; explicit registration order resolves ties/dispatch.
    private void Remove(Dictionary<(double X, double Y), List<Entry>> grid, (double X, double Y) cell, Entry entry)
    {
        if (!grid.TryGetValue(cell, out List<Entry> bucket)) return;
        int index = bucket.IndexOf(entry);
        if (index >= 0) { bucket[index] = bucket[^1]; bucket.RemoveAt(bucket.Count - 1); }
        if (bucket.Count == 0) { grid.Remove(cell); _buckets.Push(bucket); }
    }
    // =========================================================
    // Floor, rather than truncation, keeps negative coordinates and exact cell edges correct.
    private (double X, double Y) Cell(Vector2 position) => (Math.Floor((double)position.X / CellSize), Math.Floor((double)position.Y / CellSize));
    // =========================================================
    // Scale-aware broad-phase padding never changes the exact circle or wavefront solver's decision.
    private static double MaxComponent(Vector2 position) => Math.Max(Math.Abs((double)position.X), Math.Abs((double)position.Y));
    // =========================================================
    // Double precision preserves the instant blast's existing boundary arithmetic.
    internal static double DistanceSquared(Vector2 a, Vector2 b) { double x = (double)a.X - b.X, y = (double)a.Y - b.Y; return x * x + y * y; }
    // =========================================================
    // Explicit policy: zero radius includes the exact center; negative/non-finite queries throw.
    private static void Validate(Vector2 center, float radius)
    { if (!center.IsFinite() || !float.IsFinite(radius) || radius < 0) throw new ArgumentOutOfRangeException(nameof(radius)); }
}
