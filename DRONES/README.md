## FIGHTER — future persistent combat/fighter squad (new roadmap, 2026-11-06)

**PLANNED / NOT IMPLEMENTED.** [The fighter-drone implementation plan](Combat/FIGHTER_DRONES_IMPLEMENTATION_PLAN.md) defines a new placeable **Drone Station** and multiple persistent friendly fighter drones. Initial modes: **Auto-pilot** (intercept the enemy with shortest remaining journey to an assigned Citadel, attack while orbiting/figure-eight/kiting) and **Auto-patrol** (select a fighter and click two map waypoints for a repeating figure-eight, firing opportunistically). Finite HP/ammo, return-to-home service, real station resupply and future enemy anti-air are included. Phases FD0–FD6 keep fighter behaviour, controller, station, weapons, input and feature-local saving under `DRONES/Combat/`; reuse `DRONES/Shared/` and integration with existing combat, catalog, enemy targeting and ammo logistics **without moving or merging current construction/courier job managers**. The older one-off corridor-strike player ability remains a separate long-term idea, not a substitute for persistent fighters.

---

## Fixed acceleration and braking — 2026-11-06

**Source Implemented / Awaiting Owner Validation.** Builder and ammunition flight now integrates fixed acceleration/deceleration rates (800px/s² each by default) around the owner's configured cruising speed. Long trips add constant-speed cruise; very short trips lower peak speed without changing rates. Organic cubic routes and heading remain; one 64-segment distance table per leg plus bounded local refinement maps physical distance onto the curve instead of easing over the whole journey. No per-frame arrays, routefinding or extra Nodes/timers. Tune FlightAcceleration/FlightDeceleration on the builder config or courier scene.

New flat optional flight-profile checkpoint properties preserve timing; restored old snapshots retain their current legacy eased leg, with new rates applied on their next leg. Tables are derived, privately retained and rebuilt once after deserialization. Existing work/cargo/arrival/return ownership stays intact.

**Checks:** numerical smoke comparison of 1,000px/10,000px legs gives ~16px launch travel at 0.2s and ~16px braking distance at 0.2s before arrival for both. Existing Drone2Checks extended with distance-independent rates, short-leg peak speed, endpoint/no-overshoot and JSON/legacy cases, but not run: .NET unavailable. Owner: compare short/long builder and ammunition trips, arrival/return, pause and a fresh checkpoint.

---

## Smaller approach-fading widget — 2026-11-06

**Source Implemented / Awaiting Owner Validation.** Build/upgrade telemetry scales from 0.80 to 0.72 (another 10% smaller) and moves closer to the upper-right footprint corner (offset -8,-4). It stays hidden while queued/distant, then fades over 0.25s when its assigned builder is within 140px of the final hover arrival point. Visibility stays latched during work/orbit and composes with the existing 100% completion fade. Work without a held builder also fades in. Reveal distance/fade/scale/offset are editable in DefaultConstructionSiteVisualConfig.tres. The existing shared active-only clock reads an O(1) assignment query; no new scan, timer or process per building. Source review only; owner visual acceptance pending.

---

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
- [Combat](Combat/README.md): **new persistent fighter-drone/Drone Station roadmap** (FD0–FD6; no gameplay yet), with an older one-off player ability retained only as a separate future idea.

Both roles ignite two tiny rear cyan-white engines at outbound/return departure, soften propulsion while braking, and draw a thin fading trail from bounded recent actual positions. Builder hover has only a tiny glow and no trail. Hidden/parked entries reset their effects and have no processing. The existing movement updates drive presentation; no extra effect timers, particle nodes or flight scheduler.

Construction-only telemetry/acquisition lives in the existing shared site hologram canvas. Courier jobs never show a construction beam or site labels. Both systems preserve existing gameplay pause and save ownership. See [implementation/acceptance plan](DRONE2_UNIFIED_DRONES_IMPLEMENTATION_PLAN.md) and [focused owner checks](../TESTS/Drones/README.md). Source/resource/diff checks only; .NET unavailable, Godot visual/performance acceptance pending.
