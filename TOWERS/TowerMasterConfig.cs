// Shared Inspector-editable tower rules, XP balance and placement presentation.
using Godot;

[GlobalClass]
public partial class TowerMasterConfig : Resource
{
    [ExportGroup("Global Tower Rules")]
    [Export] public bool AllowUpgrades { get; set; } = true;
    [Export] public bool AllowSelling { get; set; } = true;
    [Export(PropertyHint.Range, "0,1,0.05")] public float DefaultSellRefundFraction { get; set; } = 0.75f;

    [ExportGroup("Tower Experience")]
    [Export(PropertyHint.Range, "0,100,0.1,or_greater")] public float KillXpMultiplier { get; set; } = 10f;
    [Export(PropertyHint.Range, "0,100,0.1,or_greater")] public float DamageXpMultiplier { get; set; } = 2f;

    [ExportGroup("Placement Preview")]
    [Export] public bool ShowPlacementPreview { get; set; } = true;
    [Export] public bool ShowSelectionRange { get; set; } = true;
}
