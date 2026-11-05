// PW4-C narrow production-service checks; disposable capacitors, no simulation loop or profile I/O.
using Godot;
using System;

internal static class PowerAllocationChecks
{
    // =========================================================
    // Reuse the tiny transformed fixture's connected terrain; only three manually stepped consumers.
    internal static void Run(PowerGridManager power, WorldGrid grid, Node2D world, Vector2I near, Vector2I middle)
    {
        power.DeliveryConfig = new PowerDeliveryConfig { FullEfficiencyCells = 100 };
        power.SetProducer("allocation:source", near, Vector2I.One, 10);
        Node2D towers = new() { Name = "Towers" }; world.AddChild(towers);
        TowerDefinition definition = GD.Load<TowerDefinition>("res://TOWERS/Offensive/BattleTower/BattleTowerDefinition.tres");
        BattleTower a = Add(near), b = Add(middle), c = Add(new(6, 4));
        try
        {
            power._PhysicsProcess(1);
            Near(Energy(a), 5, "Nearest gets requested rate"); Near(Energy(b), 5, "Middle gets remaining supply");
            Near(Energy(c), 0, "Reachable far tower can receive zero under scarcity");
            Near(c.Power.RechargeCeilingPerSecond, 5, "Potential ceiling remains positive while starved");
            Near(c.Power.LastDeliveredRefillPerSecond, 0, "Actual delivery is distinct from ceiling");
            int sorts = power.AllocationOrderRebuildCount, bfs = power.DistanceRebuildCount;
            a.Power.SetPriority(0); c.Power.SetPriority(100); power._PhysicsProcess(1);
            Near(Energy(a), 10, "Priority metadata cannot override proximity"); Near(Energy(c), 0, "Far high-priority metadata stays dormant");
            Require(power.AllocationOrderRebuildCount == sorts && power.DistanceRebuildCount == bfs,
                "Unchanged service steps reuse sort and distance caches");
            Restore(a, 99); double before = Sum(); power._PhysicsProcess(1);
            Near(Energy(a), 100, "Near request is limited by headroom"); Near(Energy(b), 15, "Middle keeps full request");
            Near(Energy(c), 4, "Unused near allocation reaches far tower in the same interval");
            Near(Sum() - before, 10, "Credited refill conserves the finite island budget");
            power.SetProducer("allocation:source", near, Vector2I.One, 20);
            before = Sum(); power._PhysicsProcess(1);
            Near(Sum() - before, 10, "Abundant output satisfies requests without banking unused generation");
            Require(power.DistanceRebuildCount == bfs, "Positive output edits do not rebuild BFS");

            b.GlobalPosition = grid.CellToWorldCenter(near); b.ForceUpdateTransform();
            Restore(a, 0); Restore(b, 0); Restore(c, 0);
            power.SetProducer("allocation:source", near, Vector2I.One, 5); power._PhysicsProcess(1);
            BattleTower first = StringComparer.Ordinal.Compare(a.SaveId, b.SaveId) < 0 ? a : b;
            BattleTower second = first == a ? b : a;
            Near(Energy(first), 5, "Equal-distance ties follow ordinal saved identity");
            Near(Energy(second), 0, "Later equal-distance identity waits without over-allocation");
            PowerSaveData saved = first.Power.Capture(); first.Power.Restore(saved);
            first.Power.Restore(new(0, saved.Priority, false, 0, 0));
            power._PhysicsProcess(1);
            Near(Energy(first), 5, "Restored capacitor retains the same deterministic tie order");
            Near(Energy(second), 0, "Restore does not rotate or duplicate the budget");

            Restore(c, 10); c.GlobalPosition = grid.CellToWorldCenter(new(0, 0)); c.ForceUpdateTransform();
            Require(c.Power.ConductiveDistanceCells == -1, "Disconnected capacitor has no delivery route");
            Require(c.Power.TryFundAttack(.1, null), "Stored charge remains spendable despite zero incoming energy");
            c.Power.Publish(); double stored = Energy(c);
            power.RemoveEndpoint("allocation:source"); before = Sum(); power._PhysicsProcess(1);
            Near(Sum(), before, "Source removal preserves all stored energy without phantom refill");
            power.SetProducer("allocation:source", near, Vector2I.One, 5); power._PhysicsProcess(1);
            Near(Energy(c), stored, "Source restoration never credits a disconnected island");
            before = Sum(); world.GetTree().Paused = true;
            try { power._PhysicsProcess(1); } finally { world.GetTree().Paused = false; }
            Near(Sum(), before, "Paused time produces no allocation");
        }
        finally
        {
            a.Free(); b.Free(); c.Free(); towers.Free();
            power.RemoveEndpoint("allocation:source"); power.DeliveryConfig = null;
        }

        BattleTower Add(Vector2I cell)
        {
            BattleTower tower = definition.Scene.Instantiate<BattleTower>(); tower.InitializeDefinition(definition);
            tower.AuthoredConfig.Power.RechargeRatePerSecond = 5;
            tower.Position = world.ToLocal(grid.CellToWorldCenter(cell)); towers.AddChild(tower); return tower;
        }
        double Sum() => Energy(a) + Energy(b) + Energy(c);
    }
    private static void Restore(BattleTower tower, double energy) => tower.Power.Restore(new(energy, tower.Power.PowerPriority, false, 0, 0));
    private static double Energy(BattleTower tower) => tower.Power.Capacitor.Energy;
    private static void Near(double actual, double expected, string message) => Require(Math.Abs(actual - expected) < 1e-8, message);
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
