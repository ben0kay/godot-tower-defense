# Shared construction — C1/C2 implemented; C3/C4 planned

Paid placements now occupy their complete footprint immediately, take authored time to build, and become operational once. Payment still uses the existing atomic wallet callback; invalid construction setup, scene/index failures and failed placement roll back cells, nodes and work before payment. Construction completion does not alter occupancy or navigation. Directly instantiated scenes remain operational unless placement explicitly starts construction.

## Source values

Each offensive `*Levels.cs` has one complete row per level, now including `WorkSeconds` beside Cost. Level1 supplies initial build time; later rows already author independent upgrade durations for C3. **Paid upgrades remain instantaneous for now**, and are unavailable during initial construction. Passive configs keep empty Levels and use Inspector `ConstructionSeconds` in their existing default `.tres`; no artificial weapon stats were introduced. Generic/test types default to0 seconds. Negative/nonfinite durations are rejected, copies stay independent, and no duration is compounded.

| Building | Initial build seconds | Later authored upgrade seconds (C3 deferred) |
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

`TowerBase` owns WorkPhase, elapsed/total/remaining time, normalized WorkProgress, IsBusy/IsOperational, and WorkStarted/WorkProgressChanged/WorkCompleted events. `TowerWorkState` is a source-only snapshot contract for future saving; no save/restore flow is implemented. Upgrading is a reserved phase; C3 will wire its pending target/application.

One `ConstructionWorkManager` in each playable World advances only active jobs. The manager stops physics processing when empty. Reusable double-buffered queues plus versioned membership handle simultaneous completions, removal and callback-created work without per-frame snapshot allocations; new jobs start advancing next tick. Unpaused game time only; fractional tick sums complete at the exact boundary with a small numerical tolerance. Progress notifications are throttled to approximately10Hz; completion is immediate. No construction nodes or polling are added per idle building.

Weapon and Factory processing is disabled during construction, with a shared operational guard protecting explicit ticks. Finished weapons regain their normal processing without stat rebuild, ammo refill, XP award or cooldown reset. Unfinished buildings/consumers stay outside logistics registration and demand searches. Operational checks also protect dispatch and arrival; completion registers the existing stock once. Factory remains100 rounds/2s,1000-round shipments; Depot remains10000 capacity. Existing drones/logistics remain the sole delivery authority.

The current Inspector reuses its runtime label for **CONSTRUCTING (Ns build)**, showing total authored duration, and disables upgrade purchase until completion. It refreshes on completion rather than polling. **There is no progress bar/countdown or staged art yet.** The regular finished artwork remains visible during C1/C2.

## C3/C4 remaining

- **C3:** timed paid upgrades retaining old-tier operation, ranked completion, selected-only progress bar/time remaining and event-driven updates. No full Inspector/stat rebuild each tick.
- **C4:** optional threshold-based SVG stage swaps, stable stage sprite/overlay, shared texture resources, optional globally budgeted/offscreen-aware sparks and completion effects. Battle1×1 and Factory3×2 need real stage examples; preserve live turret hierarchy during upgrades. Missing art/VFX must not change gameplay.
- Refund/selling, builder drones, damage/repair, research and campaign saving remain deferred.

See [TIMED_CONSTRUCTION_PASS.md](TIMED_CONSTRUCTION_PASS.md) for the accepted refinements and checklist, and `TESTS/Towers/Construction.md` for actual execution and benchmarks.
