# WORK — Consolidated Outstanding Implementation Backlog

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
| [ ] | **E3B — Enemies attack towers** | [ENEMIES/README E3B](../ENEMIES/README.md): ranged enemy attacks and electric area pulses against damageable towers. | **BLOCKED until tower HP/durability exists.** Tower health must be separately designed/tested first. |
| [ ] | **Waves Phase 2 — remaining options/content** | [Waves README](../LEVELS/Waves/README.md): more mixed authored wave content, other portal strategies and eventual elite/stealth/speed/boss modifiers. | **Partially complete:** basic mixed catalog, required portals, starting shields and ShieldEmitter already work. This is a collection of optional *follow-up features*, not a scheduler rewrite. |
| [ ] | **Waves Phase 3 — advanced progression** | [Waves README](../LEVELS/Waves/README.md): wave-completion bonuses, countdown/auto-next, announcements, scripted events, preview/history and eventual wave saving. | **Design only; subdivide by feature.** Current finite waves and R1 mission victory already work. |

## B. Outstanding systems documented elsewhere — not yet numbered/approved implementation passes

These are real gaps or explicitly proposed systems, but **need a dedicated scope/decisions before Codex should implement them**.

| Status | System / next pass to design | Existing notes and key boundary |
| --- | --- | --- |
| [x] | **HD1–HD2 — generic tower disabling + optional heat** | Implemented [shared API/authoring](../TOWERS/Shared/Conditions/README.md): independent timed/conditional handles, source refresh/cleanup, all offensive attack gates, optional selected-level heat, cooling/latch and tests. Battle is enabled via its Godot-Inspector minute profile (3min continuous fire,9s recovery); other tower opt-ins remain undecided. **HD3 Inspector heat/status presentation remains OPEN.** [Verification](../TESTS/Towers/TowerConditions.md). |
| [ ] | **Tower HP, shields, damage, destruction and repairs** | [TOWERS roadmap](../TOWERS/README.md), [E3B dependency](../ENEMIES/README.md), [Repair Tower](../TOWERS/Support/RepairTower/README.md). Decide health/shield stats and destruction/footprint/Inspector semantics **before E3B**. |
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
4. **HD1/HD2 complete; HD3 heat/disable presentation OPEN** — shared multi-source disabling and optional heat are implemented and tested. Battle now has a3min minute-based profile; other per-tower heat balance/opt-ins remain undecided. Compact Inspector presentation is a separate remaining pass. This remains independent of tower HP/E3B.
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
