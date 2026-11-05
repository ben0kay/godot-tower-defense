// Cached physical-island unions, reciprocal transmitter edges and local-only relay boost fields.
using Godot;
using System;
using System.Collections.Generic;

public partial class PowerGridManager
{
    private sealed record Distribution(PowerDistributionBuilding Tower, Vector2I Anchor, Vector2I Size,
        bool Running, int Range, double Boost, int Hop, string Peer);
    private readonly Dictionary<string, Distribution> _distribution = new(StringComparer.Ordinal);
    private readonly List<PowerDistanceMap.Link> _transmitterLinks = new();
    private int[] _networkRoots = Array.Empty<int>(), _relayVisited = Array.Empty<int>(), _relayQueue = Array.Empty<int>(), _relaySteps = Array.Empty<int>();
    private double[] _networkSupply = Array.Empty<double>(), _relayBoost = Array.Empty<double>();
    private int _relayStamp;
    private bool _connectionsDirty = true;
    public event Action DistributionChanged;

    // =========================================================
    // Snapshot inputs before publishing; old elapsed intervals always settle against the OLD network/eligibility.
    public void SetDistribution(PowerDistributionBuilding tower)
    {
        if (_grid == null) return;
        Vector2 first = _grid.ToLocal(tower.GlobalPosition) - (Vector2)(tower.Footprint - Vector2I.One) * (_grid.Config.CellSize * .5f);
        var relay = tower.AuthoredConfig as PowerRelayConfig;
        var transmitter = tower as PowerTransmitter;
        var next = new Distribution(tower, _grid.WorldToCell(_grid.ToGlobal(first)), tower.Footprint,
            tower.IsInsideTree() && !tower.IsQueuedForDeletion() && tower.Health > 0 && tower.CanOperate && !tower.IsBusy,
            relay?.RangeCells ?? 0, relay?.BoostEfficiency ?? 0,
            (tower.AuthoredConfig as PowerTransmitterConfig)?.HopCostCells ?? 0, transmitter?.PeerSaveId ?? "");
        if (_distribution.TryGetValue(tower.SaveId, out var old) && old == next) return;
        FlushCapacitors(); _distribution[tower.SaveId] = next;
        DistributionInputsChanged();
    }
    public void RemoveDistribution(PowerDistributionBuilding tower)
    {
        if (!_distribution.ContainsKey(tower.SaveId)) return;
        FlushCapacitors();
        if (tower is PowerTransmitter transmitter) ClearPeer(transmitter);
        _distribution.Remove(tower.SaveId); DistributionInputsChanged();
    }
    private void DistributionInputsChanged()
    { _connectionsDirty = true; InvalidateDelivery(); DistributionChanged?.Invoke(); }

    // =========================================================
    // Explicit player operation; no auto-pair, generation prerequisite, same-island bridge or native instance IDs.
    public bool TryLinkTransmitters(PowerTransmitter a, PowerTransmitter b, out string failure)
    {
        failure = "";
        if (GetTree().Paused) failure = "Resume before linking.";
        else if (!GodotObject.IsInstanceValid(a) || !GodotObject.IsInstanceValid(b)) failure = "Choose a Power Transmitter.";
        else if (a == b) failure = "Choose a different transmitter.";
        else if (!_distribution.TryGetValue(a.SaveId, out var ea) || !_distribution.TryGetValue(b.SaveId, out var eb)
            || ea.Tower != a || eb.Tower != b || !ea.Running || !eb.Running) failure = "Both transmitters must be complete and operational.";
        else if (a.PeerSaveId.Length > 0 || b.PeerSaveId.Length > 0) failure = "A transmitter already has a peer. Unlink it first.";
        else
        {
            int ar = RegionForFootprint(ea.Anchor, ea.Size), br = RegionForFootprint(eb.Anchor, eb.Size);
            if (ar == 0 || br == 0) failure = "Both footprints need conducting terrain.";
            else if (ar == br) failure = "Choose a transmitter on a different island.";
        }
        if (failure.Length > 0) return false;
        FlushCapacitors(); a.PeerSaveId = b.SaveId; b.PeerSaveId = a.SaveId;
        _distribution[a.SaveId] = _distribution[a.SaveId] with { Peer = b.SaveId };
        _distribution[b.SaveId] = _distribution[b.SaveId] with { Peer = a.SaveId };
        DistributionInputsChanged(); return true;
    }
    public void UnlinkTransmitter(PowerTransmitter tower)
    {
        if (!_distribution.ContainsKey(tower.SaveId)) return;
        FlushCapacitors(); ClearPeer(tower); DistributionInputsChanged();
    }
    private void ClearPeer(PowerTransmitter tower)
    {
        if (_distribution.TryGetValue(tower.PeerSaveId, out var peer) && peer.Tower is PowerTransmitter other && other.PeerSaveId == tower.SaveId)
        { other.PeerSaveId = ""; _distribution[other.SaveId] = peer with { Peer = "" }; }
        tower.PeerSaveId = "";
        if (_distribution.TryGetValue(tower.SaveId, out var entry)) _distribution[tower.SaveId] = entry with { Peer = "" };
    }
    public PowerTransmitter TransmitterPeer(PowerTransmitter tower) => _distribution.TryGetValue(tower.PeerSaveId, out var entry)
        && entry.Peer == tower.SaveId && GodotObject.IsInstanceValid(entry.Tower) && !entry.Tower.IsQueuedForDeletion()
        ? entry.Tower as PowerTransmitter : null;

    // =========================================================
    // Optional peer data defaults empty. Broken/nonreciprocal/missing peers fail safely, never form one-way edges.
    public void RestoreTransmitterLinks(IReadOnlyDictionary<string, TowerBase> towers, IReadOnlyList<TowerSaveData> saved)
    {
        foreach (var row in saved)
            if (towers[row.Id] is PowerTransmitter transmitter)
                transmitter.PeerSaveId = row.PowerPeerId ?? "";
        foreach (var row in saved)
        {
            if (towers[row.Id] is not PowerTransmitter transmitter) continue;
            if (!towers.TryGetValue(transmitter.PeerSaveId, out var candidate) || candidate is not PowerTransmitter peer
                || peer == transmitter || peer.PeerSaveId != transmitter.SaveId) transmitter.PeerSaveId = "";
        }
        foreach (var row in saved) if (towers[row.Id] is PowerTransmitter transmitter) SetDistribution(transmitter);
        EnsureDelivery(); DistributionChanged?.Invoke();
    }

    // =========================================================
    // Rebuild a small union graph only when source totals or distribution inputs change. Sum each physical island once.
    private void EnsureConnections()
    {
        if (!_connectionsDirty || _grid == null) return;
        int count = IslandCount + 1;
        if (_networkRoots.Length != count) { _networkRoots = new int[count]; _networkSupply = new double[count]; }
        Array.Clear(_networkSupply); for (int i = 0; i < count; i++) _networkRoots[i] = i;
        _transmitterLinks.Clear();
        foreach (var pair in _distribution)
        {
            var a = pair.Value;
            if (a.Tower is not PowerTransmitter || !a.Running || string.CompareOrdinal(pair.Key, a.Peer) >= 0
                || !_distribution.TryGetValue(a.Peer, out var b) || b.Tower is not PowerTransmitter || !b.Running || b.Peer != pair.Key) continue;
            int ar = RegionForFootprint(a.Anchor, a.Size), br = RegionForFootprint(b.Anchor, b.Size);
            if (ar == 0 || br == 0 || ar == br) continue;
            int rootA = NetworkRoot(ar), rootB = NetworkRoot(br);
            if (rootA != rootB) _networkRoots[Math.Max(rootA, rootB)] = Math.Min(rootA, rootB);
            _transmitterLinks.Add(new(new(a.Anchor.X, a.Anchor.Y, a.Size.X, a.Size.Y),
                new(b.Anchor.X, b.Anchor.Y, b.Size.X, b.Size.Y), Math.Max(1, Math.Max(a.Hop, b.Hop))));
        }
        for (int i = 1; i < count; i++) _networkRoots[i] = NetworkRoot(i);
        for (int i = 1; i < count; i++) _networkSupply[_networkRoots[i]] += TotalsForRegion(i).Supply;
        _connectionsDirty = false;
    }
    private int NetworkRoot(int region)
    {
        int root = region; while (_networkRoots[root] != root) root = _networkRoots[root];
        while (_networkRoots[region] != region) { int next = _networkRoots[region]; _networkRoots[region] = root; region = next; }
        return root;
    }
    internal int EffectiveNetwork(int region) { EnsureConnections(); return region > 0 && region < _networkRoots.Length ? _networkRoots[region] : 0; }
    internal double EffectiveSupply(int region) { int network = EffectiveNetwork(region); return network > 0 ? _networkSupply[network] : 0; }

    // =========================================================
    // Shared best-only field; bounded four-neighbour coverage never crosses transmitter links or re-seeds other Relays.
    private void RebuildRelayBoost()
    {
        int width = _islands.Width, count = width * _islands.Height;
        if (_relayBoost.Length != count) { _relayBoost = new double[count]; _relayVisited = new int[count]; _relayQueue = new int[count]; _relaySteps = new int[count]; }
        else Array.Clear(_relayBoost);
        foreach (var entry in _distribution.Values)
        {
            int region = RegionForFootprint(entry.Anchor, entry.Size);
            if (entry.Range <= 0 || !entry.Running || region == 0 || DistanceFor(entry.Anchor, entry.Size) < 0) continue;
            if (++_relayStamp == int.MaxValue) { Array.Clear(_relayVisited); _relayStamp = 1; }
            int tail = 0, head = 0;
            for (int y = entry.Anchor.Y; y < entry.Anchor.Y + entry.Size.Y; y++) for (int x = entry.Anchor.X; x < entry.Anchor.X + entry.Size.X; x++)
            { int cell = y * width + x; _relayVisited[cell] = _relayStamp; _relaySteps[cell] = 0; _relayQueue[tail++] = cell; }
            while (head < tail)
            {
                int cell = _relayQueue[head++], steps = _relaySteps[cell], x = cell % width;
                _relayBoost[cell] = Math.Max(_relayBoost[cell], entry.Boost);
                if (steps >= entry.Range) continue;
                if (x > 0) VisitRelay(cell - 1, steps + 1, region, ref tail);
                if (x + 1 < width) VisitRelay(cell + 1, steps + 1, region, ref tail);
                if (cell >= width) VisitRelay(cell - width, steps + 1, region, ref tail);
                if (cell + width < count) VisitRelay(cell + width, steps + 1, region, ref tail);
            }
        }
    }
    private void VisitRelay(int cell, int steps, int region, ref int tail)
    {
        if (_relayVisited[cell] == _relayStamp || _islands.Regions[cell] != region) return;
        _relayVisited[cell] = _relayStamp; _relaySteps[cell] = steps; _relayQueue[tail++] = cell;
    }
    public string DescribeDistribution(PowerDistributionBuilding tower, out bool powered)
    {
        powered = false; EnsureDelivery();
        if (!_distribution.TryGetValue(tower.SaveId, out var entry)) return "DISCONNECTED";
        int region = RegionForFootprint(entry.Anchor, entry.Size);
        if (tower.IsBusy) return "CONSTRUCTION PENDING";
        if (!entry.Running) return "DISABLED";
        if (region == 0) return "DISCONNECTED · no conducting footprint";
        if (tower is PowerRelay)
        { powered = DistanceFor(entry.Anchor, entry.Size) >= 0; return powered ? "BOOSTING · local conductive recharge" : "NO LIVE SOURCE"; }
        var transmitter = (PowerTransmitter)tower;
        PowerTransmitter peer = TransmitterPeer(transmitter);
        if (peer == null) return "UNLINKED · choose Link, then another island";
        var other = _distribution[peer.SaveId];
        int peerRegion = RegionForFootprint(other.Anchor, other.Size);
        string id = peer.SaveId[..Math.Min(6, peer.SaveId.Length)];
        if (!other.Running || peerRegion == 0 || peerRegion == region) return $"LINK SUSPENDED · peer {id}";
        powered = EffectiveSupply(region) > 0;
        return powered ? $"LINKED · peer {id} · shared supply" : $"LINKED · peer {id} · NO LIVE SOURCE";
    }
    private void ClearDistribution()
    { _distribution.Clear(); _transmitterLinks.Clear(); _connectionsDirty = true; }
}
