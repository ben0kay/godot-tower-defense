// Shared passive generator building; lifetime, work, disable, config and transform events own its power registration.
using Godot;
using System.Collections.Generic;

public partial class PowerGenerator : TowerBase
{
    [Export] public PowerGeneratorConfig Config { get; set; }
    public override TowerConfig AuthoredConfig => Config;
    public override List<TowerLevel> Levels => Config.Levels;
    public override TowerFinalStats ResolveLevel(int level) => null;
    public override bool AcceptsConfig(TowerConfig config) => config is PowerGeneratorConfig typed && typed.ValidGeneration;
    protected override void ApplyConfig(TowerConfig config) => Config = (PowerGeneratorConfig)config;
    public override bool ValidateScene() => Config?.ValidGeneration == true && GetNodeOrNull<Sprite2D>("Sprite") != null;
    public float CurrentOutput { get; private set; }
    public int PowerRegion { get; private set; }
    private PowerGridManager _power;
    private WorldGrid _grid;
    private PowerGeneratorConfig _observedConfig;
    private string _powerId;

    // =========================================================
    // Shared durability initializes after concrete Ready and publishes HealthChanged, which activates finished sources.
    public override void _Ready()
    {
        SetProcess(false); SetOperationalPhysicsProcess(false);
        _observedConfig = Config;
        _power = GetNodeOrNull<PowerGridManager>("../../PowerNetworks");
        _grid = _power?.Grid; _powerId = "generator:" + SaveId;
        WorkStarted += RefreshOutput; WorkCompleted += RefreshOutput;
        DisableStateChanged += RefreshOutput; HealthChanged += RefreshOutput;
        SpatialChanged += OnSpatialChanged; _observedConfig.Changed += OnConfigChanged;
        if (_grid != null) { _grid.TerrainRebuilt += RefreshOutput; ObserveSpatialChanges(true); }
        RefreshOutput();
    }

    // =========================================================
    // The generic completion hook already publishes WorkCompleted; no extra node/timer is needed for startup.
    private void OnSpatialChanged(TowerBase tower) => RefreshOutput();
    private void OnConfigChanged() { NotifyStatsChanged(); RefreshOutput(); }

    // =========================================================
    // Tower roots are footprint CENTERS, unlike Citadels. Convert back to the first cell's center in grid-local space.
    private void RefreshOutput()
    {
        if (!GodotObject.IsInstanceValid(_power) || !GodotObject.IsInstanceValid(_grid)) return;
        int region = 0; float output = 0;
        bool running = IsInsideTree() && !IsQueuedForDeletion() && Health > 0 && CanOperate;
        if (running && _observedConfig.ValidGeneration)
        {
            float cellSize = _grid.Config.CellSize;
            Vector2 firstCenter = _grid.ToLocal(GlobalPosition) - (Vector2)(Footprint - Vector2I.One) * (cellSize * .5f);
            Vector2I anchor = _grid.WorldToCell(_grid.ToGlobal(firstCenter));
            _power.SetProducer(_powerId, anchor, Footprint, _observedConfig.Output);
            region = _power.RegionForFootprint(anchor, Footprint);
            output = region > 0 ? _observedConfig.Output : 0;
        }
        else _power.RemoveEndpoint(_powerId);
        if (CurrentOutput == output && PowerRegion == region) return;
        CurrentOutput = output; PowerRegion = region; NotifyRuntimeChanged();
    }

    // =========================================================
    // Selected-only text complements the shared HP/work/condition displays; the consumer power bar is PW4 work.
    public override string DescribeRuntime() => !GodotObject.IsInstanceValid(_power)
        ? "Generation preview · no live power network"
        : $"Generation: {CurrentOutput:0.##} / {Config.Output:0.##} power · " +
            (PowerRegion > 0 ? $"Island {PowerRegion}" : IsBusy ? "Construction pending" : IsDisabled ? "Disabled" : "No conducting island");

    // =========================================================
    // Death queues removal before HealthChanged, withdrawing output immediately; external removal follows this teardown.
    public override void _ExitTree()
    {
        if (GodotObject.IsInstanceValid(_power)) _power.RemoveEndpoint(_powerId);
        if (GodotObject.IsInstanceValid(_grid)) _grid.TerrainRebuilt -= RefreshOutput;
        if (GodotObject.IsInstanceValid(_observedConfig)) _observedConfig.Changed -= OnConfigChanged;
        WorkStarted -= RefreshOutput; WorkCompleted -= RefreshOutput;
        DisableStateChanged -= RefreshOutput; HealthChanged -= RefreshOutput; SpatialChanged -= OnSpatialChanged;
        CurrentOutput = 0; PowerRegion = 0; _power = null; _grid = null; _observedConfig = null;
        base._ExitTree();
    }
}
