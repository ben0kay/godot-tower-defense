# Shared proximity optimization — implemented

**Completed 2026-10-15.** This folder remains documentation only. Production helpers stay with their existing owners; no tower-specific branches, physics collision areas, parallel registries or per-tower indexes were introduced.

## Shared world index

`ENEMIES/Shared/EnemyRegistry.cs` owns one `EnemySpatialIndex`. `GetEnemiesInRadius(center, radius, results)` provides the GameMaker-style circular-list operation: nearby grid cells first, exact inclusive center-distance checks second. Results use the caller's list and have no cap; order is unspecified. Zero radius includes the exact center. Negative/nonfinite radius or center throws before modifying the result list; nonfinite enemy positions are excluded until moved back to finite coordinates.

The registry's Inspector **Spatial Queries / Spatial Cell Size** defaults to **128 world pixels**, measured with the paired fixtures below. Set it before the first registration/query; it is a setup value, not a live weapon or balance stat. Position buckets update from actual enemy transform notifications (including direct teleports), with an explicit update in the existing movement path. Membership changes only on cell transitions. Spawn/death/arrival/free and world unload preserve authoritative Living/Contains behavior. Negative coordinates use floor. Large queries visit occupied cells instead of allocating huge coordinate grids.

## Conventional targeting

`TowerTargeting.Find` uses a stack-owned visitor over nearby enemies; no per-search result list or native position reread. First still compares cached remaining path distance, Closest still compares the original float squared distance. Invalid/infinite First paths cannot win. Explicit registration order preserves earlier-living-entry ties despite unordered bucket storage. A conservative broad-phase boundary covers float rounding; the original range comparison makes the final decision. Battle/Laser/Blaster acquisition staggering, current-target validation, aiming, beams and cooldowns are unchanged. Shockwave Tower still emits waves without acquiring a single target.

## Instant Blaster areas

`AreaDamage.ApplyInstant` uses the same index and cached position/order snapshots. It orders recipients by registration and snapshots every scaled hit before dispatch. Membership/native lifetime is rechecked immediately before applying each hit. Pooled, cleared scratch lists are independent during nested/reentrant impacts and retain no enemy references between uses. Damage type, shield/hull overrides, penetration, XP source, linear falloff, inclusive boundary and the single instant application on impact remain unchanged. No separate primary-target hit was added.

## Moving shockwaves

Each emission acquires an O(1) revision cursor instead of scanning/seeding the world. The same spatial index retains movement samples only while needed by active cursors. It indexes conservative swept position envelopes alongside its point buckets and rejects cells outside the current swept annulus or wholly behind the old radius. Query results provide positions at that wave's previous sample and now; the unchanged quadratic `Crossing` solver, terminal-tick interpolation, contact-distance falloff and hit-once set make the actual damage decision.

Histories cover net sampled chords across multiple position writes, fast outward/inward movement and teleports; there is no assumed maximum speed. New enemies retain the previous stationary-at-first-observation rule. Each wave advances independently. Cleanup is amortized per recorded movement change, retaining samples only past the slowest active cursor; there is no per-wave scan over Living or per-frame world rebuild. Completed/unloaded emissions release their cursors.

Trade-off: a motion envelope spanning more than 64 cells uses a small conservative overflow set rather than allocating an enormous grid rectangle. Those exceptional movers must be considered by each active wave until its sample is consumed. Very large queries or all-nearby clusters necessarily approach global work; correctness takes precedence over inventing a movement-speed limit. Sub-tick motion remains the original straight interpolation between wave samples. Visual rings never decide damage.

## Verification and measured performance

`SpatialQueryChecks` compares 0/1/500/1,000-enemy circles and First/Closest against brute-force scans, including boundaries, negative/extreme positions, invalid input, movement, ties, stale paths, registration/removal and isolated-world teardown. It compares sparse blast shield/hull outputs and nested snapshots, and runs original-vs-indexed moving wave fixtures at 30/60/120 Hz with late spawns, extreme teleports, multiple writes, slower overlapping cursors and terminal overshoot. Existing combat, ranks, beams, Blaster, purple Shockwave Tower, menus, purchases and navigation suites remain active.

`TESTS/Performance/SpatialQueryBenchmarks.tscn` retains the original targeting/blast scans and test-only `BruteForceShockwave` for same-machine paired measurements. It covers 500/1,000 enemies × 50/100/200 acquisition calls, sparse/dense groups, eight impacts and eight independent waves, including movement maintenance and allocation counts. See [full measurements and reproducible workload](../../../TESTS/Performance/SpatialQueryBenchmarks.md).

Observed average total sparse-query work improved **2.72–6.82×**, with dense totals **1.03–1.20×** faster in this run. For1,000 enemies/200 tower acquisitions: sparse average **19.895→2.919ms**, p95 **29.050→3.500ms**, examined candidates **215,733→4,337** per measured tick; dense average **20.736→17.889ms**. These are headless acquisition-burst/delivery measurements, not rendered FPS guarantees. Some dense tail samples were noisier despite lower mean times. The measured all-enemies-teleport2,000,000px/tick stress case is slower with conservative overflow handling; all crossings remain correct. Full results include this limit and maintenance overhead.

No level rows, prices, rank/XP rules, damage coefficients, navigation algorithms, scene visuals, research, saving or other features were changed.
