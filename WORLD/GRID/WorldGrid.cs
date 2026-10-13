// Single-canvas grid renderer: draw only visible grid lines instead of per-tile nodes.
// Expose map bounds and grid-to-world conversion for future placement/pathing systems.
using Godot;

public partial class WorldGrid : Node2D
{
    #region Settings and State
    [Export] public WorldMasterConfig Config { get; set; }
    public Rect2 WorldBounds => new(Vector2.Zero,
        new Vector2(Config.WidthCells * Config.CellSize, Config.HeightCells * Config.CellSize));
    private Vector2 _lastCamera = new(float.NaN, float.NaN);
    private Vector2 _lastZoom = new(float.NaN, float.NaN);
    private Vector2I _lastHover = new(int.MinValue, int.MinValue);
    #endregion

    #region Grid Coordinates
    // =========================================================
    // Cell coordinates start at (0,0) in the upper left and increase to the right/down.
    public Vector2I WorldToCell(Vector2 worldPosition)
    {
        Vector2 local = ToLocal(worldPosition);
        return new Vector2I(Mathf.FloorToInt(local.X / Config.CellSize),
            Mathf.FloorToInt(local.Y / Config.CellSize));
    }

    // =========================================================
    // Shared bounds check for placement previews and cursor diagnostics.
    public bool IsInsideMap(Vector2I cell) =>
        cell.X >= 0 && cell.Y >= 0 && cell.X < Config.WidthCells && cell.Y < Config.HeightCells;
    #endregion

    #region Redraw
    // =========================================================
    // Queue new drawing only if the visible area or hovered square has changed.
    public override void _Process(double delta)
    {
        Camera2D camera = GetViewport().GetCamera2D();
        if (camera == null) return;
        Vector2I hover = WorldToCell(GetGlobalMousePosition());
        if (_lastCamera == camera.GlobalPosition && _lastZoom == camera.Zoom
            && (!Config.ShowHoverCell || _lastHover == hover)) return;

        _lastCamera = camera.GlobalPosition;
        _lastZoom = camera.Zoom;
        _lastHover = hover;
        QueueRedraw();
    }

    // =========================================================
    // Use the inverse viewport transform to limit line drawing to visible cells.
    public override void _Draw()
    {
        Rect2 map = WorldBounds;
        DrawRect(map, Config.MapColor);

        Transform2D localFromViewport = GetGlobalTransformWithCanvas().AffineInverse();
        Vector2 a = localFromViewport * Vector2.Zero;
        Vector2 b = localFromViewport * GetViewportRect().Size;
        float left = Mathf.Max(0f, Mathf.Min(a.X, b.X));
        float top = Mathf.Max(0f, Mathf.Min(a.Y, b.Y));
        float right = Mathf.Min(map.End.X, Mathf.Max(a.X, b.X));
        float bottom = Mathf.Min(map.End.Y, Mathf.Max(a.Y, b.Y));
        if (left > right || top > bottom) return;

        float zoom = GetViewport().GetCamera2D()?.Zoom.X ?? 1f;
        float lineWidth = 1f / zoom;
        int size = Config.CellSize;
        if (Config.ShowGrid)
        {
            int x0 = Mathf.Max(0, Mathf.FloorToInt(left / size));
            int x1 = Mathf.Min(Config.WidthCells, Mathf.CeilToInt(right / size));
            int y0 = Mathf.Max(0, Mathf.FloorToInt(top / size));
            int y1 = Mathf.Min(Config.HeightCells, Mathf.CeilToInt(bottom / size));
            for (int x = x0; x <= x1; x++)
                DrawLine(new Vector2(x * size, top), new Vector2(x * size, bottom), Config.GridColor, lineWidth);
            for (int y = y0; y <= y1; y++)
                DrawLine(new Vector2(left, y * size), new Vector2(right, y * size), Config.GridColor, lineWidth);
        }

        if (Config.ShowHoverCell && IsInsideMap(_lastHover))
            DrawRect(new Rect2(_lastHover.X * size, _lastHover.Y * size, size, size), Config.HoverColor);

        DrawRect(map, Config.BorderColor, filled: false, width: 2f / zoom);
    }
    #endregion
}
