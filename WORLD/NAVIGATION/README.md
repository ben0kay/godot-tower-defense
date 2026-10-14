# Shared navigation — budgeted AStarGrid2D

`NavigationGrid.cs` adapts cached WorldGrid.BlocksEnemies into one AStarGrid2D. Empty space is traversable; painted walls, Citadel footprints and towers block according to WorldGrid. Diagonals use OnlyIfNoObstacles. Citadel ApproachPoints supply goal cells outside its blocked footprint.

`PathRequestManager.cs` queues at most one request per ship, tests reachable approach candidates and selects the shortest calculated route. Every actual A* calculation counts against EnemyMasterConfig.MaxPathCalculationsPerFrame (default 8), including alternate goal tests. Work is round-robin across requests. Stale queued results restart after grid changes; living ships stop safely while waiting.

Placement connectivity uses one temporary reverse flood fill per candidate build, preserving the same diagonal rules without running A* for every spawn source. Enemy movement checks swept radius clearance separately. Current navigation targets small ships and cell centers, not arbitrary large ship footprints.

Movement consumes waypoint arrays, so a later routing implementation can replace this adapter without rewriting enemy health or tower combat. No reverse Dijkstra/flow field is active yet. Inspect pending path count in the level Debug HUD; benchmark before changing algorithms or promising 500-ship performance.

## Shared weighted route diversity

EnemyMasterConfig / ENEMIES/DefaultEnemyConfig.tres now exposes EnablePathDiversity (true), PathDiversityStrength (0.6), RouteProfileCount (4, capped at 8), PreferencePatchCells (8), and RoutePreferenceSeed. Ships receive a stable round-robin profile on spawn. The navigation service caches a small pool of AStarGrid2D instances and smooth seeded weight arrays, shared by all ships; it never allocates a navigation grid per ship. WorldGrid remains the sole authority for solids and NavigationRevision.

Weights are at least 1, keeping the octile heuristic admissible. Jumping is explicitly disabled because it bypasses weighted cells. Approach selection compares weighted costs; First tower targeting still compares the physical remaining route distance, not preference/congestion cost. Broad cost patches encourage alternate corridors without per-tile random jitter. Movement speed, smooth facing and swept obstacle checks remain unchanged. A single navigable corridor still produces shared routes; separation/waypoint offsets are not implemented.

CongestionInfluence defaults to 0 (off). When enabled, living-ship density is sampled every CongestionSnapshotSeconds (0.5), with capped costs in occupied/neighbor cells. Only sparse affected weights are refreshed per profile; old snapshots are restored before replacement. Profiles never borrow another profile's temporary state. New/repath requests consider snapshots; existing routes are not recalculated merely because crowds move. Earlier goal candidates are rescored against the current snapshot before comparison, without additional A*. Map changes invalidate solids and clear stale congestion. All actual A* searches remain within the configured frame budget.

SeparationStrength and SeparationRadius are reserved Inspector settings with no movement effect in this pass. A later separation implementation must use spatial buckets and swept obstacle clearance. Change configuration in the saved Resource before F5; runtime weight edits affect new/pending routes without forcibly repathing existing ships. Use terrain/occupancy revision changes to test repathing.

Tests include deterministic alternate corridors, swept clearance, ordinary paths with diversity disabled/zero strength, congestion/no weight leakage, 500 queued ships, budget limits and tower revision repathing. The headless timing output is a diagnostic, not a rendered 60 FPS guarantee; a synchronous 500-node spawn batch can still exceed a 16.7ms frame and future waves should spread spawn creation across frames.
