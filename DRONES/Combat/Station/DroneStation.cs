// Placeable fighter-drone hangar foundation; normal TowerBase work/health/selling/saving, no active drone systems yet.
using Godot;
using System.Collections.Generic;

public partial class DroneStation : TowerBase
{
    [Export] public DroneStationConfig Config { get; set; }
    public override TowerConfig AuthoredConfig => Config;
    public override List<TowerLevel> Levels => Config.Levels;
    public override TowerFinalStats ResolveLevel(int level) => null;

    // =========================================================
    // Reuse shared passive building lifetime; station functionality is added by future fighter phases.
    public override bool AcceptsConfig(TowerConfig config) => config is DroneStationConfig;
    protected override void ApplyConfig(TowerConfig config) => Config = (DroneStationConfig)config;
    public override bool ValidateScene() => Config != null && GetNodeOrNull<Sprite2D>("Sprite") != null;

    // =========================================================
    // This placeholder building has no AI, target search, ammo generation or per-frame process.
    public override void _Ready()
    {
        SetProcess(false);
        SetOperationalPhysicsProcess(false);
    }

    public override string DescribeRuntime() => IsBusy
        ? "Hangar under construction"
        : "Hangar ready · Fighter systems planned (FD1-B)";
}
