// PF1: notification-owned movement indexing, live reveal/swept consumers and paired duplicate-call diagnostics.
using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using static AmmoLoopFixture;

public partial class MovementRegistryChecks : Node
{
    #region Runner
    // =========================================================
    // Private production world and terrain; no source map, profile or gameplay configuration is saved.
    public override async void _Ready()
    {
        try
        {
            using (AmmoLoopFixture f = new(this))
            {
                SubsystemBattlefield.DetachSession(f.Main);
                OpenTerrain(f);
                Enemy enemy = f.Target(f.Spawns.Paths.Navigation.Grid.CellToWorldCenter(new(5, 10)));
                Notifications(f, enemy);
                ActualMovement(f, enemy);
                Reveal(f, enemy);
                PairedWork(f, enemy);
                Node2D parent = (Node2D)enemy.GetParent();
                Vector2 shift = new(128, 64), before = enemy.GlobalPosition;
                parent.Position += shift;
                parent.ForceUpdateTransform(); enemy.ForceUpdateTransform();
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                Require(enemy.GlobalPosition.IsEqualApprox(before + shift), "Ancestor transform changes world position");
                Indexed(f.Spawns.Registry, enemy, enemy.GlobalPosition, before);
                parent.Position -= shift; parent.ForceUpdateTransform(); enemy.ForceUpdateTransform();
                var registry = f.Spawns.Registry;
                Vector2 removed = enemy.GlobalPosition;
                int removedNotices = 0;
                void RemovedMove(Enemy ship) { if (ship == enemy) removedNotices++; }
                registry.EnemyPositionChanged += RemovedMove;
                try
                {
                    enemy.Arrive(); enemy.SetMovementPosition(removed + Vector2.One);
                    Require(removedNotices == 0, "Resolved enemies do not publish movement callbacks");
                }
                finally { registry.EnemyPositionChanged -= RemovedMove; }
                enemy.Free();
                List<Enemy> found = new(); registry.GetEnemiesInRadius(removed, 1, found);
                Require(!found.Contains(enemy) && !registry.Contains(enemy), "Removed enemies leave point membership");
            }
            await SubsystemBattlefield.DrainFreedFixtures(this);
            GD.Print("PASS: PF1 notification indexing, actual movement, same/cross-cell motion, teleports, ancestor transforms, reveal, swept history and removal");
            GetTree().Quit();
        }
        catch (Exception error) { GD.PushError(error.ToString()); GetTree().Paused = false; GetTree().Quit(1); }
    }
    // =========================================================
    // Keep route safety active while making this fixture independent of hand-painted terrain.
    private static void OpenTerrain(AmmoLoopFixture f)
    {
        var grid = f.Spawns.Paths.Navigation.Grid;
        grid.Level.Passable.Clear(); grid.Level.Impassable.Clear(); grid.Level.Interactive.Clear();
        for (int y = 0; y < grid.Config.HeightCells; y++)
            for (int x = 0; x < grid.Config.WidthCells; x++) grid.Level.Passable.SetCell(new(x, y), 0, new(2, 0));
        grid.RebuildTerrain(); f.Spawns.Paths.Navigation.Refresh();
    }
    #endregion

    #region Position Consumers
    // =========================================================
    // Assert synchronous point visibility without helping production with a manual registry update.
    private static void Indexed(EnemyRegistry registry, Enemy enemy, Vector2 current, Vector2 previous)
    {
        List<Enemy> found = new(); registry.GetEnemiesInRadius(current, .01f, found);
        Require(found.Contains(enemy), "New position immediately enters exact circle query");
        if (current.DistanceTo(previous) > 1)
        { registry.GetEnemiesInRadius(previous, .01f, found); Require(!found.Contains(enemy), "Old point membership is removed"); }
        Require(TowerTargeting.Find(registry, current, .01f, TowerTargeting.Priority.Closest, detectsStealth: true) == enemy,
            "Targeting reads new cached position");
    }
    // =========================================================
    // Changed transforms must notify; legitimate local/global notices need not have exactly one callback.
    private static void Notifications(AmmoLoopFixture f, Enemy enemy)
    {
        var registry = f.Spawns.Registry;
        int notices = 0; Vector2 observed = default;
        void Moved(Enemy ship) { if (ship == enemy) { notices++; observed = ship.GlobalPosition; } }
        registry.EnemyPositionChanged += Moved;
        ulong cursor = registry.BeginWaveQuery();
        try
        {
            foreach (Vector2 delta in new[] { new Vector2(1, 0), new Vector2(256, 0), new Vector2(-512, 64) })
            {
                Vector2 before = enemy.GlobalPosition, after = before + delta; int count = notices;
                enemy.SetMovementPosition(after);
                Require(notices > count && observed == after, "Movement publishes position listeners synchronously");
                Indexed(registry, enemy, after, before);
                List<EnemySpatialIndex.Sweep> swept = new();
                registry.GetWaveCandidates(cursor, (before + after) * .5f, 0, 1024, swept);
                Require(swept.Any(v => v.Enemy == enemy && v.Start.IsEqualApprox(before) && v.End.IsEqualApprox(after)),
                    "Active wave cursor retains actual movement chord");
            }
            Vector2 old = enemy.GlobalPosition; enemy.GlobalPosition += new Vector2(1024, 32);
            Indexed(registry, enemy, enemy.GlobalPosition, old);
            GD.Print($"PF1 transform notification callbacks (not an exactly-one contract): {notices}");
        }
        finally { registry.EndWaveQuery(cursor); registry.EnemyPositionChanged -= Moved; }
    }
    // =========================================================
    // Tick the actual movement loop, including swept clearance, rather than just exercising transform setters.
    private static void ActualMovement(AmmoLoopFixture f, Enemy enemy)
    {
        var grid = f.Spawns.Paths.Navigation.Grid;
        enemy.GlobalPosition = grid.CellToWorldCenter(new(5, 10));
        enemy.Movement.AcceptPath(new[] { enemy.GlobalPosition, grid.CellToWorldCenter(new(15, 10)) }, grid.NavigationRevision);
        Vector2 before = enemy.GlobalPosition;
        enemy.Movement._PhysicsProcess(1.0 / 60);
        Require(enemy.GlobalPosition != before && enemy.IsAlive, "Actual cached-route movement advances");
        Indexed(f.Spawns.Registry, enemy, enemy.GlobalPosition, before);
    }
    // =========================================================
    // Live subscriber receives ordinary movement entry/exit, not a direct manual reveal refresh.
    private static void Reveal(AmmoLoopFixture f, Enemy enemy)
    {
        Vector2 center = enemy.GlobalPosition;
        enemy.Initialize(f.Spawns.Registry, f.Spawns.Paths, spawnWithStealth: true); Disable(enemy);
        StealthBeacon beacon = f.Add<StealthBeacon>("stealth_beacon", center);
        try
        {
            Require(!enemy.IsEffectivelyStealthed, "Active beacon reveals existing enemy");
            enemy.SetMovementPosition(center + new Vector2(1024, 0));
            Require(enemy.IsEffectivelyStealthed, "Movement exits live reveal coverage");
            enemy.SetMovementPosition(center);
            Require(!enemy.IsEffectivelyStealthed, "Movement re-enters live reveal coverage");
        }
        finally { beacon.Free(); }
    }
    #endregion

    #region Paired Diagnostic
    // =========================================================
    // Compare notification-only writes with the former extra call; report real counts/timing, never assert a speedup.
    private static void PairedWork(AmmoLoopFixture f, Enemy enemy)
    {
        const int iterations = 10000;
        Vector2 origin = enemy.GlobalPosition;
        int callbacks = 0;
        void Moved(Enemy ship) { if (ship == enemy) callbacks++; }
        f.Spawns.Registry.EnemyPositionChanged += Moved;
        try
        {
            for (int i = 0; i < 100; i++) enemy.SetMovementPosition(origin + new Vector2(i % 2, 0));
            for (int run = 0; run < 3; run++)
                foreach (bool extra in run % 2 == 0 ? new[] { false, true } : new[] { true, false })
                {
                    int before = callbacks; long start = Stopwatch.GetTimestamp();
                    for (int i = 0; i < iterations; i++)
                    {
                        enemy.SetMovementPosition(origin + new Vector2(i % 2, 0));
                        if (extra) f.Spawns.Registry.UpdatePosition(enemy);
                    }
                    double ms = (Stopwatch.GetTimestamp() - start) * 1000.0 / Stopwatch.Frequency;
                    GD.Print($"PF1 run={run + 1}; explicitDuplicate={extra}; writes={iterations}; callbacks={callbacks - before}; elapsedMs={ms:0.000}");
                }
        }
        finally { f.Spawns.Registry.EnemyPositionChanged -= Moved; }
    }
    #endregion
}
