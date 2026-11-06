## CW1/CW2 — shared weapon recoil and hostile splash — 2026-11-06

**PLANNED / CODEX HANDOFF; no gameplay code changed.** CW1: extract tower-bound recoil into backwards-compatible shared presentation under `FEEDBACK/Weapons/Recoil/`, opt in Gunner, Heavy Gunner and four Obsidian Dreadnought guns, only after accepted shots; no idle processing or muzzle/aim changes. CW2 (separate later pass): keep existing `COMBAT/AreaDamage.AtDistance` and add `TowerRegistry` splash recipients plus optional enemy bullet impact settings. Preserve ordinary bullets, tower Blaster, balance and missions. [Brief plan](../COMBAT/SHARED_WEAPON_CAPABILITIES_PLAN.md).

---

## SPAWN2.5 — Portal spawn expansion + dimensional smoke — 2026-11-06

**APPROVED PLAN / NOT IMPLEMENTED; Codex before SPAWN3.** Add an event-driven, soft expanding/settling pulse to an Open portal on successful enemy spawn, plus ~4–6 large rolling purple/lavender shader-driven smoke puffs, alongside existing 48 sparks and energy flash. Reuse `PlaySpawnFeedback()`; keep emissions budgeted, offscreen-cullable, pause/unload-safe and independent of SPAWN2's restored 2.2s opening. Do not change wave/portal mechanics or implement SPAWN3/4. [Codex-ready detail](../ENEMIES/Spawning/IMPLEMENTATION_PLAN.md#phase-spawn25--per-spawn-portal-expansion--violet-dimensional-smoke-codex-handoff-approved-plan--not-implemented).

---

## FD1-A — buildable 2×3 Drone Station (2026-11-06)

**SOURCE IMPLEMENTED / AWAITING OWNER VALIDATION.** New `DRONES/Combat/Station/` contains passive `DroneStation` TowerBase type, Inspector config (600 Credits / 8s / 1,800 HP), category Utility registration (`drone_station`), precise `Footprint=Vector2i(2,3)`, original 128×192 twin-bay hangar art, and 0/35/70% staged SVG construction via shared work/drone visuals. Generic placement, select/health/damage/disabled/sell/between-wave tower checkpoint are inherited; no fighter instances, station inventory, combat, docking or patrol yet. **Codex next is FD1-B** (one actual fighter, station ownership and launch/return) and subsequently FD2–FD6. No construction/courier service changes. No Godot runtime acceptance yet. [Station](../DRONES/Combat/Station/README.md) · [Fighter plan](../DRONES/Combat/FIGHTER_DRONES_IMPLEMENTATION_PLAN.md).

---

## FIGHTER — persistent player combat drones + Drone Station (2026-11-06)

**APPROVED DESIGN / NOT IMPLEMENTED.** New [FD0–FD6 fighter-drone plan](../DRONES/Combat/FIGHTER_DRONES_IMPLEMENTATION_PLAN.md): multiple persistent armed friendly drones owned by a buildable Drone Station, **Auto-pilot** nearest-enemy-to-Citadel interception and orbit/figure-eight/kiting, and **Auto-patrol** two-click A/B figure-eight with opportunistic firing. Independent HP/ammo, station return/resupply/repair, selected-drone orders, real ammo logistics and checkpoint-safe state; future enemy anti-air is explicitly a later feature. Fighter-specific scripts/scenes/art/orders/station belong in `DRONES/Combat/`; keep existing Construction and Transport dispatch ownership unchanged. Recommend Codex implement FD0+FD1 first, then FD2/3/4/5/6 as separate passes. No code or balance changes.

---

## SPAWN2 — shared portal materialization — 2026-11-06

**Source Implemented / Awaiting Owner Validation.** Every registered START portal now forms on fresh deployment; Wave N portals use the same Dormant → Forming → Open presentation on their accepted authored wave. No Level011 map or wave data changed. A 2.2s pausable tween drives a localized violet spiral shader, growing segmented ring, art/label reveal, inward GPU curls and concentrated ignition/ripple. No fullscreen effects, audio assets or idle CPU animation loop were added. Existing 120°/s rotation and 48-particle outward per-spawn rim bursts remain the Open-state presentation.

`IsSpawnActive` means legally activated; `IsSpawnReady` additionally requires Open. Required requests retain their selected ship/count and sleep the wave scheduler during formation. A readiness event updates only that source's eligibility slot and wakes the scheduler; random requests ignore unfinished portals without consuming visual RNG. No terrain rebuild, route reassignment or new path queries. Between-wave restore uses explicit checkpoint context (including completed-wave count zero), snaps due portals Open and suppresses opening replay/warnings.

Tune `WORLD/PORTALS/EnemyPortal/Visuals/DefaultPortalMaterializationStyle.tres`: duration 2.2s, optional START stagger 0–0.2s, vortex/ring/flash, reduced motion, particles (72 inward + 24 ignition defaults), and at most 6 simultaneous visible particle pairs. Overflow/offscreen openings use the same timeline with shader/pose only; offscreen exit stops particle emission. Opening nodes hide and tween/budget slots release on completion, reset or unload. Disabled/reduced motion retains mechanical timing with simpler ring/fade and no opening particles. Later-wave activations can request one batched sector banner through the existing alert queue; START never alerts or interrupts mission intel. No suitable approved warp sound was present/wired; future sound may hook opening start/ignition without owning readiness.

**Minimal validation:** focused source/scene-reference and readiness fixture review plus one minimal Godot 4.6.3 headless shader parse (passed); .NET unavailable. Updated `PortalActivationChecks` covers forming exclusion/RNG retention, immediate START wait/resume, late activation, once-only eligibility, no navigation/assignment changes and wave-zero restore without replay. Fixture not executed; visual/FPS acceptance remains with owner. Check fresh multi-portal load, immediate START, pause/resume halfway, later-wave activation, window/camera culling, reduced effects, unload/retry and a fresh checkpoint.

---

## DRONE2 — shared drone body/flight and site UX — 2026-11-06

**Source Implemented / Awaiting Owner Validation.** Replaced ammo's obsolete TransportDrone/procedural art with CargoDrone and the builder SVG/body. Extracted one cubic/eased flight primitive, preserving280px/s builder/default source courier speeds and independent business managers/caps. Added shared retained twin-engine ignition/propulsion and speed-responsive32-point thin path history, capped one-panel-per-site telemetry and Acquiring=5 before the unchanged beam deployment hold gate. Courier optional exact-motion checkpoints and legacy remaining-leg fallback; builder flat fields/old phase numbers preserved. No inventory, dispatch, combat or balance changes. Source/resource/field-parity/diff checks only; .NET unavailable, narrow owner fixtures authored but unrun. [Owner checks](../TESTS/Drones/README.md).

---

## CTX1 — Corruptor and shared DoT — 2026-11-06

**Source Implemented / Awaiting Owner Validation.** Registered graphite/violet Corruptor with three paid tiers/five ranks and normal projectiles, construction stages, power/ammo/heat, selling and checkpoints. Shared Corruption status: hull-only1s ticks, stronger-DPS duration refresh, latest weak source attribution, safe active-only versioned scheduler and Slow/Freeze coexistence. Single-pass unaffected-first / affected fallback is Corruptor-only; Cryo stays strict. Inspector shows initial/periodic/total damage. Source/resource/SVG/diff checks + one art render only; .NET unavailable and focused fixture unrun. Owner: all six priorities, actual DoT/XP/kill flow, pause/lifecycle, art/UI, economy/heat and fresh save/load.

---

## SPAWN1 startup repair and RT2 audit — 2026-11-06

**Source Implemented / Awaiting Owner Validation.** Removed stale registrations for absent Level001 Portal2 and Level020 Portal05 (SPAWN1 had promoted previous warnings into startup SETUP ERROR). Normalized Restoration resource script-before-Power assignment; guarded post-heal/heat callback removal and bounded heat to delivered hull. Corrected power-aware repair fixtures and Level001 activation fixture setup. Source/resource/diff checks only; .NET unavailable. Owner: start Level001/020 waves; confirm funded Restoration repair, overheat/EMP recovery and heat/energy save/load. No tower costs, rates, heat timings or wave content changed.

---

# Implementation Backlog — Live Status (2026-11-06, `dev`)

> **Authority:** audited `ben0kay/godot-tower-defense` on `dev` at [`2fa1d5f0c9`](https://github.com/ben0kay/godot-tower-defense/commit/2fa1d5f0c9f8380e6a5f35fc5567185bd75d3876) against the root README, `NOTES/CodePreferences.md`, latest commits and feature-owned notes. **`main` is stable; never write/merge to main unless asked.** This document is a planning/status change only, not authorization to implement all proposals.
>
> **Status vocabulary:** **Source Implemented / Awaiting Owner Validation** means checked-in source/assets and possibly unrun fixtures; **Planned** means an owning plan describes the phase, not that code exists; **Idea / Needs Plan** means design remains to be agreed; **Accepted** only after owner confirmation in Godot. Newer source and feature notes override older dated receipts below. Keep performance work and large Godot playtests with the owner by default.

## November 6 source-delivered changes — do not schedule as missing code

| Area | Current source status | Remaining boundary / owner acceptance |
| --- | --- | --- |
| **Power PW0–PW6-A/B** | Island accounting, distance-first finite distribution, per-tower capacitors, NoPower, selected ENERGY metrics; Industrial, local **PW5-A Relay**, paired **PW5-B Transmitter**, **PW5-C coverage/link presentation**, plus **PW6-A Solar** (1×1, 75/s) and **PW6-B Fusion** (4×4, 900/s). Shared power consumers include all nine offensive types and Restoration. | **PW6-C** generator/network Inspector status and measured balance remain planned. Verify source loss, shared supply, 4×4 conductive footprint, link/relay visuals, brownouts and fresh checkpoint saves. **Optional PowerPriority slider NOT APPROVED**; distance remains primary. [Power plan](../TOWERS/Economy/Power/IMPLEMENTATION_PLAN.md). |
| **Restoration RT0–RT2** | Friendly tower hull targeting, sticky aimed repair beam, purchased L1–L3 artwork/stats, paid capacitor use, **RT2 continuous heat** (3 min sustained funded repair, 10 sec recovery to 35%). | Godot QA for repair, power + heat + EMP overlapping disables, upgraded art, save/load; **RT3** optional friend targeting/support XP and **RT4** shield/multi-target/material extensions still separate decisions. [Repair plan](../TOWERS/Support/RepairTower/IMPLEMENTATION_PLAN.md). |
| **Tower selling TS1** | Selected Inspector red SELL beside UPGRADE, confirmation, **floor(actual paid Credits × up-to-50% fraction × remaining hull ratio)**, tracked paid build/upgrade costs, once-only sale, proper owner cleanup and checkpoint persistence. | Godot acceptance for damaged/repaired/under-construction/upgrading towers, old saves with unknown paid history, wallet limits, multi-cell footprints, stale confirms and power/logistics cleanup. **Do not reimplement basic selling.** [TS1 notes](../TOWERS/Shared/Selling/README.md). |
| **Spawning SPAWN0/1 and portal feedback** | Registered portal identity/route contract; `AppearAtWave=0` START or authored Wave N; reserved invisible future portals, eligibility and setup validation, saving seam, F3 author preview, **120°/s rotation** plus capped purple `GPUParticles2D` bursts on spawn. | **SPAWN2 source implemented / owner QA open** for shared materialization and pausable readiness; **SPAWN3** mixed portal+edges/weighted sectors, **SPAWN4** concurrent timed groups, **SPAWN5** optional scripted events remain planned. Godot validation of early Wave N visibility and frame cost pending. [SPAWN0–5 plan](../ENEMIES/Spawning/IMPLEMENTATION_PLAN.md). |
| **Enemy roster and weapons** | **16 catalog ships** including 352×208 **Obsidian Dreadnought**, Capital / Champion, **13,500 hull HP** and four independent red-tracer gun mounts. Added unoccupied **Colossal** size class. F2 now larger and sorts **lowest Rank first within each Class**. Fleet artwork repolished, including latest Gunner/Heavy Gunner, Shield Emitter/Frigate and Elite Assault. | EG4 large armed-fleet/4-gun frame-time checks; independent Cruiser weapons, new Colossal/Titan types, Capital boss phases and campaign deployment are **future** work, not absent four-gun infrastructure. [Capital](../ENEMIES/Types/ObsidianDreadnought/README.md), [roster](../ENEMIES/Types/ROSTER.md). |
| **Terrain and combat sight** | Shared `WorldGrid.HasWeaponLineOfSight` applies mountains to **friendly aimed towers and hostile gun mounts**, including Capital muzzle checks; selected blocked-range sectors marked red. | Verify movement/occlusion, reacquisition and edge-of-mountain scenarios in Godot. **Physical bullet-terrain collision is not claimed by this change**; do not treat the old blanket missing-LOS note as current. [Grid](../WORLD/GRID/README.md). |
| **Audio / visuals** | Positional Battle and Machine Gun per-shot audio, **new Cryo firing WAV**, profile greeting; upgraded fleet charcoal/red SVG treatment and previous construction, selection, tier and world-callout visuals. | Audio mix/polyphony, further clips/voice, portal GPU budgets, runtime/import/scale acceptance pending. [Audio](../AUDIO/README.md). |
| **Levels, UI and saves** | S0–S3 between-wave checkpoints and Continue; LM1 shared local-paintable maps; Level011 **192×120, three checkpoint routes, Waves 1–5 totaling 624 scheduled ships**, Results XP/stat count-up, Wave Editor core/WP1/Layout V2. | Level011 **Waves 6–10** still unauthored; verify 3 routes and current ship availability. WP2–WP7, Results telemetry/XP policy, campaign unlock progression and editor acceptance remain open. |

## Remaining defined implementation phases (Codex vs regular ChatGPT)

| Priority | Plan / scope | Status | Best fit |
| ---: | --- | --- | --- |
| 1 | [**SPAWN2** all-portals cinematic materialization](../ENEMIES/Spawning/IMPLEMENTATION_PLAN.md): **all START portals warp in together on fresh map load**, and every Wave N portal uses the same swirl/ring/particle/flash sequence when activated. Distinguish Dormant/Forming/Open, pause and source readiness; checkpoint restore snaps already-open portals to idle. Existing rotation/spawn sparks are separate. | **SOURCE IMPLEMENTED / OWNER QA OPEN** | **Codex** |
| 2 | [**PW6-C** generator/network inspection](../TOWERS/Economy/Power/IMPLEMENTATION_PLAN.md): clearer selected source/supply/demand/efficiency and owner-guided rebalance, without new power architecture. | Planned; tune after playtest | Regular / Codex |
| 3 | [**Level011 Waves 6–10**](../LEVELS/Level011/README.md): finish authored escalating three-lane encounters; optionally add existing Capital only when explicitly chosen for wave balance. | Content not authored | **Regular** |
| 4 | [**WP2–WP7 Wave Editor**](../LEVELS/Waves/README.md): sprite/stats preview, controls, composition summary, renumber, Save As and ship search. Keep separate phases. | Planned, WP1 done | **Regular** for WP2–4; Codex if broad |
| 5 | [**SPAWN3** mixed sources and sectors](../ENEMIES/Spawning/IMPLEMENTATION_PLAN.md): opt-in true portal/edge mix, edge weights/ranges and editor validation; existing MapEdges/Both fallback remains. | Planned | **Codex** |
| 6 | [**SPAWN4** parallel attack groups](../ENEMIES/Spawning/IMPLEMENTATION_PLAN.md): deterministic separately timed wave batches sharing one bounded scheduler. | Planned | **Codex** |
| 7 | [**SPAWN5** scripted surprise encounters](../ENEMIES/Spawning/IMPLEMENTATION_PLAN.md): one-shot trigger/lifetime policies only after SPAWN1–4 acceptance. | Concept in phased plan; not automatic approval | **Codex** |
| 8 | [**PF0/PF3–PF5 + EG4**](AGENT_FINDINGS/Performance.md): measured Level100 400/500-ship and realistic armed/Capital CPU/GPU baselines, then one evidence-backed fix. | Profiling/QA, not missing architecture | **Codex + owner FPS captures** |
| 9 | [**Tower shields / durability**](../TOWERS/Shared/Durability/README.md): real tower shield absorption/recharge, separate from existing hull repair and enemy shield system. | Needs dedicated approved design | **Codex** |
| 10 | [**RT3/RT4** Restoration extensions](../TOWERS/Support/RepairTower/IMPLEMENTATION_PLAN.md): optional ally policy/support XP; shield/mobile-target/nanite logistics later. **RT2 is done.** | Plan/choices; separate authorization | Regular / Codex |
| 11 | [**E3 terrain**](../WORLD/TERRAIN/Edges/IMPLEMENTATION_PLAN.md) and [**VE3–VE7 atmosphere**](../WORLD/BACKGROUNDS/README.md): material transitions, local ambience/FX, lighting, event-driven battle atmosphere. | Planned separately; E1/E2, VE1/VE2 exist | Codex or small Regular slices |
| 12 | [**Wave Phase 3**](../LEVELS/Waves/README.md): optional countdown/auto-start, wave preview/history/bonuses and scripted events; coordinate with SPAWN4/5 rather than making two schedulers. | Partly scoped; current manual scheduler works | Codex |
| 13 | [**Advanced enemies**](../ENEMIES/README.md): EMP/electrical disruption, support healing/buffs, jamming, advanced bosses; reuse existing enemy weapons/abilities. | Concept/feature plans vary | Codex |
| 14 | [**Research + campaign progression**](../SYSTEMS/Progression/Research/README.md): effect families, prerequisites, tower/mission availability, doctrine slots, new rewards and replay policy. | Foundation only; needs slices | Codex |
| 15 | [**R3/R4 Results**](../UI/Menus/Results/README.md): actual earned/spent Credits, first-clear/replay and bonus policy, further level-up visual celebration. Count-up already done. | Partial | Regular / Codex |
| 16 | [**Options, audio, damage numbers, camera feedback**](../UI/Menus/Pause/README.md): small separable menu/presentation tasks; see owning [FEEDBACK](../FEEDBACK/README.md) and [AUDIO](../AUDIO/README.md). | Planned/ideas; no duplicate existing visuals | **Regular** |
| 17 | [**Magazines/reloads**](../TOWERS/Shared/Ammunition/README.md), [persistent Fighter Drones / Drone Station](../DRONES/Combat/FIGHTER_DRONES_IMPLEMENTATION_PLAN.md), advanced logistics and new towers. | **FIGHTER FD0–FD6 designed, not implemented**; magazine/reload/logistics extensions remain separate | **Codex (fighter)** |

**Defer / do not silently implement:** the saved but inactive `PowerPriority` (0–100) slider, VE5C bloom (deliberately reverted), mid-combat/multi-slot saves, speculative cross-genre architecture and any fresh enemy difficulty/content balance without owner direction.

## Owner validation queue — NOT unfinished engine passes

| Verify in Godot (owner-led, lightweight AI checks unless requested) | Source already delivered |
| --- | --- |
| **SPAWN0/1:** future portal hidden/reserved before Wave N, appears once in numbered wave, correct route/F3 preview; optional slow rotation and purply spawn particles at wave pressure. | Existing portal scenes, new activation and shader/GPUParticles resources; fixtures authored, not executed in this environment. |
| **TS1:** 50% maximum health-scaled sales, stale modal/reentrant action, build/upgrade costs, wallet cap, selling a generator/relay/active repair tower, checkpoint restore. | Shared selling/Inspector and paid-history saving. |
| **PW4/5/6:** 1×1 Solar, 4×4 Fusion, Industrial/Citadel production, linked islands, Relay boost, distances, outages and new Save/Load. | Power network and generator resources; do not imply measured energy/FPS from source. |
| **RT2:** continuous funded healing → heat/overheat → recovery, stacked EMP/NoPower, tiers L1–L3, save/reload. | RT0–RT2 source; no broad Godot regression claimed. |
| **Capital / terrain sight:** four muzzle origins, near/far tower visibility behind mountains, bullet lifetime when blocked, hit visuals and 16-entry F2 rank order, high-load FPS. | Shared gun/mountain sight and catalog. |
| **Prior changes:** S0–S3 saves, Level011 and three routes, Wave Editor WP1/layout, results count-up, NP3 narrator, CD3/CD4 visuals, VE1/2/E2 opt-in, PA4 projectile handling and audio/polyphony. | Source implementations and historical verification notes below. |

## Backlog maintenance rule

**Read this live index first**, then the relevant feature README/plan and current `dev` source. Move shipped tasks from **Planned** to **Source Implemented / Awaiting Owner Validation**, never automatically to **Accepted**. Keep the [idea intake](#b2-ideas-without-an-approved-detailed-implementation-plan) separate from approved phase plans. Log new requests here with an owner, a smallest next slice and a status; large new mechanics require a feature-local plan before implementation. Historical dated entries following this index are evidence, **not additional live work orders**.

---

## Dated implementation receipts (historical; superseded by live index)

## RT2 — Restoration Tower heat — 2026-11-06

**Source Implemented / Awaiting Owner Validation.** The existing Repair Tower's power funding was already live; the remaining RT2 heat integration now uses shared continuous-beam timing, actual repaired/energy-funded seconds, overheat disable and automatic cooling. Default Heat preset: 3 minutes nonstop repair to overheat, 10 seconds to cool to 35%; configurable in `DefaultRepairTowerConfig.tres`. Existing Inspector HEAT meter and saves apply. No power-generation, target-policy, XP or other tower changes. Verify heat during real repairs, pauses/full HP, energy exhaustion, overheat/EMP and upgrade/reload in Godot.

---

## TS1 — Health-based tower selling

**Source Implemented / Awaiting Owner Validation.**

Shared paid-cost history, health-based refund calculation, guarded modal sale, existing lifecycle removal, compact side-by-side Inspector actions and checkpoint persistence are source implemented. Owner acceptance: inspect normal/maximum/passive towers, construction/upgrades, damage/repair and stale confirmation; double-click, wallet cap, multi-cell rebuilding, power/logistics/repair cleanup and save/load. No balance/combat/wave changes.

---

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

## Historical index handoff (November 5 snapshot superseded)

The former November 5 current-status/suggested-jobs list is replaced by the **November 6 live index at the top**. In particular, PW5-A/B/C, PW6-A/B, RT2, TS1 and SPAWN0/1 are **already source implemented**, and the Obsidian Capital ship is registered. The 26-area cross-reference below is retained with refreshed status for navigation, not as a second source of truth. No dev/main merge or gameplay acceptance is implied.

## A. Ranked work areas — largest to smallest

The original 26-area order is preserved for continuity. **Not every ranked item is still missing:** some are delivered in source and remain listed only for testing or future extensions. Complexity estimates total scope/integration risk, not time per chat. **Codex** = reserve Work/Codex for deeper cross-system work; **Regular** = reasonable for a narrowly scoped normal ChatGPT coding conversation. Some medium items can use either depending on boundaries.

| # | Work area | Complexity | Suggested tool | Status on `dev` / next useful slice |
| ---: | --- | --- | --- | --- |
| 1 | [Campaign and battlefield saving](../SYSTEMS/Saving/README.md) | Very high | **Codex** | **SOURCE DONE (S0–S3), QA OPEN:** one-current-checkpoint between-wave save, profile store, Esc Save/Load and Campaign Continue. Verify resume in Godot, original reward tickets, active builders/ammo. Mid-combat save and multiple slots remain excluded. |
| 2 | [Electricity / island power networks](../TOWERS/Economy/Power/README.md) | Very high | **Codex** | **PW0–PW6-A/B SOURCE IMPLEMENTED / QA OPEN:** Citadel, Industrial, Solar (1×1) and Fusion (4×4), distance-first capacitors, Relay/Transmitter linking and PW5-C visuals. **PW6-C** source/demand Inspector and balance still planned; optional priority slider inactive/unapproved. |
| 3 | [PF0/PF3–PF5 combat performance](AGENT_FINDINGS/Performance.md) | Very high | **Codex + local measurements** | **PARTIAL / PROFILE NEXT:** PF1 routing/cache fixes and PF2 F4 profiling exist. Reproduce Level100 400/500-enemy CPU/GPU frame timings, then one measured PF4 fix and PF5 validation. |
| 4 | [Research system expansion](../SYSTEMS/Progression/Research/README.md) | Very high | **Codex** | **PARTIAL:** foundation/research starter nodes exist; broader tree, prerequisites, unlocks, doctrines and story branches remain. |
| 5 | [Wave Phase 3: advanced progression](../LEVELS/Waves/README.md) | High | **Codex** | **PARTIAL:** finite manual mixed waves, banners and narrator exist. Editor .tres pilot/WP1 delivered; auto start, history, extras and scripted encounters remain separate work. |
| 6 | [E3B remaining enemy attack/ability types](../ENEMIES/README.md) | High | **Codex** | **PARTIAL:** Existing Gunner/Heavy Gunner and four-gun Obsidian Capital reuse shared weapons/hostile bullets; EMP, electrical pulses, support buffs/healing and phase-based bosses remain open. |
| 7 | [Tower shields and repair producers](../TOWERS/Shared/Durability/README.md) | High | **Codex** | **PARTIAL:** Restoration L1–L3 hull healing, capacitor funding and **RT2 heat now source implemented**; tower shield absorption/regeneration does not exist. RT3 targeting/support XP and RT4 advanced targets remain separate design. |
| 8 | [EG4 armed-enemy stress verification / multi-gun groundwork](../ENEMIES/Weapons/README.md) | High | **Codex + owner metrics** | **OPEN PROFILE/EXTENSION:** four independent gun mounts exist on Obsidian; measure mixed armed groups and bullet renderer on real CPU/GPU. Further independently armed Cruiser types are optional new content, not missing common mounts. |
| 9 | [VE5 environmental lighting](../WORLD/BACKGROUNDS/README.md) | High | **Codex** | **NOT IMPLEMENTED:** ambient/local 2D lighting. Do not reinstate user-reverted VE5C bloom. |
| 10 | [VE7 battle-responsive atmosphere](../WORLD/BACKGROUNDS/README.md) | High | **Codex** | **NOT IMPLEMENTED:** battle-responsive event-driven bounded motes/dust. |
| 11 | [Persistent Fighter Drones + Drone Station](../DRONES/Combat/FIGHTER_DRONES_IMPLEMENTATION_PLAN.md) | High | **Codex** | **PLANNED FD0–FD6 / NOT IMPLEMENTED:** new player-owned squad, selectable Auto-pilot (Citadel urgency pursuit) and two-point Auto-patrol (figure-eight), finite HP/ammo, station service/relaunch and future explicit anti-air integration. Historical one-off player ability remains separate. `DRONES/Combat/` owns the new feature; existing builders/couriers stay independent. |
| 12 | [Optional magazines, reloads and ammo variations](../TOWERS/Shared/Ammunition/README.md) | Medium–high | **Codex** | **NOT IMPLEMENTED extension:** reload/magazine/ammo variations; functioning Factory → Depot → Tower pipeline already exists. |
| 13 | [Enemy spawning and dynamic portal plan](../ENEMIES/Spawning/IMPLEMENTATION_PLAN.md) | Medium–high to high | **Regular SPAWN2; Codex SPAWN3–5** | **SPAWN0/1 SOURCE IMPLEMENTED / QA OPEN:** authored START/Wave N hidden reserved portals, setup eligibility, route checks, F3, subtle rotating shader and capped purple spawn particles. **SPAWN2 SOURCE IMPLEMENTED / QA OPEN:** shared one-shot appearance/readiness. **SPAWN3** real mixed edges/sectors, **SPAWN4** concurrent groups, **SPAWN5** optional scripted triggers remain. |
| 14 | [VE3 industrial corridor/material accents](../WORLD/BACKGROUNDS/README.md) | Medium–high | **Codex** | **NOT IMPLEMENTED:** VE3 industrial material/foundation accents. |
| 15 | [Organic terrain edges E3](../WORLD/TERRAIN/Edges/IMPLEMENTATION_PLAN.md) | Medium–high | **Codex** | **NOT IMPLEMENTED extension:** E1/E2 atlas and generic mask source are done; E3 rock shapes and material transitions still planned. |
| 16 | [VE6 local environmental FX](../WORLD/BACKGROUNDS/README.md) | Medium | Codex / Regular | **NOT IMPLEMENTED:** opt-in lava shimmer, steam, smoke and vents. |
| 17 | [VE4 regional ambience](../WORLD/BACKGROUNDS/README.md) | Medium | Codex / Regular | **NOT IMPLEMENTED:** regional ambience and local effects. |
| 18 | [R3/R4 results and mission scoring extensions](../UI/Menus/Results/README.md) | Medium | Codex / Regular | **PARTIAL:** R2 presentation, saved XP, bar/level-up summary and **stat count-up on dev** are implemented. Still open: Credits telemetry, additional R3 stats, first-clear/replay/anti-farming, scoring bonuses and richer level-up flair. |
| 19 | [NP3 wave narration follow-through](../LEVELS/Waves/Presentation/README.md) | Medium | **Regular** | **SOURCE DONE / QA:** NP1 wave banners and NP2 tactical narration exist; NP3 regression/viewport/skip/voice follow-through remains. |
| 20 | [CD3 builder-drone UX / desktop QA](../TOWERS/ConstructionDrones/README.md) | Medium | **Regular** | **CD1–CD4 SOURCE DONE / QA:** build/upgrade drones, eased 280px/s flight, cached shader cone and 24Hz scan lines, CV1 holograms. Check overlaps, clipping, 20-build GPU load, tier transitions, mid-upgrade Save/Load. |
| 21 | [Player options/settings](../UI/Menus/Pause/README.md) | Medium | **Regular** | **PARTIAL:** Esc Save and Load implemented under S3; **Options/settings remains a placeholder**. |
| 22 | [Audio and tactical voice event wiring](../AUDIO/README.md) | Medium | **Regular** | **PARTIAL:** profile greeting and positional **Battle, Machine Gun and Cryo Tower** fire clips now shipped. Enemy/other towers, impact/voice/wave events, ambience and sound settings remain open; owner mix/polyphony QA. |
| 23 | [Floating damage numbers](../FEEDBACK/DamageNumbers/README.md) | Small–medium | **Regular** | **NOT IMPLEMENTED:** capped, merged hit/damage numbers. |
| 24 | [Camera shake and impact reactions](../FEEDBACK/CameraEffects/README.md) | Small–medium | **Regular** | **NOT IMPLEMENTED:** bounded camera shake/impact feedback. |
| 25 | More research nodes using **existing** effect kinds | Small–medium | **Regular** | **OPEN content:** author approved research nodes/effects. Homing/precision unlock requires separate mechanic work. |
| 26 | Authored mixed-wave content and balance | Small | **Regular** | **CONTENT OPEN:** Level003 has painted wave/map resources; Level011 three-lane 192×120 map has only Waves 1–5 (624 scheduled). **Waves 6–10** not authored; Obsidian Capital is now a registered option but not yet inserted into existing wave resources. |

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
| [Restoration Tower](../TOWERS/Support/RepairTower/README.md) | Support folder | **RT0–RT2, L1–L3 and power are source implemented**. Remaining: optional RT3 ally target policy/support XP, RT4 multi-target/shield/drone/material ideas and Godot acceptance. |
| [Scrap Refinery](../TOWERS/Economy/ScrapRefinery/README.md) | Economy folder | Planned raw-resource processing; new resource economy needs separate decisions. |
| [Hybrid Solar Tower](../TOWERS/Economy/SolarTower/README.md) | Historical hybrid concept | **PW6-A standalone Solar Generator is already implemented** under Economy/Power/SolarGenerator. Only an intentionally different hybrid role would warrant a new type/plan; no duplication. |
| Tower selling/refunds | [Shared TS1 selling](../TOWERS/Shared/Selling/README.md) | **TS1 source implemented**: actual paid-cost × max 50% × remaining hull ratio, Inspector sale/confirm, save and full cleanup. Future salvage/modifiers require a separate design. |
| Tower maintenance / operational wear | [Tower roadmap](../TOWERS/README.md) | Different from existing heat and construction; optional per-instance condition. |
| Armor/resistance and more damage interactions | [Combat](../COMBAT/README.md) | Typed shield/hull damage exists; armor mitigation and enemy-specific resistance are not live. |
| Enemy support heal, commander buffs, boss/hybrid, special ship attacks | [Enemy roadmap](../ENEMIES/README.md) | Individual future abilities, not one 'implement everything' pass. |
| Advanced ammo/resource logistics | [Economy roadmap](../TOWERS/Economy/README.md) | Fuel, raw materials, specialized ammo or research throughput are *not* needed for the current working loop. |
| Research doctrine slots, respec, DME story and Zy0n unlocks | [Progression plan](../SYSTEMS/Progression/PLAYER_XP_RESEARCH_FOUNDATION_PASS.md) | Long-term design beyond the small existing research graph. |
| Wave preview/history and auto start | [Waves phase 3](../LEVELS/Waves/README.md) | Could become small independent UI/timing passes instead of one big rewrite. |
| Level authoring convenience | [Authoring](../LEVELS/AUTHORING.md) | **LM1 shared BaseLevelMap inheritance now exists**, with level-local paintable layers for all existing maps including Level011. A richer visual authoring tool remains optional future work. |
| Homing unlock, prediction, terrain line-of-sight | [Projectile plan](../TOWERS/Shared/Projectiles/PROJECTILE_GUIDANCE_ACCURACY_PLAN.md) and [Grid](../WORLD/GRID/README.md) | **Shared mountain sight for friendly towers and enemy guns is implemented**. Physical projectile-versus-terrain contact, optional homing research unlock and predictive leading are distinct unimplemented ideas; avoid duplicating LOS. |
| Future multi-genre empire/space shooter | [Root project vision](../README.md) | Deliberately **not** current TD scope; do not extract the game into a speculative cross-mode framework. |

The uploaded childhood GameMaker game notes are historical design inspiration; they are **not** a one-to-one list of missing Godot implementation phases.

## B2. Ideas without an approved detailed implementation plan

This is **intake only**, not implementation permission. The user-authored [Ideas.md](Ideas.md) is the original list; the status below reconciles it with `dev` source so an agent does **not rebuild existing foundations**. A concept can have a folder README without a **scoped, approved implementation phase**. Suggested next slices are **proposals requiring owner approval**, not finished designs.

| Idea ID | Feature / existing baseline | Planning status / smallest next decision | Likely owner |
| --- | --- | --- | --- |
| **F01** | Hit reactions: kinetic sparks (HI1) and shield ricochets exist. | **Partial / Needs plan:** shield ripple and explosive impact families, caps/per-hit aggregation; no duplicated HI1 renderer. | FEEDBACK + COMBAT · Regular |
| **F02** | Floating damage numbers. | **Needs detailed pass:** shield/hull/critical values, short-window merging, size and pooled draw caps. Existing [feature folder](../FEEDBACK/DamageNumbers/README.md). | FEEDBACK · Regular |
| **F03** | Upgrade/rank celebrations: aqua LEVEL and gold RANK world callouts exist. | **Partial / optional:** extra pulse/glow only after reviewing the shipped [WorldCallouts](../FEEDBACK/WorldCallouts/README.md). | FEEDBACK · Regular |
| **F04** | Better enemy death visuals and boss wreckage. | **Idea / Needs plan:** small fragment budget vs large staged explosion; purely cosmetic and cleanup-safe. | ENEMIES + FEEDBACK · Regular / Codex |
| **F05** | Dynamic elite/boss wave announcements. | **Partial:** NP1 banners/NP2 narrator exist; define elite/boss variants without a second alert queue. | LEVELS/Waves/Presentation · Regular |
| **F06** | Heat/HP/disabled tower visual state. | **Partial:** heat/disabled indicator exists; decide barrel glow, vents and sparking independent of real disables. | FEEDBACK + TOWERS · Regular |
| **F07** | Kill-streak combo HUD. | **Idea / Needs plan:** cosmetic-only time window and decay, wave-owned kill source, no reward changes. | UI + FEEDBACK · Regular |
| **F08** | Citadel impact reaction. | **Partial:** critical health warning exists; define single hit flash/ripple, capped shake and suppression. | FEEDBACK + Citadel · Regular |
| **F09** | Selected-tower target line / reticle. | **Partial:** selected range/callout exists; design opt-in current-target-only visual that sleeps unselected. | FEEDBACK/Selection · Regular |
| **F10** | Context event feed. | **Partial:** warnings/narrator/callouts exist; decide de-duplication, priorities, max queue and event ownership. | UI/Feedback · Regular |
| **F11** | Tower efficiency telemetry in Inspector. | **Idea / Needs plan:** DPS, uptime, kills, shield damage; bounded samples not per-shot UI redraws. | TOWERS + UI · Codex |
| **F12** | Additional sound layers. | **Partial:** Battle/Machine Gun/Cryo positional clips exist; prioritize shield break, armour hit, rank-up and wave cues. | AUDIO + feature assets · Regular |
| **F13** | Portal activation visuals. | **Source Implemented / Awaiting Owner Validation:** shared START/Wave N vortex opening, readiness gate, restore/no-replay, 120°/s idle spin and outward rim bursts. | WORLD/PORTALS · Owner QA |
| **F14** | Construction completion burst. | **Partial:** CV1/CD3 construction visuals and pooled flashes exist; audit appearance before authoring more. | TOWERS/ConstructionDrones · Regular |
| **F15** | Post-wave compact summary. | **Idea / Needs plan:** confirmed wave counters, rewards and best tower without conflicting with mission Results. | LEVELS + UI · Regular |
| **M01** | Tower synergies and adjacency. | **Idea / Needs plan:** choose **one** synergy; distinguish actual power Relay boosts from tower-to-tower combat buffs. | TOWERS/Shared · Codex |
| **M02** | Enemy support fleets. | **Partial:** Shield Emitter/Frigate role exists; plan repair, commander speed buff or jammer as one separate opt-in ability. | ENEMIES/Abilities · Codex |
| **M03** | Player battlefield abilities. | **Idea / Needs plan:** pick one (orbital strike/repair/shield/overcharge), define cost/cooldown and save semantics; combat drone concept is related. | DRONES + UI · Codex |
| **M04** | Branching tower upgrades. | **Idea / Needs plan:** one tower, a choice at paid tier, data/save compatibility; keep earned rank separate. | TOWERS/Shared/Upgrades · Codex |
| **M05** | Carriers/splitting ships. | **Partial:** DeathSpawn on destruction already works. Mid-flight fighter launch and additional split rules need a separate ability plan. | ENEMIES/Abilities · Codex |
| **M06** | Multi-phase bosses. | **Idea / Needs plan:** Capital Obsidian has guns/HP but **no health-threshold phases**. Choose state triggers and effects for a future boss. | ENEMIES/Abilities · Codex |
| **M07** | Enemy damage-type vulnerabilities. | **Idea / Needs plan:** typed damage/shield-vs-hull exist; add per-type mitigation only with designed balance and one damage authority. | COMBAT + ENEMIES · Codex |
| **M08** | Tower target priorities. | **Implemented baseline:** six existing modes; do not reimplement. Specialist filters are a separate extension needing agreement. | TOWERS/Shared/Targeting · only if extended |
| **M09** | Ammo supply drones. | **Implemented baseline:** Factory → Depot → Tower already delivers. Magazines, specialty rounds and deeper logistics are separate planned extensions. | TOWERS/Shared/Ammunition · only if extended |
| **M10** | Random mission modifiers. | **Idea / Needs plan:** one deterministic, clearly previewed modifier and no hidden mutation of base enemy definitions. | LEVELS + SYSTEMS · Codex |
| **M11** | Research loadout slots. | **Idea / Needs plan:** active slots, purchases and save contract; existing research foundation is not a doctrine loadout. | SYSTEMS/Progression · Codex |
| **M12** | Salvage drones. | **Idea / Needs plan:** debris ownership, collectible credit caps and differences from existing construction/ammo drone fleets. | ECONOMY + DRONES · Codex |
| **M13** | Environment hazards. | **Idea / Needs plan:** start with one authored solar-storm/radiation/asteroid mechanic and explicit effects; not merely cosmetic VE4–7. | WORLD + COMBAT · Codex |
| **M14** | Active tower abilities. | **Idea / Needs plan:** select one tower, explicit cooldown/trigger and supported save state, not a generic ability framework first. | TOWERS + UI · Codex |
| **M15** | Enemy formations. | **Idea / Needs plan:** formation leader/follow spacing and route/checkpoint ownership; coordinate with SPAWN4. | ENEMIES/Movement · Codex |
| **M16** | Endless survival. | **Idea / Needs plan:** opt-in separate mode/wave ramp, reward policy and save/score boundaries; retain finite authored campaigns. | LEVELS + SYSTEMS · Codex |

### Additional unplanned decisions not to lose

| Gap | Current boundary | Suggested first deliverable (proposal only) |
| --- | --- | --- |
| **Tower shield absorption** | Hull repair and enemy shields exist; no dedicated approved tower-shield phase. | One shield stat/damage interception policy + capped feedback and save semantics; agree regeneration/balance first. |
| **Physical enemy-bullet vs mountain impact** | New shared LOS blocks *acquisition/fire* behind mountains; enemy-bullet physics were not changed by that pass. | Decide whether already-fired hostile shots should clip on terrain and what happens if target/shooter moves; then scope a shared collision profile. |
| **Capital/Titan/Colossal roster expansion** | One Capital exists; Colossal/Titan classes are empty. | One specifically designed new enemy and classification/art/weapon contract, no bulk placeholders. |
| **Campaign unlock ladder** | Missions, player XP and research foundation work; unlock ordering not selected. | One small level/tower unlock sequence with stable IDs, replay and save policy. |
| **Results Credits and replay policy** | Existing tally placeholders / baseline mission XP. | Credits earned/spent telemetry first; choose first-clear and repeat rewards separately. |
| **Tower wear/maintenance** | Heat and disabling exist; no operational condition degradation. | Choose whether feature is genuinely needed; don't build universal nuisance deterioration from the old GameMaker notes automatically. |
| **Map editor / wave authoring usability** | LM1 inherited map and WP1 Wave Editor work. | Finish WP2–WP7 first; only then consider larger visual editors or procedural mission authoring. |
| **Post-TS1 salvage/research bonuses** | Health-scaled selling already works and credits paid are tracked. | Specify any economy upgrade or salvage drone benefit without double-refunding paid value. |

**Intake policy:** Keep F/M IDs in [Ideas.md](Ideas.md); this section maps them to actual source and the next approval decision. Create a feature-owned phased plan **only when selected**, with smallest scope, affected systems, Inspector config, performance lifecycle and owner acceptance; do not label it `Approved` in advance.

## C. Already implemented — don't assign the same work again

**November 6 source rollup:** PW5-A/B/C Relay/Transmitter/visuals; PW6-A/B Solar/Fusion; RT2 heat; TS1 health-based selling and paid-cost save history; SPAWN0/1 authored wave portals with shader rotation and bounded spawn particles; 16-ship roster with four-gun Obsidian Capital (13,500 HP), Colossal classification, F2 rank sorting; shared friendly/hostile mountain LOS; Cryo Tower per-shot sound; additional flat charcoal/red fleet SVG polishing. **Owner runtime/visual/performance QA remains pending.**

**Added since the original October 23 audit (historical source rollup through November 3):** S0–S3 checkpoint saving; CD4 upgrade-service drones, CV1 upgrade holograms, DU1/DU2 disable; cached soft-gradient construction beams and organic travel; Level003 mission plus `.tres` authoring; Wave Editor core/WP1/Layout V2; Results stat count-up; Enemy003 source rename (Shield Emitter, stable `shield_emitter_mk1`), new ship art and Enemy001 damage sprites; authored staged construction/purchased-tier art for several offensive towers; Sniper InstantTarget delivery. See owning feature docs and root README for exact per-tower coverage. **Desktop acceptance remains separate.**


- **HD4 authored heat presets (2026-10-26):** Machine Gun (2min), Laser (3.5min), Blaster Cannon/Rocket (4min), Shockwave (3min), each with a modest independent recovery setting; shared beam/pulse timing, Inspector opt-in and source-level focused checks are implemented. Battle remains unchanged; desktop Godot acceptance pending. [Conditions](../TOWERS/Shared/Conditions/README.md).
This list describes **source delivery**, not universal desktop approval.

- **Tower/combat:** IT1–IT4 Inspector/6 target modes; CT1–CT3 Cryo; ST1 Stasis; HD1–HD3 generic disable/heat/Inspector; existing tower HP/damage/destruction; PA1–PA3 straight/projectile spread/homing contract; PV tracer/muzzle effects; Battle plus six offensive recoil rollouts; SR1–SR4 cosmetic shield ricochets.
- **World/enemies:** GE1–GE3 inherited enemy scenes; E2A/E2B catalogs/ShieldEmitter; E3A DeathSpawn; N4 corridor movement; MC1–MC3 multiple Citadels/HUD; SE1 stealth/Stealth Beacon; EG1–EG3 TowerRegistry/hostile bullets/Gunner Drone; shared route pool and segment-safety cache.
- **Levels/economy/UI:** Data-driven mission catalog/shared battlefield; Level002 tutorial/Level020 Seven-Wave Five Gates; Level100 two-wave stress shortcut; CP1–CP3 Credits economy and Resource Tower/ARG; Factory → Depot → Tower ammunition drones; CD1/CD2 Citadel builder; F2 multi-batch enemy debug with 1:1 previews; PF2 performance instrumentation; NP1 banners and NP2 narrator source; R2 Results vector/XP animation source; P1/P2 mission XP and P3 Levels/Research hub foundations.
- **Visuals:** VE1 space background/twinkle/nebula and VE2 portal/Citadel energy visuals; E1 atlas repair and E2 reusable rendered organic edge masks.

## D. Remaining verification and measurement (not missing architecture)

**November 6 addition:** see the **Owner validation queue** near the top for TS1, PW6-A/B, RT2, SPAWN0/1, Capital/EG4, terrain sight, Cryo WAV and latest fleet art. No long-running Godot benchmark was executed by this documentation update; code shipped elsewhere does not imply runtime acceptance.

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


