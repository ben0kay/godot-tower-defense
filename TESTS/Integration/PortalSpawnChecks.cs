// Verify portal-center spawn variation without replacing the authored map.
using Godot;
using System;
using System.Collections.Generic;

public static class PortalSpawnChecks
{
    // =========================================================
    // Seed wave requests and verify varied, bounded, obstacle-safe positions at portal centers.
    public static void Run(WorldGrid grid, SpawnManager spawns)
    {
        var centers = new List<Vector2>();
        foreach (NodePath path in grid.Level.Settings.ActivePortals)
        {
            Node2D portal = grid.Level.GetNode<Node2D>(path);
            if (portal.GetNode<Marker2D>("SpawnExit").Position != Vector2.Zero)
                throw new InvalidOperationException("SpawnExit must be at portal center.");
            centers.Add(portal.GlobalPosition);
        }
        RandomNumberGenerator random = new() { Seed = 456 };
        PackedScene scene = GD.Load<PackedScene>("res://ENEMIES/Types/Enemy001/Enemy001.tscn");
        HashSet<Vector2> positions = new();
        for (int i = 0; i < 32; i++)
        {
            Enemy ship = spawns.SpawnWave(scene, random);
            if (ship == null) throw new InvalidOperationException("Centered portal spawn failed.");
            ship.Movement.SetPhysicsProcess(false);
            bool bounded = false;
            foreach (Vector2 center in centers)
            {
                Vector2 difference = ship.GlobalPosition - center;
                if (Mathf.Abs(difference.X) <= 8.01f && Mathf.Abs(difference.Y) <= 8.01f) bounded = true;
            }
            if (!bounded || !spawns.Paths.Navigation.HasClearance(ship.GlobalPosition, ship.Definition.RadiusPixels))
                throw new InvalidOperationException("Portal spawn offset is unbounded or blocked.");
            positions.Add(ship.GlobalPosition); ship.TakeDamage(10000);
        }
        if (positions.Count < 8) throw new InvalidOperationException("Spawn positions lack variation.");
        VerifySizeOverrides(grid, spawns, scene);
        GD.Print("PASS: portal-centered spawns vary safely within eight pixels on each axis");
    }

    // =========================================================
    // Per-instance editor/game overrides scale art and both wave/debug spread without touching topology.
    private static void VerifySizeOverrides(WorldGrid grid, SpawnManager spawns, PackedScene scene)
    {
        EnemyPortal first = grid.Level.GetNode<EnemyPortal>(grid.Level.Settings.ActivePortals[0]);
        EnemyPortal second = grid.Level.GetNode<EnemyPortal>(grid.Level.Settings.ActivePortals[1]);
        float oldFirst = first.SizeMultiplier, oldSecond = second.SizeMultiplier;
        int revision = grid.NavigationRevision;
        try
        {
            first.SizeMultiplier = 0.5f; second.SizeMultiplier = 3f;
            if (first.GetNode<Node2D>("Visuals").Scale != Vector2.One * 0.5f ||
                second.GetNode<Node2D>("Visuals").Scale != Vector2.One * 3 ||
                first.GetNode<Marker2D>("SpawnExit").GlobalPosition != first.GlobalPosition ||
                first.SizeCells != Vector2I.One || grid.NavigationRevision != revision)
                throw new InvalidOperationException("Portal size must affect only visuals/spread, not exit or topology.");
            RandomNumberGenerator random = new() { Seed = 543 };
            float largest = 0;
            for (int i = 0; i < 32; i++)
            {
                foreach (var request in new[] { (first, 4f, grid.Level.Settings.ActivePortals[0].ToString()),
                    (second, 24f, grid.Level.Settings.ActivePortals[1].ToString()) })
                {
                    Enemy enemy = spawns.SpawnWave(scene, random, request.Item3);
                    if (enemy == null) throw new InvalidOperationException("Scaled portal failed to spawn safely.");
                    enemy.Movement.SetPhysicsProcess(false);
                    Vector2 offset = enemy.GlobalPosition - request.Item1.GlobalPosition;
                    if (Mathf.Abs(offset.X) > request.Item2 + 0.01f || Mathf.Abs(offset.Y) > request.Item2 + 0.01f ||
                        !spawns.Paths.Navigation.HasClearance(enemy.GlobalPosition, enemy.Definition.RadiusPixels) ||
                        !spawns.Paths.Navigation.SegmentClear(request.Item1.GlobalPosition, enemy.GlobalPosition, enemy.Definition.RadiusPixels))
                        throw new InvalidOperationException("Scaled spread is unbounded or blocked.");
                    if (request.Item1 == second) largest = Mathf.Max(largest, Mathf.Max(Mathf.Abs(offset.X), Mathf.Abs(offset.Y)));
                    enemy.TakeDamage(10000);
                }
                Enemy debug = spawns.Spawn(scene, portalOnly: true); debug.Movement.SetPhysicsProcess(false);
                bool valid = false;
                foreach (EnemyPortal portal in new[] { first, second })
                {
                    Vector2 offset = debug.GlobalPosition - portal.GlobalPosition;
                    float extent = 8 * portal.SizeMultiplier;
                    if (Mathf.Abs(offset.X) <= extent + 0.01f && Mathf.Abs(offset.Y) <= extent + 0.01f) valid = true;
                }
                if (!valid) throw new InvalidOperationException("Debug spawning ignored portal size.");
                debug.TakeDamage(10000);
            }
            if (largest <= 8.1f) throw new InvalidOperationException("Large portal did not expand spawn variation.");
            float baseOffset = spawns.Paths.Config.PortalSpawnOffsetPixels;
            spawns.Paths.Config.PortalSpawnOffsetPixels = 0;
            Enemy centered = spawns.SpawnWave(scene, random, grid.Level.Settings.ActivePortals[1].ToString());
            if (centered.GlobalPosition != second.GlobalPosition) throw new InvalidOperationException("Zero base offset must stay centered even with size override.");
            centered.TakeDamage(10000); spawns.Paths.Config.PortalSpawnOffsetPixels = baseOffset;
            second.SizeMultiplier = float.NaN;
            if (second.SizeMultiplier != 1) throw new InvalidOperationException("Invalid portal size must fall back safely.");
            GD.Print("PASS: independent portal size overrides scale visual/debug/wave spread with clearance and unchanged navigation");
        }
        finally { first.SizeMultiplier = oldFirst; second.SizeMultiplier = oldSecond; }
    }
}
