// Checkpoint logistics preserves physical cargo and rebuilds references/reservations before dispatch resumes.
using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public partial class AmmunitionLogistics
{
    // =========================================================
    // Capture active delivery order, scheduler phase and real stock already aboard each drone.
    internal LogisticsSaveData CaptureCheckpoint() => new()
    {
        Jobs = _jobs.Select(j => new DeliverySaveData(j.Source.SaveId,
            GodotObject.IsInstanceValid(j.Destination) && !j.Destination.IsQueuedForDeletion() ? j.Destination.SaveId : "",
            j.Cargo.Amount, j.Reserved, j.Drone.Returning, SavePoint.From(j.Drone.GlobalPosition), j.Drone.Speed)).ToList(),
        Pending = _pending.Select(t => t.SaveId).ToList(), Factories = _factories.Select(t => t.SaveId).ToList(),
        Depots = _depots.Select(t => t.SaveId).ToList(), FactoryCursor = _factoryCursor, RequestCursor = _requestCursor,
        FactoryTurn = _factoryTurn, Elapsed = _elapsed, Lost = LostRounds, Delivered = DeliveredRounds
    };

    // =========================================================
    // No launch/removal transaction is replayed: cargo initializes from its saved amount, source stock stays unchanged.
    internal void RestoreCheckpoint(LogisticsSaveData data, IReadOnlyDictionary<string, TowerBase> towers)
    {
        if (_jobs.Count != 0) throw new InvalidOperationException("Restore requires an empty delivery service.");
        TowerBase Find(string id) => towers.TryGetValue(id, out var tower) ? tower : throw new InvalidDataException("Delivery endpoint is missing.");
        if (!_factories.Select(t => t.SaveId).ToHashSet().SetEquals(data.Factories) ||
            !_depots.Select(t => t.SaveId).ToHashSet().SetEquals(data.Depots))
            throw new InvalidDataException("Saved operational logistics endpoints differ.");
        _factories.Clear(); foreach (string id in data.Factories)
            _factories.Add(Find(id) as AmmunitionFactory ?? throw new InvalidDataException("Invalid factory endpoint."));
        _depots.Clear(); foreach (string id in data.Depots)
            _depots.Add(Find(id) as AmmunitionDepot ?? throw new InvalidDataException("Invalid depot endpoint."));
        foreach (var saved in data.Jobs)
        {
            var source = Find(saved.Source) as AmmoBuilding ?? throw new InvalidDataException("Invalid drone source.");
            var destination = saved.Destination.Length == 0 ? null : Find(saved.Destination);
            if (!Live(source) || (destination == null && !saved.Returning) || (destination != null && !Live(destination)))
                throw new InvalidDataException("Saved delivery endpoint is not operational.");
            if (saved.Cargo > source.DronePayload || BusySlots(source) >= source.DroneSlots)
                throw new InvalidDataException("Saved drone payload/slots exceed source capacity.");
            var inventory = destination == null ? null : Inventory(destination);
            if (destination != null && inventory == null) throw new InvalidDataException("Delivery target has no ammunition inventory.");
            if (saved.Reserved > (inventory?.FreeSpace ?? 0) - (destination == null ? 0 : ReservedSpace(destination)))
                throw new InvalidDataException("Delivery reservation exceeds available capacity.");
            var job = new AmmoDeliveryJob { Source = source, Destination = destination, DestinationInventory = inventory,
                Cargo = new AmmoInventory(source.DronePayload, saved.Cargo), Reserved = saved.Reserved };
            job.Drone = DroneScene.Instantiate<TransportDrone>();
            _jobs.Add(job); _busy[source] = BusySlots(source) + 1;
            if (destination != null)
            {
                _reserved[destination] = ReservedSpace(destination) + saved.Reserved;
                if (destination.Ammunition != null) _delivering.Add(destination, job);
            }
            job.Drone.Initialize(source, destination, saved.Speed, () => Arrive(job), () => Complete(job, false), () => Complete(job, true));
            AddChild(job.Drone); job.Drone.GlobalPosition = saved.Position.Vector();
            job.Drone.SetLoaded(saved.Cargo > 0); if (saved.Returning) job.Drone.ReturnHome();
        }
        // Registration may have generated demands from creation stock; replace that order after restoring all inventory.
        _pending.Clear(); _pendingSet.Clear();
        foreach (string id in data.Pending)
        {
            var tower = Find(id);
            if (!_stockHooks.ContainsKey(tower) || !Config.NeedsResupply(tower.Ammunition.Inventory.Amount, tower.Ammunition.Inventory.Capacity))
                throw new InvalidDataException("Invalid saved resupply demand.");
            _pending.Add(tower); _pendingSet.Add(tower);
        }
        foreach (var tower in _stockHooks.Keys)
        {
            bool low = Config.NeedsResupply(tower.Ammunition.Inventory.Amount, tower.Ammunition.Inventory.Capacity);
            if (low != _pendingSet.Contains(tower)) throw new InvalidDataException("Incomplete saved demand list.");
            tower.Ammunition.SetDeliveryStatus(low, _delivering.ContainsKey(tower));
        }
        _factoryCursor = data.FactoryCursor; _requestCursor = data.RequestCursor; _factoryTurn = data.FactoryTurn;
        _elapsed = data.Elapsed; LostRounds = data.Lost; DeliveredRounds = data.Delivered;
    }
}
