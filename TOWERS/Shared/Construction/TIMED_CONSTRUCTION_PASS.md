# Timed Construction, Timed Upgrades and Visual Build Stages — Codex Implementation Pass

> **STATUS: PLANNED / NOT IMPLEMENTED.** This file is the implementation brief for **C1–C4**. Complete stages sequentially, test each, and update this checklist **only after code really lands**. Follow `NOTES/CodePreferences.md` and the existing Godot 4 C# architecture. Documentation created 2026-10-16.

## Goal

Placed towers and economy buildings take **authored, configurable time** to be constructed. Their appearance visibly progresses through optional SVG/texture stages, with subtle welding sparks and completion effects. Purchased tower upgrades can also have their own **independent work durations**. Reuse existing placement, credits, inventories, ranks, stat pipeline and Inspector; **no new economy**, no construction drones/repair/saving system in this pass.

This is one shared, optional construction capability: a Battle Tower, passive Ammo Factory and 3×2 building should all use the same work-lifecycle API, without being forced to inherit each other's combat or production behavior.

### First-pass gameplay decisions (implement these defaults)

1. **Placement:** validate grid/navigation normally; charge the existing wallet **once**; claim **the entire footprint immediately**; instantiate the tower in `Constructing` state, with its construction visuals. A construction site is an obstacle exactly like a finished building. No second nav update on completion.
2. **Initial construction:** until work completes the tower is **not operational**: no targeting/firing, factory ammunition production, depot dispatch or destination deliveries, or other service provided by that structure. Its building is still selectable and its Inspector shows progress. Construction itself does not consume ammunition.
3. **Upgrade:** charge the existing next-level recipe **once at start**; remain at the **old purchased tier and old active stats** during `Upgrading`, continuing existing operation at that tier; apply the full new `TowerLevel` row/FinalStats **only when time completes**. Reject a second purchase/upgrade while busy. An upgrading building remains fully occupying the same footprint; it is never temporarily removed or repathed.
4. **Time:** use unpaused game time; construction/upgrade stops with ordinary SceneTree pause (including Results/Pause). A zero-duration authored row may complete immediately for compatibility/tests, with **no delayed operational gap**. Values must be finite and nonnegative. Each level has its own `WorkSeconds`, **not accumulated** across previous levels.
5. **Cancel/removal in this first pass:** do not add a player-facing Cancel/Refund button or a selling flow. A work job disappears safely with its structure/scene. Any failed construction **before payment commits** rolls back all cells, nodes, subscriptions, work and expense atomically. Later destruction/cancel/refund design is out of scope.
6. **No required artwork:** gameplay must work without construction-stage assets or particles, falling back to simple progress/regular art. Completed operational artwork must match the existing tower/Ammo Factory/Depot scenes.

**Why the proposed upgrade behavior:** retaining old-tier performance avoids unexpectedly switching off an established defense for a timed upgrade. Changing this policy can be a separate future gameplay decision; don't secretly make temporary downtime the default.

## Architecture that exists today — review before editing

| Existing file/authority | What must be preserved / changed narrowly |
| --- | --- |
| `TOWERS/Shared/Upgrades/TowerLevel.cs` | Level 1 is construction; later absolute `TowerLevel` rows are upgrade targets. Add individual `WorkSeconds`, copied by `CreateInstanceCopy`; preserve current compact per-weapon `*Levels.cs` authoring and existing prices/stats. |
| `TOWERS/Shared/TowerConfig.cs` | Passive Factory/Depot use **empty Levels lists**, with `ConstructionCosts` outside level rows. Give these building configs an authored construction-duration option **without inventing fake weapon stats** or assuming they have `Levels[0]`. Use a consistent fallback that only applies to passive/no-level types. |
| `TOWERS/Shared/TowerBase.cs` | Own per-instance construction phase, elapsed/total work, percent, target upgrade tier and events. Normal `UpgradeLevel`, `FinalStats`, `RankProgress`, `Ammunition`, `RuntimeChanged` remain separate. No per-tower work `_PhysicsProcess` for idle instances. |
| `TOWERS/Placement/TowerPlacementController.cs` | Already validates **entire rectangles**, atomic wallet/occupancy/rollback and selects via any occupied cell. Start construction as part of the paid creation lifecycle **before `_Ready` allows weapons/factories/logistics to activate**, not as a second placement transaction. |
| `TOWERS/Shared/Upgrades/TowerUpgradeController.cs` | Currently resolves next level and commits it immediately inside the wallet callback; refactor minimally to schedule paid work instead, with stale level/busy checks and exact single apply on completion. |
| `TOWERS/Offensive/` and `TOWERS/Economy/Ammunition/` | Battle/Laser/Blaster/Shockwave weapon activity, Ammo Factory automatic production, Depot and logistics registration/dispatch/receiving must respect whether the particular endpoint is operational. Do not merely hide an unfinished Factory while it continues producing rounds. |
| `UI/GameplayHUD/` and `TOWERS/Placement/TowerPlacementController.cs` | Selected-tower Inspector already shows upgrades, ranks and runtime info. Add a discrete status/progress view, time remaining and disabled duplicate upgrade, refreshed from construction events, not a per-frame full Inspector rebuild. |
| `TOWERS/Placement/MULTI_CELL_FOOTPRINT_PASS.md` | Factory 3×2; Depot 2×2; offensive towers 1×1; all footprint cells must be same PASSABLE/IMPASSABLE terrain class. No nav change for already-IMPASSABLE terrain; existing all-PASSABLE path validation remains. |
| `SYSTEMS/Saving/README.md` | Campaign-level saves are **not yet implemented**. Work progress should have a small restorable state contract for future saves, but **do not implement** campaign save/load now. |
| `SYSTEMS/Progression/PLAYER_XP_RESEARCH_FOUNDATION_PASS.md` | Planned player research may later reduce construction duration; **don't create an unrelated research modifier or change RP/XP in this pass**. Apply future construction speed modifiers once from authoritative inputs when designed. |

### Suggested minimal state contract

- `WorkPhase`: `Constructing`, `Operational`, `Upgrading`; optionally internal `None` while not placed. Keep exact names flexible.
- `WorkSeconds`, `ElapsedWorkSeconds`, `Progress01`, `RemainingSeconds`, `PendingUpgradeLevel` and `IsOperational`, `IsBusy`.
- State transition events: `WorkStarted`, `WorkProgressChanged` (throttled/only when needed), `VisualStageChanged`, `WorkCompleted` / an equivalent focused `RuntimeChanged`.
- **Only active jobs tick.** Prefer one small world/level-owned `ConstructionWorkManager` tracking a set of constructing/upgrading towers, using one process/physics callback for active jobs, stopping the manager when empty. Avoid mandatory construction helper nodes and loops for each idle tower.
- Full stage and completion notification must happen after coherent state transitions. Subscription/unsubscription must tolerate early failure, repeated removal and unloading.

## C1 — Authorable work duration, state transitions and timer service

- [ ] Add `WorkSeconds` to the plain `TowerLevel` row (Level1 = first build, Level2+ = duration of that purchased tier), preserve copy/validation. Extend each real offensive `*Levels.cs` in its existing one-row-per-level style, **without replacing the current stat/price data**.
- [ ] Passive `AmmoFactoryConfig` and `AmmoDepotConfig` have no `TowerLevel` rows: expose a simple `ConstructionSeconds`/fallback on shared `TowerConfig` (or specific existing building configs) for types with empty `Levels`. Do not fabricate combat rows or run `ResolveLevel` for passive buildings.
- [ ] Use modest **provisional, readily editable** positive build/upgrade times for real playable buildings, with zero-time compatibility for tests/fixtures and optional types. **No balance rework**; document actual chosen seconds. Validation rejects NaN, Infinity and negative seconds.
- [ ] Implement per-instance work-state API and one sparse active-job scheduler. Correct exact-boundary completion, repeated completion idempotence, pause/resume and freeing before completion. Skip finished/removed nodes. Expose clean events for the Inspector and stage presenter.
- [ ] Add focused headless tests for phases, 0/positive times, carry/precision, pause, scheduling many active works without iterating all idle towers, independent instances, invalid durations and cleanup.

**C1 done:** deterministic work lifetime and per-row durations work without changing actual combat/economy yet.

## C2 — Timed initial placement and all capability gating

- [ ] Wire `TowerPlacementController.CommitTower` to start the construction state **before** adding the tower to the live scene. Preserve the same single `EconomyManager.TryPurchase`, whole-footprint claim, one instance per building, and full rollback on any failed scene/preflight/index/work initialization.
- [ ] No weapon targeting, projectiles, beams, splash or shockwaves while initial construction is incomplete. On exact completion, enable the normal behavior **once**, without target-cooldown or damage duplication. Preserve XP and purchased level=1; no instant rank promotions from mere construction.
- [ ] Ensure Ammo Factory creates its valid internal helper if needed but **does not produce** until built. Depot cannot launch/receive legitimate shipments until operational; logistics must check both source and destination and cancel/avoid reservations/jobs for inactive endpoints. Tower ammo stock/capacity may be initialized privately, but unfinished weapon consumers should not attract delivery drones. On completion, resupply/production resumes through existing scheduling automatically.
- [ ] Test an incomplete 3×2 Factory and 2×2 Depot: fully occupied/selected, no free production/stock shipments/reserved delivery slots, and normal supply pipeline after completion. Preserve **Factory 100 per 2 seconds, full 1,000 shipments, Depot 10,000 capacity** and established drone physics. No duplicate registration.
- [ ] Preserve placed 1×1 offensive weapons, their current ammo and ranks, terrain structural rule, path checking and performance. **Completion causes no extra occupancy/repath**; removal during work releases every occupied cell exactly once. Headless tests for invalid placement/insufficient funds, rollback, all-IMPASSABLE versus all-PASSABLE navigation and direct scene instantiation without placement (test fixtures) behaving safely.

**C2 done:** every real catalog building starts as an occupied, non-operational construction site and activates normally on completion.

## C3 — Timed paid upgrades and selected Inspector experience

- [ ] Update upgrade controller so payment schedules the **specific next tier**. Keep `UpgradeLevel`, `FinalStats`, aiming, attack timers, ammunition contents and earned Rank XP **unchanged while Upgrading**. Preserve old-tier operation throughout.
- [ ] At completion resolve/commit the **one selected absolute target row** using the then-valid Rank context, notify stats and synchronize ammo capacity exactly once. A rank promotion during work must remain accurate: do not apply a stale precomputed snapshot from upgrade-start and overwrite earned rank. No refill of spent ammunition on upgrade.
- [ ] Guard against repeated mouse clicks, stale expectedLevel, insufficient secondary resources, sold/freed towers (future), failed payment and max-tier requests. One successful paid upgrade = one delayed tier transition and exactly one resource deduction. Do not let Inspector offer an additional upgrade mid-work.
- [ ] Add clear selected-tower Inspector section: `CONSTRUCTING / UPGRADING / OPERATIONAL`, small progress bar, `elapsed / total` or seconds left, and `Upgrading to Level N` when relevant. Show what old tier currently provides and what next tier will provide; preview remains free. No per-frame rebuilding of entire Inspector, especially at 500 enemies.
- [ ] Use work events to refresh only the selected building; update UI only at useful intervals (e.g. changed percent or 0.1s). Closing/reselecting Inspector reads current state; changing selection and unloading unsubscribes cleanly.
- [ ] Regression tests: level1→2→3 timings, purchase-at-start, no early stats, accurate ranked completion, no duplicate payment, existing multi-resource rollback and Inspector errors, pause/resume, build+upgrade overlap across different instances.

**C3 done:** purchased upgrades take real time, retain old-tier activity, and the Inspector accurately shows work status.

## C4 — SVG build stages, optional spark/VFX hooks and full verification

**Visual architecture: presentation is optional, not game state.**

- [ ] Provide a small reusable construction-visual component, used by any `TowerBase` without inserting per-building state machines. The **TowerDefinition / tower config or optional stage-presenter resource** should expose a series of `(threshold 0..1, Texture2D/SVG asset)` entries and optional effect references. Node/art transform and anchor align to the **whole rectangular footprint**, not one tile. Do not hardcode 4 stages, and do not require every tower to author stages on day one.
- [ ] Support e.g. `stage0.svg` (bare foundation), `stage1.svg` (frame), `stage2.svg` (nearly complete), final existing finished SVG as default operational stage. Discrete progress thresholds (such as 0%, 35%, 70%, 100%) are editable; update the sprite **only when stage index changes**, not every frame. If stages are missing/invalid, fall back to existing sprite/progress UI with no gameplay impact.
- [ ] For an actual first visual demo, supply **minimal placeholder stage visuals** for at least one **1×1 Battle Tower** and one **3×2 Ammunition Factory** (or a reusable stage overlay proven against both), reusing existing SVG vocabulary. Do not overwrite current final artwork or manually adjust authored level terrain. Different stage assets may have different numbers, and factory/depot visuals should stay centered within their full footprint.
- [ ] Add optional low-cost construction effect(s): a small welding spark emitter / intermittent flash during work, plus optional one-shot completion flash/puff. Reuse `FEEDBACK/` patterns. Respect Pause, cleanup and deletion; no effects during build-ghost preview, idle operation or offscreen when reasonable. A building with **no VFX or SVG stages works identically**.
- [ ] **Upgrades are visually distinct from rebuilding a structure**: retain the existing live old-tier model while working, with an optional scaffolding/glow overlay and completion cue. Only the initial build needs the bare-to-finished sequence. Do not replace/remove a live turret sprite in a way that breaks turret rotation or attacks during upgrade.
- [ ] Visuals must not own work progress, change physics/collisions, block input or modify `WorldGrid`. Do not instantiate an extra visual node every construction frame. Stop looping emitters and clean one-shot effects when a scene exits or construction ends.
- [ ] Validate appearance and transitions for construction, upgrading and completion, including missing SVG and VFX resources, mid-progress tower selection, 1×1 and 3×2 structures, different authored durations, pause/resume and rapid scene unload. Provide a real Godot editor screenshot/playtest checklist; headless geometry checks alone don't establish visual quality.
- [ ] Run focused new `TESTS/Towers/ConstructionChecks.cs` + `*.tscn` (or existing-suite inclusion), existing `TESTS/Towers/TowerUpgradeChecks.cs`, `TESTS/Towers/FootprintChecks.cs`, `TESTS/Economy/AmmunitionLoopChecks.cs`, `TESTS/UI/MenuTowerFlowChecks.cs`, Citadel/Results navigation and enemy combat/headless integration suites. Record *actual* executed results and known pre-existing environment failures.

**C4 done:** players visibly see construction stages and optional sparks, without gameplay depending on stage art.

## Deferred — explicitly NOT part of C1–C4

- Refund/cancel UI, selling, destroying/damaging construction sites, tower HP/shields, repair drones, builder workforce/delivery vehicles, timed research, electricity shortages, material supply chains and wear.
- Full campaign/level save/restore; do define future serializable **source state** (phase, target tier, elapsed/total time) without writing save infrastructure. Never serialize derived `FinalStats`, active tweens or particle internals.
- Permanent Mass Production research construction-speed bonuses. Coordinate later with `SYSTEMS/Progression/`: do not prematurely modify profile XP, RP or planned research catalog.
- Optional animated cranes/robot builders, per-piece skeletal assembly, continuous geometry deformation and complex stage blending. The first pass can use simple SVG stage swaps + lightweight particles.
- Major rebalancing of tower damage/range/ammo, multi-cell footprint dimensions, production batch size or drone shipment sizes.

## Final acceptance checklist (Codex)

- [ ] Costs are charged **once at construction/upgrade start**, never at completion or stage-change.
- [ ] Building occupies whole rectangle immediately; routing/terrain rules remain identical to the completed footprint pass.
- [ ] New structures cannot operate until built; Factory/Depot logistics do not bypass this.
- [ ] Timers advance only in unpaused game time; cleanup is safe on fail, deletion and scene unload.
- [ ] Old tower level/stats remain active during timed upgrade; next level applies once on completion.
- [ ] Inspector shows status + correct progress; base gameplay works without SVG/VFX assignments.
- [ ] At least one 1×1 and one 3×2 construction demonstrate authored stage imagery and optional sparks/completion feedback.
- [ ] No unrelated ammunition, rank, wallet, navigation, tower-level balance or Campaign/save changes.
- [ ] Document verified tests and actual live-view limitations; update this status, `TOWERS/Shared/Construction/README.md`, `TOWERS/Shared/Upgrades/README.md`, `TOWERS/README.md` and relevant tower/Ammo READMEs **only for work actually completed**.

### Suggested Codex request

Read `TOWERS/Shared/Construction/TIMED_CONSTRUCTION_PASS.md` and `NOTES/CodePreferences.md`. Implement **C1–C4 sequentially**, checking the current GitHub HEAD for other work before changes and validating after each phase. Preserve generic tower/economy code and existing footage/upgrade/payment/navigation behavior. Provide a list of changed files, timings chosen, verification and any explicitly deferred items.
