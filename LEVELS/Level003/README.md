## Wave 4 pressure update — 2026-11-03

Wave 4 now sends **Bulwarks together with Standards and Scouts**, rather than waiting until the end to send a slow, isolated heavy-only batch. It remains **32 enemies total** (12 Standard, 12 Scout, 8 Bulwark), with two mixed 16-enemy batches:

| Batch | Standard | Scout | Bulwark | Interval | Delay after |
| --- | ---: | ---: | ---: | ---: | ---: |
| 1 | 8 | 4 | 4 | 0.55s | 0.4s |
| 2 | 4 | 8 | 4 | 0.50s | 0s |

Each batch shuffles its exact weighted proportions, meaning stronger units are distributed within **both** groups. Under uninterrupted successful spawning, the two batches take roughly **16.15 seconds**, down from about **28.3 seconds** before. Enemy types, shields, stealth and total enemy count are unchanged; waves 1–3 stay intact. The new settings live only in `Level003Waves.tres`, editable from the Wave Editor tab. The original `Level003Waves.cs` is retained as a **historical reference**, not the active Wave 4 recipe.

---

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

Waves now live in the editable **`Level003Waves.tres`** (four waves, 9 batches), referenced directly by `Level003Config.tres → WaveSource`. Open the **Wave Editor** workspace tab after enabling the `addons/wave_editor` plugin; edit and Save this resource to tune the waves without changing C# code. The original `Level003Waves.cs` is retained only as a source reference and parity-check fixture, **not** the gameplay wave source. No per-level scheduler or launcher duplication. Waves 1–3 retain their original intervals and gaps. Wave 4 uses two faster mixed batches (0.55s/0.50s intervals), with Bulwarks in each group and only a 0.4s break.

Settings: **1,200 starting Credits**, **1,000 Citadel HP**, normal provisional profile XP (500 on victory, 0 on defeat). Edit the level-local configuration or Wave Editor resource without affecting Level002. A short text-only mission intro now introduces lava hazards and funneling enemies across them; see `Level003Briefing.tres`. The owner's Level003Briefing.mp3 is registered through Level003BriefingVoice.tres as voice_mission_level003_intro, with the same voice-held typewriter and text-only fallback as Levels 1 and 2. No additional research.

## Play / campaign

`Level003Mission.tres` is **enabled** and registered after Level002 in `LEVELS/Catalog/DefaultMissionCatalog.tres`. Deploy from **Campaign → Levels → Level003** once you've painted a navigable map; Level002's Next now points here. Retry/relaunch use the existing shared `LEVELS/Runtime/Battlefield.tscn`; there is no `MainLevel003` or `WorldLevel003`.

**Validation:** a focused regression comparison against the original C# wave recipe has been added but not run here. Godot/.NET editor, headless and desktop checks remain local; paint passable ground before testing combat.
