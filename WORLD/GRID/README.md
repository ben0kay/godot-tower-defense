## PF2 debug update (2026-10-20)

Optional subsystem timing hooks feed the existing F4 panel (Shift+F4 toggles profiling; F8 saves a report). Old GridDebugOverlay/Info labels are retired; F2/F3 remain. See `DEBUG/README.md` for inclusive timing semantics, fixed sample storage and validation details. Historical notes below remain as reference.

## Portal/Citadel editor grid snapping — 2026-10-18 (implemented)

Portal and Citadel remain reusable scenes. Shared `[Tool] WorldObjectFootprint` now snaps their roots to the enclosing map's `Terrain/Passable` cell centers when entering the editor tree or moving/duplicating them. Default tiles give centers `32 + 64*n`. It uses the actual TileMapLayer conversion, including tile size and transforms, rather than hardcoding 64px or enabling the editor's global snap. The Citadel root still anchors the upper-left cell of its 3×3 footprint, not its visual center.

**Usage:** build C# once, open a Level scene and drag/duplicate a Portal or Citadel normally. Leave **Editor Placement → Snap To Grid In Editor** enabled (default). Disable per instance for deliberate free placement; enabling it again snaps immediately. Existing off-grid instances align when opened in the editor; save the level to retain the corrected positions. No authored map coordinates or tile cells were rewritten by this implementation. Standalone Portal/Citadel scenes have no enclosing map and keep their original local origin.

Only the root position changes. SpawnExit/ApproachPoints local offsets, footprint size, portal SizeMultiplier, route links, rotation and scale are untouched. No snapping executes during gameplay and no process/physics loop is added; editor transform events are coalesced into deferred corrections. A custom map must expose `Terrain/Passable` with a valid TileSet under an ancestor, matching the existing map structure. Moving a root without such a layer leaves it free rather than assuming an incorrect grid.

Verification: C# build, headless actual-tool editor and runtime checks cover both real scenes, negative coordinates, opt-out/re-enable, 80px tiles, transformed parents/maps, duplication, retained child offsets and standalone origins. See `TESTS/World/EditorGridSnap.md`. Existing planning/history below is retained.

---

# WORLD / GRID

`WorldGrid.cs` is the single cached gameplay grid plus a transparent, viewport-limited debug overlay. Default dimensions remain 100×80 cells at 64 px, configured in `WORLD/DefaultWorldConfig.tres`.

It reads the live `World/Level` Ground and Walls layers on startup, applies object footprints, and stores build permission, static blockage, reservations and runtime tower occupancy in compact arrays. `CanBuild`, `BlocksEnemies`, `IsOccupied`, `PlacementFailure`, `TryOccupy` and `ReleaseOccupancy` expose shared queries/mutations. `RebuildTerrain` refreshes static rules while retaining occupancy. Future selling/destruction must release occupancy and remove the corresponding placement-controller tower reference.

`WorldObjectFootprint.cs` reserves authored Portal/Citadel cells. Its root is the upper-left footprint cell center; SizeCells and BlocksEnemies are Inspector-editable.

`GridDebugOverlay.cs`, now mounted under Level001/Debug/HUD, reports cell rules, occupancy, camera metrics and navigation revision. `NavigationRevision` invalidates WORLD/NAVIGATION routes and spawn connectivity caches. Background rendering belongs to the level, keeping grid lines/hover overlays transparent.

`NavigationChanged` retains single-cell blocked/unblocked and null/full terrain notifications. Multi-cell claims publish `NavigationBatchChanged(Rect2I, bool)` only after all cells change, incrementing NavigationRevision once. Shared navigation consumes batches incrementally, not as terrain rebuilds. Terrain-blocked cells gain occupancy without events/revisions. Rectangle validation uses TerrainBlocksEnemies for homogeneous category checks, with exact anchor/size claims guarding atomic release. FootprintCenter respects the grid transform and the top-left-anchor convention; static WorldObjectFootprint reservations retain their separate authored convention. See `TOWERS/Placement/MULTI_CELL_FOOTPRINT_PASS.md`.
