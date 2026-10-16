# Enemy Navigation Performance Review

## Summary

The navigation system already avoids per-enemy A* grids and per-frame pathfinding. Moving enemies follow cached routes; path searches and route validation are centralized and budgeted. The clearest measured cost to tune is optional corridor variation (N4) during route assignment, rather than steady-state movement.

All timings below are headless CPU measurements from the repository's fixtures. They are not rendered frame-time or target-hardware guarantees.

## Prioritized Improvements

### 1. Tune corridor variation before changing the navigation architecture

N4 adds bounded clearance sampling when a selected enemy receives a route. In the straight and winding test maps, selected route builds added roughly 1.9-2.1 ms per ship on average. At 500 ships and 100% selection, path-work frame maxima reached 13.2 ms. The configured 2 ms path-work budget is soft: an individual native search and route build cannot be interrupted, so a single request can exceed it before yielding.

The master toggle currently defaults off. Keep it off when route spread is not important; otherwise test a lower selection percentage (for example, 25-50%) before enabling it for every enemy. If route-build cost remains material, increase `CorridorVariationSpacingCells` or reduce variation strength. Both reduce route-sampling work at the cost of less frequent or smaller lateral deviations.

Implementation: [CorridorRouteRefinement.cs](Movement/CorridorRouteRefinement.cs), [EnemyRoute.cs](Movement/EnemyRoute.cs), and [EnemyMasterConfig.cs](EnemyMasterConfig.cs).

### 2. Measure repath bursts separately from normal movement

The centralized path manager limits the number of searches per physics frame and uses a soft time budget. A tower placement or another broad navigation change can enqueue many replacement paths; N4 refinement adds work to each accepted route and can increase the time until all affected enemies receive new routes. Profile these bursts separately from an unchanged-route movement frame before raising path budgets. Raising the count can reduce queue latency while increasing per-frame spikes.

The manager also scans the pending request list to choose the next request. This is simple and bounded in normal use, but could become a candidate for a priority-queue redesign only if profiling shows large pending queues make selection expensive. Mutable urgency and fairness make a heap more complex, so this should not be a speculative refactor.

Implementation: [PathRequestManager.cs](../WORLD/NAVIGATION/PathRequestManager.cs).

### 3. Verify and remove the duplicate spatial-index update if useful

Movement calls `Enemy.SetMovementPosition`, which changes the transform. `Enemy` listens for transform changes and updates its registry spatial index; movement then explicitly calls `Enemy.Registry.UpdatePosition` as well. The index returns early when the position is unchanged, so the second call is likely just an extra lookup rather than a full bucket move. Removing the explicit call may save a small amount of per-step work, but measure it first and retain transform-notification coverage for direct repositioning and teleports.

Implementation: [EnemyMovement.cs](Movement/EnemyMovement.cs), [Enemy.cs](Shared/Enemy.cs), and [EnemySpatialIndex.cs](Shared/EnemySpatialIndex.cs).

### 4. Preserve clearance checks unless profiling proves they dominate

Each moving step checks swept segment clearance, and this protects against ships entering newly blocked terrain while validation and replacement-path work are pending. Route corner checks use cached data; route audits and A* are not performed every movement tick. These checks are safety-critical, so avoid reducing their sampling or frequency without tests for newly blocked cells, narrow corridors, diagonal corners, and larger enemy radii.

Implementation: [EnemyMovement.cs](Movement/EnemyMovement.cs) and [NavigationGrid.cs](../WORLD/NAVIGATION/NavigationGrid.cs).

## Recorded Measurements

The corridor fixture measured normal movement-frame maxima around 1.6-2.1 ms at 500 enemies. With 0% corridor variation, build means were about 0.03-0.07 ms per ship; with 100% selection, means were about 1.9-2.1 ms. Path-frame maxima at 500 enemies and 100% selection were about 13.2 ms in both straight and winding maps. There were also isolated outliers in unselected route-build batches, so these figures are samples, not hard bounds.

See [CorridorPerformance.md](../TESTS/Performance/CorridorPerformance.md) for the full 100/500-enemy, 0/50/100% results and test conditions. The broader placement and selective-repath timings are in [NavigationPerformance.md](../TESTS/Performance/NavigationPerformance.md).

## Suggested Next Profiling Pass

1. Compare 0%, 25%, 50%, and 100% corridor variation on the target machine with rendering enabled.
2. Record steady-state movement frame times separately from a mass-repath event, including p95 and maximum path-work time and pending queue length.
3. If steady movement is material, profile clearance checks, transform notifications, and registry updates before changing their behavior.
4. Keep safety and repath regression suites passing when evaluating any clearance or movement optimization.

The current headless tests establish correctness and CPU behavior for their fixtures; they do not establish a rendered 500-enemy/60-FPS guarantee. A moving-swarm visual and performance check in the Godot editor remains necessary.
