// Corruptor authored controls use ordinary absolute level rows and shared power/heat/targeting contracts.
using Godot;
[GlobalClass]
public partial class CorruptorTowerConfig : ProjectileTurretConfig
{
    public override System.Collections.Generic.IReadOnlyList<TowerRankDefinition> Ranks => CorruptorTowerRanks.Rows;
    public CorruptorTowerConfig()
    {
        DisplayName = "Corruptor Tower";
        Description = "Hull-piercing pulses corrupt enemy ships. Prefers fresh targets, then refreshes affected ships.";
        Levels = CorruptorTowerLevels.Create(); FiringMode = BarrelFireMode.Single;
    }
    private bool _heatEnabled = true;
    private double _overheatMinutes = 3.5, _recoverySeconds = 10;
    [ExportGroup("Heat")]
    [Export] public bool HeatEnabled
    { get => _heatEnabled; set { if (_heatEnabled == value) return; _heatEnabled = value; EmitChanged(); } }
    [Export(PropertyHint.Range, "0.1,30,0.1,suffix:min")] public double OverheatAfterMinutes
    { get => _overheatMinutes; set { if (_overheatMinutes == value) return; _overheatMinutes = value; EmitChanged(); } }
    [Export(PropertyHint.Range, "1,120,0.5,suffix:s")] public double OverheatRecoverySeconds
    { get => _recoverySeconds; set { if (_recoverySeconds == value) return; _recoverySeconds = value; EmitChanged(); } }
    public override HeatTiming? SustainedHeatTiming => HeatEnabled ? new(OverheatAfterMinutes * 60, OverheatRecoverySeconds) : null;
    [ExportGroup("Level 1 Corruption")]
    [Export(PropertyHint.Range, "0,1000,1,or_greater")] public int Level1HitDamage
    { get => Levels[0].Stats.DamagePerShot; set => SetLevelStats(1, Levels[0].Stats with { DamagePerShot = value }); }
    [Export(PropertyHint.Range, "0,1000,0.1,or_greater")] public float Level1DamagePerSecond
    { get => Levels[0].Stats.Corruption?.DamagePerSecond ?? 0; set => SetLevelStats(1, Levels[0].Stats with { Corruption = (Levels[0].Stats.Corruption ?? default) with { DamagePerSecond = value } }); }
    [Export(PropertyHint.Range, "0.1,60,0.1,or_greater")] public float Level1DurationSeconds
    { get => Levels[0].Stats.Corruption?.DurationSeconds ?? 0; set => SetLevelStats(1, Levels[0].Stats with { Corruption = (Levels[0].Stats.Corruption ?? default) with { DurationSeconds = value } }); }
    [Export(PropertyHint.Range, "0.01,20,0.01,or_greater")] public float Level1AttackRate
    { get => Levels[0].Stats.ShotsPerSecond; set => SetLevelStats(1, Levels[0].Stats with { ShotsPerSecond = value }); }
    [Export(PropertyHint.Range, "1,1024,1,or_greater")] public float Level1Range
    { get => Levels[0].Stats.RangePixels; set => SetLevelStats(1, Levels[0].Stats with { RangePixels = value }); }
    [ExportGroup("Level 2 Corruption")]
    [Export(PropertyHint.Range, "0,1000,1,or_greater")] public int Level2HitDamage
    { get => Levels[1].Stats.DamagePerShot; set => SetLevelStats(2, Levels[1].Stats with { DamagePerShot = value }); }
    [Export(PropertyHint.Range, "0,1000,0.1,or_greater")] public float Level2DamagePerSecond
    { get => Levels[1].Stats.Corruption?.DamagePerSecond ?? 0; set => SetLevelStats(2, Levels[1].Stats with { Corruption = (Levels[1].Stats.Corruption ?? default) with { DamagePerSecond = value } }); }
    [Export(PropertyHint.Range, "0.1,60,0.1,or_greater")] public float Level2DurationSeconds
    { get => Levels[1].Stats.Corruption?.DurationSeconds ?? 0; set => SetLevelStats(2, Levels[1].Stats with { Corruption = (Levels[1].Stats.Corruption ?? default) with { DurationSeconds = value } }); }
    [Export(PropertyHint.Range, "0.01,20,0.01,or_greater")] public float Level2AttackRate
    { get => Levels[1].Stats.ShotsPerSecond; set => SetLevelStats(2, Levels[1].Stats with { ShotsPerSecond = value }); }
    [Export(PropertyHint.Range, "1,1024,1,or_greater")] public float Level2Range
    { get => Levels[1].Stats.RangePixels; set => SetLevelStats(2, Levels[1].Stats with { RangePixels = value }); }
    [ExportGroup("Level 3 Corruption")]
    [Export(PropertyHint.Range, "0,1000,1,or_greater")] public int Level3HitDamage
    { get => Levels[2].Stats.DamagePerShot; set => SetLevelStats(3, Levels[2].Stats with { DamagePerShot = value }); }
    [Export(PropertyHint.Range, "0,1000,0.1,or_greater")] public float Level3DamagePerSecond
    { get => Levels[2].Stats.Corruption?.DamagePerSecond ?? 0; set => SetLevelStats(3, Levels[2].Stats with { Corruption = (Levels[2].Stats.Corruption ?? default) with { DamagePerSecond = value } }); }
    [Export(PropertyHint.Range, "0.1,60,0.1,or_greater")] public float Level3DurationSeconds
    { get => Levels[2].Stats.Corruption?.DurationSeconds ?? 0; set => SetLevelStats(3, Levels[2].Stats with { Corruption = (Levels[2].Stats.Corruption ?? default) with { DurationSeconds = value } }); }
    [Export(PropertyHint.Range, "0.01,20,0.01,or_greater")] public float Level3AttackRate
    { get => Levels[2].Stats.ShotsPerSecond; set => SetLevelStats(3, Levels[2].Stats with { ShotsPerSecond = value }); }
    [Export(PropertyHint.Range, "1,1024,1,or_greater")] public float Level3Range
    { get => Levels[2].Stats.RangePixels; set => SetLevelStats(3, Levels[2].Stats with { RangePixels = value }); }
    // =========================================================
    // All three paid levels use the same resolved weapon profile and normal weak attribution token.
    public override DamageInfo CreateHit(TowerFinalStats stats, DamageSource source = null) =>
        stats.Weapon.Value.CreateHit(stats.DamagePerShot, stats.ShieldPenetration, source);
    public override string DescribeStats(TowerFinalStats stats) => base.DescribeStats(stats)
        + $"\nDamage type: Energy\nCorruption: {stats.Corruption?.DamagePerSecond ?? 0:0.##} hull HP/s for {stats.Corruption?.DurationSeconds ?? 0:0.##}s"
        + $"\nTotal potential: {stats.DamagePerShot + (stats.Corruption?.TotalDamage ?? 0):0.##} hull HP\nTargets: unaffected first; affected fallback; immune excluded";
    public override string DescribeChanges(TowerFinalStats now, TowerFinalStats future) => base.DescribeChanges(now, future)
        + (now.Corruption == future.Corruption ? "" : $"Corruption: {now.Corruption?.DamagePerSecond:0.##} → {future.Corruption?.DamagePerSecond:0.##} HP/s; {now.Corruption?.DurationSeconds:0.##} → {future.Corruption?.DurationSeconds:0.##}s\n");
    public override CorruptorTowerConfig CreateInstanceCopy() => (CorruptorTowerConfig)base.CreateInstanceCopy();
}
