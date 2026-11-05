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
    [ExportGroup("Selected Range Colors")]
    [Export] public Color VisibleRangeColor { get; set; } = new(0.37f, 0.82f, 0.95f, 0.55f);
    [Export] public Color BlockedRangeColor { get; set; } = new(1f, 0.25f, 0.22f, 0.7f);
    [Export(PropertyHint.Range, "0,0.3,0.01")] public float VisibleRangeFillOpacity { get; set; } = 0.035f;
    [Export(PropertyHint.Range, "0,0.4,0.01")] public float BlockedRangeFillOpacity { get; set; } = 0.14f;
}

