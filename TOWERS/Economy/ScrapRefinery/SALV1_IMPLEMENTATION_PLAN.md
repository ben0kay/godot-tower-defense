# SALV1 — Enemy Scrap Recovery, Salvage Drones and Refined Metal
**Repository:** `ben0kay/godot-tower-defense` · **Branch:** `dev` ONLY  
**Status:** Codex-ready implementation draft; NOT IMPLEMENTED. Implement only when explicitly assigned.  
**Feature owner:** `TOWERS/Economy/ScrapRefinery/` (refinery); `DRONES/` (recovery job/flight); `ECONOMY/` (resource balance); `ENEMIES/` (kill event).  
**Goal:** A real physical resource chain: eligible enemy kill → rare scrap drop → salvage drone collects → delivers to an operational Scrap Refinery → refinery converts raw scrap into **Refined Metal** → future mixed-resource tower prices.

## 0. Owner decisions and provisional balance

**Confirmed direction:** all eligible enemy kills have a **1% base scrap-drop chance**; scrap must be physically recovered and refined; **Refined Metal** is the material future towers may require to build; future permanent research may raise the chance. Scrap must **not** directly turn into Credits or XP.

**Suggested v1 balance, editable in Inspector (owner may tune after playtesting):**

| Setting | Proposed starting value |
| --- | --- |
| Drop chance per legitimate enemy kill | **1%** (0.01 probability); a single independent roll per killed ship |
| Scrap amount per drop | **1 raw scrap** |
| Maximum uncollected world pickups | **64** simultaneously; bounded oldest/expired cleanup |
| Pickup lifetime | **120 seconds** of unpaused mission time, pausing while reserved |
| Recovery drone dispatch | **2 per living Citadel**, with small global cap if many Citadels |
| Drone cargo | **1 raw scrap** per sortie |
| Pickup hover duration | **0.4 seconds** |
| Scrap Refinery build price | **350 Credits** |
| Scrap Refinery footprint / build time / initial hull | **2×2**, **5 seconds**, **1,000 HP** |
| Refinery input buffer | **20 raw scrap** |
| Conversion recipe | **1 raw scrap → 5 Refined Metal** |
| Processing time | **8 seconds** per batch, while operational |
| Refinery output | Credited to the existing mission wallet on **actual batch completion**, not pickup |
| Refinery power consumption | **Deferred** initially; do not invent an alternate power allocator |

These numbers are starting suggestions, **not current source facts or guaranteed balance**. Later tower examples might cost Credits **and** 5–20 Refined Metal; **do not change any existing tower's recipe in SALV1**.

The ordinary per-enemy KillRewardCredits remains unchanged and is paid normally. A scrap drop is a separate physical resource opportunity.

## 1. Prerequisites and ownership

Read `CodePreferences.md`, `project.md`, `NOTES/Ideas.md` (M12), `DRONES/ideas.md` (I2), `TOWERS/Economy/ScrapRefinery/README.md`, and relevant current code before implementation.

Inspect these existing live seams, not old plans alone:

- `ENEMIES/Shared/EnemyRegistry.cs` — `EnemyKilled` emits once on genuine kills; arrivals and other removals don't.
- `ENEMIES/Shared/EnemyDefinition.cs` — authored enemy identity/rewards; do not duplicate kill payout.
- `FEEDBACK/DeathExplosion/` — existing destruction feedback, independent of salvage.
- `ECONOMY/Wallet/EconomyManager.cs` — existing multi-resource balances, `Grant`, `TrySpend`, `BalanceChanged`, checkpoint.
- `ECONOMY/Resources/ResourceDefinition.cs`, `ECONOMY/Transactions/ResourceCost.cs` — stable resource IDs and mixed-material recipes.
- `TOWERS/Shared/TowerBase.cs`, `TowerConfig.cs`, `TowerDefinition.cs`, `TOWERS/DefaultTowerCatalog.tres` — placeable Economy building/footprint and shared construction.
- `TOWERS/Economy/Ammunition/AmmunitionFactory/` — reference for passive production, **not** an excuse to copy ammo dispatch.
- `DRONES/Shared/DroneBody.tscn`, `DroneFlightMotion.cs`, construction/courier presentation — reuse motion and visuals; **separate salvage job ownership**.
- `WORLD/OBJECTIVES/Citadel/` — Citadel launch/home and multi-Citadel identity.
- `SYSTEMS/Saving/LevelSaveData.cs`, `CampaignSession.cs`, `SaveValidation.cs` — between-wave authoritative restore and bounded schema.
- `LEVELS/Session/LevelSessionController.cs` — outcome freeze and existing four-second victory presentation hold.

Do not add a global singleton, a second wallet, a duplicate enemy-death listener inside every tower, or independent resource rewards in Results.

## 2. SALV1-A — Drop events and physical scrap pickups

1. Introduce a small world-scoped salvage coordinator (suggest `DRONES/Salvage/` or `ECONOMY/Salvage/`; choose one authoritative job owner and document it). Subscribe **once** to `EnemyRegistry.EnemyKilled` and unsubscribe on world unload.
2. For each legitimate kill, perform **one** Bernoulli roll with default probability `0.01`. Use the enemy's actual last world position; include death by ordinary damage or hazards, exclude Citadel arrival, non-kill removal and duplicate lifecycle callbacks.
3. Use a **dedicated per-mission RNG**, separate from wave spawning RNG. Persist its state with salvage checkpoints so save/reload does not reroll the next wave of drops.
4. Create a small dark-metal scrap fragment with a restrained glow, gentle blink/hover and optional micro-particles; do not attach permanent wreckage to every enemy. Prefer a pooled, capped representation with immutable pickup IDs.
5. A drop has a state: **Available → Reserved → Collected** (or **Expired**). Only one recovery job may reserve it. After collection it is no longer a world pickup; the drone becomes the unique owner of one unit of cargo.
6. Expire unclaimed scrap after 120 seconds of game time. When temporarily reserved, suspend expiry; on a canceled job, explicitly restore or retire it without duplication. Cap outstanding pickups; never perform a scan for each enemy per frame.
7. No Credits, Metal, XP, tower XP, player XP or research points are granted merely because the drop exists or a drone touches it.
8. A drop can exist temporarily before any refinery is built. No collection dispatch takes place until an operational refinery can accept it; it may expire in the meantime.

Use a shared update/timer for only active pickups and flights, not independent idle polling on every building or enemy.

## 3. SALV1-B — Recovery drones and real delivery

**Source:** a living Citadel, consistent with `DRONES/ideas.md` I2 and the existing Citadel construction-drone idea. **Destination:** an **operational Scrap Refinery**, not the Citadel. The usual sortie is:

**Citadel launch → scrap location → 0.4s hover/pickup → refinery delivery → empty return to Citadel → slot released.**

- Initial maximum **2 active salvage drones per Citadel**; respect multi-Citadel play, world teardown and controlled global caps.
- Select an eligible available pickup and an operational refinery with **reserved input-buffer capacity**. Prefer short routes/valid source identity, deterministic ties, and no per-frame global searches.
- Launch smoothly using the existing shared `DroneFlightMotion` / `DroneBody` presentation. Give salvage drones a distinct small claw/grabber or carried-fragment indicator. They may fly over impassable terrain just like the existing aircraft; no ground A* and no tower-style path blocking.
- Reserve destination capacity **before** launch. Scrap remains on the ground until pickup; transfer it into one drone's cargo exactly once when pickup finishes; add it to the refinery's raw-input stock **only after arriving there**. Then return the drone home.
- A drone must not instantly teleport scrap, grant metal or create extra scrap on cancellation, retry, repeated callback or unload.
- If the target item expires/disappears before pickup, release its reservation and return/reassign. If a refinery becomes non-operational, reassign to another valid refinery if capacity permits. If no refinery is available **after pickup**, carry the raw scrap back to the Citadel in a finite, saved pending-cargo buffer until an operational refinery can receive it; do not silently mint metal or lose cargo. If the home Citadel itself is destroyed and there is no valid remaining home, explicitly cancel and account for lost cargo without creating spendable resources.
- An unfinished/refinery-under-construction/disabled/destroyed building cannot accept or refine material. Handle scene exit, sale, and input reservation release safely.
- A salvage manager may reuse **visuals/flight math** but must not reuse ammunition stock/courier manifests or fighter combat logic as its authoritative accounting owner.

Keep job data bounded, event-driven and easy to inspect/debug. Idle flight and optional glow effects should sleep or be absent when no work exists.

## 4. SALV1-C — Placeable Scrap Refinery

Create a proper Economy-category `Scrap Refinery` under `TOWERS/Economy/ScrapRefinery/`:

- Add its `TowerBase`-compatible scene, typed `ScrapRefineryConfig`, default `.tres`, `TowerDefinition`, authorable first-level artwork/icon and catalog registration. Use the existing Economy build cards, placement rules, credits wallet, construction drones, durability, disable status, selling, saved placement IDs and Inspector routing.
- Use a visually recognizable **dark sci-fi industrial refinery**: vents, intake, processing chamber, restrained amber molten light and small activity feedback. VFX must be optional and should run only during active processing.
- Start with **one paid construction tier** and no invented weapon/ranks, ammunition supply or research prerequisites.
- Keep an actual **finite raw Scrap input stock** on the building (default 20). Only a completed real delivery increments this stock, with reservations preventing overfill.
- While operational with at least one unit in storage, begin a **finite batch**. At the beginning of each batch reserve/consume exactly one raw unit (document which). After an uninterrupted 8 seconds of actual unpaused processing, produce exactly 5 Refined Metal via the existing `EconomyManager.Grant`. If disabled or non-operational, pause without advancing or duplicating work; resume consistently.
- Handle full/overflowing wallet explicitly. If the wallet cannot receive the batch output, **do not consume input or silently discard output**. Either keep completed output pending until claimable or pause before consuming, with clear Inspector status. Pick one precise conservation-safe policy.
- Provide a compact selected-building Inspector readout: **Scrap X/20; Processing Y%; Metal output +5; Status (Idle / Waiting for scrap / Processing / Paused / Buffer full)**. Make build costs, stock and progress inspectable without new global UI panels.
- Optional subtle progress motion/steam should reuse the existing sci-fi visual language, not create a complex new UI manager.

Do not grant raw Scrap to the mission wallet as a spendable currency. It is physical pickup/drone cargo/refinery inventory, not an extra globally accessible balance.

## 5. SALV1-D — Refined Metal wallet resource and interface

- Add `ECONOMY/Resources/RefinedMetal.tres` as a distinct `ResourceDefinition` with a **stable ID `refined_metal`**, sensible display name/icon and description. This is the only new **wallet** resource in SALV1.
- Existing `EconomyManager` already handles arbitrary resource IDs and mixed construction costs. Reuse `Grant`, `GetBalance`, `CanAfford`, `TrySpend/TryPurchase`, and `BalanceChanged`. No second wallet or custom metal purchase checks.
- Make the amount visible somewhere compact in the game HUD, preferably adjacent to Credits, using changes to the resource balance; do not repeatedly poll the wallet.
- Future tower recipe example (NOT to author on any existing tower now): **350 Credits + 10 Refined Metal** in the normal `ResourceCost` array. Existing build-price rendering/affordability should accommodate it.
- Refined Metal is **mission-local for SALV1**, retained in between-wave checkpoints using the existing wallet snapshot. It does **not automatically carry to the next mission**: the current campaign has no persistent shared material inventory. If the owner later wants cross-mission metal carryover, design that explicitly as a separate persistence/economy pass rather than silently modifying profile progression.
- No direct metal XP or Credit conversion. The Refinery converts only *recovered scrap* to metal.

## 6. SALV1-E — Checkpoints, mission outcomes and safety

Check the existing S0–S3 saving contract: saves are allowed between waves, including when building operations and transport drones exist. Salvage must not make ordinary legal between-wave saves unexpectedly unavailable.

- Extend `LevelSaveData` with a bounded, backward-compatible **optional salvage section** (default empty for old saves); capture drop IDs/positions/remaining lifetimes, dedicated RNG state, reservations, active job phases/paths/current cargo, Citadel home and destination identities, and any pending Citadel raw-cargo stock.
- Extend `TowerSaveData` only as necessary for each Refinery's **raw scrap stock, input reservations, in-progress batch and pending output**, with validation that non-refinery buildings cannot carry refinery-specific fields.
- The wallet already captures Refined Metal. Restore wallet/resource balances, refinery state, pickups, reservations and flights **without replaying death/drop events, pickups, transfers or production callbacks**.
- Enforce bounded counts, finite positions/times, valid lifecycle phases, unique IDs, cargo conservation, stable owner identities and valid destination capacities in `SaveValidation` and feature-level restore. Preserve the existing content-hash and versioning policy; acknowledge that new assets may invalidate strict old dependency fingerprints.
- Pause freezes drop lifetime, drone motion and refinement. Rebuild/Continue must resume one authoritative unit of scrap at most once, with no negative inventory or repeated material grants.
- **Victory/defeat:** the session currently freezes its outcome immediately and displays Results after a 4-second victory hold. **Do not change that timing or delay victory for salvage.** Stop new dispatch/refinement rewards when the outcome latches; cleanly retire unprocessed, uncollected mission-local scrap and unfinished jobs on Results/unload. Do not invent post-victory metal rewards. Record this intentional v1 limitation for owner playtesting.
- No salvage persistent inventory across mission exit in this phase.

## 7. Research expansion hook — future only

The drop rate must be resolved from **one configurable base chance** at mission startup, not hardcoded as `Random.NextDouble() < .01` in multiple locations. Design for later **additive percentage-point modifiers** from frozen deployment research:

- Base = 1.0%
- Future Salvage Detection I example = 1.5% total
- Future Salvage Detection II example = 2.0% total

Cap effective probability to `[0,1]`, and expose the resolved chance in a read-only debug/Inspector surface where practical.

**Do not implement new Research nodes/effects, campaign XP changes, rare component tables, Scrap Refinery research unlocks, or existing-tower metal requirements in SALV1.** Those are separate approved follow-ups.

## 8. Performance and validation

- Work only from kill, pickup, flight, build state, stock/batch or explicit save events; active-only flight/batch clocks are acceptable. No full enemy/scene/map scan each frame.
- Keep globally pooled/capped scrap visuals and a finite flight slot budget. Enemies with no drop get no extra scene nodes.
- Reuse the shared engine's rendering/sound/flight effects; no 1 particle system or light per killed enemy.
- Avoid allocations and sorting in hot death/target paths; wake and sleep inactive processors.
- Ensure Refund/sale/disable/capacity failure never duplicates or loses stock silently. Do not disrupt existing ammo couriers or constructors.
- Source review and **at most one basic build**, if available; no broad stress suites or invented performance claims. Owner validates visuals/gameplay/FPS in Godot.

### Owner acceptance checks

1. Force 100% chance temporarily in Inspector: kill enemies and verify one physical pickup per true kill; no pickups for arrivals or repeated death callbacks.
2. Restore 1%; confirm roughly rare drops, including on environmental kills. Check high-density waves with capped items.
3. No refinery: pickups wait/expire, no drone/metal magic. Build an operational Refinery: Citadel dispatches drones, visible pickup and transport to the correct plant.
4. Watch processing: 1 delivered Scrap is consumed once, 8 seconds of unpaused processing creates exactly **5 Refined Metal**; adding/removing power should not affect v1 unless expressly authored.
5. Multiple refineries/Citadels; full buffer, blocked/disabled/destroyed refinery, return and re-target; no cargo duplication.
6. Metal shows in HUD; can pay a **temporary test-only** `ResourceCost` recipe combining Credits + Metal, with rollback on insufficient funds; existing production tower costs untouched.
7. Pause, between-wave Save/Continue and retry; compare inventory, pending pickups, drone cargo, timers and RNG state before/after.
8. Victory after final-wave scrap drops preserves existing Results timing and only the resources earned before the outcome. Defeat/unload leaves no stale processing.

## 9. Delivery scope and documentation

When the owner explicitly assigns this plan to Codex, implement **SALV1-A through SALV1-E as one complete working vertical slice** on `dev`. Do not ship a fake pickup system or unfinished refinery that never yields usable metal.

Document briefly in `NOTES/WORK.md`, plus a concise usage update to `TOWERS/Economy/ScrapRefinery/README.md` and relevant source-owner README only where necessary. One focused commit; push `dev`; report SHA, files, settings, source/build checks and exact Godot owner test steps. Never merge `main` without separate authorization.

**Future separate phases:** research increases to salvage chance; metal-bearing tower recipes; power-dependent refining; rare alloys/advanced material processing; campaign-persistent materials, optional wreck upgrades and strategic refinery variants.
