// Selected-only diagnostic extraction from existing caches; never changes authoritative distribution/energy.
using Godot;
using System;
using System.Collections.Generic;

public enum PowerLinkVisualState { Powered, Unpowered, Suspended }

public partial class PowerGridManager
{
    public int ActiveTransmitterPairs { get { EnsureConnections(); return _transmitterLinks.Count; } }
    public string NetworkSummary(int region) => $"network {EffectiveNetwork(region)} · supply {EffectiveSupply(region):0.##}/s · active pairs {ActiveTransmitterPairs}";
    public string DistributionDetails(PowerDistributionBuilding tower)
    {
        EnsureDelivery();
        if (!_distribution.TryGetValue(tower.SaveId, out var entry)) return "No live network";
        int region = RegionForFootprint(entry.Anchor, entry.Size);
        string network = $"Island {region} · net {EffectiveNetwork(region)} · {EffectiveSupply(region):0.##}/s";
        if (tower is PowerRelay) return network + $"\nBoost: {entry.Range} cells · up to {entry.Boost:P0}";
        var peer = TransmitterPeer((PowerTransmitter)tower);
        if (peer == null) return network + "\nPeer: none";
        var other = _distribution[peer.SaveId];
        return network + $"\nPeer {peer.SaveId[..Math.Min(8, peer.SaveId.Length)]} · island {RegionForFootprint(other.Anchor, other.Size)}";
    }
    public PowerLinkVisualState LinkVisualState(PowerTransmitter tower)
    {
        EnsureConnections();
        var peer = TransmitterPeer(tower);
        if (peer == null || !_distribution.TryGetValue(tower.SaveId, out var a) || !_distribution.TryGetValue(peer.SaveId, out var b)
            || !a.Running || !b.Running) return PowerLinkVisualState.Suspended;
        int ar = RegionForFootprint(a.Anchor, a.Size), br = RegionForFootprint(b.Anchor, b.Size);
        if (ar == 0 || br == 0 || ar == br) return PowerLinkVisualState.Suspended;
        return EffectiveSupply(ar) > 0 ? PowerLinkVisualState.Powered : PowerLinkVisualState.Unpowered;
    }
    // =========================================================
    // Reuse the bounded Relay scratch buffers only when selected/invalidated; coverage cannot cross a link or gap.
    public bool CopyRelayPresentation(PowerRelay relay, List<Vector2I> cells, List<Vector2I> route, out bool truncated)
    {
        cells.Clear(); route.Clear(); truncated = false; EnsureDelivery();
        if (!_distribution.TryGetValue(relay.SaveId, out var entry) || !entry.Running || entry.Range <= 0 || entry.Boost <= 0) return false;
        int region = RegionForFootprint(entry.Anchor, entry.Size);
        if (region == 0 || DistanceFor(entry.Anchor, entry.Size) < 0) return false;
        int width = _islands.Width, count = width * _islands.Height;
        if (++_relayStamp == int.MaxValue) { Array.Clear(_relayVisited); _relayStamp = 1; }
        int head = 0, tail = 0;
        for (int y = entry.Anchor.Y; y < entry.Anchor.Y + entry.Size.Y; y++) for (int x = entry.Anchor.X; x < entry.Anchor.X + entry.Size.X; x++)
        { int cell = y * width + x; _relayVisited[cell] = _relayStamp; _relaySteps[cell] = 0; _relayQueue[tail++] = cell; }
        while (head < tail)
        {
            int cell = _relayQueue[head++], steps = _relaySteps[cell], x = cell % width;
            cells.Add(new(x, cell / width));
            if (steps >= entry.Range) continue;
            if (x > 0) VisitRelay(cell - 1, steps + 1, region, ref tail);
            if (x + 1 < width) VisitRelay(cell + 1, steps + 1, region, ref tail);
            if (cell >= width) VisitRelay(cell - width, steps + 1, region, ref tail);
            if (cell + width < count) VisitRelay(cell + width, steps + 1, region, ref tail);
        }
        Vector2I current = entry.Anchor;
        int best = int.MaxValue;
        for (int y = entry.Anchor.Y; y < entry.Anchor.Y + entry.Size.Y; y++) for (int x = entry.Anchor.X; x < entry.Anchor.X + entry.Size.X; x++)
        { int d = _distances.DistanceAt(x, y); if (d >= 0 && d < best) { best = d; current = new(x, y); } }
        // Descending exact cached costs reconstruct a shortest route without another map flood fill.
        for (int step = 0; step <= 4096; step++)
        {
            route.Add(current); int distance = _distances.DistanceAt(current.X, current.Y);
            if (distance == 0) return true;
            if (step == 4096) { truncated = true; return true; }
            if (!PreviousRouteCell(current, distance, out current)) { route.Clear(); return true; }
        }
        return true;
    }
    private bool PreviousRouteCell(Vector2I cell, int distance, out Vector2I previous)
    {
        int region = RegionAt(cell);
        // Deterministic tie order; ordinary conductive segments and positive-hop links both follow the source field.
        for (int i = 0; i < 4; i++)
        {
            Vector2I next = cell + (i == 0 ? Vector2I.Left : i == 1 ? Vector2I.Up : i == 2 ? Vector2I.Right : Vector2I.Down);
            if (RegionAt(next) == region && _distances.DistanceAt(next.X, next.Y) == distance - 1) { previous = next; return true; }
        }
        foreach (var link in _transmitterLinks)
        {
            Vector2I a = new(link.A.X, link.A.Y), b = new(link.B.X, link.B.Y);
            Vector2I next = cell == a ? b : cell == b ? a : new(-1, -1);
            if (next.X >= 0 && distance >= link.Cost && _distances.DistanceAt(next.X, next.Y) == distance - link.Cost) { previous = next; return true; }
        }
        previous = default; return false;
    }
}
