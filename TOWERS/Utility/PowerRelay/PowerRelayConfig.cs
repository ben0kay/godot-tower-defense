// Typed, provisional local conductive coverage and best-only recharge ceiling; no peer/link fields.
using Godot;
using System;

[GlobalClass]
public partial class PowerRelayConfig : PowerDistributionConfig
{
    private int _range = 10;
    private double _efficiency = .85;
    [ExportGroup("Local Conductive Boost (provisional)")]
    [Export(PropertyHint.Range, "1,64,1")] public int RangeCells
    { get => _range; set { if (value < 1 || value > 64) throw new ArgumentOutOfRangeException(nameof(value)); if (_range == value) return; _range = value; EmitChanged(); } }
    [Export(PropertyHint.Range, "0,1,0.01")] public double BoostEfficiency
    { get => _efficiency; set { if (!double.IsFinite(value) || value < 0 || value > 1) throw new ArgumentOutOfRangeException(nameof(value)); if (_efficiency == value) return; _efficiency = value; EmitChanged(); } }
    public PowerRelayConfig() { DisplayName = "Power Relay"; Description = "Boosts local capacitor recharge on powered conducting terrain. Does not connect islands or generate power."; ConstructionSeconds = 3; MaxHealth = 800; }
    public override string DescribeStats(TowerFinalStats final) => $"Coverage: {RangeCells} conductive cells\nRecharge ceiling: up to {BoostEfficiency:P0} of nominal\nZero generation · overlaps do not stack";
}
