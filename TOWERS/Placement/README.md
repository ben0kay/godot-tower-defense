# Generic tower placement — Phase 4A

Main/TowerPlacement exports Settings and Catalog. Available registered IDs choose a definition's scene/art/config; placed selection is indexed by grid anchor as TowerBase. No Battle-specific IDs, exported assets, casts or pivot checks live in the controller. Each scene owns its config compatibility and optional scene preflight.

Only 1×1 is supported. WorldGrid terrain/occupancy/reservations and SpawnManager connectivity checks precede the shared EconomyManager.TryPurchase. Creation initializes a private config, claims occupancy, enters World/Towers and indexes the instance before payment commits. Failure removes partial node/index state and releases occupancy; failed/repeated/route-sealing builds charge nothing. Multi-cell placement, selling and saving remain future work.

ConstructionCosts on the type config is the sole price. Wallet/config events refresh preview affordability; cursor movement does not aggregate prices each frame. Selected identity/stats/tiers and previews come from the actual instance. Types format their capability-specific stats; range draws only when InspectionRange exists. Paid upgrades preserve aim/cooldown/navigation and use the same resolver as preview. A type with no tiers shows No upgrades available.

Ctrl toggles the independent tray; left-click builds repeatedly, right-click/Esc cancels; selecting a world instance opens the independent Inspector. Test-only PassiveTestTower proves paid nonweapon placement without turret nodes.
