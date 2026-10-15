// Editable single-barrel starter turret stats for automatic targeting and firing.
// The shared DefaultBattleTowerConfig.tres is editable in the Godot Inspector.
using Godot;
using System.Collections.Generic;

[GlobalClass]
public partial class BattleTowerConfig : TowerConfig
{
    // =========================================================
    // Preserve the existing Battle identity when authored resources omit default-valued fields.
    public BattleTowerConfig()
    {
        DisplayName = "Battle Tower";
        Description = "Versatile baseline turret: one barrel and balanced range/fire rate.";
    }

    [ExportGroup("Combat")]
    [Export(PropertyHint.Range, "32,1024,16")] public float RangePixels { get => _rangePixels; set => SetStat(ref _rangePixels, value); }
    [Export(PropertyHint.Range, "0,10,0.1")] public float ShotsPerSecond { get => _shotsPerSecond; set => SetStat(ref _shotsPerSecond, value); }
    [Export(PropertyHint.Range, "1,100,1")] public int DamagePerShot { get => _damagePerShot; set => SetStat(ref _damagePerShot, value); }
    [Export(PropertyHint.Range, "50,3000,25")] public float ProjectileSpeed { get => _projectileSpeed; set => SetStat(ref _projectileSpeed, value); }
    [ExportGroup("Shield Penetration")]
    [Export(PropertyHint.Range, "0,1,0.01")] public float ShieldPenetration { get => _shieldPenetration; set => SetStat(ref _shieldPenetration, value); }
    [ExportGroup("Aiming")]
    [Export(PropertyHint.Range, "0,720,5")] public float RotationSpeedDegrees { get => _rotationSpeedDegrees; set => SetStat(ref _rotationSpeedDegrees, value); }
    [Export(PropertyHint.Range, "0,180,1")] public float AimToleranceDegrees { get => _aimToleranceDegrees; set => SetStat(ref _aimToleranceDegrees, value); }
    [Export] public bool RotateWhileTracking { get; set; } = true;

    [ExportGroup("Targeting")]
    [Export] public TowerTargeting.Priority TargetPriority { get; set; } = TowerTargeting.Priority.First;
    [Export(PropertyHint.Range, "0.05,2,0.05")] public float TargetSearchInterval { get => _targetSearchInterval; set => SetStat(ref _targetSearchInterval, value); }

    #region Stat Change Notifications
    private float _shieldPenetration = 0.05f;
    private float _rangePixels = 256f;
    private float _shotsPerSecond = 1f;
    private int _damagePerShot = 12;
    private float _projectileSpeed = 600f;
    private float _rotationSpeedDegrees = 180f;
    private float _aimToleranceDegrees = 12f;
    private float _targetSearchInterval = 0.15f;

    // =========================================================
    // Inspector and C# edits invalidate listening instances without per-frame polling.
    private void SetStat<T>(ref T field, T value)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return;
        field = value; EmitChanged();
    }
    #endregion

    // =========================================================
    // Copy external tier assets explicitly; Duplicate(true) alone retains these array references.
    public override BattleTowerConfig CreateInstanceCopy() => (BattleTowerConfig)base.CreateInstanceCopy();

    // =========================================================
    // Keep the existing eight-stage Battle adapter and exact tier arithmetic.
    public override TowerFinalStats ResolveStats(int level) => TowerStatPipeline.Resolve(this, level);

    // =========================================================
    // Weapon-specific detail belongs to the weapon config, not generic placement or HUD.
    public override string DescribeStats(TowerFinalStats final) =>
        $"Barrels: 1\nRange: {final.RangePixels:0} px\nRate: {final.ShotsPerSecond:0.##} shots/s\nDamage: {final.DamagePerShot}\nShield Penetration: {final.ShieldPenetration * 100:0.##}%\nTargeting: {TargetPriority}";

    // =========================================================
    // Describe the same resolved snapshot used by upgrade application.
    public override string DescribeChanges(TowerFinalStats now, TowerFinalStats future)
    {
        string changes = "";
        if (now.ShieldPenetration != future.ShieldPenetration) changes += $"Shield Penetration: {now.ShieldPenetration * 100:0.##}% → {future.ShieldPenetration * 100:0.##}%\n";
        if (now.DamagePerShot != future.DamagePerShot) changes += $"Damage: {now.DamagePerShot} → {future.DamagePerShot}\n";
        if (now.ShotsPerSecond != future.ShotsPerSecond) changes += $"Rate: {now.ShotsPerSecond:0.##} → {future.ShotsPerSecond:0.##} shots/s\n";
        if (now.RangePixels != future.RangePixels) changes += $"Range: {now.RangePixels:0} → {future.RangePixels:0} px\n";
        if (now.ProjectileSpeed != future.ProjectileSpeed) changes += $"Projectile: {now.ProjectileSpeed:0} → {future.ProjectileSpeed:0} px/s\n";
        if (now.RotationSpeedDegrees != future.RotationSpeedDegrees) changes += $"Turn: {now.RotationSpeedDegrees:0} → {future.RotationSpeedDegrees:0}°/s\n";
        if (now.AimToleranceDegrees != future.AimToleranceDegrees) changes += $"Aim tolerance: {now.AimToleranceDegrees:0} → {future.AimToleranceDegrees:0}°\n";
        if (now.TargetSearchInterval != future.TargetSearchInterval) changes += $"Target search: {now.TargetSearchInterval:0.##} → {future.TargetSearchInterval:0.##}s\n";
        return changes;
    }
}
