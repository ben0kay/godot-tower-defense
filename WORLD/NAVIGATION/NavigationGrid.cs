// AStar adapter over the authoritative WorldGrid; no independent terrain rules.
// Placement connectivity uses one temporary flood fill, never one A* per spawn.
using Godot;
using System.Collections.Generic;
using System.Diagnostics;

public partial class NavigationGrid : Node
{
    #region Shared Goals and Lifecycle
    public WorldGrid Grid { get; private set; }
    public int Revision => Grid.NavigationRevision;
    public readonly List<Vector2I> Goals = new();
    public readonly List<CitadelDestination> Destinations = new();
    public CitadelDestination SingleDestination => Destinations.Count == 1 ? Destinations[0] : null;
    // =========================================================
    // Missing/retired multi-objective identities never default to a different Citadel.
    public IReadOnlyList<Vector2I> GoalsFor(CitadelDestination destination) => destination == null
        ? SingleDestination?.Goals ?? (IReadOnlyList<Vector2I>)System.Array.Empty<Vector2I>()
        : destination.Available ? destination.Goals : System.Array.Empty<Vector2I>();
    [Export] public EnemyMasterConfig Config { get; set; }
    public int CostRevision { get; private set; }
    public int CongestionRevision => _crowdRevision;
    public const int NeutralProfile = -1;
    public int SharedProfileCount => _profiles.Count - 1;
    public int TotalSharedProfileCount => _profiles.Count;
    private readonly List<Profile> _profiles = new();
    private readonly Dictionary<Vector2I, float> _crowd = new();
    private EnemyRegistry _registry;
    private float _crowdTimer;
    private int _crowdRevision;
    private (bool, float, int, int, int) _settings;
    private sealed class Profile
    {
        public AStarGrid2D AStar;
        public float[] BaseWeights;
        public bool Neutral;
        public readonly List<Vector2I> CrowdCells = new();
        public int CrowdRevision = -1;
        public float Influence = -1f;
    }
    private int _revision = -1;
    public int FullRebuildCount { get; private set; }
    public int IncrementalCellUpdates { get; private set; }
    public double LastAStarMilliseconds { get; private set; }
    public double LastGridUpdateMilliseconds { get; private set; }
    private bool[] _floodVisited, _solid;
    private int[] _floodQueue;

    // =========================================================
    // Initialize after Grid has cached the authored level and object footprints.
    public override void _Ready()
    {
        Grid = GetNode<WorldGrid>("../../Grid");
        _registry = GetNode<EnemyRegistry>("../Registry");
        Refresh();
        Grid.NavigationChanged += OnGridChanged; Grid.NavigationBatchChanged += OnGridBatchChanged;
    }

    // =========================================================
    // One explicitly owned debug pool isolates preview weights from every live enemy profile/cache.
    // Copy the current crowd snapshot once; previews never scan enemies or refresh congestion themselves.
    internal static NavigationGrid CreatePreview(NavigationGrid live, EnemyMasterConfig settings)
    {
        NavigationGrid preview = new() { Grid = live.Grid, Config = settings };
        preview.Refresh();
        foreach (var entry in live._crowd) preview._crowd.Add(entry.Key, entry.Value);
        preview._crowdRevision = live._crowdRevision;
        return preview;
    }

    // =========================================================
    // Detached debug pools have no tree lifecycle; release native grids explicitly on the main thread.
    internal void ReleasePreview()
    {
        foreach (Profile profile in _profiles) profile.AStar.Dispose();
        _profiles.Clear(); foreach (var destination in Destinations) destination.Dispose(); Free();
    }

    // =========================================================
    // Synchronize only on an occupancy/terrain revision; update() would reset solid flags.
    public void Refresh()
    {
        EnsureProfiles();
        if (_revision == Revision) return;
        long started = Stopwatch.GetTimestamp(); FullRebuildCount++;
        _crowd.Clear(); _crowdRevision++; _crowdTimer = 0f;
        _solid ??= new bool[Grid.Config.WidthCells * Grid.Config.HeightCells];
        for (int y = 0; y < Grid.Config.HeightCells; y++)
            for (int x = 0; x < Grid.Config.WidthCells; x++)
            {
                Vector2I cell = new(x, y); bool solid = Grid.BlocksEnemies(cell);
                _solid[y * Grid.Config.WidthCells + x] = solid;
                foreach (Profile profile in _profiles) profile.AStar.SetPointSolid(cell, solid);
            }
        RefreshGoals();
        _revision = Revision;
        LastGridUpdateMilliseconds = Stopwatch.GetElapsedTime(started).TotalMilliseconds;
    }

    // =========================================================
    // One tower modifies one cell in each shared profile; terrain reloads defer a full refresh.
    private void OnGridChanged(Vector2I? changed, bool blocked)
    {
        if (changed == null || _revision < 0) { _revision = -1; return; }
        long started = Stopwatch.GetTimestamp();
        foreach (Profile profile in _profiles) profile.AStar.SetPointSolid(changed.Value, blocked);
        _solid[changed.Value.Y * Grid.Config.WidthCells + changed.Value.X] = blocked;
        IncrementalCellUpdates++; _revision = Revision;
        RefreshGoals();
        LastGridUpdateMilliseconds = Stopwatch.GetElapsedTime(started).TotalMilliseconds;
    }

    // =========================================================
    // Apply the accepted rectangle incrementally once; the grid already contains its complete final occupancy.
    private void OnGridBatchChanged(Rect2I rectangle, bool blocked)
    {
        if (_revision < 0) return;
        long started = Stopwatch.GetTimestamp();
        for (int y = rectangle.Position.Y; y < rectangle.End.Y; y++) for (int x = rectangle.Position.X; x < rectangle.End.X; x++)
        {
            Vector2I cell = new(x, y); if (Grid.TerrainBlocksEnemies(cell)) continue;
            foreach (Profile profile in _profiles) profile.AStar.SetPointSolid(cell, blocked);
            _solid[y * Grid.Config.WidthCells + x] = blocked; IncrementalCellUpdates++;
        }
        _revision = Revision; RefreshGoals();
        LastGridUpdateMilliseconds = Stopwatch.GetElapsedTime(started).TotalMilliseconds;
    }

    // =========================================================
    // Goals are a few authored markers, so incremental changes never scan the whole map.
    private void RefreshGoals()
    {
        if (Destinations.Count == 1 && GodotObject.IsInstanceValid(Destinations[0].Root) && Grid.Level.Citadels.Count == 1 && Destinations[0].Root == Grid.Level.Citadels[0])
        {
            var only = Destinations[0]; only.Goals.Clear();
            Node markers = only.Root.GetNodeOrNull<Node>("ApproachPoints");
            if (markers != null) foreach (Node child in markers.GetChildren())
                if (child is Marker2D marker) { var cell = Grid.WorldToCell(marker.GlobalPosition); if (!Grid.BlocksEnemies(cell) && !only.Goals.Contains(cell)) only.Goals.Add(cell); }
            Goals.Clear(); Goals.AddRange(only.Goals); return;
        }
        foreach (var destination in Destinations) destination.Registered = false;
        List<CitadelDestination> ordered = new();
        foreach (Node2D root in Grid.Level.Citadels)
        {
            if (!GodotObject.IsInstanceValid(root)) continue;
            CitadelDestination destination = Destinations.Find(d => d.Root == root) ?? new(Grid.Level, root);
            destination.Registered = true; destination.Goals.Clear(); ordered.Add(destination);
            Node approaches = root.GetNodeOrNull<Node>("ApproachPoints");
            if (approaches != null) foreach (Node child in approaches.GetChildren())
                if (child is Marker2D marker)
                {
                    Vector2I cell = Grid.WorldToCell(marker.GlobalPosition);
                    if (!Grid.BlocksEnemies(cell) && !destination.Goals.Contains(cell)) destination.Goals.Add(cell);
                }
        }
        Destinations.Clear(); Destinations.AddRange(ordered);
        Goals.Clear(); if (Destinations.Count > 0) Goals.AddRange(Destinations[0].Goals);
    }

    #endregion

    #region Shared Route Calculations
    // =========================================================
    // One actual A* calculation; PathRequestManager bounds calls per physics frame.
    public Vector2[] FindPath(Vector2 position, Vector2I goal) => FindPath(position, goal, 0, out _);

    // =========================================================
    // Each profile has its own shared cache; no temporary weights leak into another request.
    public Vector2[] FindPath(Vector2 position, Vector2I goal, int profileId, out float cost)
    {
        Refresh(); cost = float.PositiveInfinity; LastAStarMilliseconds = 0;
        Vector2I start = Grid.WorldToCell(position);
        if (Grid.BlocksEnemies(start) || Grid.BlocksEnemies(goal)) return System.Array.Empty<Vector2>();
        Profile profile = ResolveProfile(profileId);
        ApplyCrowd(profile);
        long calculationStarted = Stopwatch.GetTimestamp();
        Godot.Collections.Array<Vector2I> cells = profile.AStar.GetIdPath(start, goal);
        LastAStarMilliseconds = Stopwatch.GetElapsedTime(calculationStarted).TotalMilliseconds;
        Vector2[] points = new Vector2[cells.Count];
        if (cells.Count == 0) return points;
        cost = position.DistanceTo(Grid.CellToWorldCenter(cells[0])) / Grid.Config.CellSize;
        for (int i = 0; i < cells.Count; i++)
        {
            points[i] = Grid.CellToWorldCenter(cells[i]);
            if (i > 0) cost += ((Vector2)(cells[i] - cells[i - 1])).Length()
                * profile.AStar.GetPointWeightScale(cells[i]);
        }
        return points;
    }

    // =========================================================
    // Debug queries share deterministic caches but leave live path-manager timing diagnostics unchanged.
    public Vector2[] FindPreviewPath(Vector2 position, Vector2I goal, int profile, out float cost)
    {
        double previous = LastAStarMilliseconds;
        try { return FindPath(position, goal, profile, out cost); }
        finally { LastAStarMilliseconds = previous; }
    }

    // =========================================================
    // Reserved neutral ID never aliases weighted profile zero or receives congestion weights.
    private Profile ResolveProfile(int id) => id == NeutralProfile ? _profiles[^1]
        : _profiles[((id % SharedProfileCount) + SharedProfileCount) % SharedProfileCount];

    // =========================================================
    // Re-score an earlier candidate against today's snapshot without another A* calculation.
    public float RouteCost(Vector2 position, Vector2[] route, int profileId)
    {
        if (route.Length == 0) return float.PositiveInfinity;
        Profile profile = ResolveProfile(profileId);
        float influence = !profile.Neutral && Config.EnablePathDiversity ? Mathf.Max(0f, Config.CongestionInfluence) : 0f;
        float cost = position.DistanceTo(route[0]) / Grid.Config.CellSize;
        for (int i = 1; i < route.Length; i++)
        {
            Vector2I cell = Grid.WorldToCell(route[i]);
            _crowd.TryGetValue(cell, out float density);
            cost += route[i - 1].DistanceTo(route[i]) / Grid.Config.CellSize
                * (profile.BaseWeights[cell.Y * Grid.Config.WidthCells + cell.X] + influence * density);
        }
        return cost;
    }

    // =========================================================
    // Build a bounded pool only on configuration changes, never a full grid per enemy.
    private void EnsureProfiles()
    {
        var settings = (Config.EnablePathDiversity, Mathf.Max(0f, Config.PathDiversityStrength),
            Mathf.Clamp(Config.RouteProfileCount, 1, 16), Mathf.Max(2, Config.PreferencePatchCells), Config.RoutePreferenceSeed);
        if (_profiles.Count > 0 && _settings == settings) return;
        foreach (Profile profile in _profiles) profile.AStar.Dispose();
        _profiles.Clear(); _settings = settings; CostRevision++; _revision = -1;
        int count = settings.Item1 ? settings.Item3 : 1;
        for (int id = 0; id <= count; id++)
        {
            Profile profile = new() { Neutral = id == count, AStar = new AStarGrid2D(),
                BaseWeights = new float[Grid.Config.WidthCells * Grid.Config.HeightCells] };
            profile.AStar.Region = new Rect2I(0, 0, Grid.Config.WidthCells, Grid.Config.HeightCells);
            profile.AStar.DiagonalMode = AStarGrid2D.DiagonalModeEnum.OnlyIfNoObstacles;
            profile.AStar.DefaultComputeHeuristic = AStarGrid2D.Heuristic.Octile;
            profile.AStar.DefaultEstimateHeuristic = AStarGrid2D.Heuristic.Octile;
            profile.AStar.JumpingEnabled = false; // Godot jumping bypasses point weights.
            profile.AStar.Update();
            for (int y = 0; y < Grid.Config.HeightCells; y++)
                for (int x = 0; x < Grid.Config.WidthCells; x++)
                {
                    float weight = 1f + (settings.Item1 && !profile.Neutral ? settings.Item2 * Preference(x, y, id) : 0f);
                    profile.BaseWeights[y * Grid.Config.WidthCells + x] = weight;
                    profile.AStar.SetPointWeightScale(new(x, y), weight);
                }
            _profiles.Add(profile);
        }
    }

    // =========================================================
    // Smooth seeded value noise creates broad preferences, not random tile-by-tile zigzags.
    private float Preference(int x, int y, int profile)
    {
        float px = (float)x / _settings.Item4, py = (float)y / _settings.Item4;
        int ix = Mathf.FloorToInt(px), iy = Mathf.FloorToInt(py);
        float tx = Mathf.SmoothStep(0f, 1f, px - ix), ty = Mathf.SmoothStep(0f, 1f, py - iy);
        return Mathf.Lerp(Mathf.Lerp(Noise(ix, iy, profile), Noise(ix + 1, iy, profile), tx),
            Mathf.Lerp(Noise(ix, iy + 1, profile), Noise(ix + 1, iy + 1, profile), tx), ty);
    }

    // =========================================================
    // Integer hashing is deterministic across game runs and does not mutate global RNG state.
    private float Noise(int x, int y, int profile)
    {
        uint hash = unchecked((uint)_settings.Item5 ^ (uint)x * 374761393u ^ (uint)y * 668265263u ^ (uint)(profile + 1) * 2246822519u);
        hash = unchecked((hash ^ (hash >> 13)) * 1274126177u); hash ^= hash >> 16;
        return (hash & 0x00ffffffu) / 16777215f;
    }

    // =========================================================
    // O(living ships) at a throttled interval, with bounded density and no route recalculations.
    public void UpdateCongestion(double delta)
    {
        _crowdTimer -= (float)delta;
        if (_crowdTimer > 0f) return;
        _crowdTimer = Mathf.Max(0.1f, Config.CongestionSnapshotSeconds);
        _crowd.Clear(); _crowdRevision++;
        if (!Config.EnablePathDiversity || Config.CongestionInfluence <= 0f) return;
        foreach (Enemy enemy in _registry.Living)
        {
            Vector2I center = Grid.WorldToCell(enemy.GlobalPosition);
            for (int y = -1; y <= 1; y++)
                for (int x = -1; x <= 1; x++)
                {
                    Vector2I cell = center + new Vector2I(x, y);
                    if (!Grid.IsInsideMap(cell)) continue;
                    _crowd.TryGetValue(cell, out float density);
                    _crowd[cell] = Mathf.Min(8f, density + (x == 0 && y == 0 ? 1f : 0.25f));
                }
        }
    }

    // =========================================================
    // Restore the previous sparse snapshot before applying a new one to this shared profile.
    private void ApplyCrowd(Profile profile)
    {
        float influence = !profile.Neutral && Config.EnablePathDiversity ? Mathf.Max(0f, Config.CongestionInfluence) : 0f;
        if (profile.CrowdRevision == _crowdRevision && profile.Influence == influence) return;
        foreach (Vector2I cell in profile.CrowdCells)
            profile.AStar.SetPointWeightScale(cell, profile.BaseWeights[cell.Y * Grid.Config.WidthCells + cell.X]);
        profile.CrowdCells.Clear();
        if (influence > 0f)
            foreach (var entry in _crowd)
            {
                profile.AStar.SetPointWeightScale(entry.Key,
                    profile.BaseWeights[entry.Key.Y * Grid.Config.WidthCells + entry.Key.X] + influence * entry.Value);
                profile.CrowdCells.Add(entry.Key);
            }
        profile.CrowdRevision = _crowdRevision; profile.Influence = influence;
    }

    #endregion

    #region Clearance and Connectivity
    // =========================================================
    // Circle-vs-cell clearance protects continuous movement and placement near ships.
    public bool HasClearance(Vector2 position, float radius, Vector2I? extraBlock = null) =>
        HasClearance(position, radius, extraBlock.HasValue ? new Rect2I(extraBlock.Value, Vector2I.One) : default);

    // =========================================================
    // Rectangle proposal checks only tiles intersecting the enemy circle, never every footprint tile per enemy.
    public bool HasClearance(Vector2 position, float radius, Rect2I proposal)
    {
        Vector2 local = Grid.ToLocal(position);
        float size = Grid.Config.CellSize;
        Vector2I min = Grid.WorldToCell(position - Vector2.One * radius);
        Vector2I max = Grid.WorldToCell(position + Vector2.One * radius);
        for (int y = min.Y; y <= max.Y; y++)
            for (int x = min.X; x <= max.X; x++)
            {
                Vector2I cell = new(x, y);
                if (!Grid.BlocksEnemies(cell) && !proposal.HasPoint(cell)) continue;
                Vector2 closest = new(Mathf.Clamp(local.X, x * size, (x + 1) * size),
                    Mathf.Clamp(local.Y, y * size, (y + 1) * size));
                if (local.DistanceSquaredTo(closest) <= radius * radius) return false;
            }
        return true;
    }

    // =========================================================
    // Sample the swept segment, including both endpoints, to prevent high-speed tunneling.
    public bool SegmentClear(Vector2 from, Vector2 to, float radius)
    {
        int steps = Mathf.Max(1, Mathf.CeilToInt(from.DistanceTo(to) / Mathf.Max(1f, radius * 0.5f)));
        for (int i = 0; i <= steps; i++)
            if (!HasClearance(from.Lerp(to, (float)i / steps), radius)) return false;
        return true;
    }

    // =========================================================
    // Handoff visibility also respects corner-safe cell transitions for tiny-radius ships.
    public bool ConnectionClear(Vector2 from, Vector2 to, float radius)
    {
        if (!SegmentClear(from, to, radius)) return false;
        int steps = Mathf.Max(1, Mathf.CeilToInt(from.DistanceTo(to) / Mathf.Max(0.5f, Mathf.Min(Grid.Config.CellSize * 0.1f, radius * 0.5f))));
        Vector2I previous = Grid.WorldToCell(from);
        for (int i = 1; i <= steps; i++)
        {
            Vector2I cell = Grid.WorldToCell(from.Lerp(to, (float)i / steps));
            Vector2I step = cell - previous;
            if (step.X != 0 && step.Y != 0 && (Grid.BlocksEnemies(previous + new Vector2I(step.X, 0))
                || Grid.BlocksEnemies(previous + new Vector2I(0, step.Y)))) return false;
            previous = cell;
        }
        return true;
    }

    // =========================================================
    // Flood from all valid goals with a candidate tower cell temporarily excluded.
    public bool[] ReachableWithout(Vector2I? blocked = null) =>
        ReachableWithout(blocked.HasValue ? new Rect2I(blocked.Value, Vector2I.One) : default);

    public int PlacementFloodCount { get; private set; }
    // =========================================================
    // One scratch-buffer flood excludes the complete proposal, with no live A* or occupancy mutation.
    public bool[] ReachableWithout(Rect2I proposal) { PlacementFloodCount++; return Flood(proposal, null); }

    // =========================================================
    // Death-release clearance validates connectivity to the inherited next checkpoint, not only the Citadel.
    public bool[] ReachableTo(Vector2[] targets) => Flood(default, targets);
    // =========================================================
    // Destination-specific proposal flood reuses the same buffers; callers consume before the next flood.
    public bool[] ReachableFor(CitadelDestination destination, Rect2I proposal = default, float radius = 0)
    { PlacementFloodCount++; return Flood(proposal, null, GoalsFor(destination), radius); }
    public bool[] ReachableTo(Vector2[] targets, Rect2I proposal) => Flood(proposal, targets);
    private bool[] Flood(Rect2I proposal, Vector2[] targets, IReadOnlyList<Vector2I> goals = null, float radius = 0)
    {
        Refresh();
        int width = Grid.Config.WidthCells, count = width * Grid.Config.HeightCells;
        if (_floodVisited == null || _floodVisited.Length != count)
        { _floodVisited = new bool[count]; _floodQueue = new int[count]; }
        System.Array.Clear(_floodVisited);
        int head = 0, tail = 0;
        goals ??= Goals;
        int destinations = targets?.Length ?? goals.Count;
        for (int i = 0; i < destinations; i++)
        {
            Vector2I goal = targets == null ? goals[i] : Grid.WorldToCell(targets[i]);
            if (Grid.IsInsideMap(goal) && !Grid.BlocksEnemies(goal) && !proposal.HasPoint(goal)
                && (radius <= 0 || HasClearance(Grid.CellToWorldCenter(goal), radius, proposal)))
            {
                int index = goal.Y * width + goal.X;
                if (_floodVisited[index]) continue;
                _floodVisited[index] = true; _floodQueue[tail++] = index;
            }
        }
        while (head < tail)
        {
            int index = _floodQueue[head++], x = index % width;
            if (x > 0) Visit(index - 1);
            if (x + 1 < width) Visit(index + 1);
            if (index >= width) Visit(index - width);
            if (index + width < count) Visit(index + width);
        }
        // Four-way flood has identical connectivity to corner-safe diagonals: every allowed
        // diagonal also has walkable cardinal links. This avoids redundant diagonal tests.
        void Visit(int index)
        {
            if (_floodVisited[index] || _solid[index] || proposal.HasPoint(new Vector2I(index % width, index / width))) return;
            if (radius > 0 && !HasClearance(Grid.CellToWorldCenter(new(index % width, index / width)), radius, proposal)) return;
            _floodVisited[index] = true; _floodQueue[tail++] = index;
        }
        return _floodVisited;
    }

    // =========================================================
    // One component map per radius/revision/proposal distinguishes disconnected approach regions.
    // Callers retain this event-time result across all sources/ships; no per-enemy flood or A*.
    public int[] ConnectedComponents(Rect2I proposal = default, float radius = 0)
    {
        Refresh(); int width = Grid.Config.WidthCells, count = width * Grid.Config.HeightCells;
        int[] labels = new int[count]; _floodQueue ??= new int[count]; int component = 0;
        for (int start = 0; start < count; start++)
        {
            if (labels[start] != 0) continue;
            int head = 0, tail = 0;
            if (!Open(start)) { labels[start] = -1; continue; }
            labels[start] = ++component; _floodQueue[tail++] = start;
            while (head < tail)
            {
                int index = _floodQueue[head++], x = index % width;
                if (x > 0) Visit(index - 1); if (x + 1 < width) Visit(index + 1);
                if (index >= width) Visit(index - width); if (index + width < count) Visit(index + width);
            }
            void Visit(int index)
            {
                if (labels[index] != 0) return;
                if (!Open(index)) { labels[index] = -1; return; }
                labels[index] = component; _floodQueue[tail++] = index;
            }
        }
        return labels;
        bool Open(int index)
        {
            Vector2I cell = new(index % width, index / width);
            return !_solid[index] && !proposal.HasPoint(cell) && (radius <= 0 || HasClearance(Grid.CellToWorldCenter(cell), radius, proposal));
        }
    }
    // =========================================================
    // Constant-time component reads: zero means outside/blocked, never a usable region.
    public int ComponentAt(int[] labels, Vector2 position)
    { Vector2I cell = Grid.WorldToCell(position); return Grid.IsInsideMap(cell) ? System.Math.Max(0, labels[cell.Y * Grid.Config.WidthCells + cell.X]) : 0; }

    // =========================================================
    // Shared bounds-safe query into a connectivity result.
    public bool IsReachable(bool[] reached, Vector2I cell) =>
        Grid.IsInsideMap(cell) && reached[cell.Y * Grid.Config.WidthCells + cell.X];

    // =========================================================
    // Release the native pathfinding resource with the world.
    public override void _ExitTree() { foreach (var destination in Destinations) destination.Dispose(); if (GodotObject.IsInstanceValid(Grid)) { Grid.NavigationChanged -= OnGridChanged; Grid.NavigationBatchChanged -= OnGridBatchChanged; } foreach (Profile profile in _profiles) profile.AStar.Dispose(); }
    #endregion
}
