## PW4-C scarcity direction revised — 2026-11-05 (plan only)

The owner requires **distance-based precedence** when the finite per-island generator budget cannot satisfy all capacitor requests: nearer valid conductive-path consumers charge first, equal-distance consumers share any insufficient remainder proportionally, and more distant ones may receive zero. PW4-B's cached nearest-source distance and recharge ceiling stay authoritative; the existing PW4-A/B proportional budget allocation will be replaced in PW4-C. Saved `PowerPriority` is **not** used to choose recipients; a future small and bounded per-tower slider bias may be introduced only with separate authorization. Retain source accounting, buffers, no-power recovery and event-driven service. Details: [implementation plan](../IMPLEMENTATION_PLAN.md). No code changed by this note.

---

## PW4-B — conductive-distance recharge — 2026-11-05

**Source Implemented / Awaiting Owner Validation.** The existing PowerGridManager now caps recharge by cached shortest **four-neighbour conductive distance** to a positive-output Citadel/generator footprint. Source footprints seed all valid cells at distance zero; a consumer receives at its nearest valid footprint cell. Both footprints must lie wholly on one island. Vacuum/Lava/diagonal gaps, unassigned footprints and zero-output sources cannot invent a route. Stored energy, capacity, spending and owned NoPower hysteresis are unchanged.

Shared Inspector config: `TOWERS/Economy/Power/Shared/DefaultPowerDelivery.tres`, assigned to `World/PowerNetworks.DeliveryConfig`. Defaults are **4 full-efficiency cells**, **12 falloff cells**, **0.10 minimum efficiency on reachable routes**. Curve: `max(floor, 1 / (1 + max(0, distance - full) / falloff))`; no reachable source always yields zero. Thus distance 4 → 100%, 16 → 50%, 40 → 25%, with the 10% floor only at long connected distances. Actual credited recharge is still bounded by the shared finite island budget/headroom; this is a ceiling, not promised delivery. Battle's existing 10/s nominal refill becomes 5/s at distance 16, with its original 100 capacity and 12/s active draw retained. No extra consumers opted in.

**Performance:** one reusable multi-source BFS field, lazily rebuilt/coalesced on topology or active anchor changes; no per-tower AStar, per-frame flood fill or source scan. Positive→positive output edits, demand changes and curve edits do not rebuild distances. Cached consumer distance/efficiency refresh only on placement/topology/distribution revision; regular service retains its modest cadence and asleep-when-idle behavior. Two retained int arrays add about 2 MB at 500×500 cells (memory estimate, not a benchmark). Dirty snapshots settle the previous interval against previous island labels/settings before switching, preserving time/budget accounting. Cache diagnostics expose DistanceRebuildCount, LastDistanceRebuildMilliseconds and PoweredCellCount.

Consumer API: `ConductiveDistanceCells` (-1 unreachable), `DeliveryEfficiency` and `RechargeCeilingPerSecond`. Distance/efficiency are derived, never saved; the existing checkpoint owner warms final delivery after all source states restore and before unpause. Save schema/charge/priority remain unchanged; the existing fingerprint may reject pre-change saves. **PW4-C priority scheduling and full PW4-D rate/efficiency/priority controls remain planned; PW5 relays are not implemented.**

Source/resource review only: execution environment unavailable, no build/runtime/benchmark claim. The optional PowerNetworkChecks fixture now includes a tiny detour/diagonal/multi-source distance check and two actual near/far capacitors on a transformed grid. Owner: rebuild, inspect near/distant Battle refill, source removal/EMP, isolation and a fresh Save/Load. Earlier dated phase notes below are historical.

---

## PW4-A — tower capacitors (2026-11-05)

**Source Implemented / Awaiting Owner Validation.** Battle Tower alone now opts into a local capacitor: **100 energy capacity, 10/s recharge, 0/s idle, 12/s active, empty initial fill, 15% restart**. Real shots prepay energy and ammunition before launch; no global wave-active drain. Charge survives source loss, timed work pauses power use/recharge, and built EMP/heat/NoPower-disabled towers may recharge. Save/load preserves charge, priority and hysteresis and derives only power's owned NoPower handle. Existing wave composition, combat stats, prices, generation rates and other tower types are unchanged.

The existing manager owns a shared 0.25s game-time service, dormant when no useful refill/idle drain remains. A provisional full-efficiency proportional refill is capped by island production × elapsed time; no unlimited refill or global energy bank. Source/topology changes settle the preceding interval before updating their caches. Reused consumer snapshots and sparse island requests avoid per-frame tower/map scans. **PW4-B distance, PW4-C priority scheduling, PW4-D Inspector bar and PW5 relays remain planned.** Priority is stored (default 50) but has no allocation effect yet. Existing requested-demand diagnostics are not capacitor delivered energy.

See [consumer implementation](../../../Shared/Power/README.md) and [power plan](../IMPLEMENTATION_PLAN.md). Source/resource review only; .NET build/runtime remains for owner. Earlier dated phase notes below are historical.

---

## PW3 — buildable Industrial Generator (2026-11-04)

**Source implemented / owner Godot validation pending.** `PowerGenerator.cs` / `PowerGeneratorConfig.cs` provide one passive producer implementation for generator definitions. Industrial is registered in the Economy catalog with 250 provisional output, 300 Credits, 5s work, 1200 HP and a 2×2 footprint. No paid upgrades or fuel. Source lifetime follows work, shared disabling, hull/config/spatial and terrain events; no generator processing callback. Existing tower SaveId/work/health/disable restoration reconstructs output without a schema addition. See [Industrial guide](../IndustrialGenerator/README.md).

The selected generator shows current/configured generation and island in existing runtime details. The selected-tower **power bar beside heat/ammo, supplied/required text and Power Priority slider** remain PW4 work with real consumers. No consumption or shortage enforcement was enabled by PW3. The earlier PW2-only statements below are historical and superseded for generator availability.

---

## PW2 — Citadel starter power and island accounting (2026-11-04)

**Source Implemented / Awaiting Owner Validation.** Each initialized living Citadel now registers a producer on its conducting island. `WORLD/OBJECTIVES/Citadel/DefaultCitadelPower.tres → Output` defaults to **100 provisional power**; the shared Battlefield's CitadelSession exports the resource under **Citadel Starter Power**. This is a capacity/rate budget, not a battery that accumulates over time. Output remains available between waves and is removed on death or objective removal. Multiple Citadels use distinct stable objective IDs.

The existing PowerGridManager now caches per-island supply/requested-demand, margins and producer/consumer counts. `SetProducer`, `SetRequestedDemand`, `RemoveEndpoint` and `TotalsForRegion` are the shared registration/query API for later generators/consumers. IDs are world-scoped and owned by the calling feature; repeated identical updates are no-ops. Different islands do not pool power. Region 0 registrations are counted as unassigned but contribute no usable supply/demand. Every footprint cell must be on one conducting island.

Accounting is event-driven: ordinary updates adjust only affected cached totals, and a changed terrain topology remaps existing registrations once. No per-frame scans/timers or extra per-Citadel nodes were added. F4 shows world-summed supply/requested demand and hovered-island totals; the sum is diagnostic, not a shared cross-island pool. Demand is normally **0**, because no actual tower consumers are attached until PW4. No priority allocation, shutdown, draw settings, generator buildings, relays or fuel mechanics are implemented in PW2.

Live edits to the bound resource's Output refresh sources through Resource.Changed; replace the resource reference before startup/restart. Runtime Citadel relocation requires the existing explicit WorldGrid.RebuildTerrain call. Custom Citadel roots must expose WorldObjectFootprint geometry; unassigned terrain is visible in F4. Health changes that remain alive do not rewrite generation. All binding/event cleanup is owned by CitadelSession.

Save/Continue derives the same registrations from authored config and restored objective identity/health; no energy balance, region ID or native handle is serialized and no schema bump is needed. Registration replaces stable IDs rather than duplicating output. The existing dependency-byte fingerprint can still reject checkpoints from an earlier source/resource version; this guard is unchanged.

**Brief checks:** source/resource review only; Godot/.NET is unavailable. Existing focused PowerNetworkChecks gained registration/update/removal, island-split, unassigned source, invalid input and config-change checks; they were not executed here. Owner: rebuild C#, inspect F4 on the Citadel island and an isolated island, edit Output, and check a new Save/Load/Continue without duplicate output. Generation values and future opening demand require tuning together before consumer enforcement.

---

## PW1 topology ownership — 2026-11-04

`PowerGridManager.cs` is the one scene-owned `World/PowerNetworks` service. It reads WorldGrid's cached conductivity after Grid initialization, listens to `TerrainRebuilt` and skips work unless `PowerTerrainRevision` changes. `PowerIslandMap.cs` uses two reusable int arrays for iterative four-neighbour regions, cell counts and multi-cell footprint queries. No process/physics callbacks, per-building nodes or scans of enemies/towers. Region IDs are snapshot-local (0 = none), not durable save IDs; topology rebuilds on loading. F4 reads cached diagnostics. See [PW0 audit and phases](../IMPLEMENTATION_PLAN.md) and [checks](../../../../TESTS/Power/README.md).

Later Citadel starter power and all generator types will register with this authority. No generation, consumer registration, allocation, outage, fuel, relay graph or PowerMasterConfig is added by PW1. The original generator design below remains future work; owner Godot validation is pending.

---

# Shared power — PW1 topology; generator implementation planned

## Why one shared implementation?
**Solar Generator, Industrial Generator and Fusion Reactor all perform the same job: produce electricity.** They should eventually share one PowerGenerator building implementation and one PowerGeneratorConfig shape, using separate stable tower IDs, authored settings, artwork and build-menu entries. Do not create three copies of electricity-generation logic.

They are **three generator TYPES**, not purchased upgrade levels of one building. A Solar Generator could have its own independent Level 1/2/3 progression, as could Industrial and Fusion. Preserve TOWERS' existing complete/absolute per-level row conventions if purchased upgrades are added; do not make purchasing a level turn Industrial into Fusion.

| Generator | Tier / use | Relative production | Distinguishing purpose |
| --- | --- | --- | --- |
| **Solar Generator** | Small autonomous outpost; cheap and sustainable | Low | Excellent for a small isolated terrain island |
| **Industrial Generator** | Standard starting generator used on most levels | Medium | Reliable general-purpose base workhorse |
| **Fusion Reactor** | Costly endgame infrastructure | Very high | Supports major installations and intensive defenses |

Actual balance values, costs, work times, artwork, footprints, potential fuels and durability are **not chosen**. The first power pass should not require a fuel production chain.

## Common generator design
- A placed generator registers its output once with the [POWER network](../README.md) corresponding to its **connected terrain island**. No tower-to-generator cables, supply radii or explicit tower subscriptions within one island.
- Manually paired [Power Relays](../../../Utility/PowerRelay/README.md) join islands and their supply/demand pools. An unlinked Solar island stays independent. A generator automatically shares its output across any deliberately relay-joined network, without extra local/grid switches.
- Prospective per-generator tunables: production amount/rate, construction cost, optional build/upgrade work duration, durability, operating status; future research output/efficiency modifiers. Keep **generator source settings** separate from current running state and network-derived totals.
- If the tower catalog exposes three build choices, each should have a **distinct stable ID and resource/config**, but reuse the same generator class and network registration; never hardcode generator names in POWER's accounting logic.
- The future power implementation can use a shared `PowerGenerator.cs` and `PowerGeneratorConfig.cs` here, plus three separate authored resources/scenes as needed. **No C# classes/resources/scenes are authorized by this README alone.**

## Existing SolarTower note
[Existing SolarTower](../../SolarTower/README.md) is an older possible **hybrid solar + weak defensive laser** concept. The dedicated Solar Generator is a pure producer. Decide later whether the hybrid becomes an upgrade/variant of the common generator or a distinct registered placeable; avoid duplicate default Solar Generator entries.

## Testing later
Different types sharing one implementation, independent upgraded levels, registration/deregistration, multiple generators per island, relay network merges/splits, shutdown/destroy, research changes and saving.

**Documentation-only. Current Tower Defense remains unchanged.**
