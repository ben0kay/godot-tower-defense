// Editable production/building defaults; no duplicate inventory or delivery authority.
using Godot;

[GlobalClass]
public partial class AmmunitionFactoryConfig : TowerConfig
{
    [ExportGroup("Construction")]
    [Export] public int ConstructionCredits { get => ConstructionCosts[0].Amount; set => ConstructionCosts[0].Amount = Mathf.Max(0, value); }
    [ExportGroup("Production")]
    [Export(PropertyHint.Range, "1,10000,1")] public int BatchAmount { get; set; } = 100;
    [Export(PropertyHint.Range, "0.05,120,0.05")] public float ProductionSecondsPerBatch { get; set; } = 2;
    [Export(PropertyHint.Range, "1,100000,1")] public int InternalBufferCapacity { get; set; } = 1000;
    [ExportGroup("Transport")]
    [Export(PropertyHint.Range, "0,20,1")] public int DroneSlots { get; set; } = 1;
    [Export(PropertyHint.Range, "1,10000,1")] public int DronePayload { get; set; } = 1000;
    [Export(PropertyHint.Range, "1,10000,1")] public int ShipmentAmount { get; set; } = 1000;
    [Export(PropertyHint.Range, "1,2000,1")] public float DroneSpeed { get; set; } = 220;

    // =========================================================
    // Prices use the existing wallet recipe, while passive buildings keep an empty combat-level list.
    public AmmunitionFactoryConfig()
    {
        DisplayName = "Ammunition Factory"; Description = "Produces ammunition; factory drones carry buffered stock to depots.";
        ConstructionCosts = new TowerLevel(1, 200, default).Costs;
    }
    public override string DescribeStats(TowerFinalStats final) =>
        $"Production: {BatchAmount} ammo / {ProductionSecondsPerBatch:0.##}s\nBuffer: {InternalBufferCapacity}\nShipment: {ShipmentAmount:N0} ammo per trip\nDrones: {DroneSlots} × {DronePayload} ammo at {DroneSpeed:0} px/s";
}
