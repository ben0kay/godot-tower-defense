## PW6-A/B generator additions — 2026-11-06

**Source Implemented / Awaiting Owner Validation.** Solar 1×1 (75/sec, 125 Credits, 3s, 700 HP) and Fusion 4×4 (900/sec, 1,200 Credits, 12s, 2,500 HP) are registered and share the existing Industrial Generator code, normal construction/staged SVGs, operations and save seams. No new source algorithm or priority slider. **PW6-C** UI polish and balancing remain planned. See [Power Plan](../TOWERS/Economy/Power/IMPLEMENTATION_PLAN.md). Brief editor checks: exact footprints, stages, F4 output, disable/removal and new between-wave save.

---

## PW5-C — distribution visuals and inspection — 2026-11-05

**Source Implemented / Awaiting Owner Visual Validation.** Selecting a Power Relay now shows faint cyan filled/outlined **reachable conductive cells**, plus a thin dashed shortest route to a live source and the label **Nearest live source / Island supply shared**. No circular coverage spanning gaps. Inactive/source-less Relays show no coverage/route. Deselecting hides the diagnostic immediately. The route follows cached conductive distance and positive Transmitter hops when supply is remote; ties are deterministic and a source footprint terminates the path.

Powered Transmitter pairs now have a narrow cyan core, faint halo and two restrained travelling highlights. Unpowered retained links are dashed blue-grey; suspended links are dashed amber; unlink/removal hides the line. Only the lower-SaveId endpoint owns a renderer. Highlights are cosmetic, not an exclusive source or measured energy-flow direction. No power generation, allocation, booster, hop cost, consumer charge, combat, construction or save-format changes.

Selected distribution Inspector status remains bounded to three lines, with concise state, physical island / effective network / shared supply, plus peer identity/island or Relay boost coverage/ceiling. F4's existing throttled power row now distinguishes hovered physical-island output from joined-network supply and global active pair count. No new diagnostic polling loop or growing status rows.

**Tuning:** `FEEDBACK/Power/DefaultPowerDistributionVisuals.tres` exposes coverage fill/edge, source path/dash length, link core/halo/status colors and widths, pulse period (default 2.8s) and animation cadence (15 Hz default; clamped 5–30). Restart/redeploy after authoring style changes. Art and simulation settings stay with their existing owners.

**Performance/lifecycle:** Relay geometry uses retained buffers and caches by delivery revision/selected transform, and only builds while selected. Coverage extraction reuses the bounded conductive search scratch; route reconstruction descends the existing cost field without a second source search. Straight route sections collapse to one drawn segment; exceptional paths cap at 4,096 steps with an explicit preview-capped label. No per-frame map traversal/pathfinding. Link geometry updates on lifecycle/transform/state events. Only an onscreen powered pair processes its small local cosmetic clock; redraw is capped at 15 Hz by default, processing sleeps offscreen/hidden/unpowered/suspended, and inherited scene pause freezes animation. Static visuals rely on normal canvas culling. Removing a tower destroys its owned child visuals; no autoload or new power service.

**Validation:** minimal source/diff and native resource-reference checks only; .NET unavailable. No C# build, full Godot gameplay/visual acceptance or FPS benchmark is claimed. Owner: pull/rebuild, load Level011, select a powered Relay near a gap, then select/deselect and remove its source. Pair Transmitters, inspect active/unpowered/EMP-suspended styles, pan away/back, pause and try a fresh save/load. PW6/new generators and the optional priority slider remain deferred.


---

## PW5-A/B — two Utility power buildings — 2026-11-05

**Source Implemented / Awaiting Owner Validation.** Power Relay locally boosts capacitor recharge on conducting terrain (best-only 10-cell/85% provisional ceiling). Separate Power Transmitter supports explicit LINK → another island’s Transmitter / UNLINK, one retained reciprocal peer and a thin static cyan/amber link. Shared cached networks count real output once, use positive-hop source distance and retain finite distance-first allocation. Provisional Relay 150 Credits/3s/800 HP; Transmitter 200/4s/900 HP; both 1×1, no generation/upkeep or local boost from Transmitters. Existing energy/heat/ammo/work/disable/save owners are reused; optional durable peer IDs resolve before unpause. Minimal source/resource/SVG checks only; .NET unavailable, C# fixtures authored but unrun. PW5-C visual polish, PW6 and priority slider remain planned.

[Full details and owner checks](../TOWERS/Economy/Power/IMPLEMENTATION_PLAN.md).

---

# Implementation Backlog — Live Status (2026-11-05)

> **Current snapshot checked against `dev` at [f90010ba7](https://github.com/ben0kay/godot-tower-defense/commit/f90010ba7db42eda2408532692c7cb57cac8b3a3); source evidence, not Godot gameplay acceptance.** The dated entries farther down preserve their original implementation history, which may contain superseded `PLANNED` wording. For task planning, use this section, the ranked table and each feature's newest owning README/plan rather than older notes. No gameplay code is authorized by this backlog update.

## Recent completed source work (November 4–5)

| Area | Verified current source status | Remaining boundary |
| --- | --- | --- |
| **Power PW0–PW4-D** | Conducting islands, 100/s Citadel and 250/s Industrial output, tower capacitors, cached distance falloff, **distance-first finite allocation**, selected ENERGY/rate feedback. All **nine offensive types plus Restoration** opt into shared consumer funding; NoPower uses the existing disable system. | Owner tuning/QA; **PW5 relays** and **PW6 Solar/Fusion** unimplemented. Saved PowerPriority is dormant; any mild priority bias needs separate approval. |
| **Restoration RT0/RT1 and tiers** | Friendly hull-targeting, aimed healing beam, sticky target until fully repaired, purchased **Levels 2–3** with tier art, and actual shared funded power consumption. | **RT2 heat is not yet wired**; additional friendly-target selector/support-XP choices and RT4 multi-target/shield/drone/material extensions require separate approval. |
| **LM1 and large Level011** | Shared `LEVELS/Shared/BaseLevelMap.tscn` with local paintable layers; Level001/002/003/011/020/100 and template now inherit it. Level011 has **192×120** local dimensions, **three portals/checkpoint routes**, and **five** escalating authored waves (**54/84/120/162/204 = 624** scheduled ships) across all 15 registered enemy types. | Godot owner route/performance/balance QA; **Waves 6–10** remain to author. Mission currently ends after Wave 5; transport death children increase actual counts. |
| **Enemies and debug** | Separate physical **Class / threat Rank / functional Role** metadata; 15-ship roster and size-class boundaries now reserve Capital/Titan for future huge ships. F2 groups by physical Class, shows native art; existing wave editor keeps IDs. New Heavy Gunner included. | No registered Capital/Titan yet; EG4 larger armed groups and multi-weapon ships remain future design/profile work. |
| **Map, tower and UI improvements** | Mars mountains with cached visuals and tower sight blocking; selected range marks obscured sectors red; Restoration holds target; selected Inspector PowerDetails no longer resizes for NoPower; shared warning triangle and tint indicate disabled placed towers. Three-tier directional Stealth Beacon, Results victory hold and Level011 authored terrain are in source. | Desktop visual checks, mountain sight/visual sorting, and selected-panel viewport acceptance remain. |
| **Audio** | Original Battle Tower shot variant WAV and Machine Gun per-shot clip are wired through the shared positional audio system; profile greeting also exists. | Owner mix/voice/latency validation, more tower/enemy cues, wave/combat voice hooks, ambience and player Options. |

## Remaining power roadmap — use the current owner decision

| Phase | Status | Codex-sized scope |
| --- | --- | --- |
| PW0–PW4-D | **Source implemented; owner acceptance pending** | Inspect near/far consumers, brownouts, buffer recovery, same-distance ties, pause/EMP, save/load, and GPU/CPU at normal game scale. **Do not reimplement.** |
| **PW5-A — local Power Relay** | **Approved design / OPEN** | Non-generating local recharge booster on its *own* powered island; cannot pair or transport. Basic powered-state visual. |
| **PW5-B — Power Transmitter** | **Approved design / OPEN** | **Separate building**, manual Transmitter-to-Transmitter cross-island link, finite merged supply, saved reciprocal IDs and **required visible beam**. |
| **PW5-C — Power visuals** | **Approved design / OPEN** | Selected Relay actual conducting coverage and source route; restrained animated cross-island link and Inspector/F4 feedback. |
| **PW6** | **OPEN** | Independent Solar Generator and Fusion Reactor types using existing shared producer implementation; balance and status/performance polish. |
| Optional bounded priority | **NOT APPROVED** | Persisted 0–100 PowerPriority is currently inactive. Later UI/limited bias must not override distance-first policy. |

**PW5 handoff:** [Authoritative plan](../TOWERS/Economy/Power/IMPLEMENTATION_PLAN.md), [local Relay](../TOWERS/Utility/PowerRelay/README.md), [separate Power Transmitter](../TOWERS/Utility/PowerTransmitter/README.md). The prior combined Relay design is obsolete: A local booster, B transmitter/link/basic beam, C visual polish. Implement one requested phase at a time; PW6 remains separate. No new manager, battery, per-tower tick or free energy.

## Other priority work still open

| Domain | Recommended next task | Distinction |
| --- | --- | --- |
| **Level011 content** | Author **Waves 6–10** with increasing Heavy/Superheavy counts; use future Capital/Titan only after actual enemy types exist. | First five editable waves already implemented; layout and three routes already authored. |
| **Repair/durability** | Decide RT2 heat profile and repair support-XP/target policy; design true **tower shields** as a separate durability feature. | Hull repair, sticky targeting, L2/L3 and funded power are already functional in source. |
| **Performance** | PF0/PF3 Level100 measured 400/500-ship baseline, then one evidenced PF4 optimization and PF5 retest; EG4 gunner groups. | PF1 routing/cache work and PF2 F4 instrumention exist; no FPS gain should be claimed without measurements. |
| **Wave Editor** | WP2 sprite/stats preview, WP3 action buttons, WP4 composition summary, WP5 renumber, WP6 Save As, WP7 enemy search. | Editor core/WP1/Layout V2 exist; Level001/003/011 currently use editable wave resources. |
| **Progression/encounters** | Campaign enemy/tower unlock ladder, research/doctrines, advanced enemy EMP/abilities, boss encounter scripting and remaining wave controls. | Keep Class, Rank and Role independent; avoid inventing Capital/Titan entries for existing small ships. |
| **Feedback and menus** | Floating damage numbers, camera shake, player Options, more audio/voice cues, richer Results telemetry and replay policy. | Existing Results XP/number count-up and current Battle/Machine Gun sounds need not be rebuilt. |
| **World/visuals** | E3 rock/edge transitions; VE3 industrial materials, VE4 ambience, VE5 lighting, VE6 local FX, VE7 responsive atmosphere. | E1/E2 and VE1/VE2 exist; E2 visual is default-off; do **not** restore reverted VE5C bloom. |
| **Longer-term extensions** | Tower magazines/reloads, real armor/resistance variations, Radar/Missile towers, player combat drone, selling/refunds, resource/fuel logistics. | Many are proposals only, not approved passes. |
| **Maintenance** | Verify Save/Load, power, repair, LM1 terrain/portals and Level011 checkpoint routes; reconcile older misleading README/WORK statements. | These are acceptance/documentation tasks, not missing foundation code. |

---

## Shared power rollout to offensive towers and Repair — 2026-11-05

**Source Implemented / Awaiting Owner Validation.** All nine offensive types plus Restoration Tower now use PW4-A–D capacitors and the existing Inspector ENERGY/power details. Each has a configurable per-type profile with modest provisional rates; Battle's baseline is unchanged. Atomic projectile/volley/pulse payments and duration-limited Laser/repair payments reuse the shared service and NoPower recovery. Active replaces idle; no new polling, generator, relay, priority slider or save schema. Existing combat, ammo, heat, construction, upgrades and checkpoint owners are preserved. Source/resource checks only; .NET unavailable, owner Godot validation pending. [Rate table and accounting](../TOWERS/Shared/Power/README.md).


---

## PW4-D — Inspector power feedback (2026-11-05) — SOURCE IMPLEMENTED / AWAITING OWNER VALIDATION

Existing ENERGY bar now has compact actual refill / possible refill / actual consumption / distance / efficiency / state details in the selected Inspector. Typed service samples distinguish connected starvation from disconnection and clear stale rates without an extra per-tower/UI loop. Priority slider remains deferred. Existing allocation, attack funding, balance and save schema are preserved. Source/scene review only; owner Godot layout/runtime check pending. See [power plan](../TOWERS/Economy/Power/IMPLEMENTATION_PLAN.md).

---

## PW4-C revised power allocation (2026-11-05) — SOURCE IMPLEMENTED / AWAITING OWNER VALIDATION

Distance is the **primary allocator** under generator shortages, not the old descending 0–100 priority order. PW4-B sets each connected tower's recharge ceiling by conductive-path distance; PW4-C now additionally gives nearer capacitors **first access to finite island generation**. Far towers can receive zero actual energy until nearer buffers are full or output increases; stored reserves persist. Keep existing PowerPriority metadata dormant. A later optional Inspector slider may only mildly bias the distance preference, requiring separate approval. PW4-D should display real refill versus distance-limited ceiling. Follow [canonical PW4-C update](../TOWERS/Economy/Power/IMPLEMENTATION_PLAN.md). The shared allocator now implements this decision; final Inspector rate/status UI remains PW4-D.

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

> **Updated:** 2026-11-05 · Verified `dev` at [f90010ba7](https://github.com/ben0kay/godot-tower-defense/commit/f90010ba7db42eda2408532692c7cb57cac8b3a3). The November 3 and original October 23 audits remain below as historical context; newer deliveries are summarized at the very top. No main/dev merge implied.
> **Scope:** Status/documentation update only, not gameplay implementation, merge permission or Godot runtime acceptance. Recheck current source and each linked feature README before work. Follow [CodePreferences](CodePreferences.md) and [root README](../README.md).

## Current snapshot (November 5)

- **Delivered in source:** S0–S3 saves; CD1–CD4 construction/upgrade drones; HD1–HD4 heat; shared power PW0–PW4-D and ten powered tower types; Restoration tower L1–L3 with sticky hull repair; disabled tower visual feedback; selected PowerDetails; LM1 shared map scenes; Level011 three checkpoint-guided lanes with Waves 1–5; size-based enemy classes; Stealth Beacon directional scan/tier art; painted mountain/sight blocking; Battle/Machine Gun fire audio, and prior Results R2/stat count-up.
- **Main remaining gameplay:** PW5 relays, PW6 Solar/Fusion, optional RT2 heat and tower shields, advanced enemy EMP/special abilities, research/doctrines/unlocks, the player combat drone, reload/magazine variation, future major visual FX, player options, floating numbers and camera feedback. Map-edge **candidate spawning** exists in SpawnManager; per-wave edge preferences/custom edge-region authoring remain proposed.
- **Content:** Level011 Waves **6–10 are not yet authored** (only 5 exist, 624 scheduled enemies), Capital/Titan classes have no registered ship yet; Wave Editor WP2–WP7 and campaign unlock ladder remain open. Level003 currently contains painted terrain, contrary to older blank-template notes; maintain/verify its routes rather than declaring it entirely blank.
- **Acceptance caveat:** most changes above are source-implemented, with pending owner Godot.NET editor, UI, gameplay, saving and profiling verification. Never infer benchmark FPS or gameplay balance from source review.
- **Branch:** Work on `dev`; do not merge or alter `main` without an explicit release request. The old November 3 ahead/behind figures are historical, not a current branch comparison.

## Suggested next jobs

| Priority | Job | Scope |
| ---: | --- | --- |
| 1 | **PW4 in-game acceptance / PW5-A relay booster** | Short near/far shortage check first; then add only local non-generating distribution anchors. |
| 2 | **PW5-B Power Transmitters** | Separate cross-island building/link, finite merged supply, save identity, basic visible beam and link UI. |
| 3 | **RT2 heat and tower-shield design** | Restoration now repairs hulls, has L2/L3 and consumes real energy; only heat and advanced policy/shields remain. |
| 4 | **Level011 Waves 6–10** | Heavier mixed fleets on existing three lanes; Capital/Titan only after matching enemy assets are designed. |
| 5 | **WP2/WP3/WP4 Wave Editor polish** | Real sprite/stat preview, action button arrangement and useful wave summary. |
| 6 | **PF0/PF3 Level100 measurements** | Profile current 400/500-enemy workload, then choose one evidence-based optimization. |
| 7 | **PW6 Solar/Fusion** | Two distinct production buildings; don't reuse one generator's tier slots for the other. |
| 8 | **Campaign progression / research / Options / feedback** | Scoped independent passes; see ranked table and the remaining-work sections above. |

## A. Ranked work areas — largest to smallest

The original 26-area order is preserved for continuity. **Not every ranked item is still missing:** some are delivered in source and remain listed only for testing or future extensions. Complexity estimates total scope/integration risk, not time per chat. **Codex** = reserve Work/Codex for deeper cross-system work; **Regular** = reasonable for a narrowly scoped normal ChatGPT coding conversation. Some medium items can use either depending on boundaries.

| # | Work area | Complexity | Suggested tool | Status on `dev` / next useful slice |
| ---: | --- | --- | --- | --- |
| 1 | [Campaign and battlefield saving](../SYSTEMS/Saving/README.md) | Very high | **Codex** | **SOURCE DONE (S0–S3), QA OPEN:** one-current-checkpoint between-wave save, profile store, Esc Save/Load and Campaign Continue. Verify resume in Godot, original reward tickets, active builders/ammo. Mid-combat save and multiple slots remain excluded. |
| 2 | [Electricity / island power networks](../TOWERS/Economy/Power/README.md) | Very high | **Codex** | **PW0–PW4-D source implemented / owner validation pending:** cached conducting islands, Citadel/Industrial generation, distance-first finite delivery, per-type capacitors for all nine offensive towers and Repair, NoPower recovery and selected ENERGY/rate feedback. **Remaining:** owner balance/playtesting and PW5-C visual acceptance; future generator variants and separately authorized optional mild priority control. See [power phases](../TOWERS/Economy/Power/IMPLEMENTATION_PLAN.md). |
| 3 | [PF0/PF3–PF5 combat performance](AGENT_FINDINGS/Performance.md) | Very high | **Codex + local measurements** | **PARTIAL / PROFILE NEXT:** PF1 routing/cache fixes and PF2 F4 profiling exist. Reproduce Level100 400/500-enemy CPU/GPU frame timings, then one measured PF4 fix and PF5 validation. |
| 4 | [Research system expansion](../SYSTEMS/Progression/Research/README.md) | Very high | **Codex** | **PARTIAL:** foundation/research starter nodes exist; broader tree, prerequisites, unlocks, doctrines and story branches remain. |
| 5 | [Wave Phase 3: advanced progression](../LEVELS/Waves/README.md) | High | **Codex** | **PARTIAL:** finite manual mixed waves, banners and narrator exist. Editor .tres pilot/WP1 delivered; auto start, history, extras and scripted encounters remain separate work. |
| 6 | [E3B remaining enemy attack/ability types](../ENEMIES/README.md) | High | **Codex** | **PARTIAL:** Gunner Drone weapons and shared hostile bullets implemented; EMP, electrical pulses, support buffs/healing and complex bosses not. |
| 7 | [Tower shields and repair producers](../TOWERS/Shared/Durability/README.md) | High | **Codex** | **PARTIAL:** Restoration now provides aimed/sticky hull healing, 3 purchased tiers and funded PW4 power consumption. **Real tower shields/absorption remain unimplemented**; RT2 heat, ally policy/support XP and RT4 extensions remain separate. |
| 8 | [EG4 armed-enemy stress verification / multi-gun groundwork](../ENEMIES/Weapons/README.md) | High | **Codex + local measurements** | **OPEN QA/EXTENSION:** stress-test realistic gunner counts and eventually design independent Cruiser multi-guns. |
| 9 | [VE5 environmental lighting](../WORLD/BACKGROUNDS/README.md) | High | **Codex** | **NOT IMPLEMENTED:** ambient/local 2D lighting. Do not reinstate user-reverted VE5C bloom. |
| 10 | [VE7 battle-responsive atmosphere](../WORLD/BACKGROUNDS/README.md) | High | **Codex** | **NOT IMPLEMENTED:** battle-responsive event-driven bounded motes/dust. |
| 11 | [Player combat drone ability](../DRONES/Combat/README.md) | High | **Codex** | **CONCEPT ONLY:** offensive player ability drone, not ammo couriers or Citadel construction drones. |
| 12 | [Optional magazines, reloads and ammo variations](../TOWERS/Shared/Ammunition/README.md) | Medium–high | **Codex** | **NOT IMPLEMENTED extension:** reload/magazine/ammo variations; functioning Factory → Depot → Tower pipeline already exists. |
| 13 | [Map-edge spawning](../ENEMIES/Spawning/MapEdges/README.md) | Medium–high | **Codex** | **PARTIAL:** `SpawnManager` already enumerates reachability-filtered permitted map-edge candidates for `SpawnMode.MapEdges/Both` using the live grid; the MapEdges README is historical. Dedicated per-wave edge selectors/weights and authorable edge regions remain future work and need verification. |
| 14 | [VE3 industrial corridor/material accents](../WORLD/BACKGROUNDS/README.md) | Medium–high | **Codex** | **NOT IMPLEMENTED:** VE3 industrial material/foundation accents. |
| 15 | [Organic terrain edges E3](../WORLD/TERRAIN/Edges/IMPLEMENTATION_PLAN.md) | Medium–high | **Codex** | **NOT IMPLEMENTED extension:** E1/E2 atlas and generic mask source are done; E3 rock shapes and material transitions still planned. |
| 16 | [VE6 local environmental FX](../WORLD/BACKGROUNDS/README.md) | Medium | Codex / Regular | **NOT IMPLEMENTED:** opt-in lava shimmer, steam, smoke and vents. |
| 17 | [VE4 regional ambience](../WORLD/BACKGROUNDS/README.md) | Medium | Codex / Regular | **NOT IMPLEMENTED:** regional ambience and local effects. |
| 18 | [R3/R4 results and mission scoring extensions](../UI/Menus/Results/README.md) | Medium | Codex / Regular | **PARTIAL:** R2 presentation, saved XP, bar/level-up summary and **stat count-up on dev** are implemented. Still open: Credits telemetry, additional R3 stats, first-clear/replay/anti-farming, scoring bonuses and richer level-up flair. |
| 19 | [NP3 wave narration follow-through](../LEVELS/Waves/Presentation/README.md) | Medium | **Regular** | **SOURCE DONE / QA:** NP1 wave banners and NP2 tactical narration exist; NP3 regression/viewport/skip/voice follow-through remains. |
| 20 | [CD3 builder-drone UX / desktop QA](../TOWERS/ConstructionDrones/README.md) | Medium | **Regular** | **CD1–CD4 SOURCE DONE / QA:** build/upgrade drones, eased 280px/s flight, cached shader cone and 24Hz scan lines, CV1 holograms. Check overlaps, clipping, 20-build GPU load, tier transitions, mid-upgrade Save/Load. |
| 21 | [Player options/settings](../UI/Menus/Pause/README.md) | Medium | **Regular** | **PARTIAL:** Esc Save and Load implemented under S3; **Options/settings remains a placeholder**. |
| 22 | [Audio and tactical voice event wiring](../AUDIO/README.md) | Medium | **Regular** | **PARTIAL:** welcome-profile cue and actual Battle Tower / Machine Gun Tower positional gunfire exist; most combat/wave voice hooks, ambient content and saved audio settings remain. |
| 23 | [Floating damage numbers](../FEEDBACK/DamageNumbers/README.md) | Small–medium | **Regular** | **NOT IMPLEMENTED:** capped, merged hit/damage numbers. |
| 24 | [Camera shake and impact reactions](../FEEDBACK/CameraEffects/README.md) | Small–medium | **Regular** | **NOT IMPLEMENTED:** bounded camera shake/impact feedback. |
| 25 | More research nodes using **existing** effect kinds | Small–medium | **Regular** | **OPEN content:** author approved research nodes/effects. Homing/precision unlock requires separate mechanic work. |
| 26 | Authored mixed-wave content and balance | Small | **Regular** | **ONGOING CONTENT:** Level003 has painted tile layers and editable waves (verify complete connected routes); Level011 is a painted 192×120 three-lane mission with **five/ten planned waves** and 624 scheduled enemies, requiring balancing and Waves 6–10. |

### Useful scoping notes

- **New gameplay versus acceptance:** PF0/PF3/PF5, EG4, NP3 and parts of CD3 are primarily measurement/QA jobs; source code exists already. They are listed because the plan is not *accepted as fully finished*, not because their foundation is absent.
- **Do not double-count:** E3B overlaps the implemented EG1–EG3 weapon foundation; Results R4 overlaps implemented P1/P2 mission awards; optional homing research builds on implemented PA1–PA3 straight/homing handling.
- **Terrain E2 opt-in:** [DefaultTerrainEdgeConfig.tres](../WORLD/TERRAIN/Edges/DefaultTerrainEdgeConfig.tres) is **disabled by default** pending desktop shader/GPU approval. Its E2 code is already implemented; this is not an E2 coding task.
- **Current Level100 benchmark:** [Level100 README](../LEVELS/Level100/README.md) records **two waves: 400 and 500 ships**, **20 gunners each**, rather than the historical five-wave 100/200/300/400/500 composition.

## A2. Wave Editor backlog — newer than the original audit

The `addons/wave_editor` Godot main-screen plugin and native `.tres` wave resources are implemented in source. **Level001, Level003 and Level011** use editable wave resources; other missions may use existing C# recipes. See [Wave Editor guide](../addons/wave_editor/README.md) and [WP plan](../addons/wave_editor/PLAN.md).

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
- **Level003 map authoring:** mission and four-wave `Level003Waves.tres` exist, and the current scene has painted TileMap layers; check connectivity/actual playable path in Godot rather than relying on the older blank-template README.
- **Results beyond R2:** Credits earned/spent, first-clear/replay policies, anti-farming, mission-time/performance bonuses, enhanced level-up celebration. **Skippable stat count-up is already delivered in `dev`**.
- **Tower artwork completeness:** staged/tier-specific art covers several towers (including Battle, Machine Gun, Blaster, Rocket, Sniper, Cryo, Stasis, Laser, Stealth Beacon and Restoration); inspect remaining types/tiers instead of rebuilding the shared presenter.
- **Enemy hull-damage artwork:** Enemy001 has an implemented optional multi-stage damaged sprite; rolling out to other enemy types is a separate optional art pass, not a new damage system.

## B. Additional unimplemented ideas — not necessarily approved phases

These are ideas or lightweight folder notes. **Do not treat this section as authorization to implement a broad feature** without checking dependencies and desired gameplay:

| Concept | Where it is described | Scope / caution |
| --- | --- | --- |
| [Missile Tower](../TOWERS/Offensive/MissileTower/README.md) | Offensive tower folder | README-only; start with a narrow variant of existing projectile/AoE infrastructure, not a new projectile engine. |
| [Radar Tower](../TOWERS/Support/RadarTower/README.md) | Support folder | Planned; **Stealth Beacon already reveals stealth**. Decide what Radar adds before coding. |
| [Restoration Tower](../TOWERS/Support/RepairTower/README.md) | Support folder | **L1–L3 hull-repair baseline implemented**, with sticky lock, upgrade artwork and funded shared power. **Remaining:** optional RT2 heat, advanced ally targeting/support XP, shields and RT4 extensions. |
| [Scrap Refinery](../TOWERS/Economy/ScrapRefinery/README.md) | Economy folder | Planned raw-resource processing; new resource economy needs separate decisions. |
| [Hybrid Solar Tower](../TOWERS/Economy/SolarTower/README.md) | Old solar concept | Distinguish from planned **pure** Solar Generator to avoid duplicate catalog roles. |
| Tower selling/refunds | [Tower roadmap](../TOWERS/README.md) | Choose transaction/salvage/destruction policy separately. |
| Tower maintenance / operational wear | [Tower roadmap](../TOWERS/README.md) | Different from existing heat and construction; optional per-instance condition. |
| Armor/resistance and more damage interactions | [Combat](../COMBAT/README.md) | Typed shield/hull damage exists; armor mitigation and enemy-specific resistance are not live. |
| Enemy support heal, commander buffs, boss/hybrid, special ship attacks | [Enemy roadmap](../ENEMIES/README.md) | Individual future abilities, not one 'implement everything' pass. |
| Advanced ammo/resource logistics | [Economy roadmap](../TOWERS/Economy/README.md) | Fuel, raw materials, specialized ammo or research throughput are *not* needed for the current working loop. |
| Research doctrine slots, respec, DME story and Zy0n unlocks | [Progression plan](../SYSTEMS/Progression/PLAYER_XP_RESEARCH_FOUNDATION_PASS.md) | Long-term design beyond the small existing research graph. |
| Wave preview/history and auto start | [Waves phase 3](../LEVELS/Waves/README.md) | Could become small independent UI/timing passes instead of one big rewrite. |
| Level authoring convenience | [Authoring](../LEVELS/AUTHORING.md) | **LM1 shared BaseLevelMap inheritance now exists**, with level-local paintable layers for all existing maps including Level011. A richer visual authoring tool remains optional future work. |
| Homing unlock, prediction, terrain line-of-sight | [Projectile plan](../TOWERS/Shared/Projectiles/PROJECTILE_GUIDANCE_ACCURACY_PLAN.md) | Straight shots are now default; explicit homing handling already exists, but **research unlock and leading/LOS are not shipped**. |
| Future multi-genre empire/space shooter | [Root project vision](../README.md) | Deliberately **not** current TD scope; do not extract the game into a speculative cross-mode framework. |

The uploaded childhood GameMaker game notes are historical design inspiration; they are **not** a one-to-one list of missing Godot implementation phases.

## C. Already implemented — don't assign the same work again

**Added since the original October 23 audit (historical source rollup through November 3):** S0–S3 checkpoint saving; CD4 upgrade-service drones, CV1 upgrade holograms, DU1/DU2 disable; cached soft-gradient construction beams and organic travel; Level003 mission plus `.tres` authoring; Wave Editor core/WP1/Layout V2; Results stat count-up; Enemy003 source rename (Shield Emitter, stable `shield_emitter_mk1`), new ship art and Enemy001 damage sprites; authored staged construction/purchased-tier art for several offensive towers; Sniper InstantTarget delivery. See owning feature docs and root README for exact per-tower coverage. **Desktop acceptance remains separate.**


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
| **Power PW4 and Restoration** | Desktop scarce-supply allocation, distant recharge, repair beam and L2/L3 tiers, disable layering, selected fixed-height Inspector and new Save/Load; shared implementation is already authored. |
| **Shared map LM1 / Level011** | Open/move/paint inherited local layers, inspect all three checkpoint routes and 192×120 bounds, ensure active portal list matches wave selectors; profile the first five 624 scheduled enemies across real gameplay. |
| **Tower sight and disabled state** | Verify mountain red masked selection range, blocked acquisition, art tint/exclamation badge restoration and small-window Inspector appearance. |

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

**Codex next:** PW5-A local relay efficiency booster (after focused PW4 QA), PW5-B explicit paired-island links, then PW6 Solar/Fusion; separate RT2 heat/tower shield/research/advanced-enemy passes if approved; PF0/PF3 measured Level100 workload followed by one evidenced optimization. **Do not reimplement PW0–PW4-D, S0–S3 or Restoration L1–L3; fix only demonstrated runtime issues.**  
**Regular ChatGPT next:** Level011 Waves 6–10 when ready, WP2/WP3/WP4 Wave Editor improvements, campaign introduction ladder, floating damage numbers, camera impact reactions, real Options menu, audio cues or a Markdown-only stale-notes cleanup.

For every job: **read root README + CodePreferences + the linked feature README**, verify current HEAD/code, keep scope narrow, make new work opt-in/configurable when sensible, avoid per-frame full-registry scans, run truthful focused tests where possible, update relevant notes and make **one commit per job**. Source-only verification is not an in-editor visual or GPU benchmark.

**Audit scope:** no game assets/C#/.tres/.tscn modified or tested as part of this file's creation. The cleanup flags above are intentionally **not** automatic edits to those other documents.


