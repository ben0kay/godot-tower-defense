// One lazy world-scoped beacon index; movement events test only nearby reveal areas, with no frame scans.
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
    private readonly List<Enemy> _snapshot = new();
    public int ActiveBeacons => _areas.Count;
    public int LastCandidates { get; private set; }
    private sealed class Area
    {
        public Vector2 Center;
        public float RadiusSquared;
        public readonly List<Vector2I> Cells = new();
        public readonly HashSet<Enemy> Enemies = new();
    }
    // =========================================================
    // Worlds without a beacon allocate no index, subscribe to no reveal callbacks and run no detection tick.
    public static StealthRevealSystem For(EnemyRegistry registry)
    {
        var system = registry.GetNodeOrNull<StealthRevealSystem>("StealthReveals");
        if (system != null) return system;
        system = new() { Name = "StealthReveals", _registry = registry }; registry.AddChild(system); return system;
    }
    // =========================================================
    // Cell size is internal broad-phase tuning; final inclusive radius checks stay exact.
    private static Vector2I Cell(Vector2 position) => new(Mathf.FloorToInt(position.X / CellSize), Mathf.FloorToInt(position.Y / CellSize));
    // =========================================================
    // All work is driven by source and registry events.
    public override void _Ready() { SetProcess(false); SetPhysicsProcess(false); }
    #endregion

    #region Sources
    // =========================================================
    // Placement, activation or range changes query existing nearby ships once; later motion is event-driven.
    public void Register(StealthBeacon beacon)
    {
        Unregister(beacon);
        if (_areas.Count == 0)
        {
            _registry.EnemyRegistered += UpdateEnemy; _registry.EnemyPositionChanged += UpdateEnemy;
            _registry.EnemyUnregistered += RemoveEnemy;
        }
        float radius = beacon.FinalStats.RangePixels;
        Area area = new() { Center = beacon.GlobalPosition, RadiusSquared = radius * radius };
        _areas.Add(beacon, area);
        Vector2I low = Cell(area.Center - Vector2.One * radius), high = Cell(area.Center + Vector2.One * radius);
        for (int y = low.Y; y <= high.Y; y++) for (int x = low.X; x <= high.X; x++)
        {
            Vector2I cell = new(x, y);
            if (!_cells.TryGetValue(cell, out var list)) _cells.Add(cell, list = new());
            list.Add(area); area.Cells.Add(cell);
        }
        _registry.GetEnemiesInRadius(area.Center, radius, _snapshot);
        foreach (Enemy enemy in _snapshot) UpdateEnemy(enemy);
        _snapshot.Clear();
    }
    // =========================================================
    // Removing one overlapping source cannot erase another source's reveal contribution.
    public void Unregister(StealthBeacon beacon)
    {
        if (!_areas.Remove(beacon, out Area area)) return;
        foreach (Vector2I cell in area.Cells)
        {
            var list = _cells[cell]; list.Remove(area); if (list.Count == 0) _cells.Remove(cell);
        }
        foreach (Enemy enemy in area.Enemies)
        {
            if (GodotObject.IsInstanceValid(enemy)) enemy.Stealth?.RemoveReveal(area);
            if (_memberships.TryGetValue(enemy, out var sources))
            { sources.Remove(area); if (sources.Count == 0) _memberships.Remove(enemy); }
        }
        area.Enemies.Clear();
        if (_areas.Count == 0) Detach();
    }
    #endregion

    #region Membership
    // =========================================================
    // A teleport and ordinary movement share the same path; normal enemies stop at the cheap capability check.
    private void UpdateEnemy(Enemy enemy)
    {
        LastCandidates = 0;
        if (!enemy.HasStealth || !enemy.IsAlive || enemy.IsQueuedForDeletion()) return;
        Vector2 position = enemy.GlobalPosition;
        _memberships.TryGetValue(enemy, out var sources);
        if (sources != null)
        {
            for (int i = sources.Count - 1; i >= 0; i--)
            {
                Area old = sources[i];
                if (position.DistanceSquaredTo(old.Center) <= old.RadiusSquared) continue;
                old.Enemies.Remove(enemy); sources.RemoveAt(i); enemy.Stealth.RemoveReveal(old);
            }
        }
        if (_cells.TryGetValue(Cell(position), out var nearby)) foreach (Area area in nearby)
        {
            LastCandidates++;
            if (position.DistanceSquaredTo(area.Center) > area.RadiusSquared || area.Enemies.Contains(enemy)) continue;
            if (sources == null) { sources = new(); _memberships.Add(enemy, sources); }
            sources.Add(area); area.Enemies.Add(enemy); enemy.Stealth.AddReveal(area);
        }
        if (sources?.Count == 0) _memberships.Remove(enemy);
    }
    // =========================================================
    // Death, arrival, reuse and explicit removal release both sides without holding a dead native object.
    private void RemoveEnemy(Enemy enemy)
    {
        if (!_memberships.Remove(enemy, out var sources)) return;
        foreach (Area area in sources) { area.Enemies.Remove(enemy); enemy.Stealth?.RemoveReveal(area); }
    }
    // =========================================================
    // Empty indexes stop observing movement entirely, including between scenes.
    private void Detach()
    {
        if (!GodotObject.IsInstanceValid(_registry)) return;
        _registry.EnemyRegistered -= UpdateEnemy; _registry.EnemyPositionChanged -= UpdateEnemy;
        _registry.EnemyUnregistered -= RemoveEnemy;
    }
    // =========================================================
    // World teardown releases reveal handles even when enemy and tower subtrees leave in different orders.
    public override void _ExitTree()
    {
        Detach();
        foreach (var pair in _memberships) if (GodotObject.IsInstanceValid(pair.Key))
            foreach (Area source in pair.Value) pair.Key.Stealth?.RemoveReveal(source);
        _memberships.Clear(); _areas.Clear(); _cells.Clear(); _snapshot.Clear();
    }
    #endregion
}
