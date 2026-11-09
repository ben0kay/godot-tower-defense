# Scrap Refinery

Place this 2×2 Economy building for 350 Credits. Its existing artwork, five-second construction and 1,000 HP use the normal tower lifecycle.

Citadel salvage drones collect physical Scrap drops and deliver them to an operational refinery. By default the refinery stores 20 raw Scrap and makes 5 Metal from 1 Scrap in 8 seconds. Output stays in the refinery's 50-Metal buffer until a shared-body courier delivers it to [Resource Storage](../ResourceStorage/README.md). Refining pauses when disabled or out of output room.

Tune building capacities and timing in `DefaultScrapRefineryConfig.tres`. Tune mission drop chance, dispatch limits, flight speed and construction preparation in `ECONOMY/Salvage/DefaultSalvageConfig.tres`. The selected inspector shows raw and inbound Scrap, batch progress, local output and outgoing cargo. See the [SALV1 plan](SALV1_IMPLEMENTATION_PLAN.md) for the test scenarios.
