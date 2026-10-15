# Proposed optimization pass — shared proximity queries for towers and area attacks

**Status: PLANNED / NOT IMPLEMENTED.** This is a single, focused Codex implementation pass. The folder `TOWERS/Offensive/Shared/` currently serves **documentation only**; do **not** move generic C# targeting, enemy-registry or damage logic here. Put reusable code in its existing owning folders. Do not confuse this document with the already implemented `TOWERS/Shared/`.

## Objective

Optimize gameplay for **500–1,000+ living enemies, many placed towers, and overlapping area attacks**, without changing target selection, damage, enemy movement, timing, visuals or tower stats.

Right now:
- `TOWERS/Shared/Targeting/TowerTargeting.Find` scans `EnemyRegistry.Living` for each periodic acquisition by **Battle Tower, Laser Tower and Blaster Cannon**, then rejects faraway enemies.
- `COMBAT/AreaDamage/AreaDamage.ApplyInstant` scans the entire living registry for **each Blaster splash impact**.
- `TOWERS/Shared/Weapons/Shockwaves/ShockwaveEmitter` scans the full living registry **each physics tick for every active wave**, and currently seeds per-emission positions from the entire registry.
- The **Shockwave Tower** emits area waves without normal single-target acquisition; it must not be forced into `TowerTargeting.Find`.

This pass must fix **both recurring tower-target scans AND area-attack scans** through **one shared spatial proximity index**, not two competing systems.

## 1. Implement one world-scoped spatial index behind EnemyRegistry

Proposed ownership / files:

```text
ENEMIES/Shared/
  EnemyRegistry.cs          # existing authoritative membership and living list
  EnemySpatialIndex.cs      # NEW internal reusable spatial hash / uniform grid
  README.md                 # update after implementation
TOWERS/Shared/Targeting/
  TowerTargeting.cs         # use nearby range query instead of full-registry scan
  README.md                 # update after implementation
COMBAT/AreaDamage/
  AreaDamage.cs             # instant explosion nearby-enemy query
  README.md                 # update after implementation
TOWERS/Shared/Weapons/Shockwaves/
  ShockwaveEmitter.cs       # spatially limited swept wavefront candidates
  README.md                 # update after implementation
TESTS/
  [new proximity/index and performance test files as appropriate]
```

Provide an ergonomic **GameMaker `collision_circle_list`-style** API through `EnemyRegistry`, for example:
`GetEnemiesInRadius(Vector2 center, float radius, List<Enemy> results)`.
The caller requests **only nearby living enemies within its exact circle**; the registry performs **broad-phase** lookup using nearby spatial cells and **narrow-phase** squared-distance checks. The API shape can differ if it improves safety or allocation behavior; no TowerId conditionals.

**Index implementation guidelines:**
- Prefer a **lightweight spatial hash/uniform grid** over adding `Area2D` / `CollisionShape2D` instances to all existing lightweight `Node2D` enemies. Treat an engine physics-query approach as an alternative only if comparative measurements justify the extra physics representation.
- Keep `EnemyRegistry.Living` and `Contains` compatible for unrelated systems. `EnemyRegistry` is authoritative for spawn/death/arrival/scene unload, and owns one spatial index **per world** (never a process-global singleton).
- Insert on registration, remove immediately on death/arrival/external removal, update positions during the **existing enemy movement path** and any relevant teleport/reposition paths. Do not scan all enemies every frame merely to rebuild/update the index. Update cell memberships only when necessary. Handle negative positions, cell boundaries, duplicates and large queries.
- Choose a tunable, measured cell size (initial experiment around **128 world pixels**, not a tower-balance constant); keep setup understandable and allocations low. Queries must handle radii larger than a cell, including towers that upgrade their range.
- Keep queries valid and deterministic during enemy removals, respawns and world teardown. Reuse caller-provided buffers where suitable; avoid per-tower/per-tick new collections and hidden query result caps. Ensure zero-radius, non-finite/range-invalid parameters have an explicit tested policy.
- Because an unordered spatial hash can change iteration order, **preserve existing First/Closest targeting results, including deterministic tie-breaking** (current `<` comparison favors the earlier living-registry entry). Use a stable registration-order tie-breaker or otherwise demonstrate exact parity without reverting to full-registry scans.

## 2. Replace full-registry scans in conventional tower targeting

Refactor **only the shared** `TowerTargeting.Find()` to obtain in-range candidates via `EnemyRegistry` spatial query.

Retain:
- Existing public targeting API when practical; Battle, Laser and Blaster should not each implement new targeting loops.
- `First` means minimum *remaining path distance*, `Closest` means minimum actual squared tower distance; invalid/stale paths stay ineligible under First.
- Current per-tower staggered acquisition timers (typically **0.15 seconds**), target switching and cached-target range/liveness checks between searches.
- Per-tower effective `FinalStats.RangePixels`, aiming, cooldowns, projectile behavior, beam lock/lifecycle, exact range boundary inclusion.

The purpose is to avoid **examining distant enemies** in each tower scan. Do not change fire rate, AI priorities or coverage.

## 3. Apply the SAME index to instant splash attacks

Change `COMBAT/AreaDamage/AreaDamage.ApplyInstant` to get only enemies within the impact circle using the shared registry query; no full living-registry enumeration per explosion.

Preserve **exactly**:
- Weapon-authored radius and edge-damage fraction / linear falloff; no new shared hardcoded falloff constants.
- Immutable `DamageInfo`, damage type and shield/hull coefficient overrides, penetration and damage attribution/source.
- Inclusive center-distance boundary, zero-radius rule, one hit per eligible enemy including the primary, no separate direct-hit double damage.
- **Snapshot qualifying recipients before calling `Enemy.ApplyDamage`**, because the first kill can mutate the registry during dispatch. Recheck validity/membership just before applying, with no skipped surviving targets or duplicate rewards.
- Projectile travel, visual shockwaves, event/reward lifecycle unchanged.

## 4. Optimize *timed* expanding Shockwave Tower damage safely

`ShockwaveEmitter` must no longer scan `EnemyRegistry.Living` every physics tick per emission (or seed every emission by scanning every living enemy).

However, **this is NOT a normal circle of enemies at the current radius**. It is a moving wavefront with swept crossing semantics:
- Keep the existing `previousRadius -> currentRadius`, precise `Crossing(...)` relative-motion solver, wave-relative encounter distance/falloff, and per-emission **hit-once** set.
- Query **spatially limited broad-phase candidates for the annulus swept by the wavefront** during the tick, with conservative inclusion for **enemy motion between samples**. An enemy that crosses the front between frames must not be missed just because its *final* position is outside the ring/current circle.
- Implement robust motion-aware broad-phase indexing or conservative swept-position coverage and per-wave tracking only where needed; do **not** fake correctness by scanning every enemy globally or introducing an arbitrarily small movement-speed assumption.
- Ensure enemies that enter the region later, cross at cell boundaries, move inward/outward rapidly, spawn/die during a wave, or overlap multiple emissions work correctly. Different active waves each retain independent hit-once bookkeeping.
- Preserve damage occurring **only when the advancing front reaches the enemy**, not continuous full-disc damage. Visual `FEEDBACK/Shockwave` is presentation only and must not change timing/damage.
- If a chosen data structure cannot maintain motion-aware correctness while limiting candidates, explain and benchmark the trade-off rather than silently weakening the current solver. Favor preserving exact behavior over nominal benchmark gains.

## 5. Correctness tests and practical benchmarks

Before optimization, record comparable baseline measurements. After implementation, test and report:
- **Circle-query parity** versus a brute-force oracle for 0/1/500/1,000 enemies, various tower and explosion radii, center/edge/just-outside, empty cells, negative coordinates, long-range/upgraded towers, rapid movement and cleanup.
- **Targeting parity** for First/Closest, equal-score ties, invalid/infinite remaining routes, staggered searches, beam retargeting and source-removal scenarios.
- **Area blast parity** for multiple survivors, mid-dispatch deaths, no duplicate hit/reward, radius/falloff/shields, penetration/source.
- **Shockwave parity** versus the old accurate swept solver: stationary and moving targets, 30/60/120 Hz, very large delta, rapid enemy movement crossing between ticks, cell boundaries, overlapping waves, new/dead/unloaded enemies, end-of-life overshoot and no duplicate damage.
- Performance under representative **500 / 1,000 enemies**, **50 / 100 / 200 targeted towers**, sparse and dense enemy clustering, multiple simultaneous splash explosions and shockwaves. Report maintenance time, query time, candidates tested versus global count, active emission counts, allocations if measurable, and average / p95 / maximum simulation times. Compare before and after **on the same fixture/machine**.
- Check that adding the spatial index does not create net regressions in dense all-enemies-nearby cases. **Do not claim rendered 60 FPS from headless benchmarks.** Identify remaining rendering bottlenecks separately.

Compile the Godot .NET project and run existing headless gameplay, tower, combat, wave, movement, navigation and menu tests plus the new spatial-query tests. Record real pass/failure information. No performance claims without measured evidence.

## 6. Scope fence / no feature creep

This is **one optimization pass only**:
- Do NOT implement new towers, targeting priorities, research, XP/ranks, new damage rules, visual effects or scene redesign.
- Do NOT rewrite A*, route request queues, corridor variation defaults, spawn balancing, tower stat pipeline, explicit `BattleTowerLevels.cs`/other level rows, wallet/economy or saved-game behavior. Those are separate follow-ups.
- Do NOT create parallel registries or per-tower spatial indexes. Make only the smallest necessary calls from existing owners and keep the index reusable for future enemy abilities/attacks.
- Follow `NOTES/CodePreferences.md`: organized small files, explanatory headers, method separators, clear regions, readable and performance-conscious C#. Update the related READMEs from this plan to factual implementation once tests run.

**Finish condition:** conventional towers process nearby enemies only; Blaster impacts consider nearby candidates only; active Shockwave emissions examine only relevant spatial candidates per tick **without losing swept crossing correctness**. All gameplay outcomes stay identical, with measured overhead and real regression-test results.

## One-message instruction to Codex

> Implement the single optimization pass defined in `TOWERS/Offensive/Shared/README.md` against latest main. Fix BOTH tower targeting scans and Blaster/Shockwave area-attack scans via one lightweight `EnemyRegistry` spatial index / range-query API. Preserve current target priorities/timers, blast damage, source attribution and swept moving-wavefront correctness. Add parity tests and before/after 500–1,000 enemy + 50–200 tower combat benchmarks, preserve navigation/level architecture, compile, run headless suites, update READMEs, commit and push. Report actual results and any trade-offs.
