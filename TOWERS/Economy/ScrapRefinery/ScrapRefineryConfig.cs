// Inspector-authored visual-only Scrap Refinery building. SALV1 production/logistics will add physical stock later.
using Godot;
using System;

[GlobalClass]
public partial class ScrapRefineryConfig : TowerConfig
{
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
        Description = "Industrial scrap-smelting vessel. Future SALV1 recovery drones bring raw scrap here for metal refining.";
        ConstructionCosts = new TowerLevel(1, 350, default).Costs;
        ConstructionSeconds = 5; MaxHealth = 1000;
    }

    public override string DescribeStats(TowerFinalStats final) => "Planned: Scrap intake → Refined Metal processing (SALV1)";
}
