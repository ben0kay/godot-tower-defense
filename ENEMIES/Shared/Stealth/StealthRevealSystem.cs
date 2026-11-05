// One lazy world-scoped cone-source index; movement and capped scanner updates reuse existing spatial queries.
using Godot;
using System.Collections.Generic;

public partial class StealthRevealSystem : Node
{
    #region World and Index
    private const float CellSize = 256;
    private EnemyRegistry _registry;
    private readonly Dictionary<StealthBeacon, Area> _areas = new();
    private readonly Dictionary<Vector2I, List<Area>> _cells = new();
    private readonly Dictionary<Enemy, List<Area>> _memberships = new();
    private readonly Stack<List<Area>> _sourceLists = new();
    private readonly List<Enemy> _snapshot = new(), _expired = new(), _unregisterSnapshot = new();
    private readonly HashSet<Enemy> _seen = new();
    public int ActiveBeacons => _areas.Count;
    public int LastCandidates { get; private set; }
    public int DirectionUpdates { get; private set; }
    private sealed class Area
    {
        public Vector2 Center, Forward;
        public float Radius, RadiusSquared, CosineHalfAngle;
        public readonly List<Vector2I> Cells = new();
        public readonly HashSet<Enemy> Enemies = new();
    }
    // =========================================================
    // Worlds without beacons allocate no index or detection clock; the source owns its scan cadence.
    public static StealthRevealSystem For(EnemyRegistry registry)
    {
        var system = registry.GetNodeOrNull<StealthRevealSystem>("StealthReveals");
        if (system != null) return system;
        system = new() { Name = "StealthReveals", _registry = registry }; registry.AddChild(system); return system;
    }
    private static Vector2I Cell(Vector2 position) => new(Mathf.FloorToInt(position.X / CellSize), Mathf.FloorToInt(position.Y / CellSize));
    public override void _Ready() { SetProcess(false); SetPhysicsProcess(false); }
    #endregion

    #region Sources
    // =========================================================
    // Activation/config/teleport updates retain an existing Area identity, reindexing only changed geometry.
    public void Register(StealthBeacon beacon)
    {
        if (beacon.Config?.IsValid != true || !beacon.GlobalPosition.IsFinite() || !ValidHeading(beacon.ScanHeading)) return;
        float radius = beacon.FinalStats.RangePixels;
        float cosine = Mathf.Cos(Mathf.DegToRad(beacon.FinalStats.ScanConeDegrees * .5f));
        bool exists = _areas.TryGetValue(beacon, out Area area);
        if (!exists)
        {
            if (_areas.Count == 0)
            {
                _registry.EnemyRegistered += UpdateEnemy; _registry.EnemyPositionChanged += UpdateEnemy;
                _registry.EnemyUnregistered += RemoveEnemy;
            }
            area = new(); _areas.Add(beacon, area);
        }
        bool geometryChanged = !exists || area.Center != beacon.GlobalPosition || area.Radius != radius;
        if (!geometryChanged && area.Forward == beacon.ScanHeading && area.CosineHalfAngle == cosine) return;
        if (geometryChanged) RemoveCells(area);
        area.Center = beacon.GlobalPosition; area.Radius = radius; area.RadiusSquared = radius * radius;
        area.Forward = beacon.ScanHeading; area.CosineHalfAngle = cosine;
        if (geometryChanged)
        {
            Vector2I low = Cell(area.Center - Vector2.One * radius), high = Cell(area.Center + Vector2.One * radius);
            for (int y = low.Y; y <= high.Y; y++) for (int x = low.X; x <= high.X; x++)
            {
                Vector2I cell = new(x, y);
                if (!_cells.TryGetValue(cell, out var list)) _cells.Add(cell, list = new());
                list.Add(area); area.Cells.Add(cell);
            }
        }
        RefreshArea(area);
    }
    // =========================================================
    // Ordinary sweep updates mutate heading only: no unregister/register, bucket churn or new reveal identity.
    public void UpdateBeaconDirection(StealthBeacon beacon, Vector2 normalizedHeading)
    {
        if (!_areas.TryGetValue(beacon, out Area area) || !ValidHeading(normalizedHeading)) return;
        area.Forward = normalizedHeading; DirectionUpdates++; RefreshArea(area);
    }
    // =========================================================
    // Only an actual source departure releases handles. Overlapping independent sources remain untouched.
    public void Unregister(StealthBeacon beacon)
    {
        if (!_areas.Remove(beacon, out Area area)) return;
        RemoveCells(area);
        _unregisterSnapshot.Clear(); _unregisterSnapshot.AddRange(area.Enemies);
        foreach (Enemy enemy in _unregisterSnapshot) RemoveMembership(enemy, area);
        _unregisterSnapshot.Clear();
        if (_areas.Count == 0) Detach();
    }
    private void RemoveCells(Area area)
    {
        foreach (Vector2I cell in area.Cells)
        { var list = _cells[cell]; list.Remove(area); if (list.Count == 0) _cells.Remove(cell); }
        area.Cells.Clear();
    }
    #endregion

    #region Membership
    // =========================================================
    // Normal enemies exit before source lookup. Movement and teleport use the same exact cone predicate.
    private void UpdateEnemy(Enemy enemy)
    {
        LastCandidates = 0;
        if (!Eligible(enemy)) return;
        Vector2 position = enemy.GlobalPosition;
        if (_memberships.TryGetValue(enemy, out var sources))
            for (int i = sources.Count - 1; i >= 0; i--)
            { Area old = sources[i]; if (!InsideCone(position, old)) RemoveMembership(enemy, old); }
        if (_cells.TryGetValue(Cell(position), out var nearby)) foreach (Area area in nearby)
        {
            LastCandidates++;
            if (InsideCone(position, area)) AddMembership(enemy, area);
        }
    }
    // =========================================================
    // Both sides change before notification; pooled lists avoid allocating on every reveal/re-stealth sweep.
    private void AddMembership(Enemy enemy, Area area)
    {
        if (!area.Enemies.Add(enemy)) return;
        if (!_memberships.TryGetValue(enemy, out var sources))
        { sources = _sourceLists.Count > 0 ? _sourceLists.Pop() : new(); _memberships.Add(enemy, sources); }
        sources.Add(area); enemy.Stealth.AddReveal(area);
    }
    private void RemoveMembership(Enemy enemy, Area area)
    {
        area.Enemies.Remove(enemy);
        if (_memberships.TryGetValue(enemy, out var sources))
        {
            sources.Remove(area);
            if (sources.Count == 0) { _memberships.Remove(enemy); _sourceLists.Push(sources); }
        }
        if (GodotObject.IsInstanceValid(enemy)) enemy.Stealth?.RemoveReveal(area);
    }
    // =========================================================
    // Death/arrival/reuse release both sides immediately, even between scanner samples.
    private void RemoveEnemy(Enemy enemy)
    {
        if (!_memberships.Remove(enemy, out var sources)) return;
        foreach (Area area in sources) { area.Enemies.Remove(enemy); enemy.Stealth?.RemoveReveal(area); }
        sources.Clear(); _sourceLists.Push(sources);
    }
    private bool Eligible(Enemy enemy) => GodotObject.IsInstanceValid(enemy) && enemy.HasStealth && enemy.IsAlive
        && !enemy.IsQueuedForDeletion() && _registry.Contains(enemy) && enemy.GlobalPosition.IsFinite();
    // =========================================================
    // Empty indexes stop observing motion entirely; the manager itself has no frame/physics callback.
    private void Detach()
    {
        if (!GodotObject.IsInstanceValid(_registry)) return;
        _registry.EnemyRegistered -= UpdateEnemy; _registry.EnemyPositionChanged -= UpdateEnemy;
        _registry.EnemyUnregistered -= RemoveEnemy;
    }
    // =========================================================
    // World teardown releases all handles irrespective of enemy/tower subtree order.
    public override void _ExitTree()
    {
        Detach();
        foreach (var pair in _memberships) if (GodotObject.IsInstanceValid(pair.Key))
            foreach (Area source in pair.Value) pair.Key.Stealth?.RemoveReveal(source);
        _memberships.Clear(); _areas.Clear(); _cells.Clear(); _snapshot.Clear(); _expired.Clear();
        _seen.Clear(); _unregisterSnapshot.Clear(); _sourceLists.Clear();
    }
    #endregion
}
