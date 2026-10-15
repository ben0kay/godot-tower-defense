# Ammunition Depot / Storage — FUTURE / NOT IMPLEMENTED

## Purpose
Player-placeable **large central ammunition reserve**. A factory automatically moves completed batches from its small buffer to eligible depot storage whenever the depot has space. A depot is not required for a factory to begin manufacturing; without any depot, the factory simply fills and pauses.

## First-pass storage behaviour
- Proposed `StorageCapacity` and `CurrentStoredAmmo` per depot, with optional ammunition-type identity. Accept **whole completed batches** from factories only when sufficient free space exists; partial batch distribution can be designed later.
- Multiple depots may accept transfers. On construction or stock depletion, freed capacity should allow backlogged factories to transfer and resume. Do not make every factory search all depots per physics frame.
- Track accepted inventory with exact transfers between factory buffers and storage; do not create free ammo on transfer or magazine reload.
- Depots can initially store batches with no active drone, electricity or tower resupply gameplay. Later add configurable drone slots and reserve stock for deliveries.

## Drone resupply (later)
A depot provides a home/available slot for each [Transport Drone](../../../../DRONES/Transport/README.md). The [LOGISTICS/Ammunition](../Shared/Logistics/README.md) dispatcher picks jobs, reserves finite payloads and accounts for inflight/delivered stock; the drone flies to the requesting tower, unloads up to its capacity and returns. Factory → depot stays simple/automatic initially.

Future research: depot storage capacity, drone slots, dispatch efficiency and perhaps delivery priorities; stock belongs to this placed depot, not the generic Credits wallet.

See [Factory](../AmmunitionFactory/README.md) and [Economy summary](../../README.md).

**Planning only; no storage or drone systems currently run.**
