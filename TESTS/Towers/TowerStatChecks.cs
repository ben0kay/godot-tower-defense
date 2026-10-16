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
            Label details = battle.GetNode<Label>("UI/GameplayHUD/BottomArea/Inspector/Insets/Body/TowerStats");
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
            new(256, 1, 12, 600, 180, 12, .15f, ShieldPenetration: .05f),
            new(288, 1.2f, 16, 600, 180, 12, .15f, ShieldPenetration: .05f),
            new(320, 1.5f, 22, 600, 180, 12, .15f, ShieldPenetration: .05f) };
        TowerFinalStats[] laserExpected = {
            new(256, 0, 0, 1, 180, 12, .15f, new BeamStats(12), Weapon: new(DamageType.Energy, true, 6, true, .15f)),
            new(288, 0, 0, 1, 180, 12, .15f, new BeamStats(22), Weapon: new(DamageType.Energy, true, 6, true, .15f)) };
        TowerFinalStats[] blasterExpected = {
            new(256, .5f, 60, 500, 180, 12, .15f, Area: new(80, .25f), Weapon: new(DamageType.Explosive, false, 1, false, 1)),
            new(256, .5f, 75, 500, 180, 12, .15f, Area: new(96, .25f), Weapon: new(DamageType.Explosive, false, 1, false, 1)) };
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
        Require(new BattleTowerConfig().ResolveStats(2).DamagePerShot == 16, "Runtime rows never alter C# defaults");
        foreach (int level in new[] { 0, 5 })
        {
            try { battle.ResolveStats(level); throw new InvalidOperationException("Out-of-range level accepted."); }
            catch (ArgumentOutOfRangeException) { }
        }
        GD.Print("PASS: exact complete Battle1–3/Laser1–2/Blaster1–2 stats and costs, row independence and append-only expansion");
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
