# Transport Drones — FUTURE / NOT IMPLEMENTED

**First planned drone role:** ferry ammunition from an [Ammunition Depot](../../TOWERS/Economy/Ammunition/AmmunitionDepot/README.md) to a tower requesting resupply, then return to its home depot.

## Delivery sequence
1. [LOGISTICS/Ammunition](../../TOWERS/Economy/Ammunition/Shared/Logistics/README.md) selects eligible depot, reserves sufficient stock and dispatches a free transport drone.
2. Drone flies directly from the depot through/over empty space to the tower. First pass: simple straight-line flight, not enemy A* pathfinding.
3. On arrival, the tower accepts at most its free local capacity. The logistics layer accounts for delivered and leftover reserved stock; no duplication.
4. Drone returns to the depot and frees its slot. Handle destroyed tower/depot, cancelled request, pause, interrupted flight or map unload safely.

## Planned parameters
Travel speed, payload size, turnaround/unload delay (optional), visuals, per-depot available drone count, optional later health and research boosts. Keep drone **movement/lifecycle** separate from actual ammo stock/transaction ownership.

Factories sending their **production batches to depots** use a simple **automatic logical transfer initially**; transport drones are needed only for **depot-to-tower** deliveries. Later, other transport assignments can reuse this drone category if desired.

**Documentation only; no drone scenes or flight controller yet.**
