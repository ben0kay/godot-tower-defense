# WORK — Consolidated Outstanding Implementation Backlog

**Audit date:** 2026-10-16. Checked against GitHub `main` at `6363d773fc02` (feat(towers): add Sniper and twin-barrel Machine Gun with reusable firing modes). **This is a documentation/code-presence audit, not a new Godot test run.** Status is a snapshot: check current HEAD and relevant code/tests before Codex acts, particularly if another Codex task was pushed meanwhile.

**Purpose:** One entrypoint for *outstanding implementation passes scattered across project notes*. Links point to the authoritative full specification; this index intentionally does **not** replace those notes. [Code preferences](CodePreferences.md). Keep completed historical work below this index.

### Status key

- **[ ] OPEN — detailed pass exists:** ready for Codex in numbered steps; complete/test before marking done.
- **[ ] OPEN — design only / needs pass:** future system noted in README, but no approved numbered implementation spec yet.
- **[ ] CHECK / POLISH:** feature implemented, but a verification, visual review or maintenance job is outstanding.
- **[x] COMPLETE:** listed to stop Codex from repeating the work.
- A long-term idea mentioned in a README is **not** automatically approved for a giant implementation. Split large systems into narrow passes first.

## A. Open implementation passes (Codex-ready or scoped in existing notes)

| Status | Pass / work | Authoritative notes and next action | Dependencies / caveats |
| --- | --- | --- | --- |
| [x] | **GE1–GE3 — Generic inherited enemy scene** | [Completed pass](../ENEMIES/Shared/GENERIC_ENEMY_SCENE_PASS.md): shared parent and five inherited scenes; propagation/gameplay tests and benchmarks. | Preserve catalog scene paths, ShieldEmitter and Transporter DeathSpawn; separate editable enemy scenes stay. |
| [ ] | **P1–P5 — Player/global XP + Research Foundation** | [Detailed phased Codex pass](../SYSTEMS/Progression/PLAYER_XP_RESEARCH_FOUNDATION_PASS.md): persistent per-profile XP/RP, signed mission scoring + ETA, Campaign Levels/Research, hex-origin graph and real Citadel Master I. | Extends Results R3/R4; **do not implement XP twice** or confuse player XP with tower Rank XP. Campaign save/load remains separate. |
| [ ] | **C1–C4 — Timed construction & upgrades** | [Detailed phased Codex pass](../TOWERS/Shared/Construction/TIMED_CONSTRUCTION_PASS.md): work states/durations, capability gating, timed upgrades/Inspector, stage SVGs + optional sparks/VFX. | Preserve 1×1/2×2/3×2 footprints, wallet, ammo delivery, existing levels and rank XP. |
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
| [ ] | **Tower HP, shields, damage, destruction and repairs** | [TOWERS roadmap](../TOWERS/README.md), [E3B dependency](../ENEMIES/README.md), [Repair Tower](../TOWERS/Support/RepairTower/README.md). Decide health/shield stats and destruction/footprint/Inspector semantics **before E3B**. |
| [ ] | **Power generation, island-grid electricity, shortages, priorities & relays** | [Power network](../TOWERS/Economy/Power/README.md), [generators](../TOWERS/Economy/Power/Shared/README.md), [consumer capability](../TOWERS/Shared/Power/README.md), [Power Relay](../TOWERS/Utility/PowerRelay/README.md). Solar/Industrial/Fusion share generation code; no cables; terrain islands connect by paired relays. Large system: break into several passes. |
| [ ] | **Actual campaign/mission Save–Load, slots and unlocked level progression** | [SYSTEMS/Saving](../SYSTEMS/Saving/README.md), [Campaign menu](../UI/Menus/Campaign/README.md), [Pause menu](../UI/Menus/Pause/README.md). Profile *identity* is saved, but no campaign/session saves yet. **P1–P5 progression record does not implement campaign saves.** |
| [ ] | **Reusable blank playable level template / level-loading convenience** | [LEVELS](../LEVELS/README.md), [Session](../LEVELS/Session/README.md). `LEVELS/LevelTemplate.tscn` exists with blank terrain/Citadel/Portals container, but the requested *placed active portal* and streamlined new playable-level registration/launcher flow are not done. Create a focused pass if still desired. |
| [ ] | **Tower magazines/reloading, accuracy, maintenance and selling** | [Tower roadmap](../TOWERS/README.md), [Inspector](../UI/GameplayHUD/Inspector/README.md), [Shared ammunition](../TOWERS/Shared/Ammunition/README.md). Implement separately; reload must use real stock, not create ammunition. |
| [ ] | **Additional registered towers and specialized mechanics** | [Offensive](../TOWERS/Offensive/README.md) (Missile, LOS, heat), [Support](../TOWERS/Support/README.md) (Repair/Radar), [Economy](../TOWERS/Economy/README.md) (Resource Tower/Scrap Refinery/older SolarTower). Individual folder READMEs are mostly concepts, not ready-made pass specs. |
| [ ] | **Further damage/enemy status mechanics** | [ENEMIES future abilities](../ENEMIES/README.md), [COMBAT](../COMBAT/README.md), [Waves modifiers](../LEVELS/Waves/README.md): armor mitigation, stealth/detection, EMP/slow, regeneration, buffs/status durations, additional boss/attack types; design only when a concrete first ability requires it. |
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
| [ ] | **Manual render/playtest verification** | Construction SVG/VFX once implemented, existing scene/DefenseBar appearance, 500-enemy motion, full HUD layout and ammo drone visual behaviour. Headless validation is not a substitute for looking in the Godot editor. |
| [ ] | **Stale status text / factual note cleanup** | [ENEMIES/README](../ENEMIES/README.md) still has an old N4 “planned” section; [enemypathing.md](../ENEMIES/enemypathing.md) contains a contradictory top “NOT IMPLEMENTED” line; [tower_ideas.md](../TOWERS/tower_ideas.md) still describes ranks/multi-cell/Laser as absent; [WORLD/README](../WORLD/README.md) has historical “Citadel health future” wording; [TOWERS/README](../TOWERS/README.md) has an obsolete “multi-cell placement not implemented” list item. Update obsolete lines against actual code, **do not implement those features again**. |
| [ ] | **Older ammo verification/update notes** | [Previous WORK detail below](#archived-completion-record--ammunition-pipeline), [Ammo tests](../TESTS/Economy/Ammunition.md): some original 1×1 wording and a pre-later-balance verification status are historical. Verify/refresh after current-footprint and shipment defaults, not new inventory architecture. |

## D. Already delivered — not outstanding implementation passes

- [x] **Enemy E2A/E2B/E3A:** mixed catalog, ShieldEmitter, Transporter + six-child staggered DeathSpawn and wave ownership. [ENEMIES](../ENEMIES/README.md) / [DeathSpawn tests](../TESTS/Enemies/DeathSpawn.md).
- [x] **Navigation N4, F3 route preview and checkpoint chains:** core implementation done; visual review separately listed above. [Movement](../ENEMIES/Movement/README.md), [Route preview](../ENEMIES/Movement/pathoverlay_tweaks.md).
- [x] **Multi-cell tower footprints F1–F5:** Factory **3×2**, Depot **2×2**, offense **1×1**, occupancy/path/performance tests. [Pass](../TOWERS/Placement/MULTI_CELL_FOOTPRINT_PASS.md).
- [x] **Tower core combat/stat/paid-level/rank foundation, Laser/Blaster/Shockwave, typed damage and enemy spatial queries.** [TOWERS](../TOWERS/README.md), [COMBAT](../COMBAT/README.md).
- [x] **Ammunition production, both physical drone delivery legs and offensive consumption.** See archived record below / [Ammo notes](../TOWERS/Economy/Ammunition/README.md).
- [x] **Results R0/R1:** real mission victory/defeat, snapshot, menu navigation; **not** R2–R4. [Results](../UI/Menus/Results/README.md).
- [x] **Profile identity Boot/Main/Campaign foundations, Pause basics**; not gameplay saves, XP or Research. [Profiles](../SYSTEMS/Profiles/README.md).
- [x] **Build HUD Offensive/Economy categories and compact Inspector** on the latest audited HEAD. [HUD check](../TESTS/UI/HudCategories.md).

## E. Suggested sequencing when returning

1. **Completed independent refactor:** GE1–GE3 inherited enemy scenes. No rebalancing; see `TESTS/Enemies/GenericEnemy.md` for verification limits.
2. **Campaign loop:** P1–P5 progression. Fold in the necessary Results R3 telemetry/R4 reward work once, then optionally do independent R2 polish.
3. **Building lifecycle:** C1–C4 timed construction and visual stages.
4. **Combat escalation:** first design/implement Tower HP/shields/damage/destruction; then E3B enemy attacks.
5. **Large management systems:** campaign Save/Load, then staged electricity generation/island grids/relays (order may be revisited as dependencies evolve).
6. **Pick individual optional feature passes** (advanced waves, additional towers, sounds, feedback, combat drones) rather than asking Codex to implement the entire design-only list at once.

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

