// Directional scanner: continuous head rotation, capped indexed detection and event-driven inactive shutdown.
using Godot;

public partial class StealthBeacon : TowerBase
{
    #region Configuration
    [Export] public StealthBeaconConfig Config { get; set; }
    public override TowerConfig AuthoredConfig => Config;
    public override System.Collections.Generic.List<TowerLevel> Levels => Config.Levels;
    public override float? InspectionRange => FinalStats?.RangePixels;
    public Vector2 ScanHeading => GodotObject.IsInstanceValid(_pivot) ? -_pivot.GlobalTransform.Y.Normalized() : Vector2.Zero;
    public override bool AcceptsConfig(TowerConfig config) => config is StealthBeaconConfig beacon && beacon.IsValid;
    protected override void ApplyConfig(TowerConfig config) => Config = (StealthBeaconConfig)config;
    public override TowerFinalStats ResolveLevel(int level) => ResolveRankedLevel(level);
    public override bool ValidateScene() => Config?.IsValid == true && GetNodeOrNull<Sprite2D>("Sprite") != null
        && GetNodeOrNull<Node2D>("TurretPivot") != null && GetNodeOrNull<Node2D>("TurretPivot/ScanCone") != null;
    public override string DescribeRuntime() => _registered ? "Directional stealth scan active" : "Stealth scan inactive";
    #endregion

    #region Lifecycle
    private StealthRevealSystem _system;
    private Node2D _pivot, _cone;
    private Polygon2D _sector;
    private bool _ready, _registered, _synchronizing;
    private double _scanElapsed;
    // =========================================================
    // Keep all prepared scene paths/art. The placeholder owns one polygon, created once, not every scan.
    public override void _Ready()
    {
        FinalStats = ResolveLevel(UpgradeLevel); SetProcess(false); SetOperationalPhysicsProcess(false);
        _pivot = GetNode<Node2D>("TurretPivot"); _cone = GetNode<Node2D>("TurretPivot/ScanCone");
        _sector = _cone.GetNodeOrNull<Polygon2D>("Sector");
        if (_sector == null) { _sector = new() { Name = "Sector" }; _cone.AddChild(_sector); }
        _cone.Visible = false;
        BuildCone(); ProcessPhysicsPriority = -50;
        var registry = GetNodeOrNull<EnemyRegistry>("../../EnemySystems/Registry");
        if (registry != null) { _system = StealthRevealSystem.For(registry); _system.TreeExiting += SystemExiting; }
        WorkStarted += Synchronize; WorkCompleted += Synchronize; DisableStateChanged += Synchronize;
        Destroyed += Synchronize; StatsChanged += RefreshStats; Config.Changed += Rebuild;
        SetNotifyLocalTransform(true); SetNotifyTransform(true); _ready = true; Synchronize();
    }
    // =========================================================
    // Only config/stat changes rebuild polygon geometry; heading changes inherit the parent's rotation.
    private void Rebuild()
    { if (Config.IsValid) { FinalStats = ResolveLevel(UpgradeLevel); NotifyStatsChanged(); } else Synchronize(); }
    private void RefreshStats() { BuildCone(); Synchronize(); }
    private void BuildCone()
    {
        if (!Config.IsValid || _sector == null) return;
        const int segments = 24;
        Vector2[] points = new Vector2[segments + 2]; points[0] = Vector2.Zero;
        float half = Mathf.DegToRad(FinalStats.ScanConeDegrees * .5f);
        for (int i = 0; i <= segments; i++)
            points[i + 1] = Vector2.Up.Rotated(-half + 2 * half * i / segments) * FinalStats.RangePixels;
        _sector.Polygon = points; _sector.Color = new Color(.35f, .95f, .8f, Config.ScanConeOpacity);
    }
    // =========================================================
    // Disabled, unfinished, upgrading or destroyed sources release handles immediately and have no scan tick.
    private void Synchronize()
    {
        if (!_ready || _synchronizing) return;
        bool active = IsInsideTree() && !IsQueuedForDeletion() && CanOperate && !IsBusy && Config.IsValid
            && GlobalPosition.IsFinite() && ScanHeading.IsFinite() && ScanHeading.LengthSquared() > .99f
            && GodotObject.IsInstanceValid(_system) && _system.IsInsideTree() && !_system.IsQueuedForDeletion();
        bool changed = active != _registered;
        _synchronizing = true;
        try
        {
            if (active) _system.Register(this);
            else if (GodotObject.IsInstanceValid(_system)) _system.Unregister(this);
            _registered = active; _cone.Visible = active; SetOperationalPhysicsProcess(active);
            if (changed) _scanElapsed = (GetInstanceId() % 97) / 97.0 * Config.ScanUpdateIntervalSeconds;
        }
        finally { _synchronizing = false; }
        if (changed) NotifyRuntimeChanged();
    }
    // =========================================================
    // One bounded sample per interval, never catch-up loops; explicit pause guard also covers test stepping.
    public override void _PhysicsProcess(double delta)
    {
        if (!_registered || !CanOperate || IsBusy || IsQueuedForDeletion() || !IsInsideTree() || GetTree().Paused
            || !double.IsFinite(delta) || delta <= 0) return;
        _pivot.Rotation = Mathf.PosMod(_pivot.Rotation + (float)(delta % Config.ScanRotationSeconds) * Mathf.Tau / Config.ScanRotationSeconds, Mathf.Tau);
        _scanElapsed += delta;
        if (_scanElapsed < Config.ScanUpdateIntervalSeconds) return;
        _scanElapsed %= Config.ScanUpdateIntervalSeconds;
        _system.UpdateBeaconDirection(this, ScanHeading);
    }
    // =========================================================
    // Root teleport/rotation refreshes coverage; child head rotation does not reindex or re-register the source.
    public override void _Notification(int what)
    {
        base._Notification(what);
        if ((what == NotificationTransformChanged || what == NotificationLocalTransformChanged) && _ready) Synchronize();
    }
    // =========================================================
    // The registry subtree may leave before Towers; no scanner may retain or tick a departing index.
    private void SystemExiting()
    {
        if (GodotObject.IsInstanceValid(_system)) _system.TreeExiting -= SystemExiting;
        _system = null; _registered = false; SetOperationalPhysicsProcess(false);
        if (GodotObject.IsInstanceValid(_cone)) _cone.Visible = false;
        NotifyRuntimeChanged();
    }
    // =========================================================
    // Removing the tower releases its reveal before the next world starts. The scene owns the polygon lifetime.
    public override void _ExitTree()
    {
        _ready = false; _registered = false; SetOperationalPhysicsProcess(false);
        if (GodotObject.IsInstanceValid(_cone)) _cone.Visible = false;
        if (GodotObject.IsInstanceValid(_system)) { _system.TreeExiting -= SystemExiting; _system.Unregister(this); }
        WorkStarted -= Synchronize; WorkCompleted -= Synchronize; DisableStateChanged -= Synchronize;
        Destroyed -= Synchronize; StatsChanged -= RefreshStats;
        if (GodotObject.IsInstanceValid(Config)) Config.Changed -= Rebuild;
        _system = null; _pivot = _cone = null; _sector = null; base._ExitTree();
    }
    #endregion
}
