// Real tower and resolver checks for the neutral Phase 3A pipeline and Inspector invalidation.
using Godot;
using System;

public static class TowerStatChecks
{
    // =========================================================
    // Assert baseline parity, all eight stages, safe input handling and independent live caches.
    public static void Run(Node battle)
    {
        ExplicitLevels();
        AllTowerBalanceRows();
        BattleTowerConfig defaults = GD.Load<BattleTowerConfig>("res://TOWERS/Offensive/BattleTower/DefaultBattleTowerConfig.tres");
        TowerStatTrace trace = new();
        TowerFinalStats baseline = TowerStatPipeline.Resolve(defaults, trace);
        Require(baseline.RangePixels == defaults.RangePixels && baseline.ShotsPerSecond == defaults.ShotsPerSecond &&
            baseline.DamagePerShot == defaults.DamagePerShot && baseline.ProjectileSpeed == defaults.ProjectileSpeed &&
            baseline.RotationSpeedDegrees == defaults.RotationSpeedDegrees && baseline.AimToleranceDegrees == defaults.AimToleranceDegrees &&
            baseline.TargetSearchInterval == defaults.TargetSearchInterval, "Valid final values equal authored base stats");
        Require(baseline.ShieldPenetration == 0.05f && defaults.DescribeStats(baseline).Contains("Shield Penetration: 5%"),
            "Battle base penetration enters cached stats and Inspector at level one");
        foreach (float input in new[] { -1f, 2f, float.NaN, float.PositiveInfinity })
        {
            BattleTowerConfig penetration = defaults.CreateInstanceCopy(); penetration.ShieldPenetration = input;
            float expected = input == 2 ? 1 : 0;
            Require(penetration.ResolveStats(1).ShieldPenetration == expected, "Penetration safety clamps to finite0..1");
        }
        Require(trace.Steps.Count == 8, "All eight stages execute");
        for (int i = 0; i < 8; i++)
        {
            Require(trace.Steps[i].Stage.StartsWith($"{i + 1:00} "), "Stages execute in numbered order");
            Require(trace.Steps[i].Stats == trace.Steps[0].Stats, "Neutral stages preserve baseline values");
        }
        Require(TowerStatPipeline.Resolve(defaults) == baseline, "Resolution is deterministic");
        TowerStatPipeline.Resolve(defaults, trace); Require(trace.Steps.Count == 8, "Trace reuse does not accumulate snapshots");

        BattleTowerConfig invalid = (BattleTowerConfig)defaults.Duplicate(true);
        invalid.RangePixels = float.NaN; invalid.ShotsPerSecond = float.PositiveInfinity;
        invalid.DamagePerShot = -20; invalid.ProjectileSpeed = -1; invalid.RotationSpeedDegrees = float.NegativeInfinity;
        invalid.AimToleranceDegrees = 400; invalid.TargetSearchInterval = 0;
        TowerFinalStats safe = TowerStatPipeline.Resolve(invalid);
        Require(safe.RangePixels == 0 && safe.ShotsPerSecond == 0 && safe.DamagePerShot == 0 &&
            safe.ProjectileSpeed == 1 && safe.RotationSpeedDegrees == 0 && safe.AimToleranceDegrees == 180 &&
            safe.TargetSearchInterval == 0.05f, "Nonfinite/negative values clamp to documented safe bounds");
        Require(float.IsNaN(invalid.RangePixels) && invalid.DamagePerShot == -20, "Clamping never alters authored inputs");

        PackedScene scene = GD.Load<PackedScene>("res://TOWERS/Offensive/BattleTower/BattleTower.tscn");
        BattleTower first = scene.Instantiate<BattleTower>(), second = scene.Instantiate<BattleTower>();
        first.Config = (BattleTowerConfig)defaults.Duplicate(true); second.Config = (BattleTowerConfig)defaults.Duplicate(true);
        Node towers = battle.GetNode("World/Towers");
        towers.AddChild(first); towers.AddChild(second);
        first.SetPhysicsProcess(false); second.SetPhysicsProcess(false);
        try
        {
            Require(first.FinalStats == baseline && second.FinalStats == baseline &&
                !ReferenceEquals(first.FinalStats, second.FinalStats), "Live towers have separate immutable final snapshots");
            TowerFinalStats old = first.FinalStats;
            int changes = 0; first.StatsChanged += () => changes++;
            first.Config.RangePixels = 400; first.Config.ShotsPerSecond = 0;
            Require(first.FinalStats.RangePixels == 400 && first.FinalStats.ShotsPerSecond == 0 && changes == 2,
                "Inspector-equivalent Resource edits rebuild immediately");
            Require(old == baseline && second.FinalStats == baseline && defaults.RangePixels == baseline.RangePixels,
                "Changing one tower leaves its old snapshot, other towers and shared defaults unchanged");
            first.RebuildStats(); Require(first.FinalStats.RangePixels == 400, "Explicit future-input rebuild is supported");
            TowerFinalStats cached = first.FinalStats;
            first._PhysicsProcess(1.0 / 60); first._PhysicsProcess(1.0 / 60);
            Require(ReferenceEquals(first.FinalStats, cached), "Combat ticks do not recalculate final stats");
            SpawnManager spawns = battle.GetNode<SpawnManager>("World/EnemySystems/Spawning");
            Enemy target = spawns.Spawn(GD.Load<PackedScene>("res://ENEMIES/Types/Enemy001/Enemy001.tscn"), true);
            Require(target != null, "Spawn a real target for disabled-fire checks");
            target.Movement.SetPhysicsProcess(false);
            while (spawns.Paths.PendingCount > 0) spawns.Paths._PhysicsProcess(1.0 / 60);
            first.GlobalPosition = target.GlobalPosition + new Vector2(100, 0);
            first.GetNode<Node2D>("TurretPivot").Rotation = -Mathf.Pi * 0.5f;
            int projectiles = battle.GetNode("World/Projectiles").GetChildCount();
            for (int tick = 0; tick < 30; tick++) first._PhysicsProcess(1.0 / 60);
            Require(battle.GetNode("World/Projectiles").GetChildCount() == projectiles && target.Health == target.Definition.MaxHealth,
                "Zero final fire rate prevents shots at a living in-range aligned target");
            first.Config.ShotsPerSecond = 1;
            first._PhysicsProcess(1.0 / 60);
            Require(battle.GetNode("World/Projectiles").GetChildCount() == projectiles + 1,
                "Changing Config fire rate enables shooting through updated final stats");
            foreach (Node bullet in battle.GetNode("World/Projectiles").GetChildren()) bullet.Free();
            target.Arrive();
            TowerPlacementController placement = battle.GetNode<TowerPlacementController>("TowerPlacement");
            placement.Call("SelectTower", first);
            first.Config.DamagePerShot = baseline.DamagePerShot + 1;
            Label details = battle.GetNode<Label>("UI/GameplayHUD/BottomArea/Inspector/Insets/Body/Columns/Main/TowerStats");
            Require(details.Text.Contains($"Damage: {first.FinalStats.DamagePerShot}") && details.Text.Contains("Range: 400"),
                "Selected Inspector displays updated final values");
            first.Config.ShieldPenetration = 0.1f;
            Require(first.FinalStats.ShieldPenetration == 0.1f && second.FinalStats.ShieldPenetration == 0.05f
                && defaults.ShieldPenetration == 0.05f && details.Text.Contains("Shield Penetration: 10%"),
                "Penetration edits refresh selected Inspector without changing other instances or source");
            placement.Call("SelectTower", Variant.From((GodotObject)null));
            BattleTowerConfig prior = first.Config;
            first.Config = (BattleTowerConfig)defaults.Duplicate(true);
            Require(first.FinalStats == baseline, "Replacing an instance config resolves the new source");
            cached = first.FinalStats; prior.RangePixels = 500;
            Require(ReferenceEquals(first.FinalStats, cached), "Old config no longer invalidates a replaced tower");
            BattleTowerConfig afterExit = first.Config;
            towers.RemoveChild(first); afterExit.RangePixels = 600;
            Require(ReferenceEquals(first.FinalStats, cached), "Exiting the world removes Resource subscriptions");
        }
        finally { first.Free(); second.Free(); }
        GD.Print("PASS: eight-stage tower pipeline, baseline parity, clamps, immutable caches, Inspector edits and instance independence");
    }

    // =========================================================
    // Assert full migration parity independently of the production row adapters.
    private static void ExplicitLevels()
    {
        BattleTowerConfig battle = new(); LaserTowerConfig laser = new(); BlasterCannonConfig blaster = new();
        TowerFinalStats[] battleExpected = {
            new(323.4f, 1.68f, 30, 2300, 216, 12, .15f, ShieldPenetration: .05f, Ammunition: new(1000, 1), MaxHealth: 1000f),
            new(334.4f, 1.764f, 33, 2300, 216, 12, .15f, ShieldPenetration: .05f, Ammunition: new(1000, 1), MaxHealth: 1050f),
            new(344.3f, 1.848f, 36, 2300, 216, 12, .15f, ShieldPenetration: .05f, Ammunition: new(1000, 1), MaxHealth: 1102.5f) };
        TowerFinalStats[] laserExpected = {
            new(323.4f, 0, 0, 1, 216, 12, .15f, new BeamStats(12), Weapon: new(DamageType.Energy, true, 6, true, .15f), Ammunition: new(1000, 0, 2), MaxHealth: 1000f),
            new(334.4f, 0, 0, 1, 216, 12, .15f, new BeamStats(14), Weapon: new(DamageType.Energy, true, 6, true, .15f), Ammunition: new(1000, 0, 2), MaxHealth: 1050f) };
        TowerFinalStats[] blasterExpected = {
            new(323.4f, .5f, 60, 575, 216, 12, .15f, Area: new(80, .25f), Weapon: new(DamageType.Explosive, false, 1, false, 1), Ammunition: new(600, 3), MaxHealth: 1000f),
            new(334.4f, .5f, 66, 575, 216, 12, .15f, Area: new(88, .25f), Weapon: new(DamageType.Explosive, false, 1, false, 1), Ammunition: new(600, 3), MaxHealth: 1050f) };
        CheckRows(battle, battleExpected, new[] { 200, 150, 250 });
        CheckRows(laser, laserExpected, new[] { 250, 175 });
        CheckRows(blaster, blasterExpected, new[] { 300, 200 });
        TowerFinalStats last = battle.ResolveStats(3);
        battle.SetLevelStats(2, battle.Levels[1].Stats with { RangePixels = 700, DamagePerShot = 999,
            ShotsPerSecond = 7, ProjectileSpeed = 1500, RotationSpeedDegrees = 300,
            AimToleranceDegrees = 80, TargetSearchInterval = .8f, ShieldPenetration = .9f });
        Require(battle.ResolveStats(3) == last, "Level3 ignores every level2 combat field");
        TowerLevel fourth = new(4, 400, battle.Levels[2].Stats with { DamagePerShot = 30, RangePixels = 400 });
        battle.Levels.Add(fourth);
        Require(battle.ResolveStats(4).DamagePerShot == 30 && battle.Levels[3].Costs[0].Amount == 400,
            "Adding another complete row requires no progression-code changes");
        Require(new BattleTowerConfig().ResolveStats(2).DamagePerShot == 33, "Runtime rows never alter C# defaults");
        foreach (int level in new[] { 0, 5 })
        {
            try { battle.ResolveStats(level); throw new InvalidOperationException("Out-of-range level accepted."); }
            catch (ArgumentOutOfRangeException) { }
        }
        GD.Print("PASS: exact complete Battle1–3/Laser1–2/Blaster1–2 stats and costs, row independence and append-only expansion");
    }

    // =========================================================
    // Check every purchased row's new authored range, turn and projectile values.
    // Laser's beam and Shockwave's expanding pulse are intentionally not travelling projectiles.
    private static void AllTowerBalanceRows()
    {
        TowerConfig[] towers = { new BattleTowerConfig(), new LaserTowerConfig(), new BlasterCannonConfig(),
            new MachineGunTowerConfig(), new SniperTowerConfig(), new RocketTowerConfig(),
            new CryoTowerConfig(), new StasisTowerConfig(), new ShockwaveTowerConfig() };
        float[][] ranges = {
            new[] { 323.4f, 334.4f, 344.3f }, new[] { 323.4f, 334.4f }, new[] { 323.4f, 334.4f },
            new[] { 262.9f, 272.8f, 283.8f }, new[] { 566.5f, 576.4f, 587.4f },
            new[] { 440f, 462f, 484f }, new[] { 262.9f, 272.8f, 283.8f },
            new[] { 262.9f, 272.8f, 283.8f }, new[] { 253f, 262.9f } };
        float[] turns = { 216f, 216f, 216f, 288f, 162f, 264f, 216f, 216f, 0f };
        float[] speeds = { 2300f, 1f, 575f, 2443.75f, 2990f, 0f, 747.5f, 747.5f, 1f };
        float[] rocketSpeeds = { 575f, 598f, 621f };
        for (int t = 0; t < towers.Length; t++)
        {
            Require(towers[t].Levels.Count == ranges[t].Length, "Each offensive weapon retains its authored level count");
            for (int i = 0; i < ranges[t].Length; i++)
            {
                TowerFinalStats actual = towers[t].ResolveStats(i + 1);
                float expectedHp = i == 0 ? 1000f : i == 1 ? 1050f : 1102.5f;
                Require(Mathf.IsEqualApprox(actual.MaxHealth, expectedHp),
                    "All offensive purchased tiers gain exactly 5% compounded max HP");
                Require(Mathf.IsEqualApprox(actual.RangePixels, ranges[t][i]) &&
                    Mathf.IsEqualApprox(actual.RotationSpeedDegrees, turns[t]),
                    "Range and turret rotation increase on every purchased level");
                float speed = t == 5 ? rocketSpeeds[i] : speeds[t];
                Require(Mathf.IsEqualApprox(actual.ProjectileSpeed, speed),
                    "Projectile travel speed increases only for projectile weapons");
                if (t == 8)
                    Require(actual.Shockwave.Value.ExpansionSpeedPixelsPerSecond == (i == 0 ? 250 : 260),
                        "Shockwave's radial expansion speed is unchanged");
            }
        }
    }

    // =========================================================
    // Compare all fields, then ensure each level uses its ordinary shared Credits recipe.
    private static void CheckRows(TowerConfig config, TowerFinalStats[] expected, int[] costs)
    {
        Require(config.Levels.Count == expected.Length, "Migrated level count is exact");
        for (int i = 0; i < expected.Length; i++)
        {
            Require(config.ResolveStats(i + 1) == expected[i], $"Exact complete {config.DisplayName} level{i + 1} stats");
            Require(config.Levels[i].Level == i + 1 && config.Levels[i].Costs.Count == 1 &&
                config.Levels[i].Costs[0].Resource.ResourceId == "credits" && config.Levels[i].Costs[0].Amount == costs[i],
                "Construction and purchase rows retain their exact shared-resource prices");
        }
    }

    // =========================================================
    // Fail the headless integration scene on a stat ownership or runtime regression.
    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
