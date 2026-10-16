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

## CRITICAL — Preserve the existing IMPASSABLE vs PASSABLE terrain distinction

**Do NOT treat every newly occupied building cell as a new enemy obstacle.** The existing implementation already handles this correctly for **1×1** towers in `WORLD/GRID/WorldGrid.cs`:

- `PlacementFailure(cell)` checks `can_build` metadata, objective/portal reservations and existing tower occupancy; **terrain being IMPASSABLE does not automatically prohibit building**. The cell must still be marked buildable.
- `BlocksEnemies(cell)` is already true if either `_terrainBlocked[cell]` **OR** `_occupied[cell]` is true.
- `TryOccupy(cell)` and `ReleaseOccupancy(cell)` notify `NavigationChanged` and increase `NavigationRevision` **only when `!_terrainBlocked[cell]`**.
- `SpawnManager.ValidatePlacement(cell)` already skips the connectivity flood when the candidate is blocked terrain (`nav.Grid.BlocksEnemies(cell)`), though its existing enemy-clearance check runs before that shortcut.

**Required multi-cell behaviour:**

| Terrain covered by the entire new building | Building occupancy/selection | Enemy navigation / route check |
| --- | --- | --- |
| **100% buildable IMPASSABLE cells** (walls/buildable rock etc.) | Mark **every footprint tile** occupied to prevent overlapping buildings; allow selecting by any tile | **ZERO newly blocked cells**. Do **not** bump navigation revision, notify `NavigationChanged`, modify A* walkability, recalculate routes, or perform a connectivity flood just because a building appeared. Existing solid terrain already blocks enemies. |
| **100% buildable PASSABLE foundation cells** | Mark every tile occupied | Every covered tile becomes a **new obstacle**. Before purchase validate the **combined rectangle** for active enemy clearance and route connectivity; on commit notify/batch navigation changes for only these cells. |
| **Mixed PASSABLE + IMPASSABLE footprint** | Mark **all** tiles occupied | Only the **previously passable** covered tiles can change navigation. Validate the new combined blocking effect; **ignore already-impassable cells for new-obstacle updates**. |
| Any footprint containing `can_build=false`, reserved, already-occupied or out-of-bounds cells | Reject whole building | No mutation, no charge, no navigation updates |

For example, a **3×2 Ammunition Factory** placed wholly on six buildable impassable tiles should **not trigger a single enemy repath or navigation revision**. A 3×2 factory on six passable foundation tiles should reserve all six as blockers and update navigation appropriately. If only two of six cells were passable, only those two are **new** blockers.

**Codex guidance:** keep **building occupancy** separate from **terrain passability/new obstruction**. Reuse the existing `TerrainBlocksEnemies(cell)` / `BlocksEnemies(cell)` semantics and extend them to rectangles. Do not invent an always-blocks-nav flag per building, force all buildings to repath on placement, or run expensive path validation for an all-impassable footprint. No per-cell full-grid rebuilds; stage the whole footprint atomically and publish efficient batched change notifications only for changed passable cells. This distinction is fundamental and must have explicit tests.

## F1 — Definition and atomic multi-cell occupancy

- [ ] Allow positive `Footprint.X` and `Footprint.Y` in `TowerDefinition.Validate`, with sensible rejection of zero/negative/overflow/clearly impossible sizes. All ordinary 1×1 definitions still validate.
- [ ] Introduce generic full-rectangle grid validation, e.g. `WorldGrid.PlacementFailure(anchor, footprint)`; check **EVERY** cell against map boundaries, non-buildable tiles, existing occupancy, and reserved Citadel/portals. Preserve existing single-cell callers via wrappers/overloads.
- [ ] Implement atomic `TryOccupy(anchor, footprint)` and `ReleaseOccupancy(anchor, footprint)`: validate entire rectangle **before modifying any cell**; one building claims/releases all its tiles or none. Rollback must never leave partially occupied cells or release neighbouring towers.
- [ ] Update navigation for **all and ONLY newly blocked/unblocked PASSABLE cells** after atomic occupancy of the full rectangle. **IMPASSABLE cells still gain/lose building occupancy but must cause NO navigation revision/event/A* edit**, exactly as in current 1×1 code. Preserve incremental A* updates, stage a consistent complete footprint before callbacks, and batch notifications so listeners don't observe partial placements or duplicate expensive repaths.

## F2 — Correct full-footprint navigation/path safety

- [ ] Overload `SpawnManager.PlacementFailure` for `(anchor, footprint)`; route validation remains **before purchasing or occupying**, not after.
- [ ] Add a single temporary **whole-rectangle proposal** to `NavigationGrid.ReachableWithout`, with **one flood fill only when some covered cells are currently PASSABLE**. Checking six newly passable blocks independently is NOT equivalent: together they may seal a route that none seals alone. Already-impassable terrain is already blocked and must not be counted as a new obstacle or trigger a flood by itself. Reuse scratch buffers and preserve cardinal/corner-safe connectivity.
- [ ] For footprints introducing new obstacles on PASSABLE tiles, check active-enemy clearance against those newly blocked cells (including large-radius enemies clipping an edge); avoid a new enemy collision/repath pass solely for a footprint fully on already-IMPASSABLE terrain. Preserve the existing 1×1 safety checks where applicable.
- [ ] Preserve reachability for presently usable spawn/portal entries, live enemies and Citadel approach goals **when passable cells become blocked**; deny placements sealing/trapping paths or overlapping reserved goal cells. **Skip route validation if every covered tile was already IMPASSABLE**: such placement cannot create a new route obstruction.
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
- [ ] Reject at right/bottom map edges, if one non-anchor cell is unbuildable, occupied or reserved, or if newly blocked PASSABLE cells across the rectangle (but not individually) seal a route. Test near active enemy with nonzero radius on previously passable cells.
- [ ] Verify 4/6 occupied tiles on success, correct full center, all tiles selecting the same instance, one-time construction charge; on failure, **zero** leftover cells/index entries/nodes/charges.
- [ ] Test a 3×2 building **entirely on valid IMPASSABLE cells**: six occupied/selectable cells but **zero navigation-revision changes, zero A* walkability changes and zero enemy repath triggers** on build/removal. Test six PASSABLE cells: six new blockers and appropriate navigation notification. Test mixed PASSABLE/IMPASSABLE cells: only originally passable tiles alter navigation. Removal/rollback clears every occupied cell without touching neighbours or unnecessarily rebuilding the full navigation grid.
- [ ] In actual gameplay both real ammunition buildings place at their requested sizes, preserve drone flight origins, stock accounting and complete factory→depot→tower ammunition flow. Existing 1×1 tower, route preview, wallet, upgrade, menu and pause tests should continue to pass.
- [ ] Add focused tests under `TESTS/Towers/` and/or `TESTS/Navigation/`; run the available C# build and headless Godot suites, especially `TESTS/Economy/AmmunitionLoopChecks.tscn`, and report genuine pass/fail/environment limits.
- [ ] When implementation **actually works**, update this document's checklist, the [Placement README](README.md), `TOWERS/README.md`, and Factory/Depot READMEs to remove outdated "1×1 only" claims.

## Out of scope

Rotated or irregular footprints, 3D, built-time resizing, construction timers, selling/destruction UI, new save format, new navigation engine, any change to ammunition balance, manufacturing, drone cargo or combat. Avoid large unrelated refactors.

## Definition of done

The build preview shows **Factory 3×2** and **Depot 2×2**; both are placeable, centered, charged once, occupy exactly 6/4 tiles, selectable by clicking any tile and roll back cleanly. **Buildings on already-IMPASSABLE buildable terrain never change enemy navigation; only newly blocked PASSABLE cells trigger route safety checks/navigation changes**. Existing 1×1 towers and current ammunition gameplay remain functional.
