// Cryo production integration plus status isolation, attribution, movement and allocation regressions.
using Godot;
using System;
using System.Diagnostics;
using System.Linq;
using static AmmoLoopFixture;

public partial class CryoTowerChecks : Node
{
    // =========================================================
    // Run the focused feature alongside existing single/multi-barrel weapons on an actual battlefield.
    public override async void _Ready()
    {
        try
        {
            using (AmmoLoopFixture f = new(this))
            {
                SubsystemBattlefield.DetachSession(f.Main);
                Run(f);
                MultiBarrelTowerChecks.Run(f.Main, f.Spawns);
            }
            await SubsystemBattlefield.DrainFreedFixtures(this);
            GD.Print("PASS: Cryo CT1–CT3 status, targeting, projectile, paid levels/ranks, HUD, movement and 500-enemy checks");
            GetTree().Quit();
        }
        catch (Exception error) { GD.PushError(error.ToString()); GetTree().Paused = false; GetTree().Quit(1); }
    }

    // =========================================================
    // Also callable from the main integration runner without introducing a second world or status manager.
    internal static void Run(AmmoLoopFixture f)
    {
        Levels(f); Statuses(f); Projectiles(f); Targeting(f); Movement(f); Lava(f); Population(f);
    }

    // =========================================================
    // Real build button, payment, construction, preview, purchased levels, rank and private authored rows.
    private static void Levels(AmmoLoopFixture f)
    {
        TowerDefinition definition = f.Placement.Catalog.Find("cryo"); definition.Validate();
        Require(definition.Category == TowerDefinition.TowerCategory.Offensive && f.Hud.GetBuildButton("cryo") != null,
            "Cryo is a catalog-driven Offensive HUD choice");
        var authored = (CryoTowerConfig)definition.DefaultConfig;
        int[] damage = { 10, 11, 12 }, costs = { 250, 175, 250 };
        float[] rate = { .5f, .52f, .54f }, range = { 239, 248, 258 };
        for (int i = 0; i < 3; i++)
        {
            TowerFinalStats s = authored.ResolveStats(i + 1);
            Require(s.DamagePerShot == damage[i] && s.ShotsPerSecond == rate[i] && s.RangePixels == range[i]
                && s.ProjectileSpeed == 650 && s.Slow == new SlowStats(.33f) && s.ShieldPenetration == 0
                && s.Weapon.Value.Type == DamageType.Energy && s.Ammunition == new AmmunitionStats(600, 1)
                && authored.Levels[i].Costs[0].Amount == costs[i], "Absolute Cryo level " + (i + 1));
        }
        var copy = authored.CreateInstanceCopy();
        var third = copy.ResolveStats(3);
        copy.SetLevelStats(2, copy.Levels[1].Stats with { DamagePerShot = 999, Slow = new(.5f) });
        Require(copy.ResolveStats(3) == third && authored.ResolveStats(2).DamagePerShot == 11, "Level rows and placed configs are independent");
        TowerStatTrace trace = new(); TowerStatPipeline.Resolve(authored, 1, trace);
        Require(trace.Steps.Count == 8 && trace.Steps.All(step => step.Stats.Slow == new SlowStats(.33f)), "Eight stages preserve Slow capability");
        Require(copy.DescribeChanges(copy.ResolveStats(1), copy.ResolveStats(2)).Contains("33% → 50%"), "Slow preview uses effective stats");
        copy.SetLevelStats(1, copy.Levels[0].Stats with { Slow = new(float.NaN) });
        Require(copy.ResolveStats(1).Slow == new SlowStats(0), "Nonfinite Slow is clamped safely");
        int before = f.Wallet.GetBalance(f.Wallet.CreditsResource);
        CryoTower tower = f.Purchase<CryoTower>("cryo", false);
        Require(tower.IsBusy && !tower.IsOperational && !tower.IsPhysicsProcessing()
            && f.Wallet.GetBalance(f.Wallet.CreditsResource) == before - 250, "Paid Cryo construction gates weapon and deducts once");
        ConstructionTestClock.FinishInitial(f.Main); Disable(tower);
        Require(tower.Ammunition.Inventory.Amount == 600 && tower.MuzzleCount == 1 && tower.Config != authored,
            "Finished Cryo uses shared ammo and one muzzle");
        TowerRankController.AwardExperience(tower, 1000);
        double xp = tower.RankProgress.TotalXp;
        Require(tower.CurrentRank.Rank == TowerRank.Veteran && tower.FinalStats.Slow == new SlowStats(.33f), "Rank does not amplify permanent Slow");
        for (int level = 2; level <= 3; level++)
        {
            var preview = tower.ResolveLevel(level);
            Require(ConstructionTestClock.PurchaseAndFinish(tower, f.Wallet, f.Placement.Settings, level - 1, out string error), error);
            Require(tower.FinalStats == preview && tower.RankProgress.TotalXp == xp, "Paid preview equals final and XP survives upgrade");
        }
        Require(tower.NextUpgrade == null && f.Wallet.GetBalance(f.Wallet.CreditsResource) == before - 675,
            "Max level and complete build/upgrade costs");
        Require(authored.Ranks.Select(r => r.TotalXp).SequenceEqual(new double[] { 0, 1000, 3000, 7000, 15000 }), "Named rank thresholds");
        tower.Free(); f.ClearProjectiles();
    }

    // =========================================================
    // Definition data is shared safely; strongest-wins updates only the struck living instance.
    private static void Statuses(AmmoLoopFixture f)
    {
        Enemy a = f.Target(new(2000, 2000)), b = f.Target(new(2050, 2000));
        b.Definition = a.Definition;
        float original = a.Definition.MoveSpeed;
        Near(a.EffectiveMoveSpeed, 100, "Baseline speed");
        Require(a.ApplyStatus(new(StatusEffectId.Slow, .33f)), "First Slow changes state");
        Near(a.EffectiveMoveSpeed, 67, "33% slow produces 67 px/s");
        Require(!a.ApplyStatus(new(StatusEffectId.Slow, .33f)) && !a.ApplyStatus(new(StatusEffectId.Slow, .2f)), "Equal/weaker applications do not stack");
        Near(a.EffectiveMoveSpeed, 67, "Repeated hit keeps speed");
        Require(a.ApplyStatus(new(StatusEffectId.Slow, .5f)), "Stronger slow replaces weaker");
        Near(a.EffectiveMoveSpeed, 50, "Strongest slow wins");
        Require(!b.HasStatus(StatusEffectId.Slow) && b.EffectiveMoveSpeed == original && a.Definition.MoveSpeed == original, "Shared definition and other instance unchanged");
        b.Definition = (EnemyDefinition)b.Definition.Duplicate(true); b.Definition.StatusImmunities.Add(StatusEffectId.Slow);
        Require(!b.ApplyStatus(new(StatusEffectId.Slow, .33f)) && !b.HasStatus(StatusEffectId.Slow), "Inspector immunity rejects Slow");
        float hp = b.Health; b.ApplyDamage(new(10, DamageType.Energy)); Require(b.Health < hp, "Immunity does not block damage");
        Sprite2D sprite = a.GetNode<Sprite2D>("Sprite");
        Require(sprite.SelfModulate != Colors.White && sprite.Modulate == Colors.White, "Slow feedback uses independent sprite tint");
        a.Arrive(); Require(!a.HasStatus(StatusEffectId.Slow), "Arrival clears status");
        a.Free(); b.Free();
    }

    // =========================================================
    // Use the real shared projectile: one Energy hit, shields/overrides, XP and immutable delayed effects.
    private static void Projectiles(AmmoLoopFixture f)
    {
        CryoTower tower = f.Add<CryoTower>("cryo", new(2000, 2064));
        Enemy enemy = f.Target(new(2000, 2000));
        enemy.Definition.MaxHealth = 60; enemy.Definition.MaxShield = 100; enemy.Definition.KillRewardCredits = 3;
        enemy.Initialize(f.Spawns.Registry, f.Spawns.Paths, true); Disable(enemy);
        DamageInfo damage = tower.Config.CreateHit(tower.FinalStats, tower.DamageSource);
        DamageResult expected = DamageResolver.Resolve(damage, enemy.CurrentShield, enemy.Health);
        BasicProjectile shot = new(); shot.Initialize(enemy, 650, damage, new(StatusEffectId.Slow, .33f));
        f.Main.GetNode("World/Projectiles").AddChild(shot); shot.GlobalPosition = enemy.GlobalPosition;
        double oldXp = tower.RankProgress.TotalXp;
        shot._PhysicsProcess(.01); shot._PhysicsProcess(.01);
        Near(enemy.CurrentShield, expected.Shield, "Single Energy shield hit"); Near(enemy.Health, expected.Health, "Energy hull resolution");
        Require(enemy.HasStatus(StatusEffectId.Slow), "Surviving shields do not block Slow");
        double actual = 160 - enemy.Health - enemy.CurrentShield;
        Near((float)(tower.RankProgress.TotalXp - oldXp), (float)(3 * tower.ExperienceRules.DamageXpMultiplier * actual / 160), "Only actual damage awards XP");
        f.ClearProjectiles();
        int credits = f.Wallet.GetBalance(f.Wallet.CreditsResource);
        var lethal = new BasicProjectile(); lethal.Initialize(enemy, 650, new DamageInfo(1000, DamageType.Energy, source: tower.DamageSource), new(StatusEffectId.Slow, .33f));
        f.Main.GetNode("World/Projectiles").AddChild(lethal); lethal.GlobalPosition = enemy.GlobalPosition;
        lethal._PhysicsProcess(.01); double afterKill = tower.RankProgress.TotalXp; lethal._PhysicsProcess(.01);
        Require(!enemy.IsAlive && !enemy.HasStatus(StatusEffectId.Slow) && tower.RankProgress.TotalXp == afterKill
            && f.Wallet.GetBalance(f.Wallet.CreditsResource) == credits + 3, "Killing hit cannot apply Slow or duplicate XP/Credits");
        enemy.Free(); f.ClearProjectiles();
        enemy = f.Target(new(2000, 2000));
        tower.Config.SetLevelStats(1, tower.Levels[0].Stats with { Slow = new(.4f), Weapon = new(DamageType.Energy, true, 2, true, .5f), ShieldPenetration = .2f });
        tower.Config.TargetPriority = TowerTargeting.Priority.Closest;
        tower._PhysicsProcess(.25);
        shot = f.Main.GetNode("World/Projectiles").GetChildren().OfType<BasicProjectile>().Single(); Disable(shot);
        Require(tower.Ammunition.ConsumedRounds == 1, "Real Cryo launch consumes exactly one physical round");
        tower.Config.SetLevelStats(1, tower.Levels[0].Stats with { DamagePerShot = 900, Slow = new(.8f) });
        tower.Free(); float before = enemy.Health;
        shot._PhysicsProcess(.3);
        Near(enemy.EffectiveMoveSpeed, 60, "Launch-time Slow survives tower changes/removal");
        Near(before - enemy.Health, 5, "Launch-time Energy override survives tower removal");
        f.ClearProjectiles(); enemy.Free();
        enemy = f.Target(new(2000, 2000));
        shot = new BasicProjectile(); shot.Initialize(enemy, 650, new DamageInfo(10, DamageType.Energy), new(StatusEffectId.Slow, .33f));
        f.Main.GetNode("World/Projectiles").AddChild(shot); shot.GlobalPosition = enemy.GlobalPosition;
        enemy.Definition.StatusImmunities.Add(StatusEffectId.Slow); before = enemy.Health;
        shot._PhysicsProcess(.01);
        Near(enemy.Health, DamageResolver.Resolve(new DamageInfo(10, DamageType.Energy), 0, before).Health, "Immune target retains normal Energy hull coefficient");
        Require(!enemy.HasStatus(StatusEffectId.Slow), "Immunity gained during flight rejects only the status");
        f.ClearProjectiles(); enemy.Free();
    }

    // =========================================================
    // Filter within the existing spatial visitor; no damage fallback, including cached targets.
    private static void Targeting(AmmoLoopFixture f)
    {
        Enemy slow = f.Target(new(2000, 2000)), eligible = f.Target(new(2020, 2000)), immune = f.Target(new(2010, 2000));
        slow.ApplyStatus(new(StatusEffectId.Slow, .2f)); immune.Definition.StatusImmunities.Add(StatusEffectId.Slow);
        Vector2 origin = new(2000, 2064);
        Require(TowerTargeting.Find(f.Spawns.Registry, origin, 208, TowerTargeting.Priority.Closest) == slow,
            "Other weapons keep original target semantics");
        Require(TowerTargeting.Find(f.Spawns.Registry, origin, 208, TowerTargeting.Priority.Closest, StatusEffectId.Slow) == eligible,
            "Cryo skips nearer slowed/immune enemies");
        CryoTower tower = f.Add<CryoTower>("cryo", origin); tower.Config.TargetPriority = TowerTargeting.Priority.Closest;
        tower._PhysicsProcess(.25); f.ClearProjectiles();
        eligible.ApplyStatus(new(StatusEffectId.Slow, .33f));
        long rounds = tower.Ammunition.ConsumedRounds;
        for (int i = 0; i < 240; i++) tower._PhysicsProcess(1.0 / 60);
        Require(tower.Ammunition.ConsumedRounds == rounds && TowerTargeting.Find(f.Spawns.Registry, origin, 208,
            TowerTargeting.Priority.Closest, StatusEffectId.Slow) == null, "All slowed/immune means wait, never fallback damage");
        slow.Free(); eligible.Free(); immune.Free(); tower.Free(); f.ClearProjectiles();
    }

    // =========================================================
    // Actual routed enemy moves more slowly without changing navigation revision or requesting a new route.
    private static void Movement(AmmoLoopFixture f)
    {
        Enemy enemy = f.Spawns.Spawn(GD.Load<PackedScene>("res://ENEMIES/Types/Enemy001/Enemy001.tscn")); Disable(enemy);
        for (int i = 0; f.Spawns.Paths.PendingCount > 0 && i < 300; i++) f.Spawns.Paths._PhysicsProcess(1.0 / 60);
        var grid = f.Main.GetNode<WorldGrid>("World/Grid"); int revision = grid.NavigationRevision, pending = f.Spawns.Paths.PendingCount;
        float distance = enemy.RemainingPathDistance;
        enemy.ApplyStatus(new(StatusEffectId.Slow, .33f));
        Vector2 start = enemy.GlobalPosition; enemy.Movement._PhysicsProcess(.01);
        Near(start.DistanceTo(enemy.GlobalPosition), .67f, "Actual movement uses effective speed");
        Require(float.IsFinite(distance) && enemy.RemainingPathDistance < distance && grid.NavigationRevision == revision
            && f.Spawns.Paths.PendingCount == pending, "Slow preserves route progress and A* budgets");
        enemy.Free();
        // First uses physical route progress among eligible enemies, regardless of portal order.
        Enemy first = f.Spawns.Spawn(GD.Load<PackedScene>("res://ENEMIES/Types/Enemy001/Enemy001.tscn")); Disable(first);
        Enemy second = f.Spawns.Spawn(GD.Load<PackedScene>("res://ENEMIES/Types/Enemy001/Enemy001.tscn")); Disable(second);
        for (int i = 0; f.Spawns.Paths.PendingCount > 0 && i < 300; i++) f.Spawns.Paths._PhysicsProcess(1.0 / 60);
        Vector2 origin = first.GlobalPosition;
        Enemy preferred = TowerTargeting.Find(f.Spawns.Registry, origin, 100000, TowerTargeting.Priority.First, StatusEffectId.Slow);
        Require(preferred != null, "First finds an eligible routed enemy");
        preferred.ApplyStatus(new(StatusEffectId.Slow, .33f));
        Require(TowerTargeting.Find(f.Spawns.Registry, origin, 100000, TowerTargeting.Priority.First, StatusEffectId.Slow)
            == (preferred == first ? second : first), "First skips the slowed leading candidate");
        first.Free(); second.Free();
    }

    // =========================================================
    // Status-only movement changes leave the shared stationary hazard clock running independently.
    private static void Lava(AmmoLoopFixture f)
    {
        WorldGrid grid = f.Main.GetNode<WorldGrid>("World/Grid");
        Vector2I cell = grid.Level.Passable.GetUsedCells().First(c => !grid.BlocksEnemies(c));
        var layer = grid.Level.Interactive; int source = layer.GetCellSourceId(cell), alternative = layer.GetCellAlternativeTile(cell);
        Vector2I atlas = layer.GetCellAtlasCoords(cell);
        Enemy enemy = null;
        try
        {
            layer.SetCell(cell, 1, Vector2I.Zero); grid.RebuildTerrain();
            enemy = f.Target(grid.CellToWorldCenter(cell)); enemy.ApplyStatus(new(StatusEffectId.Slow, .33f));
            float before = enemy.Health;
            grid.InteractiveTiles._PhysicsProcess(grid.InteractiveTiles.LavaConfig.DamageIntervalSeconds);
            Require(enemy.Health < before && enemy.HasStatus(StatusEffectId.Slow), "Stationary slowed enemy still receives Lava interval damage");
        }
        finally
        {
            enemy?.Free();
            if (source < 0) layer.EraseCell(cell); else layer.SetCell(cell, source, atlas, alternative);
            grid.RebuildTerrain();
        }
    }

    // =========================================================
    // Real 500-enemy smoke plus warmed cached speed reads: allocations must stay zero with/without statuses.
    private static void Population(AmmoLoopFixture f)
    {
        Enemy[] enemies = new Enemy[500];
        for (int i = 0; i < enemies.Length; i++) enemies[i] = f.Target(new(2000 + i % 25 * 8, 2000 + i / 25 * 8));
        foreach (bool slowed in new[] { false, true })
        {
            if (slowed) foreach (Enemy enemy in enemies) enemy.ApplyStatus(new(StatusEffectId.Slow, .33f));
            float total = 0;
            for (int i = 0; i < 10000; i++) total += enemies[i % 500].EffectiveMoveSpeed;
            Stopwatch watch = new(); long before = GC.GetAllocatedBytesForCurrentThread(); watch.Start();
            for (int i = 0; i < 500000; i++) total += enemies[i % 500].EffectiveMoveSpeed;
            watch.Stop(); long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            Require(allocated == 0 && total > 0, "Cached speed common path allocates no per-tick memory");
            GD.Print($"CRYO BENCH: 500 enemies, 500000 effective-speed reads, slowed={slowed}: {watch.Elapsed.TotalMilliseconds:0.###} ms, {allocated} bytes");
        }
        foreach (Enemy enemy in enemies) enemy.Free();
    }

    // =========================================================
    // Allow float arithmetic at pixel scale without masking gameplay differences.
    private static void Near(float actual, float expected, string message) => Require(Math.Abs(actual - expected) < .002f, $"{message}: {actual} != {expected}");
}
