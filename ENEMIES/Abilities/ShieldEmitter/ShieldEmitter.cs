// Optional friendly shield-restoration component; ordinary enemies pay no ability-node/tick cost.
using Godot;
using System;
public partial class ShieldEmitter : Node
{
    #region Inspector and Instance State
    private ShieldEmitterConfig _config;
    private bool _ready, _enabled = true, _running;
    private Enemy _host;
    private EnemyRegistry _registry;
    private float _timer;
    private int _cursor;
    [ExportGroup("Shield Emitter")]
    [Export] public Enemy Host { get; set; }
    [Export] public ShieldEmitterConfig Config
    {
        get => _config;
        set
        {
            if (_config == value) return;
            if (_ready && GodotObject.IsInstanceValid(_config)) _config.Changed -= OnConfigChanged;
            _config = value;
            if (!_ready) return;
            if (_config != null) _config.Changed += OnConfigChanged;
            OnConfigChanged();
        }
    }
    [Export] public bool Enabled
    {
        get => _enabled;
        set { _enabled = value; if (_ready) OnConfigChanged(); }
    }
    public int Pulses { get; private set; }
    public int LastCandidatesChecked { get; private set; }
    public int LastRecipientsRestored { get; private set; }
    #endregion
    #region Optional Host Lifecycle
    // =========================================================
    // Find an ancestor host or use an explicit Inspector reference; cache services only after spawn initialization.
    public override void _Ready()
    {
        SetProcess(false); SetPhysicsProcess(false);
        _host = Host;
        for (Node parent = GetParent(); _host == null && parent != null; parent = parent.GetParent())
            if (parent is Enemy enemy) _host = enemy;
        if (_host == null) return;
        // Mutable tuning is private to this ability; no source Resource or allied Definition is modified.
        _config = _config == null ? null : (ShieldEmitterConfig)_config.Duplicate(true);
        _ready = true;
        if (_config != null) _config.Changed += OnConfigChanged;
        _host.Initialized += OnHostInitialized; _host.Removed += OnHostRemoved;
        if (_host.IsAlive) OnHostInitialized(_host);
    }
    // =========================================================
    // Initialize once after registry/navigation setup; repeated lifecycle notifications cannot duplicate timers.
    private void OnHostInitialized(Enemy host)
    {
        if (_running || !Enabled || Config?.IsValid != true || !host.IsAlive || host.IsQueuedForDeletion() || host.Registry == null) return;
        _registry = host.Registry; _running = true; _cursor = 0;
        _timer = Config.PulseIntervalSeconds * (0.25f + 0.75f * (host.GetInstanceId() % 97) / 97f);
        SetPhysicsProcess(true);
    }
    // =========================================================
    // Invalid/disabled/null configuration stops work; valid edits begin one staggered schedule.
    private void OnConfigChanged()
    { Stop(); if (GodotObject.IsInstanceValid(_host)) OnHostInitialized(_host); }
    // =========================================================
    // Kill/arrival leaves existing recipient shield points intact, but stops all future pulses.
    private void OnHostRemoved(Enemy host) => Stop();
    // =========================================================
    // Disable processing and release cached world service references on removal/disable.
    private void Stop() { _running = false; _registry = null; SetPhysicsProcess(false); }
    // =========================================================
    // Release host/resource subscriptions on component or level unload.
    public override void _ExitTree()
    {
        Stop();
        if (GodotObject.IsInstanceValid(_host)) { _host.Initialized -= OnHostInitialized; _host.Removed -= OnHostRemoved; }
        if (_ready && GodotObject.IsInstanceValid(_config)) _config.Changed -= OnConfigChanged;
        _ready = false; _host = null;
    }
    #endregion
    #region Budgeted Pulse
    // =========================================================
    // Cheap countdown per tick; no catch-up burst, registry scan or scene traversal between pulses.
    public override void _PhysicsProcess(double delta)
    {
        if (!_running || !GodotObject.IsInstanceValid(_host) || !_host.IsAlive || _host.IsQueuedForDeletion() || Config?.IsValid != true)
        { Stop(); return; }
        if (!double.IsFinite(delta) || delta < 0) return;
        _timer -= (float)delta;
        if (_timer > 0) return;
        _timer = Config.PulseIntervalSeconds; Pulse();
    }
    // =========================================================
    // Rotate through a bounded slice of the existing living registry; large groups are serviced across pulses.
    private void Pulse()
    {
        Pulses++; LastCandidatesChecked = 0; LastRecipientsRestored = 0;
        int count = _registry.Living.Count;
        if (count == 0 || Config.ShieldPerPulse <= 0) return;
        int limit = Math.Min(count, Math.Clamp(Config.MaxCandidatesPerPulse, 1, 512));
        int recipientLimit = Math.Clamp(Config.MaxRecipientsPerPulse, 1, 512);
        double radiusSquared = (double)Config.RadiusPixels * Config.RadiusPixels;
        for (int i = 0; i < limit; i++)
        {
            _cursor %= count; Enemy target = _registry.Living[_cursor++]; LastCandidatesChecked++;
            if (!GodotObject.IsInstanceValid(target) || !target.IsAlive || target.IsQueuedForDeletion() ||
                (!Config.AffectSelf && target == _host) || target.Definition.MaxShield <= 0 ||
                (Config.Eligibility == ShieldEmitterConfig.TargetEligibility.AlreadyShieldedOnly && target.CurrentShield <= 0) ||
                _host.GlobalPosition.DistanceSquaredTo(target.GlobalPosition) > radiusSquared) continue;
            if (target.RestoreShield(Config.ShieldPerPulse) <= 0) continue;
            LastRecipientsRestored++;
            if (LastRecipientsRestored >= recipientLimit) break;
        }
    }
    #endregion
}
