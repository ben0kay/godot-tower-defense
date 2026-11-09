// Placeable visual ResourceStorage; reuse normal construction/hull/Inspector/saving, no SALV1 inventory yet.
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

    // =========================================================
    // Design-pass only: SALV1 will introduce physical stock and valid dispatch ownership.
    public override void _Ready() { SetProcess(false); SetPhysicsProcess(false); }
    public override string DescribeRuntime() =>
        IsBusy ? "Construction in progress · Metal storage pending" : "Metal storage pending — SALV1 not connected";
}
