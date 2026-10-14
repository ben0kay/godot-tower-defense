# Level 001 — Enemy and Tower Combat Playtest

`Level001.tscn` is loaded by `WORLD/World.tscn` when you run the game (F5). It is a saved, hand-editable TileMapLayer layout, not generated during gameplay. Placeholder artwork is deliberately replaceable.

## Test and adjust

1. Pull the latest commit, let Godot import assets, and build the C# project.
2. Run **F5** to play the main game. The camera starts over the central base. WASD/arrows pan, middle mouse drags, wheel zooms; Ctrl toggles the tower tray.
3. Choose Battle Tower and try the different terrain tiles. Green previews allow building; red previews reject it. Empty space and unbuildable walls reject towers. Buildable open floors and solid platforms accept them. The debug overlay reports current cell rules and occupancy.
4. Stop the game, open **Level001.tscn**, select `Terrain/Ground` or `Terrain/Walls`, and paint using the shared TileSet palette. Save, then press F5 again. F6 runs only the map scene, without the gameplay camera/HUD/placement controller.
5. Select `Terrain/Decorations` for cosmetic tiles only. Erase ground tiles to expose open space. Walls override Ground; Decorations never change cell rules.
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

Paint decals on Decorations. A decal alone never grants build permission. The atlas is shared by all three layers and other levels; changing tile metadata affects every level using it.

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

## Manual finite waves
F5 prepares Wave 00 / 03; START WAVE appears just below the top-right status. Three Enemy001-only test waves are authored in LEVELS/Level001/Level001Waves.cs. Previous batch ships keep moving during later batches. NEXT WAVE unlocks only after all scheduled and wave-owned ships resolve, while F2 debug enemies do not count. Ordinary ships randomly choose eligible registered portals per spawn; debug F2 stays round-robin. There is no wave reward or automatic next wave yet.
