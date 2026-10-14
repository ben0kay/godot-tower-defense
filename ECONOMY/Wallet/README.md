# Session wallet

EconomyManager is the single Main/Economy node for the loaded battlefield. It initializes from World/Grid.Level.Settings.StartingCredits after the HUD is ready. A fresh Main scene starts a fresh wallet; Initialize(newLevelSettings) also clears all prior resource balances. It does not carry campaign balances or save state.

GetBalance, CanAfford, TrySpend, TryPurchase and Grant identify resources by their stable ResourceId, not display text or asset instance. BalanceChanged fires after all balances in a transaction are committed. Credits changes push GameplayHUD.SetCredits; there are no economy/HUD polling ticks.

EconomyMasterConfig sets maximum balance and the explicit AllowNegativeBalances policy (off by default). Starting values clamp to the valid nonnegative range. Invalid/negative entries and duplicate-cost overflow reject the complete transaction. Grants reject overflow instead of silently losing excess.

TryPurchase validates the complete recipe, runs a synchronous world commit callback, then deducts once. While that callback runs, wallet mutation/reset and recursive purchases are prevented. A false result or exception does not deduct; the caller must undo any world changes it made. TowerPlacementController does this for its scene/occupancy/index.
