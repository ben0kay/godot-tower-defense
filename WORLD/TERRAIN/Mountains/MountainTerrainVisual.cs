// One cached faceted mountain mesh from painted metadata; no per-cell nodes or frame processing.
using Godot;
using System.Collections.Generic;

public partial class MountainTerrainVisual : Node2D
{
    [Export] public MountainVisualConfig Config { get; set; }
    private WorldGrid _grid;
    private ArrayMesh _mesh, _shadow;
    private readonly HashSet<Vector2I> _cells = new();
    private readonly List<Vector2I> _ordered = new();
    private static readonly Vector2I[] Ring =
    {
        new(0, 0), new(1, 0), new(2, 0), new(2, 1),
        new(2, 2), new(1, 2), new(0, 2), new(0, 1)
    };

    // =========================================================
    // World installs its map and initializes Grid before this sibling enters Ready.
    public override void _Ready()
    {
        SetProcess(false); SetPhysicsProcess(false);
        _grid = GetNode<WorldGrid>("../Grid");
        _grid.TerrainRebuilt += Rebuild;
        Rebuild();
    }

    // =========================================================
    // Only attachment/explicit terrain rebuilds regenerate geometry; occupancy has no subscription.
    public void Rebuild()
    {
        ClearMeshes(); _cells.Clear(); _ordered.Clear();
        if (Config?.Enabled != true || !GodotObject.IsInstanceValid(_grid)) { QueueRedraw(); return; }
        TileMapLayer layer = _grid.Level.Impassable;
        Transform2D relative = _grid.GlobalTransform.AffineInverse() * layer.GlobalTransform;
        Vector2 offset = (relative * layer.MapToLocal(Vector2I.Zero)
            - Vector2.One * (_grid.Config.CellSize * 0.5f)) / _grid.Config.CellSize;
        if (!relative.X.IsEqualApprox(Vector2.Right) || !relative.Y.IsEqualApprox(Vector2.Down)
            || !offset.IsEqualApprox(offset.Round()))
        {
            GD.PushWarning("Mountain visuals require a grid-aligned impassable layer; retaining marker artwork.");
            QueueRedraw(); return;
        }
        if (layer.TileSet.GetCustomDataLayerByName("mountain_visual") < 0) { QueueRedraw(); return; }
        foreach (Vector2I authored in layer.GetUsedCells())
        {
            TileData data = layer.GetCellTileData(authored);
            if (data == null || !data.GetCustomData("mountain_visual").AsBool()) continue;
            Vector2I cell = _grid.WorldToCell(layer.ToGlobal(layer.MapToLocal(authored)));
            if (_grid.IsInsideMap(cell)) _cells.Add(cell);
        }
        _ordered.AddRange(_cells);
        // Back to front within the static mesh. Stable order also keeps reloads deterministic.
        _ordered.Sort((a, b) => a.Y != b.Y ? a.Y.CompareTo(b.Y) : a.X.CompareTo(b.X));
        if (_ordered.Count == 0) { QueueRedraw(); return; }
        var vertices = new List<Vector3>(_ordered.Count * 24);
        var shadows = new List<Vector3>(_ordered.Count * 24);
        var colors = new List<Color>(_ordered.Count * 24);
        foreach (Vector2I cell in _ordered)
        {
            Vector2I key = cell * 2 + Vector2I.One;
            Vector3 center = Sample(key);
            for (int i = 0; i < Ring.Length; i++)
            {
                Vector3 a = Sample(cell * 2 + Ring[i]);
                Vector3 b = Sample(cell * 2 + Ring[(i + 1) % Ring.Length]);
                Vector3 normal = (b - center).Cross(a - center).Normalized();
                if (normal.Z < 0) normal = -normal;
                float light = Mathf.Clamp(0.68f + normal.Dot(new Vector3(-0.5f, -0.65f, 1).Normalized()) * 0.38f, 0.36f, 1.12f);
                Color color = Config.RockColor;
                color = new Color(color.R * light, color.G * light, color.B * light, 1);
                Add(center); Add(a); Add(b);
                void Add(Vector3 point)
                {
                    Vector2 basePoint = new(point.X, point.Y);
                    Vector2 raised = ToLocal(_grid.ToGlobal(basePoint + new Vector2(0, -point.Z)));
                    Vector2 shade = ToLocal(_grid.ToGlobal(basePoint + new Vector2(point.Z * 0.45f, point.Z * 0.25f)));
                    vertices.Add(new(raised.X, raised.Y, 0));
                    shadows.Add(new(shade.X, shade.Y, 0)); colors.Add(color);
                }
            }
        }
        _mesh = BuildMesh(vertices, colors);
        _shadow = BuildMesh(shadows, null);
        QueueRedraw();
    }

    // =========================================================
    // Shared half-cell lattice makes adjacent painted cells share ridges with no cracks.
    // Boundary vertices stay at ground height; interior vertices and cell peaks rise.
    private Vector3 Sample(Vector2I key)
    {
        int x = Mathf.FloorToInt(key.X / 2f), y = Mathf.FloorToInt(key.Y / 2f);
        bool oddX = (key.X & 1) != 0, oddY = (key.Y & 1) != 0;
        bool interior = true;
        for (int dy = oddY ? 0 : -1; dy <= 0; dy++)
            for (int dx = oddX ? 0 : -1; dx <= 0; dx++)
                interior &= _cells.Contains(new Vector2I(x + dx, y + dy));
        float size = _grid.Config.CellSize;
        float noise = Noise(key, 0);
        float height = interior ? size * Mathf.Clamp(Config.HeightCells, 0.2f, 2f)
            * (oddX && oddY ? 0.65f + noise * 0.35f : 0.3f + noise * 0.35f) : 0;
        float variation = Mathf.Clamp(Config.EdgeVariationCells, 0, 0.15f) * size;
        return new(key.X * size * 0.5f + (Noise(key, 1) - 0.5f) * variation,
            key.Y * size * 0.5f + (Noise(key, 2) - 0.5f) * variation, height);
    }

    // =========================================================
    // Explicit integer hash avoids randomized process-dependent hash seeds and global RNG state.
    private float Noise(Vector2I point, int channel)
    {
        unchecked
        {
            uint value = (uint)(point.X * 73856093 ^ point.Y * 19349663 ^ Config.Seed ^ channel * 83492791);
            value ^= value >> 16; value *= 0x7feb352d; value ^= value >> 15;
            return (value & 65535) / 65535f;
        }
    }

    // =========================================================
    // Two surfaces per world, independent of mountain count; native meshes retain draw data.
    private static ArrayMesh BuildMesh(List<Vector3> vertices, List<Color> colors)
    {
        var arrays = new Godot.Collections.Array(); arrays.Resize((int)Mesh.ArrayType.Max);
        arrays[(int)Mesh.ArrayType.Vertex] = vertices.ToArray();
        if (colors != null) arrays[(int)Mesh.ArrayType.Color] = colors.ToArray();
        var mesh = new ArrayMesh(); mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);
        return mesh;
    }

    // =========================================================
    // Godot records these two commands until explicit invalidation; no animation or redraw loop.
    public override void _Draw()
    {
        if (_shadow != null) DrawMesh(_shadow, null, Transform2D.Identity, Config.ShadowColor);
        if (_mesh != null) DrawMesh(_mesh, null, Transform2D.Identity, Colors.White);
    }

    // =========================================================
    // Release old native resources on rebuild/unload and disconnect the world event.
    private void ClearMeshes()
    {
        _mesh?.Dispose(); _shadow?.Dispose(); _mesh = null; _shadow = null;
    }
    public override void _ExitTree()
    {
        if (GodotObject.IsInstanceValid(_grid)) _grid.TerrainRebuilt -= Rebuild;
        ClearMeshes(); _grid = null;
    }
}
