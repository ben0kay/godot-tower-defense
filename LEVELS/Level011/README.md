## Faster opening and immediate Level011 Wave 6 debut — 2026-11-06

**Source Implemented / Awaiting Owner Validation.** Default shared materialization duration is halved from 2.2s to 1.1s; opening particle lifetimes scale down to drain before completion. Enemy delivery still waits for the genuine Open/readiness event, then resumes on the scheduler's next tick without a separate post-opening timer. Checkpoint/no-replay and pause semantics remain.

Level011 Wave 6 formerly shuffled four Portal4 Reinforced Drones among the first 54 ships, so a formed portal could wait for its first selected entry. One existing Reinforced Drone is now a guaranteed opening batch from Portal4, followed 0.5s later by the remaining 53 ships in that original batch (three remaining Reinforced Drones). Total wave count stays 216; Portal4 stays 26 ships; all other enemy quotas, portal assignments, later batch gaps and other waves are unchanged. The first ship waits safely through opening and launches immediately when ready. Source/resource quota review only; owner in-engine acceptance pending.

---

## Portal 4 debut and faster opening waves — 2026-11-06

**Source Implemented / Awaiting Owner Gameplay Validation.** The already-authored fourth portal at `(7136, 3552)`, with `AppearAtWave = 6` and its existing `RouteCheckpoint3a` link, is now included in `Level011Config.tres → ActivePortals`. SPAWN1 keeps it hidden/ineligible for waves 1–5 and activates it at the **accepted start of Wave 6**, before the first spawn. Wave 6 now sends **26 of its existing 216 scheduled ships** through `Portals/EnemyPortal4`: 4 Reinforced Drones from its first batch, 10 Elite Assault Ships from its second, and 12 Heavy Gunner Drones from its final batch. The mix is shuffled within each batch, so Portal4 ships are interleaved rather than a deterministic immediate first spawn. Every other wave 6 ship and the entire waves 7–10 roster remains unchanged.

**Waves 1–5** now use approximately **20% shorter between-spawn intervals** and **0.5 seconds shorter inter-batch gaps** where a positive gap existed. The authored enemy IDs, counts (54/84/120/162/204), weights, shields, portal assignments, wave order and all five banners stay unchanged. This is a pacing change, not extra enemies or faster enemy movement. The original values can be compared in Git history and adjusted through the existing Wave Editor.

**Owner check:** refresh Godot resources, confirm Portal4 is invisible during waves 1–5, appears when wave 6 begins, and successfully spawns its three assigned ship groups; inspect F3 routing from its central/lower checkpoint and check for `SETUP ERROR`/`BLOCKED`. The Wave Editor should display the new per-batch intervals and portal selectors. Source-only validation here; no in-engine gameplay or FPS claims.

---

# Level011 — Three-Lane Fleet Defence

## Waves 1–10 — 2026-11-06

**Source Implemented / Awaiting Owner Gameplay Validation.** The earlier waves 6–10 authoring preserved the 192×120 map and initial three portal paths; the subsequent Portal4 registration/pacing update above supersedes that status. This pass adds Waves 6–10 to the native editable `Level011Waves.tres`, preserves Waves 1–5 exactly, and makes Wave 10 the mission's final encounter.

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

**Portal activation:** SPAWN1 can activate authored late portals at Wave N, but this pass does not alter existing portal `AppearAtWave` values or portal scenes. Portals 1–3 are active from deployment; Portal4 is registered but reveals at the accepted start of Wave 6. Every explicitly referenced portal must be active and reachable when its wave starts. If a portal is configured to appear after a wave that references it, the existing wave preflight will report a setup error until the authoring is corrected.

## Existing map and editing

- **Map dimensions:** `Level011Config.tres → WorldConfig` explicitly references `Level011WorldConfig.tres` = 192×120 cells at 64px. The runtime loader applies it before Grid/Camera initialize; an unassigned link falls back to the shared 100×80 default.
- **Inheritance:** `Level011.tscn` inherits `LEVELS/Shared/BaseLevelMap.tscn`, with four level-local paintable TileMapLayers; its authored terrain is untouched.
- **Existing routes:** upper portal at (1504,2016), middle at (1504,3424), lower at (1440,4832), and late fourth portal at (7136,3552) (Wave 6, linked to RouteCheckpoint3a). Each portal references its own `Checkpoints/RouteCheckpoint*` chain. Do not rename portal paths without updating the config and wave entry selectors.
- **Citadel:** `Objectives/Citadel` at (10336,3488), unchanged.
- **Level config:** `Level011Config.tres` registers four portal paths (fourth activates at wave 6), 4,000 starting Credits, 1,000 Citadel HP and wave ID `level011`.
- **Campaign:** `Level011Mission.tres` remains enabled and deploys through shared Battlefield/World services.

To rebalance, enable the existing Godot Wave Editor, open `res://LEVELS/Level011/Level011Waves.tres`, change batches/enemies and Save. For map edits open `res://LEVELS/Level011/Level011.tscn` in the 2D editor. Confirm all three routes are reachable before judging balance. Waves 6–9 now emphasize Heavy/Superheavy ships, and Wave 10 uses the registered Capital. Titan remains unregistered.

**Validation:** source/resource structure reviewed; Godot editor import, active portal reachability, in-game performance and balance not tested here.
