// PW4-A/B refill: cached local distance ceilings, bounded by island production; priority remains PW4-C.
using Godot;
using System;
using System.Collections.Generic;

public partial class PowerGridManager
{
    [Export(PropertyHint.Range, "0.1,1,0.05")] public double CapacitorServiceSeconds { get; set; } = .25;
    private readonly HashSet<TowerPowerConsumer> _capacitors = new();
    private readonly List<TowerPowerConsumer> _capacitorStep = new();
    private readonly Dictionary<int, double> _refillRequests = new();
    private double _serviceElapsed;
    private bool _servicingCapacitors;
    internal double CapacitorTime { get; private set; }
    // =========================================================
    // Registration/source/config/attack events wake one shared clock, never one process per tower.
    internal void RegisterCapacitor(TowerPowerConsumer consumer) { _capacitors.Add(consumer); WakeCapacitors(); }
    internal void UnregisterCapacitor(TowerPowerConsumer consumer)
    {
        _capacitors.Remove(consumer);
        if (_capacitors.Count == 0) _serviceElapsed = 0; // Discard unused production time; never bank it for a future consumer.
        WakeCapacitors();
    }
    internal void WakeCapacitors() { if (IsInsideTree()) SetPhysicsProcess(_capacitors.Count > 0); }
    public override void _PhysicsProcess(double delta)
    {
        CapacitorTime += delta; _serviceElapsed += delta;
        double interval = double.IsFinite(CapacitorServiceSeconds) ? Math.Clamp(CapacitorServiceSeconds, .1, 1) : .25;
        if (_serviceElapsed >= interval) FlushCapacitors();
    }
    // =========================================================
    // Integrate once, using cached supply and memberships. PW4-C replaces proportional sharing with priority.
    internal void FlushCapacitors()
    {
        if (_servicingCapacitors || _serviceElapsed <= 0) return;
        _servicingCapacitors = true;
        double elapsed = _serviceElapsed; _serviceElapsed = 0;
        try
        {
            EnsureDelivery();
            _refillRequests.Clear();
            _capacitorStep.Clear(); _capacitorStep.AddRange(_capacitors);
            foreach (TowerPowerConsumer consumer in _capacitorStep)
            {
                if (!consumer.Live) continue;
                consumer.PrepareRefill();
                if (consumer.Region > 0)
                {
                    _refillRequests.TryGetValue(consumer.Region, out double requested);
                    _refillRequests[consumer.Region] = requested + consumer.DesiredRefill;
                }
            }
            foreach (TowerPowerConsumer consumer in _capacitorStep)
            {
                if (!consumer.Live) continue;
                int region = consumer.Region;
                _refillRequests.TryGetValue(region, out double requested);
                double fraction = requested > 0 ? Math.Min(1, TotalsForRegion(region).Supply * elapsed / requested) : 0;
                consumer.CreditRefill(consumer.DesiredRefill * fraction);
            }
            // Finish all credits before callbacks can change sources/topology or dispose another consumer.
            foreach (TowerPowerConsumer consumer in _capacitorStep) if (consumer.Live) consumer.Publish();
            bool awake = false;
            foreach (TowerPowerConsumer consumer in _capacitors) if (consumer.NeedsService) { awake = true; break; }
            SetPhysicsProcess(awake);
        }
        finally { _capacitorStep.Clear(); _servicingCapacitors = false; }
    }
    // =========================================================
    // No source nodes, consumer callbacks or accumulated time survive a level unload.
    private void ClearCapacitors()
    {
        foreach (TowerPowerConsumer consumer in _capacitors) consumer.DetachGrid();
        _capacitors.Clear();
        if (!_servicingCapacitors) _capacitorStep.Clear();
        _refillRequests.Clear(); _serviceElapsed = 0; SetPhysicsProcess(false);
    }
}
