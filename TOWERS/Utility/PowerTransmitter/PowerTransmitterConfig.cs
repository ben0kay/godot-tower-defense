// Positive link-hop distance is added to conducting source routes; no throughput cap, fuel or local boost.
using Godot;
using System;

[GlobalClass]
public partial class PowerTransmitterConfig : PowerDistributionConfig
{
    private int _hop = 8;
    [ExportGroup("Link Distance Cost (provisional)")]
    [Export(PropertyHint.Range, "1,256,1")] public int HopCostCells
    { get => _hop; set { if (value < 1 || value > 256) throw new ArgumentOutOfRangeException(nameof(value)); if (_hop == value) return; _hop = value; EmitChanged(); } }
    public PowerTransmitterConfig() { DisplayName = "Power Transmitter"; Description = "Manually pair with one transmitter on another island to share finite electricity. No local recharge boost."; ConstructionCredits = 200; ConstructionSeconds = 4; MaxHealth = 900; }
    public override string DescribeStats(TowerFinalStats final) => $"One reciprocal cross-island peer\nHop cost: max of both ends ({HopCostCells} cells here)\nZero generation · finite shared supply";
}
