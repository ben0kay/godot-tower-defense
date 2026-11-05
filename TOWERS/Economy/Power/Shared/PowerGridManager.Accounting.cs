// Event-driven island supply/requested-demand totals; no allocation decisions, power gating or frame callbacks.
using Godot;
using System;
using System.Collections.Generic;

public readonly record struct PowerNetworkTotals(double Supply, double RequestedDemand, int Producers, int Consumers)
{
    public double Margin => Supply - RequestedDemand;
}

public partial class PowerGridManager
{
    private sealed class Endpoint
    {
        public Vector2I Anchor, Size;
        public float Amount;
        public bool Producer;
        public int Region;
    }
    private readonly Dictionary<string, Endpoint> _endpoints = new(StringComparer.Ordinal);
    private PowerNetworkTotals[] _totals = Array.Empty<PowerNetworkTotals>();
    public PowerNetworkTotals Totals { get; private set; }
    public int UnassignedProducers { get; private set; }
    public int UnassignedConsumers { get; private set; }
    public int AccountingRevision { get; private set; }
    public event Action AccountingChanged;

    // =========================================================
    // IDs belong to the caller (e.g. citadel:relative-path or a future tower save GUID); repeated updates replace once.
    public void SetProducer(string id, Vector2I anchor, Vector2I size, float output) => SetEndpoint(id, anchor, size, output, true);
    public void SetRequestedDemand(string id, Vector2I anchor, Vector2I size, float demand) => SetEndpoint(id, anchor, size, demand, false);

    // =========================================================
    // Validate before mutation, then adjust only the old/new island totals; footprint membership is never inferred from art.
    private void SetEndpoint(string id, Vector2I anchor, Vector2I size, float amount, bool producer)
    {
        if (_grid == null || !IsInsideTree()) throw new InvalidOperationException("Power accounting requires a ready world.");
        if (string.IsNullOrWhiteSpace(id) || id != id.Trim() || !float.IsFinite(amount) || amount < 0 || size.X <= 0 || size.Y <= 0)
            throw new ArgumentException("Power registration needs a stable ID, positive footprint and finite nonnegative amount.");
        int region = RegionForFootprint(anchor, size);
        bool anchorChanged;
        if (_endpoints.TryGetValue(id, out Endpoint entry))
        {
            if (entry.Producer != producer) throw new ArgumentException("A power ID cannot change between producer and consumer.");
            if (entry.Anchor == anchor && entry.Size == size && entry.Amount == amount && entry.Region == region) return;
            anchorChanged = producer && ((entry.Amount > 0 && entry.Region > 0) != (amount > 0 && region > 0)
                || (entry.Amount > 0 && entry.Region > 0 && (entry.Anchor != anchor || entry.Size != size || entry.Region != region)));
            if (producer) FlushCapacitors();
            Adjust(entry, -1);
        }
        else
        {
            anchorChanged = producer && amount > 0 && region > 0;
            if (producer) FlushCapacitors(); entry = new Endpoint { Producer = producer }; _endpoints.Add(id, entry);
        }
        entry.Anchor = anchor; entry.Size = size; entry.Amount = amount; entry.Region = region;
        Adjust(entry, 1); if (anchorChanged) InvalidateDelivery(); PublishAccounting();
    }

    // =========================================================
    // Removing a dead/unloaded caller is idempotent, including teardown where the manager's dictionary is already empty.
    public void RemoveEndpoint(string id)
    {
        if (id == null || !_endpoints.TryGetValue(id, out Endpoint entry)) return;
        if (entry.Producer) FlushCapacitors();
        _endpoints.Remove(id);
        Adjust(entry, -1);
        if (entry.Producer && entry.Amount > 0 && entry.Region > 0) InvalidateDelivery();
        PublishAccounting();
    }

    // =========================================================
    // Each read is O(1). Region zero is not a shared pool: disconnected registrations contribute no usable output/demand.
    public PowerNetworkTotals TotalsForRegion(int region) => region > 0 && region < _totals.Length ? _totals[region] : default;

    // =========================================================
    // Sparse membership/state changes update cached aggregates; float inputs accumulate in double precision.
    private void Adjust(Endpoint entry, int sign)
    {
        if (entry.Region == 0)
        {
            if (entry.Producer) UnassignedProducers += sign; else UnassignedConsumers += sign;
            return;
        }
        _totals[entry.Region] = Delta(_totals[entry.Region], entry, sign);
        Totals = Delta(Totals, entry, sign);
    }

    // =========================================================
    // A zero-member side resets exactly to zero after removals, avoiding residual floating point totals.
    private static PowerNetworkTotals Delta(PowerNetworkTotals totals, Endpoint entry, int sign)
    {
        int producers = totals.Producers + (entry.Producer ? sign : 0);
        int consumers = totals.Consumers + (entry.Producer ? 0 : sign);
        double supply = totals.Supply + (entry.Producer ? sign * (double)entry.Amount : 0);
        double demand = totals.RequestedDemand + (entry.Producer ? 0 : sign * (double)entry.Amount);
        return new(producers == 0 ? 0 : Math.Max(0, supply), consumers == 0 ? 0 : Math.Max(0, demand), producers, consumers);
    }

    // =========================================================
    // Terrain changes relabel islands: rebuild only registered endpoints, before publishing the new topology snapshot.
    private void ReindexAccounting()
    {
        int count = IslandCount + 1;
        if (_totals.Length != count) _totals = new PowerNetworkTotals[count]; else Array.Clear(_totals);
        Totals = default; UnassignedProducers = UnassignedConsumers = 0;
        foreach (Endpoint entry in _endpoints.Values)
        { entry.Region = RegionForFootprint(entry.Anchor, entry.Size); Adjust(entry, 1); }
        AccountingRevision++;
    }

    // =========================================================
    // Listeners see a completed change; identical values never emit another accounting event.
    private void PublishAccounting() { AccountingRevision++; AccountingChanged?.Invoke(); WakeCapacitors(); }

    // =========================================================
    // No durable state or Node references live in accounting; future saves restore inputs through their owning features.
    private void ClearAccounting()
    {
        _endpoints.Clear(); _totals = Array.Empty<PowerNetworkTotals>(); Totals = default;
        UnassignedProducers = UnassignedConsumers = 0;
    }
}
