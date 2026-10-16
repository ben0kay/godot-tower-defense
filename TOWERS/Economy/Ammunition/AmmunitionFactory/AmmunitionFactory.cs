// Automatically advances the existing AmmoProduction helper; logistics alone ships actual buffered stock.
using Godot;
using System;

public partial class AmmunitionFactory : AmmoBuilding
{
    [Export] public AmmunitionFactoryConfig Config { get; set; }
    public AmmoProduction Production { get; private set; }
    public long ProducedRounds { get; private set; }
    public override TowerConfig AuthoredConfig => Config;
    public override AmmoInventory Inventory => Production?.Buffer;
    public override int DroneSlots => Math.Max(0, Config.DroneSlots);
    public override int DronePayload => Math.Max(1, Config.DronePayload);
    public override float DroneSpeed => float.IsFinite(Config.DroneSpeed) ? Mathf.Max(1, Config.DroneSpeed) : 220;
    public override bool AcceptsConfig(TowerConfig config) => config is AmmunitionFactoryConfig c && c.BatchAmount > 0
        && c.InternalBufferCapacity >= c.BatchAmount && c.ShipmentAmount > 0 && c.ShipmentAmount <= c.InternalBufferCapacity
        && c.DronePayload >= c.ShipmentAmount && float.IsFinite(c.ProductionSecondsPerBatch) && c.ProductionSecondsPerBatch > 0;
    protected override void ApplyConfig(TowerConfig config) => Config = (AmmunitionFactoryConfig)config;

    // =========================================================
    // Validate before payment and keep malformed production settings from creating a half-built factory.
    public override bool ValidateScene() => Config != null && Config.BatchAmount > 0 && Config.InternalBufferCapacity >= Config.BatchAmount
        && Config.ShipmentAmount > 0 && Config.ShipmentAmount <= Config.InternalBufferCapacity && Config.DronePayload >= Config.ShipmentAmount
        && float.IsFinite(Config.ProductionSecondsPerBatch) && Config.ProductionSecondsPerBatch > 0;
    public override void _Ready()
    {
        Production = new AmmoProduction(Config.BatchAmount, Config.ProductionSecondsPerBatch, Config.InternalBufferCapacity);
        RegisterBuilding(); SetOperationalPhysicsProcess(true);
    }

    // =========================================================
    // No depot is required: the finite buffer naturally pauses/resumes the existing batch helper.
    public override void _PhysicsProcess(double delta)
    {
        if (!IsOperational || !double.IsFinite(delta) || delta <= 0 || Production == null) return;
        ProducedRounds += Production.Advance(delta);
    }
    public override string DescribeRuntime() => $"Buffered ammo: {Inventory.Amount:N0} / {Inventory.Capacity:N0}\n" +
        (!IsOperational ? "Production waiting for construction" : Production.IsPaused ? "Production paused — buffer full" : "Producing automatically") + "\n" + DroneStatus;
}
