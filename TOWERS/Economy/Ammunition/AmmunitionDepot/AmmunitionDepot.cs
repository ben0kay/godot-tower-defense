// Placeable depot owns the existing AmmoStorage helper; neither placement nor this node manufactures stock.
using Godot;
using System;

public partial class AmmunitionDepot : AmmoBuilding
{
    [Export] public AmmunitionDepotConfig Config { get; set; }
    public AmmoStorage Storage { get; private set; }
    public override TowerConfig AuthoredConfig => Config;
    public override AmmoInventory Inventory => Storage?.Inventory;
    public override int DroneSlots => Math.Max(0, Config.DroneSlots);
    public override int DronePayload => Math.Max(1, Config.DronePayload);
    public override float DroneSpeed => float.IsFinite(Config.DroneSpeed) ? Mathf.Max(1, Config.DroneSpeed) : 220;
    public override bool AcceptsConfig(TowerConfig config) => config is AmmunitionDepotConfig c && c.StorageCapacity > 0;
    protected override void ApplyConfig(TowerConfig config) => Config = (AmmunitionDepotConfig)config;
    public override bool ValidateScene() => Config?.StorageCapacity > 0;
    public override void _Ready() { Storage = new AmmoStorage(Config.StorageCapacity); RegisterBuilding(); SetPhysicsProcess(false); }
    public override string DescribeRuntime() => $"Stored ammo: {Inventory.Amount:N0} / {Inventory.Capacity:N0}\nIncoming reserved: {Logistics?.ReservedSpace(this) ?? 0}\n" + DroneStatus;
}
