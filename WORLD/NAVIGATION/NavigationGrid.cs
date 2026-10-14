// AStar adapter over the authoritative WorldGrid; no independent terrain rules.
// Placement connectivity uses one temporary flood fill, never one A* per spawn.
using Godot;
using System.Collections.Generic;

public partial class NavigationGrid : Node
{
    public WorldGrid Grid { get; private set; }
    public int Revision => Grid.NavigationRevision;
    public readonly List<Vector2I> Goals = new();
    private readonly AStarGrid2D _astar = new();
    private int _revision = -1;

    // =========================================================
    // Initialize after Grid has cached the authored level and object footprints.
    public override void _Ready()
    {
        Grid = GetNode<WorldGrid>("../../Grid");
        _astar.Region = new Rect2I(0, 0, Grid.Config.WidthCells, Grid.Config.HeightCells);
        _astar.DiagonalMode = AStarGrid2D.DiagonalModeEnum.OnlyIfNoObstacles;
        _astar.DefaultComputeHeuristic = AStarGrid2D.Heuristic.Octile;
        _astar.DefaultEstimateHeuristic = AStarGrid2D.Heuristic.Octile;
        _astar.Update();
        Refresh();
    }

    // =========================================================
    // Synchronize only on an occupancy/terrain revision; update() would reset solid flags.
    public void Refresh()
    {
        if (_revision == Revision) return;
        for (int y = 0; y < Grid.Config.HeightCells; y++)
            for (int x = 0; x < Grid.Config.WidthCells; x++)
                _astar.SetPointSolid(new Vector2I(x, y), Grid.BlocksEnemies(new Vector2I(x, y)));
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
    public Vector2[] FindPath(Vector2 position, Vector2I goal)
    {
        Refresh();
        Vector2I start = Grid.WorldToCell(position);
        if (Grid.BlocksEnemies(start) || Grid.BlocksEnemies(goal)) return System.Array.Empty<Vector2>();
        Godot.Collections.Array<Vector2I> cells = _astar.GetIdPath(start, goal);
        Vector2[] points = new Vector2[cells.Count];
        for (int i = 0; i < cells.Count; i++) points[i] = Grid.CellToWorldCenter(cells[i]);
        return points;
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
    public override void _ExitTree() => _astar.Dispose();
}
