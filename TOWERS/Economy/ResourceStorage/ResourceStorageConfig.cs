// Inspector-authored visual-only Resource Storage building. SALV1 production/logistics will add physical stock later.
using Godot;
using System;

[GlobalClass]
public partial class ResourceStorageConfig : TowerConfig
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
    public ResourceStorageConfig()
    {
        DisplayName = "Resource Storage";
        Description = "Armoured material vault. Future SALV1 deliveries store Refined Metal separately from ammunition.";
        ConstructionCosts = new TowerLevel(1, 200, default).Costs;
        ConstructionSeconds = 4; MaxHealth = 1000;
    }

    public override string DescribeStats(TowerFinalStats final) => "Planned: physical Refined Metal storage (SALV1)";
}
