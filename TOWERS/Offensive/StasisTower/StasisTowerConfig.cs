// Stasis identity and presentation; all level-dependent numbers live only in StasisTowerLevels.
using Godot;

[GlobalClass]
public partial class StasisTowerConfig : ProjectileTurretConfig
{
    #region Stasis Definition

    public override System.Collections.Generic.IReadOnlyList<TowerRankDefinition> Ranks => StasisTowerRanks.Rows;
    // =========================================================
    // Reuse physical ammunition and the current purchased-level/rank/construction architecture.
    public StasisTowerConfig()
    {
        DisplayName = "Stasis Tower";
        Description = "Energy projectiles temporarily stop eligible enemies. Does not target frozen or immune enemies.";
        FiringMode = BarrelFireMode.Single; Levels = StasisTowerLevels.Create();
    }
    // =========================================================
    // Preserve shield/hull overrides and penetration through the standard damage resolver.
    public override DamageInfo CreateHit(TowerFinalStats stats, DamageSource source = null) =>
        (stats.Weapon ?? new WeaponDamageStats(DamageType.Energy, false, 1, false, 1))
            .CreateHit(stats.DamagePerShot, stats.ShieldPenetration, source);
    // =========================================================
    // Inspector and preview describe the actual cached effective status capability.
    public override string DescribeStats(TowerFinalStats stats) => base.DescribeStats(stats)
        + $"\nDamage type: {stats.Weapon?.Type}\nStasis: {stats.Freeze?.DurationSeconds ?? 0:0.##}s\nTargets: unfrozen, nonimmune only";
    // =========================================================
    // Future level/research strength changes cannot silently diverge from the upgrade preview.
    public override string DescribeChanges(TowerFinalStats now, TowerFinalStats future) =>
        base.DescribeChanges(now, future) + DescribeWeaponChanges(now, future)
        + (now.Freeze == future.Freeze ? "" : $"Stasis: {now.Freeze?.DurationSeconds ?? 0:0.##} → {future.Freeze?.DurationSeconds ?? 0:0.##}s\n");
    // =========================================================
    // Shared authored resources keep private mutable runtime level rows.
    public override StasisTowerConfig CreateInstanceCopy() => (StasisTowerConfig)base.CreateInstanceCopy();
    #endregion
}
