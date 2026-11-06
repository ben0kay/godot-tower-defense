## Shared weapon terrain vision — 2026-11-06

**Source Implemented / Awaiting Owner Validation.** Mountains now block enemy weapon acquisition, aiming and firing as well as friendly tower targeting. `WorldGrid.HasWeaponLineOfSight(from, to)` is the neutral shared API, using the existing allocation-free cached terrain traversal. `HasTowerLineOfSight` remains a compatibility wrapper; the existing `blocks_tower_sight` tile metadata, blocker count/cache, corner rules and no-blocker fast path are preserved. Friendly target queries and the selected red/visible range overlay call the neutral API; no second visibility cache, terrain scan, physics ray system or global fog system.

Enemy `TowerRegistry` exposes its already-cached owning grid. `EnemyTowerTargeting.Valid` and `Closest` use that same terrain test, with an optional sight origin independent of range origin. Enemy mounts pass their **own muzzle** for sight while retaining pivot-based range/closest scoring and stable registration-order ties. Only candidate score contenders traverse terrain, so a hidden nearest tower cannot suppress a visible fallback. Existing staggered acquisition and lock-hold intervals remain. A newly obstructed lock is cleared before aiming/firing; if aiming/countering hull rotation moves an aligned muzzle, sight is revalidated before emission. The redundant identical pre-aim lock check was removed. All four Capital guns and both Gunner variants inherit this behavior through `EnemyWeaponMount`; no per-ship branches or new processing loops.

Projectile flight/collision, already-emitted bullets, damage, splash/pulses, repair, stealth rules, navigation, tile flags, weapon balance and saves are unchanged. This pass concerns weapon line of sight only.

**Minimal validation:** focused source/diff checks only; .NET unavailable. Existing MountainSightChecks was extended with neutral/legacy API parity, reverse blockage, hostile hidden-target rejection/visible fallback, independent offset muzzle visibility, and a real Heavy Gunner mount refusing shots until terrain removal. C# checks are authored but **not executed** here; no build, gameplay or performance claim. Owner: rebuild, place a mountain between a tower and Gunner/Capital, verify neither side targets/fires through it, move around the mountain and inspect each Capital muzzle independently. Already-fired projectile collision remains unchanged by design.


---

## PW1 independent power terrain facts — 2026-11-04

WorldGrid now caches optional Boolean `conducts_power` alongside its existing terrain facts, using Passable → Interactive → Impassable precedence. Current gameplay palettes explicitly mark solid land true and Lava false; Decorations and empty space never conduct. Custom palettes missing the field default to conducting painted land; a present field of the wrong type rejects setup. When adding the field to a custom TileSet, author every desired conducting tile true (Godot's Boolean default is false). Build permission, enemy blockage, reservations and tower occupancy remain independent.

`TerrainConductsPower(cell)` is a constant-time query. `PowerTerrainRevision` changes only when the effective conductivity mask changes; `TerrainRebuilt` still publishes normal explicit rebuild completion. Existing navigation revision/events retain their original rules. The new world-owned PowerGridManager listens to that event and skips identical masks. Runtime paint/erase or conductivity edits require `RebuildTerrain`; dimensions still require restart. See [power plan](../../TOWERS/Economy/Power/IMPLEMENTATION_PLAN.md). No per-frame terrain work or power-driven navigation work was added.

---

## PF2 debug update (2026-10-20)

Optional subsystem timing hooks feed the existing F4 panel (Shift+F4 toggles profiling; F5 saves a report). Old GridDebugOverlay/Info labels are retired; F2/F3 remain. See `DEBUG/README.md` for inclusive timing semantics, fixed sample storage and validation details. Historical notes below remain as reference.

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
