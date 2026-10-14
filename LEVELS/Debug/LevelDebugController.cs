// Level-owned debug input only; remove/disable Debug to remove F2 and its HUD.
using Godot;

public partial class LevelDebugController : Node
{
    [ExportGroup("Debug Spawning")]
    [Export] public bool Enabled { get; set; } = true;
    [Export] public Key SpawnKey { get; set; } = Key.F2;
    [Export] public PackedScene EnemyScene { get; set; }
    [Export] public bool PortalOnly { get; set; } = true;
    private SpawnManager _spawns;

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
    }

    // =========================================================
    // One spawn per real key press; rapid repeated presses are intentionally supported.
    public override void _UnhandledKeyInput(InputEvent input)
    {
        if (!Enabled || _spawns == null || input is not InputEventKey key || !key.Pressed || key.Echo
            || key.Keycode != SpawnKey) return;
        _spawns.Spawn(EnemyScene, PortalOnly); GetViewport().SetInputAsHandled();
    }
}
