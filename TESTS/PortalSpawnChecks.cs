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
        GD.Print("PASS: portal-centered spawns vary safely within eight pixels on each axis");
    }
}
