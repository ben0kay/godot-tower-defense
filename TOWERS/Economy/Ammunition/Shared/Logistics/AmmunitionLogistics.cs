// One world-owned, budgeted dispatcher; exact physical cargo, incoming reservations and finite home drone slots.
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class AmmunitionLogistics : Node2D
{
    [Export] public AmmunitionMasterConfig Config { get; set; }
    [Export] public PackedScene DroneScene { get; set; }
    [Export(PropertyHint.Range, "0.05,5,0.05")] public float DispatchIntervalSeconds { get; set; } = .2f;
    [Export(PropertyHint.Range, "1,128,1")] public int SchedulingChecksPerInterval { get; set; } = 8;
    private readonly List<AmmunitionFactory> _factories = new();
    private readonly List<AmmunitionDepot> _depots = new();
    private readonly List<TowerBase> _pending = new();
    private readonly HashSet<TowerBase> _pendingSet = new();
    private readonly Dictionary<TowerBase, Action> _exitHooks = new(), _stockHooks = new();
    private readonly Dictionary<TowerBase, int> _reserved = new(), _busy = new();
    private readonly Dictionary<TowerBase, AmmoDeliveryJob> _delivering = new();
    private readonly List<AmmoDeliveryJob> _jobs = new();
    private int _factoryCursor, _requestCursor;
    private bool _shutdown, _factoryTurn = true;
    private double _elapsed;
    public IReadOnlyList<AmmoDeliveryJob> Jobs => _jobs;
    public long LostRounds { get; private set; }
    public long DeliveredRounds { get; private set; }
    public int PendingRequests => _pending.Count;

    // =========================================================
    // Sources register directly; there are no scene-tree or enemy-registry discovery scans.
    public override void _Ready()
    {
        Config ??= GD.Load<AmmunitionMasterConfig>("res://TOWERS/Shared/Ammunition/DefaultAmmunitionConfig.tres");
        DroneScene ??= GD.Load<PackedScene>("res://DRONES/Transport/TransportDrone.tscn");
        Config.Changed += RefreshRequests; SetProcess(false);
    }
    public int BusySlots(AmmoBuilding source) => _busy.GetValueOrDefault(source);
    public int ReservedSpace(TowerBase destination) => _reserved.GetValueOrDefault(destination);
    private static bool Live(TowerBase node) => GodotObject.IsInstanceValid(node) && node.IsInsideTree() && !node.IsQueuedForDeletion() && node.IsOperational;
    private static AmmoInventory Inventory(TowerBase owner) => owner is AmmoBuilding building ? building.Inventory : owner.Ammunition?.Inventory;

    // =========================================================
    // Register inventories once and retire both callbacks when an endpoint or this world leaves.
    private void Watch(TowerBase owner)
    {
        if (_exitHooks.ContainsKey(owner)) return;
        Action exit = () => Unregister(owner);
        _exitHooks.Add(owner, exit); owner.TreeExiting += exit;
    }
    public void RegisterBuilding(AmmoBuilding building)
    {
        if (_shutdown || !Live(building)) return;
        Watch(building);
        if (building is AmmunitionFactory factory && !_factories.Contains(factory)) _factories.Add(factory);
        if (building is AmmunitionDepot depot && !_depots.Contains(depot)) _depots.Add(depot);
    }
    public void RegisterConsumer(TowerBase tower)
    {
        if (_shutdown || !Live(tower) || tower.Ammunition == null || _stockHooks.ContainsKey(tower)) return;
        Watch(tower); Action changed = () => UpdateRequest(tower);
        _stockHooks.Add(tower, changed); tower.Ammunition.Inventory.Changed += changed;
        UpdateRequest(tower);
    }

    // =========================================================
    // Inventory changes update one demand; configuration edits refresh consumers once, not every frame.
    private void RefreshRequests() { foreach (TowerBase tower in _stockHooks.Keys) UpdateRequest(tower); }
    private void UpdateRequest(TowerBase tower)
    {
        if (_shutdown || !Live(tower)) return;
        AmmoInventory stock = tower.Ammunition.Inventory;
        bool low = (Config ?? GD.Load<AmmunitionMasterConfig>("res://TOWERS/Shared/Ammunition/DefaultAmmunitionConfig.tres")).NeedsResupply(stock.Amount, stock.Capacity);
        if (low && _pendingSet.Add(tower)) _pending.Add(tower);
        if (!low && _pendingSet.Remove(tower)) _pending.Remove(tower);
        tower.Ammunition.SetDeliveryStatus(low, _delivering.ContainsKey(tower));
    }

    // =========================================================
    // Fixed work count and interval: no per-frame factory×depot searches or all-tower polling.
    public override void _PhysicsProcess(double delta)
    {
        if (_shutdown || !double.IsFinite(delta) || delta <= 0) return;
        _elapsed += delta;
        double interval = float.IsFinite(DispatchIntervalSeconds) ? Math.Max(.05, DispatchIntervalSeconds) : .2;
        if (_elapsed < interval) return;
        _elapsed = 0;
        for (int i = 0; i < Math.Clamp(SchedulingChecksPerInterval, 1, 128); i++)
        {
            bool factoryTurn = _factoryTurn; _factoryTurn = !_factoryTurn;
            if (factoryTurn && _factories.Count > 0)
            {
                _factoryCursor %= _factories.Count;
                TryFactory(_factories[_factoryCursor++]);
            }
            if (!factoryTurn && _pending.Count > 0)
            {
                _requestCursor %= _pending.Count;
                TryConsumer(_pending[_requestCursor++]);
            }
        }
    }

    // =========================================================
    // Nearest eligible depot wins; equal-distance ties retain stable registration order.
    private void TryFactory(AmmunitionFactory factory)
    {
        if (!Live(factory)) return;
        int shipment = factory.Config.ShipmentAmount;
        if (shipment <= 0 || shipment > factory.DronePayload
            || factory.Inventory.Amount < shipment || BusySlots(factory) >= factory.DroneSlots) return;
        AmmunitionDepot best = null; float distance = float.PositiveInfinity;
        foreach (AmmunitionDepot depot in _depots)
        {
            if (!Live(depot) || depot.Inventory.FreeSpace - ReservedSpace(depot) < shipment) continue;
            float candidate = factory.GlobalPosition.DistanceSquaredTo(depot.GlobalPosition);
            if (candidate < distance) { best = depot; distance = candidate; }
        }
        if (best != null) Launch(factory, best);
    }
    private void TryConsumer(TowerBase tower)
    {
        if (!Live(tower) || _delivering.ContainsKey(tower)) return;
        UpdateRequest(tower);
        if (!_pendingSet.Contains(tower)) return;
        AmmunitionDepot best = null; float distance = float.PositiveInfinity;
        foreach (AmmunitionDepot depot in _depots)
        {
            if (!Live(depot) || depot.Inventory.Amount == 0 || BusySlots(depot) >= depot.DroneSlots) continue;
            float candidate = tower.GlobalPosition.DistanceSquaredTo(depot.GlobalPosition);
            if (candidate < distance) { best = depot; distance = candidate; }
        }
        if (best != null) Launch(best, tower);
    }

    // =========================================================
    // Loading removes physical stock exactly once; cargo also serves as the source-stock reservation.
    private void Launch(AmmoBuilding source, TowerBase destination)
    {
        if (!Live(source) || !Live(destination)) return;
        AmmoInventory target = Inventory(destination);
        int freeRoom = target.FreeSpace - ReservedSpace(destination);
        int amount;
        if (source is AmmunitionFactory factory)
        {
            // Factory shipments are ALWAYS one full configured load, never a premature or partial flight.
            amount = factory.Config.ShipmentAmount;
            if (amount <= 0 || amount > source.DronePayload || amount > source.Inventory.Amount || amount > freeRoom) return;
        }
        else amount = Math.Min(source.DronePayload, Math.Min(source.Inventory.Amount, freeRoom));
        if (amount <= 0) return;
        AmmoDeliveryJob job = new() { Source = source, Destination = destination, Cargo = new AmmoInventory(source.DronePayload), DestinationInventory = target };
        TransportDrone drone = DroneScene.Instantiate<TransportDrone>(); job.Drone = drone;
        if (!source.Inventory.TryTransferTo(job.Cargo, amount)) { drone.Free(); return; }
        job.Reserved = amount;
        _reserved[destination] = ReservedSpace(destination) + amount;
        _busy[source] = BusySlots(source) + 1; _jobs.Add(job);
        if (destination.Ammunition != null) _delivering.Add(destination, job);
        drone.Initialize(source, destination, source.DroneSpeed, () => Arrive(job), () => Complete(job, false), () => Complete(job, true));
        AddChild(drone);
        source.NotifyRuntimeChanged(); destination.NotifyRuntimeChanged();
        if (destination.Ammunition != null) UpdateRequest(destination);
    }

    // =========================================================
    // Only arrival unloads. A full/changed/removed destination returns cargo instead of minting or overfilling.
    private void Arrive(AmmoDeliveryJob job)
    {
        if (job.Finished) return;
        ReleaseReservation(job);
        if (Live(job.Destination) && ReferenceEquals(Inventory(job.Destination), job.DestinationInventory))
        {
            int amount = Math.Min(job.Cargo.Amount, job.DestinationInventory.FreeSpace);
            if (amount > 0 && job.Cargo.TryTransferTo(job.DestinationInventory, amount)) DeliveredRounds += amount;
        }
        job.Drone.SetLoaded(job.Cargo.Amount > 0); job.Drone.ReturnHome();
    }
    private void ReleaseReservation(AmmoDeliveryJob job)
    {
        if (job.Reserved <= 0) return;
        _reserved[job.Destination] = Math.Max(0, ReservedSpace(job.Destination) - job.Reserved); job.Reserved = 0;
        if (Live(job.Destination)) job.Destination.NotifyRuntimeChanged();
    }

    // =========================================================
    // Return leftover stock where possible. Source/drone destruction or a full home explicitly loses remaining cargo.
    private void Complete(AmmoDeliveryJob job, bool destroyed)
    {
        if (job.Finished) return;
        job.Finished = true; ReleaseReservation(job);
        if (!destroyed && Live(job.Source))
        {
            int returned = Math.Min(job.Cargo.Amount, job.Source.Inventory.FreeSpace);
            if (returned > 0) job.Cargo.TryTransferTo(job.Source.Inventory, returned);
        }
        if (job.Cargo.Amount > 0) { LostRounds += job.Cargo.Amount; job.Cargo.TryConsume(job.Cargo.Amount); }
        _jobs.Remove(job);
        _busy[job.Source] = Math.Max(0, _busy.GetValueOrDefault(job.Source) - 1);
        if (DestinationIsConsumer(job))
        { _delivering.Remove(job.Destination); if (Live(job.Destination)) UpdateRequest(job.Destination); }
        if (Live(job.Source)) job.Source.NotifyRuntimeChanged();
    }
    private bool DestinationIsConsumer(AmmoDeliveryJob job) => _delivering.TryGetValue(job.Destination, out AmmoDeliveryJob active) && active == job;

    // =========================================================
    // Endpoint removal releases reservations and source slots; active callbacks cannot keep unloaded scenes alive.
    private void Unregister(TowerBase owner)
    {
        if (_exitHooks.Remove(owner, out Action exit)) owner.TreeExiting -= exit;
        if (_stockHooks.Remove(owner, out Action changed)) owner.Ammunition.Inventory.Changed -= changed;
        _pendingSet.Remove(owner); _pending.Remove(owner);
        if (owner is AmmunitionFactory factory) _factories.Remove(factory);
        if (owner is AmmunitionDepot depot) _depots.Remove(depot);
        foreach (AmmoDeliveryJob job in _jobs.ToArray())
        {
            if (job.Source == owner) { Complete(job, true); job.Drone.QueueFree(); }
            else if (job.Destination == owner) { ReleaseReservation(job); job.Drone.ReturnHome(); }
        }
        AmmoInventory inventory = Inventory(owner);
        if (!_shutdown && inventory?.Amount > 0) LostRounds += inventory.Amount;
        _reserved.Remove(owner); _busy.Remove(owner); _delivering.Remove(owner);
    }
    public override void _ExitTree()
    {
        _shutdown = true;
        if (GodotObject.IsInstanceValid(Config)) Config.Changed -= RefreshRequests;
        foreach (TowerBase owner in _exitHooks.Keys.ToArray()) Unregister(owner);
        foreach (AmmoDeliveryJob job in _jobs.ToArray()) Complete(job, true);
    }
}
