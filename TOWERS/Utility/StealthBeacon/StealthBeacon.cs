// Passive reveal source; membership changes on enemy movement and tower lifecycle, never a polling timer.
using Godot;

public partial class StealthBeacon : TowerBase
{
    #region Configuration
    [Export] public StealthBeaconConfig Config { get; set; }
    public override TowerConfig AuthoredConfig => Config;
    public override System.Collections.Generic.List<TowerLevel> Levels => Config.Levels;
    public override float? InspectionRange => FinalStats?.RangePixels;
    public override bool AcceptsConfig(TowerConfig config) => config is StealthBeaconConfig beacon && beacon.IsValid;
    protected override void ApplyConfig(TowerConfig config) => Config = (StealthBeaconConfig)config;
    public override TowerFinalStats ResolveLevel(int level) => ResolveRankedLevel(level);
    public override bool ValidateScene() => Config?.IsValid == true && GetNodeOrNull<Sprite2D>("Sprite") != null;
    public override string DescribeRuntime() => _registered ? "Revealing enemies within range" : "Reveal beacon inactive";
    #endregion

    #region Lifecycle
    private StealthRevealSystem _system;
    private bool _ready, _registered, _synchronizing;
    // =========================================================
    // Bind once to ordinary construction, disable, durability and stat events.
    public override void _Ready()
    {
        FinalStats = ResolveLevel(UpgradeLevel); SetProcess(false); SetOperationalPhysicsProcess(false);
        var registry = GetNodeOrNull<EnemyRegistry>("../../EnemySystems/Registry");
        if (registry != null) _system = StealthRevealSystem.For(registry);
        WorkStarted += Synchronize; WorkCompleted += Synchronize; DisableStateChanged += Synchronize;
        Destroyed += Synchronize; StatsChanged += Synchronize; Config.Changed += Rebuild;
        SetNotifyLocalTransform(true); SetNotifyTransform(true); _ready = true; Synchronize();
    }
    // =========================================================
    // Tuning rebuilds one immutable snapshot and refreshes only this source's coverage.
    private void Rebuild()
    { if (Config.IsValid) { FinalStats = ResolveLevel(UpgradeLevel); NotifyStatsChanged(); } else Synchronize(); }
    // =========================================================
    // Disabled, unfinished, upgrading or destroyed beacons immediately release every affected enemy.
    private void Synchronize()
    {
        if (!_ready || _synchronizing || !GodotObject.IsInstanceValid(_system)) return;
        bool active = IsInsideTree() && !IsQueuedForDeletion() && CanOperate && !IsBusy && Config.IsValid;
        _synchronizing = true;
        try { if (active) _system.Register(this); else _system.Unregister(this); _registered = active; }
        finally { _synchronizing = false; }
        NotifyRuntimeChanged();
    }
    // =========================================================
    // Placement/explicit teleport changes refresh a source without a per-frame transform check.
    public override void _Notification(int what)
    { if ((what == NotificationTransformChanged || what == NotificationLocalTransformChanged) && _ready) Synchronize(); }
    // =========================================================
    // Removing a tower clears its reveal source before a different world can be loaded.
    public override void _ExitTree()
    {
        _ready = false; _registered = false;
        if (GodotObject.IsInstanceValid(_system)) _system.Unregister(this);
        WorkStarted -= Synchronize; WorkCompleted -= Synchronize; DisableStateChanged -= Synchronize;
        Destroyed -= Synchronize; StatsChanged -= Synchronize;
        if (GodotObject.IsInstanceValid(Config)) Config.Changed -= Rebuild;
        _system = null; base._ExitTree();
    }
    #endregion
}
