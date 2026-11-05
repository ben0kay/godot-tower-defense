// Narrow PW5 real-building checks in the existing tiny transformed terrain fixture; no gameplay loops or disk I/O.
using Godot;
using System;
using System.Collections.Generic;

internal static class PowerDistributionChecks
{
    internal static void Links(PowerGridManager power, WorldGrid grid, Node2D world, Vector2I left, Vector2I right)
    {
        Node2D towers = new() { Name = "Towers" }; world.AddChild(towers);
        var a = Add<PowerTransmitter>("PowerTransmitter", left);
        var b = Add<PowerTransmitter>("PowerTransmitter", right);
        TowerDefinition battle = GD.Load<TowerDefinition>("res://TOWERS/Offensive/BattleTower/BattleTowerDefinition.tres");
        var consumer = battle.Scene.Instantiate<BattleTower>(); consumer.InitializeDefinition(battle);
        consumer.Position = world.ToLocal(grid.CellToWorldCenter(right)); towers.AddChild(consumer);
        try
        {
            power.SetProducer("pw5:left", left, Vector2I.One, 3);
            Require(consumer.Power.RechargeCeilingPerSecond == 0, "Unlinked receiving island cannot refill");
            Require(!a.TryLink(a, out _) && a.TryLink(b, out _), "Reject self and permit explicit cross-island pair");
            Require(a.PeerSaveId == b.SaveId && b.PeerSaveId == a.SaveId, "Peer references are reciprocal stable SaveIds");
            Require(consumer.Power.ConductiveDistanceCells == 8 && power.EffectiveSupply(power.RegionAt(right)) == 3,
                "Remote source-less island receives a positive-hop route and one finite budget");
            power._PhysicsProcess(1);
            Near(consumer.Power.Capacitor.Energy, 3, "Actual receiving recharge cannot exceed source output");
            power.SetProducer("pw5:right", right, Vector2I.One, 2);
            Near(power.EffectiveSupply(power.RegionAt(left)), 5, "Both producers count once in the joined component");
            var saved = new[] { a.CaptureCheckpoint(_ => ""), b.CaptureCheckpoint(_ => "") };
            a.Unlink(); double stored = consumer.Power.Capacitor.Energy;
            power.RemoveEndpoint("pw5:right");
            Require(consumer.Power.RechargeCeilingPerSecond == 0 && consumer.Power.Capacitor.Energy == stored,
                "Unlink removes future delivery without erasing stored charge");
            power.RestoreTransmitterLinks(new Dictionary<string, TowerBase> { [a.SaveId] = a, [b.SaveId] = b }, saved);
            Require(consumer.Power.ConductiveDistanceCells == 8, "Checkpoint peer resolution restores derived links");
            TowerDisableHandle emp = b.ApplyDisable(TowerDisableReason.EMP);
            Require(consumer.Power.RechargeCeilingPerSecond == 0 && a.PeerSaveId == b.SaveId, "EMP suspends connectivity and retains pairing");
            b.RemoveDisable(emp);
            Require(consumer.Power.ConductiveDistanceCells == 8, "Recovery reactivates retained pair");
            b.Free(); Require(a.PeerSaveId.Length == 0, "Removal safely clears surviving peer");
        }
        finally
        {
            towers.Free(); power.RemoveEndpoint("pw5:left"); power.RemoveEndpoint("pw5:right");
        }
        T Add<T>(string type, Vector2I cell) where T : PowerDistributionBuilding
        {
            var definition = GD.Load<TowerDefinition>($"res://TOWERS/Utility/{type}/{type}Definition.tres");
            T tower = definition.Scene.Instantiate<T>(); tower.InitializeDefinition(definition);
            tower.Position = world.ToLocal(grid.CellToWorldCenter(cell)); towers.AddChild(tower); return tower;
        }
    }
    internal static void Relay(PowerGridManager power, WorldGrid grid, Node2D world, Vector2I source, Vector2I far)
    {
        Node2D towers = new() { Name = "Towers" }; world.AddChild(towers);
        var definition = GD.Load<TowerDefinition>("res://TOWERS/Utility/PowerRelay/PowerRelayDefinition.tres");
        var relay = definition.Scene.Instantiate<PowerRelay>(); relay.InitializeDefinition(definition);
        relay.Position = world.ToLocal(grid.CellToWorldCenter(far)); towers.AddChild(relay);
        try
        {
            power.DeliveryConfig = new PowerDeliveryConfig { FullEfficiencyCells = 0, FalloffCells = 1 };
            Require(power.DeliveryForFootprint(far, Vector2I.One).Efficiency == 0, "Source-less Relay never boosts");
            power.SetProducer("pw5:relay", source, Vector2I.One, 10);
            var delivery = power.DeliveryForFootprint(far, Vector2I.One);
            Require(delivery.DistanceCells == 2, "Relay cannot change scarcity ordering distance");
            Near(delivery.Efficiency, .85, "Eligible local Relay raises possible recharge");
            var emp = relay.ApplyDisable(TowerDisableReason.EMP);
            Near(power.DeliveryForFootprint(far, Vector2I.One).Efficiency, 1.0 / 3, "Disabled Relay restores original falloff");
            relay.RemoveDisable(emp); power.RemoveEndpoint("pw5:relay");
            Near(power.DeliveryForFootprint(far, Vector2I.One).Efficiency, 0, "Last source loss removes boost");
        }
        finally { towers.Free(); power.RemoveEndpoint("pw5:relay"); power.DeliveryConfig = null; }
    }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Near(double value, double expected, string message) => Require(Math.Abs(value - expected) < 1e-6, message);
}
