// Level-owned F2 spawning and F3 route preview; remove/disable Debug to remove these tools.
using Godot;

public partial class LevelDebugController : Node
{
    [ExportGroup("Debug Spawning")]
    private bool _enabled = true;
    [Export] public bool Enabled
    {
        get => _enabled;
        set
        {
            _enabled = value;
            if (!value && RoutePreview?.IsOpen == true) RoutePreview.Toggle();
            if (!IsInsideTree()) return;
            CanvasLayer hud = GetNodeOrNull<CanvasLayer>("HUD");
            if (hud != null) hud.Visible = value && _spawns != null;
            SetProcessUnhandledKeyInput(value && _spawns != null);
        }
    }
    [Export] public Key SpawnKey { get; set; } = Key.F2;
    [Export] public PackedScene EnemyScene { get; set; }
    [Export] public bool PortalOnly { get; set; } = true;
    [ExportGroup("Route Preview")]
    [Export] public Key PreviewKey { get; set; } = Key.F3;
    [Export(PropertyHint.Range, "1,8,1")] public int PreviewCalculationsPerFrame { get; set; } = 1;
    [Export(PropertyHint.Range, "0,5,0.1")] public float PreviewMillisecondsPerFrame { get; set; } = 1;
    public RoutePreviewOverlay RoutePreview { get; private set; }
    private SpawnManager _spawns;
    private int _nextEnemyIndex;

    // =========================================================
    // Deferred lookup allows the live world's grid/services to finish their startup first.
    public override void _Ready() => Callable.From(ConnectServices).CallDeferred();

    // =========================================================
    // F6 map-only viewing is safe: it has no gameplay services and does not spawn ships.
    private void ConnectServices()
    {
        _spawns = GetNodeOrNull<SpawnManager>("../../EnemySystems/Spawning");
        GetNode<CanvasLayer>("HUD").Visible = Enabled && _spawns != null;
        SetProcessUnhandledKeyInput(Enabled && _spawns != null);
        if (Enabled && _spawns != null)
        {
            RoutePreview = new RoutePreviewOverlay { Name = "RoutePreview",
                MaxCalculationsPerFrame = PreviewCalculationsPerFrame, MaxMillisecondsPerFrame = PreviewMillisecondsPerFrame };
            AddChild(RoutePreview); RoutePreview.Initialize(_spawns);
        }
    }

    // =========================================================
    // F2 cycles catalog entries in Inspector order; only a successful spawn advances the index.
    public override void _UnhandledKeyInput(InputEvent input)
    {
        if (!Enabled || _spawns == null || input is not InputEventKey key || !key.Pressed || key.Echo) return;
        if (key.Keycode == PreviewKey)
        { RoutePreview?.Toggle(); GetViewport().SetInputAsHandled(); return; }
        if (key.Keycode != SpawnKey) return;

        var scenes = _spawns.Catalog?.Scenes;
        if (scenes != null && scenes.Count > 0)
        {
            int index = _nextEnemyIndex % scenes.Count;
            if (_spawns.Spawn(scenes[index], PortalOnly) != null)
                _nextEnemyIndex = (index + 1) % scenes.Count;
        }
        else if (EnemyScene != null) _spawns.Spawn(EnemyScene, PortalOnly);

        GetViewport().SetInputAsHandled();
    }
}
