# Level011 — Three-Lane Fleet Defence

## Waves 1–5 (2026-11-05)

**Source Implemented / Awaiting Owner Gameplay Validation.** The current painted **192×120** map (64px cells = 12,288×7,680 world pixels) retains its existing terrain, background, route checkpoints, three portal positions, Citadel, and independent WorldConfig. This pass changes only the Level011 wave resource, local active-portal list, mission description and this README.

| Wave | Name | Scheduled | Content |
| --- | --- | ---: | --- |
| 1 | Three-Lane Contact | 54 | Standard Drones and Swift Interceptors from all three entrances |
| 2 | Interceptor Wings | 84 | Reinforced/Racer ships, Gunner Drones and Shield Emitters |
| 3 | Crossfire Formation | 120 | Bulwarks, Heavy Gunners, shielded Shield Frigates and fast flanking groups |
| 4 | Armoured Breakthrough | 162 | Heavy Bulwarks, Elite Assault Ships and two Drone Transporters |
| 5 | Fleet Spearhead | 204 | A shielded Cruiser, three Heavy Warships, a Heavy Transporter and varied escorts |
| **Total** | **First half of ten-wave plan** | **624** | Plus transported drones/interceptors released on death |

The authored `Level011Waves.tres` remains a native editable `WaveSetResource` for the existing **Wave Editor**. Each batch uses explicit `Portals/EnemyPortal1` (upper), `Portals/EnemyPortal2` (middle), or `Portals/EnemyPortal3` (lower). Batches may mix entries from multiple lanes; exact weighted enemy quotas are deterministically shuffled by the existing scheduler. Intervals and pauses vary, and several entries opt into shields. All 15 registered enemy types appear across the five waves: Tiny, Light, Standard, Heavy and Superheavy physical Classes. **Capital and Titan have no registered ships yet.**

**Important:** only waves 1–5 are authored. The game will currently award mission victory after clearing wave 5; waves 6–10 can be appended later as heavier encounters. Carrier DeathSpawn children can increase the real enemy total beyond 624. Counts, timing, bonuses, waves and difficulty are provisional until you test them; no global enemy/tower balance changed.

## Existing map and editing

- **Map dimensions:** `Level011WorldConfig.tres` = 192×120 cells at 64px, leaving all other maps' dimensions unchanged.
- **Inheritance:** `Level011.tscn` inherits `LEVELS/Shared/BaseLevelMap.tscn`, with four level-local paintable TileMapLayers; its authored terrain is untouched.
- **Existing routes:** upper portal at (1504,2016), middle at (1504,3424), lower at (1440,4832). Each portal references its own `Checkpoints/RouteCheckpoint*` chain. Do not rename portal paths without updating the config and wave entry selectors.
- **Citadel:** `Objectives/Citadel` at (10336,3488), unchanged.
- **Level config:** `Level011Config.tres` has three active portals, 2,000 starting Credits, 1,000 Citadel HP and wave ID `level011`.
- **Campaign:** `Level011Mission.tres` remains enabled and deploys through shared Battlefield/World services.

To rebalance, enable the existing Godot Wave Editor, open `res://LEVELS/Level011/Level011Waves.tres`, change batches/enemies and Save. For map edits open `res://LEVELS/Level011/Level011.tscn` in the 2D editor. Confirm all three routes are reachable before judging balance. Wave 6 onward should shift toward more Heavy/Superheavy ships, with Capital/Titan only after those types actually exist.

**Validation:** source/resource structure reviewed; Godot editor import, active portal reachability, in-game performance and balance not tested here.
