// Editable single-barrel starter turret stats for automatic targeting and firing.
// The shared DefaultBattleTowerConfig.tres is editable in the Godot Inspector.
using Godot;

[GlobalClass]
public partial class BattleTowerConfig : Resource
{
    [ExportGroup("Identity")]
    [Export] public string DisplayName { get; set; } = "Battle Tower";
    [Export] public string Description { get; set; } = "Versatile baseline turret: one barrel and balanced range/fire rate.";

    [ExportGroup("Construction")]
    [Export] public Godot.Collections.Array<ResourceCost> ConstructionCosts { get; set; } = new();

    [ExportGroup("Combat")]
    [Export(PropertyHint.Range, "32,1024,16")] public float RangePixels { get; set; } = 256f;
    [Export(PropertyHint.Range, "0.1,10,0.1")] public float ShotsPerSecond { get; set; } = 1f;
    [Export(PropertyHint.Range, "1,100,1")] public int DamagePerShot { get; set; } = 12;
    [Export(PropertyHint.Range, "50,3000,25")] public float ProjectileSpeed { get; set; } = 600f;
    [ExportGroup("Aiming")]
    [Export(PropertyHint.Range, "0,720,5")] public float RotationSpeedDegrees { get; set; } = 180f;
    [Export(PropertyHint.Range, "0,180,1")] public float AimToleranceDegrees { get; set; } = 12f;
    [Export] public bool RotateWhileTracking { get; set; } = true;

    [ExportGroup("Targeting")]
    [Export] public TowerTargeting.Priority TargetPriority { get; set; } = TowerTargeting.Priority.First;
    [Export(PropertyHint.Range, "0.05,2,0.05")] public float TargetSearchInterval { get; set; } = 0.15f;
}
