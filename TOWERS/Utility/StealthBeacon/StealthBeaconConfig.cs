// Directional stealth scanner: three purchased coverage tiers and one Inspector-editable scan period.
using Godot;

[GlobalClass]
public partial class StealthBeaconConfig : TowerConfig
{
    #region Configuration
    private float _rotationSeconds = 4.5f, _updateSeconds = .1f, _opacity = .08f;

    [ExportGroup("Level 1 Scanner")]
    [Export(PropertyHint.Range, "1,1024,1")] public float RevealRange
    {
        get => Levels[0].Stats.RangePixels;
        set => SetLevelStats(1, Levels[0].Stats with { RangePixels = value });
    }
    [Export(PropertyHint.Range, "10,180,1")] public float ScanConeDegrees
    {
        get => Levels[0].Stats.ScanConeDegrees;
        set => SetLevelStats(1, Levels[0].Stats with { ScanConeDegrees = value });
    }

    [ExportGroup("Level 2 Scanner")]
    [Export(PropertyHint.Range, "1,1024,1")] public float Level2RevealRange
    {
        get => Levels[1].Stats.RangePixels;
        set => SetLevelStats(2, Levels[1].Stats with { RangePixels = value });
    }
    [Export(PropertyHint.Range, "10,180,1")] public float Level2ScanConeDegrees
    {
        get => Levels[1].Stats.ScanConeDegrees;
        set => SetLevelStats(2, Levels[1].Stats with { ScanConeDegrees = value });
    }

    [ExportGroup("Level 3 Scanner")]
    [Export(PropertyHint.Range, "1,1024,1")] public float Level3RevealRange
    {
        get => Levels[2].Stats.RangePixels;
        set => SetLevelStats(3, Levels[2].Stats with { RangePixels = value });
    }
    [Export(PropertyHint.Range, "10,180,1")] public float Level3ScanConeDegrees
    {
        get => Levels[2].Stats.ScanConeDegrees;
        set => SetLevelStats(3, Levels[2].Stats with { ScanConeDegrees = value });
    }

    [ExportGroup("Scan Timing and Presentation")]
    [Export(PropertyHint.Range, "0.1,3600,0.1")] public float ScanRotationSeconds { get => _rotationSeconds; set => Set(ref _rotationSeconds, value); }
    [Export(PropertyHint.Range, "0.01,1,0.01")] public float ScanUpdateIntervalSeconds { get => _updateSeconds; set => Set(ref _updateSeconds, value); }
    [Export(PropertyHint.Range, "0,0.3,0.01")] public float ScanConeOpacity { get => _opacity; set => Set(ref _opacity, value); }

    // =========================================================
    // Live edits to tier geometry or cadence use the original event-driven rebuild.
    private void Set(ref float field, float value) { if (field == value) return; field = value; EmitChanged(); }

    // =========================================================
    // Complete absolute snapshots: range and full scan angle increase modestly at each paid level.
    public StealthBeaconConfig()
    {
        DisplayName = "Stealth Beacon";
        Description = "Sweeps a directional cone to reveal stealthed enemies for all towers. Upgrades increase scan coverage.";
        Levels = new()
        {
            new(1, 250, new(256, 0, 0, 0, 0, 0, 0, MaxHealth: 1800, ScanConeDegrees: 70), 2),
            new(2, 175, new(280, 0, 0, 0, 0, 0, 0, MaxHealth: 1890, ScanConeDegrees: 80), 3),
            new(3, 250, new(304, 0, 0, 0, 0, 0, 0, MaxHealth: 1984.5f, ScanConeDegrees: 90), 4)
        };
    }

    // =========================================================
    // Preflight all three tiers, including user-edited angle/range values.
    public bool IsValid => Levels != null && Levels.Count == 3
        && Levels.TrueForAll(level => level != null && FiniteRange(level.Stats.RangePixels, 1, 1024)
            && FiniteRange(level.Stats.ScanConeDegrees, 10, 180))
        && FiniteRange(ScanRotationSeconds, .1f, 3600) && FiniteRange(ScanUpdateIntervalSeconds, .01f, 1)
        && FiniteRange(ScanConeOpacity, 0, .3f);
    private static bool FiniteRange(float value, float minimum, float maximum) => float.IsFinite(value) && value >= minimum && value <= maximum;
    public override TowerFinalStats ResolveStats(int level) => TowerStatPipeline.Resolve(this, level);

    // =========================================================
    // Existing Inspector and NEXT/Gain preview use the selected effective tier, not Level 1 constants.
    public override string DescribeStats(TowerFinalStats stats) =>
        $"Reveal cone: {stats.ScanConeDegrees:0.#}° / {stats.RangePixels:0}px\nScan revolution: {ScanRotationSeconds:0.##}s\nReveals enemies inside the scan for all towers";

    public override string DescribeChanges(TowerFinalStats now, TowerFinalStats future)
    {
        string changes = "";
        if (now.ScanConeDegrees != future.ScanConeDegrees)
            changes += $"Cone: {now.ScanConeDegrees:0.#}° → {future.ScanConeDegrees:0.#}°\n";
        if (now.RangePixels != future.RangePixels)
            changes += $"Range: {now.RangePixels:0} → {future.RangePixels:0} px\n";
        return changes;
    }
    #endregion
}
