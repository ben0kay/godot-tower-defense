## Elite mk.1 plain heavy interceptor — 2026-10-17

- [x] Added `Types/Enemy006` (`elite_mk1`) as a new inherited plain enemy, 80×60 SVG, 1,000 hull HP, 250 optional shields, 100px/s, 24px radius, 5 Citadel damage and 10 Credits.
- [x] Registered in the default catalog after Big mk.1 for F2/F3/waves, with no changes to Level001/Level002 waves, shared enemies, navigation, abilities or combat.
- [x] Updated catalog integration checks and enemy documentation in the same commit.
- [ ] Godot .NET compilation/headless catalog suite and in-game silhouette, shield FX and F2 route test pending.

---

## Stable two-row build browser — 2026-10-17

- Ctrl opening now clears the old tower selection and shows the **empty Inspector shell immediately**. Hover/focus fills the same reserved panel with existing build details/cost/affordability; no tile can be displaced by the panel appearing.
- Tray width **always reserves Inspector width plus the existing gutter**, including while details are hidden. The default 560×330 Inspector scene is unchanged. The tray uses two rows of catalog tiles; further columns scroll horizontally. Category filters recompute columns from visible entries and reset horizontal scroll; keyboard focus follows off-screen tiles.
- The shared BottomBarHeight default is now 260px to fit header, tabs, two 65px tile rows and the horizontal scrollbar. Width remains responsive in UIMasterConfig/GameplayHUD. A compact-width header hides its hint when needed.
- Affordable choice still hides the lower HUD while placing. RMB/Escape/success restore the tray plus blank shell; Ctrl closes. Successful placement remains unselected. IsInspectionVisible continues to mean **active details**, not merely the empty shell, preventing stale tower events from refilling it.
- Verification: SDK build, PlacementHudChecks (both maps plus real Rocket mouse hit/click at 1920×1080,1366×768,960×540; blank shell/stable panel bounds; 40 entries/two-row horizontal overflow/category reflow) CompactInspectorChecks and HudCategoryChecks (including the dynamic 40-type tray) passed. Desktop visual review is still pending.

This amendment supersedes older statements that cancellation must hide the entire Inspector panel. No shield/combat/research/scoring/Inspector-art changes are included.

---

## P3 Campaign hub / Levels split — implemented 2026-10-17

- Campaign now shows the selected profile's saved player level, XP and Research Points, followed by distinct **LEVELS** and **RESEARCH** destinations.
- **LEVELS** opens UI/Menus/Campaign/Levels/LevelSelectionMenu.cs/.tscn with the existing Level001 and Level002 mission descriptions/launch buttons. Back returns to Campaign. Research continues to return to Campaign; Results already returns to this hub after mission rewards.
- Existing MenuLayout, UIButtonFactory, profile guards and MenuNavigation are reused. No Research access was added to Results/gameplay. Both missions remain available; mission unlocking, completion stars, campaign saves/slots and Continue/Load remain separate future work.
- This completes the remaining **P3 menu organization slice**, alongside the previously implemented progression display. It does not complete all P1–P5/R2/R4 scoring or polish.
- Verification: SDK build succeeded with zero warnings/errors. MenuTowerFlowChecks, ResearchMenuChecks and ResultsChecks passed; viewport checks passed at 1920×1080, 1366×768 and 960×540. The broader controls runner hit a native Godot finalizer/resource crash; no full-suite pass is claimed. Details are recorded in TESTS/UI/CampaignHub.md. Desktop visual review remains for the user.

Older menu descriptions below are retained as history and superseded by this amendment.

---

## Mission XP connection — implemented 2026-10-17

User approved a provisional, tunable mission-to-profile XP loop before finalized scoring. **This amendment supersedes older statements that missions never grant XP; preserve the original P1–P5/R4 design below as future scope.**

- **Delivered remaining P1/P2/R4 foundation:** each level exposes VictoryBaseXP / DefeatBaseXP (500 / 0 initially); deterministic itemized scoring; captured deployment profile/settings; persisted once-only mission numbers/receipt; atomic XP + level-up RP + award marker; honest failed-save UI and retry. v1/v2 progression migrates to v3 without resetting XP, research, RP or stored curve.
- **Delivered Results/Campaign loop:** saved XP itemization, actual level changes/RP and final within-level bar; show/Skip/reopen never grants. RETURN TO CAMPAIGN opens the existing hub. Research remains Campaign-only. Profileless F6/preview says NOT SAVED.
- **Intentionally deferred original scoring:** first-clear/replay history, replay reduction/anti-farming policy, ETA/time modifiers, kill/tower/performance bonuses and deductions. Replays currently earn the same baseline; signed line-item data is ready for later reward deductions floored at zero, without removing earned levels.
- **Still open:** separate P3 Levels area, mission unlock progression, detailed XP count-up/sounds/R2 polish, Credits telemetry, campaign/battlefield Save–Load/slots and full P5 regressions. Do not mark the combined P1–P5/R4 long-term brief fully complete.
- **Verification:** .NET SDK build (0 warnings/errors), MissionXPChecks, PlayerProgressionChecks, ResultsChecks, ResearchFoundationChecks, ResearchMenuChecks and BattleResearchChecks passed. Results-frame geometry verified at 1920×1080, 1366×768 and 960×540; desktop visual review remains. Existing unrelated historical full-suite blockers remain recorded below.
- **Failure boundary:** leaving a mission whose XP could not save does not persist a recoverable pending battlefield/reward; use RETRY XP SAVE before leaving. Monotonic watermarks assume the existing single active battlefield/application flow.

Authoring/ownership: SYSTEMS/Progression/Missions/README.md. Validation: TESTS/Progression/MissionXP.md.

---

## Tower HP / shared DefenseBar foundation — implemented 2026-10-17

- [x] **All registered towers/structures have hull:** provisional 100 HP, offensive per-level MaxHealth through the existing eight-stage pipeline, active total rank HpBonus, passive Factory/Depot Inspector health setting. No final HP balance implied.
- [x] **Damage and lifecycle:** shared typed DamageResolver, explicit capped repair API, no regeneration; promotions/upgrades preserve missing HP. Zero HP stops operation, cancels work and uses existing removal cleanup for whole-footprint occupancy/navigation, selected Inspector and logistics. No refund/reward.
- [x] **Same DefenseBars system:** existing event-driven scene/style attached to fixed root, visible when damaged, footprint clearance, no new polling/renderer. Live compact Inspector HP, rank HP bonus and changed upgrade HP comparison. Inspector layout resources/dimensions preserved.
- [x] **Targeted verification:** full C# compile with Godot source generators; TowerHealthChecks (all nine types, HP safety/ranks/paid tiers, damage/repair, pause, weapon/passive destruction and whole-footprint cleanup), HeatInspectorChecks, CompactInspectorChecks, PlacementHudChecks (both maps), ConstructionChecks and BattleResearchChecks passed. Heat/MAX/busy Inspector geometry remains 560×330 at 1920×1080, 1366×768 and 960×540.
- [ ] **Broader verification / desktop:** desktop visuals remain unreviewed. Broader runners hit existing assumptions outside this hull change: EnemyIntegrationChecks stops at “Unavailable registration cannot be built” after the placement-flow changes; TowerStatChecks expects every neutral-stage snapshot identical despite Stage07 now materializing heat; standalone TowerRankChecks reaches a stale travelling-wave falloff expectation (2.291304 vs 2.125) after the range rebalance. No full-suite pass claimed; these older tests were not rewritten in this pass.
- [ ] **Remaining follow-ups:** E3B enemy tower attacks still need their own producer/targeting design. Tower shields, armor, repair producers, destruction visuals and campaign/battlefield saving remain planned. Historical HP-not-implemented notes below are superseded only for this hull foundation. See TOWERS/Shared/Durability/README.md and TESTS/Towers/TowerHealth.md.

---

# WORK — Consolidated Outstanding Implementation Backlog

## Big mk.1 visual enlargement — 2026-10-17

- [x] Enlarged `big_mk1` artwork from **96×72 to 112×120 px**. Kept the original heavy hull with uniform scale and added large upper/lower plated wings (3.5× Enemy001 width, 5× height). The medium Bulwark stays 64×48.
- [x] Updated exact sprite-size catalog assertions and feature/wave/feedback documentation. **No enemy HP/shield/speed/credits/abilities, scene transforms, wave authoring or navigation algorithm changed.** Big mk.1 keeps its existing 24px circular navigation radius.
- [ ] **Godot visual QA outstanding:** reimport the SVG, verify silhouette/bar clearance, rotations, narrow corridors/wing overlap and mass-wave look; run EnemyCatalogChecks. No Godot runtime test run in this GitHub-only art pass.

---

## Big mk.1 heavy enemy — 2026-10-17

- [x] Registered `ENEMIES/Types/Enemy005` / `big_mk1` in `DefaultEnemyCatalog.tres`, using one inherited plain enemy scene and 112×120 right-facing plated SVG. Original GameMaker-inspired balance: **1,000 hull, 200 optional shields, 80px/s, 5 Citadel damage, 10 Credits**, 24px navigation radius. No new AI or ability nodes.
- [x] F2/F3 catalog discoverability and wave syntax (`Enemy("big_mk1", shield: true)` for 200 starting SP). **Level001 and Level002 waves remain unchanged.**
- [x] Shared DefenseBar takes visual sprite height once on spawn so large ships' hull/shield bars clear their artwork; small ships keep authored minimum. Updated catalog/shield/credit regressions and relevant READMEs in this same commit.
- [ ] **Godot QA pending:** compile/run enemy catalog + GenericEnemy integration runners, F2 cycle, shielded Big mk.1 test wave, portal/corridor navigation for 24px clearance radius, oversized artwork overlap, shield aura/bar visual, rewards and 500-ship performance. No runtime tests performed in this GitHub-only change.

---


## Build preview / unobstructed placement UI — implemented 2026-10-17

- [x] Ctrl-open tray: hover or keyboard-focus a tower tile to show its description, cost, build time, footprint, basic effective stats and CAN AFFORD / INSUFFICIENT RESOURCES in the existing right Inspector. No ghost/payment just for browsing.
- [x] Affordable tile click arms one placement and temporarily hides BottomArea (tray + Inspector). Logical tray-open state stays intact; the permanent top status remains usable. Unaffordable clicks keep the preview/tray visible, create no tower and emit TowerPlacementController.BuildDenied(towerId, reason) for future audio; no audio playback was added.
- [x] RMB/Escape cancel the ghost and restore the tray with no tower selected; Ctrl cancels and actually closes the build tray. Reopening Ctrl does not resume canceled placement.
- [x] Successful placement no longer calls SelectTower(newTower). It ends placement, restores the tray, clears the Inspector and consumes/suppresses the placement frame's selection path. A separate later world click selects the tower normally. Restoring the tray under a stationary cursor does not immediately reopen a hover preview.
- [x] Preserved the latest Inspector resize: existing560×330 dimensions, fonts, right-column/heat structure, styles and layout resources. Compact build descriptions and wrapped affordability text fit the same panel. No InspectorPanel.tscn, UIMasterConfig.cs or DefaultUIConfig.tres changes in this pass.

Verification: TESTS/UI/PlacementHud.md. Current-control descriptions here supersede historical auto-selection/always-visible placement-preview notes below; original plans remain preserved.

---

## Laser Tower silver/yellow visual refinement — 2026-10-17

- [x] Replace Laser Tower SVG with silver/gunmetal body and yellow emitter/rails, retaining existing 64×64 scene sizing and muzzle marker.
- [x] Recolor only the Laser beam preset to ivory-yellow core / golden halo and reduce wave/end wander. Extend shared BeamRenderer's opt-in irregularity with smoothly sampled deterministic noise, preserving zero-irregularity sine fallback and inactive behavior.
- [x] Update Laser visual fixture assertions and the two beam READMEs; no gameplay/targeting/ammo/upgrade changes.
- [ ] **Godot verification pending:** run `TESTS/Towers/LaserTowerChecks` via its existing suite and inspect beam at normal zoom, short/long distances, pause, multiple beams and end wander. This pass was source-validated and its SVG preview-rendered, but no Godot/.NET headless tests could run in this environment.

---

## Enemy shield meter in shared DefenseBar — 2026-10-17

- [x] Added the **shield bar above the regular hull bar**, inside the existing `FEEDBACK/DefenseBars/DefenseBar` Node2D (no extra scene node per enemy). Full shields show both stacked rows, preserving normal hull-bar behavior when unshielded or after shield break. Uses warm red-orange shield tint, separate Inspector-configurable shield thickness/gap, existing width/border/offset, movement counter-rotation and `DefenseBarsEnabled`.
- [x] Shared Enemy shield-change bridge now notifies both `DefenseBar.SetShield` and the existing `ShieldVisual` aura for spawn/damage/explicit restoration; no shield regen, extra damage logic, spawn changes or polling.
- [x] Updated `TESTS/Combat/EnemyShieldChecks.cs` and feedback/enemy docs for full/partial/depleted/recovered shields.
- [ ] **Verify:** run Godot .NET shield fixture and visual test on Level001 shielded Enemy001; confirm shield row sits distinctly above hull, stays horizontal on turns, does not overlap other enemy indicators, and both disappear on removal. Runtime testing unavailable during this GitHub-only pass.

---


## Offensive tower range balance — 2026-10-17

- [x] Increased **all seven offensive tower types at every authored purchased level** approximately 15% (nearest whole px): Battle 294/304/313, Laser 294/304, Blaster 294/304, Shockwave 230/239, Cryo 239/248/258, Machine Gun 239/248/258, Sniper 515/524/534. Source of truth remains each `*Levels.cs` row; rank bonuses/Inspector/range rings/upgrade previews resolve normally. No modifier or change to weapon cooldown, damage, ammo, build costs or Blaster blast radius.
- [x] Updated exact-value regression assertions and per-type README balance tables in the **same commit**.
- [ ] **Godot validation outstanding:** rerun TowerStat, TowerUpgrade, Laser, Shockwave, Cryo, MultiBarrel and TowerRank checks. Review placement-range rings, upgrade comparison, Shockwave propagation and level playfeel. No Godot/.NET runtime available to run tests in this pass.

---


## Inspector visual trim / right-side bars — 2026-10-17

- [x] Narrowed `InspectorPanel.tscn` and the HUD default from **620×330** to nominal **560×330**; the Main stats/paid-upgrade area shrinks, while the six target-priority buttons retain their original **210px right-section layout**.
- [x] Moved the Heat text/bar under the right-side Combat Rank/XP bar. Left-side disabled reasons remain independent. Adjusted `GameplayHUD.cs` cached paths and Heat/Inspector/upgrade regression expectations; no placement/Ctrl/visibility/targeting/heat mechanics changes.
- [ ] **Verify in Godot after the latest commit:** run `TESTS/UI/CompactInspectorChecks.tscn`, `HeatInspectorChecks.tscn`, `InspectorTargetingChecks.tscn` and existing HUD/upgrade test suites. Visually review all nine families, the hot+disabled+Legendary/active-upgrade worst case, header/title and CURRENT/NEXT/GAIN alignment at multiple viewports; ensure no right-side wrap/clipping or unexpected height expansion. These checks have **not been executed for 560px**. Codex's separate placement-mode Inspector/HUD visibility pass must retain this new layout and node paths. See [Inspector current layout](../UI/GameplayHUD/Inspector/README.md).

---

## Battle research proof node — implemented 2026-10-17

- [x] **Battle Tower Weapons I:** battle_weapons_1, 1 RP, requires Research Core, axial (-1,0), +5% Battle Tower damage from the next mission. Saved ownership/RP use the existing progression v2 record; no new currency or XP scoring rules.
- [x] **Tower permanent research Stage04:** an immutable deployment snapshot is captured once in CitadelSession, shared with paid placement/build previews, and retained by each tower. Selected paid level → rank → research → safety clamps → final stats. Standard Battle damage30/33/36 becomes32/35/38 using existing nearest/ties-to-even integer rounding. All ranks/tiers, config rebuilds and paid previews retain the bonus without compounding.
- [x] **Reusable targeted damage effect:** catalog defines TowerDamagePercent + TargetTowerId; only Battle has an authored node. Other towers, Citadel health, ammo, heat, prices, authored rows, profileless F6 and other profiles are unchanged by this purchase. No progression I/O in combat/stat resolution.
- [x] **Graph extension:** real third node, balanced labels, and initial/reset view fits authored branches; pan/zoom selection remains shared geometry.

Verification and remaining manual review: TESTS/Research/BATTLE_RESEARCH.md. Mission XP rewards/replays/award tokens remain deferred. Earlier research-expansion examples below are preserved as history; Battle damage is now implemented rather than a future-only example.

---

## Shield aura art polish — 2026-10-17

- [x] Replaced the nearly invisible shield outline with a **single baked radial-fill SVG** (lightly translucent centre, thicker high-contrast bright rim, layered outer glow and static electric zap filaments). Tuned `FEEDBACK/Shields/DefaultShieldVisualStyle.tres` to 1.65× size, 0.82 idle opacity, 1.0 hit/break peaks, warm red-orange tint; `ShieldVisual.tscn` uses linear filtering. No change to shield gameplay, wave flags, regeneration, particles or per-frame CPU work.
- [ ] **Visual QA:** open Godot to re-import `FEEDBACK/Shields/Artwork/ShieldAura.svg`; check Shielded Enemy001 at normal zoom, small/large enemies, line-of-fire visibility, hit/break fade, layering and mass-wave performance. XML/reference checks passed; Godot headless/desktop tests were not run in this authoring session.

---

## Shield visual foundation (SV1) — 2026-10-17

- [x] Reusable **[FEEDBACK/Shields](../FEEDBACK/Shields/README.md)** presentation: one baked tintable reddish SVG aura, auto-sized behind the enemy, event-only shield damage/break flashes, lazy attachment only to shielded enemy instances, shield-restoration reactivation, no per-frame shield visual script processing. Purely cosmetic; existing shield arithmetic, shield generator and wave opt-in remain unchanged.
- [ ] **Godot desktop/headless SV1 verification** — run modified `TESTS/Combat/EnemyShieldChecks.cs`; inspect spawn/damage/break/restore, layering, red tint on different ship shapes, and 500-enemy performance. The code/resources are pushed but not compiled or visually tested here.
- [ ] **Later, separate decisions:** tower HP/shields/durability, natural shield regeneration, shield bars, premium sparks/shaders. None are implemented in SV1.

---

## Level001 combat/shield playtest balance — 2026-10-17

- [x] Standard Drone (`basic_mk1` / Enemy001) has **20 optional shield points** in its saved EnemyDefinition; zero-shield ordinary/F2 starts remain unchanged.
- [x] Level001's existing three waves remain manual and now contain **38 / 40 / 55** enemies with **7 / 12 / 18** shielded entries respectively. Wave 1 introduces shielded Standard Drones; waves 2–3 add Scouts; final wave adds a few Bulwarks. **No ShieldGenerator** is authored; no shield regeneration, mechanic refactor or new difficulty system was introduced. See [Level001Waves.cs](../LEVELS/Level001/Level001Waves.cs) and [Authoring.md](../LEVELS/Waves/Authoring.md).
- [ ] **Balance/playtest verification:** Confirm 20-point red aura is readable, portal navigation works for the few Level001 Bulwarks, and the three escalating waves are still manageable at 1,000 starting Credits. C# build/headless/desktop test runs are outstanding for this GitHub-only balance pass.

---

## Current amendment — Research before mission XP balancing (2026-10-17)

**This amendment supersedes older status/order statements below; all original plans are retained.** The user explicitly prioritized real mission statistics and the Campaign-only Research Room before finalized XP rewards/deductions.

- [x] **Research foundation / P4 first playable slice:** Campaign → RESEARCH, axial center-origin hex graph, pan/zoom, keyboard selection, prerequisite/effect/cost/state panel and atomic per-profile purchase. Research Core is free/virtual; Citadel Master I costs 1 RP and gives +10% maximum/current starting Citadel HP on both deployed levels. Results and gameplay have no Research link; purchase authority validates the active Research Room. See UI/Menus/Campaign/Research/README.md and SYSTEMS/Progression/Research/README.md.
- [x] **R3 actual telemetry slice:** event-driven paid placements, first construction completions, paid upgrades, unpaused mission clock; existing official kills/breaches/waves/integrity retained. Frozen results show these real counters. Successful placement and completed construction are separate metrics. Credits earned/spent remain untracked/null, not fabricated.
- [x] **Progression v2:** purchased stable research IDs and RP payment saved together; existing v1 XP/level/curve/RP migrate without reset or identity-index changes. XP grant remains a development helper, not a mission reward API.
- [ ] **Remaining P1/P2 / Results R4:** mission reward calculator, editable scoring/ETA, first-clear/replay history, once-only run-token awards and Results XP animation/itemization are intentionally deferred until scoring is chosen. Do not add provisional reward tuning merely to use Research; the default-off DEBUG XP grant supplies test RP.
- [ ] **Remaining P3:** separate LEVELS area/hub reorganization; current two deployment actions remain and Research is added alongside them.
- [ ] **Remaining R2/R3/P5:** desktop visual review, Credits/optional additional statistics, expanded/full regression and optional presentation polish. No tower damage research was added: future tower modifiers use 04_PermanentResearch; no fake catalog nodes.

Focused tests and scope/limits: TESTS/Research/README.md. Do not mark full P1–P5 or all Results R3/R4 complete. Follow the revised sequence in the progression brief rather than repeating the already delivered foundation.

---

**Audit date:** 2026-10-16 (latest audited pushed commit UTC 2026-10-17). Checked against GitHub `main` at `967435719798` (Add Battle Tower construction SVG stages and shared optional level artwork). **This is a documentation/code-presence audit, not a new Godot test run.** Status is a snapshot: check current HEAD and relevant code/tests before Codex acts, particularly if another Codex task was pushed meanwhile.

**Purpose:** One entrypoint for *outstanding implementation passes scattered across project notes*. Links point to the authoritative full specification; this index intentionally does **not** replace those notes. [Code preferences](CodePreferences.md). Keep completed historical work below this index.

### Status key

- **[ ] OPEN — detailed pass exists:** ready for Codex in numbered steps; complete/test before marking done.
- **[ ] OPEN — design only / needs pass:** future system noted in README, but no approved numbered implementation spec yet.
- **[ ] CHECK / POLISH:** feature implemented, but a verification, visual review or maintenance job is outstanding.
- **[x] COMPLETE:** listed to stop Codex from repeating the work.
- A long-term idea mentioned in a README is **not** automatically approved for a giant implementation. Split large systems into narrow passes first.


## Initial progression/menu foundation — 2026-10-17

The user selected a smaller first slice of P1/P3: separate per-profile persistent XP/levels/RP, editable starting curve, multi-level carry and +1 RP per promotion, shared Main/Campaign display, and an off-by-default DEBUG grant option. See [Progression README](../SYSTEMS/Progression/README.md). Mission XP scoring, replay/first-clear history, once-only mission awards, Results XP, the Levels/Research hub split and Research Room remain **OPEN**. Do not mark all P1 or P3 complete. Verification: TESTS/Progression/README.md.

## A. Open implementation passes (Codex-ready or scoped in existing notes)

| Status | Pass / work | Authoritative notes and next action | Dependencies / caveats |
| --- | --- | --- | --- |
| [x] | **IT1–IT4 — compact Inspector + six targeting priorities** | [Implementation/checklist](../UI/GameplayHUD/Inspector/INSPECTOR_TARGETING_PASS.md): non-scrollable700×330 two-column Inspector, independent earned rank and paid upgrades, six per-instance choices and cached checkpoint-aware First/Last. | Automated compilation/headless results: [verification](../TESTS/UI/InspectorTargeting.md). Desktop visual review remains pending; see manual verification backlog. |
| [x] | **CT1–CT3 — Cryo Tower + reusable enemy status-effect foundation** | [New 2026-10-16 Codex plan](../TOWERS/Offensive/CryoTower/CRYO_TOWER_IMPLEMENTATION.md): energy-projectile Cryo, permanent nonstacking 33% slow, scalable optional status infrastructure and tests. | **IMPLEMENTED**, with focused headless tests. Cryo only targets unslowed/nonimmune enemies. See [verification](../TESTS/Towers/CryoTower.md); manual playfeel remains outstanding. Stasis remains deferred. |
| [ ] | **P1–P5 — Player/global XP + Research Foundation** | [Detailed phased Codex pass](../SYSTEMS/Progression/PLAYER_XP_RESEARCH_FOUNDATION_PASS.md): persistent per-profile XP/RP, signed mission scoring + ETA, Campaign Levels/Research, hex-origin graph and real Citadel Master I. | Extends Results R3/R4; **do not implement XP twice** or confuse player XP with tower Rank XP. Campaign save/load remains separate. |

| [ ] | **R2 — Mission Results visual polish** | [Results remaining passes](../UI/Menus/Results/README.md): layout, transitions, skippable count-up, sounds and viewport testing. | R0/R1 outcome/results behavior already complete. Optional independent polish pass. |
| [ ] | **R3 — Actual level-run telemetry / extra stats** | [Results R3](../UI/Menus/Results/README.md): placed towers, successful upgrades, Credits earned/spent, unpaused timer, frozen final telemetry. | **P2 already covers the subset needed for XP/time**; coordinate instead of duplicating stat owners. Extra optional breakdown may be later. |
| [ ] | **R4 — Mission XP scoring/persistence** | [Results R4](../UI/Menus/Results/README.md), implemented through [P1–P2](../SYSTEMS/Progression/PLAYER_XP_RESEARCH_FOUNDATION_PASS.md) in the approved plan. | **Overlapping scope:** after P1/P2, reconcile R4 checklist; don't create a separate XP wallet/service. |
| [ ] | **E3B — Enemies attack towers** | [ENEMIES/README E3B](../ENEMIES/README.md): ranged enemy attacks and electric area pulses against damageable towers. | **HP prerequisite implemented 2026-10-17.** Enemy attack targeting/delivery still needs a separate scoped pass; no automatic tower attacks enabled. |
| [ ] | **Waves Phase 2 — remaining options/content** | [Waves README](../LEVELS/Waves/README.md): more mixed authored wave content, other portal strategies and eventual elite/stealth/speed/boss modifiers. | **Partially complete:** basic mixed catalog, required portals, starting shields and ShieldEmitter already work. This is a collection of optional *follow-up features*, not a scheduler rewrite. |
| [ ] | **Waves Phase 3 — advanced progression** | [Waves README](../LEVELS/Waves/README.md): wave-completion bonuses, countdown/auto-next, announcements, scripted events, preview/history and eventual wave saving. | **Design only; subdivide by feature.** Current finite waves and R1 mission victory already work. |

## B. Outstanding systems documented elsewhere — not yet numbered/approved implementation passes

These are real gaps or explicitly proposed systems, but **need a dedicated scope/decisions before Codex should implement them**.

| Status | System / next pass to design | Existing notes and key boundary |
| --- | --- | --- |
| [x] | **HD1–HD3 — generic tower disabling, heat and compact Inspector** | Implemented [shared API/authoring](../TOWERS/Shared/Conditions/README.md): independent timed/conditional handles, source refresh/cleanup, all offensive attack gates, optional selected-level heat, cooling/latch and tests. Battle is enabled via its Godot-Inspector minute profile (3min continuous fire,9s recovery); other tower opt-ins remain undecided. **HD3 Inspector heat/status presentation is implemented.** [Verification](../TESTS/Towers/TowerConditions.md). |
| [ ] | **Tower HP, shields, damage, destruction and repairs** | [TOWERS roadmap](../TOWERS/README.md), [E3B dependency](../ENEMIES/README.md), [Repair Tower](../TOWERS/Support/RepairTower/README.md). Hull HP/damage/destruction/footprint/Inspector foundation implemented 2026-10-17; shields, repair producers and further durability balance remain planned. |
| [ ] | **Power generation, island-grid electricity, shortages, priorities & relays** | [Power network](../TOWERS/Economy/Power/README.md), [generators](../TOWERS/Economy/Power/Shared/README.md), [consumer capability](../TOWERS/Shared/Power/README.md), [Power Relay](../TOWERS/Utility/PowerRelay/README.md). Solar/Industrial/Fusion share generation code; no cables; terrain islands connect by paired relays. Large system: break into several passes. |
| [ ] | **Actual campaign/mission Save–Load, slots and unlocked level progression** | [SYSTEMS/Saving](../SYSTEMS/Saving/README.md), [Campaign menu](../UI/Menus/Campaign/README.md), [Pause menu](../UI/Menus/Pause/README.md). Profile *identity* is saved, but no campaign/session saves yet. **P1–P5 progression record does not implement campaign saves.** |
| [ ] | **Reusable blank playable level template / level-loading convenience** | [LEVELS](../LEVELS/README.md), [Session](../LEVELS/Session/README.md). `LEVELS/LevelTemplate.tscn` exists with blank terrain/Citadel/Portals container, but the requested *placed active portal* and streamlined new playable-level registration/launcher flow are not done. Create a focused pass if still desired. |
| [ ] | **Tower magazines/reloading, accuracy, maintenance and selling** | [Tower roadmap](../TOWERS/README.md), [Inspector](../UI/GameplayHUD/Inspector/README.md), [Shared ammunition](../TOWERS/Shared/Ammunition/README.md). Implement separately; reload must use real stock, not create ammunition. |
| [ ] | **Additional registered towers and specialized mechanics** | [Offensive](../TOWERS/Offensive/README.md) (Missile, LOS; heat/disable tracked separately above), [Support](../TOWERS/Support/README.md) (Repair/Radar), [Economy](../TOWERS/Economy/README.md) (Resource Tower/Scrap Refinery/older SolarTower). **Sniper + Machine Gun are already implemented** and must not be re-created. Cryo now has its own **CT1–CT3 completed** pass in section A. |
| [ ] | **Further damage/enemy status mechanics** | [ENEMIES future abilities](../ENEMIES/README.md), [COMBAT](../COMBAT/README.md), [Waves modifiers](../LEVELS/Waves/README.md): armor mitigation, stealth/detection, EMP, regeneration, buffs/status durations, additional boss/attack types. The **first concrete slow/status foundation is implemented in** [CT1–CT3](../TOWERS/Offensive/CryoTower/CRYO_TOWER_IMPLEMENTATION.md); coordinate future statuses with it rather than create duplicates. |
| [ ] | **Live gameplay wiring for shared Alerts/Toasts** | [UI/Alerts foundation](../UI/Alerts/README.md) is **implemented and mounted** in the shared GameplayHUD, with configurable banners, priority/queues, deduplication, and DEBUG F7 preview. **Not implemented:** real producers from Citadel, waves, tower construction/low ammo, research or combat; wire narrow event-driven sources later, without per-frame popup spam. Sounds/voice/history/save persistence are separate future work. |
| [ ] | **Audio game content/event wiring & settings** | [AUDIO](../AUDIO/README.md): foundation exists; actual clips, sound event connections, menu music/ambience, voice lines and optional player preferences remain future stages. |
| [ ] | **Visual combat feedback polish** | [Damage Numbers](../FEEDBACK/DamageNumbers/README.md), [Camera Effects](../FEEDBACK/CameraEffects/README.md): unimplemented optional effects; [DefenseBars](../FEEDBACK/DefenseBars/README.md) and death/shockwave visuals already exist. |
| [ ] | **Player combat drone ability** | [DRONES/Combat](../DRONES/Combat/README.md): entirely prospective attack drone; **do not confuse with completed transport drone logistics**. |
| [ ] | **Research expansion beyond first proof node** | [Player research foundation](../SYSTEMS/Progression/PLAYER_XP_RESEARCH_FOUNDATION_PASS.md): the giant research tree, DME/Zy0n story gates, unlockable towers, doctrine loadout slots, production buffs and refunds/respec are **explicitly deferred beyond P1–P5**. |
| [ ] | **Further economy/ammo logistics options** | [Economy](../TOWERS/Economy/README.md), [Ammo loop](../TOWERS/Economy/Ammunition/README.md): raw materials, differing ammo types, research-controlled throughput, additional resource loops—not missing portions of the implemented first two-leg pipeline. |
| [ ] | **Optional advanced world art** | [Backgrounds](../WORLD/BACKGROUNDS/README.md): parallax, shaders, planet/nebula animations and unique level art; cosmetic backlog, not a mechanics prerequisite. |
| [ ] | **Options menu / persisted personal settings** | [Pause](../UI/Menus/Pause/README.md), [AUDIO](../AUDIO/README.md). Existing Pause Options is a disabled placeholder; needs its own specification. |

## C. Verification, profiling and note cleanup — not missing gameplay passes

| Status | Task | Evidence / do not redo |
| --- | --- | --- |
| [ ] | **N4 corridor movement desktop visual/performance review** | [N4 implementation record](../ENEMIES/enemypathing.md), [CPU timings](../TESTS/Performance/CorridorPerformance.md), [follow-up profiling suggestions](../ENEMIES/ENEMYPERFORMANCE.md). N4 is **implemented**, only visual 0/50/100% comparison and rendered 500-enemy checks remain. |
| [ ] | **Profiling only if measured bottlenecks warrant** | [ENEMYPERFORMANCE](../ENEMIES/ENEMYPERFORMANCE.md), [findings](AGENT_FINDINGS/Performance.md): repath-burst latency, potential duplicate spatial-index position update, clearance cost; preserve safety and measure before optimizing. |
| [ ] | **Godot/.NET standard build and full-suite blockers** | [TESTS/README](../TESTS/README.md), [Ammunition evidence](../TESTS/Economy/Ammunition.md), [Footprint evidence](../TESTS/Towers/Footprints.md). Previous environment lacked expected .NET/MSBuild/NuGet dependencies; full integration also hit preexisting Level002 painted-terrain assertion. **Recheck current environment/map before claiming unresolved or fixed.** |
| [ ] | **Beam ripple/GPU effects test and visual QA** | [Beam renderer](../TOWERS/Shared/Weapons/Beams/README.md), [Laser tests](../TESTS/Towers/LaserTowerChecks.cs): build and run modified LaserTowerChecks (muzzle anchor, bounded impact drift, allocation/node stability, GPU emitter toggles), then desktop check short/long beams, moving enemies, multiple beams, pause, EMP, target loss and low ammo. New visuals are pushed but **not yet Godot/.NET-tested** in this session. |
| [ ] | **Alerts F7 visual/pause verification** | [Alerts README](../UI/Alerts/README.md) has a debug preview and manual checklist: topbar clearance, priority interruption, toast merging/queue bounds, pause behavior, scene transitions and smaller viewport. The GitHub-only port is **not yet Godot C# built or visually playtested**. |
| [ ] | **Manual render/playtest verification** | Battle construction **stage SVGs are implemented**; check in-game stage positions/transition vs the provided rendered contact sheet ([test checklist](../TESTS/Towers/ConstructionArtwork.md)). Check existing DefenseBar, 500-enemy motion, full HUD and ammo drone visuals. **Construction sparks/VFX were explicitly excluded from this C4 scope**. Headless validation is not a substitute for the Godot editor. |
| [ ] | **Stale status text / factual note cleanup** | [ENEMIES/README](../ENEMIES/README.md) still has an old N4 “planned” section; [enemypathing.md](../ENEMIES/enemypathing.md) contains a contradictory top “NOT IMPLEMENTED” line; [tower_ideas.md](../TOWERS/tower_ideas.md) still describes ranks/multi-cell/Laser as absent; [WORLD/README](../WORLD/README.md) has historical “Citadel health future” wording; [TOWERS/README](../TOWERS/README.md) has an obsolete “multi-cell placement not implemented” list item. Update obsolete lines against actual code, **do not implement those features again**. |
| [ ] | **Older ammo verification/update notes** | [Previous WORK detail below](#archived-completion-record--ammunition-pipeline), [Ammo tests](../TESTS/Economy/Ammunition.md): some original 1×1 wording and a pre-later-balance verification status are historical. Verify/refresh after current-footprint and shipment defaults, not new inventory architecture. |

## D. Already delivered — not outstanding implementation passes

- [x] **GE1–GE3 inherited enemy scenes:** one [GenericEnemy.tscn](../ENEMIES/Shared/GenericEnemy.tscn) supplies Sprite, Movement and DefenseBar; five type scenes inherit and retain their artwork/abilities. [Implementation and tests](../ENEMIES/Shared/GENERIC_ENEMY_SCENE_PASS.md).
- [x] **Enemy E2A/E2B/E3A:** mixed catalog, ShieldEmitter, Transporter + six-child staggered DeathSpawn and wave ownership. [ENEMIES](../ENEMIES/README.md) / [DeathSpawn tests](../TESTS/Enemies/DeathSpawn.md).
- [x] **Navigation N4, F3 route preview and checkpoint chains:** core implementation done; visual review separately listed above. [Movement](../ENEMIES/Movement/README.md), [Route preview](../ENEMIES/Movement/pathoverlay_tweaks.md).
- [x] **Multi-cell tower footprints F1–F5:** Factory **3×2**, Depot **2×2**, offense **1×1**, occupancy/path/performance tests. [Pass](../TOWERS/Placement/MULTI_CELL_FOOTPRINT_PASS.md).
- [x] **Tower core combat/stat/paid-level/rank foundation, Laser/Blaster/Shockwave, typed damage and enemy spatial queries.** [TOWERS](../TOWERS/README.md), [COMBAT](../COMBAT/README.md).
- [x] **Reusable beam visual polish (2026-10-17):** [Shared BeamRenderer](../TOWERS/Shared/Weapons/Beams/README.md) has adaptive animated ripple, endpoint drift, drawn impact glow and independently optional scene-owned `GpuParticles2D` impact/muzzle emitters. [Laser style](../TOWERS/Offensive/LaserTower/Beam/README.md) enables subtle ripple/contact sparks, reusing [FEEDBACK/Shared](../FEEDBACK/Shared/README.md) particle assets. **Cosmetic only; GPU appearance and changed Laser test assertions not run in Godot here.**
- [x] **Sniper Tower + twin-barrel Machine Gun Tower:** current playable scenes/configs/levels/ranks and shared projectile firing mode. [Sniper](../TOWERS/Offensive/SniperTower/README.md) / [Machine Gun](../TOWERS/Offensive/MachineGunTower/README.md). Note: a subsequent balance commit made Battle faster and tuned Machine Gun speed—don't replace these values with old samples.
- [x] **C1–C3 timed construction/upgrades plus C4 Battle-only SVG artwork pilot:** operational gating, progress and paid upgrade timers are live; optional artwork presenter, Battle stage SVGs and per-level artwork fallback are in code. [Pass](../TOWERS/Shared/Construction/TIMED_CONSTRUCTION_PASS.md) / [pilot verification](../TESTS/Towers/ConstructionArtwork.md). **Remaining only optional factory/other type-specific stage content and desktop visual QA; sparks/VFX not requested.**
- [x] **Ammunition production, both physical drone delivery legs and offensive consumption.** See archived record below / [Ammo notes](../TOWERS/Economy/Ammunition/README.md).
- [x] **Shared UI Alerts foundation:** old isometric 2D major-alert and toast system ported into [UI/Alerts](../UI/Alerts/README.md), installed once through GameplayHUD with DEBUG F7 demonstration; **gameplay event wiring and actual Godot verification are still pending**. No tower/combat/wave mechanics changed.
- [x] **Results R0/R1:** real mission victory/defeat, snapshot, menu navigation; **not** R2–R4. [Results](../UI/Menus/Results/README.md).
- [x] **Profile identity Boot/Main/Campaign foundations, Pause basics**; not gameplay saves, XP or Research. [Profiles](../SYSTEMS/Profiles/README.md).
- [x] **Build HUD Offensive/Economy categories and compact Inspector** on the latest audited HEAD. [HUD check](../TESTS/UI/HudCategories.md).

## E. Suggested sequencing when returning

1. **Inspector visual review:** IT1–IT4 source and automated tests are complete. Manually assess the compact non-scrollable Inspector in-game; do not recreate six priorities or add a second journey estimator.
2. **Cryo Tower CT1–CT3** — implemented: reusable status-effect architecture and permanent nonstacking slow; **do not implement Stasis now**. Coordinate targeting-related edits in `TowerTargeting.cs` with IT1–IT4 so concurrent Codex work doesn't overwrite changes.
3. **Campaign progression P1–P5** — profile XP, mission awards, Research Room and first real research; coordinate Results R3/R4 telemetry and don't conflate this with still-missing campaign saves.
4. **HD1–HD3 complete; thermal balance for other towers remains open** — shared multi-source disabling and optional heat are implemented and tested. Battle now has a3min minute-based profile; other per-tower heat balance/opt-ins remain undecided. Compact event-driven Inspector heat/status presentation is implemented. This remains independent of tower HP/E3B.
5. **Tower durability + enemy E3B** — first implement tower health/shields/destruction safely, then enable enemy attacks. Only proceed after dependency passes are scoped and tested.
6. **Large management systems** — campaign Save/Load, followed by modular electricity/island power grids/relays, when ready.
7. **Optional/future** — Results R2 polish, more stage artwork (Battle pilot already done), advanced waves, additional towers, sounds, feedback, combat drones and graphical/performance checks. Don't redo complete GE1–GE3 or C1–C4 pilot.

**Codex handoff:** “Read `NOTES/WORK.md`, `NOTES/CodePreferences.md`, and the linked authoritative pass file for the single item selected. Verify HEAD and implemented code first. Implement and test only that scope; update WORK and the source pass checklists after verified completion.”

---

## Archived completion record — Ammunition pipeline (historical)

**Status: IMPLEMENTED AND HEADLESS-VERIFIED (2026-10-16), with known standard-build/full-suite environment/map limitations below.** The original foundation helpers were reused, not replaced: AmmoInventory, AmmoProduction, AmmoStorage and AmmunitionMasterConfig.

### Completed gameplay

- [x] Optional AmmunitionStats in each offensive tower's complete independent C# level rows, through the existing cached eight-stage pipeline. Current state is separate.
- [x] Per-instance reserves; configurable InitialAmmoFraction (full by default); per-launch Battle/Blaster charges, per-pulse Shockwave charge and prepaid time-based Laser use.
- [x] Empty weapons stop unpaid attacks and resume after physical resupply. Damage types, shield penetration, AoE/wavefront delivery and source attribution are preserved.
- [x] Stock persists through rank promotions and purchased levels; capacity growth never refills, shrink explicitly discards excess and records it.
- [x] Buildable/selectable Factory and Depot, Economy catalog entries, vector placeholders, 1×1 navigation-safe placement and existing wallet/payment failure behavior.
- [x] Automatic factory whole-batch production, finite buffer, full/no-depot pause and departure-driven resume using AmmoProduction.Advance.
- [x] Separate depot AmmoStorage inventory; more than one producer/store, deterministic nearest eligible selection.
- [x] Visible factory-owned Factory→Depot drones **and** depot-owned Depot→Tower drones, carrying finite real cargo and returning home. No instant shipments.
- [x] Incoming space reservations, finite home slots, single outstanding consumer shipment, scarce-stock retries, partial/surplus delivery and interruption cleanup.
- [x] Selected-tower/building runtime Inspector status and authoritative next-level ammo previews, using coalesced events rather than HUD polling/pipeline work per shot.
- [x] Existing pure helper tests executed first; focused weapon, delivery safety, conservation, purchase and real-physics automatic-loop tests added under TESTS/Economy.
- [x] Relevant navigation/menu/placement regressions and remaining diagnostic integration checks executed; actual outcomes documented.
- [ ] Clean standard SDK/editor build and unmodified full integration pass: blocked by existing container SDK/MSBuild/NuGet dependencies and Level002 tile-count assertion respectively. These are not claimed passing.
- [ ] Manual desktop graphical playtest: headless scenes/motion/transactions are verified, actual desktop rendering has not been manually inspected.

### Editable provisional defaults

| Item | Values |
| --- | --- |
| Battle, all current levels | 1000 reserve; 1 per successful projectile launch |
| Laser, all current levels | 1000 reserve; 2 rounds per firing second, retaining prepaid fractions |
| Blaster, all current levels | 600 reserve; 3 per projectile launch |
| Shockwave, all current levels | 400 reserve; 5 per emitted pulse |
| Global threshold | Existing DefaultAmmunitionConfig.tres:10% |
| Factory | 200 Credits;100 ammo/2s;**1000 buffer; 1000 ShipmentAmount (full load to dispatch);**1 drone;1000 payload;220px/s |
| Depot | 150 Credits;**10000 storage**;2 drones;250 payload;220px/s |
| Dispatcher | 0.2s scheduling interval;8 alternating endpoint checks |

Battle1000/1 and the10% rule follow the requested example. Other defaults are provisional tuning, not permanent balance. Production/storage settings initialize when placed; transport settings affect subsequent dispatch. Offensive level values remain in each *Levels.cs file only. Building noncombat settings live in their Inspector config resources.

### Factory dispatch tuning (2026-10-16)

**New defaults implemented in code, regression tests updated; current adjustment has not been re-run against Godot yet.** The factory produces 100 rounds every two seconds but waits until its configurable `ShipmentAmount` reaches **1,000** before launching. Every flight is an exact 1,000-round load from its finite 1,000-round buffer. If no depot can reserve 1,000 free slots, the factory holds the stock and pauses, rather than sending an early/partial drone. Depot storage is now 10,000. Depot-to-tower resupply remains as before (depot drone payload 250; global 10% demand). Edit `AmmunitionFactoryConfig.cs` / Godot Factory config and `AmmunitionDepotConfig.cs` / Depot config; no hardcoded scheduler threshold is necessary. See [tests](../TESTS/Economy/Ammunition.md).

### Ownership and safety

TowerAmmunition wraps the existing stock helper; factories/depot nodes wrap the existing production/storage helpers. TOWERS/Economy/Ammunition/Shared/Logistics owns requests, stock and incoming-capacity reservations. DRONES/Transport owns reusable direct flight/rendering/callbacks, not damage or ammo minting. One logistics node lives in each World, not an Autoload. No enemy queries or per-frame building cross-product searches occur.

Load source→cargo at departure; unload cargo→destination on arrival. Home slots remain occupied until physical return. Missing destination/capacity reduction returns surplus. Missing source/drone loses remaining cargo once. A return to an already-full home returns what fits and explicitly counts remaining loss. Removed endpoint stock also counts as loss. World unload clears jobs and subscriptions. Conservation includes initial stock, factory production, all local stock/cargo, paid consumption and explicit loss/discard.

### Verification and testing

Run `res://TESTS/Economy/AmmunitionLoopChecks.tscn` after building. It first runs the original AmmoFoundationChecks, then small focused WeaponAmmunitionChecks and DeliverySafetyChecks using AmmoLoopFixture. It buys Factory/Depot/Battle through actual buttons/payment, fires900 Battle projectiles to the threshold, verifies both real drone legs and arrival-only stock changes, and verifies automatic real-physics callbacks/pause with no manual drone ticking. TESTS/Economy/Ammunition.md records exact results and outstanding environment/map limitations.

For a quick desktop test, place Factory and Depot near a Battle Tower. Default starting stock is1000; reduce that config's InitialAmmoFraction before placement if waiting for900 shots is inconvenient. Select each building/tower to inspect stock, slots and demand; amber cargo indicates loaded flight. No debug/manual refill is needed for production deliveries.

### Still deferred

No permanent research, saving, magazines/reload timers, power grids/consumption, raw material inputs, construction timers, per-tower threshold sliders, differentiated ammo types, selling, tower health or combat drones were implemented. Future work should build on this working loop rather than recreate inventory, production, storage, threshold or reservations.


## Cryo CT1–CT3

Implemented shared lazy permanent-Slow status storage/Inspector immunity, immutable projectile payload, Cryo Energy weapon/catalog/artwork, explicit levels/ranks and effective Slow stat previews. Targeting excludes any already-slowed or immune enemy, without damage-only fallback. Reuses construction, physical ammo and spatial queries. Stasis/DoT/timed scheduler/research/saving remain deferred. See TOWERS/Offensive/CryoTower/README.md and TESTS/Towers/CryoTower.md for verification.

## PV1/PV2 implemented — 2026-10-17

Projectile tracers, real-muzzle bounded flashes and tower-owned Battle/Machine Gun/Sniper/Cryo presets are implemented. Sniper additionally has optional subtle red barrel-forward aiming. No HUD, gameplay stats, targeting or ammo systems were redesigned. [Authoring](../TOWERS/Shared/Weapons/Visuals/README.md), [actual verification and remaining desktop review](../TESTS/Towers/ProjectileVisuals.md).

## PV3 follow-up / Inspector width — 2026-10-17

PV3 automated/resources/documentation work was already included in the tracer/muzzle commit. Follow-up retains the desktop visual-review task and reruns the focused suite. Inspector default is now620×330, with content-sized CURRENT/NEXT/GAIN columns and thin drawn vertical separators; no scrolling, smaller fonts or gameplay changes. Focused all-building/viewport geometry checks pass (620px wide, max331px high). See TESTS/UI/InspectorTargeting.md.

## Barrel origins / flash visibility follow-up — 2026-10-17

Blaster migrated from center launch to its actual barrel-tip marker with a shared tower-owned orange flash preset. Battle/Machine Gun/Sniper/Cryo already use real muzzle markers; Laser uses its beam muzzle and Shockwave intentionally remains centered. Shared flashes now have first-process presentation protection, a bright hold/linear fade, larger/longer presets and draw above projectiles. Preserves8c7ab2e's20% Battle/Machine Gun rate balance. See TESTS/Towers/ProjectileVisuals.md for regression evidence and pending desktop appearance review.


