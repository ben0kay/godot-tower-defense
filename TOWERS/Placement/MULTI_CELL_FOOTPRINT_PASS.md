# Codex Implementation Pass — Rectangular Tower Footprints

> **Status: PLANNED / NOT IMPLEMENTED.** This is the implementation checklist for Codex. It does not mean that multi-cell placement currently works. Keep existing 1×1 gameplay intact.

## Objective and required real buildings

Extend generic tower placement to support rectangular `TowerDefinition.Footprint: Vector2I` sizes such as 1×1, 2×2, 3×2, 2×3, without building-specific placement branches.

| Building | REQUIRED footprint | Tiles occupied |
| --- | --- | --- |
| **Ammunition Factory** (`ammo_factory`) | **3×2** | **6** |
| **Ammunition Depot / Storage** (`ammo_depot`) | **2×2** | **4** |
| Other existing towers | **Remain 1×1** by default | 1 |

**Both actual buildable economy structures MUST be updated in this pass**, not merely used as examples. Set the footprints in `TOWERS/Economy/Ammunition/AmmunitionFactory/AmmunitionFactoryDefinition.tres` and `TOWERS/Economy/Ammunition/AmmunitionDepot/AmmunitionDepotDefinition.tres`. Recenter/fit the real scenes' artwork as needed, with full-building roots positioned correctly.

**Do not change ammunition balance.** The factory still makes 100 rounds every two seconds, dispatches exactly **1,000 ammo per completed delivery shipment**, and the depot still holds **10,000 ammo**. Its 10% consumer resupply trigger, drone transport, price, storage mechanics and economy should behave exactly as before.

## Read these files before implementation

- `NOTES/CodePreferences.md`, root `README.md`, [Placement README](README.md).
- `TOWERS/Shared/TowerDefinition.cs`: the exported footprint property already exists, but `Validate()` rejects any size other than 1×1.
- `TOWERS/Shared/TowerBase.cs`: `GridCell` anchor, `Footprint`, `SetPlacement`.
- `TOWERS/Placement/TowerPlacementController.cs`: single-cell preview, one-cell placement, one-cell selection index, commit/rollback and wallet transaction.
- `WORLD/GRID/WorldGrid.cs`: per-cell terrain, reservation and occupancy caches; single-cell `PlacementFailure`, `TryOccupy`, `ReleaseOccupancy`, `NavigationChanged`.
- `WORLD/GRID/WorldObjectFootprint.cs`: existing authored multi-cell portal/Citadel reservation convention; **do not** mix those static world objects with runtime placeable towers.
- `ENEMIES/Spawning/SpawnManager.cs`: single proposed-cell `PlacementFailure`/`ValidatePlacement`; `WORLD/NAVIGATION/NavigationGrid.cs`: single-block `ReachableWithout`, `HasClearance`, change handling; `WORLD/NAVIGATION/PathRequestManager.cs`.
- Both real ammunition building definitions/scenes and `TOWERS/Economy/Ammunition/Shared/Logistics/` (drone origin/return).
- Tests: `TESTS/Towers/TowerCatalogChecks.cs`, `TESTS/UI/MenuTowerFlowChecks.cs`, `TESTS/Performance/PlacementPerformanceChecks.cs`, `TESTS/Navigation/`, `TESTS/Economy/AmmunitionLoopChecks.cs`, `TESTS/Economy/DeliverySafetyChecks.cs`.

## Coordinate convention

Use `Footprint.X` for width in cells and `Footprint.Y` for height. The clicked mouse cell is the **top-left anchor**. A footprint covers all `anchor + new Vector2I(x,y)` where `0 <= x < width` and `0 <= y < height`.

`TowerBase.GridCell` remains this top-left anchor. But place the **single** tower/building scene origin at the center of the **whole occupied rectangle**, rather than at the center of the anchor tile. Grid size and transformations are from `WorldGrid`/`WorldMasterConfig` (presently 64 px): a 3×2 building is 192×128 px and has a center offset (96,64) from the top-left corner. Do not make separate nodes per tile or move an already placed building when its Inspector updates.

Rectangular footprints have a fixed orientation this pass; no rotation, L-shapes or upgrade-time resizing.

## F1 — Definition and atomic multi-cell occupancy

- [ ] Allow positive `Footprint.X` and `Footprint.Y` in `TowerDefinition.Validate`, with sensible rejection of zero/negative/overflow/clearly impossible sizes. All ordinary 1×1 definitions still validate.
- [ ] Introduce generic full-rectangle grid validation, e.g. `WorldGrid.PlacementFailure(anchor, footprint)`; check **EVERY** cell against map boundaries, non-buildable tiles, existing occupancy, and reserved Citadel/portals. Preserve existing single-cell callers via wrappers/overloads.
- [ ] Implement atomic `TryOccupy(anchor, footprint)` and `ReleaseOccupancy(anchor, footprint)`: validate entire rectangle **before modifying any cell**; one building claims/releases all its tiles or none. Rollback must never leave partially occupied cells or release neighbouring towers.
- [ ] Update navigation for **all** changed cells, with one logically consistent completed footprint. Preserve incremental A* updates and avoid a full rebuild per cell. `NavigationChanged` currently accepts one `Vector2I?` cell; adapt notification/batching safely so listeners don't see half-committed rectangles and per-cell events don't cause wasteful repeated repaths.

## F2 — Correct full-footprint navigation/path safety

- [ ] Overload `SpawnManager.PlacementFailure` for `(anchor, footprint)`; route validation remains **before purchasing or occupying**, not after.
- [ ] Add a single temporary **whole-rectangle blocker** to `NavigationGrid.ReachableWithout`, performing just **one flood fill** on the combined proposal. Checking 6 individual cells separately is NOT equivalent: together they may seal a route that none seals on its own. Reuse scratch buffers and preserve cardinal/corner-safe connectivity.
- [ ] Check circle-vs-rectangle/cells clearance for **all active enemies**, including large-radius units partly touching any footprint cell; do not allow placing a building on or too close to an enemy.
- [ ] Preserve reachability for presently usable spawn/portal entries, live enemies and Citadel approach goals; deny any placement sealing/trapping paths or overlapping reserved goal cells.
- [ ] Keep expensive flood fills and active-enemy checks to click-time or deliberate revision-aware caches. **Do not run the complete route validation in the per-frame placement preview update.** Preserve 100/500+ enemy performance and budgeted path requests.

## F3 — Placement controller, preview, interaction, wallet

- [ ] Read selected definition's footprint for all validity checks, preview drawing, committing, indexing and removal (no factory/depot special cases).
- [ ] Draw exactly the 3×2/2×2 region under the ghost, green/red for buildability/affordability. Show actual `Footprint: X x Y` in build preview and placed-tower Inspector, not hardcoded `1 x 1`. Center ghost and sprite on the entire rectangle.
- [ ] Clicking **any occupied tile** selects its **single owning tower/building**. Maintain efficient cell→tower lookup (e.g. one dictionary mapping per occupied cell); no O(all placed towers) hit-test every click. Clean all mapping entries on rollback/removal.
- [ ] Keep `EconomyManager.TryPurchase` authoritative: **one payment per whole building**, not per occupied tile. Invalid map/route/overlap, wallet failure and scene instantiation failure must not charge, leave orphaned nodes or partial occupancy.
- [ ] Preserve 1×1 firing towers, existing previews, build tray, Ctrl/Esc/RMB cancel, selected Inspector, paid upgrades and range presentation. Don't introduce mandatory combat nodes on passive factories/depots.

## F4 — Apply actual Factory 3×2 and Depot 2×2

- [ ] Edit **Factory** `AmmunitionFactoryDefinition.tres` to `Footprint = Vector2i(3, 2)` using valid Godot resource serialization. Confirm 6 occupied cells.
- [ ] Edit **Depot** `AmmunitionDepotDefinition.tres` to `Footprint = Vector2i(2, 2)`. Confirm 4 occupied cells.
- [ ] Inspect/update `.tscn` Sprite2D artwork framing so icons/scenes look appropriate for the larger rectangle and scene origins/drone home positions remain centered. Avoid unintentionally stretching build-tray icons.
- [ ] Keep factory production, **1,000 shipment**, depot **10,000 inventory**, prices, drone return behaviour, tower ammunition consumption and all related ammo tests intact.

## F5 — Focused tests and verification

- [ ] Validate 1×1, 2×2, 3×2, 2×3 footprints; reject invalid zero/negative widths/heights. **Change existing test asserting that 2×2 is invalid** in `TESTS/Towers/TowerCatalogChecks.cs`.
- [ ] Reject at right/bottom map edges, if one non-anchor cell is unbuildable, occupied or reserved, or if a whole rectangle (but not its individual cells) seals a route. Test near active enemy with nonzero radius.
- [ ] Verify 4/6 occupied tiles on success, correct full center, all tiles selecting the same instance, one-time construction charge; on failure, **zero** leftover cells/index entries/nodes/charges.
- [ ] Verify removal/rollback clears every occupied cell while neighbouring towers stay; navigation updates every blocked/unblocked tile without unnecessary full rebuilds.
- [ ] In actual gameplay both real ammunition buildings place at their requested sizes, preserve drone flight origins, stock accounting and complete factory→depot→tower ammunition flow. Existing 1×1 tower, route preview, wallet, upgrade, menu and pause tests should continue to pass.
- [ ] Add focused tests under `TESTS/Towers/` and/or `TESTS/Navigation/`; run the available C# build and headless Godot suites, especially `TESTS/Economy/AmmunitionLoopChecks.tscn`, and report genuine pass/fail/environment limits.
- [ ] When implementation **actually works**, update this document's checklist, the [Placement README](README.md), `TOWERS/README.md`, and Factory/Depot READMEs to remove outdated "1×1 only" claims.

## Out of scope

Rotated or irregular footprints, 3D, built-time resizing, construction timers, selling/destruction UI, new save format, new navigation engine, any change to ammunition balance, manufacturing, drone cargo or combat. Avoid large unrelated refactors.

## Definition of done

The build preview shows **Factory 3×2** and **Depot 2×2**; both are placeable, centered, charged once, occupy exactly 6/4 tiles, are selectable by clicking any tile, block navigation safely without sealing routes, and roll back cleanly on failed placement. Existing 1×1 towers and current ammunition gameplay remain functional.
