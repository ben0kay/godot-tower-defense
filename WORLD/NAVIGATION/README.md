# Shared navigation — weighted, incremental and budgeted

WorldGrid is the sole authority for terrain, footprints and tower occupancy. NavigationGrid caches a small pool of shared AStarGrid2D profiles, not one grid per enemy. Weighted preferences, optional congestion, corner-safe diagonals and Citadel approach goals remain active. Jumping is disabled because it bypasses point weights; weights stay at least 1 for the octile heuristic.

## Updates and route invalidation

WorldGrid.NavigationChanged reports one changed cell on tower placement/removal, or a full invalidation after RebuildTerrain. A tower updates that one solid flag in every shared profile immediately. Full map scans are reserved for startup, terrain reloads and profile configuration changes. FullRebuildCount and IncrementalCellUpdates expose which work actually ran.

EnemyRoute caches swept segment cell dependencies when assigning a path. PathRequestManager indexes ships by those cells. A new block marks only potentially affected remaining routes; cells behind a ship are ignored. Marking is cheap, and exact checks run in a separate bounded queue. Unaffected ships continue moving. A confirmed affected ship stops and requests a new route; immediate per-tick swept clearance also stops a ship safely if validation has not reached it yet. Removing a block leaves valid routes alone. Failed routes retry on the existing timer.

Full terrain changes queue audits, rather than stopping/repathing all ships immediately. Existing routes do not compare the global revision each physics tick. Pending A* candidates still restart on navigation/cost revisions to prevent accepting stale results.

## Scheduling and Inspector settings

Select ENEMIES/DefaultEnemyConfig.tres:

| Setting | Default | Meaning |
| --- | --- | --- |
| MaxPathCalculationsPerFrame | 8 | Maximum actual A* goal queries per physics frame |
| MaxPathfindingMillisecondsPerFrame | 2 ms | Optional soft budget for the entire path-work stage; 0 disables |
| MaxRouteValidationsPerFrame | 16 | Maximum cached-route validations per frame |
| MaxRouteValidationMillisecondsPerFrame | 0.5 ms | Optional soft validity-work budget; 0 disables |
| FairRequestEveryCalculations | 4 | Regular oldest-request service alongside distance-based urgency |
| RouteAuditIntervalSeconds | 3 s | Staggered lightweight audits; 0 disables periodic audits |
| EnablePathOffsets | true | Safe movement refinement without extra A* |
| PathOffsetMaxPixels | 8 px | Maximum stable per-spawn lateral bias |

Duplicate requests coalesce. Ships close to the blocking segment get higher path priority; regular oldest-request service prevents distant/spawn requests from starving. A native A* call and its path assignment cannot be interrupted, so time budgets may overrun by one work item. The count limit remains strict. Candidate approaches are compared by weighted cost; First targeting uses physical remaining refined-path distance.

## Route preferences and congestion

EnablePathDiversity, PathDiversityStrength (0.6), RouteProfileCount (4, capped at 8), PreferencePatchCells (8), and RoutePreferenceSeed configure smooth deterministic cost patterns. Ships receive a profile on spawn. Disable diversity for ordinary shortest paths. No algorithm can create another corridor where only one exists.

CongestionInfluence defaults to 0/off. Throttled snapshots of living ship density affect new/repath queries; existing routes are not periodically recalculated for crowds. Per-profile sparse weight caches restore old snapshot costs before replacing them. A* candidates are rescored consistently against the current snapshot. Separation settings remain reserved and inactive.

## Connectivity and diagnostics

Build clicks still reject sealed usable entries and trapped active ships. Connectivity uses reusable flat flood-fill buffers and cardinal links: these have the same connected components as diagonals allowed only when both side cells are walkable. It remains a synchronous correctness check, but does not rebuild A* profiles or allocate a queue/result array per click. ReachableWithout returns borrowed scratch data: copy it if it must survive another flood call. SpawnManager keeps a separate reusable cache.

Level001/Debug/HUD displays placement-validation, grid-update, native A*, total path-work and route-validation timings independently, plus work counts/queues. Tests and before/after measurements are in TESTS/README.md and TESTS/NavigationPerformance.md. These are headless CPU diagnostics, not a rendered 500-ship/60-FPS guarantee. Long individual searches/path assignments and synchronous mass spawning can still exceed a requested time budget.
