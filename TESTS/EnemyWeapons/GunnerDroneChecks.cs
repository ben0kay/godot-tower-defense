// EG3 integration: compact starter Gunner, F2/catalog, split hull/gun, accepted-shot flashes and tower damage.
using Godot;
using System;
using System.Linq;
using static AmmoLoopFixture;

public partial class GunnerDroneChecks : Node
{
    private const string WeaponPath = "Sprite/Weapons/Minigun";
    #region Runner and Catalog
    // =========================================================
    // Use real battlefields; no source wave edits, profile writes or fabricated enemy/tower health.
    public override async void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
        try
        {
            using (AmmoLoopFixture f = new(this))
            {
                SubsystemBattlefield.DetachSession(f.Main);
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                Definitions(f); CatalogCycle(f); Firefight(f); HeavyVariant(f); StealthAndFreeze(f); Overflow(f);
            }
            await SubsystemBattlefield.DrainFreedFixtures(this);
            GD.Print("PASS: EG3 Gunner Drone starter/heavy gunship art and projectiles, catalog cycle, muzzle snapshots, accepted-shot flashes and tower HP loss");
            GetTree().Quit();
        }
        catch (Exception error) { GetTree().Paused = false; GD.PushError(error.ToString()); GetTree().Quit(1); }
    }
    // =========================================================
    // The catalog has two opt-in gunships; all other ships remain unarmed and authored waves omit both.
    private static void Definitions(AmmoLoopFixture f)
    {
        var catalog = f.Spawns.Catalog; catalog.Rebuild();
        var map = catalog.ToSceneMap();
        Require(map.Count == 15 && catalog.Find("gunner_drone_mk1") != null && catalog.Find("heavy_gunner_drone_mk1") != null,
            "All 15 catalog entries include distinct starter and heavy gunner IDs");
        foreach (var pair in map)
        {
            Node ship = pair.Value.Instantiate();
            Require(HasWeapon(ship) == (pair.Key == "gunner_drone_mk1" || pair.Key == "heavy_gunner_drone_mk1"), "Weapon is opt-in only on the new ship: " + pair.Key);
            ship.Free();
        }
        foreach (var waves in new[] { Level001Waves.Create(), Level002Waves.Create(), Level020Waves.Create() })
            Require(waves.SelectMany(w => w.Batches).SelectMany(b => b.Entries).All(e => e.EnemyId != "gunner_drone_mk1" && e.EnemyId != "heavy_gunner_drone_mk1"),
                "Current authored waves do not include either armed gunner ship");
        EnemyDefinition definition = catalog.DefinitionFor("gunner_drone_mk1");
        Require(definition.Class == EnemyClass.Standard && definition.Rank == EnemyRank.Common && definition.Role == EnemyRole.Fighter && definition.MaxHealth == 135 && definition.MoveSpeed == 75
            && definition.RadiusPixels == 18 && definition.KillRewardCredits == 8 && definition.MaxShield == 0,
            "Gunner has independent editable playtest balance");
        Enemy preview = catalog.Find("gunner_drone_mk1").Instantiate<Enemy>();
        Require(preview.GetNode<Sprite2D>("Sprite").Texture.GetSize() == new Vector2(48, 30),
            "Lightweight starter Gunner hull is 48x30, smaller than the 64x48 Bulwark");
        var gun = preview.GetNode<EnemyMinigun>(WeaponPath);
        Require(gun.GetNode<Sprite2D>("TurretPivot/GunSprite").Texture != preview.GetNode<Sprite2D>("Sprite").Texture
            && gun.GetNode<Sprite2D>("TurretPivot/GunSprite").Texture.GetSize() == new Vector2(12, 44)
            && gun.GetNode<Marker2D>("TurretPivot/GunSprite/Muzzle").Position == new Vector2(0, -18),
            "Long-barrel 12x44 gun retains the physical muzzle marker at its new canvas tip");
        preview.Free();
    }
    // =========================================================
    // Invoke the parked legacy helper through one complete catalog cycle, keeping source order unchanged.
    private static void CatalogCycle(AmmoLoopFixture f)
    {
        var debug = f.Main.GetNode<LevelDebugController>("World/Level/Debug");
        Require(debug.RoutePreview != null, "Deferred F2/F3 service connected");
        int gunners = 0, heavies = 0; LegacyCatalogSpawner legacy = new();
        foreach (PackedScene scene in f.Spawns.Catalog.Scenes)
        {
            Require(f.Spawns.Registry.Living.Count == 0, "Debug cycle fixture starts empty");
            legacy.SpawnNext(f.Spawns);
            Enemy ship = f.Spawns.Registry.Living.Single(); Disable(ship);
            if (ship.Definition.EnemyId == "gunner_drone_mk1") gunners++;
            if (ship.Definition.EnemyId == "heavy_gunner_drone_mk1") heavies++;
            debug._UnhandledKeyInput(new InputEventKey { Keycode = Key.F2, Pressed = true, Echo = true });
            Require(f.Spawns.Registry.Living.Count == 1, "Key repeat does not emit extra armed ships");
            ship.Free();
        }
        Require(gunners == 1 && heavies == 1, "F2 catalogs starter and heavy Gunner Drones exactly once");
    }
    #endregion

    #region Real Gunner Combat
    // =========================================================
    // Real catalog spawning/initialization binds shared navigation and the optional mount without a bespoke enemy script.
    private static Enemy Spawn(AmmoLoopFixture f, bool stealth = false)
    {
        Enemy ship = f.Spawns.SpawnWave("gunner_drone_mk1", new RandomNumberGenerator { Seed = 42 }, spawnWithStealth: stealth);
        Require(ship != null, "Gunner fits a real portal/navigation source: " + f.Spawns.LastMessage);
        Disable(ship); ship.GlobalPosition = new(2000, 2064); ship.Rotation = 0;
        return ship;
    }
    // =========================================================
    // The gun aligns independently, flashes at its physical muzzle and delivers a genuine world-owned Kinetic shot.
    private static void Firefight(AmmoLoopFixture f)
    {
        Enemy ship = Spawn(f);
        var gun = ship.GetNode<EnemyMinigun>(WeaponPath);
        var pivot = gun.GetNode<Node2D>("TurretPivot");
        var muzzle = gun.GetNode<Marker2D>("TurretPivot/GunSprite/Muzzle");
        var renderer = gun.GetNode<WeaponVisualRenderer>("WeaponVisuals");
        var bullets = f.Main.GetNode<EnemyBulletSystem>("World/EnemyBullets");
        BattleTower tower = f.Add<BattleTower>("battle", new(2000, 2000));
        float hp = tower.Health; Transform2D hull = ship.Transform; var movement = ship.Movement;
        // Reset after fixture repositioning to capture the same authored right-facing initial gun heading.
        gun.RefreshConfiguration(); gun._PhysicsProcess(1);
        Require(gun.Running && gun.Aligned && gun.Target == tower && gun.ShotRequests == 1 && bullets.ActiveCount == 1,
            "Actual Gunner scene finds the nearby tower and launches once");
        var shot = bullets.Snapshot(0);
        Require(ship.Transform == hull && ship.Movement == movement && shot.Position.IsEqualApprox(muzzle.GlobalPosition)
            && shot.Direction.IsEqualApprox(-muzzle.GlobalTransform.Y.Normalized()) && tower.Health == hp,
            "Gun aims while hull/navigation ownership and launch-time HP remain unchanged");
        Require(shot.Visual.Tapered && shot.Visual.Length == 9 && Mathf.IsEqualApprox(shot.Visual.Width, 1.5f) && !shot.Visual.BrightTip
            && shot.Visual.Body.R > shot.Visual.Body.G && renderer.TotalFlashes == 1
            && renderer.ToGlobal(renderer.LastFlashPosition).IsEqualApprox(muzzle.GlobalPosition)
            && renderer.LastFlashStyle.Length == 5 && renderer.LastFlashStyle.Duration < .08f,
            "Dedicated small red tracer and tiny warm muzzle flash share the real launch origin");
        long count = gun.ShotRequests; gun._PhysicsProcess(.001);
        Require(gun.ShotRequests == count && renderer.TotalFlashes == 1, "Cooldown opportunities do not flash");
        bullets.Advance(.1);
        Require(tower.Health < hp && bullets.ActiveCount == 0, "Shot uses existing tower hull damage");
        var settings = (EnemyMinigunConfig)gun.Config.Duplicate(true);
        settings.MuzzleFlash = (MuzzleFlashStyle)settings.MuzzleFlash.Duplicate();
        settings.ProjectileVisual = (ProjectileVisualStyle)settings.ProjectileVisual.Duplicate();
        gun.Config = settings;
        settings.MuzzleFlash.LengthPixels = 30; settings.ProjectileVisual.LengthPixels = 50;
        gun._PhysicsProcess(1);
        Require(renderer.LastFlashStyle.Length == 5 && bullets.Snapshot(0).Visual.Length == 9,
            "Spawn-captured visual snapshots ignore later edits to mutable source clones");
        gun.Enabled = false;
        Require(renderer.ActiveFlashes == 0 && !renderer.IsProcessing() && bullets.ActiveCount == 1,
            "Disabling mount clears transient flash without cancelling emitted bullet");
        ship.Free(); bullets.Advance(.1);
        Require(tower.Health < hp, "Bullets remain owned by the world after source removal");
        bullets.Clear(); tower.Free();
    }
    // =========================================================
    // Existing hull alpha propagates through the weapon branch; Freeze prevents future gunfire without changing routing.
    // =========================================================
    // The heavy variant is a distinct catalog ship with its own slower red projectiles.
    private static void HeavyVariant(AmmoLoopFixture f)
    {
        EnemyCatalog catalog = f.Spawns.Catalog;
        EnemyDefinition data = catalog.DefinitionFor("heavy_gunner_drone_mk1");
        Require(data != null && data.Class == EnemyClass.Heavy && data.Rank == EnemyRank.Veteran
            && data.Role == EnemyRole.Fighter && data.MaxHealth == 260 && data.MaxShield == 0
            && data.MoveSpeed == 52 && data.RadiusPixels == 24 && data.TurnSpeed == 4.4f
            && data.CitadelDamage == 4 && data.KillRewardCredits == 14,
            "Separate Heavy Gunner uses its own tankier, slower authored definition");

        Enemy preview = catalog.Find("heavy_gunner_drone_mk1").Instantiate<Enemy>();
        var hull = preview.GetNode<Sprite2D>("Sprite");
        var gun = preview.GetNode<EnemyMinigun>(WeaponPath);
        var gunSprite = gun.GetNode<Sprite2D>("TurretPivot/GunSprite");
        var muzzle = gun.GetNode<Marker2D>("TurretPivot/GunSprite/Muzzle");
        EnemyMinigunConfig config = (EnemyMinigunConfig)gun.Config;
        Require(hull.Texture.GetSize() == new Vector2(62, 42) && gunSprite.Texture.GetSize() == new Vector2(18, 50)
            && gun.GetNode<Node2D>("TurretPivot").Position == new Vector2(-2, 0)
            && muzzle.Position == new Vector2(0, -21),
            "62x42 armoured hull and 18x50 independent gun match the real barrel-tip marker");
        Require(config.RangePixels == 275 && config.ShotsPerSecond == 3.5f
            && config.BulletSpeedPixels == 850 && config.DamagePerBullet == 4
            && config.RotationSpeedDegrees == 180 && config.BulletLifetimeSeconds == 2.8
            && !config.UseBursts && config.ProjectileVisual != null
            && config.ProjectileVisual.Tapered && config.ProjectileVisual.LengthPixels == 11
            && Mathf.IsEqualApprox(config.ProjectileVisual.CoreThicknessPixels, 2.2f)
            && config.ProjectileVisual.BodyColor.R > config.ProjectileVisual.BodyColor.G,
            "Heavy Gunner has an independent 4-damage, 850px/s, broad red 11x2.2 shot preset");
        preview.Free();

        Enemy ship = f.Spawns.SpawnWave("heavy_gunner_drone_mk1", new RandomNumberGenerator { Seed = 42 });
        Require(ship != null, "Heavy Gunner spawns through the shared enemy catalog and portal");
        Disable(ship); ship.GlobalPosition = new(2000, 2064); ship.Rotation = 0;
        gun = ship.GetNode<EnemyMinigun>(WeaponPath);
        muzzle = gun.GetNode<Marker2D>("TurretPivot/GunSprite/Muzzle");
        var bullets = f.Main.GetNode<EnemyBulletSystem>("World/EnemyBullets");
        BattleTower tower = f.Add<BattleTower>("battle", new(2000, 2000));
        float hp = tower.Health;
        gun.RefreshConfiguration(); gun._PhysicsProcess(1);
        Require(gun.Running && gun.ShotRequests == 1 && bullets.ActiveCount == 1,
            "Heavy Gunner aims and fires using the same shared EnemyMinigun world-bullet system");
        var shot = bullets.Snapshot(0);
        Require(shot.Position.IsEqualApprox(muzzle.GlobalPosition)
            && shot.Direction.IsEqualApprox(-muzzle.GlobalTransform.Y.Normalized())
            && Mathf.IsEqualApprox(shot.Speed, 850f)
            && shot.Visual.Tapered && shot.Visual.Length == 11
            && Mathf.IsEqualApprox(shot.Visual.Width, 2.2f)
            && shot.Visual.Body.R > shot.Visual.Body.G,
            "Heavy world-owned bullet starts at its muzzle, flies slower and keeps an enlarged red tracer");
        bullets.Advance(.15);
        Require(tower.Health < hp, "Heavy Gunner shot damages towers through the normal Kinetic pipeline");
        ship.Free(); bullets.Clear(); tower.Free();
    }
    private static void StealthAndFreeze(AmmoLoopFixture f)
    {
        Enemy ship = Spawn(f, stealth: true);
        var hull = ship.GetNode<Sprite2D>("Sprite");
        var gun = ship.GetNode<EnemyMinigun>(WeaponPath);
        var gunSprite = gun.GetNode<Sprite2D>("TurretPivot/GunSprite");
        Require(ship.IsEffectivelyStealthed && Mathf.IsEqualApprox(hull.Modulate.A, .3f)
            && hull.IsAncestorOf(gunSprite), "Gun and flash branch inherit the same hidden hull alpha");
        BattleTower tower = f.Add<BattleTower>("battle", new(2000, 2000));
        Require(ship.ApplyStatus(new StatusEffectPayload(StatusEffectId.Freeze, 0, 3)), "Existing Freeze applies to the gunner");
        gun._PhysicsProcess(1);
        Require(gun.ShotRequests == 0 && !gun.Aligned, "Frozen gunner cannot request or flash a shot");
        ship.Arrive(); Require(!gun.Running, "Normal arrival removes the optional mount");
        ship.Free(); tower.Free();
    }
    // =========================================================
    // A rejected central shot must never produce a cosmetic flash that falsely implies a fired round.
    private static void Overflow(AmmoLoopFixture f)
    {
        var bullets = f.Main.GetNode<EnemyBulletSystem>("World/EnemyBullets");
        for (int i = 0; i < bullets.Capacity; i++)
            Require(bullets.TrySpawn(new(Vector2.Zero, Vector2.Right, 1, 100)), "Fill bounded launch budget");
        Enemy ship = Spawn(f); var gun = ship.GetNode<EnemyMinigun>(WeaponPath);
        BattleTower tower = f.Add<BattleTower>("battle", new(2000, 2000));
        gun._PhysicsProcess(1);
        Require(gun.ShotRequests == 0 && gun.GetNode<WeaponVisualRenderer>("WeaponVisuals").TotalFlashes == 0 && bullets.Overflow > 0,
            "Overflow creates neither false flash nor accepted-shot count");
        ship.Free(); tower.Free(); bullets.Clear();
    }
    // =========================================================
    // Non-global C# scripts are identified by runtime type rather than native FindChildren class filters.
    private static bool HasWeapon(Node node)
    {
        if (node is EnemyWeaponMount) return true;
        foreach (Node child in node.GetChildren()) if (HasWeapon(child)) return true;
        return false;
    }
    #endregion
}
