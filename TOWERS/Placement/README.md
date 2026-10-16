# Generic tower placement — Phase 4A

Main/TowerPlacement exports Settings and Catalog. Available registered IDs choose a definition's scene/art/config; placed selection is indexed by grid anchor as TowerBase. No Battle-specific IDs, exported assets, casts or pivot checks live in the controller. Each scene owns its config compatibility and optional scene preflight.

Only 1×1 is supported. WorldGrid terrain/occupancy/reservations and SpawnManager connectivity checks precede the shared EconomyManager.TryPurchase. Creation initializes a private config, claims occupancy, enters World/Towers and indexes the instance before payment commits. Failure removes partial node/index state and releases occupancy; failed/repeated/route-sealing builds charge nothing. Multi-cell placement, selling and saving remain future work.

ConstructionCosts on the type config is the sole price. Wallet/config events refresh preview affordability; cursor movement does not aggregate prices each frame. Selected identity/stats/tiers and previews come from the actual instance. Types format their capability-specific stats; range draws only when InspectionRange exists. Paid upgrades preserve aim/cooldown/navigation and use the same resolver as preview. A type with no tiers shows No upgrades available.

Ctrl opens the tray or closes it and immediately cancels active placement. Left-click builds repeatedly; right-click/Esc clears the placement ghost and build-preview Inspector while leaving the tray open. Reopening the tray requires a fresh tower choice; selecting a world instance opens the independent Inspector. Test-only PassiveTestTower proves paid nonweapon placement without turret nodes.

## Startup recovery

If the exported Catalog is null at runtime, placement loads `res://TOWERS/DefaultTowerCatalog.tres` and reports the recovery once. An explicitly assigned custom catalog is preserved. If the default resource also fails to load, startup reports its exact path; preview processing and world input stay disabled, avoiding cascading null-reference errors. Wallet/HUD subscriptions are connected only after catalog validation and ghost creation succeed. The authored Main scene still assigns its catalog normally.

Ctrl/Escape shortcuts are now scoped bindings in the shared CONTROLS/GameInput router. Placement Escape has priority over Pause: one press cancels placement only; another opens Pause. RMB remains local; Ctrl closes the tray and cancels placement as before.

Ammunition Factory/Depot now use this same catalog, 1×1 route-safe occupancy, wallet transaction and selection flow as weapons. Their runtime inventory/production/slot information is supplied by optional RuntimeChanged/DescribeRuntime; a selected-only coalesced label refresh does not rebuild combat stats or previews.

## Codex implementation pass — multi-cell rectangular footprints (PLANNED)

**[MULTI_CELL_FOOTPRINT_PASS.md](MULTI_CELL_FOOTPRINT_PASS.md)** is the step-by-step coding and test brief. The **Ammunition Factory must become 3×2** (six tiles) and the **Ammunition Depot must become 2×2** (four tiles), while other towers remain 1×1. The pass covers atomic grid occupancy/rollback, combined navigation clearance, preview, any-tile selection, centered art and tests. The existing 1×1-only statements above describe the current implementation; **nothing has been coded for these new sizes yet**. Leave 1,000-round factory shipments and 10,000-round depot storage unchanged.

### Critical terrain distinction for the upcoming footprint pass

**Codex must preserve the current grid rule:** buildings may occupy buildable **IMPASSABLE** tiles (e.g. buildable walls) without changing enemy navigation, since that terrain already blocks enemies. Placement/removal there updates occupancy/selection only — **no navigation revision, path update or repath**. Buildings on buildable **PASSABLE foundation** tiles introduce new obstacles and therefore need route protection and navigation updates. **New structural rule:** for ANY footprint larger than 1×1, all occupied cells must share one underlying terrain classification: either all buildable PASSABLE foundations or all buildable IMPASSABLE terrain. A mixed 3×2/2×2 footprint is **rejected outright before charging Credits, occupying cells, or invoking navigation checks**. Use terrain metadata, not visual texture sameness. Existing 1×1 buildings are unaffected. This is specified and tested explicitly in [MULTI_CELL_FOOTPRINT_PASS.md](MULTI_CELL_FOOTPRINT_PASS.md). No gameplay code has been changed for this clarification.
