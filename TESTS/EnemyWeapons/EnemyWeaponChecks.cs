// EG1 focused regression: spatial tower membership/geometry plus optional enemy mount lifecycle, aim and cadence seam.
using Godot;
using System;
using System.Collections.Generic;
using static AmmoLoopFixture;

public partial class EnemyWeaponChecks : Node
{
    #region Runner
    // =========================================================
    // Private production worlds avoid profile writes, source-map edits or live catalog/wave changes.
    public override async void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
        try
        {
            using (AmmoLoopFixture f = new(this))
            {
                SubsystemBattlefield.DetachSession(f.Main);
                Index(f); Mount(f); Cadence(f); Optional(f);
                using AmmoLoopFixture other = new(this);
                SubsystemBattlefield.DetachSession(other.Main);
                Require(other.Main.GetNode<TowerRegistry>("World/TowerRegistry").Count == 0, "Worlds do not share tower membership");
            }
            await SubsystemBattlefield.DrainFreedFixtures(this);
            GD.Print("PASS: EG1 tower spatial membership, footprints, deterministic targeting, independent aim, pause, lifecycle and minigun requests");
            GetTree().Quit();
        }
        catch (Exception error) { GetTree().Paused = false; GD.PushError(error.ToString()); GetTree().Quit(1); }
    }
    #endregion

    #region Tower Index
    // =========================================================
    // Exercise real scene addition, all footprint cells, negative coordinates, exact edges, transform updates and death.
    private static void Index(AmmoLoopFixture f)
    {
        var index = f.Main.GetNode<TowerRegistry>("World/TowerRegistry");
        var grid = f.Main.GetNode<WorldGrid>("World/Grid");
        List<TowerBase> scratch = new();
        BattleTower first = f.Add<BattleTower>("battle", new(2000, 2000));
        index.Free(); index = new TowerRegistry { Name = "TowerRegistry" }; f.Main.GetNode("World").AddChild(index);
        Require(index.Count == 1 && index.Contains(first), "Startup discovery includes a pre-existing tower");
        BattleTower second = f.Add<BattleTower>("battle", new(2200, 2000));
        Require(index.Count == 2 && !index.IsProcessing() && !index.IsPhysicsProcessing(), "Event-only index observes actual tower container");
        Require(EnemyTowerTargeting.Closest(index, new(2100, 2000), 100, scratch) == first, "Inclusive closest-center tie uses insertion serial");
        first.GlobalPosition = new(-2000, -2000); first.ForceUpdateTransform();
        Require(EnemyTowerTargeting.Closest(index, new(2100, 2000), 100, scratch) == second
            && EnemyTowerTargeting.Closest(index, new(-2000, -2000), 1, scratch) == first, "Direct root relocation updates buckets including negative space");
        var definition = f.Placement.Catalog.Find("ammo_factory");
        if (definition == null) throw new InvalidOperationException("Missing ammunition factory fixture definition");
        AmmunitionFactory factory = f.Add<AmmunitionFactory>(definition.TowerId, new(4000, 4000));
        Require(factory.Footprint.X > 1 || factory.Footprint.Y > 1, "Fixture uses a multi-cell footprint");
        Require(index.TryGetBounds(factory, out Rect2 bounds)
            && bounds.Size.IsEqualApprox((Vector2)factory.Footprint * grid.Config.CellSize), "Complete multi-cell footprint is cached");
        Vector2 edge = bounds.End;
        index.GetNearby(edge, 0, scratch);
        Require(scratch.Count == 1 && scratch[0] == factory, "Footprint far corner is indexed inclusively without duplicates");
        Require(EnemyTowerTargeting.Closest(index, edge + new Vector2(1, 0), 1, scratch) == null, "Tower selection remains center based despite footprint broadphase");
        Rect2 before = bounds;
        factory.SetPlacement(new(12, 12), new(5000, 5000)); factory.ForceUpdateTransform();
        Require(index.TryGetBounds(factory, out bounds) && bounds.Position != before.Position, "SetPlacement relocation refreshes the complete rectangle");
        second.QueueFree();
        Require(!EnemyTowerTargeting.Valid(index, second, new(2200, 2000), 100), "Queued target is immediately ineligible before tree exit");
        second.Free();
        first.ApplyDamage(new DamageInfo(99999, DamageType.Kinetic));
        Require(index.Count == 1 && !index.Contains(first), "Destroyed hull unregisters before deferred deletion");
        first.Free(); factory.Free();
        Require(index.Count == 0, "Tree removal releases every bucket/member");
        index.GetNearby(Vector2.Zero, float.NaN, scratch); Require(scratch.Count == 0, "Invalid query is safely empty");
    }
    #endregion

    #region Mount Lifecycle
    // =========================================================
    // Add an optional real component before initialization; ordinary enemy scenes are never edited or armed.
    private static (Enemy Enemy, EnemyWeaponMount Mount, Node2D Pivot) Create(AmmoLoopFixture f, EnemyWeaponConfig config, bool minigun = false)
    {
        Enemy enemy = GD.Load<PackedScene>("res://ENEMIES/Types/Enemy001/Enemy001.tscn").Instantiate<Enemy>();
        enemy.Definition = (EnemyDefinition)enemy.Definition.Duplicate(true); enemy.Definition.MaxHealth = 10000;
        EnemyWeaponMount mount = minigun ? new EnemyMinigun() : new EnemyWeaponMount();
        mount.Config = config; mount.Name = "TestWeapon";
        Node2D pivot = new() { Name = "TurretPivot" };
        Sprite2D gun = new() { Name = "GunSprite" };
        Marker2D muzzle = new() { Name = "Muzzle", Position = new(0, -16) };
        gun.AddChild(muzzle); pivot.AddChild(gun); mount.AddChild(pivot); enemy.AddChild(mount);
        f.Main.GetNode("World/Enemies").AddChild(enemy); enemy.GlobalPosition = new(2000, 2064);
        Require(!mount.Running && !mount.IsPhysicsProcessing(), "Uninitialized optional mount sleeps");
        enemy.Initialize(f.Spawns.Registry, f.Spawns.Paths); Disable(enemy);
        return (enemy, mount, pivot);
    }
    // =========================================================
    // Mount owns only the child pivot; hull transform and movement/path components retain their original owners.
    private static void Mount(AmmoLoopFixture f)
    {
        using EnemyWeaponConfig config = new() { RotationSpeedDegrees = 360 };
        var (enemy, mount, pivot) = Create(f, config);
        BattleTower tower = f.Add<BattleTower>("battle", new(2064, 2064));
        var movement = enemy.Movement; var hostTransform = enemy.Transform;
        Require(mount.Running && mount.SearchRemaining > 0 && mount.SearchRemaining < config.TargetSearchSeconds, "Initialization staggers first query");
        mount._PhysicsProcess(.001); Require(mount.TargetQueries == 0, "No eager per-frame full reacquisition");
        mount._PhysicsProcess(1);
        Require(mount.Target == tower && mount.Aligned && enemy.Transform == hostTransform && enemy.Movement == movement
            && Mathf.Abs(pivot.GlobalRotation - Mathf.Pi / 2) < .001f, "Gun tracks right while hull and route owner remain unchanged");
        enemy.Rotation = 1;
        mount._PhysicsProcess(.01);
        Require(Mathf.Abs(pivot.GlobalRotation - Mathf.Pi / 2) < .001f, "Hull rotation does not rotate the gun's cached world heading");
        int queries = mount.TargetQueries; float angle = pivot.GlobalRotation;
        enemy.GetTree().Paused = true; mount._PhysicsProcess(10); enemy.GetTree().Paused = false;
        Require(mount.TargetQueries == queries && pivot.GlobalRotation == angle, "Pause freezes query and gun clocks");
        mount._PhysicsProcess(double.NaN); mount._PhysicsProcess(-1);
        Require(mount.TargetQueries == queries, "Malformed deltas never advance acquisition");
        tower.GlobalPosition += new Vector2(2000, 0); tower.ForceUpdateTransform();
        mount._PhysicsProcess(.001);
        Require(mount.Target == null && !mount.Aligned && mount.TargetQueries == queries, "Out-of-range lock clears immediately without bypassing the query interval");
        tower.GlobalPosition = enemy.GlobalPosition + new Vector2(0, -64); tower.ForceUpdateTransform();
        mount._PhysicsProcess(1); Require(mount.Target == tower, "Periodic acquisition finds returned target");
        mount.Enabled = false; Require(!mount.Running && mount.Target == null && !mount.IsPhysicsProcessing(), "Explicit disable clears/sleeps");
        mount.Enabled = true; mount._PhysicsProcess(1); Require(mount.Target == tower, "Re-enable captures a fresh schedule");
        config.RangePixels = 1; mount._PhysicsProcess(1);
        Require(mount.Target == tower, "Source edits do not mutate the captured runtime configuration");
        config.RangePixels = float.NaN; mount.RefreshConfiguration();
        Require(!mount.Running && mount.Target == null, "Invalid config disables only the optional mount");
        config.RangePixels = 260; mount.RefreshConfiguration();
        enemy.Initialize(f.Spawns.Registry, f.Spawns.Paths); Disable(enemy);
        Require(mount.Running && mount.Target == null && mount.SearchRemaining > 0, "Reinitialization clears old lock and restaggers");
        enemy.Arrive();
        Require(!mount.Running && mount.Target == null && !mount.IsPhysicsProcessing(), "Arrival stops optional weapon before deferred removal");
        enemy.Free(); tower.Free();
    }
    #endregion

    #region Minigun Seam and Opt In
    // =========================================================
    // EG1 timing emits immutable requests only: no bullet, damage, flash or new catalog enemy is manufactured.
    private static void Cadence(AmmoLoopFixture f)
    {
        using EnemyMinigunConfig config = new();
        var (enemy, component, _) = Create(f, config, true);
        EnemyMinigun gun = (EnemyMinigun)component;
        BattleTower tower = f.Add<BattleTower>("battle", new(2000, 2000));
        int requests = 0; EnemyShotRequest last = default; float hp = tower.Health;
        gun.ShotRequested += request => { requests++; last = request; };
        for (int i = 0; i < 90; i++) gun._PhysicsProcess(1.0 / 60);
        Require(requests >= 7 && requests <= 9 && last.Direction.IsEqualApprox(Vector2.Up)
            && last.Origin == gun.GetNode<Marker2D>("TurretPivot/GunSprite/Muzzle").GlobalPosition,
            "Staggered 1.5s burst has bounded cadence and real barrel-tip straight direction");
        for (int i = 0; i < 20; i++) gun._PhysicsProcess(1.0 / 60);
        int resting = requests;
        for (int i = 0; i < 20; i++) gun._PhysicsProcess(1.0 / 60);
        Require(requests == resting && tower.Health == hp && f.Main.GetNode("World/Projectiles").GetChildCount() == 0,
            "Burst rest is quiet; EG1 requests cannot apply fake hits or spawn bullets");
        enemy.GetTree().Paused = true; gun._PhysicsProcess(100); enemy.GetTree().Paused = false;
        Require(requests == resting, "Paused gun emits no requests");
        gun.Enabled = false; gun._PhysicsProcess(100); Require(requests == resting, "Disabled gun emits no requests");
        gun.Enabled = true; gun._PhysicsProcess(10);
        Require(requests <= resting + 1, "Large delta never emits an unbounded catch-up burst");
        enemy.Arrive(); gun._PhysicsProcess(100); Require(!gun.Running, "Removed source cannot request more fire");
        enemy.Free(); tower.Free();
    }
    // =========================================================
    // Existing ordinary enemy scenes keep no mandatory weapon child, scheduler or extra physics callback.
    private static void Optional(AmmoLoopFixture f)
    {
        Enemy plain = f.Target(new(2000, 2000));
        Require(!HasWeapon(plain), "Existing Enemy001 stays unarmed");
        plain.Free();
    }
    // =========================================================
    // Native FindChildren type filtering does not identify every non-global C# script class.
    private static bool HasWeapon(Node root)
    {
        if (root is EnemyWeaponMount) return true;
        foreach (Node child in root.GetChildren()) if (HasWeapon(child)) return true;
        return false;
    }
    #endregion
}
