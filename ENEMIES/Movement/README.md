# Enemy Movement and Navigation (Future)

Enemies travel from the spawn positions chosen by `ENEMIES/Spawning` to **reachable approach cells around the Citadel**, not necessarily its solid center.

The logical terrain grid in `WORLD/TERRAIN/Rules/` will distinguish blocked cells from passable ones; deployed towers may add dynamic blockers on passable buildable cells. Reuse the centralized navigation approach documented in `NOTES/NAVIGATION_PERFORMANCE_PLAN.md`, rather than running full pathfinding per enemy each frame.

No navigation or enemy movement is implemented in this folder yet.
