// Passive placeable ammunition structure; no mandatory targeting, weapon stats, ranks or attack nodes.
using Godot;
using System.Collections.Generic;

public abstract partial class AmmoBuilding : TowerBase
{
    public abstract AmmoInventory Inventory { get; }
    public abstract int DroneSlots { get; }
    public abstract int DronePayload { get; }
    public abstract float DroneSpeed { get; }
    protected AmmunitionLogistics Logistics;
    public override List<TowerLevel> Levels => AuthoredConfig.Levels;
    public override TowerFinalStats ResolveLevel(int level) => null;

    // =========================================================
    // Each concrete building creates its own foundation helper before registering with this world's dispatcher.
    protected void RegisterBuilding()
    {
        Inventory.Changed += NotifyRuntimeChanged;
        Logistics = GetNodeOrNull<AmmunitionLogistics>("../../AmmunitionLogistics");
        Logistics?.RegisterBuilding(this); SetProcess(false);
    }
    public override void _ExitTree() { if (Inventory != null) Inventory.Changed -= NotifyRuntimeChanged; }

    // =========================================================
    // Shared source-slot status supplements each building's own stock/production information.
    protected string DroneStatus => $"Drones: {Logistics?.BusySlots(this) ?? 0} / {DroneSlots} busy";
}
