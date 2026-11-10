// Level-owned spawning and route-preview services; remove/disable Debug to remove these tools.
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
        }
    }
    [ExportGroup("Route Preview")]
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
        if (!OS.IsDebugBuild() || !IsInsideTree()) return;
        _spawns = GetNodeOrNull<SpawnManager>("../../EnemySystems/Spawning");
        if (Enabled && _spawns != null)
        {
            SpawnMenu = new EnemySpawnMenu { Name = "EnemySpawnMenu" }; AddChild(SpawnMenu); SpawnMenu.Initialize(_spawns);
            RoutePreview = new RoutePreviewOverlay { Name = "RoutePreview",
                MaxCalculationsPerFrame = PreviewCalculationsPerFrame, MaxMillisecondsPerFrame = PreviewMillisecondsPerFrame };
            AddChild(RoutePreview); RoutePreview.Initialize(_spawns);
        }
    }

    #endregion
}
