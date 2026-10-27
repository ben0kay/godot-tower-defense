# Level003 — The Bulwark Advance

**Design-ready blank map, four waves authored; runtime/playtest pending (2026-10-27).** This is a direct copy of `LEVELS/LevelTemplate.tscn` with a separate Level003 identity/config and no painted cells. Open `res://LEVELS/Level003/Level003.tscn` in Godot's **2D editor** and paint your own layout. No generated floors, walls, decorations or gameplay tile placements were added.

## Editable layout

- **Passable**, **Impassable**, **Interactive**, **Decorations**: all present with their normal TileSets, all completely empty.
- One movable `Portals/EnemyPortal1` at the template position (3424, 2592) and one movable `Objectives/Citadel` at (3872, 2528).
- The template's space background, disabled optional atmosphere, checkpoints container and F2/F3/debug helpers are retained.
- Paint **connected Passable ground** between the portal and Citadel (plus whatever extra paths/walls you want) before starting waves. A genuinely blank map cannot spawn/path enemies; this is intentional for editing, **not** a ready-to-fight map. Move the entrance/objective before painting if desired. Additional portals must be registered in `Level003Config.tres → ActivePortals`.

## Four manually started waves

Only existing catalog enemies are used: **Enemy001** = `basic_mk1`, **Enemy002** = `scout_mk1`, **Enemy004** = `medium_mk1` (Bulwark). No shields, stealth, support ships, special attacks or DeathSpawn children.

| Wave | Enemy001 | Enemy002 | Enemy004 | Total | Encounter |
| --- | ---: | ---: | ---: | ---: | --- |
| 1 | 12 | 6 | 0 | 18 | Familiar mixed entry |
| 2 | 14 | 8 | 0 | 22 | Higher light-ship pressure |
| 3 | 14 | 8 | 4 | 26 | Introduce Bulwarks |
| 4 | 12 | 12 | 8 | 32 | Mixed finale |
| **Total** | **52** | **34** | **12** | **98** | |

Waves live exclusively in `Level003Waves.cs` (four `Wave(...)` definitions) and are registered by the `WaveSetSource` sub-resource in `Level003Config.tres`; no per-level scheduler or launcher duplication. Spawn gaps are 0.6–1.3 seconds, with pauses between batches. Wave 4's first 16 enemies use exact 75/25 weighted composition.

Settings: **1,200 starting Credits**, **1,000 Citadel HP**, normal provisional profile XP (500 on victory, 0 on defeat). Edit the level-local configuration/resource or waves without affecting Level002. No custom narrator/audio or additional research.

## Play / campaign

`Level003Mission.tres` is **enabled** and registered after Level002 in `LEVELS/Catalog/DefaultMissionCatalog.tres`. Deploy from **Campaign → Levels → Level003** once you've painted a navigable map; Level002's Next now points here. Retry/relaunch use the existing shared `LEVELS/Runtime/Battlefield.tscn`; there is no `MainLevel003` or `WorldLevel003`.

**Validation:** authoring/source inspection only. No Godot/.NET headless or desktop checks were run; paint first and do a brief local map-routing/combat smoke test.
