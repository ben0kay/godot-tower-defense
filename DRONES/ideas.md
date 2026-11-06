# DRONES — Future ideas for a lively, operational base

**Status:** IDEAS / DESIGN ONLY — November 6, 2026. Nothing in this file authorizes implementation or changes existing game behaviour. Use small, explicitly approved follow-on phases if an idea is selected. These ideas are deliberately separate from the active [FD fighter phases](Combat/FIGHTER_DRONES_IMPLEMENTATION_PLAN.md) and the [DRONE2 shared-flight plan](DRONE2_UNIFIED_DRONES_IMPLEMENTATION_PLAN.md).

## Vision

Make the Citadel feel like a **living defensive military base**. During preparation, construction, active waves and recovery, small aircraft can launch, navigate, perform meaningful field work and return. The important visual impression is **a coordinated ecosystem of drones** — engineering, logistics, fighters and future specialist support — rather than constant decorative clutter or a costly full simulation.

Existing foundations (verified on `dev`): Citadel-sourced `TOWERS/ConstructionDrones/` already performs paid build/upgrade jobs; `DRONES/Transport/` has physically visible ammo couriers owned by `TOWERS/Economy/Ammunition/Shared/Logistics/`; `DRONES/Combat/` has paid persistent fighter drones with FD1-B/FD2 autopilot source implemented; the **Drone Station is a building** under `TOWERS/Utility/DroneStation/`. `TOWERS/Support/RepairTower/` already implements a powered Restoration Tower that repairs tower hulls. `SYSTEMS/Progression/Research/` has a **campaign Research Room** with paid permanent nodes, but **no approved drone-repair or maintenance research effect** yet. Do not confuse that screen with a new physical “Research Station” building — such a building would be a separate design decision.

## Owner ideas — important future mechanics

### IDEA-R1 — Citadel-launched field repair drones

**Goal:** Research unlocks **repair drones that fly out from the Citadel** to damaged towers anywhere valid on the battlefield. Visually, they swoop out with engine contrails, decelerate at the tower, circle to a service position and project a focused blue/cyan repair beam, little welding arcs and controlled sparks. When finished, they bank away and fly back to their Citadel.

- **Trigger:** tower health falls below a configurable threshold (or a player explicitly requests repair). Queue only real, damaged, alive, repair-eligible `TowerBase` instances. Decide explicitly whether under-construction/upgrading towers qualify; default proposal is **no**, to avoid interfering with existing work states.
- **Target selection:** first address emergencies using health percentage and urgency, with nearest-Citadel travel distance as a tie-breaker. An authorable service priority can be added later; do not dispatch several full repair drones to one tower without useful work. A task is reserved once, can be canceled on destruction/full repair, and re-evaluates safely when other repair systems heal it.
- **Source:** the **Citadel**, not the Fighter Drone Station and not automatically the existing Restoration Tower. If there are multiple Citadels, choose a valid owning/live source deterministically; fighters, builders and ammo couriers retain their separate owners and dispatch limits.
- **Real repair:** reuse authoritative `TowerBase` health restoration / event hooks and shared power, economy or resource accounting when the final costs are approved. A service flight has finite repair throughput/amount and a bounded service duration; **do not grant free full healing on contact** unless explicitly selected as a balance rule. No repair or cost may be applied twice on callbacks, pause or load.
- **Coexistence with Restoration Tower:** it remains the **local continuous-range repair turret**; field drones are **remote, travel-limited, limited-slot emergency assistance**. They should not make the Restoration Tower pointless. Its own heat/energy/aiming remain untouched.
- **Research gate:** unlock via a new approved campaign Research Room node (suggested name **Field Engineering Drones I**). The current research effect enum only knows Citadel HP and TowerDamage%; a *new explicit effect kind and frozen deployment snapshot* would be needed. If the owner eventually adds a placeable Research Station, it is a separate `TOWERS/` building that integrates with the same progression policy, not a second free-standing research economy.
- **Config candidates:** research ID, max Citadel service drones, repair dispatch threshold, emergency priority, HP/s, maximum HP restored per sortie, home range, travel/hover speed, recharge/cooldown, service cost, repair beam and spark profile.
- **Player feedback:** selected tower indicates `Repair requested / Drone en route / Repairing` without covering its main HP/energy UI; limited visual destination marker only when useful. Drone visibly approaches from the Citadel, not a spawned-in-place particle.
- **Save/perf:** reuse the world's construction-drone route/visual primitives and pooled artwork as *presentation*, while giving repair missions their own job ownership/slots. Subscribe to actual damage and job completion, use a bounded priority queue, no per-frame tower scans or duplicate service jobs. S0–S3 between-wave checkpoints must conserve any paid materials and in-flight task state (or cancel/refund with an explicit policy).

**Suggested first implementation:** one or two repair drones launched from a single Citadel, emergency hull-only repairs, visible return and an optional research-gated enable. Add multi-Citadel and more complex costs only after that works.

### IDEA-M1 — Tower maintenance / wear-and-tear drones

**Goal:** Eventually, towers accrue a **separate mechanical condition/wear state** and research-unlocked maintenance drones service them before they deteriorate. This is *not* the same thing as damaged hull HP or the existing heat/overheating system.

- **Wear sources (proposed):** actual weapon operation/firing time, heavy sustained usage, repeated overloads or other explicitly authored stress. **Avoid unavoidable wear simply from sitting idle** at the start; idle passive structures should not turn into chores without a deliberate economy/gameplay decision.
- **Early gameplay target:** condition from 100% down slowly; *little or no penalty for normal short waves*. Only sufficiently poor condition eventually yields subtle mechanical effects (e.g. slightly longer reload / small reduction in efficiency), before severe wear is considered as a separate balancing pass. Do not introduce random catastrophic failures, HP erosion, permanent disabling or an oppressive repair tax by default.
- **Flight loop:** Citadel sends a mechanical maintenance drone on its own dispatch queue. The drone lands/hovers beside a tower and performs a short **inspection scan → a few articulated welding/cooling/screw-tool effects → progress completion → return**. It raises *condition*, **not HP**. Visuals should reflect the actual tower being worked on.
- **Research unlock:** suggested node **Automated Maintenance I**, potentially requiring **Field Engineering Drones I**. This research unlocks the service *capability*; a future upgraded level could reduce job duration or boost active maintenance slots. Use existing permanent research ownership where suitable, but do not pretend either node already exists.
- **Condition model:** define a value with bounded 0–100 semantics, authoritative source owner and save/load rules. Make it Inspector-tunable per tower family (some may opt out) and show an unobtrusive **CONDITION** indicator when selected. Specify how damage/EMP/heat/corrosion differ, and which system actually consumes resources for each.
- **Queue priority:** very worn/critical towers first; do not interrupt a tower's construction/upgrade or create visual fights with its existing beam, overheat and repair animation. Services can be canceled and requeued when the tower dies, is sold or a mission ends.
- **Costs:** finite service capacity and actual approved resource/energy costs, not invisible economic minting. Maintain predictable, player-readable balance and a “maintenance disabled / simplified” option if this becomes a sandbox setting.
- **Performance:** wear only advances on real relevant usage events or a bounded shared accumulator, **never a new `_Process` for each tower**. Dispatch/condition presentation is event-driven; idle drones sleep.

**Important distinction:** `Hull HP` = combat damage, healed by repair. `Heat` = temporary sustained-operation limit, cooled by existing heat rules. `Condition` = long-term mechanical wear, restored by maintenance. Separate data, source rules, UI and effects.

### IDEA-RM — Research progression and priority rules

- Use the existing **permanent campaign Research Room** as the default unlock source until/unless a physical Research Station is separately approved.
- Proposed progression: **Field Engineering Drones I** (unlock 1 Citadel repair slot), **Field Engineering II** (+1 slot or faster repairs), **Automated Maintenance I** (unlock condition service), **Predictive Diagnostics** (dispatch before serious wear), **Rapid Response** (faster launch/return with meaningful costs). **Names, XP/RP prices and effects are illustrative**, not pre-authorized balance.
- Distinguish permanent research from **battle-specific station capacity**. An unlock should not spawn a free tower or fighter, consume a permanent purchase twice, or retroactively rewrite the active mission's immutable research snapshot.
- Reserve an *emergency override* (player-click priority or urgent below-threshold request) so one critically damaged tower gets help even while the maintenance queue is busy. If services share Citadel docks later, prioritize life-saving hull repair over cosmetics.
- **Scheduling:** share low-level flight presentation and perhaps a bounded service-job interface, **not** the existing construction/drone cargo/fighter business manager. Repair/maintenance ownership should be modular, with single-consumer cost application.

## Extra ideas — creating a lively battlefield (assistant suggestions)

These are optional ideas, not requested functionality yet. Prefer effects which make **actual work visible**, and add purely cosmetic traffic only if the battlefield would otherwise feel empty.

| Idea | On-screen impression | Gameplay value / safe scope |
| --- | --- | --- |
| **I1 — Recon/scanner drones** | Small scouts leave the Citadel or a future radar facility, perform a circular sweep, reveal a scanner cone, then fly home. | Later provide temporary stealth-detection information or portal warning, using the existing stealth/reveal systems. Avoid revealing all enemies for free. |
| **I2 — Salvage/recovery drones** | Tiny claw drones approach wreckage, pick up a scrap piece, return with a visible strapped load and deposit it. | Later connect to a *new explicitly balanced* salvage resource. **Do not award duplicate kill Credits**, and do not create permanent wreckage nodes for every ship. |
| **I3 — Ammunition convoy choreography** | Two or three existing ammo deliveries depart a depot in slightly offset, organic arcs, then separate to supply different towers. | Mostly presentation: retain current finite cargo reservation, flight timing and dispatch. No new ammo or duplicate logistics system. |
| **I4 — Mobile cooling/overheat assistance** | Service drone hovers by an overheated turret, sprays a faint cooling plume and takes off. | Only if approved as a separate researched gameplay option. Must respect existing heat disable/restart rules; never silently reset heat. |
| **I5 — Emergency firefighting / stabilizer crews** | Damaged structures spark or smoke *briefly*; a service drone deploys a blue stabilizing cone as a visual “response to damage.” | Initially feedback-only. Future shield/repair mechanics require explicit costs and cannot bypass `COMBAT/Damage`. |
| **I6 — Hangar activity** | Fighter hangar lights up on launch, bay shutters open, service arms move, runway strobes guide returning fighters. | High visual reward with low CPU cost, driven by actual launch/dock/service events at `TOWERS/Utility/DroneStation/`. No independent physics tick for decorative doors. |
| **I7 — Citadel traffic control** | Soft signal lights, subtle launch-pad pulse, one or two drones taxiing/loitering before a genuine job. | Tiny event-driven cinematic cues when assignments begin/end. No infinite crowd of idle agents; share small visual pools. |
| **I8 — Drone formation and near-miss avoidance** | Fighters departing together fan out naturally; returning and supply drones use slightly offset approaches so they do not clip visually. | Cosmetic local separation/arrival offsets, bounded by actual operational slots; do not introduce global full-world collision avoidance. |
| **I9 — Tactical support / relay drone** | A specialist drone parks in the air near a group of towers with an unobtrusive hovering blue signal ring. | Much later, opt-in short-lived targeting/telemetry support. Never create free power from nothing or duplicate the existing power relays. |
| **I10 — Between-wave recovery flights** | A few visible drone sorties clean up damage, repair, restock and return before the next assault. | Use real queued jobs and the already-existing wave countdown; no automatic free resets or forced waits before the next wave. |

## Visual language — the “lively base” look

- **Different silhouettes and colour accents by job:** cyan/white engineering, blue-green logistics, focused amber mechanical maintenance, clean turquoise repair beams, faster winged fighters. Clear intent at game zoom, not a screen full of unreadable particles.
- **Purposeful motion:** launch with acceleration, smooth curving flight, braking near target, 0.2–0.5s hover/acquisition, occasional slight change of working angle, work beam, peel away, return to the Citadel. Leverage `DRONES/Shared/` movement and contrail conventions without forcing cargo and repair logic together.
- **Visible job confirmation:** the player should be able to follow a damaged tower → dispatch → travel → work → result → return. Ambient flourishes should complement real work rather than obscure damage, health, enemies or projectiles.
- **Occasional micro-interactions:** a service drone pauses, tiny status LEDs pulse, a gantry swivels, another drone banks around it. Don't use heavy per-ship collision/physics for visual niceties.
- **Balanced intensity:** calm activity before waves; urgent repair bursts after heavy combat; no cloud of pointless drones idling over every building. Optional VFX quality profiles, reduced-motion controls, camera-visibility culling and shared small pools.

## Architecture and implementation guardrails

1. **Folder ownership:** `DRONES/` owns drone-specific motion/presentation; future Citadel support drones can have `DRONES/Support/` (or another approved feature folder). Citadel source/dispatch integration must be coordinated with `WORLD/OBJECTIVES/Citadel/`. **Buildings live under `TOWERS/`**, including `TOWERS/Utility/DroneStation/`, future repair depots or any physical Research Station.
2. **Existing mechanics are authoritative:** repair existing tower HP via its health API; wear would be a new explicit component, not a duplicate HP/heat/EMP bar. Existing ConstructionDrones, FighterDroneManager, ammo cargo and local Restoration Tower keep their current ownership.
3. **Research:** `SYSTEMS/Progression/Research/` already implements permanent Research Room nodes. New unlock effects must be **explicit additions** with stable IDs, campaign save compatibility, frozen deployment behaviour and validated fallbacks; don't invent a second Research Service.
4. **Flight/job budget:** route creation once per job, finite Citadel service slots, bounded dispatch/retarget intervals, pooled/sleeping visuals. Do not perform every-tower health or wear scans every frame. Plan for 500+ enemies and multiple stations/Citadels.
5. **Economy and saves:** decide and document finite cost and stock sources before a service alters gameplay state; preserve S0–S3 between-wave snapshots and double-spend/duplicate-application safety on restore, cancel, death and building sale.
6. **Avoid punishment creep:** maintenance should make a base feel cared for and rewards meaningful upgrades. Early gameplay should remain approachable; maintenance ought not constantly demand micromanagement or arbitrarily disable a defence line.
7. **No live changes now:** no Research definitions, station files, repair behaviours, drone code, new wear variables, level content, upgrades or balance settings are implemented by this document.

## Suggested next independent design passes (only when approved)

- **DR-IDEA0 — Service contracts:** decide hull repair costs/rates, eligible targets, Citadel slot limits, how Research Room unlocks are represented and how existing repair tower effects coexist. Documentation/prototypes only.
- **DR-IDEA1 — Citadel field repair:** one real pooled outbound/working/return job, threshold and emergency priority, actual HP restoration, finite costs, event-based dispatch and lightweight VFX. Add the approved research gate.
- **DR-IDEA2 — Shared drone-service polish:** visual service choreography, arrival/return audio hooks, bay/flight control polish, minimal selected-tower and Citadel status indicators.
- **DR-IDEA3 — Condition/wear first pass:** optional and independently balanced tower-condition state + Inspector presentation, no maintenance missions yet; prove passive-tower and normal-wave behaviour is not unpleasant.
- **DR-IDEA4 — Maintenance crews:** research-gated condition restoration flights with separate work effects and economics; no hidden hull repair.
- **DR-IDEA5 — Extras:** select I1–I10 one at a time; don't add them all as a single giant phase.

**Decision to revisit when implementing:** Are remote hull repairs free but limited by Citadel capacitor energy, paid in Credits/resources, or supplied by a future maintenance inventory? What types of operational activity actually cause wear? These choices are intentionally open; do not guess them in code.
