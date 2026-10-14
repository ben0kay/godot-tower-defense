# WORLD / GRID

`WorldGrid.cs` is the single cached gameplay grid plus a transparent, viewport-limited debug overlay. Default dimensions remain 100×80 cells at 64 px, configured in `WORLD/DefaultWorldConfig.tres`.

It reads the live `World/Level` Ground and Walls layers on startup, applies object footprints, and stores build permission, static blockage, reservations and runtime tower occupancy in compact arrays. `CanBuild`, `BlocksEnemies`, `IsOccupied`, `PlacementFailure`, `TryOccupy` and `ReleaseOccupancy` expose shared queries/mutations. `RebuildTerrain` refreshes static rules while retaining occupancy. Future selling/destruction must release occupancy and remove the corresponding placement-controller tower reference.

`WorldObjectFootprint.cs` reserves authored Portal/Citadel cells. Its root is the upper-left footprint cell center; SizeCells and BlocksEnemies are Inspector-editable.

`GridDebugOverlay.cs`, now mounted under Level001/Debug/HUD, reports cell rules, occupancy, camera metrics and navigation revision. `NavigationRevision` invalidates WORLD/NAVIGATION routes and spawn connectivity caches. Background rendering belongs to the level, keeping grid lines/hover overlays transparent.

NavigationChanged now emits a single changed cell and its blocked state on occupancy changes, or null/full on terrain rebuild. NavigationRevision is still maintained. WORLD/NAVIGATION consumes those notifications incrementally; EnemyMovement no longer stops solely because the global revision changed. The level Debug readout includes separate placement/grid/path/validation timings.
