## Level001 voiced briefing — 2026-11-06

The owner's `Level001Briefing.mp3` now plays automatically on **fresh Level001 load**, via `Level001BriefingVoice.tres` (non-positional Voice category) and the existing `VoiceAssistant` queue. `Level001Briefing.tres.VoiceCueId` selects it. The original top-right **typewriter** starts with audio, and text cannot fade until the recording ends plus the standard brief reading hold. A manually started wave does not prematurely clear a still-spoken intro. Gameplay stays active and checkpoint Continue skips the intro. Tune audio level in `Level001BriefingVoice.tres`; edit text in `Level001Briefing.tres`. Runtime audio/visual verification pending.---

## Introductory tactical transmission — 2026-11-06

**Source implemented; owner visual verification pending.** On fresh Level001 load (F5), the compact **top-right narrator** starts typing a short `COMMAND INTELLIGENCE` message automatically while the battlefield stays live and usable at WAVE 00. There is no popup, close button or pause. The first draft mentions Standard Drones, protecting the Citadel for three waves and later Scouts. The text lives in `Level001Briefing.tres` under `Message`/`Speaker`; its old Title/Objectives remain authored for future layouts but aren't shown separately. A `VoiceCueId` has been reserved for later AI voice playback, not activated. When the player presses START WAVE, the normal wave alert and optional wave narration take over. Continue skips the intro; fresh retries show it again. Details: [mission intel](../Briefing/README.md).

---

## Wave setup repair — 2026-11-06

**Source Implemented / Awaiting Owner Validation.** Removed stale `Portals/EnemyPortal2` from Level001Config.ActivePortals: the authored map contains only `Portals/EnemyPortal`. SPAWN1 correctly validates registrations but had promoted this pre-existing warning into a fatal SETUP ERROR. The remaining source, map, terrain and three wave recipes are unchanged. Reload Level001 and confirm START WAVE works. Strict validation remains enabled for missing/early required sources.

---

# Level 001 — Beginner Tower Defense Introduction

`Level001.tscn` is loaded by `WORLD/World.tscn` when you run the game (F5). It is a saved, hand-editable TileMapLayer layout, not generated during gameplay. Placeholder artwork is deliberately replaceable.

## Test and adjust

1. Pull the latest commit, let Godot import assets, and build the C# project.
2. Run **F5** to play the main game. The camera starts over the central base. WASD/arrows pan, middle mouse drags, wheel zooms; Ctrl toggles the tower tray.
3. Choose Battle Tower and try the different terrain tiles. Green previews allow building; red previews reject it. Empty space and unbuildable walls reject towers. Buildable open floors and solid platforms accept them. The debug overlay reports current cell rules and occupancy.
4. Stop the game, open **Level001.tscn**, select `Terrain/Passable` or `Terrain/Impassable`, and paint using that layer's own filtered TileSet palette. Save, then press F5 again. F6 runs only the map scene, without the gameplay camera/HUD/placement controller.
5. Select `Terrain/Decorations` for cosmetic tiles only. Erase gameplay-layer tiles to expose open space. Impassable overrides Passable; Decorations never change cell rules.
6. Move Portal/Citadel instances with grid snapping at 64 px. Object root positions sit at cell centers (32 + 64*n). A Citadel root is the upper-left cell center of its 3×3 footprint.
7. Select the root and edit its `Settings` resource for spawn mode, allowed edges, active Portal paths and Citadel reference. These configure SpawnManager source permissions.

The main base occupies roughly cells (34,31)–(65,49), with disconnected islands and natural terrain nearby. Portal: (36,39), exit: (37,39). Citadel footprint: (60,39)–(62,41); approach markers lie outside the solid footprint. Keep exits/approaches reachable when repainting.

## Shared atlas palette, left to right

| Atlas coordinate | Tile | Buildable | Blocks enemies |
| --- | --- | --- | --- |
| (0,0) | Dark metal wall, amber marks | No | Yes |
| (1,0) | Raised metal platform, cyan marks | Yes | Yes |
| (2,0) | Open metal floor, cyan dashed square | Yes | No |
| (3,0) | Rocky obstacle | No | Yes |
| (4,0) | Open dirt ground | Yes | No |
| (5,0) | Transparent surface decal | No | No |

Paint decals on Decorations. A decal alone never grants build permission. The atlas is shared texture is used by separate category TileSets and other levels; changing tile metadata affects every level using it.

Portal cells are reserved against towers but passable; Citadel cells are reserved and blocked. Enemy spawning, budgeted pathfinding, route-preserving placement and Battle Tower firing are active. Paid tower construction is active; Citadel health and win/lose remain future work.

## F2 enemy test and Inspector settings

Run **F5**, then tap **F2** repeatedly to spawn Enemy001 at the Portal exit. Ships navigate around blocked cells to a reachable Citadel approach and disappear on arrival. Place Battle Towers near their route to shoot them. The Debug HUD counts living, killed and arrived ships; an invalid authored exit/goal reports a safe spawn failure.

Open Level001.tscn and select **Debug** to edit Enabled, SpawnKey, EnemyScene and PortalOnly. Debug/HUD/Info contains the readout; remove/disable Debug to remove level debug controls. Actual ships live under World/Enemies, not Debug. F6 remains map-only viewing.

Select **ENEMIES/Types/Enemy001/Enemy001.tres** for health/speed. Select **TOWERS/Offensive/BattleTower/DefaultBattleTowerConfig.tres** for weapon range, shots/second, damage, projectile speed, target priority and search interval. Default First prioritizes the shortest remaining route to the Citadel among enemies in range. Each built tower has an independent Config copy; edit that tower in Godot's Remote Inspector for temporary per-instance tuning.

Global active-enemy limit, failed-route retry and actual A* calls/frame live in **ENEMIES/DefaultEnemyConfig.tres**. Ships can overlap each other. Small ships use corner-safe cell-center routes; no large-ship routing or flocking yet. Guided bullets currently pass through walls. Route checks run on placement click; a green terrain preview may still reject a route-blocking build and explain why.

## Swarm route preferences

Select ENEMIES/DefaultEnemyConfig.tres and expand Path Diversity. Default four shared profiles use strength 0.6 and smooth eight-cell patches. Tap F2 repeatedly to compare routes where the painted map offers alternatives. Enable optional congestion with CongestionInfluence above zero; it affects new/repath requests at the snapshot interval. Turn EnablePathDiversity off for ordinary shortest paths. Separation settings are reserved and currently do nothing; single corridors can still look single-file.

## Incremental updates and safe movement offsets

EnemyMasterConfig now exposes path-work/validation millisecond budgets, validation count limit, audit interval and movement offsets. Defaults: up to eight A* calculations and 2ms path work per physics frame, sixteen route checks and 0.5ms validity work, staggered audits around every three seconds, offsets enabled with an 8px maximum. Zero milliseconds disables that time limit; zero audit interval disables periodic audits. Time limits are soft per work item.

Placing one tower updates one cell in each shared A* profile. Ships with unaffected routes continue moving. Affected remaining routes are checked/recalculated across frames, with nearby blockages prioritized. The debug HUD distinguishes placement validation, grid synchronization, native A*, total path work and validity-check time. Spawn/arrival endpoints remain exact; interior offsets taper near obstacles. Local separation is still future work.

## Starting credits and construction
In Level001Config.tres (or the level root Settings), edit Starting Resources / Starting Credits. This map currently starts at 1,000. Battle Tower requires 200 Credits by default; set Construction Costs in DefaultBattleTowerConfig.tres. F5 initializes a fresh wallet and successful builds update the top-left credit display. Invalid terrain, insufficient funds and route-blocking builds do not spend. There is no income/kill reward yet.

## Beginner wave progression (editable in Wave Editor)

Level001 now has three deliberately gentle, manually started waves in **`Level001Waves.tres`**, referenced by `Level001Config.tres → WaveSource`:

| Wave | First batch | Second batch | Total |
| --- | --- | --- | ---: |
| 1 | 10 Standard Drones · 0.90s interval | — | **10** |
| 2 | 14 Standard Drones · 0.80s interval | — | **14** |
| 3 | 10 Standard Drones · 0.75s interval | 10 mixed · 0.65s interval (**5 Standard, 5 Scout**) | **20** |

The second Wave 3 batch begins **1.0 second after the first ten Standard spawns have finished**. Only then do the fast, weaker Swift Interceptors (`scout_mk1`) arrive, mixed at 50/50 with Standards in shuffled order. This is intentionally the first encounter with Scouts; **no shields, stealth, Bulwarks or support enemies** are authored. Total mission enemy count is **44**. Waves remain manual so players can place/upgrade towers between assaults. Enemies already spawned keep moving as later batches arrive.

**To add your own commentary later:** open Godot's **Wave Editor** tab → **Open .tres** → `res://LEVELS/Level001/Level001Waves.tres`, select the wave, scroll to **Wave presentation (optional)**, and fill in Banner Title, Banner Description, Speaker, Narration, or Alert as desired. These are intentionally blank for now; the default numbered incoming-wave banner still appears. Click Save, then reload the mission to test. The older `Level001Waves.cs` mirrors the initial tutorial recipe as a reference but **is not loaded** by Level001 gameplay.

The F2 debug spawner remains separate from mission wave ownership.

## Terrain categories
Passable offers metal/dirt floors; Impassable offers walls/rocks; Interactive offers raised Foundation and Lava; Decorations offers only decals. Build permission is independent of movement: raised platforms are solid but buildable. Erase the old gameplay-layer cell before changing a cell from solid to passable or vice versa. Empty space stays passable/unbuildable. All 176 previously authored cells were preserved during the palette migration.

## Citadel durability

Open `Level001Config.tres` → **Destination / Citadel Max Health** to choose this level's starting maximum (**1,000 HP** by default). The Citadel is initialized at full health for each new level session. Enemy001 arrivals deal `EnemyDefinition.CitadelDamage = 1` each; the center HUD health number/bar updates through events. Killed ships deal no objective damage and still award their kill credits. A one-time `CitadelHealth.Destroyed` event is available at 0 HP, but an actual defeat screen and level end are a separate future pass.

## Enemy shields are deliberately reserved for later levels

The `basic_mk1` enemy can technically carry a shield in its shared enemy definition, but **Level001 never enables it in its wave entries**. Shields, stealth, heavy enemies and support ships are excluded so the first mission can focus on basic tower placement, target acquisition and the faster Scout introduction. These shared mechanics remain available to other maps.

## Interactive Terrain

Terrain/Interactive now exposes raised Foundation (source0 atlas1,0) and Lava (source1 atlas0,0). The74 saved Level001 platforms migrated without changing art/buildability/blocking; Template exposes the same palette. Lava is passable/nonbuildable and uses World/InteractiveTiles.LavaConfig for immediate entry plus periodic exposure damage. No Lava was arbitrarily painted into the current playable map. Use WorldGrid.RebuildTerrain after runtime edits; hazard-only changes avoid unnecessary navigation rebuilds. See WORLD/TERRAIN/Interactive/README.md and TESTS/Terrain/InteractiveTerrainMigration.md.
