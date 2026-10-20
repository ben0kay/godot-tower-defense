## PF1 source change — 2026-10-20 (runtime verification pending)

User explicitly chose PF1 after the screenshot/static investigation; PF0 repeatable desktop timing remains pending and was not treated as a blocker to this narrow source change.

Removed only the explicit post-`SetMovementPosition` registry update in `EnemyMovement`. `Enemy.Initialize` enables local and global transform notifications; `Enemy._Notification` retains registry updates for movement, direct teleports and ancestor transform changes. The `_terrainMovement` early return occurs **after** the registry update, so terrain traversal semantics are preserved. No clearance checks, routes, waves, spawn counts, corridor settings or subscriber logic were changed.

The old explicit call repeated a registry lookup/native position read and could repeat `EnemyPositionChanged` dispatch. `EnemySpatialIndex.Move` already skips equal positions, so this is not a claim that two full bucket moves happened. Local/global notifications may both fire; PF1 does not suppress either or impose an exactly-one callback contract.

Added standalone `TESTS/Performance/MovementRegistryChecks.tscn`: synchronous notification-only queries/listeners for same-cell/cross-cell moves and teleports; real cached-route movement with clearance; active swept-wave history; live stealth beacon entry/exit; ancestor-transform propagation; arrival/free membership; and three alternating paired timing/callback runs emulating the old extra call. It reports raw timings rather than asserting a speedup.

**Execution status:** source review only. No usable execution environment was available for this job. C# build, Godot import, new fixture, existing integration/spatial/stealth/wavefront suites and before/after timings were **not run**. No FPS gain or completed runtime acceptance is claimed. PF1 remains pending runtime verification; stop here before PF2.

Required follow-up after C# build/import: run `TESTS/Performance/MovementRegistryChecks.tscn`, `TESTS/Performance/SpatialQueryBenchmarks.tscn`, `TESTS/Integration/EnemyIntegrationChecks.tscn`, `TESTS/Stealth/StealthChecks.tscn` and `TESTS/Navigation/CorridorNavigationChecks.tscn`. Record actual results, paired callback/timing output and comparable desktop Level100 measurements when available. If notification-only coverage fails, restore the explicit call rather than shipping stale spatial/reveal state.

