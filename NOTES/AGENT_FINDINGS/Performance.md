## PF2 implementation — 2026-10-20

User approved PF2 and removal of the old grid Info overlay. Optional timing scopes now feed F4; Shift+F4 starts/stops, F5 prints/saves reports. Default off, fixed rolling physics samples, inclusive overlap labels and world/lifecycle guards. Old Info nodes/script removed; F2/F3 retained. Actual build/headless results and remaining desktop overhead/workload measurements: `TESTS/Debug/PF2.md`. This implements the measurement tooling, not a measured diagnosis or FPS gain. Earlier PF1-only stopping instructions below describe the prior job.

## PF1 source change — 2026-10-20 (runtime verification pending)

User explicitly chose PF1 after the screenshot/static investigation; PF0 repeatable desktop timing remains pending and was not treated as a blocker to this narrow source change.

Removed only the explicit post-`SetMovementPosition` registry update in `EnemyMovement`. `Enemy.Initialize` enables local and global transform notifications; `Enemy._Notification` retains registry updates for movement, direct teleports and ancestor transform changes. The `_terrainMovement` early return occurs **after** the registry update, so terrain traversal semantics are preserved. No clearance checks, routes, waves, spawn counts, corridor settings or subscriber logic were changed.

The old explicit call repeated a registry lookup/native position read and could repeat `EnemyPositionChanged` dispatch. `EnemySpatialIndex.Move` already skips equal positions, so this is not a claim that two full bucket moves happened. Local/global notifications may both fire; PF1 does not suppress either or impose an exactly-one callback contract.

Added standalone `TESTS/Performance/MovementRegistryChecks.tscn`: synchronous notification-only queries/listeners for same-cell/cross-cell moves and teleports; real cached-route movement with clearance; active swept-wave history; live stealth beacon entry/exit; ancestor-transform propagation; arrival/free membership; and three alternating paired timing/callback runs emulating the old extra call. It reports raw timings rather than asserting a speedup.

**Execution status:** source review only. No usable execution environment was available for this job. C# build, Godot import, new fixture, existing integration/spatial/stealth/wavefront suites and before/after timings were **not run**. No FPS gain or completed runtime acceptance is claimed. PF1 remains pending runtime verification; stop here before PF2.

Required follow-up after C# build/import: run `TESTS/Performance/MovementRegistryChecks.tscn`, `TESTS/Performance/SpatialQueryBenchmarks.tscn`, `TESTS/Integration/EnemyIntegrationChecks.tscn`, `TESTS/Stealth/StealthChecks.tscn` and `TESTS/Navigation/CorridorNavigationChecks.tscn`. Record actual results, paired callback/timing output and comparable desktop Level100 measurements when available. If notification-only coverage fails, restore the explicit call rather than shipping stale spatial/reveal state.

# Level 100 performance investigation — Codex handoff

**Status (2026-10-20): PF1 SOURCE CHANGE PREPARED; RUNTIME VERIFICATION PENDING.** PF1 source work is recorded below. The remaining sequence is proposed; runtime performance and regression acceptance have not been established. Preserve the original screenshot-based agent diagnosis under **Original agent findings**. Before coding, re-read the latest repository HEAD: some guidance can predate newer features.

## Intent and observed problem

Level 100 is an intentionally difficult **five-gate stress-test mission**, not a normal balanced level. Its five waves schedule 100 / 200 / 300 / 400 / 500 ships, including 20 armed Gunner Drones in each wave (see `LEVELS/Level100/README.md` and `Level100Waves.cs`). A desktop playtest during **wave 5** showed about **27 FPS** with **165 alive enemies**, despite relatively simple 2D artwork. The screenshot's F4 overlay reported **24.95 ms physics**, **12.73 ms process**, **10.45 ms path-work peak**, **1.65 ms A* peak**, **0.08 ms route-audit peak**, zero pending path/audit queues, 369 draw calls, and 2,165 render objects. These readings have different aggregation/window semantics, so do **not** add them to deduce exact frame cost or equate a short peak with an ongoing hot spot. The F4 frame average of ~16.4 ms versus 27 FPS is another reason to capture a reproducible trace before claiming a root cause.

**Working hypothesis:** CPU-side physics/simulation is under pressure; neither the snapshot nor a static scan proves which subsystem dominates. GPU load remains unmeasured. Aim to support larger swarms at useful frame rates **without nerfing the stress level, reducing enemy functionality, damaging route safety, or silently changing gameplay**. Do not promise a fixed FPS based on headless benchmarks.

## Existing optimizations — do not implement them twice

- `ENEMIES/Shared/EnemyRegistry.cs` already owns `EnemySpatialIndex`; `TowerTargeting.Find` queries nearby enemies through `VisitEnemiesInRadius`, instant `AreaDamage.ApplyInstant` uses `GetNeighborsInRadius`, and shockwave emitters use swept `GetWaveCandidates`. **These already exist**; do not add a parallel targeting/area index. Read `TOWERS/Offensive/Shared/README.md` and `TESTS/Performance/SpatialQueryBenchmarks.md`.
- Enemy routes are cached; central `WORLD/NAVIGATION/PathRequestManager.cs` budgets path requests and staggered route validation. There is no ordinary per-enemy A* search every physics tick. Pathfinding peak numbers can still matter during repaths, but do not start with an A* rewrite.
- `ENEMIES/Movement/EnemyMovement.cs` currently calls `Enemy.SetMovementPosition(nextPosition)` **and then** `Enemy.Registry.UpdatePosition(Enemy)`. `ENEMIES/Shared/Enemy.cs` already updates the registry in transform-change notifications, while `EnemyRegistry.UpdatePosition` dispatches `EnemyPositionChanged` to interested subscribers (e.g. the stealth reveal system). The additional explicit call is a **candidate redundant operation**, not a measured performance win yet; verify notification correctness before and after modifying it.
- `ENEMIES/DefaultEnemyConfig.tres` explicitly sets `EnableCorridorVariation = true` even though `EnemyMasterConfig.cs` defaults it false. Treat this as a configuration **A/B variable**, not permission to disable the shipped setting casually.
- Existing F4 metrics are implemented by `DEBUG/PerformanceOverlay.cs` and documented in `DEBUG/README.md`. Extend this rather than creating a second always-running overlay. Keep debug instrumentation off/cheap in normal play and release builds.

## Codex rules and scope

Read **root `README.md`**, **`NOTES/CodePreferences.md`**, this file, **`ENEMIES/ENEMYPERFORMANCE.md`**, **`TOWERS/Offensive/Shared/README.md`**, **`DEBUG/README.md`**, and the feature README of every touched folder. Follow the user's C# preferences: small modular changes, regions when useful, file header explanations, comments above methods preceded by `// =========================================================`, Inspector/export groups for truly tweakable values, performance-conscious code and minimal scene-tree clutter.

**Work one approved pass at a time.** Each pass: inspect current HEAD; modify only the necessary files; update related README/tests with what was really done; create **one atomic GitHub commit per approved job**. If tooling cannot run Godot/.NET or collect desktop timings, state *unverified* instead of inventing a pass or claiming an FPS gain. No gameplay code change is authorized by this documentation update alone.

## Implementation plan (ordered; stop after each pass)

### PF0 — Baseline and repeatable measurement (first, no gameplay changes)

**Goal:** establish a trustworthy before/after performance reference on the **same machine, resolution, zoom, render mode, build configuration and gameplay workload**.

1. Confirm Level 100's authored map, five portals, wave 5 configuration and actual tower layout used in the slow screenshot; record hardware, VSync/FPS-cap settings and debug vs release/editor mode. **Do not change wave counts/intervals** just to make graphs look better. Do not compare runs with materially different enemy/tower distributions as if equivalent.
2. Record steady gameplay measurements at representative **live enemy counts ~50, 100, 165, 300 and 500 when actually reachable**; report attempted/unavailable checkpoints honestly. Repeat at least 3 comparable runs where practical. Capture FPS, frame-time median/p95/peak, physics and process monitors, path/A* peaks + queues, enemy counts, tower counts, active attacks, draw calls, GPU 3D utilization and (when possible) main CPU-thread usage. Split quiet movement, active tower combat, gunner combat, and new-spawn/repath bursts into separate intervals. Use F4 + Godot Debugger Profiler/Visual Profiler and OS tools; rendering counters alone are **not** GPU utilization.
3. If reproducible desktop automation is unavailable, add a contained **debug-only** reproducible swarm scenario/benchmark to `TESTS/Performance/`, reusing existing services and not mutating saved Level 100 terrain or player profiles. Never use only headless throughput to establish desktop FPS.
4. Write an actual measurement table with **date, build/commit SHA, workload, sampling method and results** in this file or a `TESTS/Performance/` report. Label uncollected values as pending. **Acceptance:** a comparable baseline exists or specific blockers are documented. No performance claim yet.

### PF1 — Surgical duplicate movement registry-update investigation/fix

**Goal:** remove verified duplicate work while preserving movement, spatial queries and all subscribers.

1. Inspect `EnemyMovement._PhysicsProcess` (the `SetMovementPosition(...); Registry.UpdatePosition(...);` sequence), `Enemy.SetMovementPosition`, both transform notifications in `Enemy._Notification`, `EnemyRegistry.UpdatePosition`, and `EnemySpatialIndex.Move`. Check whether multiple transform notifications can already occur per logical move; distinguish repeated **event dispatch** from repeated **spatial bucket changes**.
2. Confirm with a targeted fixture that normal moving enemies are indexed and observable through notifications **without** the explicit post-move update. If validated, remove only that redundant call. Keep correct handling for teleports, direct position edits, scene transforms, entering/leaving cells, spatial queries, stealth-beacon reveal entry/exit, and active shockwave swept history. Do not replace the registry or suppress necessary notifications. If the assumption fails, retain/restore the call and document the reason.
3. Add regression assertions covering before/after positions, query results, listeners, same-cell moves, cell crossings, direct teleport, removed enemies and a live stealth subscriber. If there are already multiple notifications, measure and characterize them; **do not blindly require exactly one callback** if Godot legitimately emits both local and global transform notices.
4. Run relevant existing suites: `TESTS/Integration/EnemyIntegrationChecks.tscn`, `TESTS/Performance/SpatialQueryBenchmarks.tscn` (benchmarks are not correctness proof alone), and any dedicated stealth/movement/wavefront checks discovered at HEAD. Re-run PF0 workload if possible.
5. **Acceptance:** no route/combat/reveal correctness regression, evidence of removed duplicate work, and before/after timings reported (even if gain is small or negligible). **Stop:** do not bundle a large enemy movement refactor into PF1.

### PF2 — Lightweight, opt-in per-subsystem profiling

**Goal:** identify where the expensive physics time actually goes **before** optimizing behavior.

1. Add a contained debug-only, Inspector-configurable profiling mode owned by `DEBUG/`; reuse/extend `PerformanceOverlay` if useful. Normal F4 display and existing controls must continue working. Off must incur negligible extra overhead; avoid allocating strings/lists, doing scene searches, or creating Stopwatch objects **per enemy per tick**.
2. Aggregate sampling/counters (prefer bounded sampling or periodically timed batches) for **enemy movement / SegmentClear / transform+registry bookkeeping**, **friendly tower targeting / firing**, **hostile gunner acquisition/firing**, **projectiles and area/shockwave damage**, **pathfinding/refinement/repath**, and optional effects. If a category spans callbacks, document whether values are inclusive or overlapping. Do not report sums as an exact full-frame breakdown.
3. Display or dump mean/p95/peak, sample count, active entities, and instrumentation overhead for a bounded window. Separate physics CPU work from frame pacing and GPU rendering; Godot's built-in profiler does **not** inherently attribute individual C# methods.
4. Run PF0 comparison with diagnostics off versus on. **Acceptance:** actionable measurements and tolerably low profiler overhead. If a complete breakdown would distort performance, simplify instrumentation instead of building a second expensive simulator.

### PF3 — Controlled diagnostics and bottleneck selection

**Goal:** verify root cause with **temporary comparisons**, not permanent content changes.

1. Repeat comparable runs with the authored `EnableCorridorVariation` setting on/off, noting that disabling it changes path appearance/assignment. Restore the authored value afterward unless the user explicitly approves a gameplay/design change.
2. Isolate enemy-only movement (few/no firing towers) versus tower-heavy combat at similar alive counts; compare gunner fire on/off and expensive feedback visuals in **temporary debug/test configurations only**. Avoid claiming identical workloads if removing towers increases the live enemy population.
3. Profile how costs change as counts rise and identify one **measured** top contributor. If path queues are empty and path work is near zero in steady frames, postpone queue-data-structure rewrites. If GPU time/3D utilization is high, profile draw/shader/particle effects rather than assuming CPU.
4. **Acceptance:** ranked bottleneck list supported by numbers and a recommended narrow fix. Obtain user approval before substantial architecture or gameplay tradeoffs.

### PF4 — One targeted optimization at a time (conditional; not pre-approved)

**Only implement the highest measured source of cost:**
- **Movement/clearance:** investigate caching or cheaper equivalent lookups while retaining `CornerBlocked`, `SegmentClear`, enemy-size clearance, dynamic placement blockers and safe repaths. Never remove/skip swept movement validation merely to save time. A shared/batched movement scheduler is a **later architectural experiment only if per-node overhead is demonstrated**, with explicit lifecycle/pause/collision tests.
- **Tower/enemy targeting:** tune excessive query scheduling/cached lock checks if profiling warrants it; keep the already implemented `EnemySpatialIndex`, priority rules, stealth eligibility and ordinary firing cadence.
- **Effects/projectiles:** profile actual emitter/particle/draw cost first; optimize batching/pooling or visual update cadence without changing hit timing or damage.
- **Path bursts:** only if queue length, route-refinement time or request selection is demonstrably limiting; preserve urgency/fairness, bounded budgets, safe-prefix movement and deterministic routes.
- **Rendering:** if render/GPU timing proves it, prioritize visible effect/node/render-state costs; retain artwork appearance for manual review or propose a separate visual-quality option.
  
Each small follow-on PF4 job gets its own baseline, tests, before/after results and **single commit**. Do not fold five speculative micro-optimizations into one unmeasurable change.

### PF5 — Final regression and evidence

Run headless/integration regressions that exist at HEAD, including `TESTS/Integration/EnemyIntegrationChecks.tscn`, `TESTS/Integration/Level100StressChecks.tscn`, `TESTS/Navigation/CorridorNavigationChecks.tscn`, `TESTS/Debug/PerformanceOverlayChecks.tscn` (if instrumentation changed it), and affected specialized suites. Validate desktop Level 100 wave 5 separately with comparable enemy counts and towers; capture FPS/frame percentiles, physics/process timing, route safety, gameplay parity, and GPU/visual notes. Document each run's real result, any skipped tests and remaining bottlenecks. A pass is only **complete** when its code, relevant tests/docs and truthful execution status are recorded.

## Pass tracker

| Pass | Status | Outcome/evidence |
|---|---|---|
| PF0 — Baseline | NOT STARTED | Screenshot is a clue, not a controlled benchmark |
| PF1 — Registry duplicate fix | SOURCE IMPLEMENTED / VERIFICATION PENDING | Explicit post-move call removed; notification/consumer fixture added; build/runtime/timings unavailable |
| PF2 — Instrumentation | IMPLEMENTED; TARGET-WORKLOAD MEASUREMENTS PENDING | F4 optional subsystem timings/report; see DEBUG/README.md and TESTS/Debug/PF2.md |
| PF3 — Isolate bottleneck | NOT STARTED | Compare matched workloads/settings; no shipped balance changes |
| PF4 — Measured optimization | BLOCKED ON PF2/PF3 | Choose one evidence-backed improvement |
| PF5 — Verification | NOT STARTED | No measured FPS gain claimed |

---

## Original agent findings (preserved from pushed diagnosis)

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
