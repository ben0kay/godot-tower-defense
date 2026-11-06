// Focused CTX1 owner runner: production status clock, damage/XP pipeline, spatial targeting and projectile snapshots.
using Godot;
using System;
using System.Linq;
using static AmmoLoopFixture;

public partial class CorruptorTowerChecks : Node
{
    public override void _Ready()
    {
        try
        {
            using AmmoLoopFixture f = new(this);
            SubsystemBattlefield.DetachSession(f.Main);
            WorldGrid grid = f.Spawns.Paths.Navigation.Grid;
            grid.Level.Passable.Clear(); grid.Level.Impassable.Clear(); grid.Level.Interactive.Clear();
            for (int y = 0; y < grid.Config.HeightCells; y++) for (int x = 0; x < grid.Config.WidthCells; x++)
                grid.Level.Passable.SetCell(new(x, y), 0, new(2, 0));
            grid.RebuildTerrain();
            CorruptorTower a = f.Add<CorruptorTower>("corruptor", new(2000, 2000));
            CorruptorTower b = f.Add<CorruptorTower>("corruptor", new(2200, 2000));
            Require(a.ValidateScene() && a.Levels.Count == 3 && a.Config.Ranks.Count == 5 && a.Power != null
                && a.Ammunition != null && a.FinalStats.Heat.HasValue, "Corruptor reuses tower capabilities, paid levels and earned ranks");
            Require(a.FinalStats.DamagePerShot == 8 && a.FinalStats.Corruption == new DamageOverTimeStats(3, 8), "Launch baseline is 8 + 3/s for 8s");
            Require(TowerUpgradeComparison.Build(a.FinalStats, a.ResolveLevel(2)).Any(r => r.Name == "Total potential" && r.Current == "32"),
                "Inspector compares actual combined potential damage");
            var clock = TimedStatusScheduler.For(f.Spawns.Registry);
            CheckSnapshot(f, a, clock); CheckRefresh(f, a, b, clock); CheckTargets(f, clock);
            CheckAttribution(f, a, b, clock); CheckRemoval(f, clock);
            Require(clock.ActiveCount == 0 && !clock.IsPhysicsProcessing(), "Empty world status clock sleeps");
            GD.Print("PASS: CTX1 hull-only ticks/snapshots, refresh, targeting, XP ownership, pause, weak source and lethal iteration");
            GetTree().Quit();
        }
        catch (Exception error) { GetTree().Paused = false; GD.PushError(error.ToString()); GetTree().Quit(1); }
    }

    private static Enemy Ship(AmmoLoopFixture f, Vector2 position, float health = 100, float shields = 100)
    {
        Enemy enemy = GD.Load<PackedScene>("res://ENEMIES/Types/Enemy001/Enemy001.tscn").Instantiate<Enemy>();
        enemy.Definition = (EnemyDefinition)enemy.Definition.Duplicate(true);
        enemy.Definition.MaxHealth = health; enemy.Definition.MaxShield = shields; enemy.Definition.KillRewardCredits = 10;
        f.Main.GetNode("World/Enemies").AddChild(enemy); enemy.GlobalPosition = position;
        enemy.Initialize(f.Spawns.Registry, f.Spawns.Paths, spawnWithShield: true); Disable(enemy); return enemy;
    }
    private static StatusEffectPayload Dot(DamageSource source, float dps = 3, float seconds = 8) => new(StatusEffectId.Corruption, dps, seconds, source);
    private static void Near(float actual, float expected, string message) => Require(Math.Abs(actual - expected) < .001, message);

    // =========================================================
    // A launched physical projectile retains old values after a config edit; Freeze/Slow do not stop ticks.
    private void CheckSnapshot(AmmoLoopFixture f, CorruptorTower source, TimedStatusScheduler clock)
    {
        Enemy target = Ship(f, new(2000, 1800));
        CorruptorTowerConfig config = (CorruptorTowerConfig)source.Config;
        TowerFinalStats snapshot = source.FinalStats;
        BasicProjectile bullet = new();
        bullet.Initialize(target, snapshot.ProjectileSpeed, source.Config.CreateHit(snapshot, source.DamageSource),
            Dot(source.DamageSource, snapshot.Corruption.Value.DamagePerSecond, snapshot.Corruption.Value.DurationSeconds));
        f.Main.GetNode("World/Projectiles").AddChild(bullet); bullet.LaunchFrom(target.GlobalPosition + Vector2.Down * 80);
        config.Level1DamagePerSecond = 99; bullet._PhysicsProcess(.2);
        Near(target.Health, 92, "Immediate Energy hit bypasses shields for 8 hull HP"); Near(target.CurrentShield, 100, "Initial hit leaves shield intact");
        target.ApplyStatus(new(StatusEffectId.Slow, .33f)); target.ApplyStatus(new(StatusEffectId.Freeze, 0, 2));
        clock._PhysicsProcess(.5); Near(target.Health, 92, "No tick before one second");
        GetTree().Paused = true; clock._PhysicsProcess(50); GetTree().Paused = false;
        Near(target.Health, 92, "Gameplay pause advances neither ticks nor duration");
        clock._PhysicsProcess(.5); Near(target.Health, 89, "Launch snapshot stays at 3 DPS while frozen");
        clock._PhysicsProcess(1);
        Require(!target.HasStatus(StatusEffectId.Freeze) && target.HasStatus(StatusEffectId.Corruption)
            && Math.Abs(target.EffectiveMoveSpeed - target.Definition.MoveSpeed * .67f) < .001,
            "Freeze expiry restores permanent Slow while DoT remains active");
        clock._PhysicsProcess(20); Near(target.Health, 68, "Hitch pays all eight seconds exactly once and expires");
        Near(target.CurrentShield, 100, "Periodic ticks bypass shields");
        clock._PhysicsProcess(20); Near(target.Health, 68, "Expired effect cannot damage again");
        config.Level1DamagePerSecond = 3; target.Free();
    }
    private static void CheckRefresh(AmmoLoopFixture f, CorruptorTower a, CorruptorTower b, TimedStatusScheduler clock)
    {
        Enemy target = Ship(f, new(2000, 1800));
        target.ApplyStatus(Dot(a.DamageSource, 3, 8)); clock._PhysicsProcess(.5);
        Require(target.ApplyStatus(Dot(b.DamageSource, 2, 2)), "Weaker reapplication successfully refreshes and transfers ownership");
        clock._PhysicsProcess(.5); Near(target.Health, 97, "Refresh preserves partial tick progress and stronger DPS");
        clock._PhysicsProcess(1.5); Near(target.Health, 92.5f, "New duration replaces old remainder; final half-second pays once without stacking");
        Require(!target.HasStatus(StatusEffectId.Corruption), "Reapplied duration expires"); target.Free();
    }
    private static void CheckTargets(AmmoLoopFixture f, TimedStatusScheduler clock)
    {
        Vector2 origin = new(2000, 2000);
        Enemy near = Ship(f, origin + Vector2.Up * 50), fresh = Ship(f, origin + Vector2.Up * 150);
        near.ApplyStatus(Dot(null));
        foreach (var mode in new[] { TowerTargeting.Priority.Closest, TowerTargeting.Priority.Farthest, TowerTargeting.Priority.Strongest, TowerTargeting.Priority.Weakest })
            Require(TowerTargeting.Find(f.Spawns.Registry, origin, 250, mode, StatusEffectId.Corruption, affectedFallback: true) == fresh,
                "Fresh tier wins before selected within-tier score");
        fresh.ApplyStatus(Dot(null));
        Require(TowerTargeting.Find(f.Spawns.Registry, origin, 250, TowerTargeting.Priority.Closest, StatusEffectId.Corruption, affectedFallback: true) == near,
            "Affected fallback honors Closest");
        Require(TowerTargeting.Find(f.Spawns.Registry, origin, 250, TowerTargeting.Priority.Farthest, StatusEffectId.Corruption, affectedFallback: true) == fresh,
            "Affected fallback honors Farthest");
        near.Definition.StatusImmunities.Add(StatusEffectId.Corruption);
        Require(!near.ApplyStatus(Dot(null)), "Existing StatusImmunities rejects Corruption application");
        Require(TowerTargeting.Find(f.Spawns.Registry, origin, 250, TowerTargeting.Priority.Closest, StatusEffectId.Corruption, affectedFallback: true) == fresh,
            "Immune enemies are excluded rather than wasting Corruptor shots");
        fresh.ApplyStatus(new(StatusEffectId.Slow, .33f)); near.ApplyStatus(new(StatusEffectId.Slow, .33f));
        Require(TowerTargeting.Find(f.Spawns.Registry, origin, 250, TowerTargeting.Priority.Closest, StatusEffectId.Slow) == null,
            "Cryo retains strict already-slowed exclusion without fallback");
        near.Free(); fresh.Free();
    }
    // =========================================================
    // Status application awards nothing; real periodic/direct killing hits own normal XP and single wallet rewards.
    private static void CheckAttribution(AmmoLoopFixture f, CorruptorTower a, CorruptorTower b, TimedStatusScheduler clock)
    {
        Enemy target = Ship(f, new(2000, 1800), 6);
        double aXp = a.RankProgress.TotalXp, bXp = b.RankProgress.TotalXp;
        int kills = f.Spawns.Registry.KilledCount, credits = f.Wallet.GetBalance(f.Wallet.CreditsResource);
        target.ApplyStatus(Dot(a.DamageSource)); target.ApplyStatus(Dot(b.DamageSource, 2));
        Require(a.RankProgress.TotalXp == aXp && b.RankProgress.TotalXp == bXp, "Applying status itself grants no XP");
        clock._PhysicsProcess(2);
        Require(!target.IsAlive && target.KillingSource == b.DamageSource && b.RankProgress.TotalXp > bXp
            && a.RankProgress.TotalXp == aXp, "Latest successful source receives DoT damage and kill XP");
        double expectedXp = 10 * TowerRankController.SafeMultiplier(b.ExperienceRules.KillXpMultiplier)
            + 10 * TowerRankController.SafeMultiplier(b.ExperienceRules.DamageXpMultiplier) * 6 / 106;
        Require(Math.Abs(b.RankProgress.TotalXp - bXp - expectedXp) < .001, "Periodic kill awards the normal finite hull-damage share plus kill XP");
        clock._PhysicsProcess(10);
        Require(f.Spawns.Registry.KilledCount == kills + 1 && f.Wallet.GetBalance(f.Wallet.CreditsResource) == credits + 10,
            "DoT lethal lifecycle grants kill counts/Credits exactly once");
        target = Ship(f, new(2000, 1800), 6); target.ApplyStatus(Dot(b.DamageSource));
        target.ApplyDamage(new(10, DamageType.Energy, hullCoefficientOverride: 1, shieldPenetration: 1, source: a.DamageSource));
        Require(target.KillingSource == a.DamageSource, "Another weapon's actual killing blow supersedes pending DoT");
    }
    private static void CheckRemoval(AmmoLoopFixture f, TimedStatusScheduler clock)
    {
        CorruptorTower source = f.Add<CorruptorTower>("corruptor", new(2300, 2000)); DamageSource old = source.DamageSource;
        Enemy target = Ship(f, new(2000, 1800), 3); target.ApplyStatus(Dot(old)); source.Free();
        CorruptorTower replacement = f.Add<CorruptorTower>("corruptor", new(2300, 2000));
        clock._PhysicsProcess(1);
        Require(!target.IsAlive && target.KillingSource == old && !old.TryGetTower(out _) && replacement.RankProgress.TotalXp == 0,
            "DoT continues after source removal without crediting a replacement"); replacement.Free();
        Enemy first = Ship(f, new(2000, 1800), 1), removed = Ship(f, new(2050, 1800), 1), last = Ship(f, new(2100, 1800), 1);
        first.ApplyStatus(Dot(null)); removed.ApplyStatus(Dot(null)); last.ApplyStatus(Dot(null));
        first.KilledBeforeRemoval += _ => removed.Free();
        clock._PhysicsProcess(1);
        Require(!first.IsAlive && !last.IsAlive && clock.ActiveCount == 0,
            "Lethal tick plus removal of a different scheduled enemy does not double-remove or skip another enemy");
        Enemy trigger = Ship(f, new(2000, 1800), 1), recycled = Ship(f, new(2100, 1800), 10);
        trigger.ApplyStatus(Dot(null)); recycled.ApplyStatus(Dot(null));
        trigger.KilledBeforeRemoval += _ =>
        {
            recycled.Initialize(f.Spawns.Registry, f.Spawns.Paths, spawnWithShield: true);
            recycled.ApplyStatus(Dot(null)); Disable(recycled);
        };
        clock._PhysicsProcess(1); Near(recycled.Health, 10, "Reinitialized enemy's new status waits until the next scheduler step");
        clock._PhysicsProcess(1); Near(recycled.Health, 7, "New status generation ticks once without stale history"); recycled.Free();
        Enemy arrived = Ship(f, new(2000, 1800)); arrived.ApplyStatus(Dot(null)); arrived.Arrive();
        Require(clock.ActiveCount == 0, "Arrival clears timed effects immediately");
    }
}
