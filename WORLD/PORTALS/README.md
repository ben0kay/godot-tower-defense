# PORTALS — Reusable Physical World Spawn Objects

This folder owns **placeable Portal scenes**, *not* spawning mechanics. A Portal belongs to the map layout, can have future art/activation/VFX and provides a location where enemy ships may emerge.

- `EnemyPortal/EnemyPortal.tscn` — **minimal editor-placeable placeholder scene** with `SpawnExit` Marker2D.
- `EnemyPortal/README.md` — layout and later integration notes.

A level designer can eventually instance one or many Portals under `LevelXXX/Portals`. Some levels use **no Portals** at all and instead spawn from map edges; others may use both.

**Responsibilities:** expose exit position, optional unique ID, active state and visuals. **Do not put SpawnManager, wave timing, ship selection or navigation algorithms here.** Those belong in `ENEMIES/Spawning/`, `ENEMIES/Waves/` and `ENEMIES/Movement/`.

The current Portal scene has no collision, texture, exported properties or spawning code; it is a scene shell only. Add a PortalMasterConfig only when actual per-Portal settings are defined, rather than an empty Resource.
