// Chooses permitted reachable portal/edge entries; callers own debug or future wave timing.
using Godot;
using System.Collections.Generic;

public partial class SpawnManager : Node
{
    public EnemyRegistry Registry { get; private set; }
    public PathRequestManager Paths { get; private set; }
    public string LastMessage { get; private set; } = "F2: spawn Enemy001 at a permitted Portal.";
    private Node2D _ships;
    private int _sourceIndex, _cachedRevision = -1;
    private bool[] _reachable;
    private int _portalCount;
    private readonly List<string> _portalPaths = new();
    private readonly List<Vector2> _entries = new();

    // =========================================================
    // All runtime mechanics remain outside the physical Portal and level Debug nodes.
    public override void _Ready()
    {
        Registry = GetNode<EnemyRegistry>("../Registry");
        Paths = GetNode<PathRequestManager>("../Paths");
        _ships = GetNode<Node2D>("../../Enemies");
    }

    // =========================================================
    // Cache authored entries and connectivity until the navigation revision changes.
    private void RefreshSources()
    {
        NavigationGrid nav = Paths.Navigation;
        if (_cachedRevision == nav.Revision) return;
        _entries.Clear(); _portalPaths.Clear(); bool[] reachable = nav.ReachableWithout();
        if (_reachable == null || _reachable.Length != reachable.Length) _reachable = new bool[reachable.Length];
        System.Array.Copy(reachable, _reachable, reachable.Length);
        WorldGrid grid = nav.Grid; LevelConfig settings = grid.Level.Settings;
        if (settings.SpawnMode != LevelConfig.SpawnSourceMode.MapEdges)
            foreach (NodePath path in settings.ActivePortals)
            {
                Marker2D exit = grid.Level.GetNodeOrNull<Node2D>(path)?.GetNodeOrNull<Marker2D>("SpawnExit");
                if (exit != null && !_portalPaths.Contains(path.ToString()))
                { _entries.Add(exit.GlobalPosition); _portalPaths.Add(path.ToString()); }
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
        List<Vector2> sources = _entries;
        if (portalOnly)
        {
            sources = new();
            if (grid.Level.Settings.SpawnMode != LevelConfig.SpawnSourceMode.MapEdges)
                foreach (NodePath path in grid.Level.Settings.ActivePortals)
                {
                    Marker2D exit = grid.Level.GetNodeOrNull<Node2D>(path)?.GetNodeOrNull<Marker2D>("SpawnExit");
                    if (exit != null) sources.Add(exit.GlobalPosition);
                }
        }
        for (int i = 0; i < sources.Count; i++)
        {
            Vector2 position = sources[(_sourceIndex + i) % sources.Count];
            if (!nav.IsReachable(_reachable, grid.WorldToCell(position))
                || !nav.HasClearance(position, ship.Definition.RadiusPixels)) continue;
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
    public Enemy SpawnWave(PackedScene scene, RandomNumberGenerator random, string requiredPortal = "")
    {
        RefreshSources();
        if (scene == null || random == null) { LastMessage = "Wave enemy scene/RNG is missing."; return null; }
        if (Registry.Living.Count >= Paths.Config.MaxActiveEnemies)
        { LastMessage = "Active enemy safety limit reached; wave spawn will retry."; return null; }
        Enemy ship = scene.Instantiate<Enemy>();
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
        _ships.AddChild(ship); ship.GlobalPosition = _entries[chosen];
        ship.Initialize(Registry, Paths); LastMessage = "Wave spawned " + ship.Definition.DisplayName;
        return ship;
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
