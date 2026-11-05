// Shared distance-first capacitor refill, bounded by island production and cached conductive delivery.
using Godot;
using System;
using System.Collections.Generic;

public partial class PowerGridManager
{
    [Export(PropertyHint.Range, "0.1,1,0.05")] public double CapacitorServiceSeconds { get; set; } = .25;
    private readonly HashSet<TowerPowerConsumer> _capacitors = new();
    private readonly List<TowerPowerConsumer> _capacitorStep = new();
    private readonly Dictionary<int, double> _refillBudgets = new();
    private readonly List<TowerPowerConsumer> _capacitorOrder = new();
    private bool _capacitorOrderDirty = true;
    private int _allocationDeliveryRevision = -1;
    public int AllocationOrderRebuildCount { get; private set; }
    private double _serviceElapsed;
    private bool _servicingCapacitors;
    internal double CapacitorTime { get; private set; }
    // =========================================================
    // Registration/source/config/attack events wake one shared clock, never one process per tower.
    internal void RegisterCapacitor(TowerPowerConsumer consumer)
    { if (_capacitors.Add(consumer)) InvalidateCapacitorOrder(); WakeCapacitors(); }
    internal void InvalidateCapacitorOrder() => _capacitorOrderDirty = true;
    internal void UnregisterCapacitor(TowerPowerConsumer consumer)
    {
        if (_capacitors.Remove(consumer)) InvalidateCapacitorOrder();
        if (!_servicingCapacitors) _capacitorOrder.Remove(consumer);
        if (_capacitors.Count == 0) _serviceElapsed = 0; // Discard unused production time; never bank it for a future consumer.
        WakeCapacitors();
    }
    internal void WakeCapacitors() { if (IsInsideTree()) SetPhysicsProcess(_capacitors.Count > 0); }
    public override void _PhysicsProcess(double delta)
    {
        if (!double.IsFinite(delta) || delta <= 0 || GetTree().Paused) return;
        CapacitorTime += delta; _serviceElapsed += delta;
        double interval = double.IsFinite(CapacitorServiceSeconds) ? Math.Clamp(CapacitorServiceSeconds, .1, 1) : .25;
        if (_serviceElapsed >= interval) FlushCapacitors();
    }
    // =========================================================
    // Integrate once, prepare requests, then allocate nearest-first from one finite budget per island.
    internal void FlushCapacitors()
    {
        if (_servicingCapacitors || _serviceElapsed <= 0) return;
        _servicingCapacitors = true;
        double elapsed = _serviceElapsed; _serviceElapsed = 0;
        try
        {
            EnsureDelivery();
            _refillBudgets.Clear();
            _capacitorStep.Clear(); _capacitorStep.AddRange(_capacitors);
            foreach (TowerPowerConsumer consumer in _capacitorStep)
                if (consumer.Live) consumer.PrepareRefill();
            EnsureCapacitorOrder();
            foreach (TowerPowerConsumer consumer in _capacitorOrder)
            {
                if (!consumer.Live) continue;
                int region = EffectiveNetwork(consumer.Region);
                if (!_refillBudgets.TryGetValue(region, out double remaining))
                    remaining = Math.Max(0, EffectiveSupply(consumer.Region) * elapsed);
                double granted = Math.Min(remaining, consumer.DesiredRefill);
                double credited = consumer.CreditRefill(granted, elapsed);
                _refillBudgets[region] = Math.Max(0, remaining - credited);
            }
            // Finish all credits before callbacks can change sources/topology or dispose another consumer.
            foreach (TowerPowerConsumer consumer in _capacitorStep) if (consumer.Live) consumer.Publish();
            bool awake = false;
            foreach (TowerPowerConsumer consumer in _capacitors) if (consumer.NeedsService) { awake = true; break; }
            SetPhysicsProcess(awake);
        }
        finally
        {
            _capacitorStep.Clear(); _servicingCapacitors = false;
            // Callbacks may remove consumers after allocation; don't retain dead towers while asleep.
            if (_capacitorOrderDirty) _capacitorOrder.Clear();
        }
    }
    // =========================================================
    // Dirty-only sorting uses cached fields, never delivery queries/BFS inside the comparer.
    // Snapshot preparation has refreshed every live consumer before this cache is consumed.
    private void EnsureCapacitorOrder()
    {
        if (!_capacitorOrderDirty && _allocationDeliveryRevision == _deliveryRevision) return;
        _capacitorOrder.Clear(); _capacitorOrder.AddRange(_capacitorStep);
        _capacitorOrder.Sort(CapacitorDistanceComparer.Instance);
        _capacitorOrderDirty = false; _allocationDeliveryRevision = _deliveryRevision;
        AllocationOrderRebuildCount++;
    }
    private sealed class CapacitorDistanceComparer : IComparer<TowerPowerConsumer>
    {
        internal static readonly CapacitorDistanceComparer Instance = new();
        public int Compare(TowerPowerConsumer a, TowerPowerConsumer b)
        {
            int distance = a.CachedConductiveDistance.CompareTo(b.CachedConductiveDistance);
            return distance != 0 ? distance : StringComparer.Ordinal.Compare(a.AllocationSaveId, b.AllocationSaveId);
        }
    }
    // =========================================================
    // No source nodes, consumer callbacks or accumulated time survive a level unload.
    private void ClearCapacitors()
    {
        foreach (TowerPowerConsumer consumer in _capacitors) consumer.DetachGrid();
        _capacitors.Clear();
        if (!_servicingCapacitors) _capacitorStep.Clear();
        _refillBudgets.Clear(); _capacitorOrder.Clear(); _capacitorOrderDirty = true;
        _allocationDeliveryRevision = -1; _serviceElapsed = 0; SetPhysicsProcess(false);
    }
}

