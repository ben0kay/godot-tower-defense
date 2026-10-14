// Global enemy limits and navigation budgets; individual ship stats live in definitions.
using Godot;

[GlobalClass]
public partial class EnemyMasterConfig : Resource
{
    [ExportGroup("Navigation")]
    [Export(PropertyHint.Range, "1,64,1")] public int MaxPathCalculationsPerFrame { get; set; } = 8;
    [Export(PropertyHint.Range, "0.1,10,0.1")] public float FailedPathRetrySeconds { get; set; } = 1f;
    [ExportGroup("Safety")]
    [Export(PropertyHint.Range, "1,5000,1")] public int MaxActiveEnemies { get; set; } = 1000;
}
