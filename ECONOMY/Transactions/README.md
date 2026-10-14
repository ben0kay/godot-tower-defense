# Transactions — Planned Shared Resource Costs and Atomic Payments

**No ResourceCost class or spend implementation exists yet.** Codex will add `ResourceCost.cs` here, containing a typed reference to `ECONOMY/Resources/ResourceDefinition` and a nonnegative integer amount.

The **same resource-cost entry type** must be reusable by Battle Tower construction, all other tower types, tower upgrade tiers and future purchases/crafting. Avoid creating a second competing TowerCostEntry struct unless needed for tower-specific rules.

The wallet/service owns payment: aggregate and validate the whole cost array, prevent overflow/negative balances, **pay all or nothing**, and publish signals only after success. A denied tower placement must not deduct anything; if placement fails after reservation/payment, restore it safely. Later refunds and selling must not create resources from invalid state.

See `ECONOMY/README.md` and `TOWERS/README.md` for the handoff.
