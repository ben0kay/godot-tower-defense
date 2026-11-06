## SPAWN2 all-portals warp-in design (2026-11-06 — planned)

The next planned visual pass is **not just a fade for future-wave portals**: *every* newly appearing portal (START portals together on fresh level load; Wave N portals on activation) should play the same ambitious **spiralling violet dimensional breach** with swirling particles, growing ring, short flash and transition to the established rotating idle look. Start-at-load is `AppearAtWave=0`; numeric Wave 1 remains wave-start activation. SPAWN1 activation is already source-implemented, so SPAWN2 must add a synchronized forming/ready gate to avoid spawning out of invisible openings. Reuse existing rotating shader and small spawn bursts; constrain opening VFX without sacrificing the art direction. [Full Codex handoff](../../ENEMIES/Spawning/IMPLEMENTATION_PLAN.md#phase-spawn2--shared-cinematic-dimensional-warp-in-for-all-portals-codex-implementation-specification). **Gameplay and visuals not implemented by these notes.**

---

## SPAWN0 / SPAWN1 and portal feedback

**Source Implemented / Awaiting Owner Validation.** `EnemyPortal.AppearAtWave=0` means START, positive values reveal at that authored wave Number. All future portals remain registered/reserved; SpawnManager and WaveManager own eligibility and ordering. The shared scene now includes 2°/s shader artwork rotation and a bounded purple GPUParticles2D spawn burst, configurable per portal. See [portal authoring/visuals](EnemyPortal/README.md) and [source contract](../../ENEMIES/Spawning/README.md). Historical no-setting/planned statements below are superseded for SPAWN0/1; SPAWN2–5 remain planned.

---

## Planned Wave N activation and materialization — 2026-11-06

The current placeable `EnemyPortal` scene has **no wave-activation setting**: registered portals are visible and spawn-eligible from deployment. An authored Inspector `AppearAtWave = 0` (START default, or positive Wave N) plus hidden, pre-reserved future locations, validation of checkpoint/Citadel reachability, one-time materialization VFX and future lifetime rules are **planned only** in [the spawning implementation plan](../../ENEMIES/Spawning/IMPLEMENTATION_PLAN.md). The portal remains visuals/position/entry authoring; `WaveManager` and `SpawnManager` still own timing and selection.

---

# PORTALS — Reusable Physical World Spawn Objects

This folder owns **placeable Portal scenes**, *not* spawning mechanics. A Portal belongs to the map layout, can have future art/activation/VFX and provides a location where enemy ships may emerge.

- `EnemyPortal/EnemyPortal.tscn` — **visible editor-placeable placeholder scene** with `SpawnExit` Marker2D.
- `EnemyPortal/README.md` — layout and later integration notes.

A level designer can instance one or many Portals under `LevelXXX/Portals`. Some levels use **no Portals** at all and instead spawn from map edges; others may use both.

**Responsibilities:** expose exit position, optional unique ID, active state and visuals. **Do not put SpawnManager, wave timing, ship selection or navigation algorithms here.** Those belong in `ENEMIES/Spawning/`, `LEVELS/Waves/` and `ENEMIES/Movement/`.

The current Portal scene includes placeholder polygons, a SpawnExit and Inspector-editable WorldObjectFootprint properties. It reserves its cell against towers and is instanced in Level001. It has no spawning code or physics collision. Add a PortalMasterConfig only when actual per-Portal settings are defined, rather than an empty Resource.


Per-level/per-instance size overrides now live directly on EnemyPortal.SizeMultiplier, while source selection/timing remain in SpawnManager/WaveManager. Only Visuals and spawn spread scale; marker center/grid reservations are unchanged. Select a portal instance in the level Inspector to override the default1 multiplier.
