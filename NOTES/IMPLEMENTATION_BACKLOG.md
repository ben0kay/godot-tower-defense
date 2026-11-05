## Power PW4-C allocation revision — 2026-11-05 (planning only)

**Decision:** When output is scarce, towers **closer by existing PW4-B conductive-source distance** receive recharge before faraway towers; equal-distance towers proportionally share any limited remainder, and distant ones may receive zero. This is **not priority-first**. The already-saved 0–100 `PowerPriority` must remain inactive; a future Inspector slider may introduce only a *minor bounded bias* if separately requested. PW4-D should focus on actual delivered recharge/drain, source distance, efficiency, buffer and shortage indication, without an inert priority slider. PW4-A/B remain implemented; **PW4-C is not yet implemented**. See [authoritative plan](../TOWERS/Economy/Power/IMPLEMENTATION_PLAN.md).

---

## PW4-B — cached distance-limited recharge — 2026-11-05

**Source Implemented / Awaiting Owner Validation.** Existing power manager now owns one coalesced multi-source conductive BFS; valid Citadel/generator footprints anchor delivery. Shared Inspector curve: 4 full cells, 12 soft falloff cells, 10% reachable-route floor. Recharge ceiling = nominal refill × efficiency; no route means zero. Capacity/drain/attack funding/source output and stored charge are unchanged; no new consumers, priority scheduling or relays. Cached consumer APIs expose distance/efficiency/ceiling; final derived cache is rebuilt before checkpoint unpause without new saved fields. Performance: reused arrays, no per-tower pathfinding/frame flood fill, no distance rebuild for positive output or curve-only edits. Narrow fixture updated; source/resource review only (environment unavailable), no build/runtime/benchmark claim. [Power guide](../TOWERS/Economy/Power/Shared/README.md).

---

## RT1 — playable Restoration Tower baseline — 2026-11-05

**Source Implemented / Awaiting Owner Validation.** New `repair` Support tower: 350 Credits, 5s, 1000 HP, 1×1, 320px, 16 hull HP/s, aimed white/silver/aqua turret and shared muzzle-origin beam. RT0 registry targeting, reusable scratch/cached lock, ordinary RestoreHealth, construction, selection and checkpoint owners reused. Added optional appended RepairStats with finite clamp/final forwarding and minimal correctly labelled Inspector rows. Enemy targeting, other tower balance and waves unchanged. **RT1 has no heat/power operating cost; RT2–RT4 remain planned.** Focused fixture authored; source/resource review only (environment unavailable), no build/runtime/visual/benchmark claim. [Repair guide](../TOWERS/Support/RepairTower/README.md).

---

## SE2 — directional Stealth Beacon scanning — 2026-11-05

**Source Implemented / Awaiting Owner Validation.** Prepared head now rotates clockwise and existing ScanCone path owns one faint sector; real 70° / 256px cone, 3s revolution, 0.1s sampled detection, alpha 0.08. Existing world reveal index/source identities are reused; capped nearby queries update stationary enemies, retained members do not flicker and independent overlaps remain revealed. Pause freezes scanning; disabled/unfinished/destroyed/removed sources sleep/hide/release. Source/config edits and index teardown retain cleanup. Art/scene paths, 250 Credits, 1800 HP, 2s build, waves and other mechanics are unchanged; no power/upgrade/bubble. Focused stealth checks updated; source review only (.NET unavailable), owner build/runtime/visual validation pending. [SE2 record](../TOWERS/Utility/StealthBeacon/DIRECTIONAL_SCAN_IMPLEMENTATION_PLAN.md).

---

## RT0 — reusable friendly-tower targeting — 2026-11-05

**Source Implemented / Awaiting Owner Validation.** Added optional TargetDomain vocabulary and FriendlyTowerTargeting using the existing TowerRegistry, caller-retained scratch and O(1) lock validation. Lowest finite hull fraction wins; exact ties use registration order. Root-range/self/full/dead/queued/detached/constructing/upgrading filters; disabled targets remain repairable while disabled/busy healers cannot acquire/retain locks. Existing enemy targeting/enums, registry, HUD, catalog, waves and stats are untouched. Optional narrow fixture authored; source checks only because .NET is unavailable. **RT1–RT4 remain planned.** [Repair plan](../TOWERS/Support/RepairTower/IMPLEMENTATION_PLAN.md).

---

## Power PW4-A update — 2026-11-05

**Source Implemented / Awaiting Owner Validation.** Battle-only optional capacitor and paid-shot energy use; finite provisional full-efficiency recharge in the existing manager; owned hysteretic NoPower; checkpoint charge/priority/recovery. Defaults 100 capacity, 10/s refill, 0/s idle, 12/s active, empty initial, 15% restart. No other consumers or combat/source balance changes. **Next PW4-B distance**, then C priority allocation and D selected energy bar; relays remain PW5. Lightweight source review only; Godot .NET build/play remains for owner. [Details](../TOWERS/Shared/Power/README.md). Earlier dated updates below are historical.

---

## Power PW4 revised roadmap — 2026-11-04 (planning, no implementation)

The owner revised PW4 from instant full/on-off grid allocation to **per-building rechargeable internal buffers with conductive-distance-dependent charging**. A tower far from a source can slowly reach full stored energy while idle, then drain faster than it recharges while firing. Live generation is shared and limited per connected island; priorities govern available refill, and NoPower applies only when the stored reserve is exhausted/low. Relays in PW5 will reinforce charging near themselves **without generating electricity**, while retaining future cross-island links. PW4-D Inspector should show the **stored-energy bar**, charge/drain/efficiency and priority beside Heat/Ammo. Follow [revised PW4-A–D and PW5 plan](../TOWERS/Economy/Power/IMPLEMENTATION_PLAN.md) rather than earlier full/empty planned bar or immediate blackout language. **PW0–PW3 remain unchanged; PW4/PW5 not implemented.**

---

## Power PW3 update — 2026-11-04

Industrial Generator/shared producer building implementation is source implemented. PW4 must include the owner's selected-tower **power bar styled like heat/ammo**, with supplied/required power and priority. No consumption/shutdown is active yet; Godot owner review remains pending. See [Industrial notes](../TOWERS/Economy/Power/IndustrialGenerator/README.md).

---

## Power PW2 update — 2026-11-04

Citadel baseline generation and per-island supply/requested-demand accounting are source implemented, awaiting brief Godot owner validation. Power remains descriptive accounting until consumers arrive in PW4. Next implementation slice is PW3 Industrial Generator; see [current power notes](../TOWERS/Economy/Power/Shared/README.md).

---

## Power PW0/PW1 update — 2026-11-04

Terrain power islands and F4 topology diagnostics are now source implemented, with the phased audit recorded. Citadel starter generation is explicitly planned for PW2; no tower operation or consumer gating changed. This update supersedes the power item's earlier blanket not-implemented status; the November 3 audit below remains historical for other areas.

---

# Implementation Backlog — Current Status and Remaining Work

> **Updated:** 2026-11-03 · Source: `dev` at [5aa469e](https://github.com/ben0kay/godot-tower-defense/commit/5aa469e40b2bd8e58cfbd00387c9981ade562b36). Original October 23 audit (on old `main` commit [4b7fbe3](https://github.com/ben0kay/godot-tower-defense/commit/4b7fbe3f4f8dbb0f8978062c38db03b892c995da)) preserved below as historical context.
> **Scope:** Status/documentation update only, not gameplay implementation, merge permission or Godot runtime acceptance. Recheck current source and each linked feature README before work. Follow [CodePreferences](CodePreferences.md) and [root README](../README.md).

## Current snapshot (November 3)

- **Delivered in source:** S0–S3 between-wave Save/Load and Campaign Continue; CD1–CD4 builder drones and paid-upgrade service; DU1/DU2 upgrade disabling; CV1 build/upgrade holograms; organic flight, slower 280px/s travel and cached soft-shader beams; HD1–HD4 heat; the core Godot Wave Editor and WP1; Level003 mission/wave resources; Results R2 XP presentation and **skippable stat-number count-up**; multiple tower construction/tier-art sets.
- **Main missing systems:** island electricity/generator grids, tower shields and repair, expanded research/doctrine/unlocks, advanced EMP/support/boss abilities, offensive player combat drone, magazines/reloads, map-edge spawning, environment effects, floating damage numbers and camera reactions.
- **Partial / content development:** campaign enemy/tower introduction order, Level003 terrain painting, real scoring/credits telemetry and replay policy, future level unlocks, advanced wave mechanics, audio/settings, more tier artwork and detailed performance measurement.
- **Acceptance caveat:** many recent features are *source-implemented*, but local Godot.NET builds, editor screenshots, runtime saving/reloading, visual checks and GPU benchmarks remain explicitly outstanding. Do not count QA work as missing foundational implementation.
- **Branch caution:** `dev` and `main` diverged when reviewed (28 commits ahead / 3 behind). Inspect changes before a requested merge; **never auto-merge `main` into `dev`**. The previous warning that Results stat count-up exists only on `main` is obsolete: `dev` now contains `AnimateStatNumbers` and `UpdateStatNumber`.

## Suggested next jobs

| Priority | Job | Scope |
| ---: | --- | --- |
| 1 | **WP2 Wave Editor sprite/stat preview** | Show real registered enemy artwork, name, HP, optional shields and speed without creating live enemy simulation. |
| 2 | **WP3 Wave Editor button arrangement** | Move Add/Duplicate/Delete/Move controls above lists; retain confirmation and disabled states. |
| 3 | **Campaign introduction hierarchy** | Plan which enemies appear in each level, final-wave previews, returning enemy mixes and newly available towers. Do not change internal IDs for campaign order. |
| 4 | **WP4 wave summary** | Display authored totals, shield/stealth mix and estimated spawn scheduling duration, not a made-up difficulty score. |
| 5 | **Branch comparison before release** | Reconcile divergent dev/main commits deliberately; **do not reimplement Results count-up**. |
| 6 | **Floating damage numbers** | Pool/cap visual-only combat feedback; aggregate very rapid hits. |
| 7 | **Camera impact shake** | Opt-in bounded feedback; preserve camera controls. |
| 8 | **Player Options/settings** | Implement settings UI separately from completed Save/Load. |

## A. Ranked work areas — largest to smallest

The original 26-area order is preserved for continuity. **Not every ranked item is still missing:** some are delivered in source and remain listed only for testing or future extensions. Complexity estimates total scope/integration risk, not time per chat. **Codex** = reserve Work/Codex for deeper cross-system work; **Regular** = reasonable for a narrowly scoped normal ChatGPT coding conversation. Some medium items can use either depending on boundaries.

| # | Work area | Complexity | Suggested tool | Status on `dev` / next useful slice |
| ---: | --- | --- | --- | --- |
| 1 | [Campaign and battlefield saving](../SYSTEMS/Saving/README.md) | Very high | **Codex** | **SOURCE DONE (S0–S3), QA OPEN:** one-current-checkpoint between-wave save, profile store, Esc Save/Load and Campaign Continue. Verify resume in Godot, original reward tickets, active builders/ammo. Mid-combat save and multiple slots remain excluded. |
| 2 | [Electricity / island power networks](../TOWERS/Economy/Power/README.md) | Very high | **Codex** | **PARTIAL (PW0–PW3):** source audit/phased plan, cached conducting terrain islands and F4 diagnostics implemented; owner Godot validation pending. **PW2 source implemented:** Citadel starter output=100 provisional, cached supply/requested-demand and F4 readout; no outages yet. **PW3 source implemented:** shared generator code + Industrial Economy building (250 output / 300 Credits / 5s / 1200 HP / 2×2), existing save/work/disable support. **NEXT PW4:** consumers, priorities/shortages and selected Inspector power bar matching heat/ammo. Solar/Fusion, consumers/shortages and relays remain planned. See [power phases](../TOWERS/Economy/Power/IMPLEMENTATION_PLAN.md). |
| 3 | [PF0/PF3–PF5 combat performance](AGENT_FINDINGS/Performance.md) | Very high | **Codex + local measurements** | **PARTIAL / PROFILE NEXT:** PF1 routing/cache fixes and PF2 F4 profiling exist. Reproduce Level100 400/500-enemy CPU/GPU frame timings, then one measured PF4 fix and PF5 validation. |
| 4 | [Research system expansion](../SYSTEMS/Progression/Research/README.md) | Very high | **Codex** | **PARTIAL:** foundation/research starter nodes exist; broader tree, prerequisites, unlocks, doctrines and story branches remain. |
| 5 | [Wave Phase 3: advanced progression](../LEVELS/Waves/README.md) | High | **Codex** | **PARTIAL:** finite manual mixed waves, banners and narrator exist. Editor .tres pilot/WP1 delivered; auto start, history, extras and scripted encounters remain separate work. |
| 6 | [E3B remaining enemy attack/ability types](../ENEMIES/README.md) | High | **Codex** | **PARTIAL:** Gunner Drone weapons and shared hostile bullets implemented; EMP, electrical pulses, support buffs/healing and complex bosses not. |
| 7 | [Tower shields and repair producers](../TOWERS/Shared/Durability/README.md) | High | **Codex** | **NOT IMPLEMENTED extension:** tower HP/damage/destruction exists; tower shields, absorption and repair producers not. |
| 8 | [EG4 armed-enemy stress verification / multi-gun groundwork](../ENEMIES/Weapons/README.md) | High | **Codex + local measurements** | **OPEN QA/EXTENSION:** stress-test realistic gunner counts and eventually design independent Cruiser multi-guns. |
| 9 | [VE5 environmental lighting](../WORLD/BACKGROUNDS/README.md) | High | **Codex** | **NOT IMPLEMENTED:** ambient/local 2D lighting. Do not reinstate user-reverted VE5C bloom. |
| 10 | [VE7 battle-responsive atmosphere](../WORLD/BACKGROUNDS/README.md) | High | **Codex** | **NOT IMPLEMENTED:** battle-responsive event-driven bounded motes/dust. |
| 11 | [Player combat drone ability](../DRONES/Combat/README.md) | High | **Codex** | **CONCEPT ONLY:** offensive player ability drone, not ammo couriers or Citadel construction drones. |
| 12 | [Optional magazines, reloads and ammo variations](../TOWERS/Shared/Ammunition/README.md) | Medium–high | **Codex** | **NOT IMPLEMENTED extension:** reload/magazine/ammo variations; functioning Factory → Depot → Tower pipeline already exists. |
| 13 | [Map-edge spawning](../ENEMIES/Spawning/MapEdges/README.md) | Medium–high | **Codex** | **NOT IMPLEMENTED:** non-portal map-edge spawning compatible with route, wave and save ownership. |
| 14 | [VE3 industrial corridor/material accents](../WORLD/BACKGROUNDS/README.md) | Medium–high | **Codex** | **NOT IMPLEMENTED:** VE3 industrial material/foundation accents. |
| 15 | [Organic terrain edges E3](../WORLD/TERRAIN/Edges/IMPLEMENTATION_PLAN.md) | Medium–high | **Codex** | **NOT IMPLEMENTED extension:** E1/E2 atlas and generic mask source are done; E3 rock shapes and material transitions still planned. |
| 16 | [VE6 local environmental FX](../WORLD/BACKGROUNDS/README.md) | Medium | Codex / Regular | **NOT IMPLEMENTED:** opt-in lava shimmer, steam, smoke and vents. |
| 17 | [VE4 regional ambience](../WORLD/BACKGROUNDS/README.md) | Medium | Codex / Regular | **NOT IMPLEMENTED:** regional ambience and local effects. |
| 18 | [R3/R4 results and mission scoring extensions](../UI/Menus/Results/README.md) | Medium | Codex / Regular | **PARTIAL:** R2 presentation, saved XP, bar/level-up summary and **stat count-up on dev** are implemented. Still open: Credits telemetry, additional R3 stats, first-clear/replay/anti-farming, scoring bonuses and richer level-up flair. |
| 19 | [NP3 wave narration follow-through](../LEVELS/Waves/Presentation/README.md) | Medium | **Regular** | **SOURCE DONE / QA:** NP1 wave banners and NP2 tactical narration exist; NP3 regression/viewport/skip/voice follow-through remains. |
| 20 | [CD3 builder-drone UX / desktop QA](../TOWERS/ConstructionDrones/README.md) | Medium | **Regular** | **CD1–CD4 SOURCE DONE / QA:** build/upgrade drones, eased 280px/s flight, cached shader cone and 24Hz scan lines, CV1 holograms. Check overlaps, clipping, 20-build GPU load, tier transitions, mid-upgrade Save/Load. |
| 21 | [Player options/settings](../UI/Menus/Pause/README.md) | Medium | **Regular** | **PARTIAL:** Esc Save and Load implemented under S3; **Options/settings remains a placeholder**. |
| 22 | [Audio and tactical voice event wiring](../AUDIO/README.md) | Medium | **Regular** | **PARTIAL:** Welcome Commander cue and foundation exist; combat/wave voice wiring, ambience and settings missing. |
| 23 | [Floating damage numbers](../FEEDBACK/DamageNumbers/README.md) | Small–medium | **Regular** | **NOT IMPLEMENTED:** capped, merged hit/damage numbers. |
| 24 | [Camera shake and impact reactions](../FEEDBACK/CameraEffects/README.md) | Small–medium | **Regular** | **NOT IMPLEMENTED:** bounded camera shake/impact feedback. |
| 25 | More research nodes using **existing** effect kinds | Small–medium | **Regular** | **OPEN content:** author approved research nodes/effects. Homing/precision unlock requires separate mechanic work. |
| 26 | Authored mixed-wave content and balance | Small | **Regular** | **ONGOING CONTENT:** Level003's editable wave resource exists, but **map tiles remain blank**; paint a connected walkable path and balance encounter/new tower introductions. |

### Useful scoping notes

- **New gameplay versus acceptance:** PF0/PF3/PF5, EG4, NP3 and parts of CD3 are primarily measurement/QA jobs; source code exists already. They are listed because the plan is not *accepted as fully finished*, not because their foundation is absent.
- **Do not double-count:** E3B overlaps the implemented EG1–EG3 weapon foundation; Results R4 overlaps implemented P1/P2 mission awards; optional homing research builds on implemented PA1–PA3 straight/homing handling.
- **Terrain E2 opt-in:** [DefaultTerrainEdgeConfig.tres](../WORLD/TERRAIN/Edges/DefaultTerrainEdgeConfig.tres) is **disabled by default** pending desktop shader/GPU approval. Its E2 code is already implemented; this is not an E2 coding task.
- **Current Level100 benchmark:** [Level100 README](../LEVELS/Level100/README.md) records **two waves: 400 and 500 ships**, **20 gunners each**, rather than the historical five-wave 100/200/300/400/500 composition.

## A2. Wave Editor backlog — newer than the original audit

The `addons/wave_editor` Godot main-screen plugin and native `.tres` wave resources are implemented in source. Level003 uses these resources; older levels still author waves in C#. See [Wave Editor guide](../addons/wave_editor/README.md) and [WP plan](../addons/wave_editor/PLAN.md).

| Phase | Status | Scope / next step |
| --- | --- | --- |
| Editor foundation | **Source implemented / QA** | Open/New/Save/Reload and edit waves, batches, entries; enemy catalogue dropdown and validation. Rebuild in Godot.NET to inspect UI. |
| WP1 | **Source implemented / QA** | Exact composition bars, useful tooltips and deletion confirmation. |
| Layout V2 | **Source implemented / QA** | Direct full-height `VBoxContainer` after collapsed viewport screenshot; verify real editor after rebuilding. |
| WP2 | **OPEN** | Registered ship sprite/quick stats preview (HP, shield capacity, speed, optionally Citadel damage). |
| WP3 | **OPEN** | Move Add/Duplicate/Delete/Move controls to top of each list. |
| WP4 | **OPEN** | Wave composition/expected shielded and stealth counts/batch count/estimated spawn schedule. |
| WP5 | **OPEN** | Explicit optional auto-renumber; preserve original numbers until requested. |
| WP6 | **OPEN** | Save As / duplicate complete Wave Set without overwriting existing resources. |
| WP7 | **OPEN** | Enemy search/filter by display name/stable ID; retain unregistered warnings. |
| Later | **CONCEPT** | Exact per-type count entry versus weighted mix; optional non-live timeline preview. Agree data schema before implementing. |

## A3. Campaign progression and newer content gaps

- **Enemy introduction hierarchy:** decide first encounters and later combinations, e.g. Enemy001 → Enemy002 late in Level001, then progressive new threats. **Enemy003 is Shield Emitter**, a support ship; asset numbering does *not* dictate campaign introduction order. Do not rename stable Enemy IDs for an authoring sequence.
- **Tower availability per mission:** choose new towers or research unlocks per level; there is not yet an authoritative campaign release/unlock ladder.
- **Level003 map authoring:** mission and four-wave `Level003Waves.tres` exist, but the TileMap is an intentionally empty template; connected Passable tiles are necessary before waves can run.
- **Results beyond R2:** Credits earned/spent, first-clear/replay policies, anti-farming, mission-time/performance bonuses, enhanced level-up celebration. **Skippable stat count-up is already delivered in `dev`**.
- **Tower artwork completeness:** staged and tier-specific art already covers multiple towers (Battle, Machine Gun, Blaster, Rocket, Sniper, Cryo, Stasis, Laser); determine any remaining tiers per tower instead of rebuilding the shared presenter.
- **Enemy hull-damage artwork:** Enemy001 has an implemented optional multi-stage damaged sprite; rolling out to other enemy types is a separate optional art pass, not a new damage system.

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

The uploaded childhood GameMaker game notes are historical design inspiration; they are **not** a one-to-one list of missing Godot implementation phases.

## C. Already implemented — don't assign the same work again

**Added since the original October 23 audit (source delivered through November 3):** S0–S3 checkpoint saving; CD4 upgrade-service drones, CV1 upgrade holograms, DU1/DU2 disable; cached soft-gradient construction beams and organic travel; Level003 mission plus `.tres` authoring; Wave Editor core/WP1/Layout V2; Results stat count-up; Enemy003 source rename (Shield Emitter, stable `shield_emitter_mk1`), new ship art and Enemy001 damage sprites; authored staged construction/purchased-tier art for several offensive towers; Sniper InstantTarget delivery. See owning feature docs and root README for exact per-tower coverage. **Desktop acceptance remains separate.**


- **HD4 authored heat presets (2026-10-26):** Machine Gun (2min), Laser (3.5min), Blaster Cannon/Rocket (4min), Shockwave (3min), each with a modest independent recovery setting; shared beam/pulse timing, Inspector opt-in and source-level focused checks are implemented. Battle remains unchanged; desktop Godot acceptance pending. [Conditions](../TOWERS/Shared/Conditions/README.md).
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
| **CD3/CD4** | Desktop cached-shader beam, clipping, overlapping builds/upgrades, 20-builder FPS, progress/stage art and saved mid-upgrade restoration. |
| **Wave Editor WP1/Layout V2** | Rebuild/enable Godot.NET plugin, verify full-height UI and composition preview, then edit a sample Level003 wave. |
| **Saving S0–S3** | Check actual profile Save/Load/Continue, active builders/ammo, between-wave eligibility and safe failure recovery. |
| **VE1/VE2/E2** | Desktop shader import/render appearance and ON/OFF GPU checks at gameplay zoom; E2 remains default-off. |
| **Results R2 and number count-up** | Test victory/defeat, Skip, stat number count-up, XP-bar/level-up presentation and responsive layout on desktop; source implementation exists on `dev`. |
| **Broad regression suites** | Re-run before attributing historical failures to new code: some older all-in-one fixtures had map assumptions, old enemy counts or assertion mismatches. Record actual runs; **this documentation pass executes no tests**. |

## E. Outdated Markdown flags — clean later, not edited in this audit

**FLAGGED OUTDATED** means a *specific passage* is contradicted by a newer dated entry or by present source/configs. It does **not** mean the whole document should be deleted. Each checkbox is for a later **Markdown-only cleanup**. Keep active design rationale, test caveats and authoritative phase details; archive **entirely superseded** historical handoffs only when no active file depends on their details.

### Priority 1 — misleading entrypoints/status

- [ ] **[Root README](../README.md):** older **Launch / Game controls** section still describes F2 as immediate catalog cycling, Ctrl as only Battle/Laser/Blaster, and a small old Campaign roster. Rewrite against the current F2 batch UI, catalog-driven trays, Level001/002/020/100 and shared Battlefield. Do **not** remove the newer implementation records at the top.
- [ ] **[Root README](../README.md):** older progression and ammunition/future sections say mission awards/research or features are deferred even where newer dated updates say implemented. Reconcile current state, preserving the distinction between saved **profile progression** and implemented **single-current battlefield checkpoint**, but still-absent **multiple-slot interface**.
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
- [ ] **[Results README](../UI/Menus/Results/README.md)** and **[P1–P5 progression pass](../SYSTEMS/Progression/PLAYER_XP_RESEARCH_FOUNDATION_PASS.md):** historical "mission XP not granted", "Research future" and "P3 Levels future" sections are wrong after base awards, three research nodes and Campaign Levels. R2 **and skippable number count-up** are source-implemented. Keep *replay bonuses, first-clear history, Credits telemetry, unlocks, multiple save slots and QA* explicitly open.
- [ ] **[Economy production README](../ECONOMY/Production/README.md):** historical CP2/CP3 planned and CP1B future descriptions conflict with implemented Resource Tower/ARG and clickable Citadel Credits. Separate unfinished Generator.2 / New Interest research from completed production.
- [ ] **[Campaign menu README](../UI/Menus/Campaign/README.md):** older "planned LEVELS hub / Research" paragraphs contradict current Campaign Levels and Research navigation. Keep save/level unlocks as future.
- [ ] **[Saving README](../SYSTEMS/Saving/README.md):** "only profile index persistence" is obsolete because profile progression/research/mission receipts persist. **S0–S3 checkpoints are source-implemented**; preserve local runtime QA and multiple-slot / mid-combat-save exclusions.
- [ ] **[World README](../WORLD/README.md):** references Citadel health and waves/economy as future although those systems are live. Preserve actual grid/map/portal design; refresh the "next gameplay pass" wording.
- [ ] **[Tower roadmap](../TOWERS/README.md)** and **[tower_ideas.md](../TOWERS/tower_ideas.md):** some older foundation/HP/shield, multi-cell, recoil, Cryo/Stasis, rank or weapon-family proposals predate current source. Distinguish working hull health and modern weapon foundations from still-missing tower shield, electricity, magazines and repair.
- [ ] **[AUDIO/README.md](../AUDIO/README.md):** older "no clips/event wiring" language must account for the real Welcome Commander cue and profile-selection event. More voice/sounds are still open.

### Suggested safe cleanup order

1. **Markdown-only accuracy pass:** fix the root README, Level100 counts, PF old wave numbers, WORK index and high-priority false "not implemented" statements. Point future agents to this audit first.
2. **Feature-local history pass:** label superseded implementation handoffs as **Historical / Implemented** and update stale checkboxes only where a newer implementation/test note proves delivery. Leave genuinely pending QA open.
3. **Archive pass:** move wholly completed, self-contained implementation proposals into an appropriate NOTES archive *only after checking Markdown links*. Keep authoritative feature READMEs next to their code, and preserve useful architecture decisions rather than deleting history indiscriminately.

## F. Suggested delegation

**Codex next:** PF0/PF3 measured Level100 baseline and one evidenced fix; staged island-power/research/tower-shield/advanced-enemy tasks when approved. **Do not implement S0–S3 again; fix only demonstrated runtime issues.**  
**Regular ChatGPT next:** WP2/WP3/WP4 Wave Editor improvements, campaign introduction ladder, floating damage numbers, camera impact reactions, real Options menu, wave content or a Markdown-only stale-notes cleanup.

For every job: **read root README + CodePreferences + the linked feature README**, verify current HEAD/code, keep scope narrow, make new work opt-in/configurable when sensible, avoid per-frame full-registry scans, run truthful focused tests where possible, update relevant notes and make **one commit per job**. Source-only verification is not an in-editor visual or GPU benchmark.

**Audit scope:** no game assets/C#/.tres/.tscn modified or tested as part of this file's creation. The cleanup flags above are intentionally **not** automatic edits to those other documents.
