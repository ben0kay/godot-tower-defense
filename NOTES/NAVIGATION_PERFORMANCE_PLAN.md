# Space Tower Defense — Navigation & Performance Plan (Godot 2D / C#)

> Baseline budgeted AStarGrid2D is now implemented under WORLD/NAVIGATION; the scale/alternative-algorithm sections remain planning notes. Gameplay inspired by *Helix Defense 2*. Condensed from older GameMaker TD code and its optimization experiments. **Implementation language: C# (.NET), not GDScript.** Read [CodePreferences.md](CodePreferences.md) before implementing; refer to the user's Isometric 2D/3D repositories for organization patterns where helpful.

## Core game
- **Fully 2D, top-down** sci-fi tower defense. Alien ships attack objectives; no fixed road required.
- **Towers only on valid grid squares**; tower footprints, walls and asteroids can block enemy routes.
- Waves choose **portal spawns or any allowed map edge**; support multiple portals, destinations and wave-specific configurations.
- Ships navigate around obstacles, turn/move smoothly and retain small **safe path variation**; no rigid-body physics for each ship.
- Towers: separate base + rotating turret, **Marker2D** muzzle points for projectiles and effects, target priorities (first/last/closest/furthest/strongest/weakest), upgrades. Start with simple art.

## Lessons to retain from older GameMaker TD
| Existing technique | Godot adaptation |
| --- | --- |
| `mp_grid` pathing / blocked cells | Single authoritative grid holding navigation/building occupancy. |
| Random initial path timers, cooldowns, `needs_repath` | **Stagger path queries**; request new paths on genuine changes, not per frame. |
| Visibility `lazy_factor = 5`; staggered visibility checks | Update **noncritical** AI/VFX less frequently offscreen; never compromise obstacle correctness. |
| `path_shift` on smaller ships | Safe offset/steering variation that cannot cut corners through rocks or towers. |
| Cached static cost grid | Reuse static data **and invalidate/update** blocked cells when structures change. |
| Weighted A* and experimental flow field | Benchmark alternatives; **do not port both complex implementations by default**. |
| Target acquisition ~every 5 frames, cached target | Stagger tower searches; retain valid targets instead of scanning continuously. |
| Culling, restrained particles | Skip off-camera drawing/effects where possible; keep gameplay simulation running. |

### Legacy pitfalls to avoid
- Randomized delays alone **do not cap** peak pathfinding work when many ships need new routes.
- Failed pathfinding must **never** fall back to straight-line movement through obstacles.
- Prevent stale cached walkability when towers are placed, removed or destroyed.
- Check that tower placement **preserves required paths** from currently relevant portal/edge entries to their assigned goals.
- Path variation and diagonals must obey collision clearance and avoid corner cutting.

## Initial Godot C# implementation: budgeted AStarGrid2D
1. **NavigationGrid (C#):** compact cell state (walkable, blocked, buildable, occupied), spawn entries and goals; align it to the placement grid. Use typed arrays/enums for hot-path data where appropriate.
2. **PathRequestManager (C#):** central queue with configurable **maximum path calculations per frame**; prioritize urgent requests. New ships request a path; moving ships **reuse** theirs until needed. Avoid unnecessary allocations in frequent updates.
3. **Grid revision:** change navigation revision on tower placement/removal or obstacle destruction. Refresh affected routes; while waiting, ships must **stop safely** before newly blocked segments.
4. **Path caching:** reuse routes for compatible start/goal regions when beneficial; do not allocate one new route every frame.
5. **Movement:** follow waypoints with smooth facing, velocity and safe slight variation. Optional simple separation between ships.
6. **Invalid route:** wait/retry, or later attack blockers if designed; never pass through walls.
7. **Placement:** validate allowed footprint and occupancy; tentatively block cells, verify every required spawn-to-goal connection, then commit/reject and update navigation.

**Alternative to benchmark:** reverse Dijkstra/BFS **distance/flow fields** shared by ships headed to the same goal. Reading field directions is very cheap for swarms; rebuilding fields after layout changes has a cost. Compare actual CPU timings against cached/budgeted A*, especially for hundreds of ships and multiple destinations.

## Performance principles
- Start with lightweight **Node2D** ships implemented as Godot **C# partial classes**; separate logical simulation data from visuals so rendering can be optimized later.
- Spread targeting and other noncritical checks over frames with a **reusable staggered-check helper**.
- Use spatial buckets for nearby target queries **if** broad scans become a bottleneck.
- Avoid hundreds of active `RigidBody2D` objects, all-vs-all collision checks and unnecessary per-frame allocations.
- Cull distant VFX and throttle exhaust/explosion particles; pool objects **only when profiling justifies it**.
- Debug overlay: buildable/blocked cells, paths, navigation revision, request queue, path timings, active ships, FPS.
- **Benchmark goal, not a guarantee:** ~500 simultaneously moving enemy ships at 60 FPS on the development machine.

## Small implementation sequence
1. **Navigation prototype:** one grid arena, asteroid obstacles, portal/edge spawning, dynamic towers; test ~100–200 basic ships and show nav-debug overlay.
2. **Playable loop:** rotating turrets/Marker2D muzzles, basic weapons, enemy health, credits, waves, upgrades, win/lose.
3. **Profile & improve:** raise to 500 ships, measure spikes, test flow fields *only if needed*. Then polish artwork, particles and more maps.

## Project/code conventions
**C# (.NET) is the project language, not GDScript.** Follow `NOTES/CodePreferences.md`: a short header comment per `.cs` file, `// =====` dividers and comments **above** methods, `#region` where helpful, compact readable C#, helper nodes instead of cluttered scene trees, `[Export]` and `[ExportGroup]` for Inspector settings, and a **MasterConfig** C# class/resource in major uppercase feature folders where appropriate. Use strongly typed enums/data structures and reusable stagger helpers; avoid allocations in hot loops. Prefer cohesive, simple systems; avoid overengineering. Keep existing notes authoritative when they provide specific feature context. **Do not modify game code as part of this documentation-only step.**

## Implemented Enemy001 baseline

WorldGrid remains authoritative. NavigationGrid synchronizes solids on revisions; PathRequestManager budgets every actual A* goal search. EnemyMovement stops on stale/failed routes, follows radius-checked waypoints and caches remaining route distance. Level001/Debug owns F2 spawning and HUD. First targeting uses remaining route distance (not elapsed spawn time or straight-line goal distance); Closest remains selectable. Build clicks preserve currently usable spawn entries and active enemy connectivity. Tests cover 100 queued ships; no 500-ship FPS guarantee has been measured.

## Shared weighted route diversity

EnemyMasterConfig / ENEMIES/DefaultEnemyConfig.tres now exposes EnablePathDiversity (true), PathDiversityStrength (0.6), RouteProfileCount (4, capped at 8), PreferencePatchCells (8), and RoutePreferenceSeed. Ships receive a stable round-robin profile on spawn. The navigation service caches a small pool of AStarGrid2D instances and smooth seeded weight arrays, shared by all ships; it never allocates a navigation grid per ship. WorldGrid remains the sole authority for solids and NavigationRevision.

Weights are at least 1, keeping the octile heuristic admissible. Jumping is explicitly disabled because it bypasses weighted cells. Approach selection compares weighted costs; First tower targeting still compares the physical remaining route distance, not preference/congestion cost. Broad cost patches encourage alternate corridors without per-tile random jitter. Movement speed, smooth facing and swept obstacle checks remain unchanged. A single navigable corridor still produces shared routes; separation/waypoint offsets are not implemented.

CongestionInfluence defaults to 0 (off). When enabled, living-ship density is sampled every CongestionSnapshotSeconds (0.5), with capped costs in occupied/neighbor cells. Only sparse affected weights are refreshed per profile; old snapshots are restored before replacement. Profiles never borrow another profile's temporary state. New/repath requests consider snapshots; existing routes are not recalculated merely because crowds move. Earlier goal candidates are rescored against the current snapshot before comparison, without additional A*. Map changes invalidate solids and clear stale congestion. All actual A* searches remain within the configured frame budget.

SeparationStrength and SeparationRadius are reserved Inspector settings with no movement effect in this pass. A later separation implementation must use spatial buckets and swept obstacle clearance. Change configuration in the saved Resource before F5; runtime weight edits affect new/pending routes without forcibly repathing existing ships. Use terrain/occupancy revision changes to test repathing.

Tests include deterministic alternate corridors, swept clearance, ordinary paths with diversity disabled/zero strength, congestion/no weight leakage, 500 queued ships, budget limits and tower revision repathing. The headless timing output is a diagnostic, not a rendered 60 FPS guarantee; a synchronous 500-node spawn batch can still exceed a 16.7ms frame and future waves should spread spawn creation across frames.
