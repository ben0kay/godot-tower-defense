// Inspector-configurable rotating reveal cone; one paid tier and no weapon/ammo/power capability.
using Godot;

[GlobalClass]
public partial class StealthBeaconConfig : TowerConfig
{
    #region Configuration
    private float _coneDegrees = 70, _rotationSeconds = 3, _updateSeconds = .1f, _opacity = .08f;
    [ExportGroup("Reveal Beacon")]
    [Export(PropertyHint.Range, "1,1024,1")] public float RevealRange
    {
        get => Levels[0].Stats.RangePixels;
        set => SetLevelStats(1, Levels[0].Stats with { RangePixels = value });
    }
    [Export(PropertyHint.Range, "10,180,1")] public float ScanConeDegrees { get => _coneDegrees; set => Set(ref _coneDegrees, value); }
    [Export(PropertyHint.Range, "0.1,3600,0.1")] public float ScanRotationSeconds { get => _rotationSeconds; set => Set(ref _rotationSeconds, value); }
    [Export(PropertyHint.Range, "0.01,1,0.01")] public float ScanUpdateIntervalSeconds { get => _updateSeconds; set => Set(ref _updateSeconds, value); }
    [Export(PropertyHint.Range, "0,0.3,0.01")] public float ScanConeOpacity { get => _opacity; set => Set(ref _opacity, value); }
    // =========================================================
    // Live source edits use the same event-driven rebuild seam as range edits.
    private void Set(ref float field, float value) { if (field == value) return; field = value; EmitChanged(); }
    // =========================================================
    // Existing cost/range/HP/work are preserved; only the reveal shape and scan are new.
    public StealthBeaconConfig()
    {
        DisplayName = "Stealth Beacon"; Description = "Sweeps a directional cone to reveal stealthed enemies for all towers. Leaving the scan restores stealth.";
        Levels = new() { new(1, 250, new(256, 0, 0, 0, 0, 0, 0, MaxHealth: 1800), 2) };
    }
    // =========================================================
    // Bound source geometry and cadence before placement or runtime activation.
    public bool IsValid => FiniteRange(RevealRange, 1, 1024) && FiniteRange(ScanConeDegrees, 10, 180)
        && FiniteRange(ScanRotationSeconds, .1f, 3600) && FiniteRange(ScanUpdateIntervalSeconds, .01f, 1)
        && FiniteRange(ScanConeOpacity, 0, .3f);
    private static bool FiniteRange(float value, float minimum, float maximum) => float.IsFinite(value) && value >= minimum && value <= maximum;
    public override TowerFinalStats ResolveStats(int level) => TowerStatPipeline.Resolve(this, level);
    public override string DescribeStats(TowerFinalStats stats) => $"Reveal cone: {ScanConeDegrees:0.#}° / {stats.RangePixels:0}px\nScan revolution: {ScanRotationSeconds:0.##}s\nReveals enemies inside the scan for all towers";
    #endregion
}
