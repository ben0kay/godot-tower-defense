// Global enemy limits and navigation budgets; individual ship stats live in definitions.
using Godot;

[GlobalClass]
public partial class EnemyMasterConfig : Resource
{
    [ExportGroup("Navigation")]
    [Export(PropertyHint.Range, "1,64,1")] public int MaxPathCalculationsPerFrame { get; set; } = 8;
    [Export(PropertyHint.Range, "0.1,10,0.1")] public float FailedPathRetrySeconds { get; set; } = 1f;
    [ExportGroup("Path Diversity")]
    [Export] public bool EnablePathDiversity { get; set; } = true;
    [Export(PropertyHint.Range, "0,3,0.05")] public float PathDiversityStrength { get; set; } = 0.6f;
    [Export(PropertyHint.Range, "1,8,1")] public int RouteProfileCount { get; set; } = 4;
    [Export(PropertyHint.Range, "2,32,1")] public int PreferencePatchCells { get; set; } = 8;
    [Export] public int RoutePreferenceSeed { get; set; } = 12345;
    [ExportGroup("Optional Congestion")]
    [Export(PropertyHint.Range, "0,2,0.05")] public float CongestionInfluence { get; set; } = 0f;
    [Export(PropertyHint.Range, "0.1,5,0.1")] public float CongestionSnapshotSeconds { get; set; } = 0.5f;
    [ExportGroup("Separation (Reserved, Not Active)")]
    [Export(PropertyHint.Range, "0,10,0.1")] public float SeparationStrength { get; set; } = 0f;
    [Export(PropertyHint.Range, "0,128,1")] public float SeparationRadius { get; set; } = 48f;
    [ExportGroup("Safety")]
    [Export(PropertyHint.Range, "1,5000,1")] public int MaxActiveEnemies { get; set; } = 1000;
}
