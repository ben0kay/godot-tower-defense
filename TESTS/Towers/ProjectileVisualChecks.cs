// Real funded firing/origins, visual isolation, guided heading and bounded presentation regressions.
using Godot;
using System;
using System.Linq;
using System.Diagnostics;
using static AmmoLoopFixture;

public partial class ProjectileVisualChecks : Node
{
    // =========================================================
    // Actual production world/services; test-only clocks do not alter authored balance or source assets.
    public override async void _Ready()
    {
        try
        {
            using (AmmoLoopFixture f = new(this))
            {
                SubsystemBattlefield.DetachSession(f.Main);
                Run(f);
                MultiBarrelTowerChecks.Run(f.Main, f.Spawns);
                CryoTowerChecks.Run(f);
            }
            await SubsystemBattlefield.DrainFreedFixtures(this);
            GD.Print("PASS: PV1/PV2 real tracers/muzzle origins, funded A-B volleys, snapshots, homing, Sniper red aim, bounded idle renderer and gameplay parity");
            GetTree().Quit();
        }
        catch (Exception error) { GD.PushError(error.ToString()); GetTree().Paused = false; GetTree().Quit(1); }
    }

    // =========================================================
    // Shared focused group is also called by the existing integration runner.
    internal static void Run(AmmoLoopFixture f)
    { Battle(f); Gun(f); Sniper(f); Snapshots(f); Lifecycle(f); Blaster(f); Renderer(f); }

    // =========================================================
    // Fired bullets outlive their source; missing/disabled visuals cannot change timeout or dead-target cleanup.
    private static void Lifecycle(AmmoLoopFixture f)
    {
        BattleTower tower = f.Add<BattleTower>("battle", new(2000, 2064));
        Enemy enemy = f.Target(new(2000, 2000)); tower._PhysicsProcess(.25);
        BasicProjectile shot = Shot(f); Disable(shot); float hp = enemy.Health;
        tower.Free(); shot._PhysicsProcess(.1); shot._PhysicsProcess(.1);
        Near(enemy.Health, hp - 30, "Removing source preserves one launched hit"); f.ClearProjectiles();
        using ProjectileVisualStyle hidden = new() { Enabled = false };
        shot = new(); shot.Initialize(enemy, 1, new DamageInfo(5, DamageType.Kinetic), visual: hidden);
        f.Main.GetNode("World/Projectiles").AddChild(shot); shot.LaunchFrom(enemy.GlobalPosition); Disable(shot);
        hp = enemy.Health; shot._PhysicsProcess(.01); shot._PhysicsProcess(.01);
        Near(enemy.Health, hp - 5, "Disabled presentation still applies damage exactly once"); f.ClearProjectiles();
        shot = new(); shot.Initialize(enemy, 1, 5); f.Main.GetNode("World/Projectiles").AddChild(shot);
        shot.LaunchFrom(enemy.GlobalPosition - new Vector2(1000, 0)); Disable(shot); hp = enemy.Health;
        shot._PhysicsProcess(5); Require(shot.IsQueuedForDeletion() && enemy.Health == hp && shot.VisualSnapshot == null,
            "Legacy fallback retains five-second expiry without damage"); f.ClearProjectiles();
        shot = new(); shot.Initialize(enemy, 100, new DamageInfo(5, DamageType.Kinetic), handling: new(0, ProjectileGuidance.Homing)); f.Main.GetNode("World/Projectiles").AddChild(shot); Disable(shot);
        enemy.Free(); shot._PhysicsProcess(.01); Require(shot.IsQueuedForDeletion(), "Removed targets clean up safely"); f.ClearProjectiles();
    }

    // =========================================================
    // Every flash is a draw slot, not a new node; no-ammo/cooldown/invalid/nonoperational calls do nothing.
    private static void Battle(AmmoLoopFixture f)
    {
        BattleTower tower = f.Add<BattleTower>("battle", new(2000, 2064));
        Enemy enemy = f.Target(new(2000, 2000));
        Marker2D muzzle = tower.GetNode<Marker2D>(BattleTower.MuzzlePath);
        var renderer = tower.GetNode<WeaponVisualRenderer>("TurretPivot/WeaponVisuals");
        Require(tower.ValidateScene() && !renderer.IsProcessing() && renderer.ActiveFlashes == 0, "Real Battle muzzle contract and idle renderer");
        int nodes = CountNodes(tower); TowerFinalStats stats = tower.FinalStats;
        tower._PhysicsProcess(.25);
        BasicProjectile shot = Shot(f);
        Require(shot.GlobalPosition == muzzle.GlobalPosition && shot.GlobalPosition != tower.GlobalPosition
            && shot.VisualSnapshot.HasValue && tower.Ammunition.ConsumedRounds == 1
            && renderer.TotalFlashes == 1 && renderer.ActiveFlashes == 1 && CountNodes(tower) == nodes, "One funded Battle launch at barrel tip, one draw-slot flash");
        tower._PhysicsProcess(.001); Require(renderer.TotalFlashes == 1, "Cooldown cannot flash again");
        float health = enemy.Health;
        shot._PhysicsProcess(.1); shot._PhysicsProcess(.1);
        Near(enemy.Health, health - stats.DamagePerShot, "Muzzle move retains exactly one Kinetic hit");
        f.ClearProjectiles();
        tower.Ammunition.Inventory.TryConsume(tower.Ammunition.Inventory.Amount); tower._PhysicsProcess(1);
        Require(renderer.TotalFlashes == 1 && f.Main.GetNode("World/Projectiles").GetChildCount() == 0, "Unfunded shot creates no bullet or flash");
        enemy.Free(); tower._PhysicsProcess(1); Require(renderer.TotalFlashes == 1, "Invalid target cannot flash");
        tower.Free();
        TowerDefinition definition = f.Placement.Catalog.Find("battle");
        BattleTower site = definition.Scene.Instantiate<BattleTower>(); site.InitializeDefinition(definition);
        site.StartConstruction(f.Main.GetNode<ConstructionWorkManager>("World/ConstructionWork")); f.Towers.AddChild(site); Disable(site);
        site._PhysicsProcess(1);
        Require(site.GetNode<WeaponVisualRenderer>("TurretPivot/WeaponVisuals").TotalFlashes == 0, "Construction gates all firing presentation");
        site.Free();
    }

    // =========================================================
    // The real selector drives both bullet and flash positions, including atomic simultaneous funding.
    private static void Gun(AmmoLoopFixture f)
    {
        MachineGunTower gun = f.Add<MachineGunTower>("machine_gun", new(2000, 2064));
        Enemy enemy = f.Target(new(2000, 2000)); gun.Config.TargetPriority = TowerTargeting.Priority.Closest;
        var renderer = gun.GetNode<WeaponVisualRenderer>("TurretPivot/WeaponVisuals");
        int nodes = CountNodes(gun); Require(Mathf.IsEqualApprox(gun.FinalStats.ShotsPerSecond, 14.4f), "Cadence is 14.4 total bullets/s after 20% increase");
        for (int i = 0; i < 10; i++)
        {
            gun._PhysicsProcess(.1); BasicProjectile shot = Shot(f);
            Marker2D marker = gun.GetNode<Marker2D>("TurretPivot/RecoilRoot/Muzzles/" + (i % 2 == 0 ? "MuzzleA" : "MuzzleB"));
            Require(shot.GlobalPosition.IsEqualApprox(marker.GlobalPosition)
                && renderer.ToGlobal(renderer.LastFlashPosition).IsEqualApprox(marker.GlobalPosition), "Bullet and flash share selected A-B marker " + i);
            Require(renderer.TotalFlashes == i + 1 && gun.Ammunition.ConsumedRounds == i + 1, "One flash and one ammo charge per launch");
            f.ClearProjectiles(); renderer._Process(1); renderer._Process(1);
        }
        gun.Config.FiringMode = BarrelFireMode.Simultaneous; gun._PhysicsProcess(.1);
        Require(f.Main.GetNode("World/Projectiles").GetChildCount() == 2 && renderer.TotalFlashes == 12
            && gun.Ammunition.ConsumedRounds == 12 && renderer.ActiveFlashes == 2 && CountNodes(gun) == nodes, "Simultaneous two funded muzzles produce two slots and bullets");
        f.ClearProjectiles(); renderer._Process(1); renderer._Process(1);
        gun.Ammunition.Inventory.TryConsume(gun.Ammunition.Inventory.Amount - 1); gun._PhysicsProcess(.5);
        Require(renderer.TotalFlashes == 12 && gun.Ammunition.Inventory.Amount == 1 && !renderer.IsProcessing(), "Unfunded simultaneous volley is completely silent and idle");
        gun.Free(); enemy.Free();
    }

    // =========================================================
    // Thin red aim follows the rotating barrel; target loss, disabled style/ammo and construction hide it.
    private static void Sniper(AmmoLoopFixture f)
    {
        SniperTower tower = f.Add<SniperTower>("sniper", new(2000, 2064));
        tower.Config.TargetPriority = TowerTargeting.Priority.Closest;
        var renderer = tower.GetNode<WeaponVisualRenderer>("TurretPivot/WeaponVisuals");
        Require(!renderer.AimVisible && tower.Config.AimLaser.Color.R > tower.Config.AimLaser.Color.G
            && tower.Config.AimLaser.WidthPixels < 1, "Sniper starts idle with subtle red preset");
        Enemy enemy = f.Target(new(2000, 2000)); tower._PhysicsProcess(.25);
        Marker2D muzzle = tower.GetNode<Marker2D>("TurretPivot/RecoilRoot/Muzzles/MuzzleA");
        Require(renderer.AimVisible && renderer.ToGlobal(renderer.AimStart).IsEqualApprox(muzzle.GlobalPosition), "Aim begins at actual sniper muzzle");
        Vector2 direction = (renderer.ToGlobal(renderer.AimEnd) - muzzle.GlobalPosition).Normalized();
        Require(direction.Dot(-muzzle.GlobalTransform.Y.Normalized()) > .999f, "Aim follows barrel forward, with no doubled rotation");
        f.ClearProjectiles(); renderer._Process(1); renderer._Process(1); float before = enemy.Health;
        tower._PhysicsProcess(.01); Require(enemy.Health == before && !renderer.IsProcessing(), "Aim alone deals no damage and adds no independent processing");
        AimLaserStyle original = tower.Config.AimLaser;
        using (AimLaserStyle hiddenAim = new() { Enabled = false })
        {
            tower.Config.AimLaser = hiddenAim; tower._PhysicsProcess(.01);
            Require(!renderer.AimVisible && enemy.Health == before, "Disabled aiming preset is visual only");
            tower.Config.AimLaser = original;
        }
        enemy.GlobalPosition = new(2060, 2000); tower._PhysicsProcess(.02);
        Require(renderer.AimVisible && renderer.AimEnd.IsFinite(), "Tracking a moving target stays finite");
        enemy.Free(); tower._PhysicsProcess(.02); Require(!renderer.AimVisible, "Target removal clears aim next weapon tick");
        enemy = f.Target(new(2000, 2000)); tower._PhysicsProcess(.25);
        tower.Ammunition.Inventory.TryConsume(tower.Ammunition.Inventory.Amount); tower._PhysicsProcess(.01);
        Require(!renderer.AimVisible, "No ammo hides aim");
        tower.Free(); enemy.Free(); f.ClearProjectiles();
    }

    // =========================================================
    // Launch values survive source edits/free; tracer length does not change physical contact distance.
    private static void Snapshots(AmmoLoopFixture f)
    {
        Enemy enemy = f.Target(new(2000, 2000));
        using ProjectileVisualStyle style = new() { LengthPixels = 100, CoreColor = Colors.Red };
        BasicProjectile shot = new(); shot.Initialize(enemy, 100, new DamageInfo(5, DamageType.Kinetic), visual: style, handling: new(0, ProjectileGuidance.Homing));
        f.Main.GetNode("World/Projectiles").AddChild(shot); shot.LaunchFrom(new(1800, 2000)); Disable(shot);
        var snapshot = shot.VisualSnapshot; style.LengthPixels = 1; style.CoreColor = Colors.Blue;
        float hp = enemy.Health; shot._PhysicsProcess(.01);
        Require(enemy.Health == hp && shot.TravelDirection.Dot(Vector2.Right) > .999f && shot.VisualSnapshot == snapshot, "Long visual does not increase collision reach; styles snapshot at launch");
        enemy.GlobalPosition = new(1801, 2200); shot._PhysicsProcess(.01);
        Require(shot.TravelDirection.Dot(Vector2.Down) > .99f && shot.GlobalTransform.X.Normalized().Dot(shot.TravelDirection) > .999f,
            "Homing geometry follows actual new motion heading without repeated draw allocation");
        shot.GlobalPosition = enemy.GlobalPosition; shot._PhysicsProcess(.01); Require(shot.TravelDirection.IsFinite(), "Zero-distance impact stays finite");
        f.ClearProjectiles(); enemy.Free();
        foreach (string id in new[] { "battle", "machine_gun", "sniper", "cryo" })
        {
            var authored = (IProjectileVisualConfig)f.Placement.Catalog.Find(id).DefaultConfig;
            var copy = (IProjectileVisualConfig)f.Placement.Catalog.Find(id).DefaultConfig.CreateInstanceCopy();
            Require(ReferenceEquals(authored.ProjectileVisual, copy.ProjectileVisual) && ReferenceEquals(authored.MuzzleFlash, copy.MuzzleFlash), "Read-only presentation presets shared without per-shot Resource duplicates");
        }
        using ProjectileVisualStyle invalid = new() { LengthPixels = float.NaN, CoreThicknessPixels = -1, GlowColor = new(float.NaN, 1, 1) };
        Require(float.IsFinite(invalid.Capture().Length) && invalid.Capture().Width > 0 && invalid.Capture().Glow.R == 0, "Invalid visual edits sanitize safely");
    }

    // =========================================================
    // Splash launches from its barrel too, while damage remains deferred until the single instant impact.
    private static void Blaster(AmmoLoopFixture f)
    {
        BlasterCannon tower = f.Add<BlasterCannon>("blaster_cannon", new(2000, 2128));
        tower.Config.TargetPriority = TowerTargeting.Priority.Closest;
        Enemy enemy = f.Target(new(2000, 2000)); float hp = enemy.Health;
        tower._PhysicsProcess(.25);
        SplashProjectile shot = f.Main.GetNode("World/Projectiles").GetChildren().OfType<SplashProjectile>().Single();
        Marker2D muzzle = tower.GetNode<Marker2D>("TurretPivot/RecoilRoot/Muzzles/MuzzleA");
        var renderer = tower.GetNode<WeaponVisualRenderer>("TurretPivot/WeaponVisuals");
        Require(tower.ValidateScene() && shot.GlobalPosition.IsEqualApprox(muzzle.GlobalPosition)
            && shot.GlobalPosition != tower.GlobalPosition && renderer.TotalFlashes == 1 && enemy.Health == hp,
            "Blaster projectile/flash share real barrel tip, without launch damage");
        tower._PhysicsProcess(.001); Require(renderer.TotalFlashes == 1, "Blaster cooldown creates no extra flash");
        tower.Free(); f.ClearProjectiles(); enemy.Free();
    }

    // =========================================================
    // Warmed renderer operations allocate no managed memory, and capacity/processing remain bounded.
    private static void Renderer(AmmoLoopFixture f)
    {
        SniperTower tower = f.Add<SniperTower>("sniper", new(2000, 2000));
        var renderer = tower.GetNode<WeaponVisualRenderer>("TurretPivot/WeaponVisuals");
        Marker2D muzzle = tower.GetNode<Marker2D>("TurretPivot/RecoilRoot/Muzzles/MuzzleA");
        var style = tower.Config.MuzzleFlash;
        int nodes = CountNodes(tower);
        for (int i = 0; i < 100; i++) renderer.FlashAt(muzzle, style);
        Require(renderer.ActiveFlashes == WeaponVisualRenderer.FlashCapacity && renderer.OverwrittenFlashes == 68 && CountNodes(tower) == nodes, "Burst overflow is bounded and creates no per-shot nodes");
        renderer._Process(1);
        Require(renderer.ActiveFlashes == WeaponVisualRenderer.FlashCapacity, "New flashes survive the first long process frame before presentation");
        Require(WeaponVisualRenderer.FlashOpacity(.03f, .16f) == 1 && WeaponVisualRenderer.FlashOpacity(.08f, .16f) > .7f,
            "Bright hold and linear fade retain visible intensity");
        renderer._Process(1); Require(renderer.ActiveFlashes == 0 && !renderer.IsProcessing(), "Expired flash state is cleared");
        using (MuzzleFlashStyle editable = new() { CoreColor = Colors.Red })
        {
            renderer.FlashAt(muzzle, editable); var captured = renderer.LastFlashStyle;
            editable.CoreColor = Colors.Blue; editable.DurationSeconds = 1;
            Require(renderer.LastFlashStyle == captured, "Active flash retains copied appearance after resource edit");
            renderer._Process(1); renderer._Process(1);
        }
        for (int i = 0; i < 100; i++) { renderer.FlashAt(muzzle, style); renderer._Process(1); renderer._Process(1); }
        Stopwatch watch = new(); long before = GC.GetAllocatedBytesForCurrentThread(); watch.Start();
        for (int i = 0; i < 10000; i++) { renderer.FlashAt(muzzle, style); renderer._Process(1); renderer._Process(1); }
        watch.Stop(); long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Require(allocated == 0 && !renderer.IsProcessing(), "Warmed flash emissions/ticks allocate zero managed bytes");
        GD.Print($"PV BENCH: 10000 flash emit+expire operations: {watch.Elapsed.TotalMilliseconds:0.###}ms; {allocated} bytes; fixed slots={WeaponVisualRenderer.FlashCapacity}");
        tower.Free();
    }

    // =========================================================
    // Count owned nodes, never use scene searches in production firing code.
    private static int CountNodes(Node node) { int count = 1; foreach (Node child in node.GetChildren()) count += CountNodes(child); return count; }
    private static BasicProjectile Shot(AmmoLoopFixture f) => f.Main.GetNode("World/Projectiles").GetChildren().OfType<BasicProjectile>().Single();
    private static void Near(float a, float b, string message) => Require(Math.Abs(a - b) < .002f, message);
}
