// Moves a top-down Camera2D around a large 2D map without any player actor.
// Keyboard smoothing, immediate middle-mouse dragging, cursor zoom and zoom-aware bounds.
using Godot;

public partial class CameraController : Node2D
{
    #region Settings
    [Export] public CameraMasterConfig Config { get; set; }
    #endregion

    #region Runtime
    private Camera2D _camera;
    private WorldGrid _grid;
    private Vector2 _targetPosition;
    private bool _dragging;
    public float ZoomLevel => _camera.Zoom.X;
    #endregion

    #region Lifecycle
    // =========================================================
    // Find the world map once and center the view at launch.
    public override void _Ready()
    {
        _camera = GetNode<Camera2D>("Camera2D");
        _grid = GetNode<WorldGrid>("../World/Grid");
        _camera.Zoom = Vector2.One * Mathf.Clamp(Config.StartingZoom, Config.MinimumZoom, Config.MaximumZoom);
        _camera.MakeCurrent();
        GlobalPosition = ClampToMap(_grid.WorldBounds.GetCenter());
        _targetPosition = GlobalPosition;
    }

    // =========================================================
    // Move at comparable perceived speed across zoom levels; idle when no input.
    public override void _Process(double delta)
    {
        float x = (Input.IsKeyPressed(Key.D) || Input.IsKeyPressed(Key.Right) ? 1f : 0f)
            - (Input.IsKeyPressed(Key.A) || Input.IsKeyPressed(Key.Left) ? 1f : 0f);
        float y = (Input.IsKeyPressed(Key.S) || Input.IsKeyPressed(Key.Down) ? 1f : 0f)
            - (Input.IsKeyPressed(Key.W) || Input.IsKeyPressed(Key.Up) ? 1f : 0f);
        Vector2 motion = new Vector2(x, y).Normalized();
        if (motion != Vector2.Zero)
            _targetPosition += motion * (Config.PanSpeed / _camera.Zoom.X) * (float)delta;

        _targetPosition = ClampToMap(_targetPosition);
        float weight = Config.PanSmoothing <= 0f ? 1f : 1f - Mathf.Exp(-Config.PanSmoothing * (float)delta);
        GlobalPosition = ClampToMap(GlobalPosition.Lerp(_targetPosition, weight));
    }
    #endregion

    // =========================================================
    // A middle-button release may occur while paused; never resume with a stale drag latch.
    public override void _Notification(int what)
    {
        if (what == NotificationPaused) _dragging = false;
    }

    #region Mouse Controls
    // =========================================================
    // Consume camera navigation events without interfering with future gameplay clicks.
    public override void _UnhandledInput(InputEvent input)
    {
        if (input is InputEventMouseButton button)
        {
            if (button.ButtonIndex == MouseButton.Middle)
            {
                _dragging = button.Pressed;
                GetViewport().SetInputAsHandled();
            }
            else if (button.Pressed && (button.ButtonIndex == MouseButton.WheelUp
                || button.ButtonIndex == MouseButton.WheelDown))
            {
                ZoomAtMouse(button.ButtonIndex == MouseButton.WheelUp);
                GetViewport().SetInputAsHandled();
            }
        }
        else if (input is InputEventMouseMotion mouse && _dragging)
        {
            GlobalPosition = ClampToMap(GlobalPosition - mouse.Relative / _camera.Zoom);
            _targetPosition = GlobalPosition;
            _camera.ForceUpdateScroll();
            GetViewport().SetInputAsHandled();
        }
    }

    // =========================================================
    // Keep the world location under the cursor in place when changing magnification.
    private void ZoomAtMouse(bool zoomIn)
    {
        float step = zoomIn ? Config.ZoomStep : 1f / Config.ZoomStep;
        float newZoom = Mathf.Clamp(_camera.Zoom.X * step, Config.MinimumZoom, Config.MaximumZoom);
        if (Mathf.IsEqualApprox(newZoom, _camera.Zoom.X)) return;

        Vector2 mouseBefore = GetGlobalMousePosition();
        _camera.Zoom = Vector2.One * newZoom;
        _camera.ForceUpdateScroll();
        Vector2 mouseAfter = GetGlobalMousePosition();
        GlobalPosition = ClampToMap(GlobalPosition + mouseBefore - mouseAfter);
        _targetPosition = GlobalPosition;
        _camera.ForceUpdateScroll();
    }
    #endregion

    #region Bounds
    // =========================================================
    // Constrain the full visible rectangle, not merely the center of the camera.
    private Vector2 ClampToMap(Vector2 proposed)
    {
        if (!Config.KeepInsideMap) return proposed;
        Rect2 map = _grid.WorldBounds;
        Vector2 half = GetViewportRect().Size * 0.5f / _camera.Zoom;
        float left = map.Position.X + half.X, right = map.End.X - half.X;
        float top = map.Position.Y + half.Y, bottom = map.End.Y - half.Y;
        Vector2 center = map.GetCenter();
        return new Vector2(left > right ? center.X : Mathf.Clamp(proposed.X, left, right),
            top > bottom ? center.Y : Mathf.Clamp(proposed.Y, top, bottom));
    }
    #endregion

    // =========================================================
    // Checkpoint restore sets source state directly; callers validate before publishing the battlefield.
    internal void RestoreCamera(SavePoint position, float zoom)
    {
        if (zoom < Config.MinimumZoom || zoom > Config.MaximumZoom) throw new System.IO.InvalidDataException("Saved camera zoom is incompatible.");
        _camera.Zoom = Vector2.One * zoom; GlobalPosition = ClampToMap(position.Vector());
        _targetPosition = GlobalPosition; _dragging = false; _camera.ResetSmoothing();
    }
}
