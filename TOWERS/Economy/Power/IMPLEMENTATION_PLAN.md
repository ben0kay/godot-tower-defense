## PW4-C allocation decision — **distance first, NOT priority first** (2026-11-05)

**OWNER-APPROVED DESIGN / PLANNED, NOT IMPLEMENTED.** This section overrides older PW4-C wording below that says "higher PowerPriority first", "descending priority", or describes a priority slider as an already-approved core PW4-C feature. **PW4-A capacitor buffers and PW4-B conductive-distance recharge are source implemented and must not be re-created or changed as part of this documentation update.** The next actual coding task is PW4-C.

### Gameplay contract

- **Physical source distance is the primary authority for receiving scarce electricity.** Within each connected conducting island, a nearer tower should receive its requested energy refill ahead of a more distant tower, based on PW4-B's existing cached shortest four-neighbour conductive distance from the nearest **active Citadel or generator**. No straight-line shortcuts across unpainted space/Lava. Distance already reduces each tower's maximum recharge rate through PW4-B; **PW4-C additionally determines who gets the remaining finite supply when it cannot satisfy all requests**.
- With **sufficient generation**, every reachable eligible consumer gets its normal PW4-B distance-capped recharge, regardless of other towers. A faraway tower can still gradually fill to **100% capacity** while idle. PW4-C must not add an arbitrary range cutoff or lower capacity because of distance.
- With **insufficient generation**, near consumers get first access to the remaining network energy. More distant ones may get **partial recharge or exactly zero**, even on the same conducting island. It is intentional that distant banks may drain during combat while nearby towers keep recharging. Do not pro-rate scarce production equally across all distances (the PW4-A/B proportional allocator is provisional and is to be replaced).
- A generator/Citadel supplies a **rate** (energy per second), not a free per-tower refill. **Within one island**, generation from multiple active sources still contributes to one finite shared budget; nearest active source supplies the *distance ordering and efficiency*, not a separate per-source infinite reserve. No duplicated output for overlapping generators. A removed/disabled source immediately withdraws its rate, invalidates the affected distance mapping, and does **not** delete stored capacitor energy.
- **Internal buffers always matter.** Allocation is energy *credited into storage*; a tower does not instantly shut off just because it receives less than requested refill. Existing NoPower only follows its stored-energy sufficiency/recovery hysteresis. A fully charged or work-ineligible tower requests no refill; a depleted NoPower-disabled tower **still requests refill**.

### PW4-C implementation algorithm (Codex)

1. Reuse the single `PowerGridManager` and PW4-B's cached `ConductiveDistanceCells`, `DeliveryEfficiency`, `RechargeCeilingPerSecond`. Maintain the existing shared 0.25s default time-integrated service / pause semantics; no per-tower process loop, duplicate manager, new pathfinder or per-frame terrain scan.
2. For each active consumer and elapsed service interval, compute `desiredEnergy = min(bufferHeadroom, distanceCappedRechargePerSecond * elapsed)`. No valid powered route, incomplete/busy tower or full buffer → zero. **Do not** treat `RequestedDemand` diagnostics as real capacitor refill.
3. Partition requests by the *actual connected island/network*, reserve `availableEnergy = max(0, sum(active generator rates on that network) * elapsed)`. Sort eligible requests by **actual conductive-path distance, ascending**, never by stored `PowerPriority`. A single shared network budget is consumed once.
4. Allocate to all closer-distance groups before farther groups. If a distance group as a whole fits, give each request its full desired energy. If the remaining energy cannot fill a **same-distance** group, share that remainder **proportionally to each member's desired amount**; later-distance groups receive **zero**. Use stable SaveId ordering only for deterministic floating-point remainder/tie handling, not as the main selection criterion. No unfair winner based on array/dictionary enumeration order.
5. Do not exceed either a tower's local distance-capped recharge/headroom or the network's production × elapsed; record credited amounts only after the network allocation is resolved. Avoid double charging across pause, source death, island split/merge, hot config edit, restoration and removal. Respect all existing ammo/heat/EMP/work gates and preserve stored power and save fields.
6. Allow the service to sleep when no eligible work remains; wake on producer, topology, consumer charge/state/config events. Reuse cached topology/delivery revisions, existing no-power conditions and buffers. If same-distance consumer count is large, use temporary retained buffers and O(n log n) sort **only per active service tick**, not per frame; do not rebuild the world-wide distance field for allocation alone.

**Example (illustrative, not approved balance):** A network offers **10 energy** during a service interval. A near tower 3 tiles away wants 6; a middle tower 12 tiles away wants 7; a far tower 25 tiles away wants 4. Allocate **6 near, 4 middle, 0 far**. When output later rises, the far tower begins receiving energy within its PW4-B distance ceiling. At the same actual distance, competitors share a short group allowance proportionally.

### Priority slider — **optional future mild bias; not PW4-C's main rule**

The existing consumer runtime/save already carries a dormant `PowerPriority` value **0–100, default 50**, but **PW4-C must not enable a priority-first allocator or require a priority slider**. The owner *may later* add a control in each tower's Inspector to give it a **small** favourable/unfavourable weighting when electricity is scarce. This must remain **secondary to geography** and must never raise the PW4-B distance-based recharge ceiling, bypass a disconnected island, create energy or erase the local capacitor cost.

If specifically approved later, a viable approach is a **bounded effective distance for rationing order only**, e.g. neutral priority 50 = unchanged, 100 = at most 2 tiles closer, 0 = at most 2 tiles farther (**provisional; tune/approve separately**). Keep **actual conductive distance** for the PW4-B charge falloff and reachability. Clamp the bias so raising a tower to 100 cannot defeat a meaningful long-distance disadvantage. The slider's numerical range should reflect the mild role, not promise guaranteed service. Do not implement this speculative weighting or an inert control in PW4-C. PW4-D should first provide recharge rate, distance, efficiency, allocated refill and shortage status; add priority adjustment only after that optional mechanic is explicitly requested.

### Focused acceptance for future PW4-C

- Close/mid/far on one powered island: normal distinct PW4-B recharge with abundant generation; then near-first refill, partial middle and **zero far** when the production budget is tight; all may eventually store 100% with enough time/available production.
- Same-distance ties share scarce energy proportionally and deterministically; two nearby sources change shortest distance without creating power; a removed/EMP-disabled source reduces supply and revises ordering.
- Full/constructing/upgrading/isolated/unpowered and NoPower-recovering consumers behave correctly; stored energy persists when generation disappears; old Save/Load and F4 accounting preserve their ownership.
- Brief source/build check and manual owner gameplay validation only; no expensive benchmark or broad suite. Confirm energy conservation, existing cache wake/sleep and no frame spikes from unnecessary distance-field rebuilds.

---

## PW4-B — conductive-distance recharge — 2026-11-05

**Source Implemented / Awaiting Owner Validation.** The existing PowerGridManager now caps recharge by cached shortest **four-neighbour conductive distance** to a positive-output Citadel/generator footprint. Source footprints seed all valid cells at distance zero; a consumer receives at its nearest valid footprint cell. Both footprints must lie wholly on one island. Vacuum/Lava/diagonal gaps, unassigned footprints and zero-output sources cannot invent a route. Stored energy, capacity, spending and owned NoPower hysteresis are unchanged.

Shared Inspector config: `TOWERS/Economy/Power/Shared/DefaultPowerDelivery.tres`, assigned to `World/PowerNetworks.DeliveryConfig`. Defaults are **4 full-efficiency cells**, **12 falloff cells**, **0.10 minimum efficiency on reachable routes**. Curve: `max(floor, 1 / (1 + max(0, distance - full) / falloff))`; no reachable source always yields zero. Thus distance 4 → 100%, 16 → 50%, 40 → 25%, with the 10% floor only at long connected distances. Actual credited recharge is still bounded by the shared finite island budget/headroom; this is a ceiling, not promised delivery. Battle's existing 10/s nominal refill becomes 5/s at distance 16, with its original 100 capacity and 12/s active draw retained. No extra consumers opted in.

**Performance:** one reusable multi-source BFS field, lazily rebuilt/coalesced on topology or active anchor changes; no per-tower AStar, per-frame flood fill or source scan. Positive→positive output edits, demand changes and curve edits do not rebuild distances. Cached consumer distance/efficiency refresh only on placement/topology/distribution revision; regular service retains its modest cadence and asleep-when-idle behavior. Two retained int arrays add about 2 MB at 500×500 cells (memory estimate, not a benchmark). Dirty snapshots settle the previous interval against previous island labels/settings before switching, preserving time/budget accounting. Cache diagnostics expose DistanceRebuildCount, LastDistanceRebuildMilliseconds and PoweredCellCount.

Consumer API: `ConductiveDistanceCells` (-1 unreachable), `DeliveryEfficiency` and `RechargeCeilingPerSecond`. Distance/efficiency are derived, never saved; the existing checkpoint owner warms final delivery after all source states restore and before unpause. Save schema/charge/priority remain unchanged; the existing fingerprint may reject pre-change saves. **PW4-C distance-first shortage allocation (not priority-first) and PW4-D rate/efficiency feedback remain planned; optional mild priority adjustment is deferred. PW5 relays are not implemented.**

Source/resource review only: execution environment unavailable, no build/runtime/benchmark claim. The optional PowerNetworkChecks fixture now includes a tiny detour/diagonal/multi-source distance check and two actual near/far capacitors on a transformed grid. Owner: rebuild, inspect near/distant Battle refill, source removal/EMP, isolation and a fresh Save/Load. Earlier dated phase notes below are historical.

---

## PW4-A implementation — 2026-11-05

**Source Implemented / Awaiting Owner Validation. PW4-A only.** [Consumer guide](../../Shared/Power/README.md) documents the implementation and exact policy. Battle Tower is the only initial consumer: **100 capacity / 10 energy-per-second refill / 0 idle / 12 total active / empty initial fill / 15% restart**. A shot prepays active rate × shot interval before emission, with ammo-rejection rollback; no wave-based active spending. Timed construction/upgrades preserve charge and neither charge nor drain; built heat/EMP/NoPower disables permit charging, without idle spend. Non-power towers remain unchanged.

The existing PowerGridManager has one asleep-when-idle shared service (default 0.25s), full-efficiency same-island delivery and provisional proportional sharing capped by production × elapsed game time. No grid bank. Save state contains charge/priority/recovery/paid active remainder, not authored capacity/draw or derived owned NoPower handles. Older missing power fields read as empty; the dependency fingerprint guard remains authoritative. Compact energy runtime text is included; the selected power **bar stays PW4-D**. Priority is persisted but remains a dormant future input; **PW4-C will use geographic distance first**. This PW4-A-era status paragraph is historical: PW4-B distance is now source implemented, while PW4-C, relays and optional priority bias are not.

Validation: source/resource/diff review only; .NET unavailable, no install, suite, gameplay or benchmark run. Owner: rebuild, check completed Battle charge/fire/deplete/recover, source loss/isolation, work + heat/EMP coexistence and a fresh Save/Load. The rates are provisional and were reported before implementation; existing combat stats/source output/campaign waves were not tuned.

---

# PW4 REVISED DESIGN — distance-based charging + per-tower capacitor buffers (2026-11-04)

> **AUTHORITATIVE UPCOMING DESIGN; NOT YET IMPLEMENTED.** Supersedes the old PW4 "full power or immediate shutdown" allocation proposal and the old assumption that connected islands deliver power at identical efficiency. **PW0–PW3 remain implemented and unchanged**: PW1 conductive islands, PW2 Citadel generation 100 plus island accounting, PW3 buildable Industrial Generator 250. Treat all figures in this new PW4 section as provisional playtest values until approved. Codex: implement **one pass at a time**, only when asked. Do not silently implement PW5 relays as part of PW4.

## Player-facing goal

The entire four-neighbour **conducting island** still shares a finite generation budget, but charging a tower's **internal power buffer** becomes slower with greater **conductive-path distance** from a generator/Citadel or a powered relay. A far tower is **not capped at 30% battery**: during downtime it can eventually bank **100% energy**, provided a viable power source remains and its recharge exceeds any idle consumption. During sustained combat the tower spends stored energy faster; a distant tower can run low and must recover. A nearby tower refills more quickly and sustains combat longer. No generic damage/fire-rate debuff simply because a building is far away.

- **Sources:** Citadel and Industrial (later Solar/Fusion) produce a configurable *amount of energy per second* into their own connected network; numbers 100 and 250 are currently provisional rates for PW4's accounting interpretation. Production is continuous while the source can operate. Power is **not** automatically banked for the island when no consumer requests it.
- **Consumers:** Individual opted-in towers/buildings have a finite capacitor / battery with `Capacity`, `CurrentEnergy`, maximum `RechargeRatePerSecond`, `IdleDrawPerSecond` and `ActiveDrawPerSecond`. A per-instance `PowerPriority` value (0–100, default 50) is already saved but **inactive, reserved for possible future mild bias**. The active draw is the total active drain, **not idle + active**. Absent a capability, a tower continues normal operation and shows no power bar.
- **Buffer math:** Per elapsed game-time step, `energyNext = clamp(energyNow + deliveredEnergy - spentEnergy, 0, capacity)`. **Delivered energy** is limited by (a) valid powered conductive route, (b) effective local recharge rate after distance falloff, (c) headroom to capacity and (d) the shared generator output budget after allocation. Energy cannot go negative or exceed capacity.
- **Combat drain:** Use existing weapon/activity signals and common tower operation gates to distinguish actively attacking from idle; don't permanently drain ActiveDraw just because the global wave is active if a tower is not attacking, and don't switch to idle demand *because NoPower has disabled firing*. Prefer meaningful energy spend per actual attack/active window with a single shared policy; review continual beams/shockwaves and noncombat producers separately. A fully charged distant tower can fire normally until the capacitor drops.
- **Outage and recovery:** When a generator dies/EMP-disables, remove its output and recompute distribution. **Stored power remains**; consumers may continue spending their buffer. On depletion, use the existing owned `TowerDisableReason.NoPower` handle, *not* a new disable stack. Recharge when power comes back; release only the power handle after a configurable restart threshold (example 15% capacity) and preserve other EMP/heat/upgrading reasons. Avoid rapid on/off flicker. While NoPower is active, the capacitor can still recharge; avoid circular logic where a disabled weapon incorrectly reports zero demand indefinitely. The tower must never fire an unpaid attack.
- **Construction & upgrades:** A newly built tower should not receive a free full charge at build completion by accident. Make a clear Inspector-editable initial-fill fraction (suggested provisional default **0%**), and explicitly determine how unfinished/upgrading towers draw/charge; *do not alter construction clocks*. Upgrades preserve existing charge up to the new capacity instead of magically filling the difference. Keep buffer values per instance.
- **Illustrative scenario (NOT balance):** A 100-capacity Battle Tower consumes 12 energy/s during firing. Near its generator it receives 10/s and drains net 2/s; far away it receives 3/s and drains net 9/s. At idle, provided idle use is lower than 3/s, **both towers can eventually reach 100**.

## Distance delivery rule

1. Preserve PW1's connectivity: power cannot cross vacuum, Lava or other non-conducting cells without a PW5 explicitly linked relay pair. **Use shortest conductive-path distance (four-neighbour cells)** within an island, not straight-line Euclidean distance across gaps. A large labyrinthine island can have long electrical paths.
2. From **each active generator and Citadel** treat its valid island footprint as a local distribution anchor. Multiple active sources on one island improve coverage; supply itself is still a **single shared, finite island budget**. Recalculate a compact cached distance/efficiency field using **multi-source BFS**, or an equally bounded cache built from the existing power-island map; don't run one path search per consumer/tick. If a source is destroyed/disabled or topology changes, invalidate and refresh affected cache(s).
3. The effective recharge ceiling for one consumer is `maxRechargePerSecond × efficiency(distance)`, with configurable full-efficiency distance, soft falloff and optional low-but-positive floor **on a valid powered connected island**. Example only: 100% near, 60% mid-island, 30% far. Don't hard-code those figures; keep the curve/tuning in shared Inspector config. A distance penalty **reduces recharge rate, not maximum buffer capacity**, and does not create extra costs for the consumer by multiplying its drain.
4. Actual delivered energy per interval is `min(rechargeCeiling × elapsed, bufferHeadroom, allocatedNetworkEnergy)`. **Conservation:** for each network per interval, sum of all delivered amounts must not exceed sum of currently operating generator outputs × elapsed. The generator produces no extra energy by being nearest; there is no double counting if multiple sources overlap.
5. **If no active source is electrically reachable:** delivered rate is 0 regardless of any previously cached efficiency; the tower may continue on saved charge. Terrain edits, generator lifecycle, building relocation and Save/Load must invalidate stale mappings. A source/consumer with invalid footprint island is unassigned and must not invent a path.
6. A **relay is NOT a generator**. PW5 relays will become new **distribution anchors** only when electrically connected to a live supply, restoring/boosting effective charging nearby while respecting the **same finite source budget**. Even a standalone unpaired relay on the same island may reinforce local reach; the *optional manually paired two-island link* adds remote connectivity. No unlimited free electricity, no unpowered-relay rescue, no positive feedback from relay chains. Define any operating cost, hop penalty, or throughput cap during PW5 balancing. In PW4, model anchor calculations so adding relays in PW5 won't demand a full rewrite.

## Allocation, priorities, buffer edge cases

- Preserve independent network accounting and future manually paired relay unions. Treat power supply as a **rate** (units/sec), buffer energy as a **quantity** (units). Show both clearly; do not label a storage bar as "Power 20/20" if that actually means instantaneous supply.
- On each modest, shared **time-integrated power service tick** (e.g. every 0.2–0.5s), compute each eligible consumer's **desired refill** from PW4-B distance-capped rate × step and headroom; allocate at most network production × step. **PW4-C allocates to nearest actual conductive-distance groups first**, sharing a partial same-distance group proportionally and letting farther groups receive zero if exhausted. A full buffer never reserves power. The persisted `PowerPriority` is **not** active in PW4-C; a small future bias is optional, not the primary rule. See authoritative PW4-C decision at the top.
- Recharge does not guarantee that a tower will stay online: actual spend depends on attacking/idle state. **NoPower depends on buffer sufficiency/restart hysteresis**, not on simple network-demand > supply. A heavily buffered tower may operate throughout a temporary shortage, then drain over sustained fire.
- Avoid paradoxical allocation loops: a tower that runs out of charge should still request refill when NoPower prevents attacks; a generator does not depend on its own grid energy; relay connectivity must not vanish just because a consuming tower browns out.
- **Saving:** add current stored energy, charging/operational settings that are truly per-instance (priority, initial state if applicable) to the existing campaign checkpoint owner when PW4 is implemented. Capacity/nominal draw remains authored config, not a copied permanent save. Rebuild distribution/allocations from live sources and conductivity **before unpausing**; restore NoPower as a *derived* condition, not a stale serialized handle. Preserve wave ownership, ammo stock, heat, Credits and current saves' compatibility policy.
- **Starting balance:** use modest per-type capacity/idle/active/recharge defaults and expose them in Inspector; consider starting with Battle Tower as the consumer baseline and opt in other towers deliberately. Do not accidentally disable all early defenses. Decide when energy spends (actual shot events vs sustained attack state) before implementing. Classify utility/economy building draw deliberately rather than applying one rule to everything.

## PW4 subpasses — Codex implementation order

| Subpass | Scope / location | Done when |
| --- | --- | --- |
| **PW4-A — Tower capacitors** | `TOWERS/Shared/Power/` opt-in consumer config/runtime; absolute capacity, current per-instance charge, idle/active expenditure, event-driven activity, existing `NoPower` handle & thresholds. Basic local recharge can initially use constant full efficiency supplied by the single grid manager as a temporary baseline. | One selected test tower can charge while idle, sustain fire from buffer, deplete/recover, and save/load charge; unchanged non-power towers and heat/EMP gates still work. **Do not require relays or distance yet.** |
| **PW4-B — Distance-limited recharge** | `TOWERS/Economy/Power/Shared/` cached generator/Citadel anchors, four-neighbour conductive distances, editable falloff/efficiency; expose a per-consumer current delivery ceiling. | Near and distant towers both eventually fill while idle; far tower charges slower and drains earlier during continuous fire. Source death / isolation updates efficiency; no repeated pathfinding every tick. |
| **PW4-C — Distance-first scarce-power allocation** | Replace the provisional proportionate allocator in the existing `PowerGridManager`: assign finite source budget to **nearest conductive-distance consumers first**, proportional sharing only among requests at the same distance. Retain existing dormant 0–100 priority storage without using it. | Nearby towers take available recharge before far towers, which can receive partial/zero if output is exhausted. Sum of credits stays within generation × elapsed; stored energy and PW4-B rate ceilings remain intact. |
| **PW4-D — Selected tower Inspector** | Extend the already-live stored-energy meter beside Heat/Ammo with **actual allocated recharge, drain, real conductive distance, efficiency and shortage/NoPower state**, using meaningful or capped updates. **No live priority slider until the optional small-bias policy is separately approved.** | Player can distinguish full-but-slow, empty, power-starved and no-connected-source cases; existing stored energy still displays accurately. No extra per-tower UI processing loop. |
| **PW5 — Power Relays** | `TOWERS/Utility/PowerRelay/`: reinforce local effective signal as a non-generating distribution anchor on an existing powered island; optionally pair two relays across isolated islands to join network budgets and propagate signal. | Nearby towers recharge faster with a viable, powered relay; removing it returns natural falloff; remote island only charges when a valid powered pair connects it. Chaining never creates energy. |

**PW4-A/C dependency note:** PW4-A may introduce a tiny provisional single-network recharge service for integration, but **PW4-C owns authoritative finite production allocation**. Don't accidentally ship an unlimited refill loop as final PW4-A; wire the same owner/seam to be replaced/extended by PW4-C. PW4-B's efficiency is a capped ceiling, not a promise of delivered power. Keep these phases focused; don't build a new parallel power manager.

## Save & UI examples

- Tower Inspector example: `ENERGY 64 / 100` (buffer), `DELIVERED +3.0/s`, `FIRING −12.0/s`, `DISTANCE 25 CELLS`, `EFFICIENCY 30%`, `POWER LIMITED` when allocation is below its local recharge ceiling. Net is −9/s during that example. Priority 50 is saved but **not yet an active allocation control**. A full buffer displays full even if delivered rate is zero.
- Full battery + dead generator: still fires until depleted. Empty battery + weak source: recovers at low recharge; returns to fire only when restart threshold is reached. Generator repaired/replaced: higher recharge, *no instantaneous battery fill*.
- No power-capable tower: existing Heat and Ammo UI unchanged; no bogus power bar. Generator: shows *production output*, not a consumer capacitor unless separately designed.

## Safety / minimal verification

Read `NOTES/CodePreferences.md`, current Tower/Power/Inspector/save code and feature guides before editing. Preserve PW1 topology and PW2/PW3 generation. Avoid per-enemy/tower spatial scans, per-frame flood fills, mass physics processing or unbounded queues; one asleep-when-idle service with event-triggered cache rebuild is preferred. Light source diff review and a focused build/check if practical are sufficient; do not run extended suites or performance benchmarks by default. Report proposed power rates and any unapproved assumptions before tuning balance.

---

## PW3 implementation update — 2026-11-04

Industrial Generator is now registered as an Economy placeable, backed by shared PowerGenerator/PowerGeneratorConfig, static finished/0/35/70% construction SVGs and existing work/disable/save seams. Provisional authored values: 250 output, 300 Credits, 5 seconds, 1200 HP, 2×2. No consumer gating, fuel or paid upgrades. Runtime details show current output/island. The requested selected-tower **power bar styled like heat/ammo** is part of PW4: supplied/required power, full/empty allocation state, normal/shortage colours, neutral zero-demand handling and a nearby 0–100 priority slider, refreshed on events only. Source implemented; owner Godot build/play review pending.

---

## PW2 implementation update — 2026-11-04

`PowerGridManager.Accounting.cs` adds shared registration and cached per-island totals. `CitadelSession.Power.cs` owns source lifetime/config subscriptions after health initialization; `CitadelPowerConfig`/`DefaultCitadelPower.tres` supply a configurable provisional Output=100. F4 reads global diagnostic sums and local totals. Sources persist as derived registrations, not a saved energy balance. No frame callbacks or power gating. See [shared guide](Shared/README.md) for exact API, lifecycle and validation limits. The PW0 audit below remains design context; PW1-only statements describe that original phase, not a reason to reimplement it.

---

# Electricity networks — PW0 audit and PW1 foundation

**2026-11-04 · dev · PW0 Complete; PW1 Source Implemented (owner reports working); PW2 Source Implemented / Awaiting Owner Validation.**
PW0/PW1 and the next PW2 pass are now source implemented. Existing towers remain fully operational.
PW2 adds Citadel output and shared supply/requested-demand accounting; shortages, generator buildings,
consumers and relays remain later phases.
Follow `NOTES/CodePreferences.md`: brief source checks, one basic build if readily available;
extended gameplay/visual/performance acceptance stays with the owner.

## Agreed gameplay direction

- Each edge-connected eligible terrain landmass is one local power grid. Diagonals never join.
- The **Citadel supplies configurable basic generation** so the player can establish an opening
  defense before adding generators. Output is continuous while the objective is available/alive,
  not limited to active waves like its independent Credits production. The PW2 default is 100 provisional power; configure DefaultCitadelPower.tres and tune against future opening demand.
- Solar, Industrial and Fusion are separate build choices sharing generator logic, not successive
  purchased levels of one building. Each may have its own purchased upgrades later.
- PW4 revision: no compulsory wires or fuel logistics. **Per-tower internal capacitor buffers and conductive-distance recharge falloff are now planned**; there is no global stored-energy battery. See authoritative PW4 section at top.
- Manually paired relays join islands transitively; an alternate route can preserve a connection.
- **PW4-C scarcity is resolved by actual shortest conductive source distance first**, not numerical priority. Nearer consumers fill before farther ones; when output runs out distant consumers may receive zero. Each tower still operates on stored charge until depleted. A **future optional** 0–100 per-instance priority value may provide only bounded slight rationing bias if approved; it never replaces distance or modifies combat stats/Credits.

## PW0 source audit and integration decisions

| Existing owner | Finding and integration boundary |
| --- | --- |
| `WORLD/GRID/WorldGrid.cs` | Already caches terrain independently of occupancy and emits `TerrainRebuilt`. Add an independent conductivity cache/revision; never derive electricity from `BlocksEnemies` or `CanBuild`, and never trigger A* for power-only changes. |
| `LEVELS/Runtime/WorldMapLoader.cs` + `WORLD/World.tscn` | Loader mounts `Level` first. Grid initializes before the one `PowerNetworks` node, which reads initial cached data and subscribes to explicit terrain rebuilds. All catalog battlefields share this scene; raw map-only F6 does not run gameplay services. |
| `WORLD/TERRAIN/TileSets/` | Append optional Boolean `conducts_power` without renumbering existing fields, sources or atlas cells. All current painted gameplay terrain conducts except Lava. Decorations and empty space are excluded. Existing missing-field custom palettes default to painted land; custom insulating tiles should explicitly author false. |
| `TOWERS/Shared/Registry/TowerRegistry.cs` + `TowerBase` | Existing membership, destruction, work and spatial events are future registration inputs. PW1 does not register individual buildings, scan all towers, add per-tower nodes or change placement rules. |
| `WORLD/OBJECTIVES/Citadel/` | Future starter-generation settings/registration live here and contribute through the same producer API as generators. Support every authored Citadel and use existing stable objective IDs. Health initialization occurs in CitadelSession after World startup; register only after that initialization, remove output on death/removal, unsubscribe on teardown. |
| `TOWERS/Shared/Conditions/` | `TowerDisableReason.NoPower`, `ApplyDisable` and `RemoveDisable(handle)` already exist. PW4 owns one condition-based handle per affected consumer, removes only that handle, and preserves EMP/heat/upgrading. No second disable system. Audit producer/consumer work paths for `CanOperate` before enabling shortages. |
| `SYSTEMS/Saving/` + feature-local `*.Saving.cs` | No PW1 save schema additions: topology is derived from authored terrain. Future priorities/relay peer GUIDs are durable inputs. Rebuild registrations/allocation before unpausing; treat manager-owned NoPower as derived state rather than restoring a stale handle or a native instance ID. Preserve original research/reward ownership. |
| `DEBUG/PerformanceOverlay.cs` | Add two read-only F4 rows: cached counts/rebuild timing, plus hovered cell/region size. Use the existing throttled refresh, no new process loop or full-map overlay. |

### Terrain and structure membership policy

WorldGrid reads Passable → Interactive → Impassable. The winning gameplay tile's `conducts_power`
value wins, independently of movement/build flags. Therefore a Foundation or rock can conduct
while blocking enemies; Lava over floor is insulating, but an Impassable wall over Lava wins.
Portal/Citadel reservation and tower occupancy never create or remove conducting terrain. Art,
terrain-edge masks and path diversity do not affect electricity.

PW1 exposes `RegionAt(cell)` and `RegionForFootprint(anchor,size)` only; live source/consumer
membership comes later. The future building rule is that **every footprint cell must belong to
one conducting island**. Buildings must not create bridges over empty/nonconducting cells.
PW1 adds no placement rejection. A future producer with no valid island contributes no power
and needs a clear diagnostic. Tower positions are footprint centers; authored Citadel/Portal
roots anchor the upper-left footprint cell, so future registration must use the correct conversion.

Regions are numbered deterministically from top-left seed order, starting at 1; 0 means no island.
IDs can change after edits/merges/splits: cache them only alongside the topology revision and never
serialize them. In PW1, island and electrical-network identities coincide. PW5 can combine these
local islands through a small relay graph without replacing or duplicating terrain detection.
Map dimensions are startup configuration; changing them requires a level restart, as before.

### Demand/shortage decisions for PW2–PW4

- Baseline IdlePowerDraw and ActivePowerDraw belong to each type's non-level config; active draw
  means **total operating demand**, not idle plus active. Purchased levels do not implicitly alter
  draw; effective research adjustments must remain separate from immutable base settings.
- Define an event-driven requested activity/reservation state before enforcing power. An unpowered
  weapon must not alternate idle/active demand merely because NoPower prevents its next shot.
  The first baseline may reserve active demand for ready combat consumers while a wave is active;
  decide actual per-capability wake/idle rules before PW4, rather than polling every shot/tower.
- Revised PW4-C: allocate **energy delivered into per-tower buffers** from a finite per-network source budget by nearest **actual conductive-path distance first**; at equal distance distribute scarce energy proportionally, with stable saved identity only for deterministic remainder handling. Distant consumers can receive zero. NoPower only after stored energy runs low, with hysteretic restart. The optional future priority bias stays inactive. Keep source/activity events coalesced; no per-frame sort, recursive allocation callbacks or oscillation. See the authoritative PW4-C decision at top.
- No circular startup dependency: Citadel/generators do not require their own grid power to produce;
  relay connectivity is passive and does not disappear merely because its network has a shortage.
- Construction and paid upgrades retain current clocks initially. Whether unfinished/busy/EMP/heat
  states draw idle or zero power, and which utility/production buildings consume, needs explicit
  per-capability agreement before consumers are switched on. Do not silently suspend builder work.

## Performance and lifecycle contract

PW1 performs one iterative O(width × height) flood fill at startup and only after a changed
conductivity snapshot. A private queue and labels reuse two int arrays; no recursion, AStar,
node-per-tile, node-per-island, per-enemy or per-tower processing. At 500×500, those arrays consume
about 2 MB combined; WorldGrid adds a Boolean mask and a temporary comparison copy during explicit
terrain rebuilds. Island-size storage is one integer per island. These are memory estimates, not
measured frame timings.

WorldGrid's existing explicit rebuild still reads terrain; unchanged conductivity does not cause
another island flood fill. Occupancy changes never request one. The manager has processing disabled
and unsubscribes on exit. F4 reads O(1) cached counts and one cell lookup at its existing refresh
interval; hidden F4 stops that sampling. `LastRebuildMilliseconds` measures the topology pass only,
not the entire WorldGrid rebuild or GPU work. Full map rescans on an explicit edit can still hitch
at large sizes; incremental/chunk work should follow evidence, not speculative machinery now.

Later allocation must be event-driven/coalesced and proportional to affected networks/consumers;
future relay changes rebuild only network unions, not terrain islands. Never scan enemies for power.

## Phases and acceptance

| Phase | Scope / owner | Status / owner checks |
| --- | --- | --- |
| **PW0** | This audit, terrain/membership decisions, folder ownership and staged contracts | Complete; remaining balancing/UI decisions above are explicitly deferred. |
| **PW1** | `Shared/PowerIslandMap.cs`, `PowerGridManager.cs`, WorldGrid conductivity, appended palette metadata, shared scene attachment and F4 diagnostics | Source implemented; check separated/diagonal land, bridges, erasure, Foundation/Lava/wall precedence, unchanged rebuilds, occupancy and level teardown. No generation or shutdown. |
| **PW2** | Shared producer/demand accounting + configurable Citadel baseline registration | Source implemented / owner validation pending. DefaultCitadelPower.tres Output=100 provisional; event-driven source lifetime, per-island cached totals and F4 accounting. No actual consumers/outages yet. Check multiple Citadels, isolation, death/removal and a new Save/Load. |
| **PW3** | Shared generator/config in `Shared/`, Industrial scene/config/art in its type folder | Source implemented / owner validation pending. `industrial_generator` in Economy, provisional 250 output / 300 Credits / 5s / 1200 HP / 2×2, assembly stages, work/disable/lifetime registration and existing checkpoint reconstruction. |
| **PW4-A–D** | Tower buffers and charge/use (A); cached distance recharge (B); **distance-first** finite scarce-energy allocation (C); Inspector stored-energy bar/rates (D) | **PW4-A/B source implemented / owner validation pending; C and remaining D planned.** Optional slight priority weighting is deferred. Protect existing topology and generator output. |
| **PW5** | Non-generating local distribution booster plus optional manually paired relays bridging isolated islands | Planned. Boost local recharge efficiency without creating energy; preserve finite shared production and safe network merges/splits. See the PW4 revision above. |
| **PW6** | Solar/Fusion variants, shared status feedback and tuning | Planned. Distinct IDs/configs, one producer implementation; readable capacity/shortage feedback and measured desktop cost. |

Each phase includes its relevant save integration, feature documentation and brief source/build checks.
Do not implement subsequent phases from this table without authorization. Named save slots remain a
separate extension. The current strict dependency-byte fingerprint can reject older checkpoints after
source/resource changes; this pass preserves that guard and does not promise old-checkpoint compatibility.

## Validation record and owner handoff

Focused source/resource checks only in this environment; no .NET/Godot executable is available.
No runtime pass, FPS improvement or visual acceptance is claimed. See `TESTS/Power/README.md`
for the focused fixture and a short in-editor check. Existing authored missions, tiles' previous
gameplay flags, waves, costs, combat and tower operation are preserved.
