# Shared costs and atomic payments

ResourceCost is a GlobalClass Resource with ResourceDefinition and integer Amount. BattleTowerConfig.ConstructionCosts is an Inspector-editable Array<ResourceCost>; future tower types and upgrades can reuse the exact same shape.

DefaultBattleTowerConfig.tres contains one 200-Credits entry. The legacy BuildCost integer is retired; it is never also charged. Optional materials can be added to the same array, with balances granted through the wallet. No material generation gameplay exists yet.

EconomyManager combines duplicate ResourceIds before affordability checks, rejects malformed/null/negative/overflowing recipes, and commits all deductions before emitting events. Empty recipes are intentionally free; zero entries still require a valid resource. Grant validates amounts and caps. TryPurchase charges only after the caller's synchronous build callback succeeds.

Placement validates terrain/occupancy and spawn-to-Citadel connectivity before the purchase callback. Failed scene creation or later placement exceptions release occupancy and remove/free partial tower state, with no payment committed. Occupied cells prevent a repeat click from purchasing twice. There is no sell/refund or upgrade mechanic yet.
