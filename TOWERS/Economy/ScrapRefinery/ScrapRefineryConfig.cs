// Shared tower lifecycle and SALV1 physical material settings.
using Godot;
using System;

[GlobalClass]
public partial class ScrapRefineryConfig : TowerConfig
{
    [ExportGroup("Physical Stock")]
    [Export(PropertyHint.Range, "1,1000,1")] public int ScrapCapacity { get; set; } = 20;
    [Export(PropertyHint.Range, "1,1000,1")] public int OutputCapacity { get; set; } = 50;
    [Export(PropertyHint.Range, "1,100,1")] public int MetalPerBatch { get; set; } = 5;
    [Export(PropertyHint.Range, ".1,300,.1")] public double BatchSeconds { get; set; } = 8;

    [ExportGroup("Construction")]
    [Export(PropertyHint.Range, "0,1000000,1,or_greater")]
    public int ConstructionCredits
    {
        get => ConstructionCosts[0].Amount;
        set
        {
            int next = Math.Max(0, value);
            if (ConstructionCosts[0].Amount == next) return;
            ConstructionCosts[0].Amount = next; EmitChanged();
        }
    }

    // =========================================================
    // Use the shared Credits recipe, construction clock and passive hull contract.
    public ScrapRefineryConfig()
    {
        DisplayName = "Scrap Refinery";
        Description = "Industrial scrap-smelting vessel. Refines recovered Scrap into physical Metal.";
        ConstructionCosts = new TowerLevel(1, 350, default).Costs;
        ConstructionSeconds = 5; MaxHealth = 1000;
    }

    public override string DescribeStats(TowerFinalStats final) => "Scrap intake → Refined Metal processing";
}
