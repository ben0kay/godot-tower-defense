// Screen-space development overlay for the first interactive map test.
// Throttled to six updates a second to avoid constant string allocations.
using Godot;

public partial class GridDebugOverlay : Label
{
    #region State
    private WorldGrid _grid;
    private CameraController _camera;
    private float _elapsed;
    private SpawnManager _spawns;
    #endregion

    #region Lifecycle
    // =========================================================
    // Cache the world and camera once; the overlay never captures mouse input.
    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        Callable.From(ConnectServices).CallDeferred();
    }

    // =========================================================
    // The level Debug parent also works safely when the map is viewed alone with F6.
    private void ConnectServices()
    {
        _grid = GetNodeOrNull<WorldGrid>("../../../../Grid");
        _camera = GetNodeOrNull<CameraController>("../../../../../CameraRig");
        _spawns = GetNodeOrNull<SpawnManager>("../../../../EnemySystems/Spawning");
        SetProcess(_grid != null && _camera != null && _spawns != null);
        if (_grid != null && _camera != null && _spawns != null) UpdateDisplay();
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
            $"{rules}     Navigation revision: {_grid.NavigationRevision}\n" +
            $"F2: Spawn Enemy001 | Alive: {_spawns.Registry.Living.Count} | Killed: {_spawns.Registry.KilledCount} | Arrived: {_spawns.Registry.ArrivedCount} | Paths queued: {_spawns.Paths.PendingCount}\n" +
            _spawns.LastMessage;
    }
    #endregion
}
