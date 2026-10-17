# Codex Implementation Pass — Player XP, Campaign Hub and First Research Node

## User-approved sequence amendment — 2026-10-17

**Read this before the original P1–P5 ordering below. Nothing in the original specification is removed.** The user now wants actual mission statistics and usable research ahead of XP scoring/balancing. The original scoring formulas remain proposals for a later pass, not a prerequisite for Research.

### Revised implementation order and current status

1. **Initial XP/level/RP storage + menu display: implemented.** Keep the existing ProgressionService, stored curve, separate profile GUID file and development-only grant.
2. **Real mission-stat foundation: implemented.** LevelRunStats counts successful paid placements, completed initial builds, successful paid upgrades and unpaused game time. Placement is not construction completion. Session freezes these with official R1 killed/breached/waves/integrity data. Results renders ten rows with a compact stat scroller; Credits remain null until actual transaction telemetry exists.
3. **Campaign-only Research Room and purchase/effect loop: implemented.** Campaign has RESEARCH alongside the existing level launch actions. No entry from Results or gameplay; purchases require the actual active ResearchMenu scene. A central free Research Core connects to Citadel Master I (1 RP, +10% starting max/current Citadel HP). Hex axial coordinates support six-direction expansion, pan/zoom and mouse/keyboard selection. Info includes prerequisites, description, effect, RP cost and explicit state. Purchases save once and display RESEARCHED; insufficient/locked/repeated/failed-save requests cannot charge. Profile v1 migration to v2 preserves XP, curve and RP; v2 adds immutable purchased IDs. See Research/README.md.
4. **Next research work:** author approved real nodes/effects incrementally. Common effects may share handlers; unusual mechanics need explicit handlers. Future tower-stat research belongs in 04_PermanentResearch with cached per-level snapshots, not authored tower-level edits. No tower damage modifier is claimed implemented by the Citadel-only proof node.
5. **Later scoring pass (remaining P1 + P2 / R4): OPEN and intentionally deferred.** Choose mission reward/scoring rules, then implement deterministic breakdowns, first-clear/replay data and persisted once-only run markers before wiring Results grants. The prior formulas and anti-farming notes below remain available for that decision. Current missions do not award or deduct XP.
6. **Remaining menu/polish work:** separate LEVELS area from the full P3 design remains open; R2 presentation/sounds and Credits/detailed R3 metrics remain open. Full P5 should not be marked complete; only the focused Research/profile/stat checks recorded in TESTS/Research/README.md have been verified.

The original combined P1–P5 checkboxes below are intentionally retained as the complete long-term brief. This amendment records partial/overlapping delivery and is authoritative for the next task. Campaign/battlefield saving, save slots and offline progression remain separate and unimplemented.

---

> **STATUS: PARTIAL — initial profile progression/menu slice implemented; full P1–P5 remain open.** Implement in the ordered, testable phases P1–P5 below. This is a narrow **first playable foundation**, not the complete research tree, account economy, or full campaign save/load. Last checked against the Godot C# repository on 2026-10-16. Follow `NOTES/CodePreferences.md` and the current code before writing.

## Smaller first slice selected by user (2026-10-17)

Implemented the XP/level/RP record, atomic separate store, selected-profile grant API, editable starting curve and shared Main/Campaign display. Default-off DEBUG grant enables verification. Curve parameters persist with each saved record. See README.md for ownership and TESTS/Progression/README.md for validation limits. P1's mission calculator, replay/first-clear history and award-token persistence are still absent; its combined checkboxes intentionally remain open. P2 and P4 are unimplemented; P3 only has the progression display, without the hub split. Preserve this foundation rather than create a second XP service.

## Player experience and intended menu flow

- All newly created **profiles** start at **Player Level 1**, **0 Player XP** and **0 Research Points (RP)**.
- A level completion generates a deterministic **Player XP** breakdown: **authored baseline first**, then separate performance **bonuses and deductions**. Future scoring inputs (tower efficiency, special objectives, kill categories, etc.) must be addable without rewriting the Results UI or persistence.
- Enough Player XP automatically grants a Player Level; **every level-up grants +1 spendable RP**, including multiple level-ups from one mission. Display names/titles for player levels are a later cosmetic feature; numeric levels remain the truth.
- RP is spent **only in the Campaign Research room**, not during an active level. Earned XP is never spent.
- **Campaign Hub** displays selected **Profile Name / Player Level / XP toward the next Level** (and RP balance) and has two obvious entries: **LEVELS** and **RESEARCH**. The Levels area keeps the existing Level001/Level002 launches; Research opens the research map. Back navigation returns to Campaign/Main cleanly.
- Research is eventually a **very large expandable hex/radial tech tree**: one **central origin node** with six outward directions/branches and prerequisites connecting later nodes. First version needs a real hex-style center, connector, pan/zoom foundation, and **one actually purchasable, functional upgrade**. Do not fill the tree with fake upgrades.
- Player XP and unlocked research are **global to the selected profile across TD levels**, not per individual tower and not a session Credits currency. This foundation does NOT implement campaign slots, pause Save/Load, mission saves, offline progression, other game modes, doctrine equip slots or other research nodes.
- Future hybrid-game/campaign ownership may need deliberate migration; isolate data behind a dedicated progression service and stable IDs rather than writing ad-hoc values to UI or relying on names.

## Current repo integration points — inspect first

- `SYSTEMS/Profiles/PlayerProfile.cs`, `ProfileStore.cs` and `README.md`: profile identity/selection + versioned index only; **do not reset/rewrite existing profiles**.
- `SYSTEMS/Saving/README.md`: campaign/session save system is NOT implemented; do not pretend progression is a full campaign save. This pass may persist **profile-wide progression separately** without activating campaign Save/Load.
- `UI/Menus/Campaign/CampaignMenu.cs/.tscn`, its `README.md`, `UI/Menus/MenuNavigation.cs`, `UI/Shared/MenuLayout.cs` and `UIButtonFactory.cs`: current Campaign directly lists Level001 and Level002. Reuse existing styles and valid scene launchers.
- `LEVELS/Session/LevelSessionController.cs`, `LevelResultSnapshot.cs`, `PlayableLevelCatalog.cs`: R1 outcome arbitration and immutable mission snapshot already exist; **do not duplicate victory/defeat or wave ownership**.
- `UI/Menus/Results/LevelResults.cs`, `README.md`: existing Results includes eight stat rows and an unimplemented Player XP placeholder; replace that placeholder rather than creating a second results overlay. This extends the existing **R3 actual-stat telemetry** and **R4 XP** work, not the optional R2 animation-polish work.
- `LEVELS/LevelConfig.cs`, `Level001Config.tres`, `Level002Config.tres`: editable per-level resources; add the baseline and target completion time here (not to an enemy definition or tower stats).
- `WORLD/OBJECTIVES/Citadel/CitadelSession.cs` / `CitadelHealth`: initialize **effective Citadel max HP** using LevelConfig and purchased research before notifying the HUD. Do not mutate the authored `.tres`.
- `TOWERS/Shared/Stats/Pipeline/04_PermanentResearch.cs`: reserved neutral research stat stage. Leave it neutral for a Citadel-only first node; later **tower** research uses this stage rather than mutating `*Levels.cs` rows.
- `TESTS/UI/ResultsChecks.tscn`, `MenuTowerFlowChecks.tscn`, `MenuFoundationChecks`, `TESTS/Integration/EnemyIntegrationChecks.tscn`, profile and Citadel checks.

## Foundation data boundaries

1. **One authoritative, versioned profile progression record per profile GUID**, suggested:
   `user://Profiles/{profileId}/progression.json` (under `SYSTEMS/Progression/` service).
   Suggested source fields: `Version`, `LifetimePlayerXP`, `UnspentResearchPoints`, `PurchasedResearchIds`, `FirstCompletedLevelIds`, and an idempotent awarded-run marker/short bounded ledger as needed. The selected profile GUID is the file owner. Store **no full scene, enemies, tower instances, derived tower FinalStats, or per-run Credits**. Define a robust derivation/curve-version strategy so changing balance constants doesn't unexpectedly reset or corrupt earned player levels.
2. **Do not overload `PlayerProfile` identity or overwrite the profile-index JSON schema blindly.** Reuse `ProfileStore.Selected.Id` and the existing validated atomic-write + `.tmp`/backup recovery pattern for the new progression store. Missing progression file means new Level 1 / 0 XP / 0 RP; corrupted data must never silently reset it or award duplicate RP.
3. One shared, Inspector-editable `ProgressionMasterConfig.cs` + default `.tres` for player XP curve/replay rate/general scoring caps; per-level `LevelConfig` exposes **VictoryBaseXP** and **TargetCompletionSeconds** in an Inspector "Player XP Rewards" group. `TargetCompletionSeconds <= 0` means **no time modifier** until a level designer authors an ETA. Validate malformed negative/nonfinite settings before scoring.
4. Maintain **strictly separate meanings**: Player XP (permanent level progress), RP (spendable unlock currency), tower Rank XP (existing earned tower progression), Credits (existing level wallet). Research purchases must never spend Credits or alter tower Rank XP.
5. Research catalog definitions should have **stable string IDs** (never display names as keys), label, description, RP cost, prerequisite IDs, axial `(q,r)` coordinate, and a small explicit effect identifier/data. Validate unique IDs, acyclic prerequisites, missing dependencies and coordinate collisions at startup. A persistent purchase stores its ID only; recompute effects from purchased IDs on future level initialization. Never serialize function pointers or final applied stats.
6. Keep all save/reward/business logic independent of Control scenes, and no permanent progression autoload tied to gameplay frames. The UI reads an immutable view and subscribes/refreshes on explicit profile/progression changes.

## Provisional player-level curve and RP grant

**Balance placeholders — centralized and editable, not hardcoded in UI:**

- Level 1 → 2: **500 XP**; further levels cost **500 + 250 × (currentLevel − 1)** XP each. This is a simple illustrative linear threshold, not a forever-mandated balance. No maximum player level for the initial test beyond checked numeric safety.
- Every crossed threshold awards **+1 RP**. E.g. a single large mission grant crossing two levels awards 2 RP and carries remaining XP into the new level. Merely loading a profile or showing Results never awards points.
- Campaign header shows `LEVEL 1 — 350 / 500 XP`; after promotion, display the new level and its next threshold. Show spendable RP separately. An unlocked research node consumes RP **once** and remains owned after relaunch.

## Mission XP calculation contract (first pass)

**Base XP first, then additive modifiers**, then replay adjustment, then safe nonnegative final clamp. All itemized values and the resulting total are frozen alongside the mission outcome; the Results UI only displays them.

Suggested default values, all provisional:
- `VictoryBaseXP = 500` (per-LevelConfig). **Defeat awards 0 XP** for this first pass; a later configurable defeat-reward design can change that explicitly.
- **Citadel integrity bonus:** `round(100 × CurrentHP / MaxHP)`, bounded to 0..100. Zero MaxHP is invalid; never divide by zero.
- **Time bonus/penalty** if TargetCompletionSeconds > 0: `timeRatio = clamp((target - unpausedActualSeconds) / target, -1, +1)`. Positive ratio awards `round(100 × timeRatio)`; negative subtracts `round(100 × abs(timeRatio))`. E.g. target **600 s**, actual **480 s** → **+20 XP**; actual **900 s** → **−50 XP**. A disabled ETA gives exactly zero in both cases. Never count paused time, results reveals, menu time or wall-clock gaps.
- **Enemy breaches:** `−min(10 × official wave-owned breaches, 100)` XP (reuse R1's official tally; F2/unowned deaths or spawned debug entities do not count). No extra XP awarded simply for spawning/killing arbitrary debug enemies.
- **Raw victory XP** = `max(0, base + integrityBonus + signedTimeAdjustment − breachPenalty)`. Example: base500 + integrity80 + time20 − 2 breaches ×10 = **580 XP**. Show exact itemization and sign.
- **Replays:** first **successful** completion of each stable registered level ID earns 100% of calculated XP; subsequent victories award **25%** (configurable `ReplayXpMultiplier = 0.25`, consistently rounded) to limit trivial farming. Failed runs do NOT mark first completion. Display the replay multiplier explicitly if applied. Do not confuse a results retry with a unique **first clear**.
- Capture an immutable `MissionXpBreakdown` (or similarly small DTO): baseline, each signed modifier, replay adjustment, XP awarded, before/after player level and current XP / RP gained. The **calculator** is pure and deterministic from the frozen official mission stats + authored level settings + existing completion history; **the UI must not calculate rewards**, grant progression or mutate gameplay.
- Keep score modifiers as small named contributors or fields so a future pass can add tower efficiency, no-loss bonus, difficulty, optional objectives and other performance factors without changing reward storage. **Do not implement** a giant pluggable rule/reflection engine for one or two modifiers. Do not award player XP per bullet, per enemy death or each frame.
- Malformed thresholds, overflow, absent data and negative penalties must safely clamp/error. Never produce negative XP or subtract already-earned lifetime XP because of a slow mission.

## P1 — Profile-wide XP/RP storage and pure scoring foundation

- [ ] Add `SYSTEMS/Progression/` code and README: profile progression record, validated atomic store, small shared progression manager/accessor, editable `ProgressionMasterConfig` with `.tres`. If a selected profile has no progression file, treat it as Level1/0XP/0RP without rewriting existing identity data.
- [ ] Implement Player Level thresholds, XP carry across one/several levels, +1 RP per level gained and replay/first-completion history with stable `Level001`/`Level002` identifiers. Persist one coherent update of XP, RP and first-clear status. Validate and guard against overflow/invalid duplicate grants.
- [ ] Implement pure `MissionXpCalculator` + immutable breakdown. Add `LevelConfig.VictoryBaseXP`, `TargetCompletionSeconds`; leave default ETA off until authored. Preserve all LevelConfig spawn, Citadel health, wave and terrain values.
- [ ] No gameplay, UI, Citadel or tower stats changed yet. Write focused pure tests: default level1, exact threshold, multi-level grant, carry, defeat zero, target early/late/on-time, disabled ETA, clamped negative, breached enemies, replay reduction, old profile migration, profile isolation, corrupted save/backup.

## P2 — Official mission stats and once-only Results XP award

- [ ] Add minimal event/clock telemetry for **unpaused mission time** (may be a lightweight level-owned `LevelRunStats`), and reuse existing verified R1 `EnemiesBreached`, `CitadelHealth`, wave provenance and outcome. Preserve nullable placeholder rows for unrelated Credits/tower fields if not yet tracked; do not fabricate stats. Optional R3 Credits/tower event counts may be implemented separately.
- [ ] Extend `LevelResultSnapshot` or a companion immutable awarded-result DTO with required scoring facts/breakdown. **Freeze gameplay/outcome and capture exactly once**, respecting R1's final lethal-breach race and E3A offspring wave ownership.
- [ ] The **level session / progression authority**, not `LevelResults`, computes and **commits an XP award once** after an authenticated final victory. Generate a unique run ID at mission start, prevent duplicate claims from repeated results, Skip Reveal, Retry, scene transitions, unloaded callbacks or repeated events; persist its award token/history consistently with the award. Failures to save must be visible; do not show a fictitious successful grant or silently lose it. Use a bounded idempotency strategy, not an ever-growing raw event list.
- [ ] `LevelResults` replaces `PLAYER EXPERIENCE // COMING SOON` with detailed **Base XP / Integrity / Time (bonus or penalty) / Breaches / Replay reduction / TOTAL XP**, then before→after player level, progress bar and `+N Research Points` when promoted. Animation and Skip only affect presentation, **never reward timing**. Defeat shows 0 earned in this phase.
- [ ] **Direct F6/test runner with no selected profile still works**. It can calculate/display an informational breakdown but explicitly displays "Profile progression not saved" and performs **no profile mutation**. No attempt to silently create/select a profile during tests.
- [ ] Dedicated tests: exactly-once victory, lethal defeat, double results/Skip/navigation, 0/1/multiple level-ups, time paused, E3A wave children, direct launch without profile, restart + persisted profile, and save-failure recovery.

## P3 — Campaign hub and actual Levels area

- [ ] Rework `CampaignMenu.tscn/.cs` into a compact **hub**, showing selected profile name, **Player Level**, `XP current / XP required`, a progress bar and current unspent RP. Use existing `MenuLayout`, `UIButtonFactory`, `UIMasterConfig`; keep the established futuristic theme and Back to Main.
- [ ] Two primary navigation entries: **LEVELS** and **RESEARCH**. Levels opens a simple Levels panel/scene with **the existing playable Level001 and Level002 entries**, using `PlayableLevelCatalog`/existing launcher paths. Do NOT duplicate the wave map/service scenes or build a generic map loader here.
- [ ] When returning from a victory, Campaign reads persisted progression so level, XP and RP reflect new rewards. Switching profiles displays isolated values. Missing/no selected profile still redirects to Boot as before; preserve direct F6 scene tests.
- [ ] Do NOT introduce fake campaign-slot, level-unlock, Continue, Load, New Campaign or Save actions. These are out of scope until `SYSTEMS/Saving/` is implemented. Existing levels remain directly playable as they are today.

## P4 — Hex research room and first fully working node

- [ ] Add `UI/Menus/Campaign/Research/` (or similarly cohesive folder) with a **real navigable Research Room**, built from a center-origin node graph. Use a scrollable/pannable, zoomable `Control`/custom drawing surface, lightweight hexagon-shaped nodes and connector lines, mouse interaction/tooltips, focus/keyboard alternative and explicit Back to Campaign.
- [ ] Central origin **`Research Core`** at axial coordinate `(0,0)`, **always available and free**; it is the root/prerequisite identity for future research, not a paid upgrade or extra RP award. Future branches can expand outward in six axial directions using stored node coordinates and prerequisite graph edges. No hardcoded screen positions per upgrade and no huge placeholder tree; visual octagonal ornaments are optional.
- [ ] First **purchasable** node: **`Citadel Master I`**, ID `citadel_master_1`, axial `(1,0)`, cost **1 RP**, prerequisite `research_core`, permanently **+10% Citadel maximum HP**. This is adapted from the user's original Research Center concept. Clear description and status: `LOCKED`, `AVAILABLE (1 RP)`, `INSUFFICIENT RP`, `RESEARCHED` (as applicable).
- [ ] Clicking a node must show a readable information panel: name, short flavour text, exact effect, prerequisite(s), current state, RP price and a working Purchase action. Locked/ineligible purchase does not charge; an already bought upgrade cannot charge twice. Save the purchased ID on successful verified payment and refresh both Research/Hub RP displays.
- [ ] **Real gameplay effect:** on initialization of a future launched level, compute effective Citadel max HP as `round(authoredLevelConfig.CitadelMaxHealth × 1.10)` when `citadel_master_1` is purchased by the active profile, then initialize `CitadelHealth` with that effective value and update HUD/results denominators consistently. Do not mutate `LevelConfig.tres`; no research effect on direct profileless F6 tests. Research purchase must persist and apply across Level001 and Level002 and after application restart.
- [ ] Research state must never apply by repeatedly multiplying the current Citadel health/max health each time a scene opens or the results animate. Each level starts from immutable authored max HP and resolves its profile bonuses **once**. Keep core `CitadelHealth` reusable.
- [ ] Future **tower stat** research should plug into the existing neutral `04_PermanentResearch` stage of the eight-stage pipeline, with explicit cached snapshot invalidation, not overwrite authored *Levels.cs; that additional modifier is **not** needed just to prove Citadel Master I.
- [ ] P4 acceptance includes an actual live transition: finish mission → gain Level2 and 1 RP → Campaign Research → buy Citadel Master I → replay/start Level001/Level002 → Citadel has 10% larger max/current starting HP; purchase still present after restart. Reject repeat purchases and insufficient-RP actions with no balance change.

## P5 — Consolidation, regression and future-ready expansion points

- [ ] Test complete profile A / profile B isolation including different purchased-node effects, XP levels, FirstClear flags and replay multipliers; verify on-disk persistence and corrupt backup handling.
- [ ] Test legitimate XP completion, time comparison/positive and negative signed modifiers, correct UI tally, XP wrap on promotion, multiple RP awards, zero/defeat reward and absolutely no double grant via navigation/reopen/Skip.
- [ ] Test research catalog validation (missing prereq, duplicate ID, cycle, duplicate axial coordinates, nonpositive cost), six-direction center-origin layout, pan/zoom/click target parity, one-time RP purchase, effective Citadel HP on both levels, and unaffected direct-launch/testing.
- [ ] Re-run relevant profile/menu, R1 results, Citadel, placement/footprint, enemy wave/E3A and headless integration regression checks. Run `dotnet build` and Godot .NET headless test runners if environment supports them; report actual results and known blockers. Test Research window layout at typical resolutions in the editor; headless tests are not a visual acceptance guarantee.
- [ ] Update the checkbox statuses, `SYSTEMS/Progression/README.md`, `SYSTEMS/Profiles/README.md`, `UI/Menus/Campaign/README.md`, `UI/Menus/Results/README.md`, `LEVELS/Session/README.md`, `SYSTEMS/Saving/README.md`, relevant tests and root README **only once actual implementation is verified**. Avoid stale "COMING SOON" player XP labels after the feature lands.
- [ ] Preserve R1 results presentation, tower Rank XP, existing ammo/drone economics, 1×1/3×2/2×2 footprints, navigation, balances, and the six-neighbour foundation placement rule. Do not change existing level unlock/selection behavior inadvertently.

## Explicitly deferred decisions / future nodes

- Larger 100+ node research catalog; branching choices, multi-prerequisite projects, DME/Zy0n unlock gates, equipment/doctrine slots, researching brand-new towers, icon art, click-to-select research bundles, research refunds/respec and research levels above tier I.
- Profile rename/custom player title display, clan/faction systems, online accounts, campaign slot creation and saved battlefield states.
- Additional performance scoring parameters (efficiency, optional objectives, score multipliers, level challenge ratings, speedrun records, survival rewards) can be added later as explicit contributors. Don't introduce empty systems for these.
- Research options should be **global/permanent by default**; a future mission doctrine/equip system may limit powerful active techniques, but first version does not need any equip inventory.
- Research Core and Citadel Master I prove the catalog/graph/purchase/activation architecture; their IDs and profile storage must allow expansion without UI/control scene rewrites.

## Definition of done

A newly created profile enters Campaign with **Level 1, 0 / 500 XP, 0 RP**. Campaign visibly offers **LEVELS** and **RESEARCH**. Winning a registered level shows an **authentic itemized XP breakdown** (base plus positive and negative performance modifiers), grants progress **once**, and earns +1 RP per level gained, with save/reload and profile separation. Research shows a **central hex root** with one connected **Citadel Master I** node (cost 1 RP). After purchase, both actual levels initialize the Citadel with **10% more max HP**; the node and RP balance persist. The new foundation does not change any other tower/combat system or masquerade as complete campaign saving.
