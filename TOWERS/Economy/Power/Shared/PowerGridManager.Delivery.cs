// PW4-B distance delivery owned by the existing grid; lazy/coalesced cache rebuild, never a per-tower path search.
using Godot;
using System;
using System.Collections.Generic;

public readonly record struct PowerDelivery(int DistanceCells, double Efficiency);

public partial class PowerGridManager
{
    private PowerDeliveryConfig _deliveryConfig, _boundDelivery;
    private PowerDeliverySettings _deliverySettings;
    private readonly PowerDistanceMap _distances = new();
    private readonly List<PowerDistanceMap.Anchor> _anchors = new();
    private bool _distancesDirty = true;
    private int _deliveryRevision;
    public int DistanceRebuildCount { get; private set; }
    public double LastDistanceRebuildMilliseconds { get; private set; }
    public int PoweredCellCount { get { EnsureDelivery(); return _distances.ReachedCells; } }
    [ExportGroup("Capacitor Delivery")]
    [Export] public PowerDeliveryConfig DeliveryConfig
    {
        get => _deliveryConfig;
        set
        {
            if (_deliveryConfig == value) return;
            if (_grid != null) FlushCapacitors();
            _deliveryConfig = value;
            if (_grid != null) { BindDelivery(); _deliveryRevision++; WakeCapacitors(); }
        }
    }
    public int DeliveryRevision { get { EnsureDelivery(); return _deliveryRevision; } }
    // =========================================================
    // The shared scene binds one authored resource; synthetic/custom worlds keep the same configurable fallback.
    private void BindDelivery()
    {
        if (GodotObject.IsInstanceValid(_boundDelivery)) _boundDelivery.Changed -= DeliveryTuningChanged;
        _boundDelivery = DeliveryConfig ?? new PowerDeliveryConfig();
        _deliverySettings = _boundDelivery.Settings(); _boundDelivery.Changed += DeliveryTuningChanged;
    }
    private void DeliveryTuningChanged()
    {
        FlushCapacitors(); _deliverySettings = _boundDelivery.Settings();
        _deliveryRevision++; WakeCapacitors(); // Curve edits do not require another BFS.
    }
    // =========================================================
    // Source batches/loading coalesce: the next service/query builds once from the final active anchor set.
    private void InvalidateDelivery() { _distancesDirty = true; WakeCapacitors(); }
    public void EnsureDelivery()
    {
        EnsureConnections();
        if (!_distancesDirty || _grid == null) return;
        ulong start = Time.GetTicksUsec(); _anchors.Clear();
        foreach (Endpoint endpoint in _endpoints.Values)
            if (endpoint.Producer && endpoint.Amount > 0 && endpoint.Region > 0)
                _anchors.Add(new(endpoint.Anchor.X, endpoint.Anchor.Y, endpoint.Size.X, endpoint.Size.Y));
        // Read island labels, not live terrain: an old elapsed interval may be settled during TerrainRebuilt.
        _distances.Rebuild(_islands.Width, _islands.Height, _islands.Regions, _anchors, _transmitterLinks);
        RebuildRelayBoost();
        _anchors.Clear(); _distancesDirty = false; _deliveryRevision++; DistanceRebuildCount++;
        LastDistanceRebuildMilliseconds = (Time.GetTicksUsec() - start) / 1000.0;
    }
    // =========================================================
    // Source and consumer footprints both obey whole-island membership. Nearest footprint cell receives delivery.
    public PowerDelivery DeliveryForFootprint(Vector2I anchor, Vector2I size)
    {
        EnsureDelivery();
        if (_grid == null || RegionForFootprint(anchor, size) == 0) return new(-1, 0);
        int steps = DistanceFor(anchor, size);
        double efficiency = _deliverySettings.Efficiency(steps);
        if (steps >= 0)
            for (int y = anchor.Y; y < anchor.Y + size.Y; y++) for (int x = anchor.X; x < anchor.X + size.X; x++)
                efficiency = Math.Max(efficiency, _relayBoost[y * _islands.Width + x]);
        return new(steps, Math.Min(1, efficiency));
    }
    private int DistanceFor(Vector2I anchor, Vector2I size)
    {
        int best = int.MaxValue;
        for (int y = anchor.Y; y < anchor.Y + size.Y; y++) for (int x = anchor.X; x < anchor.X + size.X; x++)
        { int distance = _distances.DistanceAt(x, y); if (distance >= 0) best = Math.Min(best, distance); }
        return best == int.MaxValue ? -1 : best;
    }
    // =========================================================
    // Checkpoints restore all source inputs first, then warm this derived cache once before unpausing.
    public void RefreshConsumerDelivery()
    {
        if (_capacitors.Count == 0) return;
        EnsureDelivery(); foreach (TowerPowerConsumer consumer in _capacitors) consumer.RefreshDelivery();
    }
    private void ClearDelivery()
    {
        if (GodotObject.IsInstanceValid(_boundDelivery)) _boundDelivery.Changed -= DeliveryTuningChanged;
        _boundDelivery = null; _anchors.Clear();
    }
}
