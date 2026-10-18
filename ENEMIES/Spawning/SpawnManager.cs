// Chooses permitted reachable portal/edge entries; callers own debug or future wave timing.
using Godot;
using System.Collections.Generic;

public partial class SpawnManager : Node
{
    #region Services and Sources
    public PortalCitadelAssignments CitadelAssignments { get; private set; }
    private bool _reassignCitadels = true;
    private bool[] _sourceReachable;
    public event System.Action<Enemy> WaveEnemyCreated;
    public DeathReleaseQueue DeathReleases { get; private set; }
    internal WaveManager WaveOwner { get; set; }
    internal void NotifyWaveChild(Enemy child) => WaveEnemyCreated?.Invoke(child);
    [Export] public EnemyCatalog Catalog { get; set; }
    public EnemyRegistry Registry { get; private set; }
    public PathRequestManager Paths { get; private set; }
    public string LastMessage { get; private set; } = "F2: spawn next registered enemy at a permitted Portal.";
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
        Paths.Navigation.Grid.TerrainRebuilt += InvalidateSources;
        DeathReleases = new DeathReleaseQueue { Name = "DeathReleases" }; AddChild(DeathReleases);
        CitadelAssignments = new(Paths.Navigation);
        RefreshSources();
    }

    // =========================================================
    // Explicit level edits can change portal/edge policy without changing walkability or repathing enemies.
    private void InvalidateSources() { _cachedRevision = -1; _reassignCitadels = true; }
    // =========================================================
    // Release the level edit subscription with its spawning service.
    public override void _ExitTree()
    { if (GodotObject.IsInstanceValid(Paths?.Navigation?.Grid)) Paths.Navigation.Grid.TerrainRebuilt -= InvalidateSources; }

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
        if (settings.SpawnMode != LevelConfig.SpawnSourceMode.Portals && nav.Destinations.Count == 1)
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
        if (nav.Destinations.Count > 1 && settings.SpawnMode != LevelConfig.SpawnSourceMode.Portals)
            GD.PushWarning("Multi-Citadel edge spawning is disabled; author portals for this level.");
        if (_reassignCitadels) { CitadelAssignments.Capture(_portalObjects, _entries); _reassignCitadels = false; }
        if (_sourceReachable == null || _sourceReachable.Length != _entries.Count) _sourceReachable = new bool[_entries.Count];
        for (int i = 0; i < _entries.Count; i++)
            _sourceReachable[i] = nav.Destinations.Count == 1 ? nav.IsReachable(_reachable, grid.WorldToCell(_entries[i]))
                : i < _portalCount && CitadelAssignments.CanSpawn(_portalObjects[i], _entries[i], 0);
        for (int i = 0; i < _portalCount; i++)
            if (!_sourceReachable[i] || !nav.HasClearance(_entries[i], 0f))
                GD.PushWarning("Portal SpawnExit is blocked or cannot reach the Citadel: " + _portalPaths[i]);
        if (nav.Goals.Count == 0)
            GD.PushWarning("No usable Citadel approach markers; enemies cannot find a destination.");
        _cachedRevision = nav.Revision;
    }

    #endregion

    #region Spawn Delivery
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
        int sourceCount = portalOnly ? _portalCount : sources.Count;
        for (int i = 0; i < sourceCount; i++)
        {
            Vector2 position = sources[(_sourceIndex + i) % sourceCount];
            if (!SourceUsable((_sourceIndex + i) % sourceCount, ship.Definition.RadiusPixels)
                || !nav.HasClearance(position, ship.Definition.RadiusPixels)) continue;
            int source = (_sourceIndex + i) % sourceCount;
            if (portalOnly || source < _portalCount) position = OffsetPortal(position, ship.Definition.RadiusPixels, _spawnRandom, portals[source]);
            _sourceIndex = (_sourceIndex + i + 1) % sourceCount;
            _ships.AddChild(ship); ship.GlobalPosition = position;
            EnemyPortal chosenPortal = portalOnly || source < _portalCount ? portals[source] as EnemyPortal : null;
            ship.Initialize(Registry, Paths, checkpoint: chosenPortal?.ResolveCheckpoint(), requiresCheckpoint: chosenPortal?.RequiresCheckpoint == true, assignedCitadel: source < _portalCount ? CitadelAssignments.For(portals[source]) : nav.SingleDestination); LastMessage = "Spawned " + ship.Definition.DisplayName;
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
            if (!SourceUsable(i, ship.Definition.RadiusPixels)
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
        EnemyPortal chosenPortal = chosen < _portalCount ? _portalObjects[chosen] as EnemyPortal : null;
        WaveEnemyCreated?.Invoke(ship);
        ship.Initialize(Registry, Paths, spawnWithShield, chosenPortal?.ResolveCheckpoint(), chosenPortal?.RequiresCheckpoint == true, assignedCitadel: chosen < _portalCount ? CitadelAssignments.For(_portalObjects[chosen]) : nav.SingleDestination); LastMessage = "Wave spawned " + ship.Definition.DisplayName;
        return ship;
    }

    // =========================================================
    // Death children use captured positions, no portal/edge/wave RNG; one destination flood is shared by all probes.
    internal Enemy SpawnDeathChild(DeathReleaseJob job)
    {
        if (Registry.Living.Count >= Paths.Config.MaxActiveEnemies) { LastMessage = "Death release skipped: active enemy limit."; return null; }
        NavigationGrid nav = Paths.Navigation; float radius = job.Definition.RadiusPixels;
        if (!float.IsFinite(radius) || radius <= 0 || radius >= nav.Grid.Config.CellSize * .5f)
        { LastMessage = "Death release skipped: unsupported child radius."; return null; }
        bool[] reachable;
        if (job.Route is { Required: true, Complete: false } route)
        {
            if (route.Error.Length != 0 || !GodotObject.IsInstanceValid(route.Checkpoint))
            { LastMessage = "Death release skipped: invalid required checkpoint."; return null; }
            reachable = nav.ReachableTo(route.Checkpoint.Targets(nav, radius));
        }
        else { RefreshSources(); reachable = nav.Destinations.Count == 1 ? _reachable : nav.ReachableFor(job.Destination, radius: radius); }
        if (job.Destination?.Available != true) return null;
        if (nav.Destinations.Count > 1)
        {
            reachable = nav.ReachableFor(job.Destination, radius: radius);

        }
        Vector2? valid = null;
        for (int attempt = 0; attempt < job.Attempts; attempt++)
        {
            uint hash = CorridorPreference.Hash(job.Seed ^ unchecked((uint)(job.Index * 37 + attempt + 1) * 0x85ebca6bu));
            float jitter = (hash & 65535u) / 65535f;
            float angle = Mathf.Tau * (job.Index + .15f + jitter * .5f) / job.Count;
            float distance = job.Radius * (attempt == job.Attempts - 1 ? 0 : (0.55f + jitter * .45f) * (1f - (float)attempt / job.Attempts));
            Vector2 position = job.Position + Vector2.FromAngle(angle) * distance;
            if (!nav.IsReachable(reachable, nav.Grid.WorldToCell(position)) || !nav.HasClearance(position, radius)) continue;
            if (nav.Destinations.Count > 1 || job.Route is { Required: true, Complete: false })
            {
                var labels = CitadelAssignments.Connectivity(radius); int component = nav.ComponentAt(labels, position);
                if (!PortalCitadelAssignments.GoalInComponent(nav, job.Destination, labels, component)) continue;
                if (job.Route is { Required: true, Complete: false } obligations &&
                    !PortalCitadelAssignments.CheckpointsInComponent(nav, labels, component, obligations.Checkpoint, obligations.Network, radius)) continue;
            }
            valid = position; break;
        }
        if (!valid.HasValue) { LastMessage = "Death release skipped: no safe reachable local position."; return null; }
        Enemy child = null;
        try
        {
            child = job.Scene.Instantiate<Enemy>(); _ships.AddChild(child); child.GlobalPosition = valid.Value;
            job.Reservation?.Attach(child);
            child.Initialize(Registry, Paths, job.Shield, routeHandoff: job.Route, generation: job.Generation, assignedCitadel: job.Destination);
            LastMessage = "Released " + child.Definition.DisplayName; return child;
        }
        catch (System.Exception error)
        {
            if (child != null) job.Reservation?.Discard(child);
            if (GodotObject.IsInstanceValid(child)) child.Free();
            LastMessage = "Death release creation failed: " + error.Message; GD.PushWarning(LastMessage); return null;
        }
    }

    // =========================================================
    // Single-Citadel spawns retain the old cached connectivity path; multi uses cached source/radius validation.
    private bool SourceUsable(int source, float radius) => _sourceReachable[source] &&
        (Paths.Navigation.Destinations.Count == 1 ? Paths.Navigation.SingleDestination.Available : source < _portalCount && CitadelAssignments.CanSpawn(_portalObjects[source], _entries[source], radius));

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
            if ((nav.Destinations.Count > 1 || nav.IsReachable(_reachable, nav.Grid.WorldToCell(point)))
                && nav.HasClearance(point, radius) && nav.SegmentClear(center, point, radius)) return point;
        }
        return center;
    }

    #endregion

    #region Placement Validation
    // =========================================================
    // Click-only route preservation: protect active ships and every currently usable source.
    public double LastPlacementValidationMilliseconds { get; private set; }

    // =========================================================
    // Keep timing independent of early rejection paths.
    public string PlacementFailure(Vector2I cell) => PlacementFailure(cell, Vector2I.One);
    public string PlacementFailure(Vector2I cell, Vector2I footprint)
    {
        long started = System.Diagnostics.Stopwatch.GetTimestamp();
        try { return ValidatePlacement(cell, footprint); }
        finally { LastPlacementValidationMilliseconds = System.Diagnostics.Stopwatch.GetElapsedTime(started).TotalMilliseconds; }
    }

    // =========================================================
    // Full connectivity is still checked before commit, using reusable flood-fill scratch space.
    private string ValidatePlacement(Vector2I cell, Vector2I footprint)
    {
        NavigationGrid nav = Paths.Navigation;
        string failure = nav.Grid.PlacementFailure(cell, footprint); if (failure.Length != 0) return failure;
        // Existing solid foundations introduce no obstacle, flood, clearance scan or route work.
        if (footprint != Vector2I.One && nav.Grid.TerrainBlocksEnemies(cell)) return "";
        Rect2I proposal = new(cell, footprint);
        RefreshSources();
        foreach (Enemy enemy in Registry.Living)
            if (!nav.HasClearance(enemy.GlobalPosition, enemy.Definition.RadiusPixels, proposal))
                return "An enemy is too close to this cell.";
        if (nav.Grid.BlocksEnemies(cell)) return "";
        if (nav.Destinations.Count > 1 || HasCheckpointObligations()) return ValidateAssignedRoutes(proposal);
        bool[] after = nav.ReachableWithout(proposal);
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
    #endregion

    #region Assigned Route Placement
    // =========================================================
    // Only levels with mandatory checkpoint routes need the extended single-objective validation.
    private bool HasCheckpointObligations()
    {
        foreach (var portal in _portalObjects) if (portal is EnemyPortal typed && typed.RequiresCheckpoint) return true;
        foreach (var enemy in Registry.Living) if (enemy.Movement.HeadingToCheckpoint) return true;
        return false;
    }
    // =========================================================
    // Share clearance-component floods across sources/ships of the same destination and radius per click.
    private string ValidateAssignedRoutes(Rect2I proposal)
    {
        NavigationGrid nav = Paths.Navigation;
        Dictionary<float, int[]> regions = new();
        HashSet<(int, RouteCheckpoint, CheckpointNetwork, float)> checkedChains = new();
        int[] Region(float radius)
        {
            if (!regions.TryGetValue(radius, out var labels)) regions[radius] = labels = nav.ConnectedComponents(proposal, radius);
            return labels;
        }
        bool Chain(float radius, int component, RouteCheckpoint root, CheckpointNetwork network)
        {
            var key = (component, root, network, radius);
            if (!checkedChains.Add(key)) return true;
            return PortalCitadelAssignments.CheckpointsInComponent(nav, Region(radius), component, root, network, radius, proposal);
        }
        for (int i = 0; i < _portalCount; i++)
        {
            if (!_sourceReachable[i]) continue;
            var destination = CitadelAssignments.For(_portalObjects[i]);
            int[] labels = Region(0); int component = nav.ComponentAt(labels, _entries[i]);
            if (!PortalCitadelAssignments.GoalInComponent(nav, destination, labels, component))
                return "This tower would seal a portal's assigned Citadel route.";
            if (_portalObjects[i] is EnemyPortal portal && portal.RequiresCheckpoint)
            {
                var root = portal.ResolveCheckpoint();
                if (!Chain(0, component, root, CheckpointNetwork.Capture(root))) return "This tower would seal a required checkpoint route.";
            }
        }
        for (int i = _portalCount; i < _entries.Count; i++)
            if (_sourceReachable[i] && !PortalCitadelAssignments.GoalInComponent(nav, nav.SingleDestination, Region(0), nav.ComponentAt(Region(0), _entries[i])))
                return "This tower would seal an edge entry route.";
        foreach (Enemy enemy in Registry.Living)
        {
            var destination = enemy.AssignedCitadel; float radius = enemy.Definition.RadiusPixels;
            int[] labels = Region(radius); int component = nav.ComponentAt(labels, enemy.GlobalPosition);
            if (!PortalCitadelAssignments.GoalInComponent(nav, destination, labels, component))
                return "This tower would trap an enemy away from its assigned Citadel.";
            var movement = enemy.Movement;
            if (movement.HeadingToCheckpoint && !Chain(radius, component, movement.Checkpoint, movement.CapturedNetwork))
                return "This tower would seal an enemy's remaining checkpoint chain.";
        }
        return "";
    }
    #endregion

}
