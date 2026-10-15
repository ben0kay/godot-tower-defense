// Omnidirectional periodic pulse weapon; shared emitter owns each launched wave and its timed damage.
using Godot;
using System.Collections.Generic;

public partial class ShockwaveTower : TowerBase
{
    private ShockwaveTowerConfig _config;
    private bool _ready;
    private double _cooldown;
    private Sprite2D _sprite;
    private EnemyRegistry _registry;
    private Node2D _delivery, _effects;
    [Export] public bool Enabled { get; set; } = true;
    [Export] public ShockwaveTowerConfig Config
    {
        get => _config;
        set
        {
            if (_config == value) return;
            if (_ready && GodotObject.IsInstanceValid(_config)) _config.Changed -= RebuildStats;
            _config = value;
            if (!_ready) return;
            if (_config != null) _config.Changed += RebuildStats;
            RebuildStats();
        }
    }
    public override TowerConfig AuthoredConfig => Config;
    public override List<TowerLevel> Levels => Config?.Levels;
    public override float? InspectionRange => FinalStats?.RangePixels;

    // =========================================================
    // This tower opts into a wave-only config, never mandatory projectile or turret nodes.
    public override bool AcceptsConfig(TowerConfig config) => config is ShockwaveTowerConfig;
    // =========================================================
    // Validate the actual presentation needed by this scene before payment/placement.
    public override bool ValidateScene() => GetNodeOrNull<Sprite2D>("Sprite") != null;
    // =========================================================
    // Generic placement supplies its private compatible config through the established contract.
    protected override void ApplyConfig(TowerConfig config) => Config = (ShockwaveTowerConfig)config;

    // =========================================================
    // Cache world services once; ghost/build previews have no registry and never emit attacks.
    public override void _Ready()
    {
        _sprite = GetNode<Sprite2D>("Sprite"); _ready = true; SetProcess(false);
        if (Config != null) Config.Changed += RebuildStats;
        RebuildStats();
        _registry = GetNodeOrNull<EnemyRegistry>("../../EnemySystems/Registry");
        _delivery = GetNodeOrNull<Node2D>("../../Projectiles");
        _effects = GetNodeOrNull<Node2D>("../../Effects");
        SetPhysicsProcess(_registry != null && _delivery != null);
    }

    // =========================================================
    // Emit immediately, then periodically even without targets; do not burst missed intervals after a hitch.
    public override void _PhysicsProcess(double delta)
    {
        if (!Enabled || FinalStats?.Shockwave == null || !IsInsideTree() || IsQueuedForDeletion()
            || !double.IsFinite(delta) || delta <= 0 || !GodotObject.IsInstanceValid(_registry)
            || !GodotObject.IsInstanceValid(_delivery) || !_delivery.IsInsideTree()) return;
        ShockwaveStats wave = FinalStats.Shockwave.Value;
        _cooldown -= delta;
        if (wave.DamagePerWave <= 0) { _cooldown = System.Math.Max(0, _cooldown); return; }
        if (_cooldown > 1e-8) return;
        ShockwaveEmitter emission = new();
        emission.Initialize(_registry, GlobalPosition, Config.CreateHit(FinalStats, DamageSource), FinalStats.RangePixels,
            wave.ExpansionSpeedPixelsPerSecond, wave.EdgeDamageFraction, _effects, Config.WaveStyle);
        _delivery.AddChild(emission);
        _cooldown = _cooldown > -wave.IntervalSeconds ? _cooldown + wave.IntervalSeconds : wave.IntervalSeconds;
    }

    // =========================================================
    // Purchases and previews select independent complete C# rows through the shared pipeline.
    public override TowerFinalStats ResolveLevel(int level) => ResolveRankedLevel(level);
    // =========================================================
    // Explicit input changes invalidate once, while normal wave/weapon ticks read the cached result.
    public void RebuildStats() { FinalStats = Config == null ? null : ResolveLevel(UpgradeLevel); NotifyStatsChanged(); }
    // =========================================================
    // Existing emissions remain independent; new attacks stop when the source leaves the world.
    public override void _ExitTree()
    { if (GodotObject.IsInstanceValid(_config)) _config.Changed -= RebuildStats; _ready = false; }
    // =========================================================
    // Preserve purple art while the generic selection state adds the established warm tint.
    public override void SetSelected(bool selected) => _sprite.Modulate = selected ? new Color(1f, .9f, .68f) : Colors.White;
}
