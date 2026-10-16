> **CORE CODE IMPLEMENTED:** [AmmoStorage.cs](AmmoStorage.cs) provides `Inventory : AmmoInventory`, `CurrentStock`, `FreeSpace`, `TryReceiveFrom(droneCargo,rounds)` and `TryLoadInto(droneCargo,rounds)`. **No placeable depot scene exists yet.** Codex must connect a per-depot instance to a buildable scene/Inspector and supply dispatch, and only accept factory drone cargo **on arrival**. See [WORK.md](../../../../NOTES/WORK.md).

# Ammunition Depot / Storage — FUTURE / NOT IMPLEMENTED

## Purpose
Player-placeable **large central ammunition reserve**. A factory sends completed batches from its small buffer **via a visible drone** to an eligible depot with reserved space. A depot is not required for a factory to begin manufacturing; without any depot, the factory simply fills and pauses.

## First-pass storage behaviour
- Proposed `StorageCapacity` and `CurrentStoredAmmo` per depot, with optional ammunition-type identity. Accept **whole completed drone-borne batches on arrival** only when sufficient reserved space exists; partial batch distribution can be designed later.
- Multiple depots may accept transfers. On construction or stock depletion, freed capacity should allow backlogged factories to transfer and resume. Do not make every factory search all depots per physics frame.
- Track accepted inventory with exact transfers between factory buffers and storage; do not create free ammo on transfer or magazine reload.
- The standalone storage helper operates without a drone, electricity or tower resupply; **gameplay integration requires drones for incoming factory deliveries**. Later add configurable drone slots and reserve stock for deliveries.

## Drone resupply (later)
A depot provides a home/available slot for each [Transport Drone](../../../../DRONES/Transport/README.md). The [LOGISTICS/Ammunition](../Shared/Logistics/README.md) dispatcher picks jobs, reserves finite payloads and accounts for inflight/delivered stock; the drone flies to the requesting tower, unloads up to its capacity and returns. Factory → depot stays simple/automatic initially.

Future research: depot storage capacity, drone slots, dispatch efficiency and perhaps delivery priorities; stock belongs to this placed depot, not the generic Credits wallet.

See [Factory](../AmmunitionFactory/README.md) and [Economy summary](../../README.md).

**Planning only; no storage or drone systems currently run.**
