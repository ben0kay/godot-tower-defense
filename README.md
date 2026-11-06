## CW1 — shared recoil source implemented — 2026-11-06
Motion/style now live under `FEEDBACK/Weapons/Recoil/`; existing tower classes, preset paths and behaviour remain compatible adapters. Gunner, Heavy Gunner and four Capital cannons have independent accepted-shot recoil. Only gun art moves; fixed physical muzzle/aim and bullet cadence remain unchanged. Source review only; owner validation pending. CW2 remains planned.

---


## SPAWN2.5 — portal spawn swell and violet smoke
**Source Implemented / Awaiting Owner Validation.** Successful Open-portal spawns trigger a 12% soft artwork pulse and six large billowing violet puffs, alongside existing sparks and energy flash. Retained/cullable GPU emission and one coalesced cooldown keep dense traffic bounded. Opening stays 2.2s; waves/readiness/navigation unchanged. [Settings](WORLD/PORTALS/EnemyPortal/README.md).

---

## FD1-B + FD2 — Drone Station and Auto-pilot
**Source implemented / awaiting owner validation.** The [2×3 Utility Drone Station](TOWERS/Utility/DroneStation/README.md) owns paid fighters with launch/recall, persistent HP/ammo and checkpoints. Fighters intercept Citadel threats using shared budgeted remaining-journey ranking, weave/orbit/kite nearby and guard home between waves. [Settings and boundaries](DRONES/Combat/README.md). Patrol UI, weapons and service remain future work.

---

## Automatic wave progression and live victory hold — 2026-11-06

**Source Implemented / Awaiting Owner Validation.** Wave 1 remains manual; subsequent waves auto-start 30 seconds after the previous wave fully clears, with per-wave overrides in the Wave Editor and an upper-right AUTO START countdown beside the still-available NEXT WAVE button. Pause/checkpoint Continue retain the countdown. Early start, outcomes, reset and unload cancel it safely. Victory keeps the map/drones/effects alive during the existing 4-second delay, while the official stat snapshot stays fixed; the map pauses when Results opens. Defeat remains immediate. Source/lifecycle and authored-fixture review only; .NET unavailable, owner visual/gameplay acceptance pending. See LEVELS/Waves/README.md and LEVELS/Session/README.md.

---



## Drone travel, site widget and portal pacing — 2026-11-06

**Source Implemented / Awaiting Owner Validation.** Shared builder/ammo flights use fixed 800px/s² acceleration and braking with real cruise time, preserving curves and source speeds. Construction/upgrade widgets are another 10% smaller (0.72 scale), closer upper-right and fade in over 0.25s as the builder approaches within 140px. Portal opening defaults to 1.1s, and Level011 Wave 6 leads with one existing Portal4 ship rather than waiting for a shuffled request; total 216 ships / Portal4 26 preserved. Flight snapshots append optional profile data and retain legacy active legs. Source review and numerical flight/quota checks only; .NET unavailable, owner gameplay/visual acceptance pending. Details: DRONES/Shared/README.md, TOWERS/Shared/Construction/Visuals/README.md and WORLD/PORTALS/EnemyPortal/README.md.

---

## Power-capacitor resource-order fix — 2026-11-06

**Source fixed; gameplay acceptance pending.** The Sniper and several other offensive towers had authored `Power` resources but did not show ENERGY or pay for shots. Eight `Default*Config.tres` resources placed `Power` before `script`; the working Battle/Corruptor resources had the correct `script`-first order. Reordered only the script/power assignment lines in Sniper, Machine Gun, Rocket, Laser, Cryo, Stasis, Shockwave and Blaster Cannon. Underlying shared power/payment/energy values and balance unchanged. Test **fresh** deployments after reimport, especially disconnected Sniper and Laser. [Power guide](TOWERS/Shared/Power/README.md).

---

## SPAWN2 — shared portal materialization — 2026-11-06

**Source Implemented / Awaiting Owner Validation.** Every registered START portal now forms on fresh deployment; Wave N portals use the same Dormant → Forming → Open presentation on their accepted authored wave. No Level011 map or wave data changed. A 2.2s pausable tween drives a localized violet spiral shader, growing segmented ring, art/label reveal, inward GPU curls and concentrated ignition/ripple. No fullscreen effects, audio assets or idle CPU animation loop were added. Existing 120°/s rotation and 48-particle outward per-spawn rim bursts remain the Open-state presentation.

`IsSpawnActive` means legally activated; `IsSpawnReady` additionally requires Open. Required requests retain their selected ship/count and sleep the wave scheduler during formation. A readiness event updates only that source's eligibility slot and wakes the scheduler; random requests ignore unfinished portals without consuming visual RNG. No terrain rebuild, route reassignment or new path queries. Between-wave restore uses explicit checkpoint context (including completed-wave count zero), snaps due portals Open and suppresses opening replay/warnings.

Tune `WORLD/PORTALS/EnemyPortal/Visuals/DefaultPortalMaterializationStyle.tres`: duration 2.2s, optional START stagger 0–0.2s, vortex/ring/flash, reduced motion, particles (72 inward + 24 ignition defaults), and at most 6 simultaneous visible particle pairs. Overflow/offscreen openings use the same timeline with shader/pose only; offscreen exit stops particle emission. Opening nodes hide and tween/budget slots release on completion, reset or unload. Disabled/reduced motion retains mechanical timing with simpler ring/fade and no opening particles. Later-wave activations can request one batched sector banner through the existing alert queue; START never alerts or interrupts mission intel. No suitable approved warp sound was present/wired; future sound may hook opening start/ignition without owning readiness.

**Minimal validation:** focused source/scene-reference and readiness fixture review plus one minimal Godot 4.6.3 headless shader parse (passed); .NET unavailable. Updated `PortalActivationChecks` covers forming exclusion/RNG retention, immediate START wait/resume, late activation, once-only eligibility, no navigation/assignment changes and wave-zero restore without replay. Fixture not executed; visual/FPS acceptance remains with owner. Check fresh multi-portal load, immediate START, pause/resume halfway, later-wave activation, window/camera culling, reduced effects, unload/retry and a fresh checkpoint.

---

## Level011 Portal4 wave-6 debut and opening wave pacing — 2026-11-06

**Source Implemented / Awaiting Owner Gameplay Validation.** Level011's existing fourth portal (`AppearAtWave = 6`) is now registered in `ActivePortals`, and 26 scheduled Wave 6 enemies are routed through it (4 Reinforced Drones, 10 Elites, 12 Heavy Gunners across existing batches). Waves 1–5 retain their exact enemy counts/compositions but have 20% shorter spawn intervals and smaller inter-batch gaps for quicker playtesting. Waves 7–10, the map, portal placement and enemy stats are unchanged. [Level011 details](LEVELS/Level011/README.md).

---

## Construction percentage widget and portal feedback polish — 2026-11-06

**Source Implemented / Awaiting Owner Visual Validation.** Construction telemetry is percentage-only, uniformly 20% smaller, anchored upper-right of the footprint; successful completion counts to 100% then fades. Inbound/locked/online labels and job caption are removed. Portal spin defaults to 120 degrees/sec; 48 brighter/larger GPU sparks spawn from explicit 64-point rim geometry and travel radially outward. Existing event ownership, work/gameplay rules, shared bounded drawing, 750ms burst cap and culling remain. Source/resource sanity review only; .NET unavailable, owner visual/FPS checks pending. See [construction visuals](TOWERS/Shared/Construction/Visuals/README.md) and [portal feedback](WORLD/PORTALS/EnemyPortal/README.md).

---

## Level001 companion voice synchronized with mission intel — 2026-11-06

**Source implemented / owner audio acceptance pending.** The newly pushed `LEVELS/Level001/Level001Briefing.mp3` now registers as `voice_mission_level001_intro` and plays through the existing `VoiceAssistant` on a fresh Level001 load. The *same upper-right narrator* begins typing when voice playback starts and does not fade until after the clip ends (plus its usual reading hold). Camera movement/building/START WAVE remain available; early wave starts do not clear active voiced subtitles, and save-Continue skips replay. Voice is optional per mission through `MissionBriefingDefinition.VoiceCueId`. [Briefing documentation](LEVELS/Briefing/README.md).---

## DRONE2 — unified builder/ammunition drone presentation

**Source Implemented / Awaiting Owner Validation.** Construction and ammo couriers now share the cyan drone body, cubic/eased flight, twin thruster ignition and thin speed-responsive actual-path ion trails. Construction adds a short SITE LOCKED cue before the existing beam deployment and one compact shared site telemetry readout through SYSTEM ONLINE. Construction and ammo managers/caps/stock/work remain separate; authored speeds and balances are preserved. Save motion/phase compatibility and bounded active-only presentation are source implemented. .NET unavailable; owner gameplay/visual/FPS and fresh checkpoint checks pending. [Details](DRONES/README.md).

---

## SPAWN2 planning update — all-portals dimensional warp-in (2026-11-06)

The [SPAWN2 Codex handoff](ENEMIES/Spawning/IMPLEMENTATION_PLAN.md#phase-spawn2--shared-cinematic-dimensional-warp-in-for-all-portals-codex-implementation-specification) now specifies a **shared, cinematic violet swirly portal materialization** with inward curling GPU particles, growing ring, bright breach pulse and settling idle portal. **Every START (AppearAtWave=0) portal animates on fresh level deployment**, while portals due at Wave N play the same effect on their activation wave. Add a safe pausable Forming/Ready gate so no enemy can emerge from an unformed portal; between-wave checkpoint restore must not replay old appearances. Reuse existing SPAWN1 route reservations, idle rotation and short purple spawn bursts. **Historical design brief: now source implemented as described above; owner acceptance pending.**

---

## Per-level mission intel — shared upper-right typewriter (2026-11-06)

**Source implemented / owner visual acceptance pending.** Level001 now presents optional Command Intelligence text when a fresh mission finishes loading using the **same top-right typewriter narrator** as wave transmissions. No centred popup, gameplay pause or input blocking. START WAVE remains manual and replaces/clears the intro when accepted. Other levels opt in through `LevelConfig.MissionBriefing`; checkpoint Continue skips it. Voice cue metadata is reserved for a future female AI narrator but no voice plays yet. [Mission briefing details](LEVELS/Briefing/README.md).

---

## Level011 — Ten-wave fleet campaign (2026-11-06)

**Source Implemented / Awaiting Owner Validation.** Level011 now authors all ten waves in its editable `Level011Waves.tres`: waves 6–9 steadily raise Heavy/Superheavy, shield, carrier and gunship pressure; wave 10 opens with one Obsidian Dreadnought from the middle portal, followed by 264 staged support ships from all three lanes. Waves 1–5 and the map/portals remain unchanged. 1,825 ships are scheduled before possible carrier releases; owner Godot balance/FPS validation pending. [Wave details](LEVELS/Level011/README.md).

---

## RT2 — Restoration Tower shared heat — 2026-11-06

**Source Implemented / Awaiting Owner Validation.** Restoration Tower now uses the existing continuous-beam heat/overheat/cooling system, configurable in `TOWERS/Support/RepairTower/DefaultRepairTowerConfig.tres` (**3 minutes** sustained funded healing, **10 seconds** to recover to **35% heat**). Heat accrues only for actual successful hull repair, not idle aiming or unfunded seconds; the existing HEAT bar, disable handling, source-owned timer and checkpoint state are reused. No new power service, repair balance or save schema. [Repair details](TOWERS/Support/RepairTower/README.md). Owner Godot verification pending.

---

## PW6-A/B — Solar and Fusion generators — 2026-11-06

**Source Implemented / Awaiting Owner Validation.** Ctrl → Economy registers Solar Generator (1×1, 75 power/sec) and Fusion Reactor (4×4, 900 power/sec). Both reuse Industrial Generator's single shared producer, construction, power-network and checkpoint systems, with independent 0/35/70% SVG build stages and finished visuals. Balance is provisional and Inspector-configurable; no fuel, solar time-of-day or new distribution service. [Power implementation plan](TOWERS/Economy/Power/IMPLEMENTATION_PLAN.md). Godot editor acceptance is pending.

---

## Mountains block weapon vision on both sides — 2026-11-06

**Source Implemented / Awaiting Owner Validation.** Friendly towers and enemy guns now share `WorldGrid.HasWeaponLineOfSight` over the existing terrain cache. Enemy acquisition skips hidden towers; blocked locks stop aiming/firing, and every Capital gun checks from its own muzzle. Existing range/scoring, terrain metadata, navigation, weapon balance, saves and projectile collision are preserved. Narrow source checks only; .NET unavailable, Godot gameplay verification pending. [Shared terrain vision](WORLD/GRID/README.md).

---

## Obsidian Dreadnought and larger ranked F2 picker — 2026-11-05

**Source Implemented / Awaiting Owner Validation.** The default enemy catalog now has 16 ships, including its first **Capital / Champion / Fighter**, `obsidian_dreadnought_mk1`. Native 352×208 black/charcoal armour with sleek crimson accents, baked vector lighting, four reinforced weapon sockets and a separate native 24×72 rotating cannon sprite. Four independent existing EnemyMinigun mounts sit at front-left/front-right/rear-left/rear-right positions (112,−62), (112,62), (−104,−62), (−104,62), each with its own aiming/target/cooldown state and exact barrel-tip Marker2D (0,−32).

**Current balance (3× hull buff):** 13,500 HP (was 4,500)., 22px/s, 1.5 hull turn speed, 20 Citadel arrival damage, 100 kill Credits, no default shield and 2.5 death-explosion multiplier. Each gun uses the Heavy Gunner baseline: 275px range from its pivot, 5 Kinetic damage, 2.5 shots/s, 650px/s unguided red tapered 14.6×2.9 tracers, 2.8s lifetime, accepted-shot muzzle flash. Four guns can fire independently; nominal aligned total is 10 bullets/s / 50 DPS. Dedicated Capital config/visual resources allow later balance tuning without changing either Gunner variant. Existing spatial tower queries, staggered acquisition, shared bounded hostile bullet renderer/collision, stasis/pause/host removal and generic damage/status/navigation/XP/Credits systems remain authoritative. The ship intentionally keeps the existing large-enemy **24px navigation clearance** rather than changing map/path rules to match its much larger cosmetic hull. No special boss AI, wave insertion, new weapon system or autoload.

**F2:** 1,000px minimum panel, 560px default left picker column, 40px selector and 17px selected text; popup uses selector width and a viewport-bounded scrollable height (620px default). `EnemySpawnMenu` exposes picker width/height. Existing class headings remain, smallest to largest; rows show **Name — Rank**, ordered within each class by ascending rank, then name and stable ID. Header placeholders preserve exact scene/preview/spawn mapping. Hover preview still copies only native sprite art, now including all four Capital guns. Batch caps, timing, portals, shields/stealth flags and authored wave ownership are unchanged. Capital is available for F2 and future Wave Editor authoring; **no existing wave resource changed**.

**Minimal validation:** source/resource/SVG XML review and a local Inkscape hull+four-gun composite render inspected. Focused existing F2 and gunship fixture expectations updated for rank order/labels, 16 catalog ships and Capital composition; C# fixtures **not run** because .NET is unavailable. No Godot gameplay, weapon/balance or performance acceptance claim. Owner: rebuild, open F2 → CAPITAL → Obsidian Dreadnought — Champion, spawn one, and inspect native size, slow turning, four independently aimed guns/barrel origins, red tracers, stasis/death cleanup and lower-rank-first menu ordering. Keep owner wave balance unchanged until separately requested.


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

[Full details and owner checks](TOWERS/Economy/Power/IMPLEMENTATION_PLAN.md).

---

## Shared power rollout to offensive towers and Repair — 2026-11-05

**Source Implemented / Awaiting Owner Validation.** All nine offensive types plus Restoration Tower now use PW4-A–D capacitors and the existing Inspector ENERGY/power details. Each has a configurable per-type profile with modest provisional rates; Battle's baseline is unchanged. Atomic projectile/volley/pulse payments and duration-limited Laser/repair payments reuse the shared service and NoPower recovery. Active replaces idle; no new polling, generator, relay, priority slider or save schema. Existing combat, ammo, heat, construction, upgrades and checkpoint owners are preserved. Source/resource checks only; .NET unavailable, owner Godot validation pending. [Rate table and accounting](TOWERS/Shared/Power/README.md).


---

## Heavy Gunner slower, heavier red ammunition — 2026-11-05

The Heavy Gunner's dedicated minigun now fires a roughly **33% longer/wider red tapered projectile** (14.6×2.9px, previously 11×2.2), travelling at **650px/s** (was 850). Its firing rate is **2.5/s** (was 3.5/s) with **5 Kinetic damage** per bullet (was 4). The starter Gunner and its bullet style are unchanged, as are the Heavy Gunner's hull, movement speed, 260 HP, 275px range, muzzle flash and 2.8s bullet lifetime. Focused test expectations updated; Godot visual verification pending. [Heavy Gunner notes](ENEMIES/Types/HeavyGunnerDrone/README.md).

---

## Heavy Gunner Drone — new armed enemy (2026-11-05)

Added `heavy_gunner_drone_mk1` as a separate **Standard/Veteran/Fighter** enemy (the current size-based classification) with a 62×42 armoured SVG hull, independent 18×50 long-barrel cannon and 260 HP at 52px/s. Dedicated weapon config uses 4-damage Kinetic shots at 3.5/s, slower 850px/s travel, 275px range and larger **red 11×2.2 tapered tracers**. The original lightweight starter Gunner and its bullets remain completely unchanged. The new enemy is appended to the 15-entry default catalog (available in F2/Wave Editor); no authored waves were changed. Focused catalog/weapon checks and roster docs updated. **Source checks only; Godot owner validation pending.** [Heavy Gunner details](ENEMIES/Types/HeavyGunnerDrone/README.md).

---

## Stealth Beacon slower scan, three coverage tiers and split upgrade art — 2026-11-05

Stealth Beacon Level 1 now revolves every 4.5s (was 3s). Paid Level 2/3 retain that slower speed but expand genuine effective cone coverage to 80°/90° and radius to 280/304px, with +5% Max HP per tier, tier-specific Inspector previews and distinct native SVG fixed-base/rotating-head artwork. The existing 0/35/70% initial construction visuals are preserved; paid upgrades have new 35/70% stage composites. Core stealth indexing, rotation pause/disable handling and gameplay combat remain unchanged. [Stealth Beacon details](TOWERS/Utility/StealthBeacon/README.md). Source implemented; Godot validation pending.

---

## Lightweight Gunner Drone — second visual refinement (2026-11-05)

The starter Gunner's hull SVG is now **48×30** (down from 52×38), with a slimmer small-fighter silhouette; its separate gun SVG is now **12×44** (down from 20px wide and up from 40px long), using a long visible barrel and compact breech. The gun muzzle is still physically at the existing local marker **(0,-18)**, without any scene or weapon changes. **135 HP**, 18px navigation radius, projectile visuals and all firing/movement settings remain unchanged. [Gunner artwork notes](ENEMIES/Types/GunnerDrone/Artwork/README.md). Source checks only; owner visual validation pending.

---

## Machine Gun Tower one-shot firing clip — 2026-11-05

**Source Implemented / Awaiting Owner Validation.** Imported the owner's short machine-gun hit as `TOWERS/Offensive/MachineGunTower/Sounds/MachineGunFire.mp3`, trimmed/faded to remove near-silence, and registered `tower_machine_gun_fire`. The existing shared projectile turret emits a separate positional audio request for **each paid bullet**, not a loop or one sound per target/volley; zero per-ID cooldown preserves 14.4–15.36 bullets/s, subject to existing global polyphony/culling budgets. Other tower audio and damage/ammunition/heat remain unchanged. See [Machine Gun audio notes](TOWERS/Offensive/MachineGunTower/Sounds/README.md). Godot audio QA pending.

---

## PW4-B — cached distance-limited recharge — 2026-11-05

**Source Implemented / Awaiting Owner Validation.** Existing power manager now owns one coalesced multi-source conductive BFS; valid Citadel/generator footprints anchor delivery. Shared Inspector curve: 4 full cells, 12 soft falloff cells, 10% reachable-route floor. Recharge ceiling = nominal refill × efficiency; no route means zero. Capacity/drain/attack funding/source output and stored charge are unchanged; no new consumers, priority scheduling or relays. Cached consumer APIs expose distance/efficiency/ceiling; final derived cache is rebuilt before checkpoint unpause without new saved fields. Performance: reused arrays, no per-tower pathfinding/frame flood fill, no distance rebuild for positive output or curve-only edits. Narrow fixture updated; source/resource review only (environment unavailable), no build/runtime/benchmark claim. [Power guide](TOWERS/Economy/Power/Shared/README.md).

---

## RT1 — playable Restoration Tower baseline — 2026-11-05

**Source Implemented / Awaiting Owner Validation.** New `repair` Support tower: 350 Credits, 5s, 1000 HP, 1×1, 320px, 16 hull HP/s, aimed white/silver/aqua turret and shared muzzle-origin beam. RT0 registry targeting, reusable scratch/cached lock, ordinary RestoreHealth, construction, selection and checkpoint owners reused. Added optional appended RepairStats with finite clamp/final forwarding and minimal correctly labelled Inspector rows. Enemy targeting, other tower balance and waves unchanged. **RT1 has no heat/power operating cost; RT2–RT4 remain planned.** Focused fixture authored; source/resource review only (environment unavailable), no build/runtime/visual/benchmark claim. [Repair guide](TOWERS/Support/RepairTower/README.md).

---

## SE2 — directional Stealth Beacon scanning — 2026-11-05

**Source Implemented / Awaiting Owner Validation.** Prepared head now rotates clockwise and existing ScanCone path owns one faint sector; real 70° / 256px cone, 3s revolution, 0.1s sampled detection, alpha 0.08. Existing world reveal index/source identities are reused; capped nearby queries update stationary enemies, retained members do not flicker and independent overlaps remain revealed. Pause freezes scanning; disabled/unfinished/destroyed/removed sources sleep/hide/release. Source/config edits and index teardown retain cleanup. Art/scene paths, 250 Credits, 1800 HP, 2s build, waves and other mechanics are unchanged; no power/upgrade/bubble. Focused stealth checks updated; source review only (.NET unavailable), owner build/runtime/visual validation pending. [SE2 record](TOWERS/Utility/StealthBeacon/DIRECTIONAL_SCAN_IMPLEMENTATION_PLAN.md).

---

## Stealth Beacon split radar-scanner artwork and SE2 handoff — 2026-11-05

Stealth Beacon now has fixed base + independently rotatable head SVGs, a matching composite catalog icon and initial 0/35/70% construction artwork, wired through the shared split-layer presenter. The existing reveal mechanic **still reveals all stealthed enemies in radius**; real 70° rotating-cone detection and a faint swept wedge are designed in [SE2 Codex handoff](TOWERS/Utility/StealthBeacon/DIRECTIONAL_SCAN_IMPLEMENTATION_PLAN.md) but **not implemented** in this artwork pass. Scanner head and cone placeholder are prepared for Codex. Prices, 2-second construction, 1×1 footprint, 1800 HP and 256px range are unchanged. Source checks only; Godot visual validation pending.

---

## Gunner Drone compact starter-ship revision — 2026-11-04

The Gunner enemy now uses a compact **52×38** swept-fighter silhouette (formerly 64×48), a **20×40** minigun image narrowed 20% (formerly 24×40), and **135 max HP** (formerly 180, exactly 25% less). Movement/corridor radius was reduced 22→18 px to fit the smaller ship. Its separate rotating gun, barrel-tip muzzle marker, original projectile look/damage/cadence, 75px/s movement, catalog ID, loot and classification remain unchanged. Existing focused test expectations and enemy roster documentation were updated; no runtime tests run. See [Gunner details](ENEMIES/Types/GunnerDrone/README.md).

---

## Power PW3 — buildable Industrial Generator — 2026-11-04

Economy now offers Industrial Generator: provisional **250 power, 300 Credits, 5s construction, 1200 HP, 2×2**. Shared generator code uses existing construction/art, disable, selection and checkpoint lifecycle without a frame-processing loop. No consumers/outages yet. The selected-tower power bar matching heat/ammo is recorded for PW4. See [Industrial guide](TOWERS/Economy/Power/IndustrialGenerator/README.md). Source checks only; owner Godot validation pending.

---

## Power PW2 — Citadel starter generation and accounting — 2026-11-04

Citadels now contribute configurable starter power (100 provisional) to their own islands. Shared event-driven accounting and F4 show supply/requested demand without disabling towers; no consumers or generators are added yet. Edit `WORLD/OBJECTIVES/Citadel/DefaultCitadelPower.tres`. See [power details](TOWERS/Economy/Power/Shared/README.md). Source/resource review only; Godot/.NET build/runtime validation pending.

---

## Power PW0/PW1 — 2026-11-04

**PW0 audit complete; PW1 source implemented / owner Godot validation pending.** Cached four-neighbour power islands now come from independent `conducts_power` terrain metadata, with no per-frame manager work or rebuilds on tower occupancy. F4 reports topology/hovered island and rebuild timing. Tower operation remains unchanged; Citadel baseline output is planned for PW2. See [phased implementation plan](TOWERS/Economy/Power/IMPLEMENTATION_PLAN.md) and [focused checks](TESTS/Power/README.md). Generation, consumption, shortages and relays are not yet implemented.

---

## Enemy Class, Rank and Role — 2026-11-04

All 14 enemy definitions now use independent descriptive Class/Rank/Role metadata. Wave Editor sorts by Class with separate metadata details; F2 keeps catalog order and matching 1:1 previews. Stats, abilities, art, IDs, paths and waves are unchanged. See [roster/migration notes](ENEMIES/Types/ROSTER.md), including external custom-resource and checkpoint-fingerprint caveats. Source implemented; owner Godot validation pending.

---

## Ammunition Factory construction and future upgrade artwork — 2026-11-03

The existing single stationary 3×2 Ammunition Factory has full 0/35/70% initial building stage SVGs, powered by the shared event-driven construction presenter. Nine new SVGs also include future-only Level 2/3 completed designs and intermediate 35/70% upgrade artwork; **factory paid upgrades remain unimplemented** (no economic data or purchase flow was invented). The original icon, 200-Credit/8s build, ammunition production, logistics, drones and save data are unchanged. Config resource ordering follows the Resource Tower fix from `3ae0e25`. [Factory artwork notes](TOWERS/Economy/Ammunition/AmmunitionFactory/Artwork/Construction/README.md). Source checks done; Godot visual acceptance pending.

---

## Selected-tower tactical overlay — 2026-11-03

Placed-tower selection now adds a stronger translucent aqua tactical screen wash and grid, the selected tower's name and state anchored in world space beside its right edge, and one softly pulsing holographic frame padded beyond its footprint. Disabled/upgrading towers switch the palette to red using existing state signals. The shared renderer is dormant when unselected, preserves the existing radius/Inspector and supports multi-cell buildings. See [Selection Feedback](FEEDBACK/Selection/README.md). Source implemented; owner visual QA pending.

---

## Resource Tower construction and purchased upgrade artwork — 2026-11-03

Resource Tower now has staged initial assembly at 0/35/70% and separate finished Level 2 and 3 green industrial artwork with 35%/70% intermediate upgrade images. It remains **one stationary 112×112 Sprite2D** on a 2×2 footprint. Shared `TowerConstructionVisuals` controls cosmetic texture changes; no new tower logic or processing loops were added. The producer's credit payout rates, storage capacities, existing prices, 2-second work durations, collection flow and save data are unaffected. See [Resource artwork notes](TOWERS/Economy/ResourceTower/Artwork/Upgrades/README.md). Godot visual acceptance pending.

---

## Shared world callouts for tower levels and ranks — 2026-11-03

Finished paid upgrades now display a thick-arrow **aqua LEVEL** popup; XP promotions display a **gold RANK UP** popup to the right of the tower. Both use a generic `FEEDBACK/WorldCallouts/` animated text component suitable for future plain status notices (for example OVERHEATED), with Inspector-editable duration, rise and font size. Dormant effects do not process. Newer `dev` timed upgrade drones and combat are unchanged. [World Callouts](FEEDBACK/WorldCallouts/README.md). Source implemented; owner Godot visuals/compile review pending.

---

## Shockwave Tower construction and purchased artwork — 2026-11-03

Shockwave now has 0/35/70% initial construction SVGs and a distinct Level 2 completed purple resonator plus 35%/70% upgrade scaffolds. The pre-existing stationary **single Sprite2D** remains the only tower art, with its texture controlled by shared `TowerConstructionVisuals`. No turret, muzzle, gameplay logic, cooldown, wave effect, resource balance or per-frame presentation work was added. Level 1 build remains 5 seconds; the Level 2 upgrade remains 225 Credits / 6 seconds. See [Shockwave artwork notes](TOWERS/Offensive/ShockwaveTower/Artwork/Upgrades/README.md). Source wiring checked; owner Godot visual acceptance pending.

---

## Level001 gentle introductory waves and Wave Editor narration — 2026-11-03

Replaced the first mission's previous shield-heavy stress-test waves with **three gentle tutorial encounters (10 / 14 / 20, total 44)**. Waves 1–2 contain only Standard Drones; Wave 3 spawns ten Standards before its second, ten-ship weighted batch introduces five faster Scouts mixed with five Standards. No shields, stealth, Bulwarks or support enemies. `Level001Config.tres.WaveSource` now points to the new `LEVELS/Level001/Level001Waves.tres`, editable through Godot's Wave Editor so the user can write **banner/commentary/narration** later. The presentation fields remain blank. Terrain, tower balance, money, mission progression and shared runtime unchanged. [Level001 design](LEVELS/Level001/README.md).

---

## Level003 Wave 4 — integrated Bulwark escorts (2026-11-03)

Wave 4 now interleaves eight stronger Bulwarks with the twelve Standard and twelve Scout ships across **two faster mixed batches** (8/4/4 and 4/8/4), instead of sending the eight Bulwarks alone at the end. The 0.55s/0.50s spawn intervals and 0.4s between batches make the finale denser without increasing the 32-ship total or changing waves 1–3. Authoritative source: `LEVELS/Level003/Level003Waves.tres` (Wave Editor). C# baseline remains a historical test reference; focused wave assertions updated. [Level003 details](LEVELS/Level003/README.md).

---

## Laser Tower fixed-base and rotating-emitter correction — 2026-11-03

Laser Tower now separates the original silver base from its independently rotating gold energy emitter, consistent with other turret-style offensive towers. Finished artwork is split for Levels 1, 2 and 3; all original initial construction and 35%/70% purchased upgrade composites remain in use. The shared `TowerConstructionVisuals` presenter switches between the temporary construction sprite and two finished layers; selection tint now applies consistently. The `TurretPivot/Muzzle=(0,-29)` path, beam endpoint logic, costs, level stats and work timing remain unchanged. See [Laser split artwork notes](TOWERS/Offensive/LaserTower/Artwork/Upgrades/README.md). Godot visual acceptance pending.

---

## Laser Tower purchased Level 3 — 2026-11-03

Extended Laser from two to **three total purchased levels**: new Level 3 costs 250 Credits, takes 6 seconds, delivers 16 beam DPS at 345.4 px range, and has 1102.5 max HP. Added new finished L3 silver/yellow emitter art and 35%/70% assembly textures to the existing `LaserConstructionArt.tres` LevelArtwork, retaining its single rotating sprite, 0/35/70% initial build stages, L2 artwork, muzzle (0,-29), beam behaviour and existing shield/ammo/heat rules. See [Laser upgrade art](TOWERS/Offensive/LaserTower/Artwork/Upgrades/README.md). Source checks passed; live Godot check pending.

---

## Laser Tower staged construction and Level 2 artwork — 2026-11-03

Laser Tower now has three 0/35/70% initial construction SVGs and 35/70% Level 2 upgrade stages, plus distinct Level 2 completed silver/yellow art. The pre-existing **single rotating Sprite2D** is retained and connected directly to the shared event-driven `TowerConstructionVisuals`; no split-gun restructuring or combat changes. Original beam origin, beam renderer, 4-second build, 5-second/175-Credit Level 2 upgrade, DPS, ammunition and heat remain unchanged. See [Laser Tower artwork notes](TOWERS/Offensive/LaserTower/Artwork/Upgrades/README.md). Owner Godot visual acceptance pending.

---

## Sniper Tower Level 1 construction artwork completed — 2026-11-03

Added the missing initial-build 0% foundation, 35% structural-frame and 70% partial-barrel SVGs to `TOWERS/Offensive/SniperTower/Artwork/Construction/` and registered their `BuildStages` in `SniperConstructionArt.tres`. The existing shared presenter now shows unfinished artwork during the four-second construction work, then restores the finished Level 1 base/extended gun at 100%. Existing Level 2/3 upgrade stage textures, long muzzle at (0,-39), damage delivery, economy, work duration and drones are untouched. Source-only sanity review; owner should inspect the transitions in Godot. See [Sniper construction notes](TOWERS/Offensive/SniperTower/Artwork/Construction/README.md).

---

## Cryo and Stasis construction/upgrade art — 2026-11-02

Cryo and Stasis now have full event-driven initial-building visual stages (foundation/frame/assembly), independent Level 2/3 finished split base-and-emitter artwork, and 35%/70% staged upgrade composites, all using the existing `TowerConstructionVisuals` resources. Their original single muzzle, health, damage/status effects, work times, costs, targeting, recoil and ammo remain unchanged. The finished Level 1 world placement preview still uses actual tower scene art. See [Cryo](TOWERS/Offensive/CryoTower/Artwork/Upgrades/README.md) and [Stasis](TOWERS/Offensive/StasisTower/Artwork/Upgrades/README.md) notes. Godot visual acceptance pending.

---

## Blaster Cannon and Rocket Tower purchased-tier artwork — 2026-11-02

Blaster Cannon now has distinct finished Level 2 fixed-base/rotating-gun art and two upgrade-stage composites (35% and 70%). Rocket Tower now has separate Level 2 and Level 3 dual-launcher base/gun sets, with two 35%/70% upgrade-stage composites for each tier. Both reuse the already installed `TowerConstructionVisuals` and `TowerConstructionArt`; original initial-building stages are untouched. The existing Cannon **two** and Rocket **three** level tables, damage, work timers, recoil, projectile origins, launch behaviour, ammo and heat are unchanged. [Blaster artwork](TOWERS/Offensive/BlasterCannon/Artwork/Upgrades/README.md) · [Rocket artwork](TOWERS/Offensive/RocketTower/Artwork/Upgrades/README.md). Source implemented; owner runtime visual approval pending.

---

## World placement preview now uses completed Level 1 tower art — 2026-11-02

The green/red placement ghost now mirrors the real built Level 1 sprites from each tower's scene, instead of the separate `TowerDefinition.Icon` resource. Shared `TowerPlacementGhost` makes an inert visual-only copy on tower choice changes, keeping authored base/gun layers, full dimensions and multi-cell scale. The Sniper's longer barrel is no longer compressed into a 64×64 icon. Build-tray thumbnails still have their own catalog icon. See [placement notes](TOWERS/Placement/README.md). Source validation only; owner should inspect in Godot.

---

## Enemy001 hull damage artwork experiment — 2026-11-02

Only the Standard Drone (`basic_mk1`, Enemy001) now opts into three static Sprite2D artwork stages: original healthy hull, scraped/broken armour and heavily damaged exposed machinery. The reusable `EnemyDefinition.DamageAppearance` profile automatically divides **hull HP** into equal bands from its texture count (3 states = thirds, 4 = quarters, etc.), without hardcoded thresholds or per-enemy processing loops. Other enemy types retain original art, shield-only damage never changes hull art, and gameplay rules are untouched. Toggle it off by clearing `Enemy001.tres → Damage Appearance`. See [shared enemy visual documentation](ENEMIES/Shared/Visuals/README.md) and [Enemy001 details](ENEMIES/Types/Enemy001/README.md). Source implemented; owner visual acceptance in Godot pending.

---

## Sniper Tower upgrade art and extended barrel — 2026-11-02

Sniper now has distinct Level 2/3 fixed-base + rotating-gun artwork and separate **35% / 70%** upgrade assembly composites, reusing the Battle/Machine Gun shared construction presenter. The original Sniper gun and catalog artwork also have a longer barrel, with the real muzzle marker aligned to the new tip. The visual-only changes preserve instant-target combat, recoil, placement footprint, paid upgrade timing and economy. See [Sniper artwork guide](TOWERS/Offensive/SniperTower/Artwork/Upgrades/README.md). Owner Godot/editor acceptance pending.

---

## Machine Gun Level 2/3 upgrade artwork — 2026-11-02

Machine Gun Tower now has its own split fixed-base and rotating **dual-barrel** art at Levels 2 and 3, plus distinct **35% and 70%** construction composites during each paid upgrade. It uses Battle Tower's existing shared event-driven presenter with two added sprite-path references; Level1, initial construction art, muzzle sockets, firing, heat and upgrade timing are unchanged. See [Machine Gun upgrade artwork](TOWERS/Offensive/MachineGunTower/Artwork/Upgrades/README.md). Source-only implementation; Godot appearance checks remain with the owner.

---

## Reusable instant-target shots — 2026-11-02

Sniper Tower now selects shared **InstantTarget** delivery: an aligned, funded shot deals immediate damage to the current valid target instead of spawning a travelling bullet. Shared `ProjectileTurretConfig.DeliveryMode` defaults to physical projectile, so Machine Gun, Cryo, Stasis and other towers keep their existing delivery. Sniper recoil, flash, ammo, heat, XP and shield/hull math remain; cosmetic ricochets require a physical impact. See [Sniper Tower](TOWERS/Offensive/SniperTower/README.md) and [shared delivery](TOWERS/Shared/Weapons/Delivery/README.md). Source implemented; Godot gameplay/visual checks remain with the owner.

---

## Full-size Godot Wave Editor tab + Level003 .tres waves — 2026-10-29

Added a `[Tool]` C# main-screen **Wave Editor** plugin (`addons/wave_editor/`), opened from the same workspace selector as Godot's 2D/3D/Script tabs. A resizable editor now opens/creates/saves native `WaveSetResource` `.tres` files with waves, ordered batches, counts, intervals, delays, weighted enemy entries, portal/shield/stealth and optional presentation fields, while preserving the existing immutable `WaveDefinition` runtime data and all weighted spawn mechanics. **Level003** now references `LEVELS/Level003/Level003Waves.tres` with identical 18/22/26/32 enemy counts; original `Level003Waves.cs` remains only as a comparison fixture. Other levels stay C# authored. Build the C# project, enable **Project Settings → Plugins → Wave Editor**, click the new tab. See [plugin guide](addons/wave_editor/README.md). Source checks added; Godot editor/runtime playtest remains pending.

---

## Square construction-site overlay on paid upgrades — 2026-10-27

Timed tower upgrades now also display the existing **cyan square construction hologram** (footprint brackets, blueprint and scanner) over the still-visible tower, from accepted upgrade purchase through queued drone/beam and staged artwork to work completion. Existing `World/Effects/ConstructionSiteHolograms` handles initial builds **and** upgrades with no new nodes, per-tower animation or payment/timing changes; zero-duration upgrades remain instant. [CV1 visual documentation](TOWERS/Shared/Construction/Visuals/README.md). Source checks updated; editor/visual confirmation pending.

---

## CD4 builder drones for upgrades — 2026-10-27

Successful nonzero-duration paid tower upgrades now queue the same nearest-Citadel pooled construction drone as initial builds. The tower is disabled and its upgrade work clock remains at zero until the beam deploys, then existing timed upgrade progress starts. Battle Level2/Level3 now show **35% and 70%** distinct assembly SVGs before the completed split-base/gun art appears at 100%; the drone returns independently. Opt out with `TOWERS/ConstructionDrones/DefaultConstructionDroneConfig.tres → ServiceUpgrades` (enabled by default). No new timers or changes to upgrade prices/stats; existing mid-work checkpoint ownership remains. Source-only validation; Godot runtime/visual checks pending. See [CD4](TOWERS/ConstructionDrones/README.md).

---

## Paid upgrades now disable towers; Battle Tower Level2/3 artwork — 2026-10-27

Paid tower upgrades now suspend attacking/aiming/owner physics until the shared work clock finishes. The selected Inspector reports **DISABLED: UPGRADING** and retains progress/current stats; zero-duration upgrades remain instant. The Battle Tower alone pilots separate Level2/Level3 fixed-base and rotating-gun SVG pairs, plus a 35%-progress temporary upgrade scaffold. No new process loops, purchase/stat changes or loss of muzzle/recoil. Existing save work phases reconstruct the upgrade disable. Source implemented on `dev`; Godot/.NET, zoom and save-resume smoke checks pending. See [DU plan](TOWERS/Shared/Upgrades/UPGRADE_DISABLE_TIER_ART_PLAN.md).

---

## Level003 blank map + four-wave Bulwark debut — 2026-10-27

`LEVELS/Level003/` now contains a blank copy of LevelTemplate for user-painted terrain, a separate config and a four-wave `basic_mk1`/Enemy001, `scout_mk1`/Enemy002 and `medium_mk1`/Enemy004 recipe. Enemy004 starts in wave 3; authored totals are 18/22/26/32. Enabled in Campaign as the immediate Next after Level002. The template portal/Citadel may be moved; **paint a walkable path before trying combat**. See [Level003](LEVELS/Level003/README.md). Source-only validation; Godot/.NET run pending.

---

## More opt-in tower heat profiles — 2026-10-26

Machine Gun (2 min / 10s recovery), Laser (3.5 min / 10s), Blaster Cannon (4 min / 12s), Rocket (4 min / 12s) and Shockwave (3 min / 12s) now enable the existing shared heat capability in their shipped config resources. Laser calibrates heat per damaging beam-second, Shockwave per real pulse interval; shot weapons use their selected paid-level cadence. Battle remains 3 min / 9s; Sniper, Cryo and Stasis stay heatless. Each type exposes Inspector Heat Enabled / Overheat After Minutes / Recovery Seconds. Firing, ammo, price, tower stats and pause/save ownership are unchanged. This is source-implemented; desktop/.NET/Godot verification remains pending. See [heat conditions](TOWERS/Shared/Conditions/README.md).

---

## Organic builders and credit-gain feedback — 2026-10-26

Construction drones now accelerate/brake along curved flights, face travel, hover and reposition
around buildings while beaming, then turn into a smooth return. Motion is checkpointed and stays
inside the active-only shared manager. Credit-gain pulses peak 50% larger and last slightly longer.
See TOWERS/ConstructionDrones/README.md and UI/GameplayHUD/TopBar/README.md. Source/diff review
completed; local .NET/Godot/visual acceptance remains pending.

---

## CV1 shared construction-site holograms — 2026-10-26

Every paid unfinished building now projects a cyan footprint bracket, travelling perimeter indicator and slowly rotating blueprint scanner, including sites **awaiting a queued Citadel builder**. One shared `World/Effects` renderer reads the existing work registration lifecycle, adjusts intensity when the drone releases its hold and sleeps when all construction finishes. Inspector tuning and footprint sizing: [CV1 visuals](TOWERS/Shared/Construction/Visuals/README.md). Desktop visual/performance review remains pending.

---

## Between-wave battlefield saves — S2/S3 (2026-10-26)

Esc Save/Load and profile Campaign Continue now connect to typed battlefield checkpoints. Save
is disabled during combat with **Unavailable during combat**. Towers, work/builders, stock/cargo,
production, wave/RNG progress, frozen research and original reward identity restore into a prepared
paused battlefield. Profileless F6 remains supported. Content changes and retired reward tickets
reject restoration explicitly. Source/diff review completed; **.NET/Godot build/runtime acceptance
remains pending** in the local editor. See [saving](SYSTEMS/Saving/README.md).

---

## Development backlog and documentation audit (2026-10-23)

For current unfinished work **ranked by complexity**, suggested **Codex vs regular ChatGPT** task sizes, additional unimplemented concepts, and a **checklist of outdated Markdown to clean**, see **[NOTES/IMPLEMENTATION_BACKLOG.md](NOTES/IMPLEMENTATION_BACKLOG.md)**. This index distinguishes implemented source, pending QA and historically superseded plan text. Recheck current GitHub HEAD before starting a phase.

---

## Citadel construction drones — CD1/CD2 (2026-10-21)

Paid initial tower/building sites now wait for a cyan builder from the nearest available Citadel. Its beam deploys before ordinary construction time starts; the drone returns after completion. One capped manager/pool handles all towers, with no ground A*. Upgrades/direct/zero-second instances retain their behavior. Settings: TOWERS/ConstructionDrones/DefaultConstructionDroneConfig.tres. Build and dedicated builder/footprint/F4 checks pass; desktop visual/GPU review remains. See TOWERS/ConstructionDrones/README.md for observed tests and existing broad-suite fixture limits.

## Cosmetic shield ricochets — 2026-10-21

Battle/Machine Gun/Sniper shield hits can now leave a short reflected cosmetic streak after real positive shield absorption. One capped world buffer handles all effects; combat remains one hit. Settings: FEEDBACK/DefaultFeedbackConfig.tres and FEEDBACK/Ricochet/DefaultRicochetStyle.tres. Build, focused ricochet and projectile handling checks pass; desktop appearance/GPU review remains. See FEEDBACK/Ricochet/README.md.

## Shared organic terrain edges — E1/E2 (2026-10-21)

Mars atlas clipping/banding is corrected. A reusable profile-driven visual mask now rounds selected terrain boundaries and holes at level load/terrain rebuild, without altering painted tiles, navigation or build rules. The master remains opt-in: WORLD/TERRAIN/Edges/DefaultTerrainEdgeConfig.tres → Enabled=true, then restart the level. Add other tile materials through profiles; no Mars-specific renderer code. See WORLD/TERRAIN/Edges/README.md for settings, tested behavior and remaining desktop visual review.

## F2 enemy artwork preview — 2026-10-21

The wider F2 window now shows the selected enemy's actual-size1:1 sprite on the right, including separate gun artwork. Large artwork scrolls without shrinking. Preview code lives in DEBUG/Spawning and does not spawn gameplay enemies.

## F2 independent batches — 2026-10-21

Start multiple enemy batches concurrently from F2, each with its own amount/gap and Shielded/Stealthed checkboxes. Active rows show progress and individual Cancel controls; Cancel All stops all pending deliveries. All batches share one bounded spawn budget. See DEBUG/Spawning/README.md.

## F2 batch spawner — 2026-10-21

F2 opens a menu to choose enemy, amount and spawn gap in seconds; each ship uses a random valid portal. Close with F2/Escape, or cancel remaining deliveries explicitly. Debug tool implementations now live together under DEBUG (including F3 navigation, audio/alert previews and optional XP grant). Old direct F2 cycling is parked and unbound. See DEBUG/README.md for structure and controls.

## Safety-cache churn correction — 2026-10-20

Active enemies now retain their current segment's safety result, and the shared cache evicts least-recently-used entries. This prevents repeated rebuilding when active segments exceed shared capacity, while immediate navigation invalidation and swept fallbacks remain. See TESTS/Performance/SharedRoutePool.md for the reproduced pressure case and verification.

## Cached movement safety — 2026-10-20

Enemies now share conservative route-segment safety results across waves, avoiding repeated swept wall checks on certified unchanged geometry. Only relevant walkability changes expire certificates; existing staggered repaths and uncertain-geometry fallback remain. Toggle **Enable Route Safety Cache** under the enemy config's Shared Route Pool. See WORLD/NAVIGATION/README.md and TESTS/Performance/SharedRoutePool.md.

## Shared finished routes — 2026-10-20

Compatible enemies now reuse bounded raw paths and finished corridor variations, with selective tower-block invalidation and private movement progress. Toggle **Shared Route Pool** in `ENEMIES/DefaultEnemyConfig.tres`. It warms through ordinary budgeted requests, with no exhaustive startup bake. Trajectories may repeat more often. See `WORLD/NAVIGATION/README.md` and `TESTS/Performance/SharedRoutePool.md` for behavior and measured results.

## PF2 profiling — 2026-10-20

The F4 performance panel now supports optional subsystem CPU timings: Shift+F4 toggles profiling, F5 saves a report. The older top-left grid debug readout is removed; F2/F3 remain. Profiling defaults off. See `DEBUG/README.md` for controls, nested timing meanings and report location, and `TESTS/Debug/PF2.md` for actual verification.

## Projectile handling update (PA1–PA3)

Optional immutable handling now carries launch spread and explicit Straight/Homing guidance through level → selected rank → captured research → safety → final snapshots. Projectile towers default to straight physical shots; splash uses a fixed launch destination. See `TOWERS/Shared/Projectiles/PROJECTILE_GUIDANCE_ACCURACY_PLAN.md` for implementation decisions and retained pass history.

### Gunner Drone handling polish — 2026-10-20

Gunner now holds valid targets for1s, searches every0.2s when idle, fires continuously at6/s, and uses small rounded-front/tapered-tail red energy streaks. All three are data-driven; travelling bullets and Kinetic damage remain intact. Import/build and the three focused enemy-weapon headless checks passed. Desktop visual approval remains pending. See ENEMIES/Weapons/README.md and TESTS/EnemyWeapons/README.md.
---

### EG3 Gunner Drone — source implemented (2026-10-20)

F2/catalog now includes Gunner Drone: a distinct64×48 hull matching medium Enemy004's size, separate rotating gun SVG, small muted-red tracers and tiny orange-red accepted-shot flashes. It keeps routing while shooting nearby towers via EG1/EG2. Normal waves remain unchanged. New scene and balance: [GunnerDrone](ENEMIES/Types/GunnerDrone/README.md). Build/runtime/visual verification remains pending because execution was unavailable; EG4 profiling is still future work.

### EG2 hostile bullet foundation — source implemented (2026-10-20)

Enemy minigun mounts can now launch straight world-owned bullets that sweep against living tower footprints and apply existing Kinetic tower damage. One retained shot buffer and renderer replace per-bullet scene nodes; pause, lifetime, overflow and source-removal handling are included. Generic muted-red tracer appearance and budgets are Inspector editable. Existing enemies/waves remain unarmed; Gunner Drone/F2/art is EG3, benchmarking is EG4. Build/runtime/visual verification remains pending because execution was unavailable. See [enemy bullets](ENEMIES/Projectiles/README.md).

### EG1 enemy weapon foundation — source implemented (2026-10-20)

Optional enemy weapon mounts now acquire nearby living towers through one event-driven spatial TowerRegistry and aim a separate gun pivot while normal routing continues. Minigun burst timing exposes an immutable shot-request seam. Existing enemies remain unarmed; hostile bullets/damage (EG2), Gunner Drone/F2/art (EG3) and profiling (EG4) remain future work. C# build/Godot/runtime tests are pending because execution was unavailable. See [enemy weapons](ENEMIES/Weapons/README.md).

### Offensive projectile recoil rollout — 2026-10-20

Machine Gun, Sniper, Blaster Cannon, Rocket, Cryo and Stasis now join Battle with fixed bases and recoiling gun/muzzle assemblies, each using an editable preset in its Visuals folder. Laser and stationary Shockwave retain their beam/radial effects. Gameplay values and Inspector layout are unchanged. Source/static review completed; Godot import/build/tests and desktop visual verification remain pending. See [recoil notes](TOWERS/Shared/Weapons/Recoil/README.md).

### Battle recoil pilot — R1 + R2 (2026-10-20)

Battle Tower now has a fixed base and a split recoiling gun/muzzle, configured by an optional shared visual-only preset. Construction stages and composite artwork fallbacks remain supported. Source implementation is complete; Godot import/build/runtime and visual validation are pending. See [recoil notes](TOWERS/Shared/Weapons/Recoil/README.md) and [checks](TESTS/Towers/TowerRecoil.md).

## SE1 — stealth enemies and reveal beacon — implemented 2026-10-20 UTC

Waves may now author `Enemy("basic_mk1", stealth: true)` independently of `shield: true`. Default spawns remain ordinary; no existing wave/map was changed. Stealth ships render at30% alpha, revealed ships at100%, with configurable/dimmed defense feedback that composes with Cryo/Stasis. All aimed towers share detection filtering and cached-lock validation; private TowerConfig.CanDetectStealth opts a specialist in without globally revealing enemies. Existing attacks already emitted, splash and untargeted Shockwave remain able to damage hidden ships (stealth is targeting, not immunity).

New Ctrl / Utility Stealth Beacon:250 Credits,1×1,2s build,180 HP,256px range, one paid tier. It reveals enemies inside range for all towers; overlap, exit/re-entry, disable/reactivation, construction, destruction, relocation, enemy removal/reuse and world teardown are source-owned. One lazy spatial reveal index listens to enemy motion only while beacons are active; no per-frame enemy scans or per-beacon processing. See ENEMIES/Shared/Stealth, FEEDBACK/Stealth, TOWERS/Shared/Detection, TOWERS/Utility/StealthBeacon and TESTS/Stealth READMEs. Existing Inspector layout/resize is retained. Earlier blanket “stealth/detection future” notes below are historical; stealth preference scoring, Radar, tower-grant detection beacons and further content remain separate future work.

---

## Lightweight data-driven levels — implemented 2026-10-19

Shared `LEVELS/Runtime/Battlefield.tscn` + `WORLD/World.tscn` replace per-level service copies. Existing Main launchers and World variants are thin compatibility shortcuts; F6 paths stay valid. Each level now has a MissionDefinition resource and the Campaign roster is `LEVELS/Catalog/DefaultMissionCatalog.tres`. Menu number/title/description/map are data, with a bounded scrollable mission list. New missions deploy/retry directly from their resource; no per-level launcher/world or menu/catalog code is required. Next advances only to the immediate enabled number.

LevelConfig directly references an Inspector-assignable C# WaveSetSource; the three existing wave recipes keep identical Create() content and now implement Build(). WaveCatalog's old central switch is removed; string-ID compatibility resolves via the mission roster. Painting, map/config ownership, portal/Citadel placement, credits/HP/XP values and progression identities are preserved. Explicit off-tree preparation supports the existing multi-Citadel fixture. Citadel destination cleanup is now idempotent across objective/world teardown.

Authoring steps: `LEVELS/AUTHORING.md`; ownership: `LEVELS/Catalog/README.md`, `LEVELS/Runtime/README.md`; validation: `TESTS/Integration/LightweightLevels.md`. This completes shared-launcher/catalog convenience, not a visual wave editor, campaign unlock/save system or hot map switching. Earlier duplicated-launcher instructions below are historical.

---

## ARG automatic income correction — 2026-10-19

ARG eligible kills now grant Credits directly to the existing wallet: no collection button, stored income or storage limit. The Resource Tower and Citadels retain manual collection. A single mouse-transparent green floating number beside each ARG combines rapid successful payouts (+1 → +2 → +5), then floats/fades away after a quiet interval (default 0.8s). Popup duration/color are editable on ARG config. Higher tiers still earn 2/3 per eligible kill. Full-wallet rejection displays no false income and is not banked. Existing kill eligibility, pause/construction/upgrade gates and ordinary kill rewards/XP remain intact. ARG previews omit unused storage. Earlier buffered-ARG descriptions below are historical and superseded by this correction.

---

## Resource Tower and Adaptable Resource Generator — 2026-10-19

The Ctrl **Economy** tray now includes two muted-green Credit producers, each with three paid tiers and an Inspector **Collect Credits** action. Resource Tower earns periodically during active waves; ARG earns from credited kills by nearby offensive towers. Existing Citadel income/collection is retained. Balance and tuning: `ECONOMY/Production/README.md`; verification/known baseline test failures: `TESTS/Economy/CreditTowers.md`.

---

## Performance overlay and F2 tools — 2026-10-18

**F4** toggles the new click-through performance panel in debug builds on all playable battlefields. The contained script and settings guide live in `DEBUG/`. Level002/template now include the shared F2 spawn/F3 route-preview tools; use Campaign or a Main launcher for gameplay rather than running the raw map alone. See `DEBUG/LevelTools/README.md`.

---

## Terrain Edges scaffold — opt-in visual foundation (2026-10-18)

`WORLD/TERRAIN/Edges/` now defines editable edge-style resources, Mars material mappings, an on-demand material cache and a pure 8-neighbour mask. This is a foundation for Codex, **not live corner smoothing**: renderer draws nothing; existing levels and artwork are unchanged. See [the Codex handoff](WORLD/TERRAIN/Edges/IMPLEMENTATION_PLAN.md).

---

## Playable Level002 tutorial — 2026-10-18

Campaign now offers Level001, the newly painted **Level002** (four gentle unshielded waves), and preserved Level020. Open `MainLevel002.tscn` for direct F6 play. Level001 Next advances to002; no level skips to020. See `LEVELS/Level002/README.md` for exact composition/settings. Earlier unfinished-Level002 notes below are historical.

---

## Level002 moved to Level020; fresh Level002 design map — 2026-10-18

The Five Gates is now `LEVELS/Level020/Level020.tscn`, with `Level020Config.tres`, `Level020Waves.cs` and WaveSetId `level020`. Its terrain, object layout, wave composition and balance are preserved. Its standalone launcher/world variant are `MainLevel020.tscn` / `WORLD/WorldLevel020.tscn`; Campaign labels/launches Level020 and PlayableLevelCatalog registers numeric ID20. Scene/script UID sidecars are retained across renames. Tests and current documentation reference the moved map.

Fresh `LEVELS/Level002/Level002.tscn` is copied from the current LevelTemplate with a new root name and independent `Level002Config.tres` copied from DefaultLevelConfig. Empty terrain layers and template background/Portal/Citadel/checkpoint overlay are retained. The template itself is unchanged. Open this scene in Godot's 2D editor to design it. It has no authored waves or playable launcher yet and is not offered as a finished Campaign mission. Add those when the map is ready. Results cannot skip the missing playable Level002 to reach Level020; Level001's Next is disabled for now. Level020 remains available directly from Campaign and F6 on MainLevel020.

Earlier Level002/Five Gates references in historical notes below refer to the map now called Level020. No old design plan or map content was removed. Verification: see `TESTS/Integration/LevelReorganization.md`.

---

## Multi-Citadel HUD and F3 — MC3 (2026-10-18)

Two or more Citadels now divide the top-center HUD into equal-width health meters in one horizontal row. Single-Citadel layout is preserved. F3 previews and labels each portal's cached assigned destination, including secondary Citadels and checkpoint chains. Existing maps remain unchanged. See `UI/GameplayHUD/TopBar/README.md` and `TESTS/UI/MultiCitadelHud.md`; desktop appearance review remains.

---

## Multiple Citadel foundation — MC1/MC2 (2026-10-18)

Levels can author multiple independently damageable Citadels with cached fixed portal destinations. Routing, checkpoints, children, placement protection and any-Citadel defeat respect those identities. Single-Citadel maps bypass destination comparisons. Existing gameplay maps/UI are unchanged; multi-Citadel HUD/F3 presentation remains MC3. See `WORLD/OBJECTIVES/Citadel/README.md`.

---

## Stasis Tower — 2026-10-18

Green Stasis is available in the Offensive build tray: low-rate Energy projectiles temporarily stop unfrozen, nonimmune enemies. Cryo slow persists underneath and returns on expiration. Initial test balance: 350 Credits, 3-second stop, one shot every 5 seconds. See `TOWERS/Offensive/StasisTower/README.md`.

---

## Campaign hub — 2026-10-17

Campaign now displays profile progression with LEVELS and RESEARCH. LEVELS opens the existing two-mission chooser; both mission buttons launch the same battlefields. Results returns to Campaign, and Research remains between missions. See UI/Menus/Campaign/Levels/README.md.

---

## Mission XP foundation — 2026-10-17

Mission outcomes now grant configurable per-level player XP (victory500 / defeat0 initially) once to the deployment profile. Results shows the saved reward/level changes/RP and returns to Campaign for Research. See SYSTEMS/Progression/Missions/README.md; first-clear/replay balancing and battlefield saves remain deferred.

---

# Godot Tower Defense — C#/.NET

## Research-first extension — 2026-10-17

Permanent player research now has a first playable foundation: Campaign → RESEARCH → Research Core / Citadel Master I (1 RP, +10% starting Citadel HP). Ownership and RP persist per profile; v1 XP records migrate without reset. Results now tracks real placements, initial construction completions, paid upgrades and unpaused time alongside kills/breaches/waves/integrity. Mission XP scoring/rewards remain deferred. See SYSTEMS/Progression/Research/README.md and TESTS/Research/README.md.

---

A 2D top-down sci-fi tower-defense game in Godot 4.7.x. Editable Level001 and five-portal Level020 maps, enemy spawning and A* routing, finite manual waves, Battle/Laser/Blaster combat, shared instant area damage, wallet/kill credits, Citadel HP and the eight-stage tower-stat foundation are implemented.

## Launch

Build C# in the Godot .NET editor, then press **F5**. The project starts at **Boot**, where you create/select a local profile. Select **Campaign** in Main Menu, then **Play Level 001** or **Play Level 020** to enter the selected battlefield. Profiles persist to Godot `user://Profiles/profiles.json` (versioned JSON with backup recovery). **Campaign/gameplay saves are not yet implemented**: each deployment starts a fresh level session.

For rapid map or combat testing, open `Main.tscn` (Level001) or `MainLevel020.tscn` (Level020) and press **F6** (Run Current Scene). This bypasses profile/menus and remains valid for headless integration checks.

## Game controls

- WASD/arrows: move camera; middle mouse: pan; mouse wheel: zoom.
- Ctrl: toggle tower build tray. Choose Battle, Laser or Blaster Cannon, left-click valid cells to place, right-click/Escape to cancel; select towers to inspect them.
- Escape: cancel active placement first; otherwise open/resume Pause. Save/Load/Options are disabled placeholders.
- F11: toggle fullscreen everywhere, restoring windowed/maximized mode. Shared shortcuts live in `CONTROLS/`.
- F2: cycle the enemy catalog through the level's debug system.
- F3: open Route Preview; choose portal/enemy/path samples and click Simulate Paths (debug only).
- Start/Next Wave: use the top-right wave button.

## Configuration and structure

- `UI/DefaultUIConfig.tres`: HUD layout, palette and common button colors.
- `UI/Shared/UIButtonFactory.cs`: consistent button styling across menus, HUD and dialogs.
- `UI/Menus/`: Boot, Main, Campaign and level-owned Pause.
- `SYSTEMS/Profiles/`: local profile records and persistence.
- `SYSTEMS/Saving/`: planned campaign persistence boundaries (not implemented yet).
- `LEVELS/Level001/`: editable terrain, portal/objective configuration and finite waves.
- `WORLD/`, `ENEMIES/`, `TOWERS/`, `ECONOMY/`: reusable gameplay systems.

Use feature READMEs for detailed implementation and development plans, especially `TOWERS/README.md`, `LEVELS/README.md`, `UI/Menus/README.md`, `SYSTEMS/Saving/README.md` and `NOTES/CodePreferences.md`. Run `TESTS/Integration/EnemyIntegrationChecks.tscn` in the Godot .NET headless runner for gameplay regression checks.

## Long-term vision — optional multi-genre game (planning only)

**Current priority: build, polish and complete Tower Defense as a self-contained, fully playable game.** Do **not** reorganize or prematurely extract its systems for this vision. All TD code, gameplay, saving work, assets and **UI** can remain local to this repository and be developed normally. This is a future direction, **not** a request to implement Idle Clicker, Space Shooter or cross-genre infrastructure now.

The eventual concept is **one persistent sci-fi campaign / empire** with three connected gameplay modes:

- **Idle Clicker / Empire Management (eventual main hub):** mine/process resources, accumulate Credits, run factories, manufacture towers/ships, and progress research. Offline progress is computed from saved state and elapsed time when the campaign is reopened (with production/storage constraints), without running the game continuously in the background.
- **Tower Defense (this project, the present focus):** defend levels using manufactured equipment and research bonuses from the future empire. For example, manufacturing five Battle Towers puts five in persistent inventory; **successfully deploying** one deducts one from that available inventory. Failed/cancelled placement must not consume stock. Tower losses, mission rewards and other persistence rules will be decided when integration is actually designed.
- **Space Shooter (future):** an optional action-oriented mode for exploring space, mining resources directly and fighting enemies. Camera/genre (2D top-down versus first-person/3D) can be decided later. Its rewards would feed the same campaign economy.

### Development and future integration principles

1. **Do Tower Defense first.** Continue building towers, enemies, levels, movement, interactive terrain, construction, ammunition, upgrades, UI, performance and testing without waiting for the other genres. The present game must stay runnable independently for development/testing.
2. **Keep the current UI here.** Menus, HUD, Inspector, build tray, visual theme and shared UI helpers should continue to evolve inside TD. **Only later**, if/when other genres exist and consistent presentation is desirable, extract or share suitable UI components/theme resources across modes. Do not move or refactor UI into a cross-game framework today.
3. **Preserve sensible boundaries, not speculative abstractions.** Continue using stable tower/resource IDs, separate authored definitions from per-instance state, reuse the existing transaction/economy and stat-pipeline ownership, and avoid tightly coupling tower combat to menus or a hypothetical Idle system. No new general-purpose cross-genre managers, interfaces or folder hierarchy are required now.
4. **Integrate through a shared campaign state later.** When development actually reaches the hybrid phase, the persistent campaign should own resource balances, manufactured inventory, research, progression and cross-mode rewards. Each mode should consume/return explicit campaign data rather than maintaining conflicting copies. TD can retain a **standalone/test mode** with its current starting resources and placement, alongside a future campaign-connected mode.
5. **Saving comes in stages.** Current local profiles are not campaign saves; follow \`SYSTEMS/Saving/README.md\` when implementing TD saves. Design persistence with stable IDs and clean level/session boundaries so eventual offline progression and cross-genre inventories can be added without serializing live scene nodes or cached derived stats. Do not build an offline simulator until the Idle mode exists.
6. **Avoid a late three-game merge.** If the larger vision is pursued, build one modest end-to-end connection at a time: resource production → tower inventory → TD deployment → mission results; add research and Shooter integration afterward. The TD implementation should not be rewritten wholesale to achieve this.

**Bottom line:** Treat this repository as the complete Tower Defense game **today**, with the *option* to become the Tower Defense module within a broader multi-genre game **later**. This vision must not block or complicate current TD work.

## Ammunition supply

All four offensive weapons consume finite local reserves. Build an Ammunition Factory (200 Credits) and Depot (150 Credits) from the Ctrl tray: factories manufacture automatically, visible drones carry stock to depots, then depot drones supply towers at/below10%. New weapons start full by default; their config InitialAmmoFraction permits quicker low/empty testing. See `TOWERS/Economy/Ammunition/README.md` and `TESTS/Economy/Ammunition.md`. Magazines, power, inputs, research and campaign saving remain deferred.

## Player progression foundation (2026-10-17)

Separate per-profile XP/levels/Research Points now live in SYSTEMS/Progression (see its README). Main and Campaign show the selected profile's level, XP bar and RP. Missing progression starts at Level 1; saved grants survive restart and profile switching. Identity/index format is unchanged. The optional menu DEBUG grant is Inspector-controlled and off by default. Mission rewards/Results XP, replay rules, research purchasing and full campaign/battlefield saving remain deferred; this does not complete P1–P5 or Results R4.
