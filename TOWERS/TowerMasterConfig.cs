// Shared, Inspector-editable switches for future tower gameplay.
// Only configuration scaffolding: no tower logic reads these values yet.
using Godot;

[GlobalClass]
public partial class TowerMasterConfig : Resource
{
    [ExportGroup("Global Tower Rules")]
    [Export] public bool AllowUpgrades { get; set; } = true;
    [Export] public bool AllowSelling { get; set; } = true;
    [Export(PropertyHint.Range, "0,1,0.05")] public float DefaultSellRefundFraction { get; set; } = 0.75f;

    [ExportGroup("Placement Preview")]
    [Export] public bool ShowPlacementPreview { get; set; } = true;
    [Export] public bool ShowSelectionRange { get; set; } = true;
}
