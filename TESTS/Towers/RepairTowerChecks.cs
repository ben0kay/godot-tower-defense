// RT1 narrow fixture: real catalog/config/registry/health/beam seams without profile writes or broad simulations.
using Godot;
using System;
using System.Linq;
using static AmmoLoopFixture;

public partial class RepairTowerChecks : Node
{
    // =========================================================
    // Optional owner F6 check; no authored maps, balance resources or campaign files are edited.
    public override async void _Ready()
    {
        try
        {
            using (AmmoLoopFixture f = new(this))
            {
                SubsystemBattlefield.DetachSession(f.Main);
                Check(f);
                CheckStickyTarget(f);
            }
            await SubsystemBattlefield.DrainFreedFixtures(this);
            GD.Print("PASS: RT1 repair stats, friendly hull/beam, pause/disable/full/range/work and checkpoint inputs");
            GetTree().Quit();
        }
        catch (Exception error) { GetTree().Paused = false; GD.PushError(error.ToString()); GetTree().Quit(1); }
    }
    // =========================================================
    // Face up without special aim overrides; damage and repair use production hull methods.
    private void Check(AmmoLoopFixture f)
    {
        Vector2 origin = new(2000, 2000);
        RepairTower healer = f.Add<RepairTower>("repair", origin);
        BattleTower target = f.Add<BattleTower>("battle", origin + Vector2.Up * 100,
            config => { config.Power = null; config.MaxHealth = 100; });
        TowerDefinition definition = f.Placement.Catalog.Find("repair");
        Require(definition.Category == TowerDefinition.TowerCategory.Support && definition.Footprint == Vector2I.One
            && healer.Levels.Count == 3 && healer.AuthoredConfig.BuildSeconds == 5 && healer.Levels[0].Costs[0].Amount == 350,
            "Three support tiers preserve Level 1 cost, work and footprint");
        Require(healer.Levels[1].Costs[0].Amount == 225 && healer.Levels[2].Costs[0].Amount == 325
            && healer.Levels[1].WorkSeconds == 6 && healer.Levels[2].WorkSeconds == 7,
            "Support upgrades use the existing paid level and construction clocks");
        TowerFinalStats second = healer.ResolveLevel(2), third = healer.ResolveLevel(3);
        Require(second.Repair?.HullPerSecond == 20 && second.RangePixels == 344 && second.MaxHealth == 1050
            && third.Repair?.HullPerSecond == 24 && third.RangePixels == 368 && third.MaxHealth == 1102.5f,
            "Purchased tiers improve hull repair and range while preserving +5% hull progression");
        Require(healer.Config.DescribeChanges(healer.FinalStats, second).Contains("Repair:")
            && healer.Config.DescribeChanges(healer.FinalStats, second).Contains("Range:"),
            "Upgrade Inspector reports repair and radius gains");
        Require(healer.FinalStats.Repair?.HullPerSecond == 16 && healer.FinalStats.RangePixels == 320
            && healer.FinalStats.MaxHealth == 1000 && healer.Ammunition == null && healer.Power == null && healer.FinalStats.Beam == null,
            "Independent repair capability has no fabricated damage/ammo/power");
        Require(TowerUpgradeComparison.Build(healer.FinalStats, healer.FinalStats).Any(r => r.Name == "Repair (HP/s)")
            && !TowerUpgradeComparison.Build(healer.FinalStats, healer.FinalStats).Any(r => r.Name == "Damage"), "Inspector labels repair independently");
        var beam = healer.GetNode<BeamRenderer>("BeamRenderer");
        Hurt(target, 50); healer._PhysicsProcess(.5);
        Near(target.Health, 58, "16 HP/s applied only to damaged hull"); Require(beam.IsActive, "Valid aligned repair enables shared beam");
        Vector2 start = beam.ToGlobal(beam.GetNode<Line2D>("Core").GetPointPosition(0));
        Require(start.DistanceTo(healer.GetNode<Marker2D>("TurretPivot/Muzzle").GlobalPosition) < .001, "Beam leaves physical emitter tip");
        var stop = healer.ApplyDisable(TowerDisableReason.EMP);
        float before = target.Health; healer._PhysicsProcess(1);
        Require(target.Health == before && !beam.IsActive && !healer.IsPhysicsProcessing(), "Disabled healer clears beam and sleeps");
        healer.RemoveDisable(stop);
        var targetStop = target.ApplyDisable(TowerDisableReason.EMP); healer._PhysicsProcess(.5);
        Near(target.Health, before + 8, "Disabled target may still receive hull repair"); target.RemoveDisable(targetStop);
        GetTree().Paused = true; before = target.Health; healer._PhysicsProcess(1); GetTree().Paused = false;
        Require(target.Health == before, "Pause never applies repair time");
        target.RestoreHealth(100);
        Require(target.Health == target.MaxHealth && !beam.IsActive, "Full hull immediately clears all repair presentation");
        Hurt(target, 1); healer._PhysicsProcess(.5);
        Require(target.Health == target.MaxHealth && !beam.IsActive, "Final partial interval clamps exactly without over-heal");
        Hurt(target, 20); healer._PhysicsProcess(.5);
        target.GlobalPosition += Vector2.Right * 1000; target.ForceUpdateTransform();
        Require(!beam.IsActive, "Moved out-of-range lock clears immediately");
        target.GlobalPosition = origin + Vector2.Up * 100; target.ForceUpdateTransform();
        healer.Config.HullPerSecond = float.NaN;
        Require(healer.FinalStats.Repair?.HullPerSecond == 0 && !healer.IsPhysicsProcessing(), "Nonfinite repair is safely clamped and sleeps");
        healer.Config.HullPerSecond = 16;
        Require(f.Placement.Catalog.Find("repair").DefaultConfig.ResolveStats(1).Repair?.HullPerSecond == 16, "Instance edits never mutate canonical profile");
        healer.Free();
        RepairTower site = definition.Scene.Instantiate<RepairTower>(); site.InitializeDefinition(definition);
        var work = f.Main.GetNode<ConstructionWorkManager>("World/ConstructionWork");
        site.StartConstruction(work); f.Towers.AddChild(site); site.GlobalPosition = origin; Disable(site);
        before = target.Health; site._PhysicsProcess(1);
        Require(target.Health == before && !site.GetNode<BeamRenderer>("BeamRenderer").IsActive, "Unfinished structure never repairs");
        ConstructionTestClock.FinishWork(f.Main); site._PhysicsProcess(.5);
        Require(target.Health > before, "Completed shared work enables repair without another purchase");
        Hurt(site, 100);
        TowerSaveData saved = site.CaptureCheckpoint(_ => null); site.Free();
        RepairTower restored = definition.Scene.Instantiate<RepairTower>(); restored.InitializeDefinition(definition);
        restored.PrepareCheckpoint(saved, work, null); f.Towers.AddChild(restored);
        restored.SetPlacement(new(saved.X, saved.Y), origin); restored.RestoreCheckpoint(saved, _ => 0); Disable(restored);
        Near(restored.Health, saved.Health, "Existing checkpoint reconstruction preserves hull without repurchase");
        Require(restored.Power == null && restored.Ammunition == null && restored.FinalStats.Repair?.HullPerSecond == 16,
            "Restore derives repair capability with no transient target/beam save fields");
        restored.Free(); target.Free();
    }
    // =========================================================
    // Both ships stay eligible while their relative health fractions cross; only completion releases the lock.
    private void CheckStickyTarget(AmmoLoopFixture f)
    {
        Vector2 origin = new(2000, 2000);
        RepairTower healer = f.Add<RepairTower>("repair", origin);
        BattleTower first = f.Add<BattleTower>("battle", origin + Vector2.Up * 100,
            config => { config.Power = null; config.MaxHealth = 100; });
        BattleTower second = f.Add<BattleTower>("battle", origin + Vector2.Up * 150,
            config => { config.Power = null; config.MaxHealth = 100; });
        Hurt(first, 70); Hurt(second, 60);
        healer._PhysicsProcess(.5);
        Near(first.Health, 38, "Initial lowest-integrity target is selected");
        healer._PhysicsProcess(.5); // First becomes healthier than second.
        Near(first.Health, 46, "Repair remains on first while fractions cross");
        Hurt(second, 20); // A newly more urgent target must not steal the active beam.
        healer._PhysicsProcess(.5);
        Near(first.Health, 54, "Current valid target survives periodic search deadlines");
        Near(second.Health, 20, "Other damaged target waits");
        first.RestoreHealth(100);
        healer._PhysicsProcess(.5);
        Near(second.Health, 28, "Completion releases the lock and selects the next damaged tower");
        healer.Free(); first.Free(); second.Free();
    }
    private static void Hurt(TowerBase tower, float amount) => tower.ApplyDamage(new DamageInfo(amount, DamageType.Kinetic, hullCoefficientOverride: 1));
    private static void Near(float a, float b, string message) => Require(Math.Abs(a - b) < .001, message);
}

