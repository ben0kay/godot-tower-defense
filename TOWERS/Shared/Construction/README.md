# Shared construction — C1–C3 implemented; C4 planned

Paid placements now occupy their complete footprint immediately, take authored time to build, and become operational once. Payment still uses the existing atomic wallet callback; invalid construction setup, scene/index failures and failed placement roll back cells, nodes and work before payment. Construction completion does not alter occupancy or navigation. Directly instantiated scenes remain operational unless placement explicitly starts construction.

## Source values

Each offensive `*Levels.cs` has one complete row per level, now including `WorkSeconds` beside Cost. Level1 supplies initial build time; later rows supply the independent paid upgrade duration. **Paid upgrades retain the old tier throughout work**, applying the selected complete next tier only on completion; no second purchase is allowed while busy. Passive configs keep empty Levels and use Inspector `ConstructionSeconds` in their existing default `.tres`; no artificial weapon stats were introduced. Generic/test types default to0 seconds. Negative/nonfinite durations are rejected, copies stay independent, and no duration is compounded.

| Building | Initial build seconds | Paid upgrade seconds |
| --- | ---: | --- |
| Battle | 3 | 4,5 |
| Laser | 4 | 5 |
| Blaster | 5 | 6 |
| Shockwave | 5 | 6 |
| Sniper | 4 | 5,6 |
| Machine Gun | 4 | 5,6 |
| Ammunition Factory | 8 | No purchased levels |
| Ammunition Depot | 5 | No purchased levels |

These positive values are provisional playtest defaults. No combat values/prices changed.

## Runtime ownership and performance

`TowerBase` owns WorkPhase, elapsed/total/remaining time, normalized WorkProgress, IsBusy/IsOperational, and WorkStarted/WorkProgressChanged/WorkCompleted events. `TowerWorkState` is a source-only snapshot contract for future saving; no save/restore flow is implemented. Upgrading captures PendingUpgradeLevel and duration at purchase; completion resolves that tier through the same stat pipeline with the then-current Rank. Earned XP, aiming/cooldowns, stock and old-tier operation continue during work. Capacity synchronizes once on completion without refill.

One `ConstructionWorkManager` in each playable World advances only active jobs. The manager stops physics processing when empty. Reusable double-buffered queues plus versioned membership handle simultaneous completions, removal and callback-created work without per-frame snapshot allocations; new jobs start advancing next tick. Unpaused game time only; fractional tick sums complete at the exact boundary with a small numerical tolerance. Progress notifications are throttled to approximately10Hz; completion is immediate. No construction nodes or polling are added per idle building.

Weapon and Factory processing is disabled during construction, with a shared operational guard protecting explicit ticks. Finished weapons regain their normal processing without stat rebuild, ammo refill, XP award or cooldown reset. Unfinished buildings/consumers stay outside logistics registration and demand searches. Operational checks also protect dispatch and arrival; completion registers the existing stock once. Factory remains100 rounds/2s,1000-round shipments; Depot remains10000 capacity. Existing drones/logistics remain the sole delivery authority.

The existing selected-tower Inspector has a compact work section: **CONSTRUCTING / UPGRADING to Level N / OPERATIONAL**, progress bar, elapsed/total seconds and time remaining. Only its work controls update on throttled progress events; full stat/preview resolution occurs at input/state changes, not each timer tick. Closing/switching/unloading detaches work listeners; reselecting reads current progress. A paid upgrade preview says the cost is already paid and its button stays disabled. Current stats continue describing the old active tier. Regular finished artwork remains visible; stage SVGs/VFX remain C4.

## C4 remaining

- **C4:** optional threshold-based SVG stage swaps, stable stage sprite/overlay, shared texture resources, optional globally budgeted/offscreen-aware sparks and completion effects. Battle1×1 and Factory3×2 need real stage examples; preserve live turret hierarchy during upgrades. Missing art/VFX must not change gameplay.
- Refund/selling, builder drones, damage/repair, research and campaign saving remain deferred.

See [TIMED_CONSTRUCTION_PASS.md](TIMED_CONSTRUCTION_PASS.md) for the accepted refinements and checklist, and `TESTS/Towers/Construction.md` for actual execution and benchmarks.
