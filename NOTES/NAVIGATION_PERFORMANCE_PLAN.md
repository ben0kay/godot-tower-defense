# Space Tower Defense — Navigation & Performance Plan (Godot 2D)

> Planning notes only; **not yet implemented**. Gameplay inspired by *Helix Defense 2*. Condensed from older GameMaker TD code and its optimization experiments. Read [CodePreferences.md](CodePreferences.md) before implementing; refer to the user's Isometric 2D/3D repositories for organization patterns where helpful.

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

## Initial Godot implementation: budgeted AStarGrid2D
1. **NavigationGrid:** compact cell state (walkable, blocked, buildable, occupied), spawn entries and goals; align it to the placement grid.
2. **PathRequestManager:** central queue with configurable **maximum path calculations per frame**; prioritize urgent requests. New ships request a path; moving ships **reuse** theirs until needed.
3. **Grid revision:** change navigation revision on tower placement/removal or obstacle destruction. Refresh affected routes; while waiting, ships must **stop safely** before newly blocked segments.
4. **Path caching:** reuse routes for compatible start/goal regions when beneficial; do not allocate one new route every frame.
5. **Movement:** follow waypoints with smooth facing, velocity and safe slight variation. Optional simple separation between ships.
6. **Invalid route:** wait/retry, or later attack blockers if designed; never pass through walls.
7. **Placement:** validate allowed footprint and occupancy; tentatively block cells, verify every required spawn-to-goal connection, then commit/reject and update navigation.

**Alternative to benchmark:** reverse Dijkstra/BFS **distance/flow fields** shared by ships headed to the same goal. Reading field directions is very cheap for swarms; rebuilding fields after layout changes has a cost. Compare actual CPU timings against cached/budgeted A*, especially for hundreds of ships and multiple destinations.

## Performance principles
- Start with lightweight **Node2D** ships; separate logical simulation data from visuals so rendering can be optimized later.
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
Follow `NOTES/CodePreferences.md`: clearly described scripts, `# =====` dividers and comments **above** functions, sensible regions, compact readable code, helper nodes instead of cluttered scene trees, grouped exports/settings, and a **MasterConfig** in major uppercase feature folders where appropriate. Prefer cohesive, simple systems; avoid overengineering. Keep existing notes authoritative when they provide specific feature context. **Do not modify game code as part of this documentation-only step.**
