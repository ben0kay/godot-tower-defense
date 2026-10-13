# WORLD / GRID

A lightweight top-down **visual grid**. Initial defaults: **64-pixel cells**, **100 x 80** cells (6,400 x 5,120 world units).

`WorldGrid.cs` converts positions to cell indices, shares map bounds, and draws only visible lines with one Node2D. `GridDebugOverlay.cs` reports the hovered cell and camera metrics.

**Important:** No cells are currently marked buildable/blocked and no navigation is implemented. The later placement/navigation systems must store actual buildability and occupancy; this visual overlay is not pathfinding data.

Tweak all initial map and grid settings in `WORLD/DefaultWorldConfig.tres`. Navigation details: `NOTES/NAVIGATION_PERFORMANCE_PLAN.md`.
