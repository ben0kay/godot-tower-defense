// EG3 integration: real inherited medium-size gunner, F2/catalog, split hull/gun, accepted-shot flashes and tower damage.
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
                Definitions(f); F2(f); Firefight(f); StealthAndFreeze(f); Overflow(f);
            }
            await SubsystemBattlefield.DrainFreedFixtures(this);
            GD.Print("PASS: EG3 Gunner Drone split artwork/medium size, actual F2 cycle, muzzle snapshots, accepted-shot flashes and tower HP loss");
            GetTree().Quit();
        }
        catch (Exception error) { GetTree().Paused = false; GD.PushError(error.ToString()); GetTree().Quit(1); }
    }
    // =========================================================
    // The catalog adds one armed type; all previous identities remain unarmed and authored waves omit the gunner.
    private static void Definitions(AmmoLoopFixture f)
    {
        var catalog = f.Spawns.Catalog; catalog.Rebuild();
        var map = catalog.ToSceneMap();
        Require(map.Count == 14 && catalog.Find("gunner_drone_mk1") != null, "Fourteenth catalog entry has a unique stable ID");
        foreach (var pair in map)
        {
            Node ship = pair.Value.Instantiate();
            Require(HasWeapon(ship) == (pair.Key == "gunner_drone_mk1"), "Weapon is opt-in only on the new ship: " + pair.Key);
            ship.Free();
        }
        foreach (var waves in new[] { Level001Waves.Create(), Level002Waves.Create(), Level020Waves.Create() })
            Require(waves.SelectMany(w => w.Batches).SelectMany(b => b.Entries).All(e => e.EnemyId != "gunner_drone_mk1"),
                "Current authored waves do not include the new armed ship");
        EnemyDefinition definition = catalog.DefinitionFor("gunner_drone_mk1");
        Require(definition.Family == EnemyFamily.Drone && definition.MaxHealth == 180 && definition.MoveSpeed == 75
            && definition.RadiusPixels == 22 && definition.KillRewardCredits == 8 && definition.MaxShield == 0,
            "Gunner has independent editable playtest balance");
        Enemy preview = catalog.Find("gunner_drone_mk1").Instantiate<Enemy>();
        Texture2D medium = GD.Load<Texture2D>("res://ENEMIES/Types/Enemy004/Artwork/enemy004.svg");
        Require(preview.GetNode<Sprite2D>("Sprite").Texture.GetSize() == medium.GetSize() && medium.GetSize() == new Vector2(64, 48),
            "Hull matches actual medium Enemy004 size, not its shape");
        var gun = preview.GetNode<EnemyMinigun>(WeaponPath);
        Require(gun.GetNode<Sprite2D>("TurretPivot/GunSprite").Texture != preview.GetNode<Sprite2D>("Sprite").Texture
            && gun.GetNode<Marker2D>("TurretPivot/GunSprite/Muzzle").Position == new Vector2(0, -18),
            "Separate gun SVG has one real barrel-tip muzzle");
        preview.Free();
    }
    // =========================================================
    // Invoke the actual existing debug handler through one complete catalog cycle, keeping source order unchanged.
    private static void F2(AmmoLoopFixture f)
    {
        var debug = f.Main.GetNode<LevelDebugController>("World/Level/Debug");
        Require(debug.RoutePreview != null, "Deferred F2/F3 service connected");
        int gunners = 0;
        foreach (PackedScene scene in f.Spawns.Catalog.Scenes)
        {
            Require(f.Spawns.Registry.Living.Count == 0, "Debug cycle fixture starts empty");
            debug._UnhandledKeyInput(new InputEventKey { Keycode = Key.F2, Pressed = true });
            Enemy ship = f.Spawns.Registry.Living.Single(); Disable(ship);
            if (ship.Definition.EnemyId == "gunner_drone_mk1") gunners++;
            debug._UnhandledKeyInput(new InputEventKey { Keycode = Key.F2, Pressed = true, Echo = true });
            Require(f.Spawns.Registry.Living.Count == 1, "Key repeat does not emit extra armed ships");
            ship.Free();
        }
        Require(gunners == 1, "F2 includes exactly one new Gunner Drone entry per cycle");
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
        Require(shot.Visual.Length == 7 && Mathf.IsEqualApprox(shot.Visual.Width, 1.3f) && !shot.Visual.BrightTip
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
        Require(renderer.LastFlashStyle.Length == 5 && bullets.Snapshot(0).Visual.Length == 7,
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
