// Level-owned F2 spawning and F3 route preview; remove/disable Debug to remove these tools.
using Godot;

public partial class LevelDebugController : Node
{
    #region Configuration
    [ExportGroup("Debug Spawning")]
    private bool _enabled = true;
    [Export] public bool Enabled
    {
        get => _enabled;
        set
        {
            _enabled = value;
            if (!value && RoutePreview?.IsOpen == true) RoutePreview.Toggle();
            if (!value) { SpawnMenu?.Close(); SpawnMenu?.CancelBatch(); }
            if (!IsInsideTree()) return;
            SetProcessUnhandledKeyInput(value && _spawns != null);
        }
    }
    [Export] public Key SpawnKey { get; set; } = Key.F2;
    [ExportGroup("Route Preview")]
    [Export] public Key PreviewKey { get; set; } = Key.F3;
    [Export(PropertyHint.Range, "1,8,1")] public int PreviewCalculationsPerFrame { get; set; } = 1;
    [Export(PropertyHint.Range, "0,5,0.1")] public float PreviewMillisecondsPerFrame { get; set; } = 1;
    public RoutePreviewOverlay RoutePreview { get; private set; }
    private SpawnManager _spawns;
    public EnemySpawnMenu SpawnMenu { get; private set; }
    #endregion

    #region Lifecycle and Shortcuts

    // =========================================================
    // Deferred lookup allows the live world's grid/services to finish their startup first.
    public override void _Ready() => Callable.From(ConnectServices).CallDeferred();

    // =========================================================
    // F6 map-only viewing is safe: it has no gameplay services and does not spawn ships.
    private void ConnectServices()
    {
        _spawns = GetNodeOrNull<SpawnManager>("../../EnemySystems/Spawning");
        SetProcessUnhandledKeyInput(Enabled && _spawns != null);
        if (Enabled && _spawns != null)
        {
            SpawnMenu = new EnemySpawnMenu { Name = "EnemySpawnMenu" }; AddChild(SpawnMenu); SpawnMenu.Initialize(_spawns);
            RoutePreview = new RoutePreviewOverlay { Name = "RoutePreview",
                MaxCalculationsPerFrame = PreviewCalculationsPerFrame, MaxMillisecondsPerFrame = PreviewMillisecondsPerFrame };
            AddChild(RoutePreview); RoutePreview.Initialize(_spawns);
        }
    }

    // =========================================================
    // F2 opens the batch menu; F3 retains its independent preview shortcut. No key press directly spawns.
    public override void _UnhandledKeyInput(InputEvent input)
    {
        if (!Enabled || _spawns == null || input is not InputEventKey key || !key.Pressed || key.Echo) return;
        if (key.Keycode == PreviewKey)
        { SpawnMenu?.Close(); RoutePreview?.Toggle(); GetViewport().SetInputAsHandled(); return; }
        if (key.Keycode != SpawnKey) return;
        if (RoutePreview?.IsOpen == true) RoutePreview.Toggle();
        SpawnMenu?.Toggle(); GetViewport().SetInputAsHandled();
    }
    // =========================================================
    // Escape dismisses this panel before gameplay interprets it as a pause/placement action.
    public override void _Input(InputEvent input)
    {
        if (SpawnMenu?.IsOpen == true && input is InputEventKey { Pressed: true, Echo: false, Keycode: Key.Escape })
        { SpawnMenu.Close(); GetViewport().SetInputAsHandled(); }
    }
    #endregion
}
