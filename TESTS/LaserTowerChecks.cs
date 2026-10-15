// Real catalog placement, sustained beam/typed DPS and visual independence regression coverage.
using Godot;
using System;
using System.Reflection;
using System.Collections.Generic;
public static class LaserTowerChecks
{
    // =========================================================
    // Verify genuine beam capability math through unchanged staged progression and production APIs.
    public static async System.Threading.Tasks.Task Run(Node main, SpawnManager spawns)
    {
        await main.ToSignal(main.GetTree(), SceneTree.SignalName.ProcessFrame);
        TowerPlacementController placement = main.GetNode<TowerPlacementController>("TowerPlacement");
        GameplayHUD hud = main.GetNode<GameplayHUD>("UI/GameplayHUD");
        EconomyManager wallet = main.GetNode<EconomyManager>("Economy");
        WorldGrid grid = main.GetNode<WorldGrid>("World/Grid");
        Node2D towers = main.GetNode<Node2D>("World/Towers");
        TowerDefinition definition = placement.Catalog.Find("laser");
        Require(definition != null && hud.GetBuildButton("laser") != null, "Real Laser definition registers its typed scene");
        definition.Validate();
        wallet.Initialize(new LevelConfig { StartingCredits = 1000 });
        hud.GetBuildButton("laser").EmitSignal(Button.SignalName.Pressed);
        Vector2I cell = default; bool found = false;
        foreach (Vector2I candidate in grid.Level.Passable.GetUsedCells())
            if (grid.PlacementFailure(candidate).Length == 0 && spawns.PlacementFailure(candidate).Length == 0)
            { cell = candidate; found = true; break; }
        Require(found, "Laser build cell is route-safe");
        placement.Call("TryPlace", cell);
        LaserTower tower = towers.GetChild<LaserTower>(towers.GetChildCount() - 1); tower.SetPhysicsProcess(false);
        Require(tower.TowerTypeId == "laser" && grid.IsOccupied(cell) && wallet.GetBalance(wallet.CreditsResource) == 750 &&
            tower.FinalStats.Beam.Value.DamagePerSecond == 12 && tower.FinalStats.ShieldPenetration == 0, "Dynamic action places/pays the correct Energy tower");
        Require(tower.Config != definition.DefaultConfig && tower.UpgradeTiers[0] != definition.DefaultConfig.UpgradeTiers[0], "Laser source/tier data is private");
        placement.Call("TryPlace", cell);
        Require(wallet.GetBalance(wallet.CreditsResource) == 750, "Repeat Laser occupancy cannot charge");
        Label details = hud.GetNode<Label>("BottomArea/Inspector/Insets/Body/TowerStats");
        Label upgrade = hud.GetNode<Label>("BottomArea/Inspector/Insets/Body/UpgradeInfo");
        Require(details.Text.Contains("DPS: 12") && details.Text.Contains("Shield: 6.00x / Hull: 0.15x") && !details.Text.Contains("shots/s") && upgrade.Text.Contains("DPS: 12 → 22"), "Inspector displays actual beam capability/preview");
        tower.Config.HullCoefficient = 0.2f;
        Require(details.Text.Contains("Hull: 0.20x") && ((LaserTowerConfig)definition.DefaultConfig).HullCoefficient == 0.15f,
            "Selected Laser Inspector refreshes coefficients without mutating the shared source");
        tower.Config.HullCoefficient = 0.15f;
        TowerFinalStats preview = tower.ResolveLevel(2); int revision = grid.NavigationRevision;
        hud.GetNode<Button>("BottomArea/Inspector/Insets/Body/UpgradeButton").EmitSignal(Button.SignalName.Pressed);
        Require(tower.UpgradeLevel == 2 && tower.FinalStats == preview && tower.FinalStats.Beam.Value.DamagePerSecond == 22 &&
            tower.FinalStats.RangePixels == 288 && wallet.GetBalance(wallet.CreditsResource) == 575 && grid.NavigationRevision == revision,
            "Laser175-Credit tier applies exact DPS/range preview without changing navigation");
        placement.Call("SelectTower", default(Variant)); placement.Call("CancelPlacement");
        tower.Free(); grid.ReleaseOccupancy(cell);
        var index = (Dictionary<Vector2I, TowerBase>)typeof(TowerPlacementController)
            .GetField("_placedTowers", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(placement);
        index.Remove(cell);
        PackedScene enemyScene = GD.Load<PackedScene>("res://ENEMIES/Types/Enemy001/Enemy001.tscn");
        // Each rate fixture starts with a stationary valid lock and full shield: no shield break yet.
        foreach (int rate in new[] { 30, 60, 120 })
        {
            Enemy target = spawns.SpawnWave(enemyScene, new RandomNumberGenerator { Seed = 7 }, spawnWithShield: true);
            target.Movement.SetPhysicsProcess(false);
            LaserTower laser = Create(definition, towers, target.GlobalPosition + new Vector2(0, 64));
            BeamRenderer beam = laser.GetNode<BeamRenderer>("BeamRenderer");
            int nodes = Count(main), projectiles = main.GetNode("World/Projectiles").GetChildCount();
            for (int i = 0; i < rate; i++) laser._PhysicsProcess(1.0 / rate);
            Require(Mathf.Abs(target.CurrentShield - 28) < 0.003f && target.Health == 60 && beam.IsActive,
                "12 base DPS removes72 shield per second at " + rate + "Hz");
            Require(Count(main) == nodes && main.GetNode("World/Projectiles").GetChildCount() == projectiles, "Beam attack creates no per-tick nodes/projectiles");
            Marker2D muzzle = laser.GetNode<Marker2D>("TurretPivot/Muzzle");
            Line2D core = beam.GetNode<Line2D>("Core");
            Require(core.GetPointCount() == 2 && beam.ToGlobal(core.GetPointPosition(0)).IsEqualApprox(muzzle.GlobalPosition) &&
                beam.ToGlobal(core.GetPointPosition(1)).IsEqualApprox(target.GlobalPosition), "World endpoints stay anchored to muzzle/target");
            TowerFinalStats cached = laser.FinalStats; float health = target.Health, shield = target.CurrentShield;
            beam.Style.CoreWidth = 6; beam.Style.OuterWidth = 15; beam.Style.CoreColor = Colors.Magenta; beam.Style.PulseAmount = 0;
            Require(core.Width == 6 && core.DefaultColor == Colors.Magenta && ReferenceEquals(cached, laser.FinalStats) &&
                target.Health == health && target.CurrentShield == shield, "Visual edits change lines without touching stats or damage");
            laser.Enabled = false;
            Require(!beam.IsActive && !beam.Visible && core.GetPointPosition(1) == Vector2.Zero, "Disable immediately clears endpoints");
            laser._PhysicsProcess(0.1); Require(target.CurrentShield == shield, "Disabled Laser deals no damage");
            laser.Enabled = true; target.GlobalPosition += new Vector2(1000, 0); laser._PhysicsProcess(0.01);
            Require(!beam.IsActive && target.CurrentShield == shield, "Out-of-range loses lock without damage");
            laser.Free(); target.Arrive();
            Enemy exposed = spawns.Spawn(enemyScene, true); exposed.Movement.SetPhysicsProcess(false);
            LaserTower hullLaser = Create(definition, towers, exposed.GlobalPosition + new Vector2(0, 64));
            for (int i = 0; i < rate; i++) hullLaser._PhysicsProcess(1.0 / rate);
            Require(Mathf.Abs(exposed.Health - 58.2f) < 0.003f, "Exposed hull DPS remains1.8 across " + rate + "Hz");
            hullLaser.Free(); exposed.Arrive();
        }
        Enemy hull = spawns.Spawn(enemyScene, true); hull.Movement.SetPhysicsProcess(false);
        LaserTower attacker = Create(definition, towers, hull.GlobalPosition + new Vector2(0, 64));
        for (int i = 0; i < 60; i++) attacker._PhysicsProcess(1.0 / 60);
        Require(Mathf.Abs(hull.Health - 58.2f) < 0.003f, "12 Energy DPS applies1.8 unshielded hull damage per second");
        BeamRenderer renderer = attacker.GetNode<BeamRenderer>("BeamRenderer");
        TowerFinalStats beforeEdit = attacker.FinalStats;
        attacker.Config.HullCoefficient = 0.2f;
        Require(!ReferenceEquals(beforeEdit, attacker.FinalStats), "Defense override edits invalidate once through Resource.Changed");
        TowerFinalStats afterEdit = attacker.FinalStats;
        attacker._PhysicsProcess(0.01);
        Require(ReferenceEquals(afterEdit, attacker.FinalStats), "Active beam ticks reuse cached effective stats");
        attacker.Config.HullCoefficient = 0.15f;
        float beforeHealth = hull.Health;
        attacker.GetNode<Node2D>("TurretPivot").Rotation = Mathf.Pi;
        attacker.Config.RotationSpeedDegrees = 0; attacker.Config.RotateWhileTracking = false; attacker.Config.AimToleranceDegrees = 1;
        attacker._PhysicsProcess(0.1);
        Require(hull.Health == beforeHealth && !renderer.IsActive && attacker.Rotation == 0, "Misalignment gates damage/beam without root rotation");
        attacker.GetNode<Node2D>("TurretPivot").Rotation = 0;
        attacker._PhysicsProcess(0.1); Require(renderer.IsActive, "Restored alignment resumes sustained lock");
        attacker.Config.DamagePerSecond = 0;
        Require(!renderer.IsActive, "Zero-DPS input invalidation clears the beam immediately");
        attacker.Config.DamagePerSecond = 12;
        attacker._PhysicsProcess(0.01); hull.Arrive();
        Require(!renderer.IsActive, "Target arrival clears visible beam immediately");
        attacker.Free();
        Enemy broken = spawns.SpawnWave(enemyScene, new RandomNumberGenerator { Seed = 2 }, spawnWithShield: true);
        broken.Movement.SetPhysicsProcess(false); broken.TakeDamage(70);
        LaserTower spill = Create(definition, towers, broken.GlobalPosition + new Vector2(0, 64));
        spill._PhysicsProcess(1);
        Require(broken.CurrentShield == 0 && Mathf.IsEqualApprox(broken.Health, 58.95f), "Beam tick converts remaining base units on fractional shield break");
        int credits = wallet.GetBalance(wallet.CreditsResource);
        spill.Config.DamagePerSecond = 10000; spill._PhysicsProcess(1); spill._PhysicsProcess(1);
        Require(!spill.GetNode<BeamRenderer>("BeamRenderer").IsActive && wallet.GetBalance(wallet.CreditsResource) == credits + 3,
            "Beam kill hides immediately and pays once");
        spill.Free();
        Enemy near = spawns.Spawn(enemyScene, true), far = spawns.Spawn(enemyScene, true);
        near.Movement.SetPhysicsProcess(false); far.Movement.SetPhysicsProcess(false);
        far.GlobalPosition += new Vector2(64, 0);
        for (int i = 0; i < 20; i++) spawns.Paths._PhysicsProcess(1.0 / 60);
        LaserTower chooser = Create(definition, towers, near.GlobalPosition + new Vector2(0, 64));
        chooser.Config.TargetPriority = TowerTargeting.Priority.First;
        Enemy expected = TowerTargeting.Find(spawns.Registry, chooser.GlobalPosition, chooser.FinalStats.RangePixels, TowerTargeting.Priority.First);
        Require(expected != null, "Assigned First fixture has a valid route candidate");
        chooser._PhysicsProcess(0.2);
        FieldInfo lockField = typeof(LaserTower).GetField("_target", BindingFlags.Instance | BindingFlags.NonPublic);
        Require(lockField.GetValue(chooser) == expected, "Laser uses shared First path-distance acquisition");
        chooser.Config.TargetPriority = TowerTargeting.Priority.Closest;
        near.GlobalPosition = chooser.GlobalPosition + new Vector2(0, -64);
        far.GlobalPosition = chooser.GlobalPosition + new Vector2(0, -128);
        chooser._PhysicsProcess(0.2);
        Require(lockField.GetValue(chooser) == near, "Laser chooses shared Closest acquisition");
        near.GlobalPosition += new Vector2(1000, 0);
        chooser._PhysicsProcess(0.01);
        Require(!chooser.GetNode<BeamRenderer>("BeamRenderer").IsActive, "Invalid old lock stops before periodic reacquisition");
        chooser._PhysicsProcess(0.2);
        Require(lockField.GetValue(chooser) == far && chooser.GetNode<BeamRenderer>("BeamRenderer").IsActive,
            "Periodic retarget updates the one persistent renderer");
        BeamRenderer unloaded = chooser.GetNode<BeamRenderer>("BeamRenderer");
        far.Free();
        Require(!unloaded.IsActive, "External enemy unload immediately clears its beam");
        chooser.Enabled = false; near.Arrive();
        towers.RemoveChild(chooser);
        Require(!unloaded.IsActive && !unloaded.Visible, "Tower unload clears renderer state/subscriptions");
        chooser.Free();
        LaserTowerConfig operations = ((LaserTowerConfig)definition.DefaultConfig).CreateInstanceCopy();
        operations.UpgradeTiers[0].Modifiers.Add(new TowerUpgradeModifier { Stat = TowerStatId.BeamDps, Operation = TowerStatOperation.AdditivePercent, Amount = 0.2f });
        operations.UpgradeTiers[0].Modifiers.Add(new TowerUpgradeModifier { Stat = TowerStatId.BeamDps, Operation = TowerStatOperation.Multiplier, Amount = 0.5f });
        Require(Mathf.IsEqualApprox(operations.ResolveStats(2).Beam.Value.DamagePerSecond, 13.2f), "Beam DPS uses shared flat/percent/multiplier stage arithmetic without integer rounding");
        LaserTowerConfig invalid = ((LaserTowerConfig)definition.DefaultConfig).CreateInstanceCopy(); invalid.DamagePerSecond = float.NaN;
        Require(invalid.ResolveStats(1).Beam.Value.DamagePerSecond == 0, "Beam capability has finite safety clamps");
        LaserTowerConfig inherited = ((LaserTowerConfig)definition.DefaultConfig).CreateInstanceCopy();
        inherited.OverrideShieldCoefficient = false;
        DamageInfo inheritedShield = inherited.CreateHit(12);
        Require(inheritedShield.ShieldCoefficientOverride == null && inheritedShield.HullCoefficientOverride == 0.15f
            && Mathf.IsEqualApprox(DamageResolver.Resolve(inheritedShield, 100, 60).Shield, 76), "Laser shield opt-out inherits Energy 2x independently");
        inherited.OverrideHullCoefficient = false;
        Require(Mathf.IsEqualApprox(DamageResolver.Resolve(inherited.CreateHit(12), 0, 60).Health, 49.8f),
            "Laser hull opt-out inherits Energy0.85x");
        inherited.OverrideShieldCoefficient = true; inherited.ShieldCoefficient = 0;
        Require(DamageResolver.Resolve(inherited.CreateHit(12), 100, 60) == new DamageResult(100, 60), "Laser explicit zero remains an override");
        BeamStyle authored = GD.Load<BeamStyle>("res://TOWERS/Offensive/LaserTower/Beam/DefaultLaserBeamStyle.tres");
        Require(authored.CoreWidth == 3 && authored.PulseAmount == 0.06f, "Visual fixture edits preserve shared authored style");
        hud.GetBuildButton("battle").EmitSignal(Button.SignalName.Pressed); placement.Call("CancelPlacement"); hud.ClearInspection();
        wallet.Initialize(grid.Level.Settings);
        GD.Print("PASS: Laser catalog/paid upgrade, true beam DPS30/60/120Hz, alignment/range/disable/arrival/death gates, endpoints and visual independence");
    }
    // =========================================================
    // Create an isolated production Laser instance with a stationary up-facing fixture lock.
    private static LaserTower Create(TowerDefinition definition, Node parent, Vector2 position)
    {
        LaserTower laser = definition.Scene.Instantiate<LaserTower>(); laser.InitializeDefinition(definition);
        laser.Config.TargetPriority = TowerTargeting.Priority.Closest;
        parent.AddChild(laser); laser.GlobalPosition = position; laser.SetPhysicsProcess(false);
        laser.Config.AimToleranceDegrees = 180;
        // Rate parity starts with acquisition due immediately; production startup staggering is preserved.
        typeof(LaserTower).GetField("_searchTimer", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(laser, 0f);
        return laser;
    }
    // =========================================================
    // Count real nodes to detect accidental line/projectile allocation during active ticks.
    private static int Count(Node root) { int result = 1; foreach (Node child in root.GetChildren()) result += Count(child); return result; }
    // =========================================================
    // Fail the headless integration checkpoint on sustained-attack regressions.
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
