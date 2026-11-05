// SE2 focused cone scanning: real wave flags, indexed reveal/overlap/lifecycle and aimed-weapon detection gates.
using Godot;
using System;
using System.Linq;
using System.Reflection;
using static AmmoLoopFixture;
using static WaveBuilder;

public partial class StealthChecks : Node
{
    #region Runner
    // =========================================================
    // Use a private open terrain fixture; source maps and authored wave balance are never changed.
    public override async void _Ready()
    {
        try
        {
            using (AmmoLoopFixture f = new(this)) Check(f);
            GetTree().Paused = false;
            await SubsystemBattlefield.DrainFreedFixtures(this);
            GD.Print("PASS: stealth wave flags, visuals/status composition, detection/locks, directional stationary sweeps, stable overlaps, lifecycle and aimed weapons");
            GetTree().Quit();
        }
        catch (Exception error) { GD.PushError(error.ToString()); GetTree().Paused = false; GetTree().Quit(1); }
    }
    #endregion

    #region Spawn and Reveal
    // =========================================================
    // Exercise the same wave builder, scheduler and spawn initialization used by production levels.
    private void Check(AmmoLoopFixture f)
    {
        var grid = f.Spawns.Paths.Navigation.Grid;
        grid.Level.Passable.Clear(); grid.Level.Impassable.Clear(); grid.Level.Interactive.Clear();
        for (int y = 0; y < grid.Config.HeightCells; y++) for (int x = 0; x < grid.Config.WidthCells; x++) grid.Level.Passable.SetCell(new(x,y), 0, new(2,0));
        grid.RebuildTerrain(); f.Spawns.Paths.Navigation.Refresh();
        f.Wallet.Grant(f.Wallet.CreditsResource, 10000);
        var waves = f.Main.GetNode<WaveManager>("Waves");
        waves.Configure(new[] { Wave(1, Batch(1, 0, 0, Enemy("basic_mk1", shield: true, stealth: true))) }, f.Spawns.Catalog.ToSceneMap());
        Require(waves.StartWave(), "Stealth wave starts");
        Enemy hidden = f.Spawns.Registry.Living.Single(); Disable(hidden);
        Require(hidden.HasStealth && hidden.IsEffectivelyStealthed && hidden.CurrentShield > 0, "Shield and stealth flags coexist on actual wave spawn");
        Sprite2D sprite = hidden.GetNode<Sprite2D>("Sprite");
        Near(sprite.Modulate.A, .3f, "Hidden ship alpha");
        Near(hidden.GetNode<DefenseBar>("DefenseBar").Modulate.A, .3f, "Defense feedback dims with ship");
        Near(hidden.GetNode<ShieldVisual>("ShieldVisual").Modulate.A, .3f, "Shield aura dims with ship");
        Enemy normal = f.Target(new(700,700));
        Require(!normal.HasStealth && !normal.IsEffectivelyStealthed, "Ordinary spawn has no stealth allocation");
        Require(f.Hud.GetBuildCategoryButton("Utility") != null, "Registered beacon exposes the Utility category");
        f.Hud.GetBuildCategoryButton("Utility").EmitSignal(Button.SignalName.Pressed);
        StealthBeacon beacon = f.Purchase<StealthBeacon>("stealth_beacon", false);
        Vector2 center = beacon.GlobalPosition;
        hidden.GlobalPosition = center + Vector2.Up * 100; normal.GlobalPosition = center + new Vector2(180,0);
        Require(hidden.IsEffectivelyStealthed && beacon.IsBusy, "Unfinished paid beacon does not reveal");
        Require(beacon.Levels.Count == 3, "Stealth Beacon has three purchased tiers");
        Near(beacon.Config.ScanRotationSeconds, 4.5f, "Level 1 scan rotates more slowly");
        Near(beacon.ResolveLevel(1).ScanConeDegrees, 70f, "Level 1 full cone");
        Near(beacon.ResolveLevel(2).ScanConeDegrees, 80f, "Level 2 expanded full cone");
        Near(beacon.ResolveLevel(3).ScanConeDegrees, 90f, "Level 3 expanded full cone");
        Near(beacon.ResolveLevel(2).RangePixels, 280f, "Level 2 range");
        Near(beacon.ResolveLevel(3).RangePixels, 304f, "Level 3 range");
        float unfinishedHeading = beacon.GetNode<Node2D>("TurretPivot").Rotation;
        beacon._PhysicsProcess(.75);
        Require(beacon.GetNode<Node2D>("TurretPivot").Rotation == unfinishedHeading
            && !beacon.GetNode<Node2D>("TurretPivot/ScanCone").Visible, "Initial construction has no rotation or visible cone");
        Require(!TowerDetection.CanDetect(hidden) && TowerDetection.CanDetect(hidden, true), "Specialist override does not globally reveal");
        foreach (var mode in Enum.GetValues<TowerTargeting.Priority>())
            Require(TowerTargeting.Find(f.Spawns.Registry, center, 256, mode) != hidden, "Hidden enemy excluded before priority scoring");
        Require(TowerTargeting.Find(f.Spawns.Registry, center, 256, TowerTargeting.Priority.Closest,
            preference: TowerTargeting.Preference.Shielded) == normal, "Hidden shield preference cannot bypass detection");
        Require(TowerTargeting.Find(f.Spawns.Registry, center, 256, TowerTargeting.Priority.Closest, detectsStealth: true) == hidden,
            "Specialist acquisition finds closest hidden ship");
        ConstructionTestClock.FinishWork(f.Main);
        Require(!hidden.IsEffectivelyStealthed && hidden.Stealth.RevealCount == 1, "Completing construction reveals existing in-cone ship");
        Near(sprite.Modulate.A, 1, "Revealed ship alpha");
        var pivot = beacon.GetNode<Node2D>("TurretPivot");
        var cone = beacon.GetNode<Node2D>("TurretPivot/ScanCone");
        var sector = cone.GetNode<Polygon2D>("Sector");
        var scanSystem = f.Spawns.Registry.GetNode<StealthRevealSystem>("StealthReveals");
        Require(cone.Visible && sector.Polygon.Length == 26, "Prepared cone owns one bounded sector polygon");
        Near(sector.Color.A, .08f, "Default faint scan opacity");
        hidden.GlobalPosition = center + Vector2.Down * 100;
        Require(hidden.IsEffectivelyStealthed, "Behind scanner is not revealed");
        hidden.GlobalPosition = center + Vector2.Right;
        Require(hidden.IsEffectivelyStealthed, "No passive close-range detection bubble");
        hidden.GlobalPosition = center + Vector2.Up * 100;
        int transitions = 0; Action changed = () => transitions++;
        hidden.Stealth.Changed += changed;
        int samples = scanSystem.DirectionUpdates;
        beacon._PhysicsProcess(.11);
        Require(scanSystem.DirectionUpdates == samples + 1 && hidden.Stealth.RevealCount == 1 && transitions == 0,
            "In-place direction update preserves source identity and retained reveal without hide/reveal flicker");
        beacon._PhysicsProcess(.64);
        Require(hidden.IsEffectivelyStealthed && transitions == 1 && !TowerTargeting.Valid(hidden, center, 256),
            "Actual head sweep restores stealth for stationary target and invalidates cached lock");
        beacon._PhysicsProcess(beacon.Config.ScanRotationSeconds - .75f);
        Require(!hidden.IsEffectivelyStealthed && transitions == 2, "Returning scan reveals stationary enemy again");
        hidden.Stealth.Changed -= changed;
        Require((beacon.ScanHeading - Vector2.Up).Length() < .001f, "Head and sampled heading complete one clockwise revolution");
        Enemy newcomer = f.Target(center + Vector2.Up * 50);
        newcomer.Initialize(f.Spawns.Registry, f.Spawns.Paths, spawnWithStealth: true);
        Require(newcomer.Stealth.RevealCount == 1, "A new stealth spawn inside existing coverage is immediately revealed");
        EnemyStealthState removedState = newcomer.Stealth;
        newcomer.TakeDamage(99999);
        Require(removedState.RevealCount == 0, "Enemy death releases its reveal handles immediately");
        Require(TowerTargeting.Valid(hidden, center, 256), "Revealed cached target is eligible");
        hidden.ApplyStatus(new(StatusEffectId.Slow, .67f));
        hidden.ApplyStatus(new(StatusEffectId.Freeze, 0, 3));
        Color statusTint = sprite.SelfModulate;
        hidden.GlobalPosition = center + Vector2.Up * 256;
        Require(!hidden.IsEffectivelyStealthed, "Inclusive exact radius boundary");
        hidden.GlobalPosition = center + Vector2.Up * 257;
        Require(hidden.IsEffectivelyStealthed && !TowerTargeting.Valid(hidden, center, 500), "Leaving range immediately invalidates cached lock");
        Require(sprite.SelfModulate == statusTint, "Stealth alpha preserves concurrent Cryo/Stasis tint"); Near(sprite.Modulate.A, .3f, "Leaving restores hidden alpha");
        hidden.GlobalPosition = center + Vector2.Up * 100;
        StealthBeacon second = f.Add<StealthBeacon>("stealth_beacon", center);
        Require(hidden.Stealth.RevealCount == 2, "Overlapping beacons own independent handles: " + hidden.Stealth.RevealCount + " / " + second.DescribeRuntime() + " / " + second.GlobalPosition + " vs " + hidden.GlobalPosition + " / " + f.Spawns.Registry.GetNode<StealthRevealSystem>("StealthReveals").ActiveBeacons);

        beacon._PhysicsProcess(.75);
        Require(hidden.Stealth.RevealCount == 1 && !hidden.IsEffectivelyStealthed, "Cone rotating away removes only its own overlapping source");
        beacon._PhysicsProcess(beacon.Config.ScanRotationSeconds - .75f);
        var disabled = beacon.ApplyDisable(TowerDisableReason.EMP);
        Require(!hidden.IsEffectivelyStealthed && hidden.Stealth.RevealCount == 1, "Disabling one overlap retains other reveal");
        float stopped = pivot.Rotation; samples = scanSystem.DirectionUpdates;
        beacon._PhysicsProcess(.75);
        Require(pivot.Rotation == stopped && !cone.Visible && !beacon.IsPhysicsProcessing() && scanSystem.DirectionUpdates == samples,
            "Disabled scan/head/cone sleep without a detection tick");
        second.ApplyDamage(new DamageInfo(99999, DamageType.Kinetic));
        Require(hidden.IsEffectivelyStealthed && hidden.Stealth.RevealCount == 0, "Destroying last active source immediately restores stealth");
        beacon.RemoveDisable(disabled); Require(!hidden.IsEffectivelyStealthed, "Re-enable reveals stationary enemy");
        GetTree().Paused = true;
        stopped = pivot.Rotation; samples = scanSystem.DirectionUpdates;
        beacon._PhysicsProcess(.75);
        Require(!hidden.IsEffectivelyStealthed && pivot.Rotation == stopped && scanSystem.DirectionUpdates == samples,
            "Pause freezes visual heading and sampled membership"); GetTree().Paused = false;
        beacon.Config.RevealRange = 64; Require(hidden.IsEffectivelyStealthed, "Shrinking Inspector range releases existing target");
        beacon.Config.RevealRange = 256; Require(!hidden.IsEffectivelyStealthed, "Expanding range reveals stationary target");
        hidden.GlobalPosition = center + Vector2.Up.Rotated(Mathf.DegToRad(40)) * 100;
        Require(hidden.IsEffectivelyStealthed, "Outside default 35 degree half-angle is hidden");
        beacon.Config.ScanConeDegrees = 90; Require(!hidden.IsEffectivelyStealthed, "Cone angle edit resamples stationary enemy");
        beacon.Config.ScanConeDegrees = 70; Require(hidden.IsEffectivelyStealthed, "Restored cone removes out-of-angle member");
        beacon.Config.ScanConeOpacity = .12f; Near(sector.Color.A, .12f, "Live opacity edit updates existing polygon");
        beacon.Config.ScanRotationSeconds = 0;
        Require(!beacon.IsPhysicsProcessing() && !cone.Visible && scanSystem.ActiveBeacons == 0, "Invalid cadence safely disables only source");
        beacon.Config.ScanRotationSeconds = 6;
        hidden.GlobalPosition = center + Vector2.Up * 100;
        beacon._PhysicsProcess(.5);
        Near(pivot.Rotation, Mathf.DegToRad(30), "Edited revolution changes angular speed");
        beacon.Config.ScanRotationSeconds = 3;
        beacon._PhysicsProcess(2.75);
        beacon.Config.ScanUpdateIntervalSeconds = .2f;
        beacon._PhysicsProcess(.2);
        Require(!hidden.IsEffectivelyStealthed, "Edited sample interval keeps retained target inside actual cone");
        beacon.Config.ScanUpdateIntervalSeconds = .1f;
        beacon._PhysicsProcess(2.8);
        beacon.GlobalPosition += new Vector2(1000,0);
        Require(hidden.IsEffectivelyStealthed, "Source teleport removes old members absent from new spatial query");
        hidden.GlobalPosition = beacon.GlobalPosition + Vector2.Up * 100;
        Require(!hidden.IsEffectivelyStealthed, "Moving enemy enters relocated cone");
        beacon.GlobalPosition = hidden.GlobalPosition - Vector2.Up * 100;
        Require(!hidden.IsEffectivelyStealthed, "Idempotent source placement preserves reveal");
        hidden.GlobalPosition = center + Vector2.Up * 100;
        beacon.GlobalPosition = center;
        Require(!hidden.IsEffectivelyStealthed, "Source teleport picks up stationary enemy at destination");
        LaserTower cached = f.Add<LaserTower>("laser", center);
        typeof(LaserTower).GetMethod("SetTarget", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(cached, new object[] { hidden });
        int rounds = cached.Ammunition.Inventory.Amount; float hull = hidden.Health, shield = hidden.CurrentShield;
        beacon._PhysicsProcess(.75); cached._PhysicsProcess(.001);
        Require(hidden.IsEffectivelyStealthed && hidden.Health == hull && hidden.CurrentShield == shield && cached.Ammunition.Inventory.Amount == rounds,
            "Last cone rotating away prevents cached Laser damage and ammo use");
        cached.Free(); beacon._PhysicsProcess(2.25);
        beacon.GlobalPosition += new Vector2(1000,0); Require(hidden.IsEffectivelyStealthed, "Moving source releases old coverage");
        var system = f.Spawns.Registry.GetNode<StealthRevealSystem>("StealthReveals");
        Require(system.ActiveBeacons == 1, "Destroyed source is removed from index immediately");
        f.Towers.RemoveChild(beacon); beacon.Free(); Require(system.ActiveBeacons == 0, "Removal leaves no registered source");
        StealthBeacon teardown = f.Add<StealthBeacon>("stealth_beacon", center);
        Require(!hidden.IsEffectivelyStealthed, "Fresh source reveals before index teardown");
        f.Spawns.Registry.RemoveChild(system);
        Require(hidden.IsEffectivelyStealthed && !teardown.IsPhysicsProcessing()
            && !teardown.GetNode<Node2D>("TurretPivot/ScanCone").Visible, "Index subtree unload releases handles and sleeps scanners");
        system.Free(); teardown.Free();
        hidden.Initialize(f.Spawns.Registry, f.Spawns.Paths, spawnWithStealth: true);
        CheckWeapons(f, hidden, normal, center);
        hidden.Initialize(f.Spawns.Registry, f.Spawns.Paths);
        Require(!hidden.HasStealth && hidden.Stealth == null, "Reinitialization clears stale capability and handles"); Near(sprite.Modulate.A, 1, "Reused ordinary ship restores original opacity");
    }
    #endregion

    #region Weapon Integration
    // =========================================================
    // Hidden-only encounters cannot spend ammo/fire; direct tower detection is private per placed config.
    private void CheckWeapons(AmmoLoopFixture f, Enemy hidden, Enemy normal, Vector2 center)
    {
        normal.GlobalPosition = center + new Vector2(4000,0); hidden.GlobalPosition = center + new Vector2(100,0);
        // Use catalog IDs by scene type to keep this check independent of descriptive folder names.
        Type[] types = { typeof(BattleTower), typeof(BlasterCannon), typeof(RocketTower), typeof(LaserTower), typeof(SniperTower), typeof(MachineGunTower), typeof(CryoTower), typeof(StasisTower) };
        foreach (Type type in types)
        {
            var definition = f.Placement.Catalog.Definitions.First(d => { Node n = d.Scene.Instantiate(); bool match = n.GetType() == type; n.Free(); return match; });
            TowerBase tower = f.Add<TowerBase>(definition.TowerId, center, config => config.Power = null); // Isolate detection from PW4 charge.
            int ammo = tower.Ammunition?.Inventory.Amount ?? 0; int shots = f.Main.GetNode("World/Projectiles").GetChildCount();
            float health = hidden.Health;
            tower.Call("_physics_process", 1.0);
            Require((tower.Ammunition?.Inventory.Amount ?? 0) == ammo && f.Main.GetNode("World/Projectiles").GetChildCount() == shots && hidden.Health == health,
                type.Name + " neither fires nor spends ammo at a hidden-only target");
            Require(!tower.AuthoredConfig.CanDetectStealth, "Existing tower defaults to ordinary detection");
            if (tower is LaserTower)
            {
                typeof(LaserTower).GetMethod("SetTarget", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(tower, new object[] { hidden });
                tower.Call("_physics_process", 0.001);
                Require(hidden.Health == health && (tower.Ammunition?.Inventory.Amount ?? 0) == ammo,
                    "Cached Laser lock drops damage/ammo immediately after losing detection");
            }
            tower.AuthoredConfig.CanDetectStealth = true;
            Require(TowerDetection.CanDetect(hidden, tower.AuthoredConfig.CanDetectStealth) && hidden.IsEffectivelyStealthed,
                "Private specialist config detects without revealing to other towers");
            if (tower is BattleTower)
            {
                var stats = tower.AuthoredConfig.Levels[0].Stats;
                tower.AuthoredConfig.SetLevelStats(1, stats with { AimToleranceDegrees = 180 });
                tower.Call("_physics_process", 1.0);
                Require(f.Main.GetNode("World/Projectiles").GetChildCount() > shots,
                    "A specialist Battle Tower actually fires at an unrevealed stealth enemy");
                f.ClearProjectiles();
                Require(!f.Placement.Catalog.Find(definition.TowerId).DefaultConfig.CanDetectStealth,
                    "Specialist override remains private to this placed config");
            }
            f.Towers.RemoveChild(tower); tower.Free();
        }
    }
    // =========================================================
    // Floating-point presentation assertions are independent of rendering and fonts.
    private static void Near(float a, float b, string message) => Require(Math.Abs(a-b) < .001f, message);
    #endregion
}
