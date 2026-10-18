// Real battlefield Stasis delivery, targeting, timed lifecycle and Cryo coexistence regressions.
using Godot;
using System;
using System.Reflection;
using static AmmoLoopFixture;
public partial class StasisTowerChecks : Node
{
    #region Integration
    // =========================================================
    // Keep progression isolated; use the actual catalog, tower, projectiles and world-owned scheduler.
    public override async void _Ready()
    {
        try
        {
            using (AmmoLoopFixture f = new(this))
            {
                SubsystemBattlefield.DetachSession(f.Main);
                TowerDefinition definition = f.Placement.Catalog.Find("stasis"); definition.Validate();
                Require(f.Hud.GetBuildButton("stasis") != null, "Stasis appears in build tray");
                var config = (StasisTowerConfig)definition.DefaultConfig;
                for (int level = 1; level <= 3; level++)
                {
                    var stats = config.ResolveStats(level);
                    Require(stats.Freeze == new FreezeStats(3 + .25f * (level - 1)) && stats.Weapon?.Type == DamageType.Energy,
                        "Purchased level publishes Energy and Freeze");
                }
                var copy = config.CreateInstanceCopy();
                copy.SetLevelStats(1, copy.Levels[0].Stats with { Freeze = new(float.NaN) });
                Require(copy.ResolveStats(1).Freeze == new FreezeStats(0) && config.ResolveStats(1).Freeze == new FreezeStats(3), "Private config and safety clamp");
                int credits = f.Wallet.GetBalance(f.Wallet.CreditsResource);
                StasisTower paid = f.Purchase<StasisTower>("stasis", false);
                Require(paid.IsBusy && !paid.IsOperational && f.Wallet.GetBalance(f.Wallet.CreditsResource) == credits - 350, "Paid construction gates Stasis");
                ConstructionTestClock.FinishInitial(f.Main); Disable(paid);
                TowerRankController.AwardExperience(paid, 1000);
                for (int level = 2; level <= 3; level++)
                {
                    var preview = paid.ResolveLevel(level);
                    Require(ConstructionTestClock.PurchaseAndFinish(paid, f.Wallet, f.Placement.Settings, level - 1, out string error), error);
                    Require(paid.FinalStats == preview && paid.RankProgress.TotalXp == 1000, "Upgrade matches preview and preserves rank");
                }
                Require(paid.NextUpgrade == null && paid.Ammunition.Inventory.Amount == 600 && paid.MuzzleCount == 1, "Shared max level, ammunition and muzzle");
                paid.Free();
                Vector2 origin = new(2000, 2000);
                Enemy a = f.Target(origin + new Vector2(0, -40)), b = f.Target(origin + new Vector2(0, -100));
                a.ApplyStatus(new(StatusEffectId.Slow, .33f));
                a.ApplyStatus(new(StatusEffectId.Freeze, 0, 3));
                var clock = TimedStatusScheduler.For(f.Spawns.Registry);
                Near(a.EffectiveMoveSpeed, 0, "Freeze gates Cryo"); Near(b.EffectiveMoveSpeed, 100, "Instance isolation");
                Require(clock.ActiveCount == 1 && !a.ApplyStatus(new(StatusEffectId.Freeze, 0, 1)), "Shorter refresh cannot shorten duration");
                clock._PhysicsProcess(1);
                Require(a.ApplyStatus(new(StatusEffectId.Freeze, 0, 4)), "Longer remaining duration wins");
                clock._PhysicsProcess(3);
                Require(a.HasStatus(StatusEffectId.Freeze), "Refresh uses max, not original expiry");
                clock._PhysicsProcess(1);
                Near(a.EffectiveMoveSpeed, 67, "Expiry restores permanent Cryo");
                Require(clock.ActiveCount == 0 && !clock.IsPhysicsProcessing(), "Permanent Slow does not keep scheduler active");
                a.ApplyStatus(new(StatusEffectId.Freeze, 0, 3));
                foreach (var mode in Enum.GetValues<TowerTargeting.Priority>())
                    if (mode is not TowerTargeting.Priority.First and not TowerTargeting.Priority.Last)
                        Require(TowerTargeting.Find(f.Spawns.Registry, origin, 200, mode, StatusEffectId.Freeze) == b, "Frozen exclusion: " + mode);
                b.Definition.StatusImmunities.Add(StatusEffectId.Freeze);
                Require(TowerTargeting.Find(f.Spawns.Registry, origin, 200, TowerTargeting.Priority.Closest, StatusEffectId.Freeze) == null,
                    "No damage-only fallback when frozen/immune");
                b.Definition.StatusImmunities.Clear();
                StasisTower tower = f.Add<StasisTower>("stasis", origin);
                tower.Config.TargetPriority = TowerTargeting.Priority.Closest;
                tower._PhysicsProcess(1);
                FieldInfo target = typeof(ProjectileTurretBase).GetField("_target", BindingFlags.Instance | BindingFlags.NonPublic);
                Require(target.GetValue(tower) == b, "Live Stasis skips closer frozen enemy");
                Node projectiles = f.Main.GetNode("World/Projectiles");
                Require(projectiles.GetChildCount() == 1, "Production Stasis launches one projectile");
                BasicProjectile launched = projectiles.GetChild<BasicProjectile>(0); launched.SetPhysicsProcess(false);
                var launchEffect = (StatusEffectPayload?)typeof(BasicProjectile).GetField("_effect", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(launched);
                Require(launchEffect?.Id == StatusEffectId.Freeze && launchEffect?.DurationSeconds == 3, "Production launch snapshots resolved Freeze");
                tower.Config.SetLevelStats(1, tower.Config.Levels[0].Stats with { Freeze = new(9) });
                launched.GlobalPosition = b.GlobalPosition; launched._PhysicsProcess(.01);
                clock._PhysicsProcess(3);
                Require(!b.HasStatus(StatusEffectId.Freeze), "Changing tower after launch cannot alter captured duration");
                f.ClearProjectiles();
                BasicProjectile shot = new();
                shot.Initialize(b, 650, config.CreateHit(config.ResolveStats(1)), new(StatusEffectId.Freeze, 0, 3));
                f.Main.AddChild(shot); shot.GlobalPosition = b.GlobalPosition; shot.SetPhysicsProcess(false);
                shot._PhysicsProcess(.01); Require(b.HasStatus(StatusEffectId.Freeze), "Actual projectile applies Freeze after Energy hit");
                clock._PhysicsProcess(3); b.ApplyStatus(new(StatusEffectId.Freeze, 0, .04f));
                clock.SetPhysicsProcess(true); GetTree().Paused = true;
                await ToSignal(GetTree().CreateTimer(.08, true), SceneTreeTimer.SignalName.Timeout);
                Require(b.HasStatus(StatusEffectId.Freeze), "Pause retains timed effects");
                GetTree().Paused = false;
                await ToSignal(GetTree().CreateTimer(.12), SceneTreeTimer.SignalName.Timeout);
                Require(!b.HasStatus(StatusEffectId.Freeze) && TowerTargeting.Eligible(b, StatusEffectId.Freeze), "Expiry restores targeting eligibility");
                b.ApplyStatus(new(StatusEffectId.Freeze, 0, 1)); b.Free();
                Require(clock.ActiveCount == 0, "Free unregisters synchronously");
                a.Free(); tower.Free(); f.ClearProjectiles();
                Population(f, clock); Movement(f, clock);
            }
            await JourneyTargetingChecks.Run(this);
            await SubsystemBattlefield.DrainFreedFixtures(this);
            GD.Print("StasisTowerChecks: PASS levels, live targeting, delivery, Cryo coexistence, refresh, pause, lifecycle and 500 timed enemies"); GetTree().Quit();
        }
        catch (Exception error) { GetTree().Paused = false; GD.PushError(error.ToString()); GetTree().Quit(1); }
    }
    // =========================================================
    // Reusable scheduler storage expires a large timed population and returns to idle without route work.
    private static void Population(AmmoLoopFixture f, TimedStatusScheduler clock)
    {
        Enemy[] enemies = new Enemy[500];
        for (int i = 0; i < enemies.Length; i++)
        { enemies[i] = f.Target(new(4000 + i, 4000)); enemies[i].ApplyStatus(new(StatusEffectId.Freeze, 0, 2)); }
        Require(clock.ActiveCount == 500, "One registration per frozen enemy");
        clock._PhysicsProcess(1); clock._PhysicsProcess(1);
        Require(clock.ActiveCount == 0 && !clock.IsPhysicsProcessing(), "All effects expire and clock sleeps");
        foreach (Enemy enemy in enemies) { Near(enemy.EffectiveMoveSpeed, 100, "Population resumed"); enemy.Free(); }
    }
    // =========================================================
    // Real routed movement stops without resetting progress, then resumes at the retained Cryo speed.
    private static void Movement(AmmoLoopFixture f, TimedStatusScheduler clock)
    {
        Enemy enemy = f.Spawns.Spawn(GD.Load<PackedScene>("res://ENEMIES/Types/Enemy001/Enemy001.tscn")); Disable(enemy);
        for (int i = 0; f.Spawns.Paths.PendingCount > 0 && i < 300; i++) f.Spawns.Paths._PhysicsProcess(1.0 / 60);
        enemy.ApplyStatus(new(StatusEffectId.Freeze, 0, 1)); enemy.ApplyStatus(new(StatusEffectId.Slow, .33f));
        Vector2 start = enemy.GlobalPosition; float distance = enemy.RemainingPathDistance;
        int pending = f.Spawns.Paths.PendingCount;
        enemy.Movement._PhysicsProcess(.01);
        Require(enemy.GlobalPosition == start && enemy.RemainingPathDistance == distance, "Freeze preserves route progress");
        clock._PhysicsProcess(1); enemy.Movement._PhysicsProcess(.01);
        Near(start.DistanceTo(enemy.GlobalPosition), .67f, "Real movement resumes with Cryo applied during Freeze");
        Require(f.Spawns.Paths.PendingCount == pending, "Freeze/expiry causes no extra route requests");
        enemy.ApplyStatus(new(StatusEffectId.Freeze, 0, 1)); enemy.TakeDamage(100000);
        Require(clock.ActiveCount == 0, "Death removes active timed entry");
        if (GodotObject.IsInstanceValid(enemy)) enemy.Free();
    }
    // =========================================================
    // Float comparisons tolerate only normal movement arithmetic rounding.
    private static void Near(float actual, float expected, string message) => Require(Math.Abs(actual - expected) < .001f, message);
    #endregion
}
