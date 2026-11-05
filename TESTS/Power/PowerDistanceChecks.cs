// PW4-B tiny pure distance/curve checks; called by the existing optional PowerNetworkChecks fixture.
using System;

internal static class PowerDistanceChecks
{
    // =========================================================
    // A ring route defeats Euclidean shortcuts; source footprints, overlapping anchors and removal remain exact.
    internal static void Run()
    {
        PowerIslandMap islands = new();
        bool[] ring = { true, true, true, true, true, true, false, false, false, true, true, true, true, true, true };
        islands.Rebuild(5, 3, ring); PowerDistanceMap field = new();
        var left = new PowerDistanceMap.Anchor(0, 1, 1, 1);
        field.Rebuild(5, 3, islands.Regions, new[] { left });
        Require(field.DistanceAt(4, 1) == 6 && field.DistanceAt(2, 1) == -1 && field.ReachedCells == 12,
            "Distance follows four-neighbour conductive detour, never space/diagonals");
        var top = new PowerDistanceMap.Anchor(3, 0, 2, 1);
        field.Rebuild(5, 3, islands.Regions, new[] { left, top, top });
        Require(field.DistanceAt(3, 0) == 0 && field.DistanceAt(4, 0) == 0 && field.DistanceAt(4, 1) == 1
            && field.ReachedCells == 12, "All source footprint cells seed zero; overlapping anchors never duplicate visits");
        field.Rebuild(5, 3, islands.Regions, new[] { new PowerDistanceMap.Anchor(0, 0, 3, 2) });
        Require(field.ReachedCells == 0, "Partially invalid source footprint cannot bridge a hole");
        bool[] separated = { true, false, false, true };
        islands.Rebuild(2, 2, separated);
        field.Rebuild(2, 2, islands.Regions, new[] { new PowerDistanceMap.Anchor(0, 0, 1, 1) });
        Require(field.DistanceAt(1, 1) == -1 && field.DistanceAt(-1, 0) == -1, "Diagonal island/outside has no route");
        field.Rebuild(2, 2, islands.Regions, Array.Empty<PowerDistanceMap.Anchor>());
        Require(field.ReachedCells == 0 && field.DistanceAt(0, 0) == -1, "Last source removal clears stale distance");
        bool[] chain = { true, true, false, true, true, false, true, true };
        islands.Rebuild(8, 1, chain);
        var a = new PowerDistanceMap.Anchor(1, 0, 1, 1);
        var b = new PowerDistanceMap.Anchor(3, 0, 1, 1);
        var c = new PowerDistanceMap.Anchor(4, 0, 1, 1);
        var d = new PowerDistanceMap.Anchor(6, 0, 1, 1);
        var links = new[] { new PowerDistanceMap.Link(a, b, 8), new PowerDistanceMap.Link(c, d, 8), new PowerDistanceMap.Link(a, d, 30) };
        field.Rebuild(8, 1, islands.Regions, new[] { new PowerDistanceMap.Anchor(0, 0, 1, 1) }, links);
        Require(field.DistanceAt(7, 0) == 19 && field.DistanceAt(2, 0) == -1 && field.ReachedCells == 6,
            "Transmitters use positive-hop shortest routes and cannot energize vacuum or discount cycles");
        field.Rebuild(8, 1, islands.Regions, Array.Empty<PowerDistanceMap.Anchor>(), links);
        Require(field.ReachedCells == 0, "A linked network with no source creates no energy route");
        field.Rebuild(8, 1, islands.Regions, new[] { new PowerDistanceMap.Anchor(0, 0, 1, 1) });
        Require(field.DistanceAt(3, 0) == -1, "Unlink immediately restores physical-island isolation");
        using PowerDeliveryConfig config = new(); PowerDeliverySettings curve = config.Settings();
        Require(curve.Efficiency(4) == 1 && curve.Efficiency(16) == .5 && curve.Efficiency(-1) == 0
            && curve.Efficiency(1000000) == .1, "Full span, soft half-distance and valid-route-only floor");
        bool rejected = false;
        try { config.FalloffCells = double.NaN; } catch (ArgumentOutOfRangeException) { rejected = true; }
        Require(rejected && config.FalloffCells == 12, "Nonfinite tuning rejects before changing authored values");
    }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
