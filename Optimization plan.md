# Optimization plan

> **Canonical optimization register for `ben0kay/godot-tower-defense` · branch `dev` · consolidated 2026-11-10.**
>
> **Use this file first** to see whether an optimization is already implemented, still proposed, or duplicated by another name. Detailed design files and historical performance reports remain linked below but are **not separate competing backlogs**. A code commit means *source implemented*; it does **not** guarantee an FPS improvement or successful Godot gameplay validation. No gameplay code is changed by this consolidation.

## 1. Pass register — one identifier per job

| Canonical pass / name | Status at `dev` HEAD | What it includes / duplicate resolution | Owning evidence |
| --- | --- | --- | --- |
| **EG2** — Hostile shot buffer | **Source implemented** | Dense array-backed hostile bullets and one renderer; no per-bullet nodes. This is **not** a new projectile-pooling job. | [Enemy projectiles](ENEMIES/Projectiles/README.md); commit `f9071111cf46` |
| **FP-BUFFER** — Friendly projectile buffer | **Source implemented** | Reusable shared friendly flight/draw buffer (default 4,096) for regular tower/fighter tracers, with node fallback on overflow; not a blanket promise for splash/wave emitters. | [Friendly projectiles](TOWERS/Shared/Projectiles/README.md); `852d20478c40` |
| **PF1 / spatial queries** | **Source implemented** | Removed a redundant explicit movement-to-registry update; spatial buckets already serve targeting, swept projectile contact and AoE. Do **not** add a second spatial index. | [Performance findings](NOTES/AGENT_FINDINGS/Performance.md), [spatial results](TESTS/Performance/SpatialQueryBenchmarks.md) |
| **PF2 / diagnostics** | **Source implemented** | Shared subsystem profiler plus newer **F2 overlay**, **Shift+F2 profile** and **F3 report**; recent frame percentiles/native rendering counters, managed heap/GC, path soft-budget frequency. Benchmark observer overhead and GPU availability separately. | [Debug README](DEBUG/README.md); `d1270fed45` |
| **NAV-CACHE** — route reuse and segment safety | **Source implemented** | Bounded shared path/route pools and conservative certified-segment safety cache. Path budgets are **soft**, not hard preemption. Do not reimplement generic collective route sharing. | [Navigation README](WORLD/NAVIGATION/README.md); [perf findings](NOTES/AGENT_FINDINGS/Performance.md) |
| **FOG-PERF1** — concealed-enemy visual work | **Source implemented; desktop validation pending** | Shared staggered reveal cohorts (~30 Hz per moving enemy), coalesced radar contact redraws, hidden thruster/exhaust suspension, selective hull-rotation work. **PERF-D's original main objective is already here.** | [Lighting README](SYSTEMS/Lighting/README.md); `e78cb0cb62` |
| **P2** — concealed shields and bars | **Source implemented; desktop validation pending** | Hidden shield quads/bars and cached defense-bar layout. **Do not implement PERF-D a second time for these visuals.** | [Defense bars](FEEDBACK/DefenseBars/README.md); `1de88b122d` |
| **P4-A (formerly PERF-A)** — projectile viewport culling | **Source implemented; desktop validation pending** | **Both friendly and hostile shared tracers already cull** complete viewport bounds; native standalone Basic/Splash culling and offscreen/concealed projectile light leases also exist. The prior recommendation to add hostile culling was outdated. | [Friendly projectiles](TOWERS/Shared/Projectiles/README.md), `ENEMIES/Projectiles/EnemyBulletRenderer.cs`; `6f5509d05d` |
| **P4-B** — textured-tracer comparison | **Source implemented, optional; desktop comparison pending** | Bounded 64-style cached textured-tracer path, original style remains default, permanent right-side visual toggle. **Do not separately create a new tracer-mode system.** | [Weapon visuals](TOWERS/Shared/Weapons/Visuals/README.md); `14ed3357ed` |
| **LOS-PERF1 (formerly PERF-C)** — cached terrain sight fast path | **Planned; not implemented** | Already has **automatic zero-sight-blocker map bypass**. Add a conservative per-terrain-rebuild blocker-region prefix table so sight lines that cannot intersect mountains skip exact DDA. **Preserve LOS correctness every lock tick, including beams.** | [Detailed Codex plan](WORLD/GRID/LOS_PERF1_IMPLEMENTATION_PLAN.md) |
| **TOWER-IDLE (formerly PERF-B)** — sleep idle weapons | **Proposed; not implemented** | Consider event-driven no-enemy idle sleep and safe wake for Battle/shared projectile turrets and other compatible towers. Existing disable gating is **not** the same as no-enemy sleep. Requires lifecycle/cooldown checks and desktop profiling. | Section 2B below |
| **FOG-FOLLOWUP (formerly residual PERF-D)** | **Conditional; do not redo FOG-PERF1/P2** | Only pursue additional hidden VFX / GPU rendering dormancy after matched diagnostics show work still persists. | Section 2C below |
| **SPLASH-BUFFER (formerly PERF-E)** | **Deferred / profile first** | SplashProjectile still has independent flight nodes; consider shared-state adaptation only if node count or callback time is measured high. Shockwave gameplay stays separate. | Section 2E below |
| **HULL-BATCH** — repeated hull presentation | **Deferred / prototype only after GPU evidence** | Shares imported textures already; optional MultiMesh/batched hull rendering must preserve per-ship layers, rotation, tint, damage stages, visual scale and fog. | Section 2D below |
| **PATH-BURSTS / PF3** — target-machine bottleneck isolation | **Measurement pending** | Profile 5–8 ms path-work spikes, navigation queue/clearance, tower targeting, projectiles, effects and GPU render time in matched desktop scenes. Optimize one verified bottleneck at a time. | [Enemy navigation](ENEMIES/ENEMYPERFORMANCE.md), [perf findings](NOTES/AGENT_FINDINGS/Performance.md) |

**Previous informal aliases:** PERF-A → **P4-A** (already implemented); PERF-B → **TOWER-IDLE**; PERF-C → **LOS-PERF1**; PERF-D → **FOG-PERF1 + P2** (existing), with only conditional **FOG-FOLLOWUP** remaining; PERF-E → **SPLASH-BUFFER**. They are **not five additional jobs to be run again**.

## 2. Ordered remaining work — practical Codex handoffs

**Order is a recommendation, not authorization to implement every row.** First capture the same scenario with existing tools (section 3), then select one focused change. No sweeping renderer rewrites.

### 2A. LOS-PERF1 — safe terrain sight broadphase (next small self-contained pass)

**Status:** Planned. **Source owner:** `WORLD/GRID/WorldGrid.cs` + `WorldGridSight.cs`; existing detailed [LOS-PERF1 plan](WORLD/GRID/LOS_PERF1_IMPLEMENTATION_PLAN.md).

- `WorldGrid.RebuildTerrain()` **already** caches `TowerSightBlockerCount` from Boolean `blocks_tower_sight`. `HasWeaponLineOfSight` already returns true without tile traversal when this count is zero. No new "no mountains" map-scan toggle should be added.
- For maps with sight blockers, precompute a conservative *summed-area table* of `_towerSightBlocked` once at explicit terrain rebuild. If a padded segment bounding rectangle contains no blockers, skip DDA; if it contains any, keep the current exact supercover traversal.
- Retain mountain/hostile muzzle/corner checks, live target visibility, off-map semantics, moving target checks and continuous beams. Do **not** defer LOS to only when cooldown ends; towers must not see a blocked enemy while waiting to fire.
- Cost to establish: static build/rebuild work and memory versus avoided per-tick DDA on the actual map. This is not expected to change FPS much on blocker-free maps because those were already fast.

### 2B. TOWER-IDLE / PERF-B — no-enemy weapon sleep

**Status:** Proposed, not yet a stand-alone implementation plan. **Owners to inspect:** `TOWERS/Offensive/BattleTower/BattleTower.cs`, `TOWERS/Shared/Weapons/MultiBarrel/ProjectileTurretBase.cs`, `TOWERS/Offensive/LaserTower/LaserTower.cs`, `ENEMIES/Shared/EnemyRegistry.cs`, existing `TowerBase.SetOperationalPhysicsProcess`.

- Several active weapon scripts continue a physics callback with no valid enemies; their searches are staggered and use spatial queries, but a large empty battlefield can still do many idle tower callbacks.
- **Only if profiling says Towers CPU remains significant**, introduce a lightweight *world-roster-level* wake/sleep notification for compatible offensive weapons. Stop unnecessary target acquisition/aiming loops while there are zero viable live enemies, without per-frame polling or scanning every tower on each tick.
- Wake on the first relevant enemy registration, reload/Continue, completion of construction, re-enable or power recovery, and transitions that actually change an attacker's eligibility. Treat no enemy in range differently from **no enemies in the world**: do not sleep a weapon forever just because its initial local search missed.
- Preserve firing cadence, accumulated/paused cooldown, automatic power/heat/idle drain, ammunition funding, immediate target loss, continuous beam stop/restart, target-priority changes, status weapons, stealth detection, event unsubscribe and scene teardown.
- Avoid touching noncombat production, drone logistics or savings. If the machinery to coordinate every weapon becomes complex or gains are tiny, **defer this pass** rather than add another scheduler.

### 2C. FOG-FOLLOWUP / PERF-D — only remaining measurable cosmetic waste

**Status:** Conditional after **FOG-PERF1 and P2 already landed**.

- Current code already hides fully concealed enemy thrusters/exhaust, shield quads and bars; reveal checks are staggered and dark contacts use a shared SVG/culling path.
- Compare fully hidden / mixed / visible enemy swarms with equivalent zoom and lighting settings. Measure actual GPU exhaust count, draw submissions, CPU and available GPU timing.
- Only then consider extra hidden cosmetic component dormancy, source-specific draw suppression or per-style visual grouping. Keep radar contacts, combat reveal authority, projectile damage, enemy AI and visible appearance unchanged.
- **Do not implement** an independent Fortress Defense `EnemyVisibilityCoordinator` on top of the existing Tower Defense lighting/reveal coordinator. Reuse its existing notifications and `Enemy.SetFogConcealed`.

### 2D. HULL-BATCH — repeated ship art, rendering bottleneck experiment

**Status:** Deferred. Tower Defense already shares imported textures; identical textures do not automatically combine all draw submissions. A new batching layer is only justified by a controlled GPU/render-object comparison.

- Pilot on one very common simple enemy hull; preserve individual positions, rotations, appearance variants, visual scale, damage textures and light-reveal/fog material.
- Keep separate weapon mounts, layered capital ships, damage overlays and hit/UI presentation initially out of scope.
- Consider Godot 2D batch-compatible presentation or MultiMesh **only after** source-order, fog and normal-zoom correctness can be preserved. Do not collapse ships into one gameplay target or stop their simulation offscreen.

### 2E. SPLASH-BUFFER / PERF-E — complex projectile nodes (low priority)

**Status:** Deferred. Standard friendly/hostile bullets are already world-buffered. `TOWERS/Shared/Projectiles/SplashProjectile.cs` remains node-based; shockwave damage uses a separate shared area mechanic.

- Measure actual active SplashProjectile count, callbacks, node creation/free time and physics cost before doing anything. If counts are small, keep the current simple node lifecycle.
- Only if a hotspot, adapt splash flight to a bounded shared owner with **identical** travel, target-loss, impact AoE, light leases, visuals, pause, cleanup and exactly-once damage. Keep specialized shockwave propagation separate.
- Do not pool every weapon kind or merge friendly/hostile systems simply to reduce class count.

### 2F. PATH-BURSTS / PF3 — pathing and movement measurement

**Status:** Existing caches are implemented; target-machine performance work remains.

- Use profiler path-work peak, pending requests, validation queue, A* peak, over-budget ticks and `Clearance`/Movement CPU rather than relying solely on red warnings. The **2 ms path-work budget is soft**: a single non-interruptible A* or route-refinement step can exceed it.
- Compare steady moving fleet versus placement/mass-repath burst. Corridor variation should be compared at 0%, 25%, 50%, 100% if large route-build costs appear; preserve collision-safe movement.
- Bounded shared route pooling and segment-safety caching already exist. Do not re-add "collective brain" route sharing, duplicate registry updates or per-enemy A*.
- Only pursue a new data structure, clearance cache change, queue scheduler or route algorithm if the profile isolates cost, and keep path correctness for multiple Citadels, portal checkpoints, blocked foundations and large ships.
- Detailed historical reports live in [Navigation findings](NOTES/AGENT_FINDINGS/Performance.md), [Navigation plan](NOTES/NAVIGATION_PERFORMANCE_PLAN.md) and [Enemy navigation review](ENEMIES/ENEMYPERFORMANCE.md). These are reference material, not separately auto-approved changes.

### 2G. OTHER CANDIDATES — evidence required

- **Lighting/VFX:** capped native lights, grouped tower glows, projectile-light leases, shared radar contacts, pooled hull sparks and bounded shield ricochets are already in source. Investigate remaining render/GPU/particle cost **before** adding more pools. Use `SYSTEMS/Lighting/README.md`, `FEEDBACK/` and the current F6 lighting comparison.
- **Hostile projectile overflow:** `EnemyBulletSystem` has a fixed maximum and reports `Overflow`; it may reject a new hostile shot when full. This is primarily a gameplay correctness/capacity policy, **not** automatically an FPS improvement. Profile actual overflow first; only then design a bounded fallback preserving weapon funding/damage.
- **Targeting/LOS measurement:** the `Targeting` profiler category covers acquisition; repeated `TowerTargeting.Valid` checks also occur inside tower physics and may be charged under `Towers`. Do not infer LOS's exact time from a Targeting row alone. If it becomes the bottleneck, add narrowly scoped counters or paired timing before an additional cadence change.
- **New salvage/refinery/building logistics:** retain bounded drones/cargo/processing, avoid per-building polling and duplicate inventories; treat this as normal new-feature perf review rather than a speculative global optimizer.

## 3. Measurement protocol — prove which pass helps

1. Use the **same** branch/build, level, camera/zoom, renderer, resolution, live enemy mix, tower mix, waves and lighting state. Compare repeated steady-state samples and spike windows separately.
2. **F2** shows the performance panel; **Shift+F2** starts/stops detailed subsystem profiling; **F3** saves the report. **F1 is not required.** F6 compares lighting and fog presentation. Check current game keys, not historical F4/F5 documentation.
3. Record live enemies, towers, buffered **friendly/hostile** shots, standalone projectile nodes, draw calls, render objects, active GPU exhaust, lights, frame mean/p95/p99 and engine physics/process. Native CPU/GPU render time can be N/A depending on renderer; do not label missing readings as zero.
4. Measure with overlay and native render timing instrumentation **on versus off** when investigating small improvements; retain profiling overhead disclaimers.
5. Sample at least these representative cases when feasible: (a) lots of towers and zero enemies, (b) moving fleet with towers idle, (c) sustained friendly + hostile volleys, (d) mostly concealed and fully revealed fleets, (e) sparse mountain and blocker-free maps, (f) mass repath after placement.
6. For each implemented pass, preserve enemy movement/pathing, terrain LOS both ways, projectile collision/damage, power and ammunition funding, fog transitions, pause and Save/Continue. Source checks are not gameplay QA; report tests not run honestly.
7. Implement only **one measured change at a time** in `dev`, with a source/build check per `CodePreferences.md`. Record the owner-run before/after profile in this file, update its pass status, and link the commit. No blanket "x% faster" claim without matched numbers.

## 4. Dense-fleet baseline (retained original evidence)

In the Level003 captures, the 319-enemy view showed 163 FPS, 355 draw calls and 2,561 render objects. The laggy 296-enemy view showed 57 FPS, 537 draw calls and 3,594 render objects. Movement p95 was similar at 7.19 versus 7.35 ms per sampled physics tick. The slow view had 29 buffered friendly shots and zero hostile shots. The owner reports similar FPS with lighting on and off after the radar-dot fix. These observations prioritize rendering and effects investigation. GPU timing and individual effect costs were unmeasured.

These screenshots are **different scenes/views**, not matched before/after benchmark runs. The relative FPS and draw differences suggest investigating visual effects/render work but do not prove a particular GPU or CPU bottleneck. Your machine/renderer's actual GPU timing was not captured in the baseline.

## 5. Existing visual implementation notes and verification (preserved)

The following subsections preserve the earlier root plan's measurements and acceptance checklists. Their original "implemented in source" statements are **not** desktop performance guarantees. Do not interpret the older speculative thruster or P4 suggestions as outstanding tasks: use the canonical register in section 1.

### P2 verification

Owner screenshot before P2, with FOG-PERF1 in use and mixed visibility: 314 enemies, 35 towers, 75 FPS, 20.4 ms displayed frame time, 31.1 ms peak, 7.46 ms engine process, 13.73 ms physics, 630 draw calls and 3,873 render objects. The owner reports that this run rarely fell below 60 FPS. This single view is a reference, not a matched comparison. Post-P2 runtime measurements are pending. No batch prototype was added because individual-bar overhead has not been isolated.

Use F2 for the current Performance overlay, Shift+F2 to profile, and F3 to save the report. For P2, compare 200–500 ships in fully concealed, fully revealed and mixed combat views. For a separate shield rendering comparison, temporarily suppress only the aura quad in a local profiling build and leave shield state and bars active.

### Verification

Use the same level, camera, zoom, resolution and comparable fleet mix. Change one visual layer at a time. Record frame time and spikes alongside live enemies, draw calls and render objects. Use the rendering profiler for GPU measurements where available. If attribution is still unclear, add counters for active exhaust emitters, on-screen fog contacts, visible shield effects and defense bars.

Check fog transitions, camera edges and crowded combat after each change. Preserve enemy count, pathing, targeting, projectile collisions and damage. Keep [navigation work](NOTES/NAVIGATION_PERFORMANCE_PLAN.md) separate from these rendering passes.

### P4-A verification

Source implementation retains tracer artwork and the shared RevealEntities shader. Shared renderers reject complete screen-space footprints, with a two-pixel antialiasing margin. Standalone drawing stays cached for native CanvasItem culling, using the same tracer footprint plus a conservative zoom margin. Fog remains a pixel shader operation for bullets. Only the capped light pool tests reveal-disk overlap.

No crowded-battle FPS or GPU comparison was practical without the Godot .NET runtime. Compare the same scene with dense friendly and hostile fire, fully offscreen shots, tails/glows/tips crossing every edge, fog crossings and F6, and camera pan/zoom/rotation. Include buffered overflow or UseBufferedTracers=false and Splash rounds. Record F2 / Shift+F2 / F3 reports with draw calls and render objects. Native standalone command caching is retained. Common hull batching and simplified tracer rendering remain separate work.

### P4-B verification

The permanent debug panel at middle-right switches tracer rendering live. Original rendering remains the default. Compare both modes at the same camera/zoom with dense friendly and hostile volleys, then disable UseBufferedTracers to exercise BasicProjectile fallback. Check switching with rounds in flight and while paused, texture colors and dimensions, tapered shots, fog edges, F6, and viewport clipping. Account for the first bake/upload of each style before recording steady-state results. Resize the window and open/close F1 to check the independent panel.

No matched runtime FPS or GPU measurements are available in the current environment. The 64-style cache is bounded and preserves original-renderer fallback for additional styles. The textured mode remains optional pending owner comparison.

### Overlay diagnostics

Use F2 for the 600-frame p95/p99 distribution, latest native viewport CPU/GPU rendering time, managed heap/GC totals and existing visual count/toggle snapshots. F3 now saves the diagnostic context with or without retained Shift+F2 samples. Native GPU availability depends on the renderer; missing readings are N/A. MeasureRendering on PerformanceOverlay controls timestamp instrumentation while the overlay is open. Compare measurement enabled/disabled if investigating small differences. No matched hardware FPS measurements have been recorded for this diagnostic update.

The path warning now shows its soft budget and over-budget tick count beside a separate nested A* peak. With the default 2 ms budget, a 5–8 ms path-work peak turns red. Use the frequency and frame distribution to distinguish occasional work spikes from sustained pressure.


## 6. References and backlog ownership

- **This root file:** current canonical IDs, implemented/planned status, duplicates, priorities and next measurement.
- **[LOS-PERF1 detailed plan](WORLD/GRID/LOS_PERF1_IMPLEMENTATION_PLAN.md):** ready-to-implement shared WorldGrid change, **not** already in gameplay code.
- **[World Grid](WORLD/GRID/README.md):** authoritative terrain facts and current no-blocker sight fast path.
- **[Tower targeting](TOWERS/Shared/Targeting/README.md) / [Enemy projectiles](ENEMIES/Projectiles/README.md) / [Friendly projectiles](TOWERS/Shared/Projectiles/README.md):** implemented combat ownership.
- **[Lighting](SYSTEMS/Lighting/README.md) / [Defense bars](FEEDBACK/DefenseBars/README.md):** implemented fog/VFX behaviours.
- **[Debug tools](DEBUG/README.md):** current FPS, path warnings, projectiles, optional native timings, and report shortcuts.
- **[Navigation review](ENEMIES/ENEMYPERFORMANCE.md) / [Navigation performance findings](NOTES/AGENT_FINDINGS/Performance.md) / [Navigation plan](NOTES/NAVIGATION_PERFORMANCE_PLAN.md):** historical measurements and algorithm proposals. Retained for technical context, **not** a second active execution queue.
- **Fortress Defense comparison:** its planned shared projectile migration is substantially similar to Tower Defense's **already implemented** buffered friendlies/hostiles. Its enemy visibility coordination is a design reference only; Tower Defense already has its own FOG-PERF1 reveal owner. No cross-repository source changes authorized.

### Maintenance rule for Codex and future plans

Before starting any performance task, inspect **this register, current `dev` source and the relevant owning README**. If a proposed pass aliases an already implemented one, **do not duplicate it**. Update the single register row and its link/status when a change lands. A separate implementation specification may remain for technical detail, but its status must not contradict this register. No agent should silently optimize gameplay correctness away.
