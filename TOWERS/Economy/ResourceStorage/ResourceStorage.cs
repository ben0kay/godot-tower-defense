// Placeable SALV1 building with mission-local physical stock.
using Godot;
using System.Collections.Generic;

public partial class ResourceStorage : TowerBase
{
    [Export] public ResourceStorageConfig Config { get; set; }
    public override TowerConfig AuthoredConfig => Config;
    public override List<TowerLevel> Levels => Config.Levels;
    public override TowerFinalStats ResolveLevel(int level) => null;
    public override bool AcceptsConfig(TowerConfig config) =>
        config is ResourceStorageConfig typed && typed.Levels.Count == 0 &&
        float.IsFinite(typed.MaxHealth) && typed.MaxHealth > 0 &&
        double.IsFinite(typed.BuildSeconds) && typed.BuildSeconds > 0;
    protected override void ApplyConfig(TowerConfig config) => Config = (ResourceStorageConfig)config;
    public override bool ValidateScene() => Config != null && GetNodeOrNull<Sprite2D>("Sprite")?.Texture != null;

    internal int Metal, Reserved, Incoming;
    internal int Capacity => Mathf.Clamp(Config.MetalCapacity, 1, 1000);
    internal int Available => Metal - Reserved;
    internal int Room => Capacity - Metal - Incoming;
    public override void _Ready() { SetProcess(false); SetPhysicsProcess(false); }
    public override string DescribeRuntime() =>
        $"Metal {Metal}/{Capacity} · Reserved {Reserved} · Incoming {Incoming} · Available {Available}" +
        (CanOperate ? "" : " · Offline");
}
