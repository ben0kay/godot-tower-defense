// Balanced single-barrel starter turret's proposed stats; combat isn't active yet.
// The shared DefaultBattleTowerConfig.tres is editable in the Godot Inspector.
using Godot;

[GlobalClass]
public partial class BattleTowerConfig : Resource
{
    [ExportGroup("Identity")]
    [Export] public string DisplayName { get; set; } = "Battle Tower";
    [Export] public string Description { get; set; } = "Versatile baseline turret: one barrel and balanced range/fire rate.";

    [ExportGroup("Proposed Balance (No Firing Yet)")]
    [Export(PropertyHint.Range, "0,2000,10")] public int BuildCost { get; set; } = 100;
    [Export(PropertyHint.Range, "32,1024,16")] public float RangePixels { get; set; } = 256f;
    [Export(PropertyHint.Range, "0.1,10,0.1")] public float ShotsPerSecond { get; set; } = 1f;
    [Export(PropertyHint.Range, "1,100,1")] public int DamagePerShot { get; set; } = 12;
}
