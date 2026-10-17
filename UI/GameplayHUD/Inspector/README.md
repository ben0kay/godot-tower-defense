# Compact placed-tower Inspector — IT1–IT4

The existing bottom-right Inspector is **non-scrollable**, with no foldouts. Default target size is **620×330** through `UIMasterConfig` / `DefaultUIConfig.tres`; natural content may grow the panel slightly rather than clip controls. The Ctrl tray remains independent and keeps its own horizontal catalog scrolling. Background opacity remains 90%; text/buttons stay opaque.

## Layout

- Header: tower name, current purchased level, operational/busy status and close. Busy work shows a small progress bar and remaining seconds.
- Left: typed **CURRENT / NEXT / GAIN** stats, paid upgrade price/time and purchase button. Maximum tier shows current-only stats and MAX LEVEL, without fake future values. Construction/upgrades retain the existing transaction and operational gates.
- Right: six target buttons (aimed weapons only), compact named Combat Rank, within-rank XP bar, next rank, active damage/range bonuses, ammunition/delivery state and footprint. Rank XP is visibly separate from paid Credits upgrades.
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
