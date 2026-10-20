// One world-scoped placed-tower footprint index, updated by membership/transform/death events, never per-frame scans.
using Godot;
using System;
using System.Collections.Generic;

public partial class TowerRegistry : Node
{
    #region World and Buckets
    private const double CellSize = 128;
    private Node _towers;
    private WorldGrid _grid;
    private long _order;
    private ulong _query;
    private readonly Dictionary<TowerBase, Entry> _entries = new();
    private readonly Dictionary<(double X, double Y), List<Entry>> _cells = new();
    private readonly List<Entry> _wide = new();
    private readonly Stack<List<Entry>> _buckets = new();
    private sealed class Entry
    {
        public TowerBase Tower;
        public Rect2 Bounds;
        public long Order;
        public ulong Query;
        public Action OnDeath;
        public readonly List<(double X, double Y)> Cells = new();
        public bool Wide, Indexed;
    }
    public int Count => _entries.Count;
    public int LastCandidates { get; private set; }
    // =========================================================
    // World composition owns one index. A single startup discovery supports already-present map/debug towers.
    public override void _Ready()
    {
        SetProcess(false); SetPhysicsProcess(false);
        _towers = GetNode<Node>("../Towers"); _grid = GetNode<WorldGrid>("../Grid");
        _towers.ChildEnteredTree += OnEntered; _towers.ChildExitingTree += OnExiting;
        foreach (Node child in _towers.GetChildren()) OnEntered(child);
    }
    // =========================================================
    // Only successful world membership is indexed; detached previews and failed preflight scenes stay absent.
    private void OnEntered(Node child)
    { if (child is TowerBase tower) Register(tower); }
    // =========================================================
    // Explicit removal and whole-world teardown share idempotent cleanup.
    private void OnExiting(Node child)
    { if (child is TowerBase tower) Unregister(tower); }
    // =========================================================
    // Construction and disabled structures remain damageable; only dead/queued/detached objects are excluded.
    public bool Contains(TowerBase tower) => GodotObject.IsInstanceValid(tower) && _entries.TryGetValue(tower, out Entry entry) && entry.Indexed
        && tower.IsInsideTree() && !tower.IsQueuedForDeletion() && !tower.IsDestroyed && tower.Health > 0
        && tower.GetParent() == _towers;
    // =========================================================
    // A stable per-world insertion serial resolves equidistant targets independently of bucket enumeration order.
    public long RegistrationOrder(TowerBase tower) => _entries.TryGetValue(tower, out Entry entry) ? entry.Order : long.MaxValue;
    #endregion

    #region Event Driven Membership
    // =========================================================
    // One small retained entry/delegate per placed tower; no per-query connections or discovery scans.
    private void Register(TowerBase tower)
    {
        if (_entries.ContainsKey(tower) || tower.IsDestroyed || tower.IsQueuedForDeletion()) return;
        Entry entry = new() { Tower = tower, Order = ++_order };
        entry.OnDeath = () => Unregister(tower);
        _entries.Add(tower, entry);
        tower.Destroyed += entry.OnDeath; tower.SpatialChanged += Update;
        tower.ObserveSpatialChanges(true); Update(tower);
    }
    // =========================================================
    // Death removes the complete footprint immediately, before deferred native deletion.
    private void Unregister(TowerBase tower)
    {
        if (!_entries.Remove(tower, out Entry entry)) return;
        RemoveCells(entry);
        if (GodotObject.IsInstanceValid(tower))
        { tower.Destroyed -= entry.OnDeath; tower.SpatialChanged -= Update; tower.ObserveSpatialChanges(false); }
    }
    // =========================================================
    // Full multi-cell geometry is measured in grid space then transformed to a conservative world AABB.
    private void Update(TowerBase tower)
    {
        if (!_entries.TryGetValue(tower, out Entry entry)) return;
        RemoveCells(entry);
        if (!tower.GlobalPosition.IsFinite() || !TowerDefinition.ValidFootprint(tower.Footprint)) return;
        Vector2 half = (Vector2)tower.Footprint * (_grid.Config.CellSize * .5f);
        Vector2 center = _grid.ToLocal(tower.GlobalPosition);
        Vector2 a = _grid.ToGlobal(center - half), b = _grid.ToGlobal(center + half);
        Vector2 c = _grid.ToGlobal(center + new Vector2(half.X, -half.Y));
        Vector2 d = _grid.ToGlobal(center + new Vector2(-half.X, half.Y));
        if (!a.IsFinite() || !b.IsFinite() || !c.IsFinite() || !d.IsFinite()) return;
        Vector2 low = a.Min(b).Min(c).Min(d), high = a.Max(b).Max(c).Max(d);
        entry.Bounds = new(low, high - low);
        if (!entry.Bounds.Size.IsFinite()) return;
        entry.Indexed = true;
        var first = Cell(low); var last = Cell(high);
        double width = last.X - first.X + 1, height = last.Y - first.Y + 1;
        if (width * height > 256) { entry.Wide = true; _wide.Add(entry); return; }
        for (int y = 0; y < height; y++) for (int x = 0; x < width; x++)
        {
            var key = (first.X + x, first.Y + y);
            if (!_cells.TryGetValue(key, out var bucket))
            { bucket = _buckets.Count > 0 ? _buckets.Pop() : new(); _cells.Add(key, bucket); }
            bucket.Add(entry); entry.Cells.Add(key);
        }
    }
    // =========================================================
    // Recycle empty spatial buckets across relocation; no repeated scene traversal.
    private void RemoveCells(Entry entry)
    {
        foreach (var key in entry.Cells)
        {
            var bucket = _cells[key]; bucket.Remove(entry);
            if (bucket.Count == 0) { _cells.Remove(key); _buckets.Push(bucket); }
        }
        entry.Cells.Clear(); entry.Indexed = false; if (entry.Wide) { _wide.Remove(entry); entry.Wide = false; }
    }
    #endregion

    #region Spatial Queries
    // =========================================================
    // Caller retains the buffer. Inclusive footprint/disc broadphase has no result truncation or duplicate towers.
    public void GetNearby(Vector2 center, float radius, List<TowerBase> results)
    {
        results.Clear(); LastCandidates = 0;
        if (!center.IsFinite() || !float.IsFinite(radius) || radius < 0) return;
        if (++_query == 0) { foreach (Entry entry in _entries.Values) entry.Query = 0; _query = 1; }
        var low = Cell((double)center.X - radius, (double)center.Y - radius);
        var high = Cell((double)center.X + radius, (double)center.Y + radius);
        double width = high.X - low.X + 1, height = high.Y - low.Y + 1;
        if (!double.IsFinite(width * height) || width * height > _cells.Count)
        {
            foreach (var pair in _cells)
                if (pair.Key.X >= low.X && pair.Key.X <= high.X && pair.Key.Y >= low.Y && pair.Key.Y <= high.Y)
                    foreach (Entry entry in pair.Value) Visit(entry, center, radius, results);
        }
        else for (int y = 0; y < height; y++) for (int x = 0; x < width; x++)
            if (_cells.TryGetValue((low.X + x, low.Y + y), out var bucket))
                foreach (Entry entry in bucket) Visit(entry, center, radius, results);
        foreach (Entry entry in _wide) Visit(entry, center, radius, results);
    }
    // =========================================================
    // Exact AABB/disc overlap follows coarse buckets; target selection applies its separate center-distance rule.
    private void Visit(Entry entry, Vector2 center, float radius, List<TowerBase> results)
    {
        if (entry.Query == _query) return;
        entry.Query = _query; LastCandidates++;
        if (!Contains(entry.Tower)) return;
        Vector2 end = entry.Bounds.End, start = entry.Bounds.Position;
        double dx = Math.Max(0, Math.Max((double)start.X - center.X, (double)center.X - end.X));
        double dy = Math.Max(0, Math.Max((double)start.Y - center.Y, (double)center.Y - end.Y));
        if (dx * dx + dy * dy <= (double)radius * radius) results.Add(entry.Tower);
    }
    // =========================================================
    // Exposes cached broadphase bounds for later swept hostile bullets; this does not apply any damage.
    public bool TryGetBounds(TowerBase tower, out Rect2 bounds)
    {
        if (Contains(tower) && _entries.TryGetValue(tower, out Entry entry)) { bounds = entry.Bounds; return true; }
        bounds = default; return false;
    }
    // =========================================================
    // Floor supports negative coordinates; double keys avoid integer-cell overflow.
    private static (double X, double Y) Cell(Vector2 p) => Cell(p.X, p.Y);
    // =========================================================
    // Double query endpoints avoid narrowing huge finite center/radius sums back into float infinity.
    private static (double X, double Y) Cell(double x, double y) => (Math.Floor(x / CellSize), Math.Floor(y / CellSize));
    // =========================================================
    // Unsubscribe whichever subtree exits first; the world owns all buffers and target references.
    public override void _ExitTree()
    {
        if (GodotObject.IsInstanceValid(_towers))
        { _towers.ChildEnteredTree -= OnEntered; _towers.ChildExitingTree -= OnExiting; }
        foreach (Entry entry in _entries.Values) if (GodotObject.IsInstanceValid(entry.Tower))
        { entry.Tower.Destroyed -= entry.OnDeath; entry.Tower.SpatialChanged -= Update; entry.Tower.ObserveSpatialChanges(false); }
        _entries.Clear(); _cells.Clear(); _wide.Clear(); _buckets.Clear(); _towers = null; _grid = null;
    }
    #endregion
}
