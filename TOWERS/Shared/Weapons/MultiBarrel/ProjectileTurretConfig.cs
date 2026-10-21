// Reusable configuration for aimed Kinetic projectile towers; authored absolute numbers remain in each type's Levels file.
using Godot;

[GlobalClass]
public partial class ProjectileTurretConfig : TowerConfig, ITowerTargetingPolicy, IProjectileVisualConfig
{
    [ExportGroup("Firing")]
    [Export] public BarrelFireMode FiringMode { get; set; } = BarrelFireMode.Single;
    [ExportGroup("Weapon Visuals")]
    [Export] public bool ShieldRicochet { get; set; }
    [Export] public ProjectileVisualStyle ProjectileVisual { get; set; }
    [Export] public MuzzleFlashStyle MuzzleFlash { get; set; }
    [Export] public AimLaserStyle AimLaser { get; set; }
    [ExportGroup("Aiming")]
    [Export] public bool RotateWhileTracking { get; set; } = true;
    [ExportGroup("Targeting")]
    [Export] public TowerTargeting.Priority TargetPriority { get; set; } = TowerTargeting.Priority.First;
    public override string InspectorTargetPriorityLabel => TargetPriority.ToString();

    // =========================================================
    // C# Level 1 accessors edit the authored instance row, never a second shadow balance source.
    public float RangePixels { get => Levels[0].Stats.RangePixels; set => SetLevelStats(1, Levels[0].Stats with { RangePixels = value }); }
    public float ShotsPerSecond { get => Levels[0].Stats.ShotsPerSecond; set => SetLevelStats(1, Levels[0].Stats with { ShotsPerSecond = value }); }
    public int DamagePerShot { get => Levels[0].Stats.DamagePerShot; set => SetLevelStats(1, Levels[0].Stats with { DamagePerShot = value }); }
    public float ProjectileSpeed { get => Levels[0].Stats.ProjectileSpeed; set => SetLevelStats(1, Levels[0].Stats with { ProjectileSpeed = value }); }

    // =========================================================
    // Use the same eight-stage level/rank resolution as the four existing offensive towers.
    public override TowerFinalStats ResolveStats(int level) => TowerStatPipeline.Resolve(this, level);

    // =========================================================
    // This is bullet damage (Kinetic), not a per-muzzle damage multiplier.
    public virtual DamageInfo CreateHit(TowerFinalStats final, DamageSource source = null) =>
        new(final.DamagePerShot, DamageType.Kinetic, shieldPenetration: final.ShieldPenetration, source: source);

    // =========================================================
    // Inspector and purchased-level preview display the same effective snapshots as combat.
    public override string DescribeStats(TowerFinalStats final) =>
        $"Range: {final.RangePixels:0} px\nRate: {final.ShotsPerSecond:0.##} bullets/s\nDamage: {final.DamagePerShot}\nProjectile: {final.ProjectileSpeed:0} px/s\nShield Penetration: {final.ShieldPenetration * 100:0.##}%\nFire mode: {FiringMode}\nTargeting: {TargetPriority}";

    // =========================================================
    // Display only changed stats; each level row replaces the previous one completely.
    public override string DescribeChanges(TowerFinalStats now, TowerFinalStats future)
    {
        string changes = "";
        if (now.DamagePerShot != future.DamagePerShot) changes += $"Damage: {now.DamagePerShot} → {future.DamagePerShot}\n";
        if (now.ShotsPerSecond != future.ShotsPerSecond) changes += $"Rate: {now.ShotsPerSecond:0.##} → {future.ShotsPerSecond:0.##} bullets/s\n";
        if (now.RangePixels != future.RangePixels) changes += $"Range: {now.RangePixels:0} → {future.RangePixels:0} px\n";
        if (now.ProjectileSpeed != future.ProjectileSpeed) changes += $"Projectile: {now.ProjectileSpeed:0} → {future.ProjectileSpeed:0} px/s\n";
        if (now.ShieldPenetration != future.ShieldPenetration) changes += $"Shield Penetration: {now.ShieldPenetration * 100:0.##}% → {future.ShieldPenetration * 100:0.##}%\n";
        return changes;
    }
}
