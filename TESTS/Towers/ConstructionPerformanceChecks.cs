// Active-only scheduler benchmark, separated from enemy navigation and rendering costs.
using Godot;
using System.Diagnostics;
using System.Collections.Generic;
using static AmmoLoopFixture;

internal static class ConstructionPerformanceChecks
{
    // =========================================================
    // 500 idle buildings stay outside scheduler work;100 simultaneous jobs complete without per-frame snapshots.
    internal static void Run(Node runner)
    {
        Node scope = new(); runner.AddChild(scope); ConstructionWorkManager manager = new(); scope.AddChild(manager);
        try
        {
            for (int i = 0; i < 500; i++) ConstructionChecks.Site(scope, manager, 0);
            List<PassiveTestTower> active = new();
            for (int i = 0; i < 100; i++) active.Add(ConstructionChecks.Site(scope, manager, 10));
            double total = 0, peak = 0; Stopwatch watch = new();
            for (int i = 0; i < 600; i++)
            {
                watch.Restart(); manager.Advance(1.0 / 60); watch.Stop();
                total += watch.Elapsed.TotalMilliseconds; peak = System.Math.Max(peak, watch.Elapsed.TotalMilliseconds);
                Require(manager.LastJobsVisited == 100, "Only100 active jobs visited, never500 idle buildings");
            }
            Require(manager.ActiveCount == 0 && !manager.IsPhysicsProcessing(), "Simultaneous completion retires all100 jobs");
            foreach (PassiveTestTower tower in active) Require(tower.IsOperational, "No skipped simultaneous completion");
            manager.Advance(1); Require(manager.LastJobsVisited == 0, "Empty manager visits no buildings");
            for (int i = 0; i < 100; i++) ConstructionChecks.Site(scope, manager, 10);
            GD.Print($"CONSTRUCTION BENCH 500 idle +100 active,600 ticks: mean={total / 600:F4}ms peak={peak:F4}ms;100 visited/tick,0 when idle; not rendered FPS");
        }
        finally { scope.Free(); }
    }
}
