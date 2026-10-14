// Chooses permitted reachable portal/edge entries; callers own debug or future wave timing.
using Godot;
using System.Collections.Generic;

public partial class SpawnManager : Node
{
    [Export] public EnemyCatalog Catalog { get; set; }
    public EnemyRegistry Registry { get; private set; }
    public PathRequestManager Paths { get; private set; }
    public string LastMessage { get; private set; } = "F2: spawn Enemy001 at a permitted Portal.";
    private readonly RandomNumberGenerator _spawnRandom = new();
    private Node2D _ships;
    private int _sourceIndex, _cachedRevision = -1;
    private bool[] _reachable;
    private int _portalCount;
    private readonly List<string> _portalPaths = new();
    private readonly List<Node2D> _portalObjects = new();
    private readonly List<Vector2> _entries = new();

    // =========================================================
    // All runtime mechanics remain outside the physical Portal and level Debug nodes.
    public override void _Ready()
    {
        if (Catalog == null) Catalog = EnemyCatalog.Default; else Catalog.Rebuild();
        _spawnRandom.Randomize();
        Registry = GetNode<EnemyRegistry>("../Registry");
        Paths = GetNode<PathRequestManager>("../Paths");
        _ships = GetNode<Node2D>("../../Enemies");
        RefreshSources();
    }

    // =========================================================
    // Catalog IDs provide generic spawn requests while preserving debug's original scene overload.
    public Enemy Spawn(string enemyId, bool portalOnly = false)
    {
        PackedScene scene = Catalog.Find(enemyId);
        if (scene == null) { LastMessage = "Unknown EnemyId: " + enemyId; return null; }
        return Spawn(scene, portalOnly);
    }
    // =========================================================
    // Wave callers retain portal/shield policy; this adapter only resolves the registered type.
    public Enemy SpawnWave(string enemyId, RandomNumberGenerator random, string requiredPortal = "", bool spawnWithShield = false)
    {
        PackedScene scene = Catalog.Find(enemyId);
        if (scene == null) { LastMessage = "Unknown EnemyId: " + enemyId; return null; }
        return SpawnWave(scene, random, requiredPortal, spawnWithShield);
    }
    // =========================================================
    // Current single-cell routing guarantees clearance only below half a navigation cell.
    // Oversized types wait safely rather than spawning into a corridor the shared path model cannot fit.
    private bool FitsNavigation(Enemy ship)
    {
        if (ship.Definition.RadiusPixels < Paths.Navigation.Grid.Config.CellSize * 0.5f) return true;
        LastMessage = "Enemy radius must be smaller than half a navigation cell; spawn will retry.";
        return false;
    }

    // =========================================================
    // Cache authored entries and connectivity until the navigation revision changes.
    private void RefreshSources()
    {
        NavigationGrid nav = Paths.Navigation;
        if (_cachedRevision == nav.Revision) return;
        _entries.Clear(); _portalPaths.Clear(); _portalObjects.Clear(); bool[] reachable = nav.ReachableWithout();
        if (_reachable == null || _reachable.Length != reachable.Length) _reachable = new bool[reachable.Length];
        System.Array.Copy(reachable, _reachable, reachable.Length);
        WorldGrid grid = nav.Grid; LevelConfig settings = grid.Level.Settings;
        if (settings.SpawnMode != LevelConfig.SpawnSourceMode.MapEdges)
            foreach (NodePath path in settings.ActivePortals)
            {
                Node2D portal = grid.Level.GetNodeOrNull<Node2D>(path);
                Marker2D exit = portal?.GetNodeOrNull<Marker2D>("SpawnExit");
                if (exit == null) GD.PushWarning("Registered Portal has no SpawnExit: " + path);
                if (exit != null && !_portalPaths.Contains(path.ToString()))
                { _entries.Add(exit.GlobalPosition); _portalPaths.Add(path.ToString()); _portalObjects.Add(portal); }
            }
        _portalCount = _entries.Count;
        if (settings.SpawnMode != LevelConfig.SpawnSourceMode.Portals)
        {
            for (int x = 0; x < grid.Config.WidthCells; x++)
            {
                if (settings.AllowedEdges.HasFlag(LevelConfig.MapEdge.Top)) _entries.Add(grid.CellToWorldCenter(new(x, 0)));
                if (settings.AllowedEdges.HasFlag(LevelConfig.MapEdge.Bottom)) _entries.Add(grid.CellToWorldCenter(new(x, grid.Config.HeightCells - 1)));
            }
            for (int y = 0; y < grid.Config.HeightCells; y++)
            {
                if (settings.AllowedEdges.HasFlag(LevelConfig.MapEdge.Left)) _entries.Add(grid.CellToWorldCenter(new(0, y)));
                if (settings.AllowedEdges.HasFlag(LevelConfig.MapEdge.Right)) _entries.Add(grid.CellToWorldCenter(new(grid.Config.WidthCells - 1, y)));
            }
        }
        for (int i = 0; i < _portalCount; i++)
            if (!nav.IsReachable(_reachable, grid.WorldToCell(_entries[i])) || !nav.HasClearance(_entries[i], 0f))
                GD.PushWarning("Portal SpawnExit is blocked or cannot reach the Citadel: " + _portalPaths[i]);
        if (nav.Goals.Count == 0)
            GD.PushWarning("No usable Citadel approach markers; enemies cannot find a destination.");
        _cachedRevision = nav.Revision;
    }

    // =========================================================
    // Debug defaults to portals; future waves may use every source permitted by LevelConfig.
    public Enemy Spawn(PackedScene scene, bool portalOnly = false)
    {
        RefreshSources();
        NavigationGrid nav = Paths.Navigation; WorldGrid grid = nav.Grid;
        if (scene == null) { LastMessage = "No enemy scene assigned."; return null; }
        if (Registry.Living.Count >= Paths.Config.MaxActiveEnemies)
        { LastMessage = "Active enemy safety limit reached."; return null; }
        Enemy ship = scene.Instantiate<Enemy>();
        if (!FitsNavigation(ship)) { ship.Free(); return null; }
        List<Vector2> sources = _entries;
        List<Node2D> portals = _portalObjects;
        if (portalOnly)
        {
            sources = new(); portals = new();
            if (grid.Level.Settings.SpawnMode != LevelConfig.SpawnSourceMode.MapEdges)
                foreach (NodePath path in grid.Level.Settings.ActivePortals)
                {
                    Node2D portal = grid.Level.GetNodeOrNull<Node2D>(path);
                    Marker2D exit = portal?.GetNodeOrNull<Marker2D>("SpawnExit");
                    if (exit != null) { sources.Add(exit.GlobalPosition); portals.Add(portal); }
                }
        }
        for (int i = 0; i < sources.Count; i++)
        {
            Vector2 position = sources[(_sourceIndex + i) % sources.Count];
            if (!nav.IsReachable(_reachable, grid.WorldToCell(position))
                || !nav.HasClearance(position, ship.Definition.RadiusPixels)) continue;
            int source = (_sourceIndex + i) % sources.Count;
            if (portalOnly || source < _portalCount) position = OffsetPortal(position, ship.Definition.RadiusPixels, _spawnRandom, portals[source]);
            _sourceIndex = (_sourceIndex + i + 1) % sources.Count;
            _ships.AddChild(ship); ship.GlobalPosition = position;
            ship.Initialize(Registry, Paths); LastMessage = "Spawned " + ship.Definition.DisplayName;
            return ship;
        }
        ship.Free(); LastMessage = "No permitted reachable spawn exit; check tiles and Citadel approach points.";
        return null;
    }

    // =========================================================
    // Wave requests sample eligible portals uniformly, falling back to permitted edges in Both.
    // Reservoir sampling avoids a per-spawn candidate allocation or any additional A* calls.
    public Enemy SpawnWave(PackedScene scene, RandomNumberGenerator random, string requiredPortal = "", bool spawnWithShield = false)
    {
        RefreshSources();
        if (scene == null || random == null) { LastMessage = "Wave enemy scene/RNG is missing."; return null; }
        if (Registry.Living.Count >= Paths.Config.MaxActiveEnemies)
        { LastMessage = "Active enemy safety limit reached; wave spawn will retry."; return null; }
        Enemy ship = scene.Instantiate<Enemy>();
        if (!FitsNavigation(ship)) { ship.Free(); return null; }
        NavigationGrid nav = Paths.Navigation;
        int chosen = -1, eligible = 0;
        for (int i = 0; i < _entries.Count; i++)
        {
            bool portal = i < _portalCount;
            if (requiredPortal.Length > 0 && (!portal || _portalPaths[i] != requiredPortal)) continue;
            // If any eligible portal was found, Both mode ignores all map-edge candidates.
            if (!portal && chosen >= 0 && chosen < _portalCount) break;
            Vector2 position = _entries[i];
            if (!nav.IsReachable(_reachable, nav.Grid.WorldToCell(position))
                || !nav.HasClearance(position, ship.Definition.RadiusPixels)) continue;
            eligible++;
            if (random.RandiRange(1, eligible) == 1) chosen = i;
        }
        if (chosen < 0)
        {
            ship.Free();
            LastMessage = requiredPortal.Length > 0 ? "Required portal unavailable: " + requiredPortal
                : "No permitted reachable source with enemy clearance; wave spawn will retry.";
            return null;
        }
        Vector2 spawn = _entries[chosen];
        if (chosen < _portalCount) spawn = OffsetPortal(spawn, ship.Definition.RadiusPixels, random, _portalObjects[chosen]);
        _ships.AddChild(ship); ship.GlobalPosition = spawn;
        ship.Initialize(Registry, Paths, spawnWithShield); LastMessage = "Wave spawned " + ship.Definition.DisplayName;
        return ship;
    }

    // =========================================================
    // Small portal-only variation; validate radius, connectivity and the segment back to center.
    // Bounded attempts fall back to the already validated center, without extra A* requests.
    private Vector2 OffsetPortal(Vector2 center, float radius, RandomNumberGenerator random, Node2D portal)
    {
        float offset = Mathf.Clamp(Paths.Config.PortalSpawnOffsetPixels, 0f, 24f) *
            (portal is EnemyPortal enemyPortal ? enemyPortal.SizeMultiplier : 1f);
        if (offset <= 0f) return center;
        NavigationGrid nav = Paths.Navigation;
        for (int attempt = 0; attempt < 8; attempt++)
        {
            Vector2 localOffset = new(random.RandfRange(-offset, offset), random.RandfRange(-offset, offset));
            Vector2 point = center + (GodotObject.IsInstanceValid(portal) ? portal.GlobalTransform.BasisXform(localOffset) : localOffset);
            if (nav.IsReachable(_reachable, nav.Grid.WorldToCell(point))
                && nav.HasClearance(point, radius) && nav.SegmentClear(center, point, radius)) return point;
        }
        return center;
    }

    // =========================================================
    // Click-only route preservation: protect active ships and every currently usable source.
    public double LastPlacementValidationMilliseconds { get; private set; }

    // =========================================================
    // Keep timing independent of early rejection paths.
    public string PlacementFailure(Vector2I cell)
    {
        long started = System.Diagnostics.Stopwatch.GetTimestamp();
        try { return ValidatePlacement(cell); }
        finally { LastPlacementValidationMilliseconds = System.Diagnostics.Stopwatch.GetElapsedTime(started).TotalMilliseconds; }
    }

    // =========================================================
    // Full connectivity is still checked before commit, using reusable flood-fill scratch space.
    private string ValidatePlacement(Vector2I cell)
    {
        RefreshSources(); NavigationGrid nav = Paths.Navigation;
        foreach (Enemy enemy in Registry.Living)
            if (!nav.HasClearance(enemy.GlobalPosition, enemy.Definition.RadiusPixels, cell))
                return "An enemy is too close to this cell.";
        if (nav.Grid.BlocksEnemies(cell)) return "";
        bool[] after = nav.ReachableWithout(cell);
        foreach (Vector2 position in _entries)
        {
            Vector2I source = nav.Grid.WorldToCell(position);
            if (nav.IsReachable(_reachable, source) && !nav.IsReachable(after, source))
                return "This tower would seal a spawn route to the Citadel.";
        }
        foreach (Enemy enemy in Registry.Living)
            if (!nav.IsReachable(after, nav.Grid.WorldToCell(enemy.GlobalPosition)))
                return "This tower would trap an active enemy.";
        return "";
    }
}
