# SALV1 — Scrap Recovery, Refining, Resource Storage and Two-Drone Construction

**Repository:** `ben0kay/godot-tower-defense` · **Branch:** `dev` only  
**Status:** **APPROVED DESIGN DIRECTION / IMPLEMENTATION PLAN — NOT YET IMPLEMENTED**  
**Purpose:** Replace the earlier SALV1 wallet-output concept with a complete, physically transported raw-material economy. This document is the **single authoritative SALV1 plan**; do not implement its superseded earlier wallet-grant variant.  
**Implementation instruction:** Follow stages A–G sequentially, building on existing systems. Do not create unrelated advanced resources, research nodes, towers with mandatory metal costs, new economy singletons or global UI overhauls.

## 0. Desired gameplay loop

```text
Enemy legitimately destroyed
  └─ 1% base roll → recoverable RAW SCRAP drops (physical pickup)
       └─ Citadel SALVAGE DRONE → pickup → SCRAP REFINERY
            └─ process 1 Scrap for 8 seconds → 5 REFINED METAL
                 └─ refinery MATERIAL TRANSFER DRONE → RESOURCE STORAGE
                      └─ Metal stock remains physically stored here

Player builds a future tower whose recipe requires Credits + Refined Metal:
  1. Validate/charge Credits and reserve enough physical Metal in Resource Storage.
  2. Citadel CONSTRUCTION DRONE #1 → construction site → begins engineering work.
  3. Citadel MATERIAL/CONSTRUCTION DRONE #2 → Resource Storage
                                        → loads reserved Refined Metal
                                        → construction site
                                        → unloads material and assists building.
  4. Work finishes only after required metal is actually delivered AND
     the canonical construction timer completes. Both drones return home.
```

**Three different logistics roles:** (a) salvage drone for raw Scrap; (b) refinery transport drone for refined output; (c) Citadel material/construction drone accompanying an engineer for metal-requiring builds. **The two site drones both depart from a Citadel.** The material drone's intermediate stop is Resource Storage. The factory/refinery output courier does **not** count as one of the two site drones.

Keep **Ammunition Depot** and **Resource Storage** as entirely separate placeable buildings, inventory types and dispatch responsibilities. Reuse the shared `DRONES/Shared/` visuals/flight curves and familiar stock/reservation mechanics, not the ammo-only ownership and manifests.

## 1. Defaults and boundaries (Inspector-editable proposals)

| Item | Initial proposal |
| --- | --- |
| Scrap chance | **1% per genuine kill**, once only; future permanent research may raise it |
| Scrap yield | 1 raw Scrap item per successful drop |
| Max outstanding drops / lifetime | 64 / 120 unpaused seconds, suspended while reserved |
| Salvage drones | Max 2 active per living Citadel, with global safety cap |
| Scrap Refinery | Economy category; 350 Credits; 2×2; 5s build; 1,000 HP |
| Refinery raw-input capacity | 20 Scrap |
| Recipe / processing time | **1 Scrap → 5 Refined Metal** / 8 unpaused seconds |
| Refinery refined-output buffer | 50 Metal units; full buffer stops new batches |
| Refinery→Storage courier | 1 active flight per operational refinery initially; carry up to 10 Metal |
| Resource Storage | Economy category; **200 Credits**, 2×2, **4s** build, **1,000 HP** |
| Storage initial/maximum Metal | 0 / **100 Refined Metal** |
| Metal-dependent construction | 1 engineering drone **plus** 1 material/construction drone from a Citadel |
| Material drone capacity | 50 Metal units for one trip (must fit the whole v1 recipe) |
| Early construction before metal delivery | **25% maximum** of real authored build progress |
| Metal delivery / assist | Actual delivery gates the remaining 75%; both drones beam/work afterward |
| Non-metal construction | Existing one-builder behaviour unchanged |

All quantities, speeds, timeouts, caps and visual intensities are provisional and should be exposed in feature-owned typed Resource presets, not buried in per-frame code. No existing tower gets a Metal cost in SALV1. Example later price: 300 Credits + 5 Refined Metal (not a change to the current catalog).

At 1% drop and 5 Metal per Scrap, average production is only **5 Metal per 100 genuine kills if everything is recovered and refined**. Future tower metal prices must account for that scarcity; do not silently raise drop chance or create free starting stock to make initial levels work.

**No campaign-wide metal inventory in this pass.** Material stocks belong to a single mission; between-wave checkpoints preserve them.

## 2. Inspect existing source first

Read `CodePreferences.md`, `project.md`, this plan, `NOTES/Ideas.md` M12, `DRONES/ideas.md` I2, the owning READMEs and current source. In particular:

- `ENEMIES/Shared/EnemyRegistry.cs` exposes the authoritative once-only `EnemyKilled` event; `EnemyDefinition.cs` owns normal Credits rewards.
- `ECONOMY/Resources/ResourceDefinition.cs`, `ECONOMY/Transactions/ResourceCost.cs` and `ECONOMY/Wallet/EconomyManager.cs` own resource identity, mixed-cost representations and the existing **Credits** transaction.
- `TOWERS/DefaultTowerCatalog.tres`, `TOWERS/Shared/TowerBase.cs` and `TOWERS/Placement/TowerPlacementController.cs` own placement, costs, paid buildings, builds and rollback.
- `TOWERS/Shared/Construction/ConstructionWorkManager.cs` owns **all actual build time**. `TOWERS/ConstructionDrones/ConstructionDroneManager.cs` owns the existing single-builder reservation, deploy/release, beams and return. **Do not create another build clock.**
- `TOWERS/Economy/Ammunition/AmmunitionDepot/AmmoStorage.cs`, `AmmunitionLogistics.cs` and the courier adapters show *separate stock, destination reservations and physical transfer on arrival*. They are examples, **not owners of Metal**.
- `DRONES/Shared/DroneBody.tscn`, `DroneFlightMotion.cs` provide shared movement and presentation without business ownership.
- `WORLD/OBJECTIVES/Citadel/` provides multi-Citadel home/launch identities.
- `SYSTEMS/Saving/LevelSaveData.cs`, `CampaignSession.cs`, `SaveValidation.cs` enforce between-wave Save/Continue; `LEVELS/Session/LevelSessionController.cs` owns victory freeze and Results timing.

Verify current `dev` source at implementation time. Existing older README sentences can be historical; do not follow them in conflict with live code.

## 3. SALV1-A — Enemy kill → physical raw Scrap

1. A world-scoped salvage owner subscribes **once** to genuine `EnemyRegistry.EnemyKilled`, separate from normal kill Credits and tower XP. All eligible killed ships, including environmental kills, get one independent roll at **0.01**. No arrivals, despawns, duplicate kill callbacks or manual removals.
2. Seed a dedicated per-mission salvage RNG, separate from wave RNG, and save its state. Future research modifies a **resolved** effective chance; v1 implements only the base. Clamp safely and avoid expensive work on the 99% of kills with no drop.
3. Spawn a simple small glowing metal fragment with one stable unique pickup ID and precise world position. Shared/pool-limited presentation, not a permanent wreck per enemy.
4. State is `Available / Reserved / PickedUp / Expired`; no double reservations. Limit to 64 world pickups; expire unreserved pickups after 120 unpaused seconds; held pickups pause their expiry. Deterministic cleanup on unload/cancel and visible map re-entry.
5. **Scrap is physical** (pickup, salvage-drone cargo, refinery input), never Credits, XP or a global wallet balance. Pickup alone pays nothing.

## 4. SALV1-B — Citadel salvage pickup → Scrap Refinery

1. Citadel launches a separate **salvage** drone when an available scrap pickup and accepting **operational** Scrap Refinery exist. Limit to 2 active salvage jobs per Citadel; select suitable targets and valid home/destination with stable tie-breaking.
2. Reserve refinery raw-input space **before** launching. Drone travels Citadel → pickup → short hover/grab → refinery → return Citadel. Actual scrap moves from world pickup into drone cargo at grab, then into refinery raw stock **only on delivery**.
3. If refinery is full, absent, disabled, destroyed or still under construction, do not create impossible deliveries. Drops can remain unclaimed until expiry; no automatic scrap-to-metal conversion.
4. If the destination is lost **after pickup**, redirect to another eligible refinery or bring cargo to a finite Citadel holding buffer with explicit later redispatch. If an owning Citadel is destroyed with no valid handoff, record actual lost cargo, do not mint resources.
5. Reuse common drone motion/graphics but keep salvage jobs independent of fighter drones, ammo couriers and the construction manager. Cancel jobs and release reservations safely.

## 5. SALV1-C — Scrap Refinery: real input, real output

Create a placeable `Scrap Refinery` in `TOWERS/Economy/ScrapRefinery/`. Reuse `TowerBase`/Economy catalog integration, normal Credits price, construction/health/disable/selling and proper authored 2×2 artwork.

- Refinery has **finite per-building raw Scrap input** (20) and **finite Refined Metal output** (50). Both are physical stock, not global wallet balances.
- One active batch consumes/reserves **one real scrap** when started, advances for **8 unpaused seconds** while operational, then generates exactly **5 Metal** in the **refinery's local output buffer**. If insufficient raw scrap or output room, wait; no unbounded buffer, free production or overflow loss. Make an in-progress batch and partial clock saveable.
- If disabled, pause safely. Refining does not create energy, bypass shared disable reasons, affect other combat or require a new power system. Optional power integration is a later phase.
- Expose actual Scrap, queued input, batch progress, Metal output, output capacity and operational status in its selected-building Inspector. Keep small activity animation/steam event-driven.
- **Critical correction to previous SALV1 plan:** do **NOT** call `EconomyManager.Grant(RefinedMetal, ...)` when the batch finishes. Production is material stock in this particular Refinery, not instant global currency.

## 6. SALV1-D — Resource Storage and refinery-to-storage delivery

Add a **new independent Economy building** at `TOWERS/Economy/ResourceStorage/`, suggested stable `TowerId = "resource_storage"`, with scene, definition, config, icon, 2×2 footprint, usual health/construction/paid placement and selected Inspector.

- Call it **Resource Storage**, not Ammunition Depot. Only Refined Metal is implemented in v1; keep its inventory representation straightforward, with a possible future path for other materials but **do not implement multiple new materials**.
- Default storage holds **100 Metal**, starts empty, and never produces/refines metal itself. Give it readable **Stored / Reserved / Available / Incoming** status. Each storage's inventory and identity are independent; no shared magically mirrored quantities.
- A bounded **refinery transport drone** travels **Scrap Refinery → Resource Storage → Scrap Refinery**, visually carrying actual refined-metal cargo. This is a distinct logistics job from raw scrap collection and from Citadel material-construction flights. Prefer one active refinery courier initially, capacity 10.
- On shipment dispatch, reserve receiving storage capacity; at pickup transfer stock from **that refinery's output buffer into flight cargo**; on arrival transfer only that cargo into **that storage's stock**. No wallet grant and no instant transfers. If storage doesn't exist or is full, refinery output remains locally buffered; once full, refining pauses. Never discard output silently.
- If a destination disappears in flight, return/redirect physical cargo to a valid refinery/storage with reserved room, or track real loss if physically impossible; no duplicate shipment on retry. Storage destruction removes its owned unreserved metal and releases all pending reservations; any affected construction requests must re-evaluate with an explicit user-visible shortage.
- Reuse inventory/capacity/reservation patterns from `AmmoStorage` and `AmmunitionLogistics` **without making metal a subtype of ammo or routing it through ammo depots**.
- Resource Storage and Scrap Refinery must both be buildable with **Credits only** to avoid a material-bootstrap deadlock.

## 7. SALV1-E — Two Citadel construction drones for metal recipes

**This is the requested new construction mechanic.** It applies only if the selected tower's existing `ConstructionCosts` includes positive `refined_metal`. Non-metal towers and their upgrades continue existing one-builder logic untouched unless separately authorized.

### E1. Correct material affordability and atomic placement

- Keep `ResourceDefinition` with stable ID **`refined_metal`**, so future `ResourceCost` arrays can describe **Credits + Metal**. **Storage stock is authoritative** for Metal: do not use `EconomyManager.Balances` as a second spendable Metal inventory or let the HUD's aggregate count be spent independently.
- Introduce a small **composite construction affordability/transaction adapter**: existing `EconomyManager` validates/charges wallet resources such as Credits; the material logistics owner validates and reserves physical Metal from an **operational Resource Storage**. A mixed-price button and placement preview must display both needs and accurate availability.
- For **v1**, a single storage must supply the **entire required Metal amount**, and the amount must fit the one material drone's payload (default cap 50). No multi-storage consolidation, multiple site delivery flights or material-bearing paid upgrades yet. Show an understandable `Insufficient Refined Metal in storage` or `Material delivery exceeds drone capacity` failure, not a silent false affordance.
- Reserve a specific storage/amount under a unique **construction order ID** during the real accepted placement transaction, **not merely on hover, opening Build Menu or choosing a preview**. The stored quantity remains there but cannot be reserved by another order. The Credit purchase and reservation/scene commit must act atomically: if validation, spawning or charge fails, release reservations and restore the placement as existing rollback already does. On success, Credits are charged exactly once; Metal is **physically withdrawn on the drone's pickup** and never additionally deducted from the wallet.
- The stored building can be disabled/destroyed while an order exists. Before pickup, release/retarget invalid reservations and wait for another eligible storage; **do not let the site complete without delivery**. After pickup, the cargo's only owner is the supply drone.

### E2. Two drones launch from one Citadel

**Drone 1 — engineer:** Use existing `ConstructionDroneManager` with its normal reservation, home selection, flight, acquisition and construction beam. It flies **Citadel → tower site**, starts the canonical build timer on completed beam deployment, and stays visibly working.

**Drone 2 — metal supply + construction assist:** A separate material-construction job launches **from a Citadel** (ideally the same assigned home as Drone 1), flies **Citadel → reserved Resource Storage → tower site**, physically loads the reserved amount, visibly carries it, unloads **exactly once** at the site, then turns on its own welding/build beam and **remains working beside Drone 1**. The two drones should approach and hover at different natural angles rather than overlap.

**Completion:** Both drones stop beaming on the existing authoritative `TowerBase.WorkCompleted` callback and curve back toward their Citadel. The supply drone releases its slot only after its return or explicit retirement. A material-drone arrival **does not instantly build** the tower; only the existing construction clock can complete it. **Do not double build speed merely because there are two beams.**

### E3. Construction progresses while material is en route

- After Drone 1 arrives and deploys its beam, real construction progress may advance up to a **configurable default cap of 25%** of the existing authored total work. This represents foundations, frame and preparation work before materials arrive.
- If Drone 2 has not delivered the reserved Metal, the site reaches its 25% cap and **holds at that actual percentage**. No fabricated elapsed time, no repeated work-complete callbacks, no visible percentage drifting forward.
- On confirmed material delivery, mark the site `Materials Delivered`, release the material-progress gate and let the **same canonical ConstructionWorkManager** continue the remaining 75% of real work time. Both drone beams work through completion; no new second construction timer. The material gate must be independent of the existing engineering-drone deployment hold—do not conflate tokens or prematurely release either.
- If metal arrives before the first builder is ready, retain delivery status and start work normally on engineer arrival. If a storage/delivery fails, keep the site as `WAITING FOR REFINED METAL` at or below the capped progress until material arrives.
- **No metal recipe?** Existing one-drone construction remains exactly as it is, including duration, upgrade holds, save compatibility, zero-duration direct cases and fallback rules.
- **Zero-duration + metal recipe:** metal must still be delivered physically before the tower becomes operational; do not let the current `BuildSeconds == 0` compatibility shortcut bypass the material gate. A new metal tower should author nonzero build time by default.

### E4. Cancellation, destruction and material ownership

- **Before drone pickup:** release source-stock reservation on actual cancellation or failed placement. If its storage is destroyed, material physically in it is lost; the site stays waiting and attempts reservation from a live storage rather than gaining free metal.
- **After pickup but before delivery:** source stock already decreased; cargo belongs to the material drone. If the site disappears or the job aborts, carry cargo back to a valid Resource Storage where possible (reserve return capacity), otherwise account for explicitly lost cargo. No duplicate stock restoration.
- **After delivery:** Metal is consumed as part of the construction site. If the site is sold/destroyed, delivered materials are **not automatically refunded** in v1; preserve the existing Credits-sale rule. State this in the UI/owner guide.
- If the site has already accepted Metal, never fetch/pay/consume it again on retries, rebinds, pauses, save reloads or upgrade events.
- No global construction material request every frame; publish state changes through the work/job owner.

## 8. SALV1-F — Interface and gameplay clarity

- Top HUD: add **Metal in Storage** total, updated via inventory change notifications (not wallet polling); optionally distinguish **Available** and **Reserved/In Transit** in a tooltip or inspector. Do not misrepresent refinery-output metal as spendable construction stock.
- Build Menu/preview: existing `ResourceCost` presentation should show future mixed **Credits + Refined Metal** recipes and a meaningful insufficient-storage message.
- Resource Storage Inspector: `Metal X/100`, `Reserved Y`, `Incoming Z`, `Available W`, and state.
- Refinery Inspector: raw Scrap, processing percentage, refined output, outgoing cargo and current status.
- Metal construction Inspector: `ENGINEERING`, `MATERIAL DRONE EN ROUTE`, `LOADING`, `METAL X/Y DELIVERED`, `WAITING FOR METAL` or `BOTH DRONES CONSTRUCTING`, with the existing real completion percent.
- Optional distinct small carried-material silhouette, brighter second beam and restrained existing construction circular telemetry. Never cover important stat/health info or require a global HUD redesign.
- No mandatory effects processing when no active flight/work.

## 9. SALV1-G — Saving, recovery, outcomes and research readiness

**Use existing S0–S3 between-wave checkpoint flow.** Saving is currently allowed with active builders, ammo shipments, construction/production and waiting jobs. Material requests must not newly disable Save.

Capture and validate authoritative states:

- Dedicated salvage RNG, drops/expiry/reservations and salvage-drone phase/cargo.
- Per-Refinery raw Scrap, committed batch input, fractional production seconds, output Metal and outbound refinery shipment.
- Per-Resource-Storage physical Metal, construction reservations, reserved incoming room and stable building ID.
- Per-material-bearing site **unique order ID**, requested material and amount, selected source, pre-delivery progress cap, delivered latch, true elapsed work, builder hold and pending material flight.
- Material/construction drone phase (Citadel→Storage→Site→assist→return), live cargo, actual movement snapshot, home/source/target IDs and reserved space.
- Existing session wallet balances stay authoritative for Credits; **do not write physical Metal into wallet balances and also into storage**.

Restore dependency order carefully: **placed buildings/inventories → physical stock and refinery batches → valid Citadel/source identity → salvage/transfer/construction orders and reservations → active drone flights/holds → construction work state → HUD summaries**. Do not replay kill rolls, deliveries, resource transfers, batch production, placement charges or construction-completed callbacks during restore.

Use bounded collections, identity/cargo conservation checks, finite durations and positions, strict stable IDs and backwards-compatible defaults where possible. Preserve the existing save version/content-hash rules; changed authored resources can legitimately invalidate incompatible old checkpoints.

**Victory/defeat:** The mission snapshot freezes when the outcome latches, and Results opens after the existing victory delay. Do not delay Victory to finish material deliveries. Stop new jobs/rewards after outcome, and cleanly release/dismiss temporary mission-only material jobs on unload. No post-victory automatic metal credit.

**Research (later, not SALV1):** resolve 1% base scrap chance through one authored config with a frozen deployment research modifier hook; example total chances 1.0% → 1.5% → 2.0%. Do **not** implement research nodes, persistent material inventory or rare metal types now.

## 10. Implementation order (Codex handoff)

Plan stages are deliberately separable; complete integrated gameplay before marking SALV1 implemented:

| Stage | Deliverable | Minimum proof |
| --- | --- | --- |
| **A** | 1% drops and capped physical Scrap | Genuine kill-only, no duplicate drops |
| **B** | Citadel salvage flight → Refinery | Scrap moves only on pickup/arrival |
| **C** | Refinery processing/output buffer | 1 Scrap creates 5 Metal locally after 8s |
| **D** | Separate Resource Storage + real refinery courier | Source output decreases, destination stock increases on arrival |
| **E** | Mixed-cost material reservation and **two Citadel site drones** | No finish before delivered stock; 25% cap; two beams; no double charge |
| **F** | HUD, build card, Inspector/status | Honest physical stock and real progress |
| **G** | Checkpoint and lifecycle completion | Save/reload does not duplicate inputs/outputs/jobs |

**A usable vertical slice means C + D + E are real**, not a decorative second drone or immediate global Metal balance. If Codex cannot complete all stages in one focused implementation, land **independent coherent stages** only when the owner agrees; no fake success claims.

### Focused owner acceptance

1. Raise drop chance to 100% in the Inspector temporarily; defeat enemies and verify physical drop / no drop on Citadel arrival. Restore 1%.
2. With no Refinery/Storage, pickups remain pending and no Metal is created in a wallet. Build both with Credits; wait for real salvage pickup, refining, and a **visible refinery→storage shipment**.
3. Verify Storage has Metal, separate from ammo depots. Inspect two different storages and a full storage; no teleporting or overfilled stock.
4. Temporarily configure a test tower recipe with Credits + e.g. **5 Refined Metal**, without permanently changing the authored existing roster. Verify build preview, proper storage reservation and exactly **two Citadel site drones**.
5. Drone 1 begins real work, capped at 25% until Drone 2 physically loads stock and delivers it. Drone 2 joins with a beam. Both remain until normal work completes and return. Metal is removed from its storage on loading, not just from the HUD.
6. Attempt simultaneous builds against the same small stock; verify no double spend. Destroy/disable storage before loading, during a flight, and after material delivery; inspect waiting and return/refund/loss policies.
7. Ordinary Credits-only builds and all normal ammo delivery/Factory/Depot behaviour remain unchanged; no extra drone or added delay.
8. Save/Continue between waves with Scrap on the ground, Refinery mid-batch, refinery cargo en route, Metal stored/reserved, and either site drone mid-flight/build. Verify conservation and no replay.
9. Try multiple Citadels and multiple storages; pause, unpause, unload, defeat and victory; verify active queues are bounded and stop correctly.

**Minimal validation:** source/diff review, at most one practical basic build, no extended test runs unless necessary. Owner handles visual, balance and FPS acceptance. Keep updates brief in `NOTES/WORK.md` and owning feature READMEs.

**When explicitly assigned to Codex:** implement only this SALV1 plan on `dev`, commit and push without touching `main`, and report code changes, settings, limitations, validations and owner testing guidance. Until then, **this document is planning only**.

**Future optional expansions:** Research increasing scrap chance, research increasing drone slots/refinery efficiency, authored metal-bearing tower tiers, material-based upgrades, cross-mission inventory, powered refining, rare alloys and additional storage resources.
