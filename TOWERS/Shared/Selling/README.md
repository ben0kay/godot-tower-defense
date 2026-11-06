# Shared tower selling — TS1

**Source Implemented / Awaiting Owner Validation.**

`TowerSelling` calculates floor(actual paid Credits × resale fraction × hull ratio), clamping the shared `TowerMasterConfig.DefaultSellRefundFraction` to 0–0.50. Health and maximum must be finite; ratio clamps to 0–1. Decimal payout arithmetic and a bounded int result prevent wallet overflow. A 200-Credit tower at 100/75/50/25/10% hull returns 100/75/50/25/10 Credits at the default rate.

`TowerBase.Selling` owns nullable paid history and the duplicate-sale latch. Successful wallet construction and upgrade transactions append only the Credits actually deducted, before notifications. Legacy unknown history stays unknown; selling is disabled rather than estimating a price. Save/load persists this source state and never replays a refund.

`TowerPlacementController.Selling` owns the selected identity/quoted amount, validates live placement membership, mission, pause, work and hull state, latches before wallet callbacks, grants once and removes the root through TreeExiting. Failed grants unlatch and retain the building. Existing owners release full grid footprint, overlays, tower indexes, power, builders, logistics and repair targets. No Destroyed event, kill reward or stock collection is synthesized.

The existing Inspector owns the red action and modal dialog. Runtime/work/wallet events update it without a processing loop. Confirm must match the same selected eligible tower and exact current refund; changed values require a fresh quote. Owner gameplay and visual acceptance remain pending.
