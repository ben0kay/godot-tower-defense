# WORK — First complete ammunition pipeline (Codex implementation task)

> **Status: CORE HELPER CLASSES IMPLEMENTED / GAMEPLAY PIPELINE NOT YET IMPLEMENTED.** This is an actionable implementation task for Codex, **not** a record that the feature exists. Implement in small, testable stages within the same work order and update the checkboxes only after verifying each result. Do not treat this as permission to introduce the entire power/research/construction roadmap.
>
> **Important change to older planning READMEs:** **Both delivery legs must use visible transport drones:** **Ammunition Factory → Ammunition Depot → ammunition-consuming Tower**. Previous notes saying factory-to-depot transfers are instant/logical **are superseded for this pass**. Update those notes to match the implemented behaviour when the feature is working.

## Implemented foundations — CODEx HANDOFF (2026-10-15)

**This foundation has already been coded and committed as `dd0fe708`. Reuse it, do not re-create equivalent implementations.** The following classes are **pure foundation helpers**, not yet attached to scenes/towers/drones.

- `TOWERS/Shared/Ammunition/AmmoInventory.cs`: `new AmmoInventory(capacity,initialAmount=0)` with `Amount`, `Capacity`, `FreeSpace`, `Changed`, `TryAdd(rounds)`, `TryConsume(rounds)`, `TryTransferTo(destination,rounds)`, `TrySetCapacity(capacity)`. Atomic full-quantity transfers only. Invalid quantities/overfill/overspend fail unchanged. Shrinking capacity below stored ammo is refused; choose an explicit cap policy for upgrades.
- `TOWERS/Shared/Ammunition/AmmunitionMasterConfig.cs` and `DefaultAmmunitionConfig.tres`: global exported `ResupplyThresholdFraction=0.10` with `NeedsResupply(current,max)` (at 1,000 maximum, request at 100 or below). **This resource is not loaded by any live controller yet.**
- `TOWERS/Economy/Ammunition/AmmunitionFactory/AmmoProduction.cs`: `new AmmoProduction(batchAmount,secondsPerBatch,bufferCapacity,initialBufferedAmmo=0)`; owns `Buffer: AmmoInventory`, `IsPaused`, `ProgressSeconds`, `Advance(deltaSeconds)` returning **rounds produced**. Produces complete batches and pauses when buffer cannot fit another. **A future factory node must invoke `Advance` at runtime; no scene invokes it yet.**
- `TOWERS/Economy/Ammunition/AmmunitionDepot/AmmoStorage.cs`: `new AmmoStorage(capacity,initialStock=0)` with `Inventory: AmmoInventory`, `CurrentStock`, `FreeSpace`, `TryLoadInto(droneCargo,rounds)`, `TryReceiveFrom(droneCargo,rounds)`. One instance per placed depot later.
- `TESTS/Economy/AmmoFoundationChecks.cs` is invoked by `TESTS/Integration/EnemyIntegrationChecks.cs`. It exercises the pure helpers and threshold. **These tests have been committed but NOT executed in this environment (Godot and .NET executables unavailable). Run `dotnet build` and `godot --headless --path . TESTS/Integration/EnemyIntegrationChecks.tscn` before integration.**

**How Codex should use the inventory:** a factory/depot's own inventory transfers into a separate `new AmmoInventory(payloadCapacity)` carried by the **visible** drone at launch; the drone cargo transfers to the destination inventory **only on arrival**. Never call `TryAdd` when delivering, because that would create ammunition. The future shared logistics dispatcher must own flight jobs, source drone slots, **destination capacity reservations**, retry/cancel/destruction handling and one outstanding tower resupply request. Both factory → depot and depot → tower require visible outbound/return flights; no instant shipment.

**NEXT (still unchecked in the stages below):** implement per-tower level ammo capacity/shot consumption, integrate `AmmoInventory` with actual offensive tower firing, add buildable Factory and Depot scenes/configs/catalog registration using `AmmoProduction` and `AmmoStorage`, implement `DRONES/Transport` and logistics for both legs, Inspector status, conservation tests, runtime validation and documentation. No weapon, HUD, tower stats, placement or drone scene was changed by the foundation pass.

## Goal / playable loop

1. Player purchases and places an **Ammunition Factory**. It **automatically** produces batches at a configurable interval without requiring input materials, electricity, manual activation or an existing depot in this first pass.
2. Completed batches accumulate in the factory's **small, finite internal ammo buffer**. A factory with no compatible depot available can keep producing until its buffer cannot hold another batch; then it **pauses** and automatically **resumes** when capacity frees.
3. With a depot available, a visible **Transport Drone flies from the factory to an eligible Ammunition Depot**, carrying a limited quantity of actual buffered stock, unloads into the depot and returns to the factory. No instant teleport/invisible transfer.
4. The **Ammunition Depot** stores a larger finite inventory. When a tower's ammunition drops to the shared low-ammo threshold, it requests a delivery. A visible **Transport Drone flies from the depot to that tower**, unloads ammo up to its free capacity, then returns to the depot.
5. Offensive towers **actually consume ammunition when firing**. Their current ammo decreases; a tower with insufficient ammunition cannot fire or deal damage until resupplied. Selection/Inspector displays useful stock/status.
6. Both buildings and their drone deliveries work during real gameplay on the existing levels, with no resource duplication or regressions to purchasing, targeting, ranks, upgrades, HUD, paths or enemy performance.

**Target example (Battle Tower):** Level 1 `AmmunitionCapacity = 1000`, `AmmunitionPerShot = 1`, default global resupply threshold **10%**. For a 1,000-capacity tower, trigger a request when current stock reaches **100 or fewer** rounds. These are initial suggested values, not universal hardcoded assumptions.

## Read the existing architecture before editing

- `NOTES/CodePreferences.md` and root `README.md`; inspect actual source and all affected scenes/configs.
- `TOWERS/README.md`: complete **absolute** C# `*Levels.cs` rows, purchased levels, earned ranks and the existing eight-stage stat resolver. Preserve this design (no cumulative upgrades or per-level `.tres` stat duplicates).
- `TOWERS/Shared/TowerBase.cs`, `TowerConfig.cs`, `Upgrades/TowerLevel.cs`, `Stats/`, and individual `Offensive/*Tower/*Levels.cs` and runtime weapon classes.
- `TOWERS/Placement/TowerPlacementController.cs`, `Shared/TowerDefinition.cs`, `DefaultTowerCatalog.tres`, build tray and selected-tower Inspector in `UI/GameplayHUD/`. New economy structures must be genuinely **buildable/selectable**, not test-only code.
- `ECONOMY/Wallet/EconomyManager.cs`, `Resources/`, `Transactions/`: retain the **one authoritative wallet**, existing construction costs and rollback/validation rules. Do not confuse map Credits balance with physical factory/depot/tower ammunition inventories.
- Future category READMEs under `TOWERS/Economy/Ammunition/`, `TOWERS/Shared/Ammunition/`, `DRONES/`. Their old *instant factory-to-depot* proposal is superseded **only for transport**, as noted above.

### Intended folder ownership

```text
TOWERS/
  Economy/
    Ammunition/
      AmmunitionFactory/    # placeable factory: production, small buffer, config/scene/art
      AmmunitionDepot/      # placeable depot: large storage, config/scene/art
      Shared/
        Logistics/         # dispatcher/jobs, reservations, destination selection
        README.md          # shared ammo-economy notes
  Shared/
    Ammunition/            # optional tower-local ammo capability; shot consumption, state, refill
DRONES/
  Transport/               # shared flying transport drone: motion, cargo journey, return, visuals
  Shared/                  # only if truly needed; common drone behaviours, NOT ammo stock authority
  Combat/                  # leave untouched; future player-ability attack drones
```

These are **responsibility boundaries**, not a mandate for one class per line or a new generic framework. Keep the transport drone implementation reusable for future non-ammo cargo, without building speculative combat drones. Buildable factories/depots remain `TowerCategory.Economy` via existing tower catalog and placement.

## Implementation checklist — sequential passes

### Pass 1 — Tower-side local ammunition

- [ ] Add **optional, reusable ammunition stats** per offensive tower type/level: `AmmunitionCapacity` and `AmmunitionPerShot` (or a clearly defined consumption rate/activation cost for continuous weapons). Source authored defaults from each tower's **complete `*Levels.cs` row**, rather than putting game-balance numbers in weapon loops or a global singleton. Add a small typed optional stat group if useful; preserve existing stat pipeline stages and cached calculation.
- [ ] Add per-placed-tower `CurrentAmmo` state separate from the authored maximum, with safe initialization, consumption and `TryAddAmmo`/capacity-limited refill semantics. Do not add mandatory weapon logic to passive buildings.
- [ ] **Battle Tower example:** capacity **1,000**, cost **1 ammo per successful shot launch**. Deduct once when the projectile actually fires, not when it hits. Empty tower does not create a projectile or deal damage; resume normally when restocked.
- [ ] Wire the **other currently playable offensive weapons** to ammo usage without changing their underlying targeting/damage semantics: Blaster charges per attack launch; Shockwave per actual emitted pulse (not once for every enemy hit); continuous Laser needs an explicit **time/rate-based** energy/ammo policy so a beam cannot deliver unlimited free damage between charges or drain ammo once per physics tick accidentally. Choose sensible **editable provisional defaults** for these and document them. Do not require magazines yet.
- [ ] Make placed-tower **initial ammunition configurable**. For minimal disruption, initially start newly placed offensive towers **full by default** and allow an explicit low/empty test setting; this is *starting stock*, not ammunition generation during gameplay. Subsequent shots reduce it permanently until delivered.
- [ ] Keep current ammo valid when buying upgrades (preserve stock, cap at new capacity; **never refill for free on upgrade**). Rank XP, purchased levels and cached stats continue working. Avoid an additional combat stat pipeline.
- [ ] Show `Ammo: current / capacity` (and eventually `Resupply requested` / `Out of ammo`) in the existing selected-tower Inspector. Use events/controlled updates for presentation, not a full per-frame HUD poll.

### Pass 2 — Buildable factory and depot

- [ ] Implement real placeable **Ammunition Factory** and **Ammunition Depot** scenes/configs/definitions, with simple coherent **vector placeholder artwork/icons**; register both in `TOWERS/DefaultTowerCatalog.tres` so the existing build UI can construct, select and inspect them. Passive buildings have no mandatory targeting, combat or ammo-consumption nodes.
- [ ] Give each a configurable Credits construction cost/footprint compatible with current **1×1 only** grid validation, occupancy and route-preservation rules. Keep normal buy/upgrade services authoritative; do not create a new wallet or bypass payment rollback.
- [ ] Factory runs an automatic **production timer**, generates complete batches of configurable size and holds them in a small `InternalBufferCapacity`. Pause when there is insufficient room for another whole batch. **No depot:** it still produces until full. Do not discard ammo, silently transfer it or let the buffer exceed capacity.
- [ ] Depot owns a configurable large `StorageCapacity` and `CurrentStock` (per instance), accepts only available space and doesn't mint ammunition. Handle more than one factory and depot, using a deterministic first-pass destination-selection rule.
- [ ] Expose factory production/buffer status and depot stored stock in the selected building Inspector. Settings and runtime inventory must remain distinct. Keep visual/UI polish modest.

### Pass 3 — Reusable transport drone and **factory → depot** flight

- [ ] Implement one lightweight **Transport Drone** scene/runtime (plus editable speed, payload amount and source drone-slot count) under `DRONES/Transport/`. For the first pass, drones fly **directly through space** from their source to their destination and back; no enemy A* ground navigation. Simple visible vector drone is sufficient.
- [ ] The factory may dispatch a limited number of its **own home-based transport drones** to eligible depots. On dispatch, claim cargo from actual factory buffer; record stock as **reserved/in-flight** so multiple jobs cannot load the same ammo. A factory with no depot/space/available drone holds its buffered stock and continues producing until full.
- [ ] **Destination capacity reservation:** avoid multiple inflight deliveries overfilling the same depot. Prefer dispatch of whole batches when source cargo/payload/destination available space permits; allow configurable payloads at least as large as the initial sample batch.
- [ ] On arrival, unload precisely the reserved quantity into the depot (or handle a valid changed-capacity/vanished destination safely). Fly drone back to the source before reusing its slot. Buffer space freed by departures lets production resume. Do **not** add ammo on drone launch and again on arrival.
- [ ] Multiple factories/depots and absent/full destinations must work without drone spam, orphaned jobs or per-frame scanning every building.

### Pass 4 — Depot → tower resupply flight

- [ ] Set a **single globally editable** `ResupplyThresholdFraction = 0.10` for the first pass, ideally in an ammunition-specific shared config/resource. Request when `CurrentAmmo <= AmmunitionCapacity * 0.10`. Keep the architecture ready for a **future per-tower Inspector threshold slider**, but **do not build the slider now**.
- [ ] A tower creates **at most one outstanding request/job** while low; no request spam on every shot/tick. Decide and test rearming after refill and retrying an unfulfilled request when stock/drones become available. Multiple depots can service towers.
- [ ] Each depot dispatches its **own available transport drone**, with a finite, configurable payload. Its cargo comes from depot stock exactly once. Prefer a sensible deterministic first-pass choice of request/depot, e.g. nearest eligible compatible depot with inventory and a free drone.
- [ ] Drone physically flies to tower, delivers up to remaining tower capacity, then returns. Tower's ammo actually increases on **arrival**, never instantly on dispatch. A tower continues shooting while delivery travels and may run empty before it arrives.
- [ ] Prevent double-refills, overfill and lost/duplicated cargo if a tower moves to another state, upgrades, dies/is removed, or a depot/factory disappears. Explicitly decide safe **return-to-source** or **loss-on-destruction** handling and test it. Busy drone slots always eventually release.

### Pass 5 — Integrate, verify and document

- [ ] Complete the **real gameplay loop**, not just isolated mock classes: purchase factory + depot + Battle Tower, watch factory fill, see drone carry cargo factory → depot, fire Battle Tower until <=10%, see depot drone bring ammunition, watch `CurrentAmmo` increase, and observe normal resumed firing.
- [ ] Verify absent depot, full depot, full factory buffer, automatic pause/resume, scarce stock, multiple simultaneous requests, repeated dispatch, two factories/depots, no ammunition and out-of-ammo weapon shutdown, tower upgrade, construction payment failure, drone job cancellation and scene unload/cleanup.
- [ ] Add/update suitable `TESTS/` deterministic integration checks, especially **conservation**: `factory buffers + depot stock + tower reserves + currently carried/in-flight cargo` changes **only** from completed factory production and actual fired shots (or explicitly modeled loss). Count magazine rounds separately if magazines are ever added.
- [ ] Keep drone updates limited to **active drones**; keep job scheduling event-driven or budgeted/staggered. No each-frame `GetNodesInGroup`/all-factory×all-depot scans, costly allocations, or all-enemy queries. Preserve 500+ enemy gameplay performance.
- [ ] Run project build, Godot scene import/runtime checks and existing integration suites, fix failures, and give a concise report of implemented passes, tests and remaining limitations.
- [ ] Update existing ammunition, depot, factory, logistics, shared tower ammo and DRONES/Transport READMEs to state the *actual implemented behaviour* (including **factory → depot via drone**) and remove contradictory instant-transfer/planning statements once proven working. Leave other speculative systems clearly labelled future.

## Global settings / balance to expose (not hardcode everywhere)

| Setting | First-pass direction |
| --- | --- |
| Battle Tower Level 1 `AmmunitionCapacity` | **1000** |
| Battle Tower Level 1 `AmmunitionPerShot` | **1** |
| Global resupply threshold | **10%** of current maximum capacity |
| Tower initial ammo | Configurable; full by default initially |
| Factory batch amount & production interval | Editable provisional values; tune in Inspector/config or compact authored table |
| Factory internal buffer | Small finite, editable |
| Depot capacity | Large finite, editable |
| Factory/depot available drones | Finite configurable slots per source |
| Transport drone speed/payload | Editable; visually confirm travel time |
| Initial ammunition type | **One shared generic type** for the first loop; leave a clean path for future differentiated ammo types without creating a universal resource network now |

Do **not** use economy Credits as the ammo counter. Don't produce ammo on magazine reload. Transport uses physical cargo and exact inventory accounting, not instant resupply. All defaults other than the agreed Battle 1000/1 and 10% threshold are **provisional balance choices**, not newly approved game design.

## Scope exclusions — explicitly later

- Electricity generation/consumption and island power grids; factories can operate without power for this initial independent pass.
- Raw materials, crafting inputs, passive/offline production, persistent campaign research and comprehensive save/load. Avoid adding a new mandatory resource prerequisite that blocks all ammo production.
- Magazine/reload timers as an additional gameplay requirement (tower-local **reserve consumption** is mandatory now; a magazine can be layered later).
- Individual resupply threshold sliders, elaborate production scheduling UI, complicated multi-ammo recipes or shipment selection screens.
- Combat/player-ability drones, drone weapons, path-following attack abilities, advanced drone navigation, repairs or drone AI.
- Sweeping refactors of combat, tower categories, enemy routes, HUD or UI theme.

## Codex execution guidance

**This is a multi-file, multi-pass coding task.** Make the passes as small, separately testable commits as sensible, but **do not stop at the documentation or a nonfunctional placeholder**. Work toward the whole factory → flying drone → depot → flying drone → consuming tower pipeline and report clearly if any stage cannot be completed safely. Preserve the actual current repo architecture (Godot C#), tests, existing scene paths and authoring patterns; do not blindly copy illustrative future class names from README files. Keep config values easy to find and adjust, avoid unnecessary nodes and runtime per-frame scanning.

**Definition of done:** The end-to-end loop is observable in a playable level; the Battle Tower example consumes and requests ammo at 10%, its delivery actually travels and replenishes stock, current other offensive weapons have sensible consumption policies, factories buffer/pause/resume properly, both delivery legs use drones, and core placement/wallet/upgrade/combat tests still pass.

**Do not mark this task complete before the player can actually place both buildings and witness all three stages of the ammunition flow.**
