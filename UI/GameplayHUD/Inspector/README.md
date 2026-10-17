# Compact placed-tower Inspector — IT1–IT4

The existing bottom-right Inspector is **non-scrollable**, with no foldouts. Default target size is **560×330** through `UIMasterConfig` / `DefaultUIConfig.tres`; natural content may grow the panel slightly rather than clip controls. The Ctrl tray remains independent and keeps its own horizontal catalog scrolling. Background opacity remains 90%; text/buttons stay opaque.

## Layout

- Header: tower name, current purchased level, operational/busy status and close. Busy work shows a small progress bar and remaining seconds.
- Left (narrowed): typed **CURRENT / NEXT / GAIN** stats, paid upgrade price/time, purchase button and active disable warnings. Maximum tier shows current-only stats and MAX LEVEL, without fake future values. Construction/upgrades retain the existing transaction and operational gates.
- Right (unchanged 210px target-priority width): six target buttons (aimed weapons only), compact named Combat Rank, within-rank XP bar, heat meter for opted-in towers directly below rank, next rank, active damage/range bonuses, ammunition/delivery state and footprint. Rank XP is visibly separate from paid Credits upgrades.
- Header tooltip: description and full technical stats/grid information. Repeated Inspector/category headings, separate total-XP line and Additional Details foldout no longer occupy the panel.

A rank promotion refreshes effective comparison rows; ordinary XP increments update the rank card only. Inventory and work progress have separate event-driven updates. Selecting a target priority does not recalculate stats, spend Credits, consume ammunition, reset cooldowns or change XP. Closing, switching selection and reselecting preserve the intended subscriptions and per-instance policy. Range/highlight drawing remains in TowerPlacementController.

## Targeting

Battle, Laser, Blaster, Sniper, Machine Gun and Cryo opt into `ITowerTargetingPolicy` through private runtime configs. First/Last compare cached estimated remaining Citadel journey; Closest/Farthest compare physical tower distance; Strongest/Weakest compare current hull plus shield. Stable registration order resolves ties. Button choices persist through paid levels, promotions and selection/pause. Shockwave, Factory and Depot have no false targeting buttons; build previews cannot change a placed tower.

Cryo's unslowed/nonimmune eligibility is applied before every scoring mode. Changing priority never permits damage-only fallback on already-slowed enemies.

## Authoring and verification

`InspectorPanel.tscn`: MarginContainer → compact header and two-column layout, not ScrollContainer. Existing C# callers/tests use the corresponding Heading / Columns/Main / Columns/Side paths. `TowerUpgradeComparison` receives typed effective snapshots; no parsing of stat descriptions. It includes optional Slow and current-only max-tier values, and handles Kinetic projectiles whose Weapon capability is null.

`TESTS/UI/InspectorTargetingChecks.tscn` exercises actual Godot control bounds for all nine registered types at 1920×1080, 1366×768 and 960×540. These are headless layout assertions, not screenshot/desktop visual approval. Very small windows or unusually long future content may require another responsive layout; do not silently restore scrolling or shrink text into illegibility.

See [pass record](INSPECTOR_TARGETING_PASS.md), [target policies](../../../TOWERS/Shared/Targeting/README.md) and [executed verification](../../../TESTS/UI/InspectorTargeting.md). Desktop visual/playfeel review remains pending.

## Narrower comparison — 2026-10-17

Default width reduced from700 to620px, retaining330px target height (observed331px natural maximum). Only stat names expand; CURRENT/NEXT/GAIN use content-sized right-aligned numeric cells with12px gaps. `TowerUpgradeGrid` draws subtle1px vertical dividers at actual sorted column boundaries, including the current-only maximum-level view. Sorting/resizing invalidates drawing; there are no separator nodes or per-frame callbacks. Font sizes, transparent background, targeting, rank/ammo and upgrade behavior remain unchanged. `CompactInspectorChecks.tscn` reuses the full all-tower/viewport/max/busy layout assertions and verifies numeric widths below90px. This replaces the previous700px default without restoring scrolling.

## HD3 — live heat and generic disabled status

Selected heat-enabled towers show a compact card on the right, directly below earned Combat Rank:  `HEAT current / capacity`, a6px vector progress bar, and `OVERHEATED / COOLING` when latched. The bar is accent-colored normally, amber above75%, and red while overheated. Heatless towers have no fake heat bar. A separate red line groups all active reasons, including counts such as `EMP ×2 + NO POWER`; it is available even on heatless buildings. Normal operational headers say DISABLED when appropriate; construction/upgrading headings keep their own independent work state.

HeatChanged/DisableStateChanged subscriptions belong to the selected tower only. Selection changes, close and unload detach them. One deferred refresh coalesces events and reads the current selection; it checks controller/HUD/tower lifetime before use. Closing/reselecting refreshes live values without polling. Heat updates do not resolve stats, rebuild comparison cells, change XP/ammo, or request navigation.

Full current/next capacity, heat per shot/beam second, cooling and restart threshold use the heat-card tooltip, reading already cached comparison snapshots. Busy upgrade information keeps its first line visible and places the duplicate progress explanation in a tooltip; the header/button still show progress. Vertical gaps were tightened without reducing font sizes. Actual all-family and worst-state geometry is620×330 at1920×1080,1366×768 and960×540, including Legendary + all reasons + active paid work. No ScrollContainer, foldout or additional rank/heat window was added. Headless geometry is not rendered screenshot approval.

Verification: [HeatInspector.md](../../../TESTS/UI/HeatInspector.md). Battle's enabled3-minute profile/editor tuning remain unchanged; other towers can opt into the existing optional heat capability later.

## Left-panel trim and right-side Heat — 2026-10-17 (current layout)

The newer visual-only follow-up narrows the default overall Inspector width **620 → 560 px** while retaining its nominal330px height. The target-priority section itself remains210px wide with unchanged three-column buttons, typography, order and gaps. Only the left Main section contracts: the stat name sits nearer CURRENT/NEXT/GAIN, and the paid-upgrade button follows the smaller width. With the Inspector right-aligned, the left edge and header title naturally move right. No new ScrollContainer or separate overlay was introduced.

`InspectorPanel.tscn` owns the new paths: `Columns/Side/Conditions/HeatText` and `HeatBar` directly after `RankSection` so earned XP and heat meters appear one underneath the other. Future Health and Shield bars may join that right side, **but are not implemented by this pass**. `Columns/Main/DisableReasons` retains left-side warning text so longer multi-source descriptions do not widen or crowd the targeting section. `GameplayHUD.cs` only rebinds these nodes and makes the Heat group visible for heat-capable towers; the same selected-only updates/tooltips and disabled-reason behavior remain.

The width default is maintained in `InspectorPanel.tscn`, `UIMasterConfig.cs` and `DefaultUIConfig.tres`. Heat/Inspector/upgrade headless checks have updated paths/size expectations. **Desktop visual confirmation and runtime suite re-execution are still pending** in this editing environment, especially hot/disabled Legendary towers at smaller window sizes. TowerPlacementController, Ctrl build-menu behavior and hide-on-placement changes remain outside scope; later Codex work should preserve this scene structure.
