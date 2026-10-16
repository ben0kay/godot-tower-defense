# DRONES — Shared mobile units (FUTURE / NOT IMPLEMENTED)

## Purpose
A dedicated umbrella for **drones as reusable moving game units**, not merely an ammunition-transport implementation. The first planned use is carrying ammunition **from factories to depots AND from depots to towers**, using the same transport drone movement implementation. Later, **player abilities** may deploy attack drones along enemy corridors to destroy enemies encountered en route. Transport and attack behaviours must not become tangled.

## Potential folder structure when code is required
```text
DRONES/
  README.md
  Shared/              # Later: identity, flight, lifecycle, visuals and reusable tuning
  Transport/           # First use: ammunition delivery drone behaviour
  Combat/              # Later: player-deployed anti-enemy path attack drones
```
Only README planning folders exist today. Do not add generic classes/frameworks before a concrete drone implementation is requested.

## Shared design principles
- Drones are **not placeable towers** and must not inherit combat/tower constraints. Transport and combat may share flight speed, collision/visualization, optional health, despawn/return lifecycle and future upgrade/research inputs.
- One drone definition/type and per-instance state where needed: current job, target/route, payload, flight state. Apply upgrades to the appropriate capabilities without accidentally giving transport drones weapons.
- [LOGISTICS/Ammunition](../TOWERS/Economy/Ammunition/Shared/Logistics/README.md) owns transfer demand and cargo/destination reservations. Both [Ammo Factories](../TOWERS/Economy/Ammunition/AmmunitionFactory/README.md) and [Ammo Depots](../TOWERS/Economy/Ammunition/AmmunitionDepot/README.md) own inventory and source drone slots for their respective flight legs. **DRONES/Transport** owns flying and arrival/return, not inventory accounting. [AmmoInventory](../TOWERS/Shared/Ammunition/AmmoInventory.cs) is already implemented and should be reused by Codex.
- A future **combat drone player ability** would choose an enemy corridor/path, launch a drone and deal damage to enemies encountered along that flight. Integrate combat with existing COMBAT damage attribution and EnemyRegistry query facilities, rather than copying weapon or enemy pathfinding systems.
- Prefer active-drone updates only, pooled/lightweight visuals and budgeted searches for high enemy counts. Saving should use stable jobs and identifiers, never live node IDs.

## Future research
Drone speed, payload, survivability, drone count/cooldowns and offensive capability are **independent tunables**; actual source/upgrade ownership is decided when research and abilities are implemented.

See [Transport](Transport/README.md) and [Combat](Combat/README.md).

**Planning only; no drones or player abilities are currently implemented.**
