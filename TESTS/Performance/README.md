## Shared route pool

`SharedRoutePoolChecks.tscn` tests raw/finished reuse, safe connectors, invalidation, independent arrays, checkpoint chains and live100/500-ship blocked-route safety; it also records three paired300-assignment CPU runs. Results/limits: `SharedRoutePool.md`.

## PF1 — notification-owned movement updates (source implemented; unverified)

EnemyMovement no longer explicitly updates the registry after SetMovementPosition; Enemy's existing local/global transform notifications retain spatial indexing and reveal callbacks. Clearance, terrain traversal and swept history remain unchanged. See `TESTS/Performance/MovementRegistryChecks.tscn` and `NOTES/AGENT_FINDINGS/Performance.md`. Build, runtime regressions and timing measurements could not run because the execution environment was unavailable. No measured speedup is claimed.

# Performance tests

Standalone PlacementPerformanceChecks.tscn and SpatialQueryBenchmarks.tscn benchmarks, plus navigation/query timing reports. Headless CPU timings are not rendered FPS guarantees.

See [the test index](../README.md) for build/run commands and known suite limitations. This folder cleanup does not change test logic.
