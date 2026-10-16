> **CODEx NEXT: implement visible transport drones for BOTH factory → depot and depot → tower.** The existing [AmmoInventory](../../TOWERS/Shared/Ammunition/AmmoInventory.cs) supports loading cargo at departure and unloading it only at arrival, and [AmmoProduction](../../TOWERS/Economy/Ammunition/AmmunitionFactory/AmmoProduction.cs) / [AmmoStorage](../../TOWERS/Economy/Ammunition/AmmunitionDepot/AmmoStorage.cs) supply the inventories. No drone scene/controller or delivery jobs are implemented.

# Transport Drones — FUTURE / NOT IMPLEMENTED

**First planned drone role:** ferry actual ammunition on **both routes**: from a Factory to an [Ammunition Depot](../../TOWERS/Economy/Ammunition/AmmunitionDepot/README.md), and from a Depot to a low-ammo tower, returning home after delivery.

## Delivery sequence
1. [LOGISTICS/Ammunition](../../TOWERS/Economy/Ammunition/Shared/Logistics/README.md) selects eligible depot, reserves sufficient stock and dispatches a free transport drone.
2. Drone flies directly from the depot through/over empty space to the tower. First pass: simple straight-line flight, not enemy A* pathfinding.
3. On arrival, the tower accepts at most its free local capacity. The logistics layer accounts for delivered and leftover reserved stock; no duplication.
4. Drone returns to the depot and frees its slot. Handle destroyed tower/depot, cancelled request, pause, interrupted flight or map unload safely.

## Planned parameters
Travel speed, payload size, turnaround/unload delay (optional), visuals, per-depot available drone count, optional later health and research boosts. Keep drone **movement/lifecycle** separate from actual ammo stock/transaction ownership.

Factories must use **factory-owned transport drones** to carry completed batches into depots. Depots must also use **depot-owned transport drones** to resupply towers. Both flight routes include physical outbound, unload and return stages. Later, other transport assignments can reuse this drone category if desired.

**Documentation only; no drone scenes or flight controller yet.**
