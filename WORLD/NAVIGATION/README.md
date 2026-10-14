# Shared navigation — budgeted AStarGrid2D

`NavigationGrid.cs` adapts cached WorldGrid.BlocksEnemies into one AStarGrid2D. Empty space is traversable; painted walls, Citadel footprints and towers block according to WorldGrid. Diagonals use OnlyIfNoObstacles. Citadel ApproachPoints supply goal cells outside its blocked footprint.

`PathRequestManager.cs` queues at most one request per ship, tests reachable approach candidates and selects the shortest calculated route. Every actual A* calculation counts against EnemyMasterConfig.MaxPathCalculationsPerFrame (default 8), including alternate goal tests. Work is round-robin across requests. Stale queued results restart after grid changes; living ships stop safely while waiting.

Placement connectivity uses one temporary reverse flood fill per candidate build, preserving the same diagonal rules without running A* for every spawn source. Enemy movement checks swept radius clearance separately. Current navigation targets small ships and cell centers, not arbitrary large ship footprints.

Movement consumes waypoint arrays, so a later routing implementation can replace this adapter without rewriting enemy health or tower combat. No reverse Dijkstra/flow field is active yet. Inspect pending path count in the level Debug HUD; benchmark before changing algorithms or promising 500-ship performance.
