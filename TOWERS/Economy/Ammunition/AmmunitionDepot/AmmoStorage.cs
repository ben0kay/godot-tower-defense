// Small depot-owned inventory wrapper. Delivery stock moves through a separate drone cargo inventory.
using System;

public sealed class AmmoStorage
{
    public AmmoInventory Inventory { get; }
    public int Capacity => Inventory.Capacity;
    public int CurrentStock => Inventory.Amount;
    public int FreeSpace => Inventory.FreeSpace;

    // =========================================================
    // Per-depot stock stays independent of the player's Credits wallet and other depots.
    public AmmoStorage(int capacity, int initialStock = 0) => Inventory = new AmmoInventory(capacity, initialStock);

    // =========================================================
    // A flying drone unloads a complete shipment; no stock appears until arrival.
    public bool TryReceiveFrom(AmmoInventory droneCargo, int rounds) =>
        droneCargo != null && droneCargo.TryTransferTo(Inventory, rounds);

    // =========================================================
    // A depot loads its physical stock onto the drone; no second charge on delivery.
    public bool TryLoadInto(AmmoInventory droneCargo, int rounds) =>
        droneCargo != null && Inventory.TryTransferTo(droneCargo, rounds);
}
