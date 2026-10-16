# Codex Implementation Pass — Rectangular Tower Footprints

> **Status: F1–F5 IMPLEMENTED and headless-verified (2026-10-16).** Generic fixed rectangular footprints are live; Factory is 3×2 and Depot is 2×2. Existing offensive towers remain 1×1. See `TESTS/Towers/Footprints.md` for actual commands, results and verification limits.

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

**Structural foundation rule (REQUIRED):** Any building occupying **more than one cell** must have a homogeneous underlying terrain category across **every footprint cell**: either **all buildable PASSABLE foundation** tiles or **all buildable IMPASSABLE** tiles. A rectangle mixing those categories is structurally invalid. This tests **terrain passability classification**, not identical atlas textures or visual tile artwork; varied artwork within the same category is fine. Keep existing 1×1 placement semantics unchanged.

## CRITICAL — Preserve the existing IMPASSABLE vs PASSABLE terrain distinction

**Do NOT treat every newly occupied building cell as a new enemy obstacle.** The existing implementation already handles this correctly for **1×1** towers in `WORLD/GRID/WorldGrid.cs`:

- `PlacementFailure(cell)` checks `can_build` metadata, objective/portal reservations and existing tower occupancy; **terrain being IMPASSABLE does not automatically prohibit building**. The cell must still be marked buildable.
- `BlocksEnemies(cell)` is already true if either `_terrainBlocked[cell]` **OR** `_occupied[cell]` is true.
- `TryOccupy(cell)` and `ReleaseOccupancy(cell)` notify `NavigationChanged` and increase `NavigationRevision` **only when `!_terrainBlocked[cell]`**.
- `SpawnManager.ValidatePlacement(cell)` already skips the connectivity flood when the candidate is blocked terrain (`nav.Grid.BlocksEnemies(cell)`), though its existing enemy-clearance check runs before that shortcut.

**Required multi-cell behaviour:** classify the **underlying terrain**, preferably using `WorldGrid.TerrainBlocksEnemies(cell)`, **not** `BlocksEnemies(cell)` (which also includes tower occupancy). Once every cell passes ordinary buildability/occupancy/reservation checks, require the *same terrain passability category* across every covered cell. Treat mixed categories as a placement failure **before** route checking, occupancy or payment.

| Terrain covered by the entire new building | Building occupancy/selection | Enemy navigation / route check |
| --- | --- | --- |
| **100% buildable IMPASSABLE cells** (walls/buildable rock etc.) | Mark **every footprint tile** occupied to prevent overlapping buildings; allow selecting by any tile | **ZERO newly blocked cells**. Do **not** bump navigation revision, notify `NavigationChanged`, modify A* walkability, recalculate routes, or perform a connectivity flood just because a building appeared. Existing solid terrain already blocks enemies. |
| **100% buildable PASSABLE foundation cells** | Mark every tile occupied | Every covered tile becomes a **new obstacle**. Before purchase validate the **combined rectangle** for active enemy clearance and route connectivity; on commit notify/batch navigation changes for only these cells. |
| **Mixed PASSABLE + IMPASSABLE footprint** (more than one cell) | **REJECT entire structure** — incompatible structural foundation; show a clear invalid preview/message | **NO** occupancy, purchase charge, navigation validation, revision, A* update, or repath. Do not support hybrid terrain footprints in this pass. |
| Any footprint containing `can_build=false`, reserved, already-occupied or out-of-bounds cells | Reject whole building | No mutation, no charge, no navigation updates |

For example, a **3×2 Ammunition Factory** on six buildable IMPASSABLE tiles occupies six cells without triggering any enemy repath or navigation revision. On six buildable PASSABLE foundation tiles it occupies six cells and triggers the normal combined obstacle/path check. **If even one of its six cells has the opposite passability type, reject the entire placement** — regardless of whether the route would otherwise remain open.

**Codex guidance:** keep **building occupancy** separate from **terrain passability/new obstruction**. Use `TerrainBlocksEnemies(cell)` to compare the base terrain classifications across the footprint, then `BlocksEnemies(cell)` for final walkability where appropriate. Do not allow mixed terrain, invent an always-blocks-nav flag per building, force all buildings to repath on placement, or run expensive path validation for an all-impassable footprint. Validate structural homogeneity once before committing. No per-cell full-grid rebuilds; stage the whole accepted footprint atomically and publish efficient batched change notifications only for changed passable cells. Test all three cases: all-PASSABLE accepted, all-IMPASSABLE accepted, mixed rejected.

## F1 — Definition and atomic multi-cell occupancy

- [x] Allow positive `Footprint.X` and `Footprint.Y` in `TowerDefinition.Validate`, with sensible rejection of zero/negative/overflow/clearly impossible sizes. All ordinary 1×1 definitions still validate.
- [x] Introduce generic full-rectangle grid validation, e.g. `WorldGrid.PlacementFailure(anchor, footprint)`; check **EVERY** cell against map boundaries, non-buildable tiles, existing occupancy, and reserved Citadel/portals. **For any footprint containing more than one cell, reject a PASSABLE/IMPASSABLE mixture** even if all tiles independently permit building. Use the underlying terrain category, not texture ID and not occupancy. Return an actionable failure message (e.g. `Building requires a uniform passable or impassable foundation.`). Preserve existing single-cell callers via wrappers/overloads.
- [x] Implement atomic `TryOccupy(anchor, footprint)` and `ReleaseOccupancy(anchor, footprint)`: validate entire rectangle **before modifying any cell**; one building claims/releases all its tiles or none. Rollback must never leave partially occupied cells or release neighbouring towers.
- [x] Update navigation for **all and ONLY newly blocked/unblocked PASSABLE cells** after atomic occupancy of the full rectangle. **IMPASSABLE cells still gain/lose building occupancy but must cause NO navigation revision/event/A* edit**, exactly as in current 1×1 code. Preserve incremental A* updates, stage a consistent complete footprint before callbacks, and batch notifications so listeners don't observe partial placements or duplicate expensive repaths.

## F2 — Correct full-footprint navigation/path safety

- [x] Overload `SpawnManager.PlacementFailure` for `(anchor, footprint)`; **WorldGrid must reject mixed terrain first**, so this path validator only receives a structurally valid all-PASSABLE or all-IMPASSABLE footprint. Route validation remains **before purchasing or occupying**, not after.
- [x] For an accepted **all-PASSABLE** footprint, add a single temporary **whole-rectangle proposal** to `NavigationGrid.ReachableWithout`, performing one combined flood fill rather than separate floods for each covered cell. An accepted **all-IMPASSABLE** footprint is already blocked and needs no route flood or navigation changes. **Mixed footprints never get this far; reject them in grid validation.** Reuse scratch buffers and preserve cardinal/corner-safe connectivity.
- [x] For footprints introducing new obstacles on PASSABLE tiles, check active-enemy clearance against those newly blocked cells (including large-radius enemies clipping an edge); avoid a new enemy collision/repath pass solely for a footprint fully on already-IMPASSABLE terrain. Preserve the existing 1×1 safety checks where applicable.
- [x] Preserve reachability for presently usable spawn/portal entries, live enemies and Citadel approach goals **when passable cells become blocked**; deny placements sealing/trapping paths or overlapping reserved goal cells. **Skip route validation if every covered tile was already IMPASSABLE**: such placement cannot create a new route obstruction.
- [x] Keep expensive flood fills and active-enemy checks to click-time or deliberate revision-aware caches. **Do not run the complete route validation in the per-frame placement preview update.** Preserve 100/500+ enemy performance and budgeted path requests.

## F3 — Placement controller, preview, interaction, wallet

- [x] Read selected definition's footprint for all validity checks, preview drawing, committing, indexing and removal (no factory/depot special cases). **Mixed-terrain footprints must produce an invalid (red) build preview and a clear structural foundation failure message.**
- [x] Draw exactly the 3×2/2×2 region under the ghost, green/red for buildability/affordability. Show actual `Footprint: X x Y` in build preview and placed-tower Inspector, not hardcoded `1 x 1`. Center ghost and sprite on the entire rectangle.
- [x] Clicking **any occupied tile** selects its **single owning tower/building**. Maintain efficient cell→tower lookup (e.g. one dictionary mapping per occupied cell); no O(all placed towers) hit-test every click. Clean all mapping entries on rollback/removal.
- [x] Keep `EconomyManager.TryPurchase` authoritative: **one payment per whole building**, not per occupied tile. Invalid map/route/overlap, wallet failure and scene instantiation failure must not charge, leave orphaned nodes or partial occupancy.
- [x] Preserve 1×1 firing towers, existing previews, build tray, Ctrl/Esc/RMB cancel, selected Inspector, paid upgrades and range presentation. Don't introduce mandatory combat nodes on passive factories/depots.

## F4 — Apply actual Factory 3×2 and Depot 2×2

- [x] Edit **Factory** `AmmunitionFactoryDefinition.tres` to `Footprint = Vector2i(3, 2)` using valid Godot resource serialization. Confirm 6 occupied cells.
- [x] Edit **Depot** `AmmunitionDepotDefinition.tres` to `Footprint = Vector2i(2, 2)`. Confirm 4 occupied cells.
- [x] Inspect/update `.tscn` Sprite2D artwork framing so icons/scenes look appropriate for the larger rectangle and scene origins/drone home positions remain centered. Avoid unintentionally stretching build-tray icons.
- [x] Keep factory production, **1,000 shipment**, depot **10,000 inventory**, prices, drone return behaviour, tower ammunition consumption and all related ammo tests intact.

## F5 — Focused tests and verification

- [x] Validate 1×1, 2×2, 3×2, 2×3 footprints; reject invalid zero/negative widths/heights. **Change existing test asserting that 2×2 is invalid** in `TESTS/Towers/TowerCatalogChecks.cs`.
- [x] Reject at right/bottom map edges, if one non-anchor cell is unbuildable, occupied or reserved, if **a single covered tile belongs to the opposite PASSABLE/IMPASSABLE terrain category**, or if an all-PASSABLE rectangle (but not its individual cells) seals a route. Test near active enemy with nonzero radius on previously passable cells.
- [x] Verify 4/6 occupied tiles on success, correct full center, all tiles selecting the same instance, one-time construction charge; on failure, **zero** leftover cells/index entries/nodes/charges.
- [x] Test a 3×2 building **entirely on valid IMPASSABLE cells**: six occupied/selectable cells but **zero navigation-revision changes, zero A* walkability changes and zero enemy repath triggers** on build/removal. Test six PASSABLE cells: six new blockers and appropriate navigation notification. Test **five PASSABLE + one IMPASSABLE** and **five IMPASSABLE + one PASSABLE**: both **reject atomically**, showing the structural-foundation reason, with no charged Credits, occupancy, instantiated building or navigation change. Also test 2×2 and 2×3 mixed foundations. Different tile artwork with the same passability category should remain accepted. Removal/rollback clears every occupied cell without touching neighbours or unnecessarily rebuilding the full navigation grid.
- [x] In actual gameplay both real ammunition buildings place at their requested sizes, preserve drone flight origins, stock accounting and complete factory→depot→tower ammunition flow. Existing 1×1 tower, route preview, wallet, upgrade, menu and pause tests should continue to pass.
- [x] Add focused tests under `TESTS/Towers/` and/or `TESTS/Navigation/`; run the available C# build and headless Godot suites, especially `TESTS/Economy/AmmunitionLoopChecks.tscn`, and report genuine pass/fail/environment limits.
- [x] When implementation **actually works**, update this document's checklist, the [Placement README](README.md), `TOWERS/README.md`, and Factory/Depot READMEs to remove outdated "1×1 only" claims.

## Out of scope

Rotated or irregular footprints, 3D, built-time resizing, construction timers, selling/destruction UI, new save format, new navigation engine, any change to ammunition balance, manufacturing, drone cargo or combat. Avoid large unrelated refactors.

## Definition of done

The build preview shows **Factory 3×2** and **Depot 2×2**. Each places only when **all covered buildable cells share the same PASSABLE/IMPASSABLE terrain type**, is centered, charged once, occupies 6/4 tiles, is selectable by clicking any occupied tile, and rolls back cleanly. **Mixed-terrain multi-cell placements are rejected before navigation and payment**. All-IMPASSABLE placement never changes enemy navigation; all-PASSABLE placement uses combined route safety. Existing 1×1 towers and current ammunition gameplay remain functional.

## Implemented mechanics and verification

WorldGrid validates all cells and homogeneous base terrain, then records exact anchor/size claims. Only a matching complete claim releases occupancy; partial/wrong-size releases cannot free neighbours. A multi-cell change publishes `NavigationBatchChanged(Rect2I, bool)` after every cell is staged, with one revision and no full rebuild. The original single-cell event/wrappers remain compatible. Already-solid terrain publishes nothing. NavigationGrid edits only changed passable cells in the shared profiles; PathRequestManager visits indexed dependencies and its existing queues coalesce affected ships. Click-time proposal clearance/flood uses one rectangle and reusable scratch buffers; previews only use cheap grid validation/affordability.

The placement controller centers the root and ghost across the rectangle, draws its complete region, indexes every occupied cell and retires exact claims/index/hooks on rollback or external removal. EconomyManager remains the sole one-price transaction. Real building Sprite2D scales fit their rectangles without modifying the separate 64px catalog icons. Drone endpoints still use centered root positions.

Focused checks cover 1×1/2×2/3×2/2×3, invalid/overflow dimensions, edge and non-anchor failures, both directions of mixed foundations, varied same-category artwork, full selection/payment, early/late creation failure, removal/neighbour safety, red/green centered previews without floods, a combined route seal that individual cells permit, active enemy trapping/radius clipping, zero wall navigation work, six-cell incremental batches and 500 affected ships within existing budgets. Actual purchased ammunition delivery tests pass at the new dimensions. Ordinary SDK and full-suite limitations are recorded in the verification report; no desktop visual review is claimed.
