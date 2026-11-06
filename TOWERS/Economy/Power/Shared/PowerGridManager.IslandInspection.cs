// Read-only island UI snapshots from existing membership, accounting and capacitor samples; never settles energy.
using Godot;
using System.Collections.Generic;

public readonly record struct IslandPowerSnapshot(int Region, double Generation, double NetworkSupply, int LinkedIslands,
    double Consumed, double Recharge, double Stored, double Capacity, int Consumers);
public readonly record struct IslandTowerPowerSample(TowerBase Tower, double Consumed, double Recharge, bool Recovering, bool Working);

public partial class PowerGridManager
{
    // =========================================================
    // Relevant registered islands include objectives, consumers and disabled distribution buildings.
    public void CopyPowerIslandRegions(HashSet<int> regions)
    {
        regions.Clear();
        foreach (var entry in _endpoints.Values) if (entry.Region > 0) regions.Add(entry.Region);
        foreach (var consumer in _capacitors) if (consumer.Live && consumer.Region > 0) regions.Add(consumer.Region);
        foreach (var entry in _distribution.Values)
        {
            int region = RegionForFootprint(entry.Anchor, entry.Size);
            if (region > 0 && GodotObject.IsInstanceValid(entry.Tower) && !entry.Tower.IsDestroyed) regions.Add(region);
        }
    }
    // =========================================================
    // Called only for the hovered island at UI sampling cadence, with caller-retained storage.
    public IslandPowerSnapshot InspectIslandPower(int region, List<IslandTowerPowerSample> towers)
    {
        towers.Clear(); EnsureConnections();
        double consumed = 0, recharge = 0, stored = 0, capacity = 0;
        foreach (var consumer in _capacitors)
        {
            if (!consumer.Live || consumer.Region != region) continue;
            var tower = consumer.InspectionOwner;
            double used = consumer.LastConsumedPerSecond, received = consumer.LastDeliveredRefillPerSecond;
            consumed += used; recharge += received;
            stored += consumer.Capacitor.Energy; capacity += consumer.Capacitor.Settings.Capacity;
            towers.Add(new(tower, used, received, consumer.Capacitor.RecoveryRequired, tower.IsBusy));
        }
        int network = EffectiveNetwork(region), linked = 0;
        if (network > 0) for (int i = 1; i < _networkRoots.Length; i++) if (_networkRoots[i] == network) linked++;
        return new(region, TotalsForRegion(region).Supply, EffectiveSupply(region), linked,
            consumed, recharge, stored, capacity, towers.Count);
    }
}
