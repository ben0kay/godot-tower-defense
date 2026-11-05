// Shared passive distribution construction inputs; these buildings never generate or consume electricity.
using Godot;
using System;

[GlobalClass]
public partial class PowerDistributionConfig : TowerConfig
{
    [ExportGroup("Provisional Construction")]
    [Export(PropertyHint.Range, "0,10000,1,or_greater")]
    public int ConstructionCredits
    {
        get => ConstructionCosts[0].Amount;
        set { int next = Math.Max(0, value); if (ConstructionCosts[0].Amount == next) return; ConstructionCosts[0].Amount = next; EmitChanged(); }
    }
    public PowerDistributionConfig() => ConstructionCosts = new TowerLevel(1, 150, default).Costs;
}
