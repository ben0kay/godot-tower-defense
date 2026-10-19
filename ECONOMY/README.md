## Credit economy towers — CP2/CP3 (2026-10-19)

Resource Tower (2×2 periodic active-wave income) and Adaptable Resource Generator (1×1 nearby killing-tower income) are now available under Economy. Each has three paid tiers, independent capped storage and Inspector collection, using the existing wallet and shared production helpers. See `ECONOMY/Production/README.md` from the repository root for balance/settings and deferred research. Earlier planned status below is historical.

---

# ECONOMY — Shared resources, session wallet and transactions

This top-level folder owns currency/resource identities, current balances and atomic payments. TOWERS/Economy instead holds future producer tower types. The HUD and individual towers never own a wallet.

## Implemented construction pass

- Resources/ResourceDefinition.cs and Credits.tres define the stable Credits resource type.
- Transactions/ResourceCost.cs is the shared typed Inspector-editable cost entry for construction and future upgrades.
- Wallet/EconomyManager.cs is one Main/Economy session node, seeded from the active level's StartingCredits.
- LEVELS/LevelConfig.cs exposes **Starting Resources / Starting Credits**. Level001Config.tres starts at **1,000**; the empty template/default starts at **0**. Duplicate the level settings resource for each map.
- BattleTowerConfig.ConstructionCosts replaces BuildCost. BattleTowerLevels.cs row 1 requires **200 Credits**; optional extra resource entries use the same transaction.
- TowerPlacementController retains terrain/occupancy and navigation connectivity checks. It creates the tower through TryPurchase; a failed build rolls back scene/occupancy and charges nothing. Repeated occupied-cell clicks cannot charge twice.
- BalanceChanged pushes the real credit balance to GameplayHUD.SetCredits on initialization and changes. No polling or fake HUD balance.

## Transaction rules

CanAfford checks without spending. TrySpend commits a complete validated recipe. TryPurchase additionally runs a synchronous creation callback before deduction; callbacks must roll back their own world changes if they fail. Grant adds a validated amount.

Duplicate resource IDs are aggregated, zero costs are allowed, malformed/null/negative cost entries and overflow are rejected. Failed operations leave balances unchanged. Every balance in a successful payment changes before notifications. Recursive wallet changes are rejected during the creation callback.

EconomyMasterConfig owns shared caps and the explicit negative-balance policy (off by default), never starting funds. Initialize(levelSettings) clears prior resource balances and loads that map's credits; reloading Main creates a fresh wallet. No campaign carry-over/save system is implied.

## Inspector workflow

Open LEVELS/Level001/Level001Config.tres and change Starting Resources / Starting Credits. Set Battle Tower prices in TOWERS/Offensive/BattleTower/DefaultBattleTowerConfig.tres / Construction / Construction Costs, expanding the ResourceCost entry and Amount. The active level and placement controller reference these saved resources.

F5 → Ctrl → Battle → place a valid tower: 200 credits are deducted, the top-left label updates, and combat stays active. Insufficient resources turn the preview red and display a rejection in the Inspector.

## Verification and future scope

TESTS/Economy/EconomyChecks.cs runs inside EnemyIntegrationChecks.tscn, covering distinct level starts/reset, duplicate and multi-resource payments, invalid recipes/overflow, reentry protection, HUD events, actual purchases, instance isolation, double clicks, insufficient funds and pre/post-occupancy rollback. The existing navigation tests also confirm sealing a required route charges nothing.

**Kill rewards are implemented:** `EnemyDefinition.KillRewardCredits` sets the amount per enemy type; `Enemy001.tres` sets **3 Credits**. `EnemyRegistry.EnemyKilled` emits only on a real kill and the session wallet grants credits through `Grant`, which pushes the updated balance to the HUD. F2 and wave kills both qualify; Citadel arrivals do not. Awards obey the wallet's max balance, and repeated hits after death cannot pay twice.

**CP1A implemented in code (runtime verification pending):** Citadel buffers now accrue only during active waves, and a shared collection method can grant stored Credits through this wallet. The collection UI remains CP1B; ARG/passive income towers, wave-completion rewards, selling/refunds and economy saving remain future passes. All can reuse this wallet/cost model; no duplicate tower-specific payment service is needed.


## Planned shared credit producers (Codex handoff)

See [Production/README.md](Production/README.md) for the full source-verified old-Citadel behavior and CP1–CP3 implementation plan: a reusable plain C# producer/buffer, independent click-collect Citadels (only accumulate while a wave is active), and a kill-attribution-based Adaptable Resource Generator. Wallet `Grant` and current HUD feedback remain authoritative; this is **not implemented**.
