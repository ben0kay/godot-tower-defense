# Per-enemy movement permissions — verification

Tested 2026-10-15 against GitHub base `339c9d4d71b24087be27361bd9bfa70b0d0f6655`, Godot 4.7.2 Mono / .NET 8.

## Results

- Direct Roslyn C# compilation with Godot source generators: passed, no diagnostics.
- `CorridorNavigationChecks.tscn`: exit 0. New MovementPermissionChecks covers 48 matrix cases (eight permission combinations, two master states, three percentages), mixed Enemy001/002/004 and a synthetic all-false definition, builder parity, stable repeated accepted routes, neutral path/cost oracle, crowd isolation, incremental obstruction/removal, different goals and 24px clearance. Existing geometry, dynamic repathing and all twelve 100/500 × straight/winding × 0/50/100% benchmark cases passed.
- `EnemyIntegrationChecks.tscn`: exit 0, including CorridorAssignmentChecks deterministic selection / wave RNG, SwarmNavigationChecks (with new alternative-corridor neutral oracle), RepathMovementChecks, SmoothRepathChecks, terrain, spatial queries, towers, ranks, waves, damage and navigation; final 31 integration checks passed. Rerun after adding the alternative-corridor assertion also passed.
- `PlacementPerformanceChecks.tscn`: exit 0, both populations passed.
- `git diff --check`: passed.

Normal `dotnet build --no-restore` could not start: this container's dotnet CLI threw System.ComponentModel.Win32Exception while retrieving process/thread information before compilation. Direct compiler/source-generator compilation and actual Godot headless runs passed. No manual graphical playtest is claimed. Suites emit the existing expected missing-Catalog fallback warning; no test errors occurred.

## Measured cost

These are headless CPU timings, with other suites running concurrently, not rendered FPS or a controlled before/after speedup claim.

| Fixture | Measurement |
| --- | --- |
| New 48-case permission fixture | 103.19 ms total, includes scene/resource creation and path assertions |
| 500 straight, 0% N4 | build mean/max 0.030/0.331 ms; movement-frame p95/max 5.02/9.20 ms |
| 500 straight, 100% N4 | build mean/max 1.719/4.940 ms; movement-frame p95/max 4.32/5.32 ms |
| 500 winding, 0% N4 | build mean/max 0.038/0.540 ms; movement-frame p95/max 4.66/5.02 ms |
| 500 winding, 100% N4 | build mean/max 1.915/6.329 ms; movement-frame p95/max 3.60/4.04 ms |
| Placement, 100 | validation max 4.40 ms; grid update max 1.40 ms; next physics max 5.29 ms |
| Placement, 500 | validation max 1.13 ms; grid update max 0.32 ms; next physics max 7.64 ms |

Permissions are cached at initialization and checked only at route construction/profile selection. Disabled corridors avoid the existing expensive N4 probes. The neutral profile adds one shared native A* grid and one unit-weight array, plus one solidity write per changed cell, independent of enemy count. There are no extra A* requests, per-enemy grids or per-frame permission scans. Existing hard calculation counts and soft time limits remain unchanged; individual route assignments can still exceed a soft limit.
