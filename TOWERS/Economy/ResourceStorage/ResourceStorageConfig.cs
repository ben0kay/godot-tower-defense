// Shared tower lifecycle and SALV1 physical material settings.
using Godot;
using System;

[GlobalClass]
public partial class ResourceStorageConfig : TowerConfig
{
    [ExportGroup("Physical Stock")]
    [Export(PropertyHint.Range, "1,1000,1")] public int MetalCapacity { get; set; } = 100;

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
        Description = "Armoured material vault. Stores Refined Metal separately from ammunition.";
        ConstructionCosts = new TowerLevel(1, 200, default).Costs;
        ConstructionSeconds = 4; MaxHealth = 1000;
    }

    public override string DescribeStats(TowerFinalStats final) => "Physical Refined Metal storage";
}
