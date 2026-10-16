// Mixed-type permission matrix and neutral-profile oracle; invoked inside the isolated corridor arena.
using Godot;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
public static class MovementPermissionChecks
{
    // =========================================================
    // Exercise real initialized ships without modifying any authored definition or wave RNG.
    public static void Run(Node parent, SpawnManager spawns, Vector2 start, Vector2I goal)
    {
        PathRequestManager paths = spawns.Paths; NavigationGrid nav = paths.Navigation; EnemyMasterConfig c = paths.Config;
        c.EnablePathDiversity = false;
        Vector2[] shortest = nav.FindPath(start, goal, 0, out float shortestCost);
        c.EnablePathDiversity = true; c.PathDiversityStrength = 0.6f; c.CongestionInfluence = 2;
        var ships = new List<Enemy>(); var definitions = new List<EnemyDefinition>();
        var clock = Stopwatch.StartNew(); int cases = 0;
        try
        {
            foreach (bool master in new[] { false, true })
                foreach (int percent in new[] { 0, 50, 100 })
                    for (int flags = 0; flags < 8; flags++)
                    {
                        c.EnableCorridorVariation = master; c.EnablePathOffsets = master; c.EnablePathDiversity = master;
                        c.CorridorVariationPercent = percent;
                        string type = flags % 3 == 0 ? "Enemy004" : flags % 3 == 1 ? "Enemy001" : "Enemy002";
                        Enemy ship = GD.Load<PackedScene>($"res://ENEMIES/Types/{type}/{type}.tscn").Instantiate<Enemy>();
                        EnemyDefinition d = (EnemyDefinition)ship.Definition.Duplicate(true); definitions.Add(d);
                        d.AllowCorridorVariation = (flags & 1) != 0; d.AllowPathOffsets = (flags & 2) != 0; d.AllowPathDiversity = (flags & 4) != 0;
                        if (flags == 0) d.EnemyId = "permission_fixture";
                        ship.Definition = d; parent.AddChild(ship); ship.GlobalPosition = start;
                        ship.Initialize(spawns.Registry, paths); ship.Movement.SetPhysicsProcess(false); ships.Add(ship);
                        EnemyMovement m = ship.Movement;
                        bool selected = d.AllowCorridorVariation && CorridorPreference.Selected(m.CorridorSeed, c);
                        Require(m.UsesCorridorVariation == selected, "Eligibility and deterministic percentage selection");
                        Require((m.RouteProfile == NavigationGrid.NeutralProfile) == !d.AllowPathDiversity, "Only disallowed ships use reserved neutral profile");
                        Vector2[] logical = nav.FindPath(start, goal, m.RouteProfile, out _);
                        m.AcceptPath(logical, nav.Revision);
                        if (!selected) Require(m.CorridorSamplingChecks == 0, "Disabled corridor bypasses N4 probes");
                        if (!selected)
                        {
                            EnemyRoute oracle = new(); oracle.Build(logical, start, nav, d.RadiusPixels,
                                master && d.AllowPathOffsets ? m.LateralOffsetPixels : 0);
                            Require(m.Waypoints.SequenceEqual(oracle.Points), "Independent offset gate matches ordinary builder");
                        }
                        else
                        {
                            EnemyRoute oracle = new(); oracle.BuildCorridor(logical, start, nav, d.RadiusPixels,
                                master && d.AllowPathOffsets ? m.LateralOffsetPixels : 0, m.CorridorSeed, c);
                            Require(m.Waypoints.SequenceEqual(oracle.Points), "Corridor and offsets are independent");
                        }
                        Vector2[] accepted = (Vector2[])m.Waypoints.Clone(); uint seed = m.CorridorSeed;
                        m.AcceptPath(logical, nav.Revision);
                        Require(seed == m.CorridorSeed && selected == m.UsesCorridorVariation && accepted.SequenceEqual(m.Waypoints), "Stable forced repath");
                        cases++;
                    }
            c.EnablePathDiversity = true; c.CongestionInfluence = 2; nav.UpdateCongestion(10);
            Vector2[] neutral = nav.FindPath(start, goal, NavigationGrid.NeutralProfile, out float cost);
            Require(neutral.SequenceEqual(shortest) && Mathf.IsEqualApprox(cost, shortestCost), "Neutral A* equals master-off shortest-path oracle despite crowd");
            Require(Mathf.IsEqualApprox(nav.RouteCost(start, neutral, NavigationGrid.NeutralProfile), cost), "Neutral multi-goal comparison ignores crowd and weights");
            nav.FindPath(start, goal, 0, out float weightedCost);
            Require(weightedCost > cost && nav.TotalSharedProfileCount == c.RouteProfileCount + 1, "Weighted profile zero is distinct; exactly one shared neutral grid");
            Vector2I block = nav.Grid.WorldToCell(neutral[neutral.Length / 2]);
            Require(nav.Grid.TryOccupy(block), "Place obstruction for mixed profile update");
            try
            {
                foreach (int profile in new[] { NavigationGrid.NeutralProfile, 0 })
                    foreach (Vector2I target in new[] { goal, goal + new Vector2I(0, 1) })
                    {
                        Vector2[] route = nav.FindPath(start, target, profile, out _);
                        Require(route.Length > 0 && route.All(p => nav.Grid.WorldToCell(p) != block), "Both shared grids receive incremental occupancy");
                        for (int i = 1; i < route.Length; i++) Require(nav.ConnectionClear(route[i - 1], route[i], 24), "Large valid radius retains clearance and corner safety");
                    }
            }
            finally { nav.Grid.ReleaseOccupancy(block); }
            Require(nav.FindPath(start, goal, NavigationGrid.NeutralProfile, out _).SequenceEqual(shortest), "Removal restores neutral oracle");
            GD.Print($"PASS: movement permissions {cases} mixed-type/master/percentage combinations; neutral shortest/crowd/cost/occupancy/large-radius oracle; {clock.Elapsed.TotalMilliseconds:0.00}ms fixture (not FPS)");
        }
        finally
        {
            foreach (Enemy ship in ships) ship.Free();
            foreach (EnemyDefinition d in definitions) d.Dispose();
            c.EnablePathDiversity = false; c.EnablePathOffsets = false; c.CongestionInfluence = 0;
            c.EnableCorridorVariation = true; c.CorridorVariationPercent = 100;
        }
    }
    // =========================================================
    // Stop the headless suite on a permission or shared-profile isolation regression.
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
