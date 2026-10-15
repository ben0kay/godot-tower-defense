# Ammunition production transfers and supply drones — FUTURE / NOT IMPLEMENTED

## Planned gameplay loop
1. [Ammo Factory](../../TOWERS/Economy/AmmunitionFactory/README.md) produces a batch with time/material constraints.
2. The batch transfers to an eligible [Ammo Depot](../../TOWERS/Economy/AmmunitionDepot/README.md) if space is available. Initially this transfer may be logical; later physical transport is optional.
3. When a [tower's local reserve](../../TOWERS/Shared/Ammunition/README.md) falls below a tunable threshold, create **one outstanding request**.
4. A shared dispatcher selects a depot with matching stock and a free drone; reserves a limited payload, launches a drone directly through space to the tower, and marks the delivery in flight.
5. The drone transfers only what fits into the tower, then returns to its home depot; the tower can keep firing while it travels or run dry if late.

## Future parameters
Batch size/interval, ammo identity, storage cap, resupply threshold, drones per depot, drone speed, carrying capacity, dispatch policy and optional future research efficiency. Keep config/maxima separate from per-instance stock, timers and active flight state.

## Conservation and performance
Account for stored, reserved/in-flight and delivered quantities **exactly once**. Explicitly handle target/depot deletion, insufficient stock, interruptions, cancellation, pause and world unload without duplicated deliveries or stranded drone slots. Use only active requests and active drone updates, not repeated full-world or full-enemy-registry scans. Drone routing can be straight-line initially, independent of enemy grid pathfinding.

Future save/checkpoint handling must either settle jobs or preserve enough stable identifiers to resume safely, without persisting live Godot node IDs.

**Design only. No gameplay implementation yet.**
