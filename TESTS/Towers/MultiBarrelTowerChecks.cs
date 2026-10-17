// Pure barrel scheduling plus live new-tower catalog, projectile origin, ammo, progression and Kinetic damage checks.
using Godot;
using System;
using System.Linq;

public static class MultiBarrelTowerChecks
{
    private static readonly Vector2 Origin = new(2000, 2000);

    // =========================================================
    // Exercise reusable muzzle selection without requiring per-barrel scene nodes or timers.
    public static void Selector()
    {
        BarrelFireController selector = new(4);
        Require(selector.ShotsPerVolley(BarrelFireMode.Single) == 1 && selector.MuzzleIndex(BarrelFireMode.Single, 0) == 0,
            "Single always uses the first muzzle");
        for (int i = 0; i < 9; i++)
        {
            Require(selector.MuzzleIndex(BarrelFireMode.Sequential, 0) == i % 4, "N-muzzle sequential rotation");
            selector.Commit(BarrelFireMode.Sequential);
        }
        Require(selector.NextMuzzle == 1 && selector.ShotsPerVolley(BarrelFireMode.Simultaneous) == 4,
            "Simultaneous fires N muzzle shots in one group");
        for (int i = 0; i < 4; i++)
            Require(selector.MuzzleIndex(BarrelFireMode.Simultaneous, i) == i, "Simultaneous uses every unique muzzle");
        selector.Commit(BarrelFireMode.Simultaneous);
        Require(selector.NextMuzzle == 1, "Simultaneous does not advance the sequential cursor");
        try { _ = new BarrelFireController(0); throw new InvalidOperationException("Zero muzzles accepted."); }
        catch (ArgumentOutOfRangeException) { }
    }

    // =========================================================
    // Verify actual production scenes register, resolve ranked levels and fire from real marker positions.
    public static void Run(Node main, SpawnManager spawns)
    {
        Selector();
        TowerCatalog catalog = main.GetNode<TowerPlacementController>("TowerPlacement").Catalog;
        TowerDefinition sniperDef = catalog.Find("sniper"), gunDef = catalog.Find("machine_gun");
        Require(sniperDef != null && gunDef != null && sniperDef.Available && gunDef.Available
            && sniperDef.Category == TowerDefinition.TowerCategory.Offensive
            && gunDef.Category == TowerDefinition.TowerCategory.Offensive, "Both offensive towers registered");
        sniperDef.Validate(); gunDef.Validate();
        Require(sniperDef.DefaultConfig is SniperTowerConfig && gunDef.DefaultConfig is MachineGunTowerConfig,
            "Each new tower uses its own config and independent level/rank tables");

        SniperTowerConfig sniperAuthored = (SniperTowerConfig)sniperDef.DefaultConfig;
        MachineGunTowerConfig gunAuthored = (MachineGunTowerConfig)gunDef.DefaultConfig;
        Require(sniperAuthored.ResolveStats(1).DamagePerShot == 80
            && sniperAuthored.ResolveStats(2).RangePixels == 524 && sniperAuthored.Ranks[1].TotalXp == 1000,
            "Sniper complete levels and earned ranks resolve");
        Require(Mathf.IsEqualApprox(gunAuthored.ResolveStats(1).ShotsPerSecond, 14.4f) && gunAuthored.ResolveStats(3).DamagePerShot == 9
            && gunAuthored.Ranks[4].TotalXp == 15000
            && gunAuthored.ResolveStats(1).ProjectileSpeed == 1062.5f
            && gunAuthored.ResolveStats(2).ProjectileSpeed == 1062.5f
            && gunAuthored.ResolveStats(3).ProjectileSpeed == 1062.5f,
            "Machine Gun complete levels, 25% faster projectiles and earned ranks resolve");

        Node2D towers = main.GetNode<Node2D>("World/Towers");
        Node projectiles = main.GetNode("World/Projectiles");
        Enemy enemy = null;
        SniperTower sniper = null;
        MachineGunTower gun = null;
        try
        {
            AreaDamageChecks.Clear(spawns);
            FreeBullets(projectiles);
            enemy = CreateEnemy(main, spawns);
            sniper = sniperDef.Scene.Instantiate<SniperTower>();
            sniper.InitializeDefinition(sniperDef);
            towers.AddChild(sniper); sniper.SetPhysicsProcess(false);
            sniper.GlobalPosition = Origin + new Vector2(0, 64);
            sniper.Config.TargetPriority = TowerTargeting.Priority.Closest;
            Require(sniper.MuzzleCount == 1 && sniper.FinalStats.DamagePerShot == 80
                && sniper.Ammunition.Inventory.Amount == 600 && sniper.Config != sniperAuthored,
                "Sniper scene binds exactly one muzzle, private config and local ammunition");

            sniper._PhysicsProcess(.25);
            BasicProjectile shot = projectiles.GetChildren().OfType<BasicProjectile>().Single();
            Marker2D sniperMuzzle = sniper.GetNode<Marker2D>("TurretPivot/Muzzles/MuzzleA");
            Near(shot.GlobalPosition, sniperMuzzle.GlobalPosition, "Sniper launches from muzzle tip");
            Require(sniper.Ammunition.Inventory.Amount == 599, "Sniper consumes exactly one round on launch");
            shot.SetPhysicsProcess(false); shot._PhysicsProcess(.2);
            Require(!enemy.IsAlive, "Unshielded Enemy001 dies from one accurate Sniper shot");
            FreeBullets(projectiles); enemy.Free(); enemy = null;

            enemy = CreateEnemy(main, spawns);
            gun = gunDef.Scene.Instantiate<MachineGunTower>();
            gun.InitializeDefinition(gunDef);
            towers.AddChild(gun); gun.SetPhysicsProcess(false);
            gun.GlobalPosition = Origin + new Vector2(0, 64);
            gun.Config.TargetPriority = TowerTargeting.Priority.Closest;
            Require(gun.MuzzleCount == 2 && gun.Config.FiringMode == BarrelFireMode.Sequential
                && gun.Ammunition.Inventory.Amount == 1200 && gun.FinalStats.DamagePerShot == 7 && Mathf.IsEqualApprox(gun.FinalStats.ShotsPerSecond, 14.4f),
                "Machine Gun starts with two muzzles and a 14.4-bullets-per-second shared attack rate");
            Marker2D a = gun.GetNode<Marker2D>("TurretPivot/Muzzles/MuzzleA");
            Marker2D b = gun.GetNode<Marker2D>("TurretPivot/Muzzles/MuzzleB");

            gun._PhysicsProcess(.25);
            gun._PhysicsProcess(.2);
            gun._PhysicsProcess(.2);
            BasicProjectile[] alternating = projectiles.GetChildren().OfType<BasicProjectile>().ToArray();
            Require(alternating.Length == 3, "Three cadence ticks each launch one and only one projectile");
            Near(alternating[0].GlobalPosition, a.GlobalPosition, "First round leaves barrel A");
            Near(alternating[1].GlobalPosition, b.GlobalPosition, "Second round leaves barrel B");
            Near(alternating[2].GlobalPosition, a.GlobalPosition, "Third round wraps to barrel A");
            Require(gun.Ammunition.ConsumedRounds == 3, "Alternating fire charges one round per shot");

            FreeBullets(projectiles);
            gun.Config.FiringMode = BarrelFireMode.Simultaneous;
            gun._PhysicsProcess(.4);
            BasicProjectile[] volley = projectiles.GetChildren().OfType<BasicProjectile>().ToArray();
            Require(volley.Length == 2 && gun.Ammunition.ConsumedRounds == 5,
                "Simultaneous mode launches two paid projectiles per volley");
            Near(volley[0].GlobalPosition, a.GlobalPosition, "Simultaneous left muzzle");
            Near(volley[1].GlobalPosition, b.GlobalPosition, "Simultaneous right muzzle");
            FreeBullets(projectiles);

            int toConsume = gun.Ammunition.Inventory.Amount - 1;
            Require(gun.Ammunition.Inventory.TryConsume(toConsume), "Fixture leaves one round in reserve");
            gun._PhysicsProcess(.5);
            Require(!projectiles.GetChildren().OfType<BasicProjectile>().Any()
                && gun.Ammunition.Inventory.Amount == 1 && gun.Ammunition.ConsumedRounds == 5,
                "Insufficient ammo does not launch or partially charge a simultaneous volley");

            TowerRankController.AwardExperience(gun, 1000);
            Require(gun.CurrentRank.Rank == TowerRank.Veteran && gun.UpgradeLevel == 1
                && gun.FinalStats.RangePixels > 239, "Earned rank changes range without purchasing a level");
            Require(gun.ResolveLevel(2).DamagePerShot >= 8 && gun.Levels[1].Stats.DamagePerShot == 8
                && Mathf.IsEqualApprox(gun.Levels[1].Stats.ShotsPerSecond, 14.88f)
                && Mathf.IsEqualApprox(gun.Levels[2].Stats.ShotsPerSecond, 15.36f),
                "Upgrade previews retain gentle damage and real fractional rate increases");

            // Keep the target stationary and destroy each projectile before its next tick:
            // test the actual firing cadence without changing enemy health or tracking.
            Require(gun.Ammunition.Inventory.TryAdd(1199), "Cadence fixture refills the depleted reserve");
            gun.Config.FiringMode = BarrelFireMode.Sequential;
            foreach (float rate in new[] { 14.4f, 14.88f, 15.36f })
            {
                gun.Config.ShotsPerSecond = rate;
                long beforeShots = gun.Ammunition.ConsumedRounds;
                for (int tick = 0; tick < 600; tick++)
                {
                    gun._PhysicsProcess(1.0 / 60);
                    FreeBullets(projectiles);
                }
                long fired = gun.Ammunition.ConsumedRounds - beforeShots;
                Require(Mathf.Abs(fired - rate * 10f) <= 2f,
                    $"Ten simulated seconds at 60 Hz preserve {rate} bullets/s (fired {fired})");
            }
            GD.Print("PASS: new Sniper/Machine Gun catalog, complete level/rank stats, one/two real muzzles, A-B-A, single-hit Sniper, paid simultaneous volley and ammo shortage");
        }
        finally
        {
            gun?.Free(); sniper?.Free(); enemy?.Free();
            FreeBullets(projectiles);
            AreaDamageChecks.Clear(spawns);
        }
    }

    // =========================================================
    // Stationary live Enemy001 fixture; no wave/editor spawn shortcut or authored resource mutation.
    private static Enemy CreateEnemy(Node main, SpawnManager spawns)
    {
        Enemy enemy = GD.Load<PackedScene>("res://ENEMIES/Types/Enemy001/Enemy001.tscn").Instantiate<Enemy>();
        main.GetNode("World/Enemies").AddChild(enemy); enemy.GlobalPosition = Origin;
        enemy.Initialize(spawns.Registry, spawns.Paths); enemy.Movement.SetPhysicsProcess(false);
        return enemy;
    }

    // =========================================================
    // Cleanup production projectile nodes without calling synthetic hit functions.
    private static void FreeBullets(Node projectiles)
    {
        foreach (BasicProjectile bullet in projectiles.GetChildren().OfType<BasicProjectile>().ToArray()) bullet.Free();
    }

    // =========================================================
    // Float positions of projectile origins match exact transformed muzzle marker positions.
    private static void Near(Vector2 actual, Vector2 expected, string message) =>
        Require(actual.DistanceTo(expected) < .05f, message + $" ({actual} != {expected})");

    // =========================================================
    // Fail fast on any attack scheduling, ammo, level, or spawn-origin regression.
    private static void Require(bool ok, string message)
    { if (!ok) throw new InvalidOperationException(message); }
}
