// Minigun burst/rest extension; source resources remain read-only while each mount captures private timing values.
using Godot;

[GlobalClass]
public partial class EnemyMinigunConfig : EnemyWeaponConfig
{
    #region Bullet Presentation and Lifetime
    [ExportGroup("Bullet Delivery")]
    [Export] public ProjectileVisualStyle ProjectileVisual { get; set; }
    [Export] public double BulletLifetimeSeconds { get; set; } = 2.5;
    #endregion

    #region Burst Timing
    [ExportGroup("Minigun Burst")]
    [Export] public double BurstSeconds { get; set; } = 1.5;
    [Export] public double RestSeconds { get; set; } = 1;
    // =========================================================
    // Invalid burst timing disables this optional weapon without allocating timers or changing the source resource.
    public bool TryCaptureBurst(out double burst, out double rest)
    {
        burst = BurstSeconds; rest = RestSeconds;
        return double.IsFinite(BulletLifetimeSeconds) && BulletLifetimeSeconds > 0 && BulletLifetimeSeconds <= 30
            && double.IsFinite(burst) && burst > 0 && burst <= 60 && double.IsFinite(rest) && rest >= 0 && rest <= 60;
    }
    #endregion
}
