## FD1-B status — 2026-11-06

**Source Implemented / Awaiting Owner Validation.** Station-owned paid fighters, launch/hold/recall/dock, elongated shared-style art and minimal checkpoint state are delivered. See [current usage](README.md). FD2–FD6 remain future work; no combat or resupply is active.

---

## FD1-A — Drone Station building and artwork (2026-11-06)

**SOURCE IMPLEMENTED / AWAITING OWNER VALIDATION.** The placeable, selectable and standard constructible **Drone Station** exists now under [`DRONES/Combat/Station/`](Station/README.md). Its native artwork and three build stages are **128×192**, matching **2 cells wide (X) by 3 cells long (Y)**. It is registered under Utility in `TOWERS/DefaultTowerCatalog.tres` as `drone_station`. Inspector-editable provisional config: **600 Credits**, **8s** build and **1,800 HP**. It inherits the shared 2×3 building placement, durability, construction drones, selling and checkpoint behaviour but is **non-operational as a fighter facility** until Codex integrates real drones. No custom station tick, slot inventory, supply or fighter state was introduced.

**FD1-B is now source implemented (owner validation pending); FD2 is next only when requested.** Do not repeat FD1-A/FD1-B. Keep the station's current scene/config/definition/artwork and append the first fighter + launch/dock/ownership logic, then FD2–FD6 as previously planned. The illustrated two landing bays are only art; future `Marker2D` anchors at local (-25,+1) and (+26,+1) are suggestions, not implemented. No gameplay/level wave edits or fighter activity claimed. A focused catalog-check addition is authored but not run. Full in-editor visual/footprint acceptance pending.

---

# FIGHTER — Player-owned combat/fighter drones and Drone Station

**Repository:** `ben0kay/godot-tower-defense` · **Branch:** `dev` only  
**Status:** **DESIGN / NOT IMPLEMENTED** (2026-11-06). Player-approved gameplay direction; **no gameplay authorization implied by this documentation change**. Implement in small named phases and preserve current construction, ammunition, towers, enemy AI, mission waves and checkpoint behaviour.

## 1. Vision and scope

The player can build a **Drone Station** and own **multiple persistent airborne Fighter Drones** that autonomously defend the map. Unlike construction or courier drones, fighters have **health, finite ammunition, weapons, tactical movement, two selectable control modes, docking/resupply and eventual vulnerability to enemy anti-air weapons**.

- **Auto-pilot:** search for the enemy posing the nearest threat to a Citadel, fly to intercept it, then fire while dynamically circling, weaving in **figure-eight paths** or kiting near the target. When the target dies/escapes, retask intelligently. Multiple drones can operate at once without constantly overlapping or wasting fire on one weak ship.
- **Auto-patrol:** select an individual fighter, click **point A** then **point B** on the battlefield. It repeatedly flies a smooth **figure-eight between/around those points**, looking for and shooting valid targets within its weapon range while staying near its assigned patrol area. It returns to the same route after a target or resupply interruption.
- **Station / resupply:** fighters launch from and return to their *own* Drone Station. Ammo is finite. At minimum, low/empty ammo prompts an orderly return, landing/docking, ammo refill, and relaunch. Damage and automatic repair at the station are also part of the design. A fighter destroyed in combat is gone; replacement/rebuild automation is a later explicit balance decision.
- **Future enemy counterplay:** anti-air-capable enemy ships can eventually detect, target and shoot these fighters. Ordinary enemy gunships currently target towers and should **not** automatically shoot fighters until an actual anti-air capability is authored.

Core idea: a **persistent, manageable friendly air wing**, not a one-time projectile or a re-skinned ammunition courier.

### Essential gameplay behaviours

| State | Gameplay |
| --- | --- |
| Docked/Ready | Fighter occupies an owned station slot, can receive orders and depart with ammo/health status. |
| Launch/Transit | Smooth physical acceleration, heading/turning, responsive contrail/engine effects. |
| Auto-pilot: Intercept | Prioritizes enemies with the **shortest remaining journey to a threatened Citadel**, not simply the closest enemy to the fighter; approaches chosen target. |
| Auto-pilot: Engage | Continually adjusts stand-off distance and weaves/orbits/kites around its moving target while attacking in range and with line of sight. |
| Auto-patrol: Circuit | Figure-eight route defined by the selected drone's two clicked world positions. Patrol can engage opportunistically without permanently abandoning its assigned course. |
| Return for service | Low ammunition, emergency health threshold, user recall, or station order initiates return; no ammunition magically refills in flight. |
| Docking/Service | Station consumes **actual** local ammunition stock; repairs health at an authored finite rate/cost; when ready, relaunches on the previous mode/order. |
| Disabled/Lost home | Safe fallback / idle / retarget or removal policy, clearly defined during implementation; never mint stock, duplicate drones or leave a fighter permanently bound to an invalid station. |
| Destroyed (future anti-air) | Proper damage/death event, no resurrection without payment and explicit replacement mechanism. |

Two player-facing modes are **Auto-pilot** and **Auto-patrol** only in the initial release. No manual WASD/mouse flight, independent defensive tower deployment or combat-drone ability cooldown is required.

## 2. Repository ownership — keep fighter-drone feature in DRONES

**Authoritative home:** `DRONES/Combat/`, with its own manager, fighter scene/config, movement, targeting, weapon adapter, station and UI/order-handling scripts. `DRONES/Shared/` continues to own **pure generic reusable drone body/flight presentation**. Existing live owners are deliberately elsewhere and must not be moved merely for folder neatness.

Proposed structure (names can be adjusted after actual source inspection):

```text
DRONES/
  README.md
  Shared/
    DroneFlightMotion.cs                # implemented generic cubic travel leg; extend narrowly if useful
    DroneBody.cs / DroneBody.tscn        # implemented shared body + engines/trail
    ...
  Transport/                           # implemented ammo CargoDrone; unchanged business owner
  Combat/
    README.md
    FIGHTER_DRONES_IMPLEMENTATION_PLAN.md   # THIS plan
    FighterDrone.cs / FighterDrone.tscn
    FighterDroneConfig.cs / DefaultFighterDroneConfig.tres
    FighterDroneManager.cs
    FighterDroneMovement.cs             # intercept, figure-eight, return/dock state
    FighterDroneTargeting.cs            # shared threat priority and per-drone target leases
    FighterDroneWeapon.cs               # friendly shot integration, no new damage resolver
    FighterDroneOrders.cs               # selected-drone modes and two-click waypoint sequence
    Artwork/                             # independent fighter hull/gun/trail visual assets
    Station/
      DroneStation.cs / DroneStation.tscn
      DroneStationConfig.cs / DefaultDroneStationConfig.tres
      DroneStationDefinition.tres
      Artwork/
    Saving/                              # fighter/station snapshots and validation, only when needed
    UI/                                  # selection, mode controls, A/B waypoint preview
```

- `TOWERS/DefaultTowerCatalog.tres`, placement, construction, Inspector, power or ammunition integrations may **reference** the station's scene/config, but station-specific gameplay scripts/visuals/data should live under `DRONES/Combat/Station/`. It is a placeable building and must follow the existing `TowerBase`/`TowerDefinition` contract rather than creating a competing building-placement framework. Its `TowerCategory` can use the existing suitable category (likely Utility or Support) without inventing a new category solely for one station.
- Reuse `DRONES/Shared/DroneFlightMotion` where it genuinely fits **finite outbound/return legs**, and `DroneBody` thruster/contrail conventions; a combat figure-eight/orbit is a **continuous steering manoeuvre**, not a finite cargo/constructor job. Do not overload `DroneFlightMotion` with combat-specific AI, weapons, health or station inventory. Fighters should look visually distinguishable from unarmed cyan cargo/builders (author a dedicated fighter sprite/gun/colour scheme).
- `TOWERS/ConstructionDrones/` keeps construction manager, holds, site work and save handling; `TOWERS/Economy/Ammunition/Shared/Logistics/` keeps all Factory→Depot→Tower courier cargo, reservations, slots and inventory accounting; `DRONES/Transport/` remains a passive courier role. **Never merge their dispatchers or global concurrency caps with fighter combat.**
- Use existing `ENEMIES/Shared/EnemyRegistry`/`EnemySpatialIndex`, `Enemy.RemainingJourneyDistance`, shared `TowerTargeting.Priority.First` semantics, `WorldGrid.HasWeaponLineOfSight`, `COMBAT/Damage` and existing friendly projectile/weapon presentation where compatible. Damage source attribution currently assumes a `TowerBase`; plan an **explicit safe drone attribution/XP policy**, rather than forging a dummy tower or extending shared code without tests.
- Fighter registry/HP need their own owner and lifecycle; do **not** register friendly drones as enemy ships or towers, or reuse `EnemyBulletSystem` for friendly attacks.

## 3. Flight and combat design (implementation guidance)

### Auto-pilot — map defence

1. On launch, acquire a threat using **remaining route/journey distance to its assigned Citadel** (the established 'First' policy), not Euclidean proximity to the drone or current HP. That gives appropriate priority to a nearby-to-breach enemy even when launched from far away. For multiple Citadels, respect each target's **existing assigned destination**, never silently choose a different Citadel.
2. Use a **single budgeted threat-acquisition service / cached candidate ranking** to distribute work across several fighters; do not let every drone full-scan 500+ enemies every physics frame. Stagger searches (example ~0.2–0.5s) and keep current targets while still valid; use a small hysteresis to avoid thrashing. Optionally apply a soft assignment cap to avoid all drones selecting one nearly dead scout, with emergency override for major threats.
3. Fly toward an interception/standoff position respecting aircraft speed, acceleration/deceleration and turn radius. Intercept direction should lead moving targets where feasible, but do not teleport or snap to their position. Use a smooth orbit/figure-eight/kiting controller: rounded lateral loops, shifting side/stand-off radius, bounded heading changes and no oscillating every physics tick. Relative weaving follows moving enemies rather than anchoring to an old coordinate.
4. Fire when target is within configured weapon range, detectable, unobstructed by existing terrain LOS and the muzzle is aligned; preserve physical projectile collision, target removal safety, damage/type and status rules. Retarget, re-intercept or resume sweeping when a target dies/arrives/escapes. A fighter is **airborne**: ground enemy navigation cells/tower occupancy should not force it to follow roads; nonetheless keep it inside map bounds and preserve weapon sight blocking by mountains/terrain.
5. While no enemies exist, permit a calm **guard/orbit near the station or defend-Citadel loiter** (Inspector choice later). No worldwide constant full scans while waves are idle.

### Auto-patrol — two-click figure-eight route

- The player selects **one** fighter (or chooses it from its station roster), selects **Auto-patrol**, clicks **point A**, then clicks **point B** in the world. Show non-blocking A marker, B preview and the resulting loop path *while editing*; right-click/Escape cancel should not destroy the fighter's previous accepted route. Only commit both points together, validate both within playable bounds; reject degenerate near-identical points and invalid world/UI clicks with concise feedback.
- Two points represent the centres/endpoints of a continuous **`∞` / figure-eight patrol path**, with a smooth crossover and rounded lobes around A and B. Speed around the path should stay **roughly world-distance constant** (arc-length parameterization/adaptive segment lengths), with believable banking/turn speed; do not use naive fixed-frequency sinusoid causing extremely slow motion on long patrols. Inspector controls: patrol width/loop radius, flight speed, turn speed, arrival tolerance and weapon acquire radius.
- Patrol acquisition uses a **limited zone around the patrol loop / fighter**. Fire on enemies entering range, optionally make a **short, bounded combat weave** or adjust facing, but always return to route instead of pursuing enemies across the whole map. Prefer a patrol leash/return-distance and finite chase duration with deterministic fallback. Keep attacks constrained by detection, LOS and shared combat rules.
- Show per-drone selected mode and both points in its HUD/Inspector; allow new A/B assignment, switch to Auto-pilot, and explicit Recall to station. Switching modes does not abandon the previous patrol route unless user overwrites it, so switching back can resume.
- Each drone has independent A/B assignments (not a single global player patrol). Optional multi-select/group orders are a **later** enhancement, not part of the first pass.

### Health, ammo, resupply and station

- Each fighter has `MaxHealth`, `CurrentHealth`, `Magazine/ReserveAmmo` (explicit design; initially one simple finite ammo reservoir is sufficient), weapon cooldown and relevant Inspector-tunable stats. HP is persistent between flights; returning to the station should **not** automatically restore it for free.
- The **Drone Station** provides persistent home ownership, finite fighter-slot capacity, landing/launch points, ammo holding capacity, resupply rate/time, repair rate/cost and status. Suggested initial station capacity **2–3 fighters** is provisional and should be Inspector-driven; source of purchased/constructed fighter units and replacement Credits must be decided within the implementation (recommend explicitly priced build/replacement). Keep drones distinct on-screen with individual HP/ammo/mode/status.
- **Ammo is conserved:** station refills must draw from real recorded stock. Prefer integrating the existing ammunition Factory/Depot production and courier system by registering Drone Station as an appropriate ammunition consumer/endpoint **if the current interfaces permit it**. Audit `AmmunitionLogistics` and `AmmoInventory` first; implement a minimal adapter, not a second parallel Factory/Depot network, nor magical station refills. During initial standalone testing a clearly **debug-only** finite supply can be allowed, never hidden as production mechanics.
- Example refill trigger: ammo <= 20–25% or out of ammo, configurable; optional low-HP return (e.g. <= 35%, tunable). Once recalled, stop spending ammo, return via continuous flight, dock, unload/load/repair within station rates and capacity, then rejoin previous mission. If station is temporarily empty, drone waits docked; no spin, endless dispatch or phantom stock. Manual **Recall** works even with full ammo.
- Station and drone destruction/disconnection: cleanup must be explicit and idempotent (rehome to another station only if implemented and capacity exists; otherwise safe loiter/retire rather than teleport). If a drone is destroyed later, remove it once, release slot and stop attacking. No auto-replacement without an authored cost/order.
- Damaged drones **future anti-air** must use real typed damage, HP and removal events, ideally a separate friendly aerial target/registry that enemy weapons can explicitly opt into. Do not globally change the enemy tower-targeting path or force every hostile projectile to hit drones by default.

## 4. Phased implementation plan for Codex

| Phase | Work | Deliverable / boundaries |
| --- | --- | --- |
| **FD0 — Contracts and station design** | Confirm tower placement/catalog station requirements, existing ammo endpoint contracts, damage attribution, friendly bullet/LOS services, selection input and between-wave saving. | Small reference contracts and decision notes; no game code. |
| **FD1 — Drone Station + one fighter** | Place station with proper construction/ownership, one persistent fighter scene/manager and launch/return; unique identity, HP/ammo state, independent visual model, bounds and slot ownership. | Working station + fighter with return/dock; no weapons/autopilot yet. Allow configurable multi-slot capacity. |
| **FD2 — Auto-pilot pursuit + aerial manoeuvres** | Shared threat ranking by smallest remaining journey to Citadel; staggered target acquisition, smooth intercept, target-relative figure-eight/kiting, idle guard and target-loss recovery. | Several fighters can fly/track without full-list polling per drone. Weapons can initially be disabled for movement acceptance. |
| **FD3 — Auto-patrol and selection UI** | Select fighter; choose mode; two map clicks for A/B; show figure-eight preview; defend route, leash, cancel/edit, repeat circuit; individual orders. | Working patrol movement and mode switching, independent per-drone A/B. No click interference with tower placement. |
| **FD4 — Fighter weapons** | Real muzzle, turn/aim/LOS, projectile/shot system, finite ammo and cooldown; damage/shield attribution/XP policy; clean hit feedback. | Fighter shoots enemies in both modes, ammo decreases only on accepted shots; uses shared combat, no independent damage math or per-frame enemy enumeration. |
| **FD5 — Station resupply and repair** | Ammo stock + proper existing logistical supply adapter, finite dock/service capacity/rates, health restoration policy, low-resource recalls, waiting/relaunch, interruption/cleanup. | Genuine ammo consumption and repair costs, no stock duplication, station loss safety. |
| **FD6 — Persistence, tests, balance and later AA seam** | Integrate existing S0–S3 between-wave save/checkpoint; snapshot station/drone ownership, orders/A/B, HP/ammo, dock/service or flight state and true purchases. Test deployment/retry/load and battle performance. Document future AA target registry. | End-to-end owner-approved baseline; **anti-air itself is not implemented** unless separately requested. |

**Suggested delivery cadence:** FD0+FD1 as first Codex task; then FD2, FD3, FD4, FD5, FD6. If a phase would make a placed station permanently invalid to existing saves, implement the minimum necessary saving integration *before* shipping that phase. Avoid a long-lived feature that corrupts the game's existing S0–S3 checkpoint flow.

## 5. Performance + deterministic behaviour

- A few active fighter controllers can update physics for smooth flight **only while deployed**; inactive/docked bodies do not tick. A central manager budgets retargeting/candidate work and uses cached EnemyRegistry spatial queries; **never O(fighters × all-enemies × frame)**.
- Reuse weapon projectile pools/batching where supported, not one custom ticking bullet node per fighter if the shared friendly system is available. Avoid a global raycast per drone per frame; validate LOS when target acquisition or actual shots require it. Hit/breach bookkeeping stays with existing world services.
- Avoid tens of thousands of individually tessellated patrol route samples: calculate/refresh curve once at order change and reuse bounded samples/arc-length lookup. No new NavigationServer/A* routes for airborne figure-eight loops. Clamp waypoints, soft map limits and collision/LOS rules.
- Targets, orders and arrivals need stable IDs and clear lifecycle detach, no NodePath saved as a fragile runtime reference; prevent ghost drones after station sale, mission reset or load. No duplicated ammo across station inventory, departing fighters, dock reservations and couriers.
- Pause freezes fighter movement, weapon cooldowns, service and targeting; no hidden background fire/resupply while the gameplay clock is frozen. Idle warpless motion and thrusters follow active states.
- Validate **multiple fighters simultaneously**, distinct routes, target deaths, stealth detection, mountain line of sight, tanky targets, station full/empty, pause, building sale/EMP, at most 500+ enemies and tower selection conflicts using short focused tests; leave extensive playtesting to owner.

## 6. Early balance knobs (provisional only, no implied final values)

**Fighter:** HP, move/cruise speed, turn speed, acceleration/deceleration, target scan interval, acquisition radius, weapon range/damage/fire rate/projectile speed, ammo capacity, return threshold, low-HP threshold, standoff radius, figure-eight lobe width, aggressiveness/target hysteresis, patrol leash and trail colour.

**Station:** building Credits/footprint/construction time, fighter capacity, fighter purchase/replacement costs, ammo storage, incoming logistics threshold, launch/dock spacing, resupply rounds/s, repair HP/s, repair resource/credit cost and power participation (integrate existing power rules only if approved). Default station capacity 2–3 is only a suggested starting point, **not an implementation requirement**.

**Future:** multiple fighter classes/roles (interceptor, support, heavy gunship), escort/follow selection, formation groups, drone experience/upgrades, drones' anti-air turret vulnerabilities and replacement automation. These are not authorized in FD1–FD6.

## 7. UX and safety acceptance

- Station is buildable and selectable through existing palette/Inspector rules; number of fighter slots, ammo stock and docked/active/servicing state are understandable without inspecting the scene tree.
- Click one fighter, choose `AUTO-PILOT` or `AUTO-PATROL`, assign A and B with a clear route preview; cannot accidentally build a tower during patrol point selection. Right-click/Escape cancels edit; pointer over UI never creates a waypoint.
- Auto-pilot picks enemy nearest **in remaining journey to an assigned Citadel**, not simply nearest to the fighter; target priority is stable and shared across multiple fighters.
- Auto-patrol repeatedly makes a **legible, smooth figure-eight**, fires at enemies while flying and stays within an authored leash.
- Low-ammo fighter visibly returns, docks, waits for stock if needed, refills an actual finite amount and relaunches on its previous orders. HP persists and repairs gradually; no instant free refill, unlimited ammunition or loss of assignment.
- Independently selected fighters, simultaneous station drone slots and removal never confuse cargo/builders or enemy targeting; no friendly fire by default.
- Between-wave saving/loading of active patrol routes and station resources is supported before feature is declared complete. No mid-combat save, wave resource modification or extra Citadel damage rules.
- Future hostile anti-air targets **only explicitly attackable flying drones** rather than silently all Drone/Builder/Cargo instances.

## 8. Out-of-scope and important distinction from earlier note

The earlier `DRONES/Combat/README.md` described an **optional short-lived player combat ability** that flies a chosen corridor. That is **not** this new request. This plan is now the authoritative **persistent fighter squad / station / auto-pilot / auto-patrol** direction. Preserve the old one-off ability as a **separate future concept**, not the assumed first fighter implementation. Do not mix it into the logistics system or treat station-based drone fighters as consumable missiles.

**Status remains planning only** until implementation is requested. After each implemented FD phase, update the owning docs/backlog with exact source status and owner validation, preserving independent role boundaries.
