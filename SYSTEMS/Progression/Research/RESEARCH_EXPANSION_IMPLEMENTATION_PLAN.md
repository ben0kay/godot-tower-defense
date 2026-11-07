# Research Expansion — Six-Branch Technology Tree (EX1–EX3)

**Repository:** `ben0kay/godot-tower-defense` · **Branch:** `dev` only  
**Date:** 2026-11-06  
**Status:** **Owner-selected design baseline / implementation PLAN ONLY. No new nodes or effects have been added to the game by this document.**  
**Source of ideas:** the owner's childhood GameMaker research notes, adapted to modern Godot tower damage, construction, power islands, fighters, real ammo logistics and mission progression. Research costs, effect strengths, node order and unlock restrictions below are **provisional modern proposals**, not literal original-game balances.

## 1. Target and what already exists

Turn the currently very small Research Room into an interconnected **six-direction technology tree**, with distinct Weapons, Citadel, Industry, Energy, Recon and Drone branches radiating from **one existing free Research Core**. Users progressively unlock connected technologies; locked nodes remain visibly connected and show requirements. A first substantial delivery targets **24 purchased nodes total (22 new + 2 existing), distributed across all six branches**; the longer catalog contains **36 purchased nodes total (34 new), plus the free core**. Stage hard capstones separately; no filler/pretend research.

The live `dev` catalog currently contains exactly **three displayed nodes**, not three separate research branches:
- `research_core` — free at axial `(0,0)`.
- `citadel_master_1` — purchased node at `(1,0)`, **1 RP**, +10% deployed Citadel max/current HP.
- `battle_weapons_1` — purchased node at `(-1,0)`, **1 RP**, +5% Battle Tower damage.

**Never delete, rename, move, rebalance, reprice or reset these existing nodes or saved purchase IDs.** This is a six-branch expansion of the current graph, not a new Research Room or profile economy. The owner already has Campaign → RESEARCH, the hex `ResearchGraph` with zoom/pan/keyboard selection, real prerequisites, guarded purchasing, v3 profile XP/RP, and immutable deployment bonuses. Reuse them.

The only current `ResearchEffectKind` values are `None`, `CitadelMaxHealthPercent` and `TowerDamagePercent`. The node catalog rejects non-root `None` effects. **A research node must have a real implemented effect before it is added to the live catalog.** Do not display inert placeholder buttons or silently use a broadly mismatched effect type just to fill the screen.

## 2. Six main branch directions and presentation

Use the existing axial hex grid; each paid first-tier node is immediately adjacent to the root. Branches grow outward with a main line and a visible fork after their second step:

| Division | Color intent (optional subtle branch cue) | First node and fixed/target axial |
| --- | --- | --- |
| **Citadel Defence** | Cyan / teal | `citadel_master_1` at **(1,0)** (existing) |
| **Drone Technology** | Electric blue | `reinforced_fighter_hulls` at **(0,1)** |
| **Reconnaissance** | Violet | `stealth_enhancement_1` at **(-1,1)** |
| **Weapons Technology** | Amber / white | `battle_weapons_1` at **(-1,0)** (existing) |
| **Industry & Logistics** | Warm gold | `mass_production_1` at **(0,-1)** |
| **Energy Engineering** | Pale turquoise | `capacitor_banks_1` at **(1,-1)** |

All colors are *optional cosmetic hints*; **RESEARCHED / AVAILABLE / LOCKED / INSUFFICIENT RP** and selection contrast must remain legible regardless of hue. Do not replace the existing one-canvas, event-driven graph with 36+ individually processing scene nodes. Center the initial camera on the core/first ring; the existing ResetView minimum 0.45× may make the outer ring clip on small viewports, so make Reset/Overview sensible while retaining pan/zoom, readable text and current selection. Optional branch headers/legends can be static text or shared draw calls; no idle animation is required.

### Coordinate contract

Within each branch, the six nodes follow a rotated six-hex local pattern: **tier 1 (1,0); tier 2 (2,0); tier 3 main (3,0) and fork (3,-1); tier 4 main (4,0) and fork (4,-1)**. Rotate around the root in six axial directions; explicit final coordinates are in the tables below. Every prerequisite shown is an **adjacent hex**, the 36 coordinates are **unique**, and each path terminates at the same central `research_core` through its prerequisites. The visually branched structure should be evident even before purchases. Multi-prerequisite AND gates are supported by the current catalog, but avoid arbitrary long diagonal connector lines in the initial six branches.

## 3. Proposed complete catalog — 36 paid nodes plus free Core

**Cost pattern per branch:** first tier **1 RP**, second tier **1 RP**, each third-tier fork **2 RP**, fourth-tier main **3 RP**, fourth-tier specialist **4 RP**. This is **13 RP per fully purchased branch** (78 RP for six complete branches), *not* an assumed target for a single campaign. The game currently earns one RP on a profile level-up; check real progression pace before finalizing advanced prices and consider giving players interesting alternative paths rather than assuming every node is bought. Existing two purchases keep their current 1 RP prices.

**Phase labels:** `EXISTING` = current shipped purchase. `EX1-A` = open a new radial branch with real effect. `EX1-B` = second tier. `EX1-C` = third-tier fork (the fuller first milestone). `EX2` = later high-impact capstones. All proposed phases remain **NOT IMPLEMENTED** until separately authorized and committed.

### Citadel Defence

Citadel survival, breach mitigation and friendly structure durability.

| Stable ID | Display name | RP | Prerequisite | Axial | Proposed effect | Phase |
| --- | --- | ---: | --- | --- | --- | --- |
| `citadel_master_1` | Citadel Master I | 1 | `research_core` | `(1, 0)` | Existing: +10% Citadel starting/max HP; preserve exact behaviour. | EXISTING |
| `citadel_master_2` | Citadel Master II | 1 | `citadel_master_1` | `(2, 0)` | Additional +10% Citadel authored baseline starting/max HP. | EX1-B |
| `reinforced_bulkheads` | Reinforced Bulkheads | 2 | `citadel_master_2` | `(3, 0)` | Citadel takes 5% less actual damage from enemy breaches. | EX1-C |
| `structural_reinforcement` | Structural Reinforcement | 2 | `citadel_master_2` | `(3, -1)` | Friendly towers deploy with +5% maximum/current hull HP. | EX1-C |
| `emergency_repairs` | Emergency Repairs | 3 | `reinforced_bulkheads` | `(4, 0)` | Once per cleared wave, restore 3% max Citadel HP, capped; never mid-wave. | EX2 |
| `fortress_protocol` | Fortress Protocol | 4 | `structural_reinforcement` | `(4, -1)` | Unlock an optional paid/cooldown emergency Citadel shield; ability balance requires separate approval. | EX2 |

### Drone Technology

Station-owned fighters, hangar service and finite logistics (never free ammo).

| Stable ID | Display name | RP | Prerequisite | Axial | Proposed effect | Phase |
| --- | --- | ---: | --- | --- | --- | --- |
| `reinforced_fighter_hulls` | Reinforced Fighter Hulls | 1 | `research_core` | `(0, 1)` | Newly deployed fighters gain +10% maximum/current hull HP. | EX1-A |
| `extended_ammunition_reserves` | Extended Ammunition Reserves | 1 | `reinforced_fighter_hulls` | `(0, 2)` | Fighter reserve capacity +10%; station stock remains finite and independent. | EX1-B |
| `advanced_docking_protocols` | Advanced Docking Protocols | 2 | `extended_ammunition_reserves` | `(0, 3)` | -15% normal fighter rearm time with actual per-round station transfers. | EX1-C |
| `logistics_coordination` | Logistics Coordination | 2 | `extended_ammunition_reserves` | `(1, 2)` | Drone Station local ammo capacity +10%; no stock added. | EX1-C |
| `squadron_expansion` | Squadron Expansion | 3 | `advanced_docking_protocols` | `(0, 4)` | +1 purchased fighter slot per station, limited by existing four-slot engine. | EX2 |
| `autonomous_combat_systems` | Autonomous Combat Systems | 4 | `logistics_coordination` | `(1, 3)` | Optional future fighter specialist target/assist mode, preserving FD4.1 LOS/arc/nearest default. | EX2 |

### Reconnaissance

Stealth detection, precise targeting and authored wave intelligence.

| Stable ID | Display name | RP | Prerequisite | Axial | Proposed effect | Phase |
| --- | --- | ---: | --- | --- | --- | --- |
| `stealth_enhancement_1` | Stealth Enhancement I | 1 | `research_core` | `(-1, 1)` | +10% effective Stealth Beacon scan radius. | EX1-A |
| `advanced_scanning` | Advanced Scanning | 1 | `stealth_enhancement_1` | `(-2, 2)` | +10° Stealth Beacon scan cone width, capped/configurable. | EX1-B |
| `tracking_computers` | Tracking Computers | 2 | `advanced_scanning` | `(-3, 3)` | -10% weapon spread on eligible projectile weapons with existing handling data. | EX1-C |
| `threat_analysis` | Threat Analysis | 2 | `advanced_scanning` | `(-2, 3)` | Show known authored composition/count of the next wave before launch. | EX1-C |
| `isrc_completion` | ISRC Completion | 3 | `tracking_computers` | `(-4, 4)` | Preview the next two authored waves with source/intel context; no unknown-spawn predictions. | EX2 |
| `precision_detection` | Precision Detection | 4 | `threat_analysis` | `(-3, 4)` | Additional detected-ship identity/threat detail only where existing reveal rules permit. | EX2 |

### Weapons Technology

Kinetic tower damage, bullet handling, range and advanced penetration.

| Stable ID | Display name | RP | Prerequisite | Axial | Proposed effect | Phase |
| --- | --- | ---: | --- | --- | --- | --- |
| `battle_weapons_1` | Battle Tower Weapons I | 1 | `research_core` | `(-1, 0)` | Existing: +5% Battle Tower damage for next deployments; preserve exact effect. | EXISTING |
| `battle_weapons_2` | Battle Tower Weapons II | 1 | `battle_weapons_1` | `(-2, 0)` | Additional +5% Battle Tower damage, additive to Weapons I. | EX1-B |
| `machine_gun_calibration_1` | Machine Gun Calibration I | 2 | `battle_weapons_2` | `(-3, 0)` | +5% Machine Gun Tower damage. | EX1-C |
| `high_velocity_ammunition` | High-Velocity Ammunition | 2 | `battle_weapons_2` | `(-3, 1)` | +10% ballistic projectile speed on eligible kinetic towers (no higher fire rate). | EX1-C |
| `improved_barrel_assembly` | Improved Barrel Assembly | 3 | `machine_gun_calibration_1` | `(-4, 0)` | +5% effective range on eligible kinetic towers (not drone guns). | EX2 |
| `kinetic_penetrators` | Kinetic Penetrators | 4 | `high_velocity_ammunition` | `(-4, 1)` | Optional finite single-additional-target penetration for qualifying kinetic rounds. | EX2 |

### Industry & Logistics

Construction efficiency and real Factory→Depot→consumer supply capacity.

| Stable ID | Display name | RP | Prerequisite | Axial | Proposed effect | Phase |
| --- | --- | ---: | --- | --- | --- | --- |
| `mass_production_1` | Mass Production I | 1 | `research_core` | `(0, -1)` | -5% new tower construction time (not already-started jobs). | EX1-A |
| `mass_production_2` | Mass Production II | 1 | `mass_production_1` | `(0, -2)` | +10% tower ammunition inventory capacity; no free stock. | EX1-B |
| `factory_optimization` | Factory Optimization | 2 | `mass_production_2` | `(0, -3)` | +10% real factory ammunition production output, never phantom ammo. | EX1-C |
| `depot_coordination` | Depot Coordination | 2 | `mass_production_2` | `(-1, -2)` | +15% effective Depot→consumer courier carry limit; stock still conserved. | EX1-C |
| `scrap_yard` | Scrap Yard | 3 | `factory_optimization` | `(0, -4)` | -3% base tower construction Credits with authoritative paid-cost tracking. | EX2 |
| `scavenger_protocols` | Scavenger Protocols | 4 | `depot_coordination` | `(-1, -3)` | Proposed +1 Credit for an eligible official wave-owned ship kill, paid once. | EX2 |

### Energy Engineering

Bounded capacitors, transmission, cooling and powered weapon trade-offs.

| Stable ID | Display name | RP | Prerequisite | Axial | Proposed effect | Phase |
| --- | --- | ---: | --- | --- | --- | --- |
| `capacitor_banks_1` | Capacitor Banks I | 1 | `research_core` | `(1, -1)` | +10% tower internal capacitor maximum (does not add stored energy). | EX1-A |
| `conductive_lattices` | Conductive Lattices | 1 | `capacitor_banks_1` | `(2, -2)` | +5% achievable tower recharge while honoring real island output/distance limits. | EX1-B |
| `long_range_distribution` | Long-Range Distribution | 2 | `conductive_lattices` | `(3, -3)` | Slightly less distance recharge loss within reachable conductive paths; no new power. | EX1-C |
| `relay_calibration` | Relay Calibration | 2 | `conductive_lattices` | `(2, -3)` | +5% effective local Relay recharge boost, respecting existing ceiling/supply budget. | EX1-C |
| `advanced_cooling_arrays` | Advanced Cooling Arrays | 3 | `long_range_distribution` | `(4, -4)` | +10% continuous funded firing time before heat reaches overheat. | EX2 |
| `energy_spike` | Energy Spike | 4 | `relay_calibration` | `(3, -4)` | +5% qualifying energy-weapon damage with +8% real shot/beam energy cost. | EX2 |

## 4. Staged Codex implementation handoff

### EX1-A — Six visible research divisions / first-ring completion

**Four new nodes:** `reinforced_fighter_hulls`, `stealth_enhancement_1`, `mass_production_1` and `capacitor_banks_1`. The other first-ring nodes, `citadel_master_1` and `battle_weapons_1`, are already shipped. Integrate these four with small, typed, actual deployment effects before adding to `DefaultResearchCatalog.tres`. Result: Core + **six functioning first-tier branches** (seven visible nodes).

- Extend the research effect vocabulary/resolver with explicit finite effects. Prefer typed bounded fields grouped by real consumer rather than one special case per ID or a generic `effectName` string.
- Cache the selected profile's purchased bonuses **once** per new deployment using the existing `ResearchDeployment` approach. Research remains profile-permanent and never changes an already active battlefield retroactively.
- Construction reduction must modify scheduled *new* work through existing construction cost/time owners; it must not shorten running jobs on load. Capacitor capacity and Beacon range must respect existing per-tower stats and limits. Fighter max HP is for **newly deployed / purchased** fighters and must not alter already-saved health without an explicit restore policy.
- Add one real `.tres` node per supported effect, existing stable IDs intact. Node descriptions and ResearchMenu's effect text must state the actual behaviour accurately.

### EX1-B — Six linear second-tier extensions

**Six new nodes:** `citadel_master_2`, `extended_ammunition_reserves`, `advanced_scanning`, `battle_weapons_2`, `mass_production_2`, `conductive_lattices`. Build on existing mechanics/typed bonus resolver and **real finite resources**. Weapons I/II and Citadel I/II add to the authored baseline rather than repeatedly multiplying deployed effects. New ammo storage capacity **never creates ammo**; higher recharge efficiency cannot produce extra network electricity. Avoid changing existing `*Levels.cs` rows, wave stats, station starting ammo or fighter ammo parity.

### EX1-C — Twelve third-tier forks (first full tree milestone)

**Twelve new nodes**, main and side option for each branch: `reinforced_bulkheads`, `structural_reinforcement`, `advanced_docking_protocols`, `logistics_coordination`, `tracking_computers`, `threat_analysis`, `machine_gun_calibration_1`, `high_velocity_ammunition`, `factory_optimization`, `depot_coordination`, `long_range_distribution`, `relay_calibration`.

Once implemented, the live Research Room has **24 paid nodes (+22 over today) plus the one free Core**, with six branches, real tiered purchases, visible side forks and no nonfunctional padding. Deliver EX1-C in a few small related effect-family commits rather than one risky all-systems rewrite; a branch may temporarily stop at its latest actually implemented node. For Wave intelligence, use authored `WaveSource`/current level schedules (including mixed source rules) without predicting future random draws, spawning enemies, consuming gameplay RNG or leaking unknown scripted surprise events. Do not introduce another wave manager.

### EX2 — Twelve advanced capstones (later, separate approvals)

Remaining **two fourth-tier nodes per division** (listed in each table above). These include substantial mechanics such as Citadel repairs/shields, paid tower-cost modifiers, XP/reward-safe salvage Credits, more detailed ISRC/intel, fighter squad slots, specialist fighter modes, projectile penetration and energy-damage/cost coupling. **These are design targets, not permission to build the associated new combat/UI systems.** Scope and approve each mechanic before implementing it, using existing shared combat, economy, saving, wave and fighter boundaries. Prioritize a few capstones rather than all 12 at once.

### EX3 — Future cross-branch experimental/Dark Matter convergence (not part of the 36)

Keep the total at **six primary divisions**. A *later* shared research convergence can require two or more bought capstones plus an actual campaign DME milestone before enabling `Dark Matter Converter`, `Zy0n Coordination`, `Zy0n Recharge`, `Zy0n Destruction`, future Magnetic/advanced Ion tower unlocks, etc. This draws on the owner's childhood GameMaker research notes, **but Dark Matter/Zy0n resource, research-inventory slots, extra tower catalog unlocks and campaign milestones do not exist here yet**. Design after the base six divisions and mission unlock policy are validated; do not add unpurchaseable `None` nodes now, pretend a storyline flag exists, or create a seventh first-ring branch.

## 5. System integration rules and compatibility

- **Authoritative research:** `ResearchDefinition.cs`, `ResearchCatalog.cs`, `DefaultResearchCatalog.tres`, `ResearchRules.cs`, `ResearchService.cs`, `ResearchDeployment.cs`; the Campaign-owned `UI/Menus/Campaign/Research/ResearchMenu.cs` and `ResearchGraph.cs` handle only selection, display, navigation and guarded purchases.
- **Existing source invariants:** Research Core always free and not persisted as a purchase; IDs only persisted; prerequisite DAG and unique axial cells validated; no duplicate charges, refund/respec, unlimited RP or runtime regranting. Preserve current profile v3 XP and **one RP per earned level** unless the owner separately approves an RP economics pass. Saved IDs from a newer/retired catalog remain safely handled as the existing resolver specifies.
- **Effect architecture:** Add a small set of explicit typed effect kinds and deployment-snapshot fields by domain. Tower damage and any new range/firing/handling changes must flow through the existing stat/build pipeline and correct Inspector/build previews. Do not change all base resources or edit authored purchased/rank level arrays to smuggle in research. Damage-percent effects should remain additive from authored baseline; use bounded order/rounding and deterministic selection.
- **Economy/resource conservation:** All Credits are paid/earned through the current authoritative services, including production and kill attribution. Factory production, courier shipments, Depot and Drone Station stocks cannot create rounds because research increased capacity. Any faster service uses atomic finite stock transfer. Research bonuses must respect power-island output/NoPower, heat, disabled states, tower sale, station destruction and construction lifecycle.
- **Fighters & waves:** Preserve FD4.1 independent nearest-in-range weapon target and movement threat anchor, FD5 finite FIFO docking and checkpoint conservation, shared targeting/LOS, F2 debug exclusion from official rewards, SPAWN3/4 source scheduling and successful wave ownership. Specialist target research must be opt-in and cannot overwrite movement/patrol policies. Research affecting wave previews is *read-only* and must not advance RNG or start/activate a wave.
- **Save compatibility:** Current progression stores stable purchased research IDs; current between-wave checkpoints store a frozen `ResearchDeployment`/relevant per-system runtime states. Plan any additive snapshot fields for new effect categories **before** source work; old saves with absent fields keep neutral zero bonuses, new saves restore exact researched values, and no load re-applies a modifier on top of an already modified stat/amount. Respect the application's existing content-fingerprint policy; no destructive migration or profile resets.
- **UI/UX:** Six visually distinguishable but compatible branches with name, cost, actual effect, prerequisite and lock state; no forced research purchases, changed menu navigation, new RP grant cheats, or all-six-branches-unlocked-on-day-one assumptions. Keep existing mouse/keyboard pan/zoom and the selected info panel useful with 25–37 nodes; initial fit/centering and small viewport navigation deserve a focused check.
- **Performance:** Research catalog validated/cached once at startup, profile bonuses captured once at mission deploy, consumers use cached numbers, no per-frame profile parsing/graph node scenes or repeated full-registry scans. Avoid duplicate feature-specific upgrade processors per tower or per drone.
- **Balance and releases:** All numerical effects/costs above are provisional. In particular one RP per player level and 78 RP for the full branch set may make deep nodes slow to reach. Owner decides whether to tune XP/RP rewards separately; no automatic RP inflation. EX1/EX2 are content/mechanic passes independent of late-game research inventory, multi-slot saving and broad campaign unlock changes.

## 6. Owner / Codex acceptance and status updates

1. **After EX1-A:** Research Room shows exactly six distinct first-ring branches around the original Core, including unchanged Citadel Master I and Battle Tower Weapons I at their original coordinates/price/effect. New nodes are truly purchasable and apply from the next deployment, with real Inspector/roster values. Existing paid research remains purchased for old profiles.
2. **After EX1-B/C:** All new nodes have unique stable IDs and coordinates, no cyclic/unknown prerequisites; locked/unaffordable/researched states and both fork lines display correctly; purchase an upstream node and see its intended descendants unlock without auto-purchasing them. Effect readouts are truthful, active mission snapshots unchanged by a later purchase. Verify save/restart, no duplicate payment, backwards-compatible checkpoint and actual gameplay changes.
3. **Domain check:** Try low power, depleted Depot/Station stock, one disabled tower and a wave preview containing a random/parallel source; research must not mint resources, bypass firing/LOS restrictions, count debug ships for Credits or corrupt post-wave state.
4. **Visual check (owner):** Read 36-node mock coordinates at gameplay UI sizes before shipping full EX2; ensure controls, selected info, legible hex text and ResetView stay practical. Graph should be visibly a *branching hex tree*, not six isolated columns of buttons.
5. **Minimal implementation verification:** Codex reads root README, `NOTES/CodePreferences.md`, this plan, feature docs and actual `dev` HEAD. Make one focused code job/subphase at a time; source/diff sanity plus at most one basic build if practical, no routine large fixtures/regressions. Update brief `NOTES/WORK.md` status and this plan's delivered stages, commit/push `dev` only. In-game gameplay, appearance and FPS validation remain with owner.

**Handoff request when ready:** “Implement **EX1-A**, the six-branch Research Room first ring, following `SYSTEMS/Progression/Research/RESEARCH_EXPANSION_IMPLEMENTATION_PLAN.md`. Preserve existing three nodes and IDs, make the four new effects real using the existing research pipeline, perform lightweight checks, document briefly and commit/push to `dev`.”

**This Markdown plan does not add any nodes, gameplay effects, UI elements or new research purchases to the current game.**
