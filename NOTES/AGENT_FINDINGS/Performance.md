Read skill `analyzing-dotnet-performance`
Completed with input: {"skill":"analyzing-dotnet-performance"}

I’ll trace the frame/physics workload indicated by the screenshot—enemy updates, tower targeting/projectiles, pathfinding, and rendering—then prioritize anything that scales with 160+ enemies and many towers.


## Diagnosis

This looks **primarily CPU/physics-bound**, with 165 live enemies and multiple active towers—not primarily an A* problem. The screenshot reports **physics at 24.95 ms** and **process at 12.73 ms**. By comparison, navigation’s peak is 10.45 ms, its A* peak is 1.65 ms, and its route-audit peak is only 0.08 ms, with both queues empty. Those navigation peaks may cause occasional hitches, but don’t look like the sustained bottleneck in this snapshot.

### Most likely contributors

1. **Per-enemy movement work scales with every active enemy.** Each enemy has its own physics callback, and each moving enemy checks route corners and runs swept clearance sampling before moving. At a typical 60 Hz, 165 enemies means roughly 9,900 movement callbacks per second, before tower and projectile processing. See `EnemyMovement._PhysicsProcess` and `NavigationGrid.SegmentClear`.

2. **There’s a redundant spatial-index update on each move.** Movement explicitly calls `Registry.UpdatePosition` after setting the enemy’s position, but the transform notification already calls it. The extra call can also repeat position-change event work when listeners are active. See `EnemyMovement` and `Enemy._Notification`. This is the clearest small optimization I found.

3. **Wave 5 is deliberately a heavy stress test.** It schedules 500 ships, with the mixed-enemy batches spawning every 54–60 ms. That readily creates the 160+ active-enemy load you saw. Reducing batch counts or slowing their intervals is the most direct way to improve this level without changing simulation code. See `Level100Waves wave 5`.

4. **Towers add more physics callbacks and targeting work.** Tower target searches are periodic and use the spatial index rather than scanning every enemy, which is good. Still, many active towers add per-tick aiming/firing work and repeated local queries. See `TowerTargeting.Find` and the `BattleTower physics loop`.

5. **Rendering is worth checking, but is less clearly the main cause.** The screenshot shows 2,165 render objects and 369 draw calls. That’s substantial scene/render complexity, though the high reported physics time points more strongly to CPU simulation. Compare performance with effects and enemy visuals reduced to see whether rendering is also contributing.

### What I’d try first

- Remove the redundant registry update, then compare the same wave.
- For Level 100, slow or reduce wave spawns to test how much cost tracks active enemy count.
- If it remains slow, profile the enemy movement callback and clearance checks before weakening route-safety checks. Longer term, batching movement updates could reduce per-node callback overhead.
- Use Godot’s profiler to capture a steady section of the wave. The screenshot’s **16.4 ms frame average** does not line up with **27 FPS** (about 37 ms/frame), though the process and physics figures sum to roughly that slower rate; the sampling windows may differ.

**Targeted scan checklist:** Hot-path string/memory patterns: 9 matching lines; LINQ patterns: 0; regex patterns: 0; three sealed helper-class declarations found. I didn’t find LINQ or regex work in the reviewed per-frame movement/targeting paths.

I haven’t changed code; this is a static diagnosis from the screenshot and source, not a runtime profile. I’d treat the duplicated update as the first surgical fix, but the main performance lever is likely reducing or batching work across the active enemies.

| Area | Assessment |
|---|---|
| Enemy movement / physics callbacks | Most likely sustained cost |
| Duplicate spatial-index update | Clear, small optimization |
| Level 100 spawn rate and active count | Strong direct load lever |
| Navigation / route audits | Some peaks, but not the apparent sustained cause |
| Rendering | Secondary possibility to measure |

> These findings are based on a screenshot and code inspection; profiling on the same hardware and build is needed to confirm the exact cost breakdown.