// Inherited remaining checkpoint obligations, captured branches and independent child movement routing.
using Godot;
using System;
using System.Linq;
using static MissionFixture;

internal static class DeathSpawnRouteChecks
{
    // =========================================================
    // Required regions are physically entered; no private movement fields are patched by either test or ability.
    internal static void Run(Node owner)
    {
        foreach (int stage in new[] { 0, 1, 3 })
        {
            using DeathSpawnFixture f = new(owner);
            RouteCheckpoint a = Checkpoint(f, "A", new(24, 20)), b = Checkpoint(f, "B", new(30, 18)),
                c = Checkpoint(f, "C", new(30, 22)), end = Checkpoint(f, "End", new(36, 20));
            a.NextCheckpoints = new() { a.GetPathTo(b), a.GetPathTo(c) }; b.NextCheckpoints = new() { b.GetPathTo(end) }; c.NextCheckpoints = new() { c.GetPathTo(end) };
            Enemy parent = f.Spawns.Catalog.Find("transporter_mk1").Instantiate<Enemy>(); f.Main.GetNode("World/Enemies").AddChild(parent);
            parent.GlobalPosition = f.Position; parent.Initialize(f.Spawns.Registry, f.Spawns.Paths, checkpoint: a, requiresCheckpoint: true);
            AmmoLoopFixture.Disable(parent);
            if (stage >= 1) { parent.GlobalPosition = a.GlobalPosition; parent.Movement._PhysicsProcess(0); }
            if (stage == 3)
            {
                parent.GlobalPosition = parent.Movement.Checkpoint.GlobalPosition; parent.Movement._PhysicsProcess(0);
                parent.GlobalPosition = end.GlobalPosition; parent.Movement._PhysicsProcess(0);
            }
            EnemyRouteHandoff captured = parent.Movement.CaptureRouteHandoff();
            f.Config(parent).ChildCount = 2; parent.TakeDamage(99999);
            // Link edits after death must not rewrite the captured branch plan.
            a.NextCheckpoints.Clear(); b.NextCheckpoints.Clear(); c.NextCheckpoints.Clear();
            f.FinishReleases();
            foreach (Enemy child in f.Spawns.Registry.Living)
            {
                EnemyRouteHandoff inherited = child.Movement.CaptureRouteHandoff();
                Require(inherited.Checkpoint == captured.Checkpoint && inherited.CompletedCount == captured.CompletedCount
                    && inherited.Complete == captured.Complete && ReferenceEquals(inherited.Network, captured.Network), "Exact remaining checkpoint/network snapshot inherited at stage " + stage);
                Require(child.Movement.CorridorSeed != parent.Movement.CorridorSeed && child.Movement.RouteProfile >= 0
                    && child.Definition.AllowPathDiversity && child.Definition.AllowPathOffsets, "Child retains own movement profile/seed/permissions");
            }
        }
        using (DeathSpawnFixture f = new(owner))
        {
            Enemy parent = f.Parent(); f.Config(parent).InheritCheckpointProgress = false; parent.TakeDamage(99999); f.Tick(.01);
            Require(!f.Spawns.Registry.Living.Single().Movement.RequiresCheckpoint, "Explicit opt-out routes directly to Citadel");
        }
        NarrowAndObstructed(owner); BrokenDestination(owner);
        GD.Print("PASS: before/mid/complete branched checkpoint inheritance, captured edits, independent movement, opt-out, narrow/blocked local positions and invalid destination safety");
    }
    // =========================================================
    // A clearance-valid portion of a narrow lane can be used, but newly occupied local space cannot be bypassed.
    private static void NarrowAndObstructed(Node owner)
    {
        using DeathSpawnFixture f = new(owner);
        for (int y = 17; y <= 23; y++) for (int x = 17; x <= 23; x++) if (y != 20)
        { f.Grid.Level.Passable.EraseCell(new(x, y)); f.Grid.Level.Impassable.SetCell(new(x, y), 0, Vector2I.Zero); }
        f.Grid.RebuildTerrain(); Enemy parent = f.Parent(); parent.TakeDamage(99999); f.FinishReleases();
        Require(f.Queue.SuccessfulChildren > 0 && f.Spawns.Registry.Living.All(child => f.Spawns.Paths.Navigation.HasClearance(child.GlobalPosition, child.Definition.RadiusPixels)), "Narrow one-cell lane safely reduces scatter");
        foreach (Enemy child in f.Spawns.Registry.Living.ToArray()) child.Free();
        parent = f.Parent(); f.Config(parent).ScatterRadiusPixels = 0; parent.TakeDamage(99999);
        Require(f.Grid.TryOccupy(new(20, 20)), "Tower obstacle appears after release is queued"); f.FinishReleases();
        Require(f.Queue.SkippedChildren == 6, "Tower-blocked captured origin never teleports to a portal");
    }
    // =========================================================
    // Deleted required nodes are not interpreted as permission to skip the remaining route.
    private static void BrokenDestination(Node owner)
    {
        using DeathSpawnFixture f = new(owner); RouteCheckpoint point = Checkpoint(f, "Required", new(25, 20));
        Enemy parent = f.Spawns.Catalog.Find("transporter_mk1").Instantiate<Enemy>(); f.Main.GetNode("World/Enemies").AddChild(parent);
        parent.GlobalPosition = f.Position; parent.Initialize(f.Spawns.Registry, f.Spawns.Paths, checkpoint: point, requiresCheckpoint: true);
        parent.TakeDamage(99999); point.Free(); f.FinishReleases();
        Require(f.Queue.SuccessfulChildren == 0 && f.Queue.SkippedChildren == 6, "Missing required checkpoint skips bounded releases safely");
    }
    private static RouteCheckpoint Checkpoint(DeathSpawnFixture f, string name, Vector2I cell)
    { RouteCheckpoint point = new() { Name = name, RadiusPixels = 24 }; f.Grid.Level.AddChild(point); point.GlobalPosition = f.Grid.CellToWorldCenter(cell); return point; }
}
