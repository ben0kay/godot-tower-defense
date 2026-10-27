## Level002 tutorial waves — 2026-10-18 (implemented)

The newly painted Level002 is now playable through **Campaign → Levels → PLAY LEVEL 002**, `MainLevel002.tscn` (F6), and Level001's Next Level result action. Its dedicated `WORLD/WorldLevel002.tscn` mounts the existing user-authored map unchanged. Level020 remains separately playable and unchanged. Level002 Next now leads to the enabled, blank-design Level003 mission. **Paint Level003's passable terrain before attempting its four waves.** It never skips ahead to Level020.

`LEVELS/Level002/Level002Waves.cs` defines four modest, manually started waves. Enemy “1–2–3” means the three introductory plain roles: Standard Drone (`basic_mk1`), Swift Interceptor (`scout_mk1`) and Bulwark Drone (`medium_mk1`, asset Enemy004). There is no Enemy003 scene. No shields, support ships, carriers, bosses or extra mechanics are introduced.

| Wave | Standard | Swift | Bulwark | Total | Introductory purpose |
| --- | ---: | ---: | ---: | ---: | --- |
| 1 | 12 | 0 | 0 | 12 | Baseline shooting/building |
| 2 | 10 | 6 | 0 | 16 | Fast, fragile targets |
| 3 | 12 | 4 | 4 | 20 | Slower, tougher targets |
| 4 | 16 | 4 | 4 | 24 | Light mixed finale |

Spawn intervals are 0.65–1.4 seconds, with 3-second gaps between batches. Wave4 uses exact weighted composition; all other batches use one type. Existing Start/Next Wave controls allow unlimited preparation time. Local settings: **1,000 starting Credits**, **1,000 Citadel HP**, existing provisional **500 victory XP / 0 defeat XP**. Both authored portals (`EnemyPortal1`, `EnemyPortal2`) are permitted; map-edge spawning is replaced by portal mode for this level. Counts/timings remain C#-authored for AI-assisted tuning as requested. No map tile, Portal/Citadel coordinate, shared scheduler or enemy/tower balance changes.

C# build and `Level002TutorialChecks.tscn` passed: actual map/scheduler spawned all 72 unshielded enemies and produced victory only after wave4. `ResultsChecks.tscn` passed actual Level001→Level002 navigation, retry, menu and missing-next behavior. The isolated tutorial test kills enemies to verify scheduling/ownership; desktop tower balance/playfeel remains provisional. Earlier unplayable-template notes below are historical and superseded by this section.

---

# Level002 — fresh map-design scene

Open `res://LEVELS/Level002/Level002.tscn` in the 2D editor to paint the next map. This is a fresh copy of `LEVELS/LevelTemplate.tscn`, with its own root name, scene identity and `Level002Config.tres`. Terrain layers start empty; template Portal/Citadel/checkpoint overlay and background are retained. The shared template and its default config are unchanged.

Edit this local config without affecting other maps. Defaults retain template map-edge spawning, zero starting Credits and no wave set. Configure terrain, sources, routes, rewards/waves and playable launcher before adding it to Campaign/PlayableLevelCatalog. It is intentionally not registered as a completed playable mission yet; use the existing map-authoring workflow in LEVELS/README.md.

The former Level002 / Five Gates map is preserved at `LEVELS/Level020/Level020.tscn`, with its renamed config/waves, `WORLD/WorldLevel020.tscn` and `MainLevel020.tscn`. Campaign now labels that map Level020. Do not put old Five Gates content back into this folder.
