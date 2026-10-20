// Optional scene-mounted enemy weapon: owner lifecycle, staggered spatial lock acquisition and independent gun aiming.
using Godot;
using System.Collections.Generic;

public partial class EnemyWeaponMount : Node2D
{
    #region Inspector and Runtime
    private EnemyWeaponConfig _config;
    private bool _ready, _enabled = true, _running;
    private Enemy _host;
    private TowerRegistry _registry;
    private Node2D _pivot;
    private Marker2D _muzzle;
    private EnemyWeaponSettings _settings;
    private double _search;
    private float _heading;
    private readonly List<TowerBase> _scratch = new();
    [Export] public Enemy Host { get; set; }
    [Export] public NodePath PivotPath { get; set; } = new("TurretPivot");
    [Export] public NodePath MuzzlePath { get; set; } = new("TurretPivot/GunSprite/Muzzle");
    [Export] public EnemyWeaponConfig Config
    { get => _config; set { _config = value; if (_ready) RefreshConfiguration(); } }
    [Export] public bool Enabled
    { get => _enabled; set { if (_enabled == value) return; _enabled = value; if (_ready) RefreshConfiguration(); } }
    public TowerBase Target { get; private set; }
    public bool Aligned { get; private set; }
    public bool Running => _running;
    public int TargetQueries { get; private set; }
    public int LastCandidates { get; private set; }
    public double SearchRemaining => _search;
    protected Enemy OwnerEnemy => _host;
    protected Marker2D Muzzle => _muzzle;
    protected EnemyWeaponSettings Settings => _settings;
    #endregion

    #region Lifecycle and Configuration
    // =========================================================
    // Ordinary enemies never instantiate this component; locate the host once, without touching navigation.
    public override void _Ready()
    {
        SetProcess(false); SetPhysicsProcess(false); _host = Host;
        for (Node ancestor = GetParent(); _host == null && ancestor != null; ancestor = ancestor.GetParent())
            if (ancestor is Enemy enemy) _host = enemy;
        _pivot = PivotPath.IsEmpty ? null : GetNodeOrNull<Node2D>(PivotPath);
        _muzzle = MuzzlePath.IsEmpty ? null : GetNodeOrNull<Marker2D>(MuzzlePath);
        _ready = true;
        if (!GodotObject.IsInstanceValid(_host) || !_host.IsAncestorOf(this)) return;
        _host.Initialized += OnInitialized; _host.Removed += OnRemoved;
        if (_host.IsAlive) OnInitialized(_host);
    }
    // =========================================================
    // Reuse/reinitialization resets the private lock, phase and captured settings rather than carrying stale world state.
    private void OnInitialized(Enemy host) => RefreshConfiguration();
    // =========================================================
    // Config assignments explicitly capture authored settings; subsequent source edits need an explicit refresh.
    public void RefreshConfiguration()
    {
        Stop();
        if (!_ready || !Enabled || !GodotObject.IsInstanceValid(_host) || !_host.IsAlive || !_host.IsInsideTree()
            || _host.IsQueuedForDeletion() || !_host.IsAncestorOf(this) || !GodotObject.IsInstanceValid(_host.Registry)
            || !_host.Registry.Contains(_host) || !GodotObject.IsInstanceValid(_pivot) || !GodotObject.IsInstanceValid(_muzzle)
            || !IsAncestorOf(_pivot) || !_pivot.IsAncestorOf(_muzzle) || !GodotObject.IsInstanceValid(Config)
            || !Config.TryCapture(out _settings) || !CaptureAdditionalSettings()) return;
        _registry = _host.Registry.GetNodeOrNull<TowerRegistry>("../../TowerRegistry");
        if (!GodotObject.IsInstanceValid(_registry) || !_registry.IsInsideTree()) { _registry = null; return; }
        _heading = _pivot.GlobalRotation;
        _search = _settings.SearchSeconds * (.1 + .9 * (GetInstanceId() % 97) / 97.0);
        _running = true; SetPhysicsProcess(true);
    }
    // =========================================================
    // Specialized weapons can validate/capture their own small immutable extension before processing starts.
    protected virtual bool CaptureAdditionalSettings() => true;
    // =========================================================
    // Arrival/death stops future requests immediately; future world-owned bullets must retain independent lifetimes.
    private void OnRemoved(Enemy host) => Stop();
    // =========================================================
    // Clear references and sleep on disabled/invalid/removed hosts; later initialization/config assignment can restart.
    private void Stop()
    {
        _running = false; Target = null; Aligned = false; _registry = null; _scratch.Clear(); _search = 0;
        ResetWeapon(); OnMountStopped(); SetPhysicsProcess(false);
    }
    // =========================================================
    // Specialized cadence has no timer nodes and is reset whenever its mount changes lifecycle.
    protected virtual void ResetWeapon() { }
    // =========================================================
    // Optional transient presentation can reset on lifecycle changes without cancelling already-delivered world shots.
    protected virtual void OnMountStopped() { }
    // =========================================================
    // Disconnect both host events regardless of enemy-versus-component teardown ordering.
    public override void _ExitTree()
    {
        Stop();
        if (GodotObject.IsInstanceValid(_host)) { _host.Initialized -= OnInitialized; _host.Removed -= OnRemoved; }
        _host = null; _pivot = null; _muzzle = null; _ready = false;
    }
    #endregion

    #region Staggered Targeting and Aim
    // =========================================================
    // At most one acquisition per interval/tick, including target loss and large deltas; cached locks are revalidated.
    public override void _PhysicsProcess(double delta)
    {
        if (IsQueuedForDeletion()) { Stop(); return; }
        if (!_running || !double.IsFinite(delta) || delta <= 0 || !IsInsideTree() || GetTree().Paused) return;
        if (!GodotObject.IsInstanceValid(_host) || !_host.IsAlive || !_host.IsInsideTree() || _host.IsQueuedForDeletion()
            || !GodotObject.IsInstanceValid(_host.Registry) || !_host.Registry.Contains(_host)
            || !GodotObject.IsInstanceValid(_registry) || !_registry.IsInsideTree() || _registry.IsQueuedForDeletion()
            || !GodotObject.IsInstanceValid(_pivot) || !_pivot.IsInsideTree() || _pivot.IsQueuedForDeletion()
            || !GodotObject.IsInstanceValid(_muzzle) || !_muzzle.IsInsideTree() || _muzzle.IsQueuedForDeletion())
        { Stop(); return; }
        if (!_host.GlobalPosition.IsFinite() || !_pivot.GlobalPosition.IsFinite() || !_muzzle.GlobalPosition.IsFinite())
        { Target = null; Aligned = false; ResetWeapon(); return; }
        _search -= delta;
        if (_search <= 0)
        {
            Target = EnemyTowerTargeting.Closest(_registry, _pivot.GlobalPosition, _settings.Range, _scratch);
            TargetQueries++; LastCandidates = _registry.LastCandidates; _search = _settings.SearchSeconds;
        }
        if (!EnemyTowerTargeting.Valid(_registry, Target, _pivot.GlobalPosition, _settings.Range))
        { Target = null; Aligned = false; ResetWeapon(); return; }
        // Stasis halts the optional gun as well as locomotion; permanent Cryo slow remains a movement modifier.
        if (_host.HasStatus(StatusEffectId.Freeze)) { Aligned = false; ResetWeapon(); return; }
        _pivot.GlobalRotation = _heading; // Counter hull turning before applying the gun's independent turn limit.
        Aligned = TurretAiming.Track(_pivot, Target.GlobalPosition, _settings.TurnSpeed, _settings.Tolerance, true, delta);
        _heading = _pivot.GlobalRotation;
        if (Aligned) OnAimReady(delta); else ResetWeapon();
    }
    // =========================================================
    // EG1 exposes an aiming seam; EG2 will consume minigun requests through the central hostile bullet system.
    protected virtual void OnAimReady(double delta) { }
    #endregion
}
