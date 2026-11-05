// RT0 narrow production-registry fixture: friendly scoring, work/disable filters, range and lock lifetime.
using Godot;
using System;
using System.Collections.Generic;
using static AmmoLoopFixture;

public partial class FriendlyTowerTargetingChecks : Node
{
    // =========================================================
    // Optional F6 fixture, not part of a large suite; never edits catalog content or writes profile saves.
    public override async void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
        try
        {
            using (AmmoLoopFixture f = new(this))
            {
                SubsystemBattlefield.DetachSession(f.Main);
                Selection(f); Lifecycle(f);
                using AmmoLoopFixture other = new(this);
                SubsystemBattlefield.DetachSession(other.Main);
                BattleTower healer = Add(f, new(2000, 2000)), foreign = Add(other, new(2000, 2000));
                Hurt(foreign, 1);
                TowerRegistry local = f.Main.GetNode<TowerRegistry>("World/TowerRegistry");
                TowerRegistry remote = other.Main.GetNode<TowerRegistry>("World/TowerRegistry");
                List<TowerBase> scratch = new();
                Require(!FriendlyTowerTargeting.Valid(local, healer, foreign, 100)
                    && FriendlyTowerTargeting.Find(remote, healer, 100, scratch) == null, "Cross-world targets and healers cannot qualify");
                healer.Free(); foreign.Free();
            }
            await SubsystemBattlefield.DrainFreedFixtures(this);
            GD.Print("PASS: RT0 friendly integrity, stable ties, root range, work/disable filters and lock lifetime");
            GetTree().Quit();
        }
        catch (Exception error) { GetTree().Paused = false; GD.PushError(error.ToString()); GetTree().Quit(1); }
    }
    // =========================================================
    // Known neutral hulls avoid coupling selection checks to damage coefficients, ranks or power balance.
    private static BattleTower Add(AmmoLoopFixture f, Vector2 position, float health = 100) =>
        f.Add<BattleTower>("battle", position, config => { config.Power = null; config.MaxHealth = health; });
    private static void Hurt(TowerBase tower, float amount) => tower.ApplyDamage(new DamageInfo(amount, DamageType.Kinetic, hullCoefficientOverride: 1));
    // =========================================================
    // Integrity is a fraction, not absolute missing HP or closest distance; exact ties use registration order.
    private static void Selection(AmmoLoopFixture f)
    {
        TowerRegistry registry = f.Main.GetNode<TowerRegistry>("World/TowerRegistry");
        List<TowerBase> scratch = new(8); Vector2 origin = new(2000, 2000);
        BattleTower healer = Add(f, origin), first = Add(f, origin + new Vector2(100, 0));
        BattleTower second = Add(f, origin + new Vector2(-100, 0), 200), full = Add(f, origin + Vector2.Up);
        Hurt(healer, 99); Hurt(first, 50); Hurt(second, 100);
        Require(FriendlyTowerTargeting.Find(registry, healer, 100, scratch) == first && scratch.Count == 0,
            "Self/full excluded; inclusive 1x1 boundary and equal integrity choose earliest registration");
        Hurt(second, 10);
        Require(FriendlyTowerTargeting.Find(registry, healer, 100, scratch) == second, "Lower fraction wins despite larger absolute hull");
        TowerDisableHandle emp = second.ApplyDisable(TowerDisableReason.EMP);
        TowerDisableHandle heat = second.ApplyDisable(TowerDisableReason.Overheated);
        TowerDisableHandle power = second.ApplyDisable(TowerDisableReason.NoPower);
        Require(FriendlyTowerTargeting.Valid(registry, healer, second, 100), "Disabled target still qualifies for repair");
        TowerDisableHandle stop = healer.ApplyDisable(TowerDisableReason.EMP);
        Require(FriendlyTowerTargeting.Find(registry, healer, 100, scratch) == null
            && !FriendlyTowerTargeting.Valid(registry, healer, second, 100), "Disabled healer cannot search or retain a repair lock");
        healer.RemoveDisable(stop); second.RemoveDisable(emp); second.RemoveDisable(heat); second.RemoveDisable(power);
        second.GlobalPosition += new Vector2(-1, 0); second.ForceUpdateTransform();
        Require(!FriendlyTowerTargeting.Valid(registry, healer, second, 100)
            && FriendlyTowerTargeting.Find(registry, healer, 100, scratch) == first, "Outside root radius drops lock despite footprint overlap");
        first.RestoreHealth(100);
        Require(!FriendlyTowerTargeting.Valid(registry, healer, first, 100), "Fully repaired target invalidates lock immediately");
        first.GlobalPosition = origin; first.ForceUpdateTransform(); Hurt(first, 1);
        Require(FriendlyTowerTargeting.Find(registry, healer, 0, scratch) == first, "Zero radius only includes coincident damaged roots");
        Require(FriendlyTowerTargeting.Find(registry, healer, float.NaN, scratch) == null
            && FriendlyTowerTargeting.Find(registry, healer, -1, scratch) == null
            && !FriendlyTowerTargeting.Valid(null, healer, first, 100), "Malformed queries are safely empty");
        healer.Free(); first.Free(); second.Free(); full.Free();
    }
    // =========================================================
    // Real work/restoration seams cover multicell root semantics, construction, upgrade and immediate removal.
    private static void Lifecycle(AmmoLoopFixture f)
    {
        TowerRegistry registry = f.Main.GetNode<TowerRegistry>("World/TowerRegistry");
        List<TowerBase> scratch = new(8); Vector2 origin = new(4000, 4000);
        BattleTower healer = Add(f, origin);
        AmmunitionFactory wide = f.Add<AmmunitionFactory>("ammo_factory", origin + new Vector2(100, 0));
        Hurt(wide, 20);
        Require(wide.Footprint.X > 1 || wide.Footprint.Y > 1, "Fixture has a multicell footprint");
        Require(FriendlyTowerTargeting.Find(registry, healer, 100, scratch) == wide
            && FriendlyTowerTargeting.Find(registry, healer, 99, scratch) == null, "Multicell root, not closest footprint edge, defines range");
        wide.Free();
        TowerDefinition definition = f.Placement.Catalog.Find("battle");
        BattleTower work = definition.Scene.Instantiate<BattleTower>(); work.InitializeDefinition(definition);
        work.AuthoredConfig.Power = null;
        work.StartConstruction(f.Main.GetNode<ConstructionWorkManager>("World/ConstructionWork"));
        f.Towers.AddChild(work); work.GlobalPosition = origin + Vector2.Up; Disable(work); Hurt(work, 10);
        Require(!FriendlyTowerTargeting.Valid(registry, healer, work, 100), "Unfinished initial construction cannot receive repairs");
        ConstructionTestClock.FinishInitial(f.Main);
        Require(FriendlyTowerTargeting.Find(registry, healer, 100, scratch) == work, "Work completion makes damaged hull eligible");
        Require(work.BeginUpgrade(1, work.NextUpgrade, work.ResolveLevel(2)), "Fixture starts real timed upgrade");
        Require(!FriendlyTowerTargeting.Valid(registry, healer, work, 100), "Timed upgrade invalidates lock");
        ConstructionTestClock.FinishWork(f.Main);
        Require(FriendlyTowerTargeting.Valid(registry, healer, work, 100), "Upgrade completion preserves damaged repair eligibility");
        f.Towers.RemoveChild(work);
        Require(!FriendlyTowerTargeting.Valid(registry, healer, work, 100), "Detached target loses membership");
        work.Free();
        BattleTower dead = Add(f, origin + Vector2.Down); Hurt(dead, 10000);
        Require(!FriendlyTowerTargeting.Valid(registry, healer, dead, 100), "Destroyed target excluded before native deletion"); dead.Free();
        BattleTower queued = Add(f, origin + Vector2.Right); Hurt(queued, 1); queued.QueueFree();
        Require(FriendlyTowerTargeting.Find(registry, healer, 100, scratch) == null, "Queued target excluded immediately"); queued.Free();
        healer.Free();
    }
}
