# Open Implementation Backlog — Complexity Ranking & Documentation Audit

> **Audit date:** 2026-10-23. **Source:** GitHub main at commit [4b7fbe3](https://github.com/ben0kay/godot-tower-defense/commit/4b7fbe3f4f8dbb0f8978062c38db03b892c995da) (last source changes seen 2026-10-21).  
> **Purpose:** one practical starting point for unfinished work, Codex-vs-regular-ChatGPT task selection, and later Markdown cleanup. **This is a documentation-only inventory**, not a new engine test or authorization to implement every idea.
>
> **Status precedence:** current code plus the latest dated implementation notes override earlier unchecked boxes and historical plan sections. An implemented source feature can still require runtime/desktop acceptance. Before editing, recheck HEAD and the authoritative feature README.

Start with the [root README](../README.md), [code preferences](CodePreferences.md), and the linked feature notes. Follow modular, performance-conscious Godot C#, shared data/configs, scoped tests and docs, and **one GitHub commit per job**.

## A. Unfinished work — largest to smallest

The ranking estimates *total project scope and integration risk*, not lines of code or a promise that every item can be done in one chat. **Codex** = reserve Work/Codex for deeper cross-system work; **Regular** = reasonable for a narrowly scoped normal ChatGPT coding conversation. Some medium items can use either depending on boundaries.

| # | Outstanding work | Complexity | Suggested tool | Reality at audit / next useful slice |
| ---: | --- | --- | --- | --- |
| 1 | [Campaign and battlefield saving](../SYSTEMS/Saving/README.md) | Very high | **Codex** | **S0/S1 foundation implemented (2026-10-26):** ownership audit, typed metadata and safe per-profile store. No gameplay callers yet. **S2 battlefield capture/restore and S3 Continue/Pause Save–Load remain pending**; see the saving implementation plan. Profile progression already persists independently. |
| 2 | [Electricity / island power networks](../TOWERS/Economy/Power/README.md) | Very high | **Codex** | **Open design.** Terrain-connected islands, shared Solar/Industrial/Fusion generator logic, optional consumers, shortages, 0–100 priority and manually paired relays. Stage region accounting before building entries, shutdowns and relays. |
| 3 | [PF0/PF3–PF5 combat performance](AGENT_FINDINGS/Performance.md) | Very high | **Codex + local measurements** | **Incomplete diagnosis, not permission for speculative rewrites.** PF1 source fix and PF2 F4 CPU profiling exist. Record reproducible Level100 400/500-enemy baselines, compare movement/fire/render loads, then choose **one measured** PF4 fix and run PF5 regressions. |
| 4 | [Research system expansion](../SYSTEMS/Progression/Research/README.md) | Very high | **Codex** | **Foundation implemented:** Research Core, Citadel Master I and Battle Tower Weapons I. Large tree, prerequisites/unlocks, doctrine/loadout slots, DME/Zy0n branches and special abilities remain separate future designs. Implement one approved effect family at a time. |
| 5 | [Wave Phase 3: advanced progression](../LEVELS/Waves/README.md) | High | **Codex** | **Partially open:** optional wave bonuses, countdown/auto-next, scripted encounters, preview/history and eventual wave-session persistence. Finite manual waves, mixed batches and basic banners already exist. Split by feature. |
| 6 | [E3B remaining enemy attack/ability types](../ENEMIES/README.md) | High | **Codex** | **Partial, not wholly missing:** EG1–EG3 already provide armed Gunner Drone, tower spatial targeting and damaging hostile bullets. Electrical area pulses, EMP disruption, support/buff ships and multi-ability bosses remain later work. |
| 7 | [Tower shields and repair producers](../TOWERS/Shared/Durability/README.md) | High | **Codex** | **Hull HP, damage and destruction are done.** Actual tower shield capacity/absorption and repair producers are not. Reuse shared damage resolution and [shield presentation](../FEEDBACK/Shields/README.md); do not rewrite existing tower health. |
| 8 | [EG4 armed-enemy stress verification / multi-gun groundwork](../ENEMIES/Weapons/README.md) | High | **Codex + local measurements** | **Open.** Benchmark 50/100/250+ firing ships and their bullets at realistic tower counts. Future independent multi-gun Cruiser is *not* implemented by EG1–EG3. |
| 9 | [VE5 environmental lighting](../WORLD/BACKGROUNDS/README.md) | High | **Codex** | **Open.** Optional 2D ambient lighting, a small number of local lights, possible occluders and reusable presets. The VE5C bloom experiment was **removed by request**; do not silently restore it. |
| 10 | [VE7 battle-responsive atmosphere](../WORLD/BACKGROUNDS/README.md) | High | **Codex** | **Open.** Shared bounded GPU motes/dust, event-driven combat intensity and smooth fade to quiet. Compare visual quality and frame costs with ON/OFF. |
| 11 | [Player combat drone ability](../DRONES/Combat/README.md) | High | **Codex** | **Concept only.** A route-following offensive ability; do not conflate it with completed ammunition transport or Citadel construction drones. |
| 12 | [Optional magazines, reloads and ammo variations](../TOWERS/Shared/Ammunition/README.md) | Medium–high | **Codex** | **Open extension.** Finite reserves and Factory → Depot → Tower deliveries already work. Reload must transfer from reserve, never mint ammunition; specialized ammo/logistics is separate later scope. |
| 13 | [Map-edge spawning](../ENEMIES/Spawning/MapEdges/README.md) | Medium–high | **Codex** | **README-only.** Add validated non-portal spawn sources without breaking current portal, route, checkpoint, ownership or Citadel destination handling. |
| 14 | [VE3 industrial corridor/material accents](../WORLD/BACKGROUNDS/README.md) | Medium–high | **Codex** | **Open.** Selective metal/foundation accent lighting and presets, respecting atlas UVs and performance. No animated node per tile. |
| 15 | [Organic terrain edges E3](../WORLD/TERRAIN/Edges/IMPLEMENTATION_PLAN.md) | Medium–high | **Codex** | **Open after E1/E2.** Jagged Mars rock silhouette, rock/dust/metal transition priority, more material profiles. E2 is already a generic opt-in rendered mask; don't build a second masking framework. |
| 16 | [VE6 local environmental FX](../WORLD/BACKGROUNDS/README.md) | Medium | Codex / Regular | **Open.** Local lava shimmer, steam/vents, smoke, fault flashes and visual-only preset integration; use bounded shared effects. |
| 17 | [VE4 regional ambience](../WORLD/BACKGROUNDS/README.md) | Medium | Codex / Regular | **Open.** Subtle localized dust wisps, industrial sparks and regional atmosphere; smaller opt-in pieces may suit regular chat. |
| 18 | [R3/R4 results and mission scoring extensions](../UI/Menus/Results/README.md) | Medium | Codex / Regular | **Partial.** Basic mission XP **is awarded/saved**, Results R2 vector presentation was source-implemented, and basic stats work. Credits earned/spent, first-clear/replay policies and performance/time bonuses remain. Do not implement a second XP service. |
| 19 | [NP3 wave narration follow-through](../LEVELS/Waves/Presentation/README.md) | Medium | **Regular** | **NP1 implemented and NP2 narrator in source.** Finish broader regression/desktop checks, any agreed skip/polish. Optional voice playback is a separate audio connection. |
| 20 | [CD3 builder-drone UX / desktop QA](../TOWERS/ConstructionDrones/README.md) | Medium | **Regular** | **CD1/CD2 implemented and tested.** Review beam/drone visuals, 20-build GPU cost, waiting Inspector presentation and agreed polish; do not rebuild the dispatcher. |
| 21 | [Player options/settings](../UI/Menus/Pause/README.md) | Medium | **Regular** | **Open.** Pause Save, Load and Options buttons are placeholders. A basic settings page may be separate; Save/Load itself belongs to item 1. |
| 22 | [Audio and tactical voice event wiring](../AUDIO/README.md) | Medium | **Regular** | **Foundation and Welcome Commander cue exist.** Additional clips, combat/wave events, menu ambience and settings remain. Verify imported audio in the Godot editor. |
| 23 | [Floating damage numbers](../FEEDBACK/DamageNumbers/README.md) | Small–medium | **Regular** | **Planned only.** Reusable capped visual-only numbers based on authoritative damage results; merge high-frequency hits to avoid label spam. |
| 24 | [Camera shake and impact reactions](../FEEDBACK/CameraEffects/README.md) | Small–medium | **Regular** | **Planned only.** Optional, bounded impact feedback; keep normal camera controls and zoom intact. |
| 25 | More research nodes using **existing** effect kinds | Small–medium | **Regular** | Expand incrementally after selecting actual bonuses/costs. New homing/precision behaviors require an explicit mechanic and unlock, not just a graph node. [Research](../SYSTEMS/Progression/Research/README.md). |
| 26 | Additional tower heat profiles | Small | **Regular** | [HD1–HD3](../TOWERS/Shared/Conditions/README.md) already exists; Battle has a **3-minute** overheat profile and **9-second** recovery. Other towers are currently heatless by default. Balance only selected opt-ins. |
| 27 | Authored mixed-wave content and balance | Small | **Regular** | Use existing [wave authoring](../LEVELS/Waves/Authoring.md), catalog and mission resources. Add only the intended combinations; no scheduler rewrite. |

### Useful scoping notes

- **New gameplay versus acceptance:** PF0/PF3/PF5, EG4, NP3 and parts of CD3 are primarily measurement/QA jobs; source code exists already. They are listed because the plan is not *accepted as fully finished*, not because their foundation is absent.
- **Do not double-count:** E3B overlaps the implemented EG1–EG3 weapon foundation; Results R4 overlaps implemented P1/P2 mission awards; optional homing research builds on implemented PA1–PA3 straight/homing handling.
- **Terrain E2 opt-in:** [DefaultTerrainEdgeConfig.tres](../WORLD/TERRAIN/Edges/DefaultTerrainEdgeConfig.tres) is **disabled by default** pending desktop shader/GPU approval. Its E2 code is already implemented; this is not an E2 coding task.
- **Current Level100 benchmark:** [Level100 README](../LEVELS/Level100/README.md) records **two waves: 400 and 500 ships**, **20 gunners each**, rather than the historical five-wave 100/200/300/400/500 composition.

## B. Additional unimplemented ideas — not necessarily approved phases

These are ideas or lightweight folder notes. **Do not treat this section as authorization to implement a broad feature** without checking dependencies and desired gameplay:

| Concept | Where it is described | Scope / caution |
| --- | --- | --- |
| [Missile Tower](../TOWERS/Offensive/MissileTower/README.md) | Offensive tower folder | README-only; start with a narrow variant of existing projectile/AoE infrastructure, not a new projectile engine. |
| [Radar Tower](../TOWERS/Support/RadarTower/README.md) | Support folder | Planned; **Stealth Beacon already reveals stealth**. Decide what Radar adds before coding. |
| [Repair Tower](../TOWERS/Support/RepairTower/README.md) | Support folder | Planned; should use actual capped tower HP and construction/disable rules. |
| [Scrap Refinery](../TOWERS/Economy/ScrapRefinery/README.md) | Economy folder | Planned raw-resource processing; new resource economy needs separate decisions. |
| [Hybrid Solar Tower](../TOWERS/Economy/SolarTower/README.md) | Old solar concept | Distinguish from planned **pure** Solar Generator to avoid duplicate catalog roles. |
| Tower selling/refunds | [Tower roadmap](../TOWERS/README.md) | Choose transaction/salvage/destruction policy separately. |
| Tower maintenance / operational wear | [Tower roadmap](../TOWERS/README.md) | Different from existing heat and construction; optional per-instance condition. |
| Armor/resistance and more damage interactions | [Combat](../COMBAT/README.md) | Typed shield/hull damage exists; armor mitigation and enemy-specific resistance are not live. |
| Enemy support heal, commander buffs, boss/hybrid, special ship attacks | [Enemy roadmap](../ENEMIES/README.md) | Individual future abilities, not one 'implement everything' pass. |
| Advanced ammo/resource logistics | [Economy roadmap](../TOWERS/Economy/README.md) | Fuel, raw materials, specialized ammo or research throughput are *not* needed for the current working loop. |
| Research doctrine slots, respec, DME story and Zy0n unlocks | [Progression plan](../SYSTEMS/Progression/PLAYER_XP_RESEARCH_FOUNDATION_PASS.md) | Long-term design beyond the small existing research graph. |
| Wave preview/history and auto start | [Waves phase 3](../LEVELS/Waves/README.md) | Could become small independent UI/timing passes instead of one big rewrite. |
| Level authoring convenience | [Authoring](../LEVELS/AUTHORING.md) | Shared Battlefield/MissionDefinition catalog already removes per-level service copies; optional visual editor remains future. |
| Homing unlock, prediction, terrain line-of-sight | [Projectile plan](../TOWERS/Shared/Projectiles/PROJECTILE_GUIDANCE_ACCURACY_PLAN.md) | Straight shots are now default; explicit homing handling already exists, but **research unlock and leading/LOS are not shipped**. |
| Future multi-genre empire/space shooter | [Root project vision](../README.md) | Deliberately **not** current TD scope; do not extract the game into a speculative cross-mode framework. |

The separate uploaded childhood GameMaker game notes are historical design inspiration; they are **not** a one-to-one list of missing Godot implementation phases.

## C. Already implemented — don't assign the same work again

This list describes **source delivery**, not universal desktop approval.

- **Tower/combat:** IT1–IT4 Inspector/6 target modes; CT1–CT3 Cryo; ST1 Stasis; HD1–HD3 generic disable/heat/Inspector; existing tower HP/damage/destruction; PA1–PA3 straight/projectile spread/homing contract; PV tracer/muzzle effects; Battle plus six offensive recoil rollouts; SR1–SR4 cosmetic shield ricochets.
- **World/enemies:** GE1–GE3 inherited enemy scenes; E2A/E2B catalogs/ShieldEmitter; E3A DeathSpawn; N4 corridor movement; MC1–MC3 multiple Citadels/HUD; SE1 stealth/Stealth Beacon; EG1–EG3 TowerRegistry/hostile bullets/Gunner Drone; shared route pool and segment-safety cache.
- **Levels/economy/UI:** Data-driven mission catalog/shared battlefield; Level002 tutorial/Level020 Seven-Wave Five Gates; Level100 two-wave stress shortcut; CP1–CP3 Credits economy and Resource Tower/ARG; Factory → Depot → Tower ammunition drones; CD1/CD2 Citadel builder; F2 multi-batch enemy debug with 1:1 previews; PF2 performance instrumentation; NP1 banners and NP2 narrator source; R2 Results vector/XP animation source; P1/P2 mission XP and P3 Levels/Research hub foundations.
- **Visuals:** VE1 space background/twinkle/nebula and VE2 portal/Citadel energy visuals; E1 atlas repair and E2 reusable rendered organic edge masks.

## D. Remaining verification and measurement (not missing architecture)

| Area | Needed before marking fully accepted |
| --- | --- |
| **PF0/PF1/PF2/PF3/PF5** | Collect *desktop* controlled Level100 CPU/GPU/frame-time baselines; validate PF1 movement registry change and current PF2 instrumentation; choose measured PF4 target. See [performance findings](AGENT_FINDINGS/Performance.md). |
| **EG4** | Gunner bullets and large armed enemy groups under realistic tower damage loads; record CPU and GPU separately. |
| **PA4** | Full visual/projectile handling acceptance of missed shots, aim, straight versus explicit homing, spread and physical impact. [Plan](../TOWERS/Shared/Projectiles/PROJECTILE_GUIDANCE_ACCURACY_PLAN.md). |
| **NP3** | .NET/Godot/UI suite for typewriter narrator, pause, retry, interruption and differing viewports. |
| **CD3** | Desktop cyan drones/beam, clipping, overlaps, multiple builders and FPS. |
| **VE1/VE2/E2** | Desktop shader import/render appearance and ON/OFF GPU checks at gameplay zoom; E2 remains default-off. |
| **Results R2** | Desktop winning/losing color, Skip, XP bar/level-up animation and responsive layout. |
| **Broad regression suites** | Re-run before attributing historical failures to new code: some older all-in-one fixtures had map assumptions, old enemy counts or assertion mismatches. Record actual runs; **this documentation pass executes no tests**. |

## E. Outdated Markdown flags — clean later, not edited in this audit

**FLAGGED OUTDATED** means a *specific passage* is contradicted by a newer dated entry or by present source/configs. It does **not** mean the whole document should be deleted. Each checkbox is for a later **Markdown-only cleanup**. Keep active design rationale, test caveats and authoritative phase details; archive **entirely superseded** historical handoffs only when no active file depends on their details.

### Priority 1 — misleading entrypoints/status

- [ ] **[Root README](../README.md):** older **Launch / Game controls** section still describes F2 as immediate catalog cycling, Ctrl as only Battle/Laser/Blaster, and a small old Campaign roster. Rewrite against the current F2 batch UI, catalog-driven trays, Level001/002/020/100 and shared Battlefield. Do **not** remove the newer implementation records at the top.
- [ ] **[Root README](../README.md):** older progression and ammunition/future sections say mission awards/research or features are deferred even where newer dated updates say implemented. Reconcile current state, preserving the distinction between saved **profile progression** and absent **battlefield/campaign slots**.
- [ ] **[NOTES/WORK.md](WORK.md):** the "Consolidated Outstanding Implementation Backlog" snapshot dated October 16–17 and later **PLANNED** blocks conflict with above-the-fold October 20–21 completion records (CD1/CD2, NP1/NP2, EG1–EG3, R2, E1/E2, VE1/VE2, etc.). Keep it as history or replace stale status with a link to **this** ranked index. Do not re-run unchecked completed passes.
- [ ] **[ENEMIES/README.md](../ENEMIES/README.md):** older "Current limitations" says no enemy weapons or tower durability; both now exist for Gunner/Kinetic and tower hulls. Other old N4 "planned" and only-three-enemy descriptions are historical. Narrowly state the genuinely absent **electrical pulses / advanced abilities**.
- [ ] **[LEVELS/README.md](../LEVELS/README.md):** top Level100 description still claims **five waves 100/200/300/400/500, 1,500 ships and 100 gunners**. The later [Level100 README](../LEVELS/Level100/README.md) and [WORK](WORK.md) record **two waves 400/500, 900 ships and 40 gunners**. Also replace obsolete central WaveCatalog switch / duplicated launcher instructions with the newer mission-resource flow.
- [ ] **[Performance agent findings](AGENT_FINDINGS/Performance.md):** PF0/PF5 and old load-test descriptions refer to **Level100 wave 5**. It is now **wave 2** for 500 ships; retain strict matched-workload requirements and refresh benchmark recipes, not their original measurements.

### Priority 2 — superseded plans embedded in feature READMEs

- [ ] **[Enemy weapons](../ENEMIES/Weapons/README.md):** original EG1–EG4 handoff says nothing implemented even though current top entries deliver EG1–EG3. Change the old block's label to **historical design**, leaving only EG4/visual QA live.
- [ ] **[Construction drones](../TOWERS/ConstructionDrones/README.md):** original CD1–CD3 plan retains unchecked CD1/CD2 and "not implemented". Mark it superseded by current CD1/CD2 notes; keep CD3 UX/GPU review open.
- [ ] **[Wave narration](../LEVELS/Waves/Presentation/README.md)** and **[wave README](../LEVELS/Waves/README.md):** old NP1/NP2 "not implemented" text conflicts with newer banner/narrator source records. Isolate NP3 verification and future voice wiring.
- [ ] **[Terrain edge plan](../WORLD/TERRAIN/Edges/IMPLEMENTATION_PLAN.md):** below the newer E1/E2 implementation entry the original handoff still states the atlas repair and render mask are NOT implemented. Relabel as historical; **E3 only** remains a new feature pass.
- [ ] **[Background/visual plan](../WORLD/BACKGROUNDS/README.md):** old VE1–VE7 umbrella says no shaders exist and all phases are planned. Newer notes show **VE1/VE2 implemented** and a requested VE5C bloom experiment **removed**. Retain VE3–VE7 as planned, and do not resurrect bloom.
- [ ] **[Shared weapons README](../TOWERS/Shared/Weapons/README.md)** and **[Recoil README](../TOWERS/Shared/Weapons/Recoil/README.md):** old text describes R1-only recoil as a future pilot and has unchecked items. Current Battle R1/R2 and other projectile tower rollout are in source; only desktop/runtime verification remains.
- [ ] **[Projectile muzzle VFX plan](../TOWERS/Shared/Weapons/Visuals/PROJECTILE_MUZZLE_VFX_IMPLEMENTATION.md)** and **[Projectile accuracy plan](../TOWERS/Shared/Projectiles/PROJECTILE_GUIDANCE_ACCURACY_PLAN.md):** original top plans claim NOT IMPLEMENTED, but later sections record PV1–PV3 and PA1–PA3 implemented. Preserve only PA4/visual checks as unresolved.
- [ ] **[Results README](../UI/Menus/Results/README.md)** and **[P1–P5 progression pass](../SYSTEMS/Progression/PLAYER_XP_RESEARCH_FOUNDATION_PASS.md):** historical "mission XP not granted", "Research future" and "P3 Levels future" sections are wrong after base awards, three research nodes and Campaign Levels. R2 visual source has since been implemented. Keep *replay bonuses, first-clear history, Credits telemetry, unlocks, saves and QA* explicitly open.
- [ ] **[Economy production README](../ECONOMY/Production/README.md):** historical CP2/CP3 planned and CP1B future descriptions conflict with implemented Resource Tower/ARG and clickable Citadel Credits. Separate unfinished Generator.2 / New Interest research from completed production.
- [ ] **[Campaign menu README](../UI/Menus/Campaign/README.md):** older "planned LEVELS hub / Research" paragraphs contradict current Campaign Levels and Research navigation. Keep save/level unlocks as future.
- [ ] **[Saving README](../SYSTEMS/Saving/README.md):** "only profile index persistence" is obsolete because profile progression/research/mission receipts persist. **Campaign/battlefield snapshots remain genuinely unimplemented**; update only the misleading identity-only line and preserve the actual save design.
- [ ] **[World README](../WORLD/README.md):** references Citadel health and waves/economy as future although those systems are live. Preserve actual grid/map/portal design; refresh the "next gameplay pass" wording.
- [ ] **[Tower roadmap](../TOWERS/README.md)** and **[tower_ideas.md](../TOWERS/tower_ideas.md):** some older foundation/HP/shield, multi-cell, recoil, Cryo/Stasis, rank or weapon-family proposals predate current source. Distinguish working hull health and modern weapon foundations from still-missing tower shield, electricity, magazines and repair.
- [ ] **[AUDIO/README.md](../AUDIO/README.md):** older "no clips/event wiring" language must account for the real Welcome Commander cue and profile-selection event. More voice/sounds are still open.

### Suggested safe cleanup order

1. **Markdown-only accuracy pass:** fix the root README, Level100 counts, PF old wave numbers, WORK index and high-priority false "not implemented" statements. Point future agents to this audit first.
2. **Feature-local history pass:** label superseded implementation handoffs as **Historical / Implemented** and update stale checkboxes only where a newer implementation/test note proves delivery. Leave genuinely pending QA open.
3. **Archive pass:** move wholly completed, self-contained implementation proposals into an appropriate NOTES archive *only after checking Markdown links*. Keep authoritative feature READMEs next to their code, and preserve useful architecture decisions rather than deleting history indiscriminately.

## F. Suggested delegation

**Codex next:** PF0 + PF3 measured performance baseline on Level100, then one evidenced fix; separately prepare a scoped Save/Load first pass when desired.  
**Regular ChatGPT next:** damage numbers, camera shake, a single additional research node, limited heat opt-ins, wave content, CD3/NP3 review, or a Markdown-only stale-notes cleanup.

For every job: **read root README + CodePreferences + the linked feature README**, verify current HEAD/code, keep scope narrow, make new work opt-in/configurable when sensible, avoid per-frame full-registry scans, run truthful focused tests where possible, update relevant notes and make **one commit per job**. Source-only verification is not an in-editor visual or GPU benchmark.

**Audit scope:** no game assets/C#/.tres/.tscn modified or tested as part of this file's creation. The cleanup flags above are intentionally **not** automatic edits to those other documents.
