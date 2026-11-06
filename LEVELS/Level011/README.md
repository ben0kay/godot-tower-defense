# Level011 — Three-Lane Fleet Defence

## Waves 1–10 — 2026-11-06

**Source Implemented / Awaiting Owner Gameplay Validation.** The existing 192×120 map, three active portal paths, terrain, Citadel, routes and level settings are unchanged. This pass adds Waves 6–10 to the native editable `Level011Waves.tres`, preserves Waves 1–5 exactly, and makes Wave 10 the mission's final encounter.

| Wave | Banner | Scheduled ships | Composition and lanes |
| --- | --- | ---: | --- |
| 1 | Three-Lane Contact | 54 | Standard Drones and Scouts across three portals |
| 2 | Interceptor Wings | 84 | Reinforced/Racer ships, Gunners and Shield Emitters |
| 3 | Crossfire Formation | 120 | Bulwarks, heavy gunners, shielded frigates and flankers |
| 4 | Armoured Breakthrough | 162 | Elite escorts and first drone carriers |
| 5 | Fleet Spearhead | 204 | Warships, shielded cruiser, Heavy Transporter, escorts |
| 6 | Heavy Fleet Escalation | 216 | More Heavy Bulwarks, Heavy Gunners, elites, shielded frigates and warships |
| 7 | Carrier Strike | 228 | Heavy Transporters/Drone Transporters behind elite and shield escorts |
| 8 | Shielded Armada | 240 | Shielded Cruisers, Heavy Warships, shield support on all lanes |
| 9 | Siege Vanguard | 252 | Concentrated heavy warships, cruisers, carriers and gunner screens |
| **10** | **Obsidian Dreadnought** | **265** | **Exactly one Capital first from Portal2**, then 264 escorts/support from all three lanes |
| **Total** | | **1,825** | Scheduled ships; transporter death releases may add children |

**Boss arrival:** Wave10 batch 1 has exactly one `obsidian_dreadnought_mk1` from `Portals/EnemyPortal2`, followed by a 7-second gap before a 36-ship shield/elite escort batch on that same lane. Subsequent staged batches bring 60 ships from Portal1, 60 from Portal3, 60 more heavy vessels and shield screens from Portal2, then a 48-ship mixed-lane final reinforcement. Existing batch ordering is sequential; each batch's weighted mix is shuffled deterministically **within** that batch, not independently spawned at all three entrances at once. The Capital is configured in its enemy resource with 13,500 hull HP and four guns; this wave does not alter its stats.

**Balancing:** Waves 6–9 progressively shift toward heavier Heavy/Superheavy and shield-support enemies; no new enemy type, global balance or navigation rule is introduced. Counts/intervals are deliberately spaced (mostly 0.42–0.74 seconds per spawn) rather than instant dumps; heavy transporter destruction can release up to ten additional children each. Heavy waves and boss survivability remain provisional pending Godot gameplay/FPS testing.

**Portal activation:** SPAWN1 can activate authored late portals at Wave N, but this pass does not alter existing portal `AppearAtWave` values or portal scenes. Every explicitly referenced Portal1/2/3 must be active and reachable when its wave starts. If a portal is configured to appear after a wave that references it, the existing wave preflight will report a setup error until the authoring is corrected.

## Existing map and editing

- **Map dimensions:** `Level011Config.tres → WorldConfig` explicitly references `Level011WorldConfig.tres` = 192×120 cells at 64px. The runtime loader applies it before Grid/Camera initialize; an unassigned link falls back to the shared 100×80 default.
- **Inheritance:** `Level011.tscn` inherits `LEVELS/Shared/BaseLevelMap.tscn`, with four level-local paintable TileMapLayers; its authored terrain is untouched.
- **Existing routes:** upper portal at (1504,2016), middle at (1504,3424), lower at (1440,4832). Each portal references its own `Checkpoints/RouteCheckpoint*` chain. Do not rename portal paths without updating the config and wave entry selectors.
- **Citadel:** `Objectives/Citadel` at (10336,3488), unchanged.
- **Level config:** `Level011Config.tres` has three active portals, 4,000 starting Credits, 1,000 Citadel HP and wave ID `level011`.
- **Campaign:** `Level011Mission.tres` remains enabled and deploys through shared Battlefield/World services.

To rebalance, enable the existing Godot Wave Editor, open `res://LEVELS/Level011/Level011Waves.tres`, change batches/enemies and Save. For map edits open `res://LEVELS/Level011/Level011.tscn` in the 2D editor. Confirm all three routes are reachable before judging balance. Waves 6–9 now emphasize Heavy/Superheavy ships, and Wave 10 uses the registered Capital. Titan remains unregistered.

**Validation:** source/resource structure reviewed; Godot editor import, active portal reachability, in-game performance and balance not tested here.
