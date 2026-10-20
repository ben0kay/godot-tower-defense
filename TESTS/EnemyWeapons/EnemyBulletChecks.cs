// EG2 focused checks: swept first-footprint damage, straight misses, source lifetime, budgets, pause and renderer ownership.
using Godot;
using System;
using static AmmoLoopFixture;

public partial class EnemyBulletChecks : Node
{
    #region Runner and Geometry
    // =========================================================
    // Isolated production worlds exercise existing damage/durability without modifying waves, catalog or profiles.
    public override async void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
        try
        {
            Geometry();
            using (AmmoLoopFixture f = new(this))
            {
                SubsystemBattlefield.DetachSession(f.Main);
                SweptHits(f); LifetimeAndMisses(f); SourceLifetime(f); LimitsAndPresentation(f);
            }
            await SubsystemBattlefield.DrainFreedFixtures(this);
            GD.Print("PASS: EG2 swept first footprint, multi-cell geometry, Kinetic HP loss, straight misses, expiry/pause, source independence and budgets");
            GetTree().Quit();
        }
        catch (Exception error) { GetTree().Paused = false; GD.PushError(error.ToString()); GetTree().Quit(1); }
    }
    // =========================================================
    // Pure slab checks include exact tangency, starts inside, parallel misses and stationary contact.
    private static void Geometry()
    {
        Rect2 rect = new(10, 10, 20, 30);
        Require(TowerFootprintCollision.SegmentRect(new(0, 20), new(1000, 20), rect, out double fraction)
            && Math.Abs(fraction - .01) < 1e-10, "Fast segment returns first entry, not final position");
        Require(TowerFootprintCollision.SegmentRect(new(0, 10), new(30, 10), rect, out _), "Exact edge contact is inclusive");
        Require(TowerFootprintCollision.SegmentRect(new(15, 15), new(15, 15), rect, out fraction) && fraction == 0, "Start inside/zero travel hits once");
        Require(!TowerFootprintCollision.SegmentRect(new(0, 9), new(1000, 9), rect, out _), "Parallel narrow miss stays a miss");
        Require(!TowerFootprintCollision.SegmentRect(new(float.NaN, 0), Vector2.Zero, rect, out _), "Invalid geometry is rejected");
    }
    #endregion

    #region Real Swept Damage
    // =========================================================
    // High-speed rounds hit the nearest actual footprint, including blockers that were not selected by the shooter.
    private static void SweptHits(AmmoLoopFixture f)
    {
        var bullets = f.Main.GetNode<EnemyBulletSystem>("World/EnemyBullets");
        BattleTower near = f.Add<BattleTower>("battle", new(2000, 2000));
        BattleTower far = f.Add<BattleTower>("battle", new(2300, 2000));
        float nearHp = near.Health, farHp = far.Health;
        Require(bullets.TrySpawn(new(new(1600, 2000), Vector2.Right, 10, 20000)), "Fast round accepted");
        Require(near.Health == nearHp, "Launch itself never applies hitscan damage");
        bullets.Advance(.1);
        float expected = DamageResolver.Resolve(new DamageInfo(10, DamageType.Kinetic), 0, nearHp).Health;
        Require(near.Health == expected && far.Health == farHp && bullets.ActiveCount == 0 && bullets.Impacts == 1,
            "Swept earliest tower takes one shared-resolver Kinetic hit");
        Require(bullets.TrySpawn(new(new(1600, 2000), Vector2.Right, 99999, 20000))
            && bullets.TrySpawn(new(new(1600, 2000), Vector2.Right, 5, 20000)), "Two same-tick rounds accepted");
        bullets.Advance(.1);
        Require(near.IsDestroyed && far.Health < farHp && bullets.ActiveCount == 0,
            "First lethal impact unregisters immediately; following round reaches next living tower");
        near.Free(); far.Free();

        AmmunitionFactory factory = f.Add<AmmunitionFactory>("ammo_factory", new(4000, 4000));
        var registry = f.Main.GetNode<TowerRegistry>("World/TowerRegistry");
        Require(registry.TryGetBounds(factory, out Rect2 bounds), "Multi-cell footprint cached");
        float hp = factory.Health;
        Require(bullets.TrySpawn(new(new(bounds.Position.X - 100, bounds.End.Y - 1), Vector2.Right, 7, 10000)), "Edge-crossing factory round accepted");
        bullets.Advance(.1);
        Require(factory.Health < hp && bullets.ActiveCount == 0, "Full multi-cell footprint takes damage away from its center");
        factory.Free();
    }
    #endregion

    #region Straight Flight and Lifetime
    // =========================================================
    // Source-independent trajectories never home; lifetime clips the segment and pause freezes every clock.
    private static void LifetimeAndMisses(AmmoLoopFixture f)
    {
        var bullets = f.Main.GetNode<EnemyBulletSystem>("World/EnemyBullets");
        var registry = f.Main.GetNode<TowerRegistry>("World/TowerRegistry");
        BattleTower tower = f.Add<BattleTower>("battle", new(2000, 2000)); float hp = tower.Health;
        Require(bullets.TrySpawn(new(new(1600, 2000), Vector2.Right, 10, 1000, .2)), "Finite-lifetime round accepted");
        EnemyBulletData before = bullets.Snapshot(0);
        f.Main.GetTree().Paused = true; bullets.Advance(100);
        Require(!bullets.TrySpawn(new(Vector2.Zero, Vector2.Right, 1, 100)), "Paused launch rejected");
        f.Main.GetTree().Paused = false;
        Require(bullets.Snapshot(0).Position == before.Position && bullets.Snapshot(0).RemainingSeconds == before.RemainingSeconds, "Pause preserves position and lifetime");
        bullets.Advance(double.NaN); bullets.Advance(-1); bullets.Advance(0);
        Require(bullets.Snapshot(0).Position == before.Position, "Malformed delta inert");
        bullets.Advance(1);
        Require(tower.Health == hp && bullets.ActiveCount == 0, "Expired before reaching tower; large step cannot hit beyond lifetime");
        Require(bullets.TrySpawn(new(new(1600, 2000), Vector2.Right, 10, 10000)), "Moving target test launch");
        tower.GlobalPosition += new Vector2(0, 300); tower.ForceUpdateTransform();
        bullets.Advance(.1);
        Require(tower.Health == hp && bullets.ActiveCount == 1 && bullets.Snapshot(0).Position.Y == 2000, "Moved target is missed and trajectory stays straight");
        bullets.Clear();
        tower.GlobalPosition = new(2000, 2000); tower.ForceUpdateTransform();
        Require(registry.TryGetBounds(tower, out Rect2 bounds), "Lifetime endpoint geometry available");
        Require(bullets.TrySpawn(new(new(bounds.Position.X - 10, 2000), Vector2.Right, 1, 1000, .01)), "Boundary-lifetime round accepted");
        bullets.Advance(1);
        Require(tower.Health < hp && bullets.ActiveCount == 0, "Contact exactly at lifetime endpoint still hits");
        hp = tower.Health;
        tower.QueueFree();
        Require(bullets.TrySpawn(new(new(1600, 2000), Vector2.Right, 10, 10000)), "Queued-tower test launch");
        bullets.Advance(.1);
        Require(tower.Health == hp, "Queued tower cannot absorb a new impact");
        bullets.Clear(); tower.Free();

        var grid = f.Main.GetNode<WorldGrid>("World/Grid"); float rotation = grid.Rotation;
        grid.Rotation = .5f;
        BattleTower rotated = f.Add<BattleTower>("battle", new(4000, 4000));
        Vector2 center = grid.ToLocal(rotated.GlobalPosition);
        Vector2 from = grid.ToGlobal(center + new Vector2(-100, 0)), to = grid.ToGlobal(center + new Vector2(100, 0));
        Require(registry.TryIntersectFootprint(rotated, from, to, out _), "Exact clipping supports transformed grid footprints");
        float half = grid.Config.CellSize * .5f;
        from = grid.ToGlobal(center + new Vector2(-half - 10, -half - 1));
        to = grid.ToGlobal(center + new Vector2(half + 10, -half - 1));
        Require(!registry.TryIntersectFootprint(rotated, from, to, out _), "Conservative world AABB does not create a rotated-corner false hit");
        rotated.Free(); grid.Rotation = rotation;
    }
    #endregion

    #region Source Lifetime and Budgets
    // =========================================================
    // Real EG1 mount now launches into world slots; killing/removing the gunner does not own or cancel those bullets.
    private static void SourceLifetime(AmmoLoopFixture f)
    {
        using EnemyMinigunConfig config = new();
        var (enemy, component, _) = EnemyWeaponChecks.Create(f, config, true);
        var gun = (EnemyMinigun)component;
        BattleTower tower = f.Add<BattleTower>("battle", new(2000, 2000)); float hp = tower.Health;
        var bullets = f.Main.GetNode<EnemyBulletSystem>("World/EnemyBullets");
        gun._PhysicsProcess(1);
        Require(gun.ShotRequests == 1 && bullets.ActiveCount == 1 && tower.Health == hp, "Actual mounted gun creates a central shot without launch damage");
        enemy.ApplyDamage(new DamageInfo(99999, DamageType.Kinetic)); enemy.Free();
        bullets.Advance(.1);
        Require(tower.Health < hp && bullets.ActiveCount == 0, "In-flight bullet survives shooter death and hits normally");
        Require(tower.RankProgress.TotalXp == 0, "Hostile hits do not award friendly tower XP");
        tower.Free();
    }
    // =========================================================
    // A tiny private world budget proves deterministic overflow, slot reuse, sanitized snapshots and no bullet children.
    private static void LimitsAndPresentation(AmmoLoopFixture f)
    {
        var world = f.Main.GetNode("World");
        world.GetNode<EnemyBulletSystem>("EnemyBullets").Free();
        using EnemyProjectileMasterConfig config = new() { MaximumActive = 2, DefaultLifetimeSeconds = .2 };
        EnemyBulletSystem bullets = new() { Name = "EnemyBullets", Config = config }; world.AddChild(bullets);
        var renderer = world.GetNode<EnemyBulletRenderer>("EnemyBulletRenderer");
        int nodes = world.GetChildCount();
        using ProjectileVisualStyle style = new() { LengthPixels = 7, CoreThicknessPixels = 1.3f, CoreColor = Colors.Red };
        var captured = style.Capture();
        Require(bullets.TrySpawn(new(Vector2.Zero, Vector2.Right, 1, 100, .2, captured))
            && bullets.TrySpawn(new(Vector2.Zero, Vector2.Down, 1, 100)), "Budget fills with retained structs");
        style.LengthPixels = 100; style.CoreColor = Colors.Blue;
        Require(bullets.Snapshot(0).Visual == captured, "Live bullet appearance is immutable after source edit");
        Require(!bullets.TrySpawn(new(Vector2.Zero, Vector2.Right, 1, 100))
            && bullets.Overflow == 1 && bullets.ActiveCount == 2 && world.GetChildCount() == nodes
            && bullets.GetChildCount() == 0 && renderer.GetChildCount() == 0, "Overflow rejects only new round without creating nodes");
        bullets.Advance(1);
        Require(bullets.ActiveCount == 0 && bullets.Expired == 2 && !bullets.IsPhysicsProcessing()
            && !renderer.IsProcessing() && !renderer.IsPhysicsProcessing(), "Expiry clears slots and both surfaces sleep");
        long rejected = bullets.Rejected;
        Require(!bullets.TrySpawn(new(Vector2.Zero, Vector2.Zero, 1, 100))
            && !bullets.TrySpawn(new(new(float.NaN, 0), Vector2.Right, 1, 100))
            && !bullets.TrySpawn(new(Vector2.Zero, Vector2.Right, -1, 100))
            && !bullets.TrySpawn(new(Vector2.Zero, Vector2.Right, 1, float.PositiveInfinity))
            && !bullets.TrySpawn(new(Vector2.Zero, Vector2.Right, 1, 100, double.NaN))
            && bullets.Rejected == rejected + 5, "Malformed launch data rejected before slot insertion");
        ProjectileVisualSnapshot invalid = captured with { Length = float.NaN, Width = float.NaN, Core = new(float.NaN, 0, 0) };
        Require(bullets.TrySpawn(new(Vector2.Zero, Vector2.Right, 1, 100, .2, invalid)), "Recycled slot accepts sanitized presentation");
        Require(float.IsFinite(bullets.Snapshot(0).Visual.Length) && float.IsFinite(bullets.Snapshot(0).Visual.Core.R),
            "No invalid style values reach drawing");
        bullets.Clear();
        Require(bullets.Spawned == bullets.Impacts + bullets.Expired + bullets.Invalidated + bullets.Cleared + bullets.ActiveCount,
            "Instrumentation accounts for every accepted round");
        bullets.TrySpawn(new(Vector2.Zero, Vector2.Right, 1, 100));
        bullets.Free(); Require(!GodotObject.IsInstanceValid(bullets), "Teardown releases its retained world buffer");
    }
    #endregion
}
