// Cryo identity and presentation; all level-dependent numbers live only in CryoTowerLevels.
using Godot;

[GlobalClass]
public partial class CryoTowerConfig : ProjectileTurretConfig
{
    public override System.Collections.Generic.IReadOnlyList<TowerRankDefinition> Ranks => CryoTowerRanks.Rows;
    // =========================================================
    // Reuse physical ammunition and the current purchased-level/rank/construction architecture.
    public CryoTowerConfig()
    {
        DisplayName = "Cryo Tower";
        Description = "Energy projectiles permanently slow eligible enemies. Does not target already-slowed enemies.";
        FiringMode = BarrelFireMode.Single; Levels = CryoTowerLevels.Create();
    }
    // =========================================================
    // Preserve shield/hull overrides and penetration through the standard damage resolver.
    public override DamageInfo CreateHit(TowerFinalStats stats, DamageSource source = null) =>
        (stats.Weapon ?? new WeaponDamageStats(DamageType.Energy, false, 1, false, 1))
            .CreateHit(stats.DamagePerShot, stats.ShieldPenetration, source);
    // =========================================================
    // Inspector and preview describe the actual cached effective status capability.
    public override string DescribeStats(TowerFinalStats stats) => base.DescribeStats(stats)
        + $"\nDamage type: {stats.Weapon?.Type}\nPermanent slow: {(stats.Slow?.Fraction ?? 0) * 100:0.##}%\nTargets: unslowed, nonimmune only";
    // =========================================================
    // Future level/research strength changes cannot silently diverge from the upgrade preview.
    public override string DescribeChanges(TowerFinalStats now, TowerFinalStats future) =>
        base.DescribeChanges(now, future) + DescribeWeaponChanges(now, future)
        + (now.Slow == future.Slow ? "" : $"Permanent slow: {(now.Slow?.Fraction ?? 0) * 100:0.##}% → {(future.Slow?.Fraction ?? 0) * 100:0.##}%\n");
    // =========================================================
    // Shared authored resources keep private mutable runtime level rows.
    public override CryoTowerConfig CreateInstanceCopy() => (CryoTowerConfig)base.CreateInstanceCopy();
}
