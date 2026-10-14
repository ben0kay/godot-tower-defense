# Tower placement

TowerPlacementController connects the HUD Battle choice to grid-snapped placement and selection of BattleTower.tscn. Ctrl toggles the tray; Battle enters build mode; left-click builds repeatedly; right-click/Esc cancels. Clicking a placed tower outside build mode opens its independent Inspector and range ring.

WorldGrid owns bounds, tile rules and occupancy. SpawnManager checks required spawn-to-Citadel connectivity and existing ship routes. These validations precede the EconomyManager.TryPurchase transaction. The tower scene/config is created, occupancy claimed and the tower indexed before the wallet commits payment. Failure frees partial scene state and releases occupancy; no credits are lost. An occupied cell rejects repeated clicks before payment.

Default Battle Tower costs 200 Credits, from BattleTowerConfig.ConstructionCosts. The preview and Inspector display the shared ResourceCost recipe. Wallet events refresh cached affordability, so preview movement does not aggregate costs every frame. Green indicates terrain/occupancy and affordability suitability; connectivity is checked on actual placement. Red shows invalid terrain or insufficient resources. No silent free-build bypass exists.

Placed configs are deeply duplicated, including nested costs. Towers live under World/Towers; a local cell dictionary provides selection lookup. GameplayHUD only displays values; Main/Economy owns balances. Combat/rotation/navigation remain active. Saving, upgrades, tower damage and selling remain future work.
