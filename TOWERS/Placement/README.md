## Cached sight-aware selected range — 2026-11-05

Placement now delegates selected range drawing to `FEEDBACK/Selection/TowerRangeOverlay`: cyan visible area, red obstructed area and matching arc segments for aimed hostile towers. Existing support/untargeted circles keep their normal color. Selection close/deselect/build previews/unload clear the cached presenter; only range/position/settings/terrain changes rebuild it, with no processing callback or per-frame sight scan. Tune colors/opacities in the existing TowerMasterConfig. [Rendering details](../../FEEDBACK/Selection/README.md). Source-only validation; Godot owner review pending.

---

## Shared selected-tower overlay (2026-11-03)

A single shared `FEEDBACK/Selection/TowerSelectionOverlay` follows this controller's existing `SelectTower` lifecycle. On selecting a placed tower, a stronger but translucent HUD tactical grid/tint appears, together with a world-space caption to the tower's right and a slightly oversized pulsing footprint bracket. Status uses the existing selected tower disable/work signals (aqua operational, red disabled/upgrading), with no new gameplay polling. Overlay clears when inspection closes, selection is removed, build previews start or the tower exits. The pre-existing selection range and lower-right Inspector are untouched. Settings and owner QA: [Selection feedback](../../FEEDBACK/Selection/README.md).

---

## Placement ghost mirrors actual completed Level 1 tower art — 2026-11-02

The green/red world placement preview is now a lightweight **visual-only copy of the selected tower's finished Level 1 scene artwork**, rather than the separate `TowerDefinition.Icon` catalog illustration. `TowerPlacementGhost.cs` instantiates the authored PackedScene **off-tree**, copies visible `Sprite2D` texture layers with their authored transforms, scale, rendering order, sprite settings and tint, then immediately frees the temporary source. The ghost is rebuilt only when the selected build type changes (not on mouse motion). The copied layers have no tower scripts, combat/production processing, construction, animation or physics. The existing placement controller retains cursor/validity, green/red tint, and footprint rectangle.

The implementation handles **split finished artwork** (`BaseSprite` + `TurretPivot/RecoilRoot/Sprite`), single-sprite towers and larger multi-cell buildings. Hidden construction overlays remain hidden; authored `TowerConstructionArt` completed Level 1 base/gun overrides are respected. Native scene sizes and proportions remain intact: the Sniper's elongated 64×80 gun is no longer stretched/squashed into the 64×64 tile, and 3×2 / 2×2 sprites retain their built-in scale.

`TowerDefinition.Icon` remains a distinct **catalog/tray thumbnail** resource. The world preview does **not** use it and needs no manually maintained composite preview asset. A small static Sprite2D copy is required to display art under a tint, but its textures and transforms come from the same tower scene as the placed Level 1 tower.

No change to build prices, categories, input, navigation, occupancy, actual tower art, level-up art, or save format. Source reviewed; owner should check Sniper, split-sprite towers, plain-sprite towers, and multi-cell structures at normal Godot zoom.

---

## CD1/CD2 successful paid construction drones — 2026-10-21

Placement synchronously binds the optional World/ConstructionDrones service in Ready. Inside the existing paid commit, positive initial work is held before AddChild; only the later successful TowerPlaced event queues a drone. Failed/rolled-back transactions cannot dispatch. Zero-second sites, disabled/absent service, normal footprint/route validation, prices and placement/input behavior retain their prior semantics. See ../ConstructionDrones/README.md. Earlier planned-only notes below are historical.

## Citadel builder drone concept (2026-10-21; planned only)

See [ConstructionDrones design](../ConstructionDrones/README.md). Placement already emits `TowerPlaced` only on a successful paid site. The future drone coordinator should attach to that success, and hold the initial work clock *before its first tick* until arrival. Existing validation, payment, footprint reservation, navigation revision and repeat-placement controls must remain unchanged. No placement code changed for this plan.

---

## Hold Right Shift to keep building — 2026-10-20

Physical Right Shift is a **hold**, not a toggle: while the same tower is armed, Right Shift + LMB places one valid paid tower and keeps its ghost/placement mode active. Release Right Shift and the next successful LMB restores the tray and exits as before. Left Shift is ignored. Rejected cells remain armed; payments, affordability, occupancy/navigation and construction continue through existing transactional rules. Same-frame follow-up clicks cannot double-pay. RMB/Escape/Ctrl and category cancellation are unchanged; focus loss clears the held key. No new Node or global GameInput action.

Added test source for two successive held placements, duplicate-input guard, release-to-one-shot and Left Shift isolation. Godot/.NET runner unavailable here; runtime and visual verification pending.

---

## Stable two-row build browser — 2026-10-17

- Ctrl opening now clears the old tower selection and shows the **empty Inspector shell immediately**. Hover/focus fills the same reserved panel with existing build details/cost/affordability; no tile can be displaced by the panel appearing.
- Tray width **always reserves Inspector width plus the existing gutter**, including while details are hidden. The default 560×330 Inspector scene is unchanged. The tray uses two rows of catalog tiles; further columns scroll horizontally. Category filters recompute columns from visible entries and reset horizontal scroll; keyboard focus follows off-screen tiles.
- The shared BottomBarHeight default is now 260px to fit header, tabs, two 65px tile rows and the horizontal scrollbar. Width remains responsive in UIMasterConfig/GameplayHUD. A compact-width header hides its hint when needed.
- Affordable choice still hides the lower HUD while placing. RMB/Escape/success restore the tray plus blank shell; Ctrl closes. Successful placement remains unselected. IsInspectionVisible continues to mean **active details**, not merely the empty shell, preventing stale tower events from refilling it.
- Verification: SDK build, PlacementHudChecks (both maps plus real Rocket mouse hit/click at 1920×1080,1366×768,960×540; blank shell/stable panel bounds; 40 entries/two-row horizontal overflow/category reflow) CompactInspectorChecks and HudCategoryChecks (including the dynamic 40-type tray) passed. Desktop visual review is still pending.

This amendment supersedes older statements that cancellation must hide the entire Inspector panel. No shield/combat/research/scoring/Inspector-art changes are included.

---

# Generic rectangular tower placement — F1–F5 implemented

## Browsing vs placement vs inspection — 2026-10-17

TowerPreviewRequested (hover/focus) shows basic definition, cost and event-driven affordability in the right Inspector, without entering placement. TowerChoiceRequested is a purchase intent: insufficient resources emit BuildDenied(towerId, reason), retain the tray/Inspector and do not reserve/pay. Affordable choice hides the lower HUD and arms a ghost; real route/terrain/payment checks still occur on the world placement click, including an affordability recheck.

During placement, grid changes update only the ghost/preview rectangle and cached PreviewFailure; they never reopen the hidden Inspector. Rejected world attempts stay armed and emit the same future-audio rejection signal. No audio clips or new alert producers are wired.

RMB/Escape cancel and restore the tray, clearing preview/ghost. Ctrl closes the logical tray and cancels immediately. Successful placement is one-shot: publish the existing committed TowerPlaced event, stop placement, restore the tray, clear Inspector/selection. It intentionally does NOT auto-select. HandleWorldClick consumes the placement frame's follow-up selection; a new later click uses normal any-cell selection. Wallet/navigation/construction/research authority is unchanged.

The HUD visibility flag is separate from logical tray-open state, so temporarily hiding BottomArea cannot accidentally toggle Ctrl. Hover/focus callbacks and preview wallet/config updates do not resurrect a closed Inspector; stationary-cursor restoration is ignored until a real browsing action. TESTS/UI/PlacementHud.md records actual checks.

---

## Research-first extension — 2026-10-17

TowerPlacementController now emits TowerPlaced only after successful wallet/occupancy commit, for LevelRunStats. Failed clicks/ghosts/rollbacks emit no placement count. Event consumers track initial work completion separately; placements and construction totals are distinct.

---

Main/TowerPlacement exports Settings and Catalog. Registered definitions supply scene/art/config and positive `Footprint` dimensions. The clicked cell is the top-left anchor; X is width and Y is height. Factory is **3×2**, Depot **2×2**, and current offensive towers **1×1**. Fixed rectangles support future sizes including 2×3; rotation/irregular shapes and upgrade-time resizing are out of scope. See [the completed pass](MULTI_CELL_FOOTPRINT_PASS.md) and [verification](../../TESTS/Towers/Footprints.md).

## Validation, claims and navigation

Every covered tile must be in bounds, buildable, unreserved and free. Multi-cell buildings require one homogeneous underlying terrain category, regardless of visual artwork: all PASSABLE or all IMPASSABLE. Mixed foundations reject before navigation/payment with a structural failure message and red preview.

WorldGrid validates the whole rectangle before claiming any cell. Exact anchor/size claims make occupancy/release atomic and prevent partial releases touching neighbouring structures. All-IMPASSABLE buildings gain occupancy/selection without navigation revisions, events, A* edits, floods or repaths. All-PASSABLE proposals run radius-aware active enemy checks and one combined reusable flood before purchase, preserving usable spawn sources and live enemy connectivity. Accepted changes publish one revision/rectangle batch after complete occupancy; shared A* profiles update incrementally and indexed route validation remains budgeted/coalesced. Existing single-cell wrappers/events preserve 1×1 callers.

## Construction and interaction

EconomyManager.TryPurchase remains authoritative: one construction recipe per building. Config/scene preflight precedes occupancy; scene/index/occupancy rollback never charges, leaves an orphan or releases neighbours. Tree exit removes all of the instance's indexed cells and exact grid claim. Every occupied tile selects the same TowerBase in constant time. The single root and ghost use the full rectangle center; preview draws its full 3×2/2×2 region. Build/placed Inspectors show actual dimensions. No required combat nodes are added to passive buildings.

Preview checks only terrain/occupancy/affordability; full navigation validation is click-time. Wallet/config events refresh price state. Stats/levels/ranks and selected-only coalesced runtime labels retain existing cached behavior. Ctrl closes tray/cancels placement; RMB/Esc cancels ghost while leaving tray open. Placement Escape precedes Pause; results retain final priority.

## Startup and economy

A null Catalog export loads `res://TOWERS/DefaultTowerCatalog.tres` with a warning; explicit catalogs remain unchanged, and a missing default reports its exact resource path. Factory/Depot reuse normal catalog, wallet and Inspector infrastructure. Factory production remains 100 rounds/2s with **1,000-round shipments**; Depot retains **10,000 storage**. Centered roots remain drone departure/return positions; transport/reservations/consumption and upgrade prices are unchanged. Selling UI and saving remain deferred.

