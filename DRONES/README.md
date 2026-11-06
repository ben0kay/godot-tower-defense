## Percentage-only construction widget — 2026-11-06

**Source Implemented / Awaiting Owner Visual Validation.** Removed DRONE INBOUND, SITE LOCKED, SYSTEM ONLINE and the job caption. The same animated circular percentage is shown while waiting/working and briefly counts up to 100% on successful completion before fading. The entire widget (arcs, dots and number) defaults to 80% scale via `TelemetryScale`, and anchors above/right of the building's upper-right footprint corner. Existing collision-avoidance lanes, shared bounded redraw, work progress, pause and save ownership remain. Acquisition footprint flashes remain visual-only and do not display status text. Source review only; .NET unavailable. Owner: check queue/acquire/build/upgrade/completion and crowded sites in Godot.

---

## DRONE2 — circular fabrication telemetry (2026-11-06)

**Source Implemented / Awaiting Owner Visual Validation.** During actual CONSTRUCTING/UPGRADING, the old dark rectangular floating readout becomes a circular cyan progress display with three independently turning arcs and drifting dots. Inbound, locked and online statuses use bare text without a panel. No drone movement, ammo delivery, construction work timing or gameplay logic changes. [Details](../TOWERS/Shared/Construction/Visuals/README.md).

---

# DRONES — shared flight and presentation, separate job owners

## DRONE2

**Source Implemented / Awaiting Owner Validation.** Builders and ammunition couriers now use the same36×28 cyan drone model, shared cubic/eased flight math and shared twin-engine/ion-trail presenter. Construction work and ammunition dispatchers remain independent.

- [Shared](Shared/README.md): plain flight state, shared body scene and cosmetic configuration.
- [Transport](Transport/README.md): active CargoDrone adapter for Factory→Depot and Depot→Tower, then return. AmmunitionLogistics owns stock, reservations, slots and callbacks.
- [Construction](../TOWERS/ConstructionDrones/README.md): ConstructionDroneManager retains FIFO, Citadel assignment, pooled visuals, site orbit, acquisition/deployment holds and return.
- [Combat](Combat/README.md): future player-ability concept; no combat drone system is introduced.

Both roles ignite two tiny rear cyan-white engines at outbound/return departure, soften propulsion while braking, and draw a thin fading trail from bounded recent actual positions. Builder hover has only a tiny glow and no trail. Hidden/parked entries reset their effects and have no processing. The existing movement updates drive presentation; no extra effect timers, particle nodes or flight scheduler.

Construction-only telemetry/acquisition lives in the existing shared site hologram canvas. Courier jobs never show a construction beam or site labels. Both systems preserve existing gameplay pause and save ownership. See [implementation/acceptance plan](DRONE2_UNIFIED_DRONES_IMPLEMENTATION_PLAN.md) and [focused owner checks](../TESTS/Drones/README.md). Source/resource/diff checks only; .NET unavailable, Godot visual/performance acceptance pending.
