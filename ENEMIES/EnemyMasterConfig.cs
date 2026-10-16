// Global enemy limits and navigation budgets; individual ship stats live in definitions.
using Godot;

[GlobalClass]
public partial class EnemyMasterConfig : Resource
{
    [ExportGroup("Navigation")]
    [Export(PropertyHint.Range, "1,64,1")] public int MaxPathCalculationsPerFrame { get; set; } = 8;
    [Export(PropertyHint.Range, "0.1,10,0.1")] public float FailedPathRetrySeconds { get; set; } = 1f;
    [Export(PropertyHint.Range, "0,20,0.1")] public float MaxPathfindingMillisecondsPerFrame { get; set; } = 2f;
    [Export(PropertyHint.Range, "1,128,1")] public int MaxRouteValidationsPerFrame { get; set; } = 16;
    [Export(PropertyHint.Range, "0,5,0.1")] public float MaxRouteValidationMillisecondsPerFrame { get; set; } = 0.5f;
    [Export(PropertyHint.Range, "1,16,1")] public int FairRequestEveryCalculations { get; set; } = 4;
    [ExportGroup("Movement Refinement")]
    [Export] public bool EnablePathOffsets { get; set; } = true;
    [Export(PropertyHint.Range, "0,24,1")] public float PathOffsetMaxPixels { get; set; } = 8f;
    [Export(PropertyHint.Range, "0,30,0.1")] public float RouteAuditIntervalSeconds { get; set; } = 3f;
    [ExportGroup("Corridor Path Variation")]
    [Export] public bool EnableCorridorVariation { get; set; } = false;
    private float _corridorPercent = 50, _corridorStrength = 0.65f;
    private int _corridorSpacing = 3;
    [Export(PropertyHint.Range, "0,100,1")] public float CorridorVariationPercent
    { get => _corridorPercent; set => _corridorPercent = float.IsFinite(value) ? Mathf.Clamp(value, 0, 100) : 0; }
    [Export(PropertyHint.Range, "0,1,0.05")] public float CorridorVariationStrength
    { get => _corridorStrength; set => _corridorStrength = float.IsFinite(value) ? Mathf.Clamp(value, 0, 1) : 0; }
    [Export(PropertyHint.Range, "2,12,1")] public int CorridorVariationSpacingCells
    { get => _corridorSpacing; set => _corridorSpacing = Mathf.Clamp(value, 2, 12); }
    [ExportGroup("Path Diversity")]
    [Export] public bool EnablePathDiversity { get; set; } = true;
    [Export(PropertyHint.Range, "0,3,0.05")] public float PathDiversityStrength { get; set; } = 0.6f;
    [Export(PropertyHint.Range, "1,16,1")] public int RouteProfileCount { get; set; } = 4;
    [Export(PropertyHint.Range, "2,32,1")] public int PreferencePatchCells { get; set; } = 8;
    [Export] public int RoutePreferenceSeed { get; set; } = 12345;
    [ExportGroup("Optional Congestion")]
    [Export(PropertyHint.Range, "0,2,0.05")] public float CongestionInfluence { get; set; } = 0f;
    [Export(PropertyHint.Range, "0.1,5,0.1")] public float CongestionSnapshotSeconds { get; set; } = 0.5f;
    [ExportGroup("Separation (Reserved, Not Active)")]
    [Export(PropertyHint.Range, "0,10,0.1")] public float SeparationStrength { get; set; } = 0f;
    [Export(PropertyHint.Range, "0,128,1")] public float SeparationRadius { get; set; } = 48f;
    [ExportGroup("Portal Spawning")]
    [Export(PropertyHint.Range, "0,24,1")] public float PortalSpawnOffsetPixels { get; set; } = 8f;
    [ExportGroup("Wave Scheduling")]
    [Export(PropertyHint.Range, "1,16,1")] public int MaxWaveSpawnAttemptsPerFrame { get; set; } = 2;
    [Export(PropertyHint.Range, "0.1,5,0.1")] public float WaveSpawnRetrySeconds { get; set; } = 0.5f;
    [Export] public int WaveRandomSeed { get; set; } = 12345;
    [ExportGroup("Death Releases")]
    [Export(PropertyHint.Range, "1,16,1")] public int MaxDeathSpawnAttemptsPerFrame { get; set; } = 2;
    [Export(PropertyHint.Range, "1,4096,1")] public int MaxPendingDeathReleaseJobs { get; set; } = 1024;
    [Export] public int DeathReleaseSeed { get; set; } = 6391;
    [ExportGroup("Safety")]
    [Export(PropertyHint.Range, "1,5000,1")] public int MaxActiveEnemies { get; set; } = 1000;
}
