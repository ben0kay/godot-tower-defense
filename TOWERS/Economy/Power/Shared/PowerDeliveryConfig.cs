// Shared distance-to-recharge tuning; a connected route limits refill rate, never capacitor capacity or combat stats.
using Godot;
using System;

[GlobalClass]
public partial class PowerDeliveryConfig : Resource
{
    private int _full = 4;
    private double _falloff = 12, _floor = .1;
    [Export(PropertyHint.Range, "0,256,1,or_greater")] public int FullEfficiencyCells
    { get => _full; set { if (value < 0) throw new ArgumentOutOfRangeException(nameof(value)); if (_full == value) return; _full = value; EmitChanged(); } }
    [Export(PropertyHint.Range, "0.1,256,0.1,or_greater")] public double FalloffCells
    { get => _falloff; set { if (!double.IsFinite(value) || value < .1) throw new ArgumentOutOfRangeException(nameof(value)); if (_falloff == value) return; _falloff = value; EmitChanged(); } }
    [Export(PropertyHint.Range, "0,1,0.01")] public double MinimumEfficiency
    { get => _floor; set { if (!double.IsFinite(value) || value < 0 || value > 1) throw new ArgumentOutOfRangeException(nameof(value)); if (_floor == value) return; _floor = value; EmitChanged(); } }
    // =========================================================
    // Snapshot before integration; resource changes settle old elapsed time before the new curve is applied.
    public PowerDeliverySettings Settings() => new(FullEfficiencyCells, FalloffCells, MinimumEfficiency);
}
public readonly record struct PowerDeliverySettings(int FullEfficiencyCells, double FalloffCells, double MinimumEfficiency)
{
    // =========================================================
    // No reachable source means zero, including when a positive floor is configured. No finite-route hard cutoff.
    public double Efficiency(int distance) => distance < 0 ? 0 : Math.Max(MinimumEfficiency,
        1 / (1 + Math.Max(0, (double)distance - FullEfficiencyCells) / FalloffCells));
}
