# Fighter / Combat Drones — persistent friendly air wing

**PLANNED / NOT IMPLEMENTED (2026-11-06).** The authoritative new direction is **multiple player-owned fighter drones**, based at a **new placeable Drone Station**, with two initial modes: **Auto-pilot** (intercept the nearest threat to a Citadel using remaining route distance; attack while orbiting/figure-eight/kiting) and **Auto-patrol** (select one drone, choose two map points A/B, fly a continuous figure-eight between them and fire at enemies entering range). Fighters have independent HP, finite ammo, return-to-station resupply/repair and eventual enemy anti-air vulnerability.

**Read the full Codex staged design:** [FIGHTER_DRONES_IMPLEMENTATION_PLAN.md](FIGHTER_DRONES_IMPLEMENTATION_PLAN.md) (FD0–FD6: contracts, station/one fighter, autopilot, patrol UI, weapons, station resupply and saving). Fighter-specific code and content belong in `DRONES/Combat/`, including `Station/`, `Artwork/`, `UI/` and optional `Saving/`. Integrate by referencing existing `TOWERS` catalog/placement, shared `COMBAT` damage/projectile, `ENEMIES` registry/targeting and ammunition source systems — **do not recreate those frameworks inside DRONES**. `DRONES/Shared/` remains only genuinely generic body/flight, and construction/cargo gameplay dispatchers remain independent in their existing owner folders.

**Scope reminder:** no fighter, station, aerial enemy target, combat damage, ammunition/repair supply or control UI is implemented by these notes; no gameplay code or scenes have been changed.

---

## Historical separate idea — short-lived player combat ability (not the new squad design)

The following old concept is retained for reference only; it is **not** the first requested fighter-drone implementation.

## Player-ability concept
A future **player ability** may launch a drone along a chosen/defined enemy path or corridor to attack enemies encountered during its flight. This is separate from depot resupply: it should not require ammo cargo, depot drone slots or a logistics dispatch job unless a future mechanic explicitly calls for those.

## Possible gameplay parameters
Flight speed, ability cooldown/charge, attack damage, attack frequency or contact radius, range/path length, lifetime/return behaviour and target filtering. Research could improve these values independently of transport capacity.

## Boundaries
- The player-ability/input system chooses when and where to deploy; this folder would own the combat drone's flight/attack behaviour.
- Reuse [DRONES shared conventions](../README.md) for generic movement/visual lifetime where helpful; integrate actual damage via existing COMBAT and enemy spatial-query/attribution services.
- A corridor-guided flight may need the authored route/corridor geometry, but must not override enemy path selection or create an independent enemy routing engine.
- Design and test hit frequency, interactions with fast/spinning enemies, cooldown UI, overlapping launches, pooling, and 500+ simultaneous enemies only when this ability is actually requested.

**Entirely speculative capability; no ability, weapon or drone implementation now.**
