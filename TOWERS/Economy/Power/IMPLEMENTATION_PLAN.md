## PW5-A/B — Relay and Transmitter implemented — 2026-11-05

**Source Implemented / Awaiting Owner Validation.** Two distinct Utility entries now reuse the existing tower construction/health/disable/placement/catalog/Inspector and `World/PowerNetworks` owner. PW5-C coverage routes/animation and PW6/new generators/priority slider remain deferred.

| Building | Provisional settings | Role |
| --- | --- | --- |
| Power Relay (`power_relay`) | 150 Credits, 3s, 800 HP, 1×1; 10 conducting-cell coverage, 85% nominal recharge ceiling | Local best-only booster; never pairs or generates |
| Power Transmitter (`power_transmitter`) | 200 Credits, 4s, 900 HP, 1×1; 8 source-distance cost cells per hop | Manually pairs with one Transmitter on another physical island; no local boost |

Each has its own typed `Default*Config.tres`, definition, scene and native SVG finished/core/0–35–70% construction artwork. Tune these provisional settings in its config; existing sources and tower power/combat profiles are unchanged.

### Shared power/cache policy

- Physical PW1 islands remain authoritative. Every footprint cell must be on one physical conducting island. Electricity crosses a gap only through a valid reciprocal, operational Transmitter pair. Work, EMP/disable, death/removal, movement, terrain, config/source changes, manual link/unlink and restore update cached inputs; no new per-building processing or per-frame map/registry traversal.
- Relays use one cached best-efficiency field, populated with bounded four-neighbour searches from eligible Relay footprints. Coverage stays on that Relay's physical island, never traverses Transmitter links and never seeds another Relay. A Relay needs a live source route (including an active Transmitter route after B). No source means zero boost. Take max(original distance efficiency, best eligible Relay ceiling), capped at 100%; overlaps never add. Original source distance still orders scarce supply.
- Active Transmitter pairs join physical islands using a small cached union graph. Source totals remain counted once per physical island and each effective component has **one finite shared service budget**. No relay/transmitter output, global battery, upkeep, throughput cap or arbitrary maximum link length.
- Source distance uses ordinary conductive segments (1 per cell) plus a positive link-hop cost: **max(both endpoints' HopCostCells)**, provisionally 8. The electrical port is the first footprint cell (current buildings are 1×1). Existing PW4-B falloff then applies to total cost. Multi-source Dijkstra runs only on a dirty linked graph; without active links, the original linear BFS is retained. Positive costs prevent zero-cost cycles and preserve meaningful nearer-source-first allocation. Link cost is distance attenuation, not a second energy deduction. A Relay can improve recharge ceiling but cannot remove distance/hop scarcity cost.
- Cached roots/totals, distance/relay arrays, bounded search buffers, edge lists and priority queue are reused. Positive→positive source-output changes update network budgets without rerunning the source field. Dirty topology/link/Relay inputs rebuild on query/service, never each frame. Old elapsed intervals flush before input mutation. Existing capacitor energy, spending/rollback, owned NoPower, heat/ammo, work clocks and pause remain unchanged.

### Controls and presentation

Select a completed operational **Power Transmitter**, click **LINK**, then click an unpaired completed operational Transmitter on a **different physical island**. No generator is required to create the pair. Self/same-island/incomplete/disabled/occupied/missing targets report a reason; another world click can retry. Right-click or Escape cancels; selection/tray/Inspector closure also retires pending linking. **UNLINK** removes both peer IDs. Temporary disable suspends connectivity while retaining IDs; recovery revalidates them.

Relay status: BOOSTING / NO LIVE SOURCE / DISCONNECTED / DISABLED, with cyan live core and dim inactive core. Transmitters report UNLINKED / LINKED / NO LIVE SOURCE / LINK SUSPENDED / DISABLED. One lower-SaveId endpoint draws a thin static cyan line when powered; a retained inactive/unpowered link is subdued amber. Removing/unlinking a pair clears the line. Lines/core/status update only on input/state events; no animation loop. Existing generic disabled tint/triangle still applies. Selected distribution status reserves three lines to avoid status-induced Inspector growth. Consumer ENERGY and PW4-D power details remain intact; tooltip now explains Relay ceilings and hop distance. Richer selection-only coverage/source routes, animated pulses and F4 network UI remain PW5-C.

### Saving and minimal checks

`TowerSaveData.PowerPeerId` is optional (default empty), bounded/typed by existing save validation and stores durable Tower SaveIds. Existing power fields/schema guard are preserved. After **all** buildings/conditions restore, `CampaignSession` resolves reciprocal peer references, safely drops missing/self/nonreciprocal peers and warms effective networks/delivery before unpause. Disabled pairs retain identity and resume when valid; same-island/invalid footprints remain suspended. Destruction/removal clears the surviving peer. Charge is never erased by a dropped link. The existing dependency content fingerprint can reject older checkpoints; do not bypass it.

Minimal source/diff, native resource-reference and SVG XML checks only. .NET is unavailable; **no C# build, Godot gameplay, visuals, FPS or benchmark acceptance is claimed**. Optional existing PowerNetworkChecks now includes narrow source-authored PW5 checks for positive-hop shortest paths/no-source/unlink, real manual pairing, finite remote recharge, two producers counted once, saved peer reconstruction, EMP suspension/recovery/removal, and Relay ceiling/source-loss/fallback. These C# fixtures were not executed here.

Owner: rebuild in Godot .NET, place both Utility types, select/link two islands and inspect remote ENERGY refill, disable/unlink/remove an endpoint, overlap Relays and check original scarcity ordering, then try a fresh between-wave Save/Load. Confirm line/cores, fixed Inspector and construction presentation in Level011. Broader acceptance remains owner playtesting.


---

# PW5 AUTHORITATIVE REVISION — Two separate power buildings (2026-11-05)

**PW5-A/B SOURCE IMPLEMENTED / AWAITING OWNER VALIDATION; PW5-C PLANNED.** This decision supersedes every earlier PW5 plan below or in older READMEs that describes **pairable Power Relays**. PW0–PW4-D are already source implemented; preserve all existing behavior. Codex must implement **only the separately requested phase**, not the whole roadmap. See [Relay](../../Utility/PowerRelay/README.md), [Transmitter](../../Utility/PowerTransmitter/README.md), and [Backlog](../../../NOTES/IMPLEMENTATION_BACKLOG.md).

| Building | Exclusive gameplay responsibility | Player-visible minimum |
| --- | --- | --- |
| **Power Relay (PW5-A)** | Boost *local* rechargeable efficiency along conducting terrain on its own powered island. No inter-island link or pairing. | Cyan live core, dim inactive core and informative selected status; selected coverage/source route in PW5-C. |
| **Power Transmitter (PW5-B)** | Pair with *one other Transmitter* across distinct conducting islands; join real electricity networks. Never grants local recharge boosts. | Actual thin cyan/blue visible energy line between an operational pair in PW5-B; polish in PW5-C. |
| **Citadel / generators (existing)** | Only these create electricity; towers hold their own capacitor energy. | Existing Inspector/diagnostic output. |

Two separate Utility catalog entries, scenes, configs and art folders; recommend stable IDs `power_relay` and `power_transmitter` after checking the catalog. Initial one level each. Construction cost, build seconds, HP/footprint and distribution settings should be conservative, Inspector-configurable and labelled **provisional**, not silently treated as approved balance.

## Shared invariant/architecture contract

1. **One authority:** use existing `World/PowerNetworks`, `PowerGridManager`, PW1 cached four-neighbour islands, PW2/3 producers, PW4-A capacitor/NoPower/Save, PW4-B cached conductive distance, PW4-C **nearer-source-first allocation** and PW4-D energy Inspector. No second manager, global battery, new update loop or per-tower per-frame traversal.
2. **Power conservation:** for each effective connected power network and shared service interval, total allocated recharge is at most the sum of **distinct live sources' output × elapsed game-time**. Relays/Transmitters create zero electricity; joining islands, overlapping coverage and transmitter chains cannot double-count producers.
3. **Charge persists:** disconnection/disabling affects *future refill only*. Do not wipe existing tower capacitors; existing owned NoPower, EMP, heat, work, spend/rollback, hysteresis and pause behavior remain authoritative.
4. **Physical validity:** source, relay, transmitter and consumer footprints must respect existing valid conducting-cell rules. Diagonal, empty space, Lava and unrelated islands never connect except by a **valid active Transmitter pair**. One remote side can be source-less and still receive power; an island does not need its own generator merely to receive a linked supply.
5. **Cached lifecycle:** recompute relevant distribution/network caches only on placement, completion/upgrading, detach/death, disable/recovery, terrain rebuild, producer live-output/availability, link/unlink and restore. No full-map/registry rescan every frame, repeated sorting or drawing-dependent energy logic. Reuse existing cached snapshots/arrays.
6. **Saving:** peer references are durable **Tower SaveIds** (not runtime instance IDs or transient region labels). Restore all buildings before resolving and verifying reciprocal links, then rebuild effective network and derived recharge before unpause. Existing power save fields and content-hash guard remain unchanged; broken peers fail safely.
7. **Allocation ordering:** PW4-C remains primarily distance-first. A local Relay increases a consumer's possible recharge **ceiling**, not its scarcity priority or its generator's output. PW5-B must specify/cache a deterministic meaningful source-to-consumer cost over conductive segments plus transmitter hops. No costless cycle/teleport ranking, no automatic energy priority slider.

## PW5-A — Power Relay, local booster ONLY (Codex pass 1)

- New small buildable `TOWERS/Utility/PowerRelay/` scene + definition/config/art, normal tower work/health/disable/selection/Inspector and saving lifecycle. No pairing or Transmitter.
- Once complete and operational, Relay becomes an eligible **non-generating secondary distribution anchor** only when its full footprint is on an island with a *reachable live Citadel or generator*. A disconnected or source-less Relay never boosts. Show understandable BOOSTING / NO LIVE SOURCE / DISCONNECTED / DISABLED states, and lit versus dim core; use shared disabled tint/badge when appropriate.
- Configurable range measured in **four-neighbour conducting cells** and capped improvement to PW4-B effective recharge rate (never over nominal per-type rate). Prefer shared cached relay-distance field. Multiple Relays use the **best eligible ceiling**, not additive bonuses, and must not endlessly relay-hop to magnify efficiency. The original producer-distance route is still valid fallback.
- Retain **original source distance as PW4-C scarcity ordering**; boosted far towers do not jump in front of nearer towers simply by building a Relay. Actual refill still obeys existing source budget and request/headroom. Don't add relay upkeep/fuel/charges without later approval.
- Acceptance (focused): near/far same-island recharge, relay on source-less island, terrain gaps/diagonals, multiple overlapping relays, source death, work/EMP/disable, destroy/rebuild, pause, shortage conservation and a new save. **Done:** eligible far towers have higher *possible* refill; without Relay the PW4-B falloff immediately returns and stored charge remains.

## PW5-B — Power Transmitter, cross-island ONLY (Codex pass 2, separate approval)

- Add distinct buildable `TOWERS/Utility/PowerTransmitter/` with its own ID/art/config/scene; no local booster behavior and no shared Relay pair control.
- Selected transmitter Inspector: **Link** then choose an eligible completed second Transmitter on another conducting island; **Unlink** to remove. Exactly **one reciprocal peer per transmitter** initially. Reject self, same-island, missing, dead, incomplete or occupied target with useful feedback. Do not auto-pair by proximity.
- Operational pair adds an undirected edge to a **small cached graph of physical islands**. One/both sides may produce; both generator-free means zero supply. Join budgets once per effective graph component; power reaches remote island through actual viable endpoints and valid conducting cells. Multiple pairs/chain paths must not duplicate output or form infinite-distance discounts.
- Temporary disable/EMP **suspends** a retained player pairing without deleting stable peer IDs; construction/destroy/removal/unlink removes active connectivity. Return to operation revalidates pairing. Save peers with stable Tower SaveIds; rebuild network after restore *before gameplay resumes*.
- Define deterministic link-hop attenuation/segment distance policy in typed Inspector configuration; retain the PW4-C distance-first intent. No first-pass throughput/fuel, arbitrary link range, purchased tiers or global battery without separate design approval.
- **REQUIRED in B:** visible thin subdued blue/cyan straight energy link between an active pair; one renderer per pair, clear inactive/broken/unpowered indication, no constant clutter or duplicate drawings. Animation and richer feedback are for C.
- Acceptance: an unlinked unit does nothing; manual pair crosses vacuum; a generator-free receiving island charges correctly; both producers counted once; unlink, source death, EMP, destruction, alternate links, save/retry, pause and budget conservation behave predictably.

## PW5-C — power-link visualization/UX polish (Codex pass 3, separate approval)

- When Relay is selected, show translucent cyan **actual reachable conducting cells** within its booster range, *not* a misleading Euclidean circle spanning gaps. Show thin dashed shortest conductive path to **nearest live source**; label `Nearest live source — island supply shared` because a network may pool multiple generators. Reuse/cache predecessor data or compute only when selected/invalidated, never each frame.
- Transmitter links: build on B's static line with a narrow cyan core, faint halo and occasional controlled pulses. Active vs suspended/unlinked status is obvious; minimize overlap with combat beams. On selection or F4, expose partner identity and effective island network info using existing throttled diagnostics.
- Respect existing selection visuals, compact fixed-size Inspector, shared disabled badge, gameplay pause, viewport culling and bounded visual update rates. Visual effects do not affect energy calculation and must remain cheap on big Level011.
- Done when player can understand exactly where **local boosting** applies and exactly which **different islands** are electrically linked, with no FPS-heavy per-frame route drawing.

## Deferred (not PW5 work)

PW6 Solar/Fusion, fuel/upkeep, premium relay tiers, persistent power beams from Relay to generator, arbitrary remote wireless power, mandatory transmitter throughput caps or maximum length, power-priority slider, additional autoloads and broad generator/power rewrites. Balance values and optional link losses/range require later owner approval.

---

## Offensive and Repair power rollout — 2026-11-05

**Source Implemented / Awaiting Owner Validation.** All nine implemented offensive types plus Restoration Tower now use the existing `TowerPowerConsumer`, `PowerCapacitor`, `PowerGridManager` and owned `NoPower` handle. No additional power clock, per-tower polling, source, relay or priority slider is added. Existing selected ENERGY bar and PW4-D actual refill/limit/use/distance/status details automatically apply to every consumer. Earlier Battle-only phase notes below are historical.

Shared projectile/volley/pulse funding is atomic; continuous Laser/Repair funds only the deliverable duration. Paid active time excludes idle draw and power is published after delivery. Per-type configurable resources use modest provisional values with Battle unchanged; see the [consumer guide](../../Shared/Power/README.md) for the complete rate table and accounting policy. Existing combat/repair/heat/ammo/work/upgrades and checkpoint owners remain in place; no schema change. Source/resource checks only; .NET unavailable and Godot owner validation pending. No PW5/relays, new generators or slider.


---

## PW4-D — selected power feedback — 2026-11-05

**Source Implemented / Awaiting Owner Validation.** The existing right-column ENERGY current/capacity bar remains. A compact three-line **PowerDetails** label in the Inspector Main column shows **actual Refill +X/s**, **distance-limited Limit +Y/s**, **actual Use −Z/s**, conductive distance, efficiency and connection state. Tooltips explain measured versus potential rates, authored idle/total-active draw and the recovery threshold. Full, charging, awaiting refill, supply-limited, connected/starved, disconnected/no-live-source, recharge-disabled and work-paused are distinct. Recovery is shown separately; no incoming supply does not erase stored energy.

`TowerPowerInspection.cs` owns a typed derived snapshot and service samples; `PowerInspectionText.cs` owns formatting. Successful funded attack payments and settled idle energy losses are counted once; rejected ammunition and config capacity clamps are not consumption. Rates use the existing shared game-time service (default 0.25s), not an instantaneous frame estimate. Full/work/disconnected states suppress stale incoming rates; one final zero sample after activity ends lets the existing shared service return to sleep. Restore/detach resets derived samples, with no save-schema change. Pause freezes both power and its displayed samples.

UI uses the existing selected-only/coalesced RuntimeChanged path and hides for build previews/non-capacitor towers/closed inspection. No new UI or per-tower processing loop, source scan, stat-grid reconstruction or allocation change. PowerPriority remains inactive with no slider; PW5 relays and additional consumers are not implemented. Existing Inspector dimensions/fonts and targeting controls are preserved; the new Main label can wrap, so desktop geometry at small resolutions remains an owner check.

Focused PowerAllocationChecks now covers typed starved/full/disconnected samples, actual-versus-ceiling formatting, paid consumption and zero-rate dormancy. Authored but not executed here. Source/scene checks only; Godot/.NET unavailable, no layout/runtime/benchmark claim. Owner: inspect a selected Battle Tower charging, firing, full, behind other near consumers under shortage, without a source, during work/pause, then switch/close/reselect and check a fresh Save/Load. Check Heat/Ammo/ENERGY and the new detail lines at normal and small window sizes.

---

## PW4-C — distance-first scarce-supply allocation — 2026-11-05

**Source Implemented / Awaiting Owner Validation.** The existing shared capacitor service now allocates finite island generation in ascending cached conductive-path distance, breaking ties by ordinal Tower SaveId. Every request is capped by eligible elapsed recharge at its PW4-B ceiling and remaining capacitor headroom. Unused energy goes immediately to later requests; zero supply/route/ineligible state delivers zero. Stored energy, shot spending and NoPower recovery remain independent. No priority slider or bias, new consumer, relay, balance change or save schema is introduced.

Allocation order is cached across service ticks. Membership, consumer remapping or delivery revisions invalidate it; a single reused list sorts only when dirty, with no delivery/BFS work inside comparisons. Sparse per-island budgets, request snapshots and list capacities are reused. Existing source/topology/config flush boundaries are retained; work eligibility transitions also flush the prior interval before changing charge state. Pause advances neither the shared clock nor allocation. Credits finish before lifecycle/publication callbacks run.

`TowerPowerConsumer.LastDeliveredRefillPerSecond` reports actual credit divided by the **latest completed service interval**, independently of `RechargeCeilingPerSecond`. It is derived, resets on restore/detach, and is not saved or presented as new Inspector UI in C. It is a historical service sample (can remain nonzero when the service sleeps after filling), not an instantaneous generation promise. PW4-D owns final live rate/status presentation. `AllocationOrderRebuildCount` permits a focused cache check without adding a debug UI poll.

Equal-distance ties are deterministic, not fair-sharing: an earlier SaveId can consume all remaining supply while it keeps requesting; later equal-distance towers wait until output/headroom permits. A bounded equal-distance fair policy remains a future decision if playtesting demonstrates a problem. Farther groups do not jump ahead.

Focused `PowerAllocationChecks` is called by the existing optional PowerNetworkChecks fixture: scarcity/abundance, leftover headroom, finite conservation, dormant priority values, stable saved identity, actual-versus-ceiling, cache reuse, source loss/restoration, disconnected stored-energy spending and pause. Authored but not executed here. Source review only; .NET/Godot unavailable, no benchmark or broad tests. Owner: rebuild, test a low-output source with near/far Battle Towers, let near buffers fill, then inspect source removal and a fresh Save/Load. Existing content-fingerprint compatibility guard remains unchanged.

---

## PW4-C revision — DISTANCE FIRST under supply shortage (2026-11-05)

**AUTHORITATIVE OWNER DECISION — PW4-C SOURCE IMPLEMENTED / AWAITING OWNER VALIDATION.** This supersedes older language below specifying power priority as the primary allocation order. PW4-A internal capacitors and PW4-B cached conductive-path recharge falloff already exist; **PW4-C must allocate scarce generated power preferentially to physically closer towers (measured along conductive tiles)**. The owner's possible tower-by-tower priority control is **deferred** and, if later approved, may only *slightly* favour a tower rather than dominate distance.

### Intended behavior

1. Keep PW4-B's four-neighbour conductive-path distance and per-consumer effective recharge ceiling. Distance is NOT direct Euclidean range through space/Lava. The Citadel and active generators are source anchors; multiple sources still share a finite island budget. A connected tower may have a positive recharge ceiling but **receive zero actual energy** when the island cannot supply it.
2. Each shared power-service interval has a strict budget: sum of usable generator/Citadel output × elapsed time. From that finite amount, satisfy **nearer requesting buffers before farther requesting buffers**. Sort/allocate by ascending cached distance from nearest live source, with deterministic stable SaveId order for ties. Use PW4-B's cached distance rather than one path query per tower. Keep PW4-A's shared service; do not scan or resort every rendering/physics frame.
3. A consumer's request is no greater than `min(capacity - storedEnergy, effectiveDistanceRechargeRate × elapsed)`. A full/paused/unfinished or otherwise ineligible tower must not reserve output; pass unused energy to the next requesting tower. A close tower that needs less than its ceiling must not waste remaining supply. No positive route or no producer means zero delivered energy.
4. **Shortage example (illustrative, not balance):** Island makes 10 energy/s. Three partially empty towers' *already distance-adjusted* desired recharge rates are 5/s each: near distance 3, middle 12, far 30 cells. Deliver 5/s near, 5/s middle, **0/s far**. As near fills, more output becomes available to distant towers. If supply covers all requests, all towers refill at their respective PW4-B ceilings. Keep actual delivered power distinct from theoretical maximum recharge.
5. **Capacitor remains independent:** zero incoming refill does not erase stored energy; towers continue fighting until energy is too low and the existing owned `TowerDisableReason.NoPower`/restart threshold takes over. No automatic damage/fire-rate debuff just for distance. Keep the strict conservation invariant: total credited refill ≤ available source energy, no double-counting, no hidden island battery. Source loss, EMP, reattachment and save restore must settle old intervals and recalculate as already designed.
6. **Equal-distance tie policy:** stable saved-identity ordering is an acceptable deterministic first pass. If tests show starvation among *equally distant* towers, document/consider a bounded fair split or rotating tie order, without allowing more distant groups to routinely outcompete nearer ones. Avoid unapproved routing/flow solvers or costly per-generator energy simulations.
7. **Priority slider later, optional:** The existing saved PowerPriority 0–100 (default 50) is compatibility metadata only in PW4-C, not active allocation. Later, if specifically approved, offer a **small clamped distance-equivalent offset or other bounded bias**, with shared Inspector tuning and examples showing distant towers cannot simply jump ahead of every close tower. Do not add an active priority slider in PW4-C or assume one must ship in PW4-D.
8. **PW4-D UI:** preserve the existing internal ENERGY current/capacity bar. Add **actual delivered +X/s**, **possible distance-ceiling +Y/s**, **consumption −Z/s**, distance/efficiency, and a useful `connected but starved` versus `disconnected` distinction. Do not show priority as if it actively changes allocation before the future feature is approved.

### Codex scope / focused checks

**PW4-C only:** replace the present provisional proportional allocation in `PowerGridManager.Capacitors.cs` with distance-first bounded distribution, retaining PW4-B distance cache and PW4-A energy spending. Test focused cases: abundant supply, near-first scarcity (far gets zero), full near buffer freeing output, stable equal-distance ties, source removal/restore, disconnected island, stored energy surviving starvation, save/load and exact energy conservation. Avoid any new per-tower `_PhysicsProcess`, per-tick BFS, repeated allocations or full-map work. Keep performance simple. **Do not implement PW4-D, PW5 or slider bias in the same pass unless separately asked.**

**Historical correction:** Any older text below that says "descending PowerPriority", "priorities govern refill allocation", "PW4-C priority scheduling", or "power priority slider required in PW4-D" is superseded by this section. The latest required phase is **PW4-C distance-first scarce-supply allocation**; the later optional mild priority preference is a separate decision.

---

## PW4-B — conductive-distance recharge — 2026-11-05

**Source Implemented / Awaiting Owner Validation.** The existing PowerGridManager now caps recharge by cached shortest **four-neighbour conductive distance** to a positive-output Citadel/generator footprint. Source footprints seed all valid cells at distance zero; a consumer receives at its nearest valid footprint cell. Both footprints must lie wholly on one island. Vacuum/Lava/diagonal gaps, unassigned footprints and zero-output sources cannot invent a route. Stored energy, capacity, spending and owned NoPower hysteresis are unchanged.

Shared Inspector config: `TOWERS/Economy/Power/Shared/DefaultPowerDelivery.tres`, assigned to `World/PowerNetworks.DeliveryConfig`. Defaults are **4 full-efficiency cells**, **12 falloff cells**, **0.10 minimum efficiency on reachable routes**. Curve: `max(floor, 1 / (1 + max(0, distance - full) / falloff))`; no reachable source always yields zero. Thus distance 4 → 100%, 16 → 50%, 40 → 25%, with the 10% floor only at long connected distances. Actual credited recharge is still bounded by the shared finite island budget/headroom; this is a ceiling, not promised delivery. Battle's existing 10/s nominal refill becomes 5/s at distance 16, with its original 100 capacity and 12/s active draw retained. No extra consumers opted in.

**Performance:** one reusable multi-source BFS field, lazily rebuilt/coalesced on topology or active anchor changes; no per-tower AStar, per-frame flood fill or source scan. Positive→positive output edits, demand changes and curve edits do not rebuild distances. Cached consumer distance/efficiency refresh only on placement/topology/distribution revision; regular service retains its modest cadence and asleep-when-idle behavior. Two retained int arrays add about 2 MB at 500×500 cells (memory estimate, not a benchmark). Dirty snapshots settle the previous interval against previous island labels/settings before switching, preserving time/budget accounting. Cache diagnostics expose DistanceRebuildCount, LastDistanceRebuildMilliseconds and PoweredCellCount.

Consumer API: `ConductiveDistanceCells` (-1 unreachable), `DeliveryEfficiency` and `RechargeCeilingPerSecond`. Distance/efficiency are derived, never saved; the existing checkpoint owner warms final delivery after all source states restore and before unpause. Save schema/charge/priority remain unchanged; the existing fingerprint may reject pre-change saves. **PW4-C priority scheduling and full PW4-D rate/efficiency/priority controls remain planned; PW5 relays are not implemented.**

Source/resource review only: execution environment unavailable, no build/runtime/benchmark claim. The optional PowerNetworkChecks fixture now includes a tiny detour/diagonal/multi-source distance check and two actual near/far capacitors on a transformed grid. Owner: rebuild, inspect near/distant Battle refill, source removal/EMP, isolation and a fresh Save/Load. Earlier dated phase notes below are historical.

---

## PW4-A implementation — 2026-11-05

**Source Implemented / Awaiting Owner Validation. PW4-A only.** [Consumer guide](../../Shared/Power/README.md) documents the implementation and exact policy. Battle Tower is the only initial consumer: **100 capacity / 10 energy-per-second refill / 0 idle / 12 total active / empty initial fill / 15% restart**. A shot prepays active rate × shot interval before emission, with ammo-rejection rollback; no wave-based active spending. Timed construction/upgrades preserve charge and neither charge nor drain; built heat/EMP/NoPower disables permit charging, without idle spend. Non-power towers remain unchanged.

The existing PowerGridManager has one asleep-when-idle shared service (default 0.25s), full-efficiency same-island delivery and provisional proportional sharing capped by production × elapsed game time. No grid bank. Save state contains charge/priority/recovery/paid active remainder, not authored capacity/draw or derived owned NoPower handles. Older missing power fields read as empty; the dependency fingerprint guard remains authoritative. Compact energy runtime text is included; the selected power **bar stays PW4-D**. Priority is persisted but unused until C. Distance, relays and the final deterministic priority allocator are not implemented.

Validation: source/resource/diff review only; .NET unavailable, no install, suite, gameplay or benchmark run. Owner: rebuild, check completed Battle charge/fire/deplete/recover, source loss/isolation, work + heat/EMP coexistence and a fresh Save/Load. The rates are provisional and were reported before implementation; existing combat stats/source output/campaign waves were not tuned.

---

# PW4 REVISED DESIGN — distance-based charging + per-tower capacitor buffers (2026-11-04)

> **AUTHORITATIVE UPCOMING DESIGN; NOT YET IMPLEMENTED.** Supersedes the old PW4 "full power or immediate shutdown" allocation proposal and the old assumption that connected islands deliver power at identical efficiency. **PW0–PW3 remain implemented and unchanged**: PW1 conductive islands, PW2 Citadel generation 100 plus island accounting, PW3 buildable Industrial Generator 250. Treat all figures in this new PW4 section as provisional playtest values until approved. Codex: implement **one pass at a time**, only when asked. Do not silently implement PW5 relays as part of PW4.

## Player-facing goal

The entire four-neighbour **conducting island** still shares a finite generation budget, but charging a tower's **internal power buffer** becomes slower with greater **conductive-path distance** from a generator/Citadel or a powered relay. A far tower is **not capped at 30% battery**: during downtime it can eventually bank **100% energy**, provided a viable power source remains and its recharge exceeds any idle consumption. During sustained combat the tower spends stored energy faster; a distant tower can run low and must recover. A nearby tower refills more quickly and sustains combat longer. No generic damage/fire-rate debuff simply because a building is far away.

- **Sources:** Citadel and Industrial (later Solar/Fusion) produce a configurable *amount of energy per second* into their own connected network; numbers 100 and 250 are currently provisional rates for PW4's accounting interpretation. Production is continuous while the source can operate. Power is **not** automatically banked for the island when no consumer requests it.
- **Consumers:** Individual opted-in towers/buildings have a finite capacitor / battery with `Capacity`, `CurrentEnergy`, maximum `RechargeRatePerSecond`, `IdleDrawPerSecond`, `ActiveDrawPerSecond` and per-instance `PowerPriority` (0–100, default 50). The active draw is the total active drain, **not idle + active**. Absent a capability, a tower continues normal operation and shows no power bar.
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
- On each modest, shared **time-integrated power service tick** (e.g. every 0.2–0.5s as a provisional target), compute each eligible consumer's **desired refill** from recharge ceiling × step and headroom, then allocate no more than network production × step. Allocate by **ascending PW4-B cached conductive distance**, stable SaveId tie-break, with optional bounded priority bias deferred; nearer eligible buffers first and unused energy passes to farther ones. An already-full buffer should not reserve power.
- Recharge does not guarantee that a tower will stay online: actual spend depends on attacking/idle state. **NoPower depends on buffer sufficiency/restart hysteresis**, not on simple network-demand > supply. A heavily buffered tower may operate throughout a temporary shortage, then drain over sustained fire.
- Avoid paradoxical allocation loops: a tower that runs out of charge should still request refill when NoPower prevents attacks; a generator does not depend on its own grid energy; relay connectivity must not vanish just because a consuming tower browns out.
- **Saving:** add current stored energy, charging/operational settings that are truly per-instance (priority, initial state if applicable) to the existing campaign checkpoint owner when PW4 is implemented. Capacity/nominal draw remains authored config, not a copied permanent save. Rebuild distribution/allocations from live sources and conductivity **before unpausing**; restore NoPower as a *derived* condition, not a stale serialized handle. Preserve wave ownership, ammo stock, heat, Credits and current saves' compatibility policy.
- **Starting balance:** use modest per-type capacity/idle/active/recharge defaults and expose them in Inspector; consider starting with Battle Tower as the consumer baseline and opt in other towers deliberately. Do not accidentally disable all early defenses. Decide when energy spends (actual shot events vs sustained attack state) before implementing. Classify utility/economy building draw deliberately rather than applying one rule to everything.

## PW4 subpasses — Codex implementation order

| Subpass | Scope / location | Done when |
| --- | --- | --- |
| **PW4-A — Tower capacitors** | `TOWERS/Shared/Power/` opt-in consumer config/runtime; absolute capacity, current per-instance charge, idle/active expenditure, event-driven activity, existing `NoPower` handle & thresholds. Basic local recharge can initially use constant full efficiency supplied by the single grid manager as a temporary baseline. | One selected test tower can charge while idle, sustain fire from buffer, deplete/recover, and save/load charge; unchanged non-power towers and heat/EMP gates still work. **Do not require relays or distance yet.** |
| **PW4-B — Distance-limited recharge** | `TOWERS/Economy/Power/Shared/` cached generator/Citadel anchors, four-neighbour conductive distances, editable falloff/efficiency; expose a per-consumer current delivery ceiling. | Near and distant towers both eventually fill while idle; far tower charges slower and drains earlier during continuous fire. Source death / isolation updates efficiency; no repeated pathfinding every tick. |
| **PW4-C — Distance-first supply allocation** | Extend existing `PowerGridManager` accounting with finite time-integrated allocation favouring nearer towers by cached conductive-path distance; no active priority slider yet. | Total credited refill never exceeds generated energy for each interval, even across many towers or overlapping generators. Shortages reduce **recharge**, and stored energy continues to be usable until depleted. No flicker/over-allocation. |
| **PW4-D — Selected tower Inspector** | Reuse selected Inspector's Heat/Ammo bar design; display **Stored Energy / Capacity** as primary bar (not full/empty grid service), live **actual Recharge +X/s, possible Recharge +Y/s, Drain −Z/s, Distance and Efficiency %**; clear NoPower/charging state and zero-draw/zero-capacity policy. | Player can distinguish **full but slowly recharging** from **empty/no generation** and understand actual received recharge versus distance-limited potential. UI refreshes on meaningful/capped state changes, not a custom per-tower UI frame loop. |
| **PW5-A Power Relay** | Local-only boost to rechargeable efficiency on its own powered island. | Does not pair or create power. |
| **PW5-B Power Transmitter** | A separate Utility building, manually paired to another Transmitter on a different island. | Visible basic link, real finite network union and checkpointed reciprocal peer IDs. |
| **PW5-C Visual polish** | Selection-only conductive Relay coverage/source route, animated link and Inspector/F4 feedback. | Distinct roles clearly shown without costly per-frame map traversal. |

**PW4-A/C dependency note:** PW4-A may introduce a tiny provisional single-network recharge service for integration, but **PW4-C owns authoritative finite production allocation**. Don't accidentally ship an unlimited refill loop as final PW4-A; wire the same owner/seam to be replaced/extended by PW4-C. PW4-B's efficiency is a capped ceiling, not a promise of delivered power. Keep these phases focused; don't build a new parallel power manager.

## Save & UI examples

- Tower inspector example: `ENERGY 64 / 100` (buffer), `CHARGING +3.0/s`, `FIRING −12.0/s`, `EFFICIENCY 30%`, `PRIORITY 50`. During combat the actual net is −9/s. A full buffer displays full even if delivered rate is currently near zero.
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
- **Conductive distance** now governs **refill allocation** when source production is insufficient; saved PowerPriority is reserved for a future mild preference bias. Towers keep operating from their own buffers until low/depleted; a threshold-based NoPower condition then gates operation. Priority never modifies cached combat stats or the Credits wallet.

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
- Revised PW4: allocate **energy delivered into per-tower buffers** within finite per-network source budget by descending PowerPriority / stable saved identity; local refill ceiling depends on cached conductive-path distance. NoPower only after buffer runs low, with hysteretic restart. Keep source/activity events coalesced; no per-frame sort, recursive allocation callbacks or oscillation. See the authoritative PW4 section at top.
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
| **PW4-A–D** | Tower buffers and charge/use (A); cached distance recharge (B); distance-first finite supply allocation (C); Inspector stored-energy bar/rates (D) | **PW4-A/B/C/D source implemented / owner validation pending.** Follow the authoritative PW4 subpass table above. Protect existing power topology and generator output. |
| **PW5-A/B/C** | **Two building types:** local-only Power Relay (A), manually paired cross-island Power Transmitter + basic visible link (B), advanced connection/coverage visuals (C). | **A/B source implemented / owner validation pending; C planned.** See canonical PW5 revision at TOP, which supersedes combined-Relay descriptions below. Implement one pass per request. |
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


