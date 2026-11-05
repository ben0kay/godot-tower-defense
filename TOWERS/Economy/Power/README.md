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

See [consumer implementation](../../Shared/Power/README.md) and [power plan](IMPLEMENTATION_PLAN.md). Source/resource review only; .NET build/runtime remains for owner. Earlier dated phase notes below are historical.

---

## Revised PW4/PW5 plan — distance charging and internal reserves (2026-11-04)

**Planning only; PW1–PW3 implementation remains unchanged.** Towers will hold individual **energy buffers**, recharged by their shared island supply at a rate diminished by **four-neighbour conductive distance** to active generators/Citadels. Faraway towers may still eventually charge completely while idle; in combat they can drain faster than distant recharge. Current buffer remains after source death, with NoPower only after depletion/low threshold. Finite shared producer output and 0–100 refill priorities prevent energy creation. The selected Inspector bar will show **stored charge / capacity**, with recharge, drain, efficiency and priority, styled like Heat and Ammo.

PW4 is broken into **PW4-A buffer/use**, **PW4-B cached distance efficiency**, **PW4-C actual source-budget allocation/priorities**, **PW4-D Inspector UI**. Later **PW5 relays** boost nearby recharge distribution without generating energy, and retain optional manual cross-island pairs. **[IMPLEMENTATION_PLAN.md](IMPLEMENTATION_PLAN.md) is authoritative**; earlier comments below about universal island efficiency, no batteries, immediate outage or relays without distribution effects are superseded, not current requirements.

---

## PW3 Industrial Generator — 2026-11-04

Industrial is now an Economy build choice using shared generator code: **250 provisional power / 300 Credits / 5 seconds / 1200 HP / 2×2 cells**. It registers only after construction and while it can operate, and withdraws output on disable/death/removal. Existing construction, selection and checkpoint systems are reused. See [Industrial guide](IndustrialGenerator/README.md). Source/resource checks only; Godot validation pending. **Next PW4:** consumers, priority/shortages and the selected-tower power bar matching heat/ammo. Existing older not-implemented notes below describe prior phases.

---

## PW2 accounting and Citadel generation — 2026-11-04

Citadel baseline output is now source implemented (**100 provisional power**, editable in `WORLD/OBJECTIVES/Citadel/DefaultCitadelPower.tres`). Shared per-island accounting tracks supply, requested demand, margin and unassigned registrations; F4 displays these totals. It is event-driven and adds no frame/physics callback. No tower draw, outages, generators or relays yet. See [current shared implementation](Shared/README.md) and [updated phases](IMPLEMENTATION_PLAN.md). Owner build/runtime review pending; PW3 Industrial Generator is next.

---

## PW0/PW1 power foundation — 2026-11-04

The power [audit and phased plan](IMPLEMENTATION_PLAN.md) are now written. `Shared/PowerGridManager.cs` owns cached four-neighbour terrain islands in `World/PowerNetworks`; `Shared/PowerIslandMap.cs` handles reusable flood-fill buffers. WorldGrid caches independent Boolean `conducts_power` metadata in normal terrain precedence. Floors/Foundation/walls/rock conduct; Lava, space and Decorations do not. F4 shows counts and the hovered island. No power is generated/consumed and no tower is disabled yet. **Source implemented / awaiting owner Godot validation.**

The Citadel will provide configurable starter generation through the same shared producer accounting in **PW2**, supporting a modest opening defense before additional generators. Multiple Citadels remain independent sources on their own islands. Output values remain undecided. Later phases cover Industrial generation (PW3), consumers/priorities/NoPower (PW4), relays (PW5), Solar/Fusion and feedback (PW6). See the plan before implementing any later phase.

---

# Economy / Power — topology foundation implemented; generation/networks expansion planned

## Building folders

- [SolarGenerator/](SolarGenerator/README.md) — compact low-output island generator.
- [IndustrialGenerator/](IndustrialGenerator/README.md) — standard starting generator for most levels.
- [FusionReactor/](FusionReactor/README.md) — expensive endgame generator.
- [Shared/](Shared/README.md) — one reusable generator implementation, grid/accounting rules and future PowerGridManager / PowerMasterConfig.

These are **different buildable types**, not purchased level tiers of a single generator. Individual building folders own authored configs/artwork; shared generator/grid logic belongs to Shared/. The per-tower *consumption* capability remains separate at [TOWERS/Shared/Power](../../Shared/Power/README.md). The Power Relay is kept in [Utility/PowerRelay](../../Utility/PowerRelay/README.md).

## Single authority
The existing **PowerGridManager** in Shared/ owns PW1 terrain topology. Extend this single authority with generation/demand, relay unions and priority allocation in later authorized phases; do not add a competing grid manager. A PowerMasterConfig may be introduced when actual accounting/operation settings are needed, not just as an empty scaffold.

## Simple island-grid rules
1. An **edge-connected group of eligible terrain tiles is one power grid** (prefer four-directional neighbours, no diagonal-only joins). Buildings on that landmass automatically share electricity, with no cables, radius, per-tower assignment or transmission links.
2. Empty space separates grids. Reuse WORLD/GRID's cached terrain facts but define **conducting terrain eligibility** carefully: some tiles are buildable but block enemies (Foundations), while empty space is passable but unbuildable. Do not simply use enemy pathability as conduction.
3. Players manually connect **a pair of Power Relays** placed on distinct islands. Their link joins both grids, transitively if there are further relay pairs. Destroying/unlinking a relay separates networks unless another connection remains.
4. All generators on a linked island contribute to the **merged** supply. An isolated Solar Generator remains local because its island is **not linked**, not because of a separate local/grid toggle.

## Buildings and usage

All three generator types use one common implementation and documented home under [PowerGenerators](Shared/README.md); they remain distinct build choices.
- [Solar Generator](SolarGenerator/README.md): cheap, modest, isolated outposts.
- [Industrial Generator](IndustrialGenerator/README.md): standard initial/main source in most maps.
- [Fusion Reactor](FusionReactor/README.md): very large, expensive endgame output.
- [Power Relay](../../Utility/PowerRelay/README.md): manual inter-island link.
- [Consumer capability](../../Shared/Power/README.md): optional baseline IdlePowerDraw/ActivePowerDraw in non-level tower type config, with research changing effective draw later.
- **Power Priority:** each placed electricity consumer has an Inspector slider 0–100 (default 50). When output is insufficient, give full power to higher-priority buildings before lower ones (with stable tie ordering and brief anti-flicker protection). Production may stop to preserve defensive towers.

## Implementation constraints
Keep one authoritative manager; cache terrain-region IDs and a small relay graph, refresh connectivity only on actual terrain/link changes, and update generation/demand from building/state events or budgeted processing. Do not scan every tile, node or tower on every frame. No compulsory wires, batteries, power losses, fuel-delivery network or complexity beyond this first design.

Save only authored identifiers/placed per-instance inputs such as priorities and relay pairings; rebuild derived grid membership and output on load. Test island detection, diagonal joins, generator merges/splits, shortages, destruction, and frame cost at large enemy/tower counts.

**PW1 provides topology only. Tower Defense remains fully operational until consumers/shortages are deliberately implemented.**
