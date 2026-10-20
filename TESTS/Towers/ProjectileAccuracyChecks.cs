// PA1-PA3: immutable handling/rank/research stats, deterministic launch spread, real swept hits and splash destinations.
using Godot;
using System;
using System.Diagnostics;
using System.Linq;
using static AmmoLoopFixture;

public partial class ProjectileAccuracyChecks : Node
{
    #region Runner and Stats
    // =========================================================
    // Isolated production services; no profiles, source waves or research purchases are modified.
    public override async void _Ready()
    {
        try
        {
            using (AmmoLoopFixture f = new(this))
            {
                SubsystemBattlefield.DetachSession(f.Main);
                Stats(f); Straight(f); SweptAndStatus(f); Splash(f); Homing(f); SpatialBudget(f);
            }
            await SubsystemBattlefield.DrainFreedFixtures(this);
            GD.Print("PASS: PA1-PA3 handling/rank/research/preview, launch spread, straight lifetime, swept first enemy, statuses, homing, splash and spatial budget");
            GetTree().Quit();
        }
        catch (Exception error) { GD.PushError(error.ToString()); GetTree().Paused = false; GetTree().Quit(1); }
    }
    // =========================================================
    // Every existing absolute row and selected total rank participates; no beam/radial handling or invented research node.
    private static void Stats(AmmoLoopFixture f)
    {
        foreach (string id in new[] { "battle", "machine_gun", "sniper", "blaster_cannon", "rocket", "cryo", "stasis" })
        {
            var config = f.Placement.Catalog.Find(id).DefaultConfig;
            for (int level = 1; level <= config.Levels.Count; level++)
            {
                var source = config.Levels[level - 1].Stats.Handling.Value;
                Require(source.Guidance == ProjectileGuidance.Straight && source.SpreadDegrees > 0, id + " authors absolute straight/spread rows");
                var final = TowerStatPipeline.Resolve(config, level, rank: config.Ranks[4], research: new(0, .36));
                Near(final.Handling.Value.SpreadDegrees, source.SpreadDegrees * .8 * .64, "Selected total rank and captured research reductions multiply once");
            }
        }
        foreach (string id in new[] { "laser", "shockwave" })
            Require(TowerStatPipeline.Resolve(f.Placement.Catalog.Find(id).DefaultConfig, 1, research: new(0, 1, true)).Handling == null,
                "Nonprojectile delivery keeps no artificial accuracy capability");
        var battle = f.Placement.Catalog.Find("battle").DefaultConfig;
        Require(TowerStatPipeline.Resolve(battle, 1, research: new(5)).DamagePerShot == 32, "Existing Battle Weapons I damage research still applies");
        Require(TowerStatPipeline.Resolve(battle, 1, research: new(0, 0, true)).Handling.Value.Guidance == ProjectileGuidance.Homing,
            "Explicit captured future homing seam has no default unlock");
        Require(TowerStatPipeline.Resolve(f.Placement.Catalog.Find("rocket").DefaultConfig, 1, research: new(0, 0, true)).Handling.Value.Guidance == ProjectileGuidance.Straight,
            "Rocket remains explicitly unguided even under a global future unlock");
        TowerStatBlock baseStats = battle.Levels[0].Stats;
        foreach (float spread in new[] { float.NaN, float.PositiveInfinity, -10f, 0f, 999f })
        {
            var result = TowerStatPipeline.Resolve(new TowerStatContext(baseStats with { Handling = new(spread, (ProjectileGuidance)999) }));
            Require(float.IsFinite(result.Handling.Value.SpreadDegrees) && result.Handling.Value.SpreadDegrees >= 0
                && result.Handling.Value.SpreadDegrees <= 180 && result.Handling.Value.Guidance == ProjectileGuidance.Straight, "Finite handling bounds");
        }
        BattleTower tower = f.Add<BattleTower>("battle", new(2000, 2200)); tower.ReceiveExperience(15000);
        var preview = tower.ResolveLevel(2);
        Near(preview.Handling.Value.SpreadDegrees, 2 * .8, "Upgrade preview retains actual current rank");
        Require(TowerUpgradeComparison.Build(tower.FinalStats, preview).Any(r => r.Name == "Spread (±deg)" && r.Tone == TowerUpgradeGain.Better),
            "Inspector shows lower spread as an improvement");
        tower.Free();
        Near(Mathf.RadToDeg(ProjectileLaunch.Direction(Vector2.Right, 10, -1).Angle()), -10, "Negative endpoint sample");
        Near(Mathf.RadToDeg(ProjectileLaunch.Direction(Vector2.Right, 10, 1).Angle()), 10, "Positive endpoint sample");
        Require(ProjectileLaunch.Direction(Vector2.Right, 0, 1) == Vector2.Right, "Zero spread is exactly precise");
    }
    #endregion

    #region Straight and Contact
    // =========================================================
    // Physical launch helper supplies deterministic sample while using the same production initialization and registry.
    private static BasicProjectile Launch(AmmoLoopFixture f, Enemy target, Vector2 from, Vector2 forward, float speed = 1000,
        ProjectileHandlingStats? handling = null, StatusEffectPayload? effect = null)
    {
        BasicProjectile shot = new(); shot.Initialize(target, speed, new DamageInfo(10, DamageType.Energy), effect, handling: handling);
        f.Main.GetNode("World/Projectiles").AddChild(shot); Disable(shot); shot.LaunchFrom(from, forward, 1); return shot;
    }
    // =========================================================
    // Moving or freed original targets cannot steer/delete a straight round; pause, misses and expiry remain real.
    private static void Straight(AmmoLoopFixture f)
    {
        Enemy target = f.Target(new(2200, 2000));
        BasicProjectile shot = Launch(f, target, new(2000, 2000), Vector2.Right, handling: new(10));
        Vector2 heading = shot.TravelDirection; target.GlobalPosition += new Vector2(0, 300);
        shot._PhysicsProcess(.1);
        Require(shot.TravelDirection == heading && shot.GlobalPosition.Y > 2000 && !shot.IsQueuedForDeletion(), "Launch spread changes real trajectory and never steers");
        target.Free(); shot._PhysicsProcess(.1); Require(!shot.IsQueuedForDeletion(), "Original target removal does not delete straight bullet");
        Vector2 before = shot.GlobalPosition; f.Main.GetTree().Paused = true; shot._PhysicsProcess(1); f.Main.GetTree().Paused = false;
        Require(shot.GlobalPosition == before, "Pause freezes actual travel");
        shot._PhysicsProcess(10); Require(shot.IsQueuedForDeletion(), "Miss expires with bounded travel"); f.ClearProjectiles();
        target = f.Target(new(3000, 2000)); shot = Launch(f, target, new(2000, 2000), Vector2.Right, 100);
        float hp = target.Health; shot._PhysicsProcess(100);
        Require(target.Health == hp && shot.GlobalPosition.X <= 2500 && shot.IsQueuedForDeletion(), "Large delta clips to five-second lifetime before collision");
        target.Free(); f.ClearProjectiles();
    }
    // =========================================================
    // Earliest circle intersection wins, even for a hidden/unselected recipient; status applies only on actual impact.
    private static void SweptAndStatus(AmmoLoopFixture f)
    {
        Enemy intended = f.Target(new(2300, 2000)), blocker = f.Target(new(2100, 2000));
        blocker.Initialize(f.Spawns.Registry, f.Spawns.Paths, spawnWithStealth: true); Disable(blocker);
        float a = intended.Health, b = blocker.Health;
        BasicProjectile shot = Launch(f, intended, new(2000, 2000), Vector2.Right, 10000, effect: new(StatusEffectId.Slow, .33f));
        shot._PhysicsProcess(.1); shot._PhysicsProcess(.1);
        Require(blocker.Health < b && intended.Health == a && blocker.HasStatus(StatusEffectId.Slow) && shot.IsQueuedForDeletion(),
            "Swept stray hits first hidden physical recipient once, with actual status payload");
        intended.Free(); blocker.Free(); f.ClearProjectiles();
        intended = f.Target(new(2200, 2000)); shot = Launch(f, intended, new(2000, 2000), Vector2.Right, handling: new(45), effect: new(StatusEffectId.Freeze, 0, 3));
        shot._PhysicsProcess(.3); Require(!intended.HasStatus(StatusEffectId.Freeze), "Miss does not apply freeze magically");
        intended.Free(); f.ClearProjectiles();
        Enemy first = f.Target(new(2100, 2000)), second = f.Target(new(2100, 2000));
        Require(ProjectileEnemyContact.First(f.Spawns.Registry, new(2000, 2000), new(2300, 2000), out _) == first, "Exact collision ties retain registration order");
        first.Free(); second.Free();
        Require(ProjectileEnemyContact.SegmentCircle(new(0, 10), new(100, 10), new(50, 0), 10, out double entry) && entry == .5,
            "Exact tangent is a physical contact");
    }
    #endregion

    #region Splash Homing and Budget
    // =========================================================
    // Splash deviation changes the impact center at fixed original range; zero spread preserves Rocket's old target point.
    private static void Splash(AmmoLoopFixture f)
    {
        Enemy target = f.Target(new(2200, 2000)); Vector2 from = new(2000, 2000), original = target.GlobalPosition;
        SplashProjectile shot = new(); shot.Initialize(target, f.Spawns.Registry, original, 1000, new(10, DamageType.Explosive), new(10, 1), handling: new(20));
        f.Main.GetNode("World/Projectiles").AddChild(shot); shot.LaunchFrom(from, 1); Disable(shot);
        Vector2 point = shot.ImpactPoint;
        Near(point.DistanceTo(from), original.DistanceTo(from), "Splash keeps original flight distance");
        Require(point.Y > original.Y, "Splash spread moves the actual impact center");
        target.GlobalPosition += new Vector2(0, 300); shot._PhysicsProcess(1);
        Require(shot.HasDetonated && shot.GlobalPosition.IsEqualApprox(point) && target.Health == 10000, "Straight splash detonates once at the missed fixed point");
        shot.Free();
        shot = new(); shot.Initialize(target, f.Spawns.Registry, original, 1000, new(10, DamageType.Explosive), new(10, 1));
        f.Main.GetNode("World/Projectiles").AddChild(shot); shot.LaunchFrom(from);
        Require(shot.ImpactPoint == original && shot.Handling.Guidance == ProjectileGuidance.Straight, "Zero-spread straight splash preserves exact original target point");
        shot.Free(); target.Free();
    }
    // =========================================================
    // Guided behavior remains explicitly selectable and uses swept travel contact/lost-target cleanup.
    private static void Homing(AmmoLoopFixture f)
    {
        Enemy target = f.Target(new(2200, 2000));
        BasicProjectile shot = Launch(f, target, new(2000, 2000), Vector2.Right, handling: new(10, ProjectileGuidance.Homing));
        target.GlobalPosition = new(2000, 2200); shot._PhysicsProcess(.01);
        Require(shot.TravelDirection.Dot(Vector2.Down) > .999, "Explicit homing can steer after initial spread");
        target.Free(); shot._PhysicsProcess(.01); Require(shot.IsQueuedForDeletion(), "Homing target loss keeps its safe cleanup policy"); f.ClearProjectiles();
    }
    // =========================================================
    // Spatial queries touch nearby candidates only; warmed collision visitors use no result collections; runtime wrapper overhead is budgeted.
    private static void SpatialBudget(AmmoLoopFixture f)
    {
        Enemy near = f.Target(new(2100, 2000));
        var distant = Enumerable.Range(0, 200).Select(i => f.Target(new(10000 + i * 100, 10000))).ToArray();
        for (int i = 0; i < 100; i++) ProjectileEnemyContact.First(f.Spawns.Registry, new(2000, 2000), new(2200, 2000), out _);
        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 1000; i++) ProjectileEnemyContact.First(f.Spawns.Registry, new(2000, 2000), new(2200, 2000), out _);
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        GD.Print($"Sweep allocation bytes={allocated}; candidates={f.Spawns.Registry.LastQueryCandidates}");
        Require(allocated <= 128 * 1000 && f.Spawns.Registry.LastQueryCandidates < 10, "Warmed swept collision has bounded runtime overhead and avoids full-enemy candidate scans");
        foreach (Enemy enemy in distant) enemy.Free(); near.Free();
    }
    // =========================================================
    // Numeric comparisons use tolerance for deterministic float geometry, never random hit-rate assertions.
    private static void Near(double actual, double expected, string message) => Require(Math.Abs(actual - expected) < .001, message);
    #endregion
}
