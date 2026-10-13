// Screen-space development overlay for the first interactive map test.
// Throttled to six updates a second to avoid constant string allocations.
using Godot;

public partial class GridDebugOverlay : Label
{
    #region State
    private WorldGrid _grid;
    private CameraController _camera;
    private float _elapsed;
    #endregion

    #region Lifecycle
    // =========================================================
    // Cache the world and camera once; the overlay never captures mouse input.
    public override void _Ready()
    {
        _grid = GetNode<WorldGrid>("../../World/Grid");
        _camera = GetNode<CameraController>("../../CameraRig");
        MouseFilter = MouseFilterEnum.Ignore;
        UpdateDisplay();
    }

    // =========================================================
    // Update the small HUD periodically, not every rendered frame.
    public override void _Process(double delta)
    {
        _elapsed += (float)delta;
        if (_elapsed < 0.15f) return;
        _elapsed = 0f;
        UpdateDisplay();
    }

    // =========================================================
    // Show map size, hovered build cell, zoom, camera position and frames per second.
    private void UpdateDisplay()
    {
        Vector2I cell = _grid.WorldToCell(_grid.GetGlobalMousePosition());
        string rules = _grid.IsInsideMap(cell)
            ? $"Build: {_grid.CanBuild(cell)} | Blocked: {_grid.BlocksEnemies(cell)} | Occupied: {_grid.IsOccupied(cell)}" : "";
        string hover = _grid.IsInsideMap(cell) ? $"{cell.X}, {cell.Y}" : "outside";
        Text = $"WASD / Arrows: Pan     Middle Mouse: Drag     Wheel: Zoom\n" +
            $"Grid: {_grid.Config.CellSize}px     Map: {_grid.Config.WidthCells} x {_grid.Config.HeightCells} cells\n" +
            $"Cell: {hover}     Zoom: {_camera.ZoomLevel:0.00}x     " +
            $"Camera: {_camera.GlobalPosition.X:0}, {_camera.GlobalPosition.Y:0}     FPS: {Engine.GetFramesPerSecond()}\n" +
            $"{rules}     Navigation revision: {_grid.NavigationRevision} (terrain test; no enemy AI yet)";
    }
    #endregion
}
