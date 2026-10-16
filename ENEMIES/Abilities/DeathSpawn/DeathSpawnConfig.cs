// Optional death-release authoring; instance/job state never lives in this shared resource.
using Godot;
using System;

public enum DeathSpawnPattern { RadialScatter }

[GlobalClass]
public partial class DeathSpawnConfig : Resource
{
    [ExportGroup("Children")]
    [Export] public string ChildEnemyId { get; set; } = "basic_mk1";
    [Export(PropertyHint.Range, "1,64,1")] public int ChildCount { get; set; } = 6;
    [Export] public bool SpawnWithShield { get; set; }
    [ExportGroup("Release")]
    [Export(PropertyHint.Range, "0,10,0.01")] public float SpawnIntervalSeconds { get; set; } = .12f;
    [Export] public DeathSpawnPattern SpawnPattern { get; set; } = DeathSpawnPattern.RadialScatter;
    [Export(PropertyHint.Range, "0,256,1")] public float ScatterRadiusPixels { get; set; } = 48;
    [Export(PropertyHint.Range, "1,32,1")] public int MaxPositionAttemptsPerChild { get; set; } = 8;
    [ExportGroup("Routing and Safety")]
    [Export] public bool InheritCheckpointProgress { get; set; } = true;
    [Export(PropertyHint.Range, "1,1,1")] public int MaxSpawnGenerations { get; set; } = 1;
    public bool IsValid => !string.IsNullOrWhiteSpace(ChildEnemyId) && ChildEnemyId == ChildEnemyId.Trim()
        && ChildCount >= 1 && ChildCount <= 64 && float.IsFinite(SpawnIntervalSeconds) && SpawnIntervalSeconds >= 0 && SpawnIntervalSeconds <= 10
        && float.IsFinite(ScatterRadiusPixels) && ScatterRadiusPixels >= 0 && ScatterRadiusPixels <= 256
        && Enum.IsDefined(SpawnPattern) && MaxPositionAttemptsPerChild >= 1 && MaxPositionAttemptsPerChild <= 32 && MaxSpawnGenerations == 1;
}
