// Placeable SALV1 building with mission-local physical stock.
using Godot;
using System.Collections.Generic;

public partial class ScrapRefinery : TowerBase
{
    [Export] public ScrapRefineryConfig Config { get; set; }
    public override TowerConfig AuthoredConfig => Config;
    public override List<TowerLevel> Levels => Config.Levels;
    public override TowerFinalStats ResolveLevel(int level) => null;
    public override bool AcceptsConfig(TowerConfig config) =>
        config is ScrapRefineryConfig typed && typed.Levels.Count == 0 &&
        float.IsFinite(typed.MaxHealth) && typed.MaxHealth > 0 &&
        double.IsFinite(typed.BuildSeconds) && typed.BuildSeconds > 0;
    protected override void ApplyConfig(TowerConfig config) => Config = (ScrapRefineryConfig)config;
    public override bool ValidateScene() => Config != null && GetNodeOrNull<Sprite2D>("Sprite")?.Texture != null;

    internal int Scrap, RawIncoming, Metal, ReturningMetal, Outbound;
    internal bool Processing;
    internal double BatchElapsed;
    internal int ScrapRoom => Mathf.Clamp(Config.ScrapCapacity, 1, 1000) - Scrap - RawIncoming;
    internal int MetalRoom => Mathf.Clamp(Config.OutputCapacity, 1, 1000) - Metal - ReturningMetal;
    internal int Yield => Mathf.Clamp(Config.MetalPerBatch, 1, 100);
    internal double Duration => double.IsFinite(Config.BatchSeconds) ? System.Math.Clamp(Config.BatchSeconds, .1, 300) : 8;
    internal bool HasWork => Processing || Scrap > 0 || Metal > 0;
    public override void _Ready() { SetProcess(false); SetPhysicsProcess(false); }
    public override string DescribeRuntime() =>
        $"Scrap {Scrap}/{Config.ScrapCapacity} (+{RawIncoming} incoming) · Batch {(Processing ? BatchElapsed / Duration : 0):P0} · " +
        $"Metal {Metal}/{Config.OutputCapacity} · Courier {Outbound} · " +
        (!CanOperate ? "Offline" : Processing ? "Refining" : Scrap > 0 && MetalRoom < Yield ? "Output full" : "Ready");
}
