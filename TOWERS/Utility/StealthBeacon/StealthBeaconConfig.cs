// Inspector-configurable reveal-only beacon; one paid tier, no weapon/ammo/rank capability.
using Godot;

[GlobalClass]
public partial class StealthBeaconConfig : TowerConfig
{
    #region Configuration
    [ExportGroup("Reveal Beacon")]
    [Export(PropertyHint.Range, "1,1024,1")] public float RevealRange
    {
        get => Levels[0].Stats.RangePixels;
        set => SetLevelStats(1, Levels[0].Stats with { RangePixels = value });
    }
    // =========================================================
    // Provisional cost/range/HP use the existing construction wallet and stat pipeline.
    public StealthBeaconConfig()
    {
        DisplayName = "Stealth Beacon"; Description = "Reveals stealthed enemies inside its radius for all towers. Leaving the radius restores stealth.";
        Levels = new() { new(1, 250, new(256, 0, 0, 0, 0, 0, 0, MaxHealth: 180), 2) };
    }
    // =========================================================
    // Bounded radius keeps the reveal index small and rejects malformed setup before payment.
    public bool IsValid => float.IsFinite(RevealRange) && RevealRange >= 1 && RevealRange <= 1024;
    public override TowerFinalStats ResolveStats(int level) => TowerStatPipeline.Resolve(this, level);
    public override string DescribeStats(TowerFinalStats stats) => $"Reveal range: {stats.RangePixels:0}px\nReveals enemies for all towers\nRequires an operational beacon";
    #endregion
}
