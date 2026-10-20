// Optional authored enemy weapon parameters; mounts capture immutable settings per initialization/config assignment.
using Godot;

public readonly record struct EnemyWeaponSettings(float Range, float TurnSpeed, float Tolerance, double SearchSeconds,
    float Damage, float BulletSpeed, float ShotsPerSecond);

[GlobalClass]
public partial class EnemyWeaponConfig : Resource
{
    #region Inspector Settings
    [ExportGroup("Targeting")]
    [Export(PropertyHint.Range, "1,4096,1,suffix:px")] public float RangePixels { get; set; } = 260;
    [Export(PropertyHint.Range, "0.05,5,0.01,suffix:s")] public double TargetSearchSeconds { get; set; } = .2;
    [ExportGroup("Aiming")]
    [Export] public float RotationSpeedDegrees { get; set; } = 240;
    [Export] public float AimToleranceDegrees { get; set; } = 6;
    [ExportGroup("Future Bullet Delivery")]
    [Export] public float DamagePerBullet { get; set; } = 2;
    [Export] public float BulletSpeedPixels { get; set; } = 1200;
    [Export] public float ShotsPerSecond { get; set; } = 6;
    #endregion

    #region Validation
    // =========================================================
    // Reject malformed capability data rather than poisoning transforms/timers or changing shared source resources.
    public bool TryCapture(out EnemyWeaponSettings settings)
    {
        settings = default;
        if (!float.IsFinite(RangePixels) || RangePixels <= 0 || RangePixels > 4096
            || !double.IsFinite(TargetSearchSeconds) || TargetSearchSeconds < .05 || TargetSearchSeconds > 5
            || !float.IsFinite(RotationSpeedDegrees) || RotationSpeedDegrees < 0 || RotationSpeedDegrees > 10000
            || !float.IsFinite(AimToleranceDegrees) || AimToleranceDegrees < 0 || AimToleranceDegrees > 180
            || !float.IsFinite(DamagePerBullet) || DamagePerBullet < 0
            || !float.IsFinite(BulletSpeedPixels) || BulletSpeedPixels <= 0
            || !float.IsFinite(ShotsPerSecond) || ShotsPerSecond <= 0 || ShotsPerSecond > 1000) return false;
        settings = new(RangePixels, RotationSpeedDegrees, AimToleranceDegrees, TargetSearchSeconds, DamagePerBullet, BulletSpeedPixels, ShotsPerSecond);
        return true;
    }
    #endregion
}
