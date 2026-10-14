// AStar adapter over the authoritative WorldGrid; no independent terrain rules.
// Placement connectivity uses one temporary flood fill, never one A* per spawn.
using Godot;
using System.Collections.Generic;

public partial class NavigationGrid : Node
{
    public WorldGrid Grid { get; private set; }
    public int Revision => Grid.NavigationRevision;
    public readonly List<Vector2I> Goals = new();
    [Export] public EnemyMasterConfig Config { get; set; }
    public int CostRevision { get; private set; }
    public int SharedProfileCount => _profiles.Count;
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
        public readonly List<Vector2I> CrowdCells = new();
        public int CrowdRevision = -1;
        public float Influence = -1f;
    }
    private int _revision = -1;

    // =========================================================
    // Initialize after Grid has cached the authored level and object footprints.
    public override void _Ready()
    {
        Grid = GetNode<WorldGrid>("../../Grid");
        _registry = GetNode<EnemyRegistry>("../Registry");
        Refresh();
    }

    // =========================================================
    // Synchronize only on an occupancy/terrain revision; update() would reset solid flags.
    public void Refresh()
    {
        EnsureProfiles();
        if (_revision == Revision) return;
        _crowd.Clear(); _crowdRevision++; _crowdTimer = 0f;
        for (int y = 0; y < Grid.Config.HeightCells; y++)
            for (int x = 0; x < Grid.Config.WidthCells; x++)
                foreach (Profile profile in _profiles)
                    profile.AStar.SetPointSolid(new Vector2I(x, y), Grid.BlocksEnemies(new Vector2I(x, y)));
        Goals.Clear();
        Node approaches = Grid.Level.Citadel?.GetNodeOrNull<Node>("ApproachPoints");
        if (approaches != null)
            foreach (Node child in approaches.GetChildren())
                if (child is Marker2D marker)
                {
                    Vector2I cell = Grid.WorldToCell(marker.GlobalPosition);
                    if (!Grid.BlocksEnemies(cell) && !Goals.Contains(cell)) Goals.Add(cell);
                }
        _revision = Revision;
    }

    // =========================================================
    // One actual A* calculation; PathRequestManager bounds calls per physics frame.
    public Vector2[] FindPath(Vector2 position, Vector2I goal) => FindPath(position, goal, 0, out _);

    // =========================================================
    // Each profile has its own shared cache; no temporary weights leak into another request.
    public Vector2[] FindPath(Vector2 position, Vector2I goal, int profileId, out float cost)
    {
        Refresh(); cost = float.PositiveInfinity;
        Vector2I start = Grid.WorldToCell(position);
        if (Grid.BlocksEnemies(start) || Grid.BlocksEnemies(goal)) return System.Array.Empty<Vector2>();
        Profile profile = _profiles[((profileId % _profiles.Count) + _profiles.Count) % _profiles.Count];
        ApplyCrowd(profile);
        Godot.Collections.Array<Vector2I> cells = profile.AStar.GetIdPath(start, goal);
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
    // Re-score an earlier candidate against today's snapshot without another A* calculation.
    public float RouteCost(Vector2 position, Vector2[] route, int profileId)
    {
        if (route.Length == 0) return float.PositiveInfinity;
        Profile profile = _profiles[((profileId % _profiles.Count) + _profiles.Count) % _profiles.Count];
        float influence = Config.EnablePathDiversity ? Mathf.Max(0f, Config.CongestionInfluence) : 0f;
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
            Mathf.Clamp(Config.RouteProfileCount, 1, 8), Mathf.Max(2, Config.PreferencePatchCells), Config.RoutePreferenceSeed);
        if (_profiles.Count > 0 && _settings == settings) return;
        foreach (Profile profile in _profiles) profile.AStar.Dispose();
        _profiles.Clear(); _settings = settings; CostRevision++; _revision = -1;
        int count = settings.Item1 ? settings.Item3 : 1;
        for (int id = 0; id < count; id++)
        {
            Profile profile = new() { AStar = new AStarGrid2D(),
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
                    float weight = 1f + (settings.Item1 ? settings.Item2 * Preference(x, y, id) : 0f);
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
        float influence = Config.EnablePathDiversity ? Mathf.Max(0f, Config.CongestionInfluence) : 0f;
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

    // =========================================================
    // Circle-vs-cell clearance protects continuous movement and placement near ships.
    public bool HasClearance(Vector2 position, float radius, Vector2I? extraBlock = null)
    {
        Vector2 local = Grid.ToLocal(position);
        float size = Grid.Config.CellSize;
        Vector2I min = Grid.WorldToCell(position - Vector2.One * radius);
        Vector2I max = Grid.WorldToCell(position + Vector2.One * radius);
        for (int y = min.Y; y <= max.Y; y++)
            for (int x = min.X; x <= max.X; x++)
            {
                Vector2I cell = new(x, y);
                if (!Grid.BlocksEnemies(cell) && cell != extraBlock) continue;
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
    // Flood from all valid goals with a candidate tower cell temporarily excluded.
    public bool[] ReachableWithout(Vector2I? blocked = null)
    {
        Refresh();
        int width = Grid.Config.WidthCells;
        bool[] reached = new bool[width * Grid.Config.HeightCells];
        Queue<Vector2I> queue = new();
        foreach (Vector2I goal in Goals)
            if (goal != blocked) { reached[goal.Y * width + goal.X] = true; queue.Enqueue(goal); }
        while (queue.Count > 0)
        {
            Vector2I cell = queue.Dequeue();
            for (int y = -1; y <= 1; y++)
                for (int x = -1; x <= 1; x++)
                {
                    if (x == 0 && y == 0) continue;
                    Vector2I next = cell + new Vector2I(x, y);
                    if (Grid.BlocksEnemies(next) || next == blocked) continue;
                    if (x != 0 && y != 0 && (Grid.BlocksEnemies(cell + new Vector2I(x, 0))
                        || Grid.BlocksEnemies(cell + new Vector2I(0, y))
                        || cell + new Vector2I(x, 0) == blocked || cell + new Vector2I(0, y) == blocked)) continue;
                    int index = next.Y * width + next.X;
                    if (reached[index]) continue;
                    reached[index] = true; queue.Enqueue(next);
                }
        }
        return reached;
    }

    // =========================================================
    // Shared bounds-safe query into a connectivity result.
    public bool IsReachable(bool[] reached, Vector2I cell) =>
        Grid.IsInsideMap(cell) && reached[cell.Y * Grid.Config.WidthCells + cell.X];

    // =========================================================
    // Release the native pathfinding resource with the world.
    public override void _ExitTree() { foreach (Profile profile in _profiles) profile.AStar.Dispose(); }
}
