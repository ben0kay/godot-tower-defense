// Editable depot storage/transport policy; current stock is a per-building AmmoStorage instance.
using Godot;

[GlobalClass]
public partial class AmmunitionDepotConfig : TowerConfig
{
    [ExportGroup("Construction")]
    [Export] public int ConstructionCredits { get => ConstructionCosts[0].Amount; set => ConstructionCosts[0].Amount = Mathf.Max(0, value); }
    [ExportGroup("Storage")]
    [Export(PropertyHint.Range, "1,1000000,1")] public int StorageCapacity { get; set; } = 10000;
    [ExportGroup("Transport")]
    [Export(PropertyHint.Range, "0,20,1")] public int DroneSlots { get; set; } = 2;
    [Export(PropertyHint.Range, "1,10000,1")] public int DronePayload { get; set; } = 250;
    [Export(PropertyHint.Range, "1,2000,1")] public float DroneSpeed { get; set; } = 220;
    public AmmunitionDepotConfig()
    { DisplayName = "Ammunition Depot"; Description = "Stores factory deliveries and sends drones to low-ammo towers."; ConstructionCosts = new TowerLevel(1, 150, default).Costs; }
    public override string DescribeStats(TowerFinalStats final) =>
        $"Storage capacity: {StorageCapacity:N0}\nDrones: {DroneSlots} × {DronePayload} ammo at {DroneSpeed:0} px/s";
}
