# Local tower ammunition and reloading — FUTURE / NOT IMPLEMENTED

## Gameplay design
Optional shared ammunition capability for offensive towers. A tower holds a local **reserve** and may optionally hold a separately loaded **magazine**. Firing consumes the defined units; reloading moves ammunition **from reserve into magazine**, never creates it. Towers without magazines must not be forced to reload, and buildings without weapons must not need this capability.

## Proposed parameters and state
Per-level authored values in each type's complete *Levels.cs rows: AmmunitionCapacity, AmmunitionPerShot or per-activation/rate consumption, optional MagazineCapacity and ReloadSeconds. Ammo type, initial stock and weapon-specific consumption need deliberate definitions: rockets, bullets, laser energy cells, sustained beams and waves may differ. Store CurrentReserve, CurrentMagazine, reload timer and pending resupply as **per-instance runtime state** separate from maxima. No stacked purchased-level bonuses.

If empty, an attack gracefully stops until it has supply; preserve current combat timing, targeting and attribution. The first pass can use a starting stock/debug refill; do not implement factories, drones or a grid in this pass.

## Later integration
[LOGISTICS/Ammunition](../../../LOGISTICS/Ammunition/README.md) handles requests/deliveries; [Ammunition Depot](../../Economy/AmmunitionDepot/README.md) stores remote supply. An accepted delivery is the only way logistics adds reserve ammo to a tower.

**Planned only. Test depletion, reload, partial refills, upgrade capacities, interruption, saves and no duplicated rounds.**
