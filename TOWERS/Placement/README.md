# Generic tower placement — Phase 4A

Main/TowerPlacement exports Settings and Catalog. Available registered IDs choose a definition's scene/art/config; placed selection is indexed by grid anchor as TowerBase. No Battle-specific IDs, exported assets, casts or pivot checks live in the controller. Each scene owns its config compatibility and optional scene preflight.

Only 1×1 is supported. WorldGrid terrain/occupancy/reservations and SpawnManager connectivity checks precede the shared EconomyManager.TryPurchase. Creation initializes a private config, claims occupancy, enters World/Towers and indexes the instance before payment commits. Failure removes partial node/index state and releases occupancy; failed/repeated/route-sealing builds charge nothing. Multi-cell placement, selling and saving remain future work.

ConstructionCosts on the type config is the sole price. Wallet/config events refresh preview affordability; cursor movement does not aggregate prices each frame. Selected identity/stats/tiers and previews come from the actual instance. Types format their capability-specific stats; range draws only when InspectionRange exists. Paid upgrades preserve aim/cooldown/navigation and use the same resolver as preview. A type with no tiers shows No upgrades available.

Ctrl toggles the independent tray; left-click builds repeatedly, right-click/Esc cancels; selecting a world instance opens the independent Inspector. Test-only PassiveTestTower proves paid nonweapon placement without turret nodes.

## Startup recovery

If the exported Catalog is null at runtime, placement loads `res://TOWERS/DefaultTowerCatalog.tres` and reports the recovery once. An explicitly assigned custom catalog is preserved. If the default resource also fails to load, startup reports its exact path; preview processing and world input stay disabled, avoiding cascading null-reference errors. Wallet/HUD subscriptions are connected only after catalog validation and ghost creation succeed. The authored Main scene still assigns its catalog normally.
