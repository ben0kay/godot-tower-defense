## CD4 — construction drones also service paid upgrades (2026-10-27)

**Implemented in source; Godot/.NET runtime and rendered appearance still require a local smoke check.** Positive-duration paid tower upgrades now use the existing Citadel construction drone manager: payment and the **UPGRADING** disable state take effect immediately; a pooled drone is queued from the nearest available Citadel; the **upgrade progress clock remains held at 0** during travel/deployment; the cyan beam finishes deploying before that specific work reservation is released; the shared `ConstructionWorkManager` then advances the existing paid-tier `WorkSeconds`. The tower stays combat-disabled throughout, new tier stats/sprites commit at 100%, and the drone flies home independently. This is the **same** bounded FIFO/flight/hover/beam/return lifecycle as initial construction, not a separate upgrade-drone class or work timer.

`ConstructionDroneMasterConfig.ServiceUpgrades` is a separate Inspector toggle, **enabled** in `DefaultConstructionDroneConfig.tres`. Turning it off preserves immediate paid upgrade timing without a builder; turning off `ShowVisuals` hides only the artwork, **not the arrival gate**. With `Enabled=false`, absent builder service, or an ordinary-mode failed dispatch, the paid upgrade simply begins its normal timer so money cannot be stranded. Strict `RequireDroneToBuild` still explicitly holds until a valid drone becomes available. Zero-second upgrades are immediate; construction and upgrades share cap/dispatch/FIFO and keep independent holds. No per-tower process loop was added.

The selected Inspector says `AWAITING UPGRADE DRONE → N` during flight, then `UPGRADING → N` with progress. The Battle Tower pilot now uses additional **35% and 70%** upgrade-stage SVGs followed by its separate finished tier base+recoiling gun; no Level1 foundation reset. Other towers inherit the shared drone gate; only Battle currently has authored tier-progress visuals. Initial build and ammo-transport ownership are unchanged. Held-upgrade work and drone phases are supported in existing S2/S3 checkpoint capture/restore, including beaming and returning states; no new save fields or file format.

Focused regression checks were updated for the old upgrade expectation and new arrival, HUD, tier artwork, capacity/toggle and two-purchase cases. Manual Godot test pending for same-game builds/upgrades, checkpoint load during outbound flight and beam, and return visuals. [Design pass](../Shared/Upgrades/UPGRADE_DISABLE_TIER_ART_PLAN.md).

---

## Slower construction drone flight — 2026-10-27

Builder outbound and return maximum flight speed is now **280 px/s**, down from **420 px/s** (one-third slower). The shared `TravelSpeed` Inspector setting is explicitly serialized in `DefaultConstructionDroneConfig.tres`, and `ConstructionDroneMasterConfig.cs` plus the motion fallback use the same 280 px/s baseline. Cubic paths, acceleration/braking, turning, orbit movement, deployment and construction durations are unchanged; the existing movement model naturally extends flight times. Already-in-progress saved flight durations retain their checkpoint state until their next flight leg. Gameplay/desktop review pending.

---

## Organic builder flight and orbit — 2026-10-26

Builders now follow cubic flight paths with eased acceleration/braking, sprite-only travel heading,
then hover and periodically glide to a randomized angle around an elevated footprint-sized ellipse.
The beam remains fixed on the site while the sprite turns. Return flights bend along the departure
heading toward the Citadel and brake on arrival. Work/deployment clocks retain their existing ownership;
hover/orbit never interrupts construction. Actual travel time naturally follows the new flight path.

`ConstructionDroneMotion.cs` is one small plain state per assignment, advanced by the same active-only
manager. No navigation queries, per-drone ticks, tweens or extra visual nodes. Parked drones remain dormant.
Settings are in DefaultConstructionDroneConfig.tres: curve 100px, turning 240°/s, minimum orbit radius
28px (grows with footprint), orbit speed 55°/s, hover rests 0.6–1.4s. Bobbing uses independent stable phase;
orbit choices use a per-assignment PRNG, never wave/combat randomness.

Checkpoint capture retains curve controls/progress, heading, orbit/rest state and PRNG state. Older
v2 builder records without Motion remain readable and blend from their saved pose into the new model;
metadata checks still validate all supplied movement values. Existing content fingerprint rules apply.

Validation: source review and diff checks only; .NET/Godot are unavailable here. Live flight/beam appearance,
arrival, interruption/return and save-during-orbit verification remain local. No extensive suite/profiling run.

---

## Between-wave checkpoint integration — 2026-10-26

S2/S3 now provide source-owned capture/restore seams for this feature through
`SYSTEMS/Saving/CampaignSession.cs` and feature-local saving helpers. Esc Save/Load and Campaign
Continue use the selected profile's checkpoint; combat Save explains **Unavailable during combat**.
Historical saving-unimplemented statements below are superseded. See `SYSTEMS/Saving/README.md`
for field ownership, restored cargo/builders, original reward tickets and compatibility limits.
Source/basic diff review completed; .NET/Godot build/runtime acceptance remains pending locally.

---

## CD3 visual-only hologram polish — 2026-10-25

Implemented scrolling scan bands, soft pulsing cyan beam, enlarged nozzle and footprint-centered glow in the existing pooled drone renderer. Added one central, capped 64-entry completion-flash renderer which emits a .28-second cyan ring only after a paid initial build completes successfully. There are no extra per-drone processing callbacks, new GPU emitters, scene nodes per flash, modifications to the work clock, flight speed, damage, placement, Inspector or research.

`ConstructionDroneMasterConfig.cs` / `DefaultConstructionDroneConfig.tres` expose the beam scan-band count (0–8, default 4), scan cycles/s (1.5), pulse Hz (2), pulse depth (.13), completion-flash enabled, duration (.28s) and opacity (.65). All are visual-only; `ShowVisuals=false` skips beam and flashes while maintaining unchanged arrival/work timing. This is deliberately limited to CD3 visual work, not the broader outstanding CD3 desktop/GPU acceptance or optional HUD ETA/sounds/research.

Focused production tests now check animated time, one completion flash (independent of returning drone), idle cleanup and visuals-off suppression. **Godot/.NET build and headless execution still require verification if unavailable in the authoring environment; live-screen look and 20-beam GPU profiling remain local tasks.** See `Visuals/README.md`.

---

## CD1/CD2 implemented — 2026-10-21

Paid positive-duration **initial** construction now waits for a Citadel builder. The site/payment/full-footprint claim and stage-0 art happen immediately. A cyan drone flies from the nearest available Citadel, deploys its holographic cone, releases that site's work hold exactly once, hovers while the original build clock runs, then returns. Upgrades, zero-second builds and direct scene instances retain their previous behavior.

### Ownership and files

- `ConstructionDroneMasterConfig.cs` / `DefaultConstructionDroneConfig.tres`: shared Inspector controls. Defaults: **Enabled=true, ShowVisuals=true, RequireDroneToBuild=false, cap8, dispatch budget4/tick, speed280px/s, arrival8px, deployment0.25s, hover12px, height48px, beam alpha0.26**. Numeric settings are sanitized. Cap clamps1–64; dispatch budget1–16.
- `ConstructionDroneManager.cs`: one active-only service in shared `WORLD/World.tscn`, synchronously bound by placement Ready. Only successful `TowerPlaced` queues work; the optional hold is registered **inside the paid commit before AddChild**. One per-site owner/token rejects duplicates and stale arrival. FIFO pending assignments stay held; outbound/building/returning drones all consume capacity. Reducing cap never cancels existing assignments.
- `ConstructionDrone.cs` / `.tscn`: imported shared36×28 cyan drone SVG plus one beam child. Neither drone nor beam has a per-frame callback. One manager advances active flights and beaming poses; lazy pooled visuals are created only when concurrency grows, up to64 roots. Parked visuals hide and do no processing. A normal drone uses **three nodes total** (root, sprite, beam); no fixed permanent flying fleet is pre-created.
- `Visuals/ConstructionBeam.cs`: retained four-vertex/color buffers, soft cyan cone, thin bright edges, nozzle and three lattice lines. No particles, custom shader, light, physics body, navigation request or per-frame arrays. Footprint-derived width/height and bounded hover support 1×1/2×2/3×2 and larger future sites. Target remains at the site's existing full footprint center.
- `TowerBase` exposes AwaitingConstructionDrone and a narrow owner/token release seam. `ConstructionWorkManager` tracks held jobs separately from its active timer batches: **held-only clocks sleep and visit zero sites**, while independent work/upgrades continue. Release adds only that job to the ordinary clock. It never advances time or completes a building.

### Timing, homes and fallback

Nearest available non-destroyed registered Citadel wins by world-space distance; stable objective number breaks ties. An optional `DroneLaunch` Node2D/Marker2D under the Citadel root supplies the exact launch point; otherwise its existing footprint center is transformed to world space. Builders fly directly through the sky, independent of terrain/enemies/ammunition logistics. No map scenes require modification.

Transitions: **queued/held → flying/held → deploying/held → beaming/work released → returning → parked**. Manager physics priority runs after the work clock, so flight/deployment delta cannot be credited as earlier construction time. Excess flight-tick time is discarded at transitions. The beam is fully deployed before release; original work events/clock remain authoritative. Completion immediately hides the beam and activates the tower, without waiting for return. Source loss after work begins does not interrupt work; returning selects a surviving home or parks safely if none exists.

Missing Citadel, invalid/missing drone scene, failed visual creation or source loss during outbound/deployment warns once per battlefield and normally releases construction without a drone. Strict RequireDroneToBuild keeps an unreleased job held and retries at most every0.25s using the bounded dispatcher. It is a testing/design switch, not the default. Tower removal/death cancels its assignment/beam and frees capacity; removal of the service itself safely releases orphan holds without completing work. Unload detaches event listeners and drops assignment/pool references.

**ShowVisuals=false preserves all flight/deployment timing. Enabled=false releases current holds and preserves the ordinary timed-build behavior.** Pause freezes both flight and work; actual mission Results pause is covered. The existing Inspector now minimally distinguishes `AWAITING CONSTRUCTION DRONE` from `CONSTRUCTING`; its size/layout is unchanged. Remaining time stays fixed until arrival. No ETA, sounds, research/hangars, combat targeting/destructible drones, upgrade builders or save integration are added. Further CD3 UX/desktop polish remains pending.

### Observed validation

Godot .NET build/import succeeded with zero C# warnings/errors. `TESTS/Towers/ConstructionDroneChecks.tscn` passes paid success/failure/duplicate/token checks on both main maps, held-clock sleep/large-delta immunity, independent work, beam/return and pool reuse, pause, visuals-off timing, FIFO/cap reduction,2×2 Depot/3×2 Factory, tower death/removal, missing/wrong scene, strict retry, lost/missing/queued-removal homes, real multi-Citadel tie/marker/return reroute, zero-distance arrival, zero-duration/direct compatibility, manager removal, actual defeat/Results freeze and20-build stress. Runtime-only broad foundations make large-building tests independent of narrow authored maze walls; saved terrain is untouched.

Observed CPU-only samples:20 active flights over120 central ticks **2.165ms total /40 managed bytes** (Stopwatch allocation);20 active beams over600 central ticks **8.576ms total /0 managed bytes**. Pool grew to20 drone roots (60 scene nodes), with no added A* calculations. This excludes draw callbacks, GPU cost and normal enemy simulation; not an FPS guarantee. F4 profiling includes dispatcher CPU in the existing **Friendly towers** row, without a new debugger/key.

`FootprintChecks` passes real 6/4-cell placement, payment/rollback, selection, navigation and500-enemy placement stress. `PerformanceOverlayChecks` passes all three battlefields. Existing ConstructionChecks passes source/timer/pause/callback and Battle artwork groups, then stops at **Could not purchase ammo_factory** in the older map-dependent timed-upgrade fixture. The same failure was reproduced on pre-feature619573d production code with only the same test-layer search correction; it is not claimed fixed or passed. Ordinary AmmoLoopFixture checks explicitly disable builders, keeping their prior timer contracts, and now search all painted layers so valid solid foundations are not ignored.

**Desktop review still required:** imported drone/bright cyan beam at normal zoom, stage art visibility, return animation/overlap,1920×1080 and960×540 presentation, and rendered20-build GPU cost. Headless correctness does not verify these visuals. CD1/CD2 source and runtime foundation are complete; CD3's remaining desktop/UX polish is not marked complete.

---

## Original CD1–CD3 handoff (retained as planning history)

# Citadel Construction Drones — implementation plan for Codex

> **STATUS: PLANNED, NOT IMPLEMENTED (2026-10-21).** This is documentation only. Do not report drone construction, arrival-gated work clocks, visuals, or tests as implemented until code exists and is verified.

## Player-facing goal

When the player successfully places a tower, the existing purchase, entire-footprint reservation, route validation and unfinished construction-site artwork happen immediately. A small cyan construction drone launches from an available **Citadel**, travels through the air to the site, circles or bobs gently near the unfinished tower, and shines a **bright cyan tapered light/hologram cone** downward onto it. **Only after the drone arrives and deploys its beam should the existing timed-construction clock start advancing.** When the normal construction stage completes, the beam retracts and the drone flies home. Towers should not aim/fire, produce resources or operate during the waiting-for-drone phase, exactly as they already do during initial construction.

This turns a functional build timer into a clear sci-fi event, *not* a separate economy, pathfinding or attack system. Preserve the existing immediate footprint blocking and payment: the delay should affect **work progress**, not placement validation. The player should see `AWAITING CONSTRUCTION DRONE` (or equivalent) in the selected tower Inspector until arrival; then regular `CONSTRUCTING` progress and stage changes.

## Existing Godot 4 / C# architecture — checked before planning

- `TOWERS/Placement/TowerPlacementController.cs` does validation/paid placement, uses `tower.StartConstruction(_construction)` **before** inserting the tower into `World/Towers`, and emits `TowerPlaced(TowerBase)` after successful placement. **Subscribe to the successful event**, not a mouse click or placement preview. Failed/refunded transactions must never dispatch a drone. Do not rebuild placement/research logic.
- `TOWERS/Shared/TowerBase.cs` owns `TowerWorkPhase`, `WorkSeconds`, `ElapsedWorkSeconds`, `WorkProgress`, `IsOperational`, `IsBusy`, and `WorkStarted` / `WorkProgressChanged` / `WorkCompleted`. Zero-second construction is an existing valid instant-complete case.
- `WORLD/ConstructionWork` uses `TOWERS/Shared/Construction/ConstructionWorkManager.cs`, a **single sparse active-job clock** that sleeps when idle. Currently work starts advancing immediately after placement; implement a *small explicit hold/release seam* for initial construction jobs so **waiting for a drone does not advance elapsed time**, but still keeps the site unoperational and occupied. Maintain C1–C4 semantics: active, unlocked upgrade jobs continue at the old tier and **should not require a drone in the first pass**.
- `TOWERS/Shared/Construction/TowerConstructionVisuals.cs` listens to existing work events and swaps authored SVG stage art at progress thresholds. No new art-state system: freeze at initial stage while waiting, then resume on the ordinary progress event. `TIMED_CONSTRUCTION_PASS.md` remains the old C1–C4 contract.
- `WORLD/OBJECTIVES/Citadel/CitadelSession.cs` exposes registered `CitadelDestination` entries via `Objectives`, each with a stable `Root`, `Health`, and `Available` flag. **There may be multiple Citadels.** No hardcoded `Citadel` scene path or assumption that there is only one.
- `WORLD/World.tscn` already has a `ConstructionWork` service and a *different* existing `DRONES/Transport/TransportDrone.tscn` for ammunition logistics. **Never reuse the ammo transport drone as a builder or alter its delivery pipeline.** Construction drones are a distinct entity/visual even if motion utility code is shared later.
- Existing `UI/GameplayHUD/` Inspector work display and `LEVELS/Session/` outcome management must stay intact; pausing or ending a mission cannot leave an unfinished site secretly constructing.

Read `NOTES/CodePreferences.md`, `TOWERS/Placement/README.md`, `TOWERS/Shared/Construction/README.md`, `WORLD/OBJECTIVES/Citadel/README.md`, `DRONES/Transport/README.md`, and the existing tests before changing code.

## Suggested owned files (design proposal; do not build all at once)

```text
TOWERS/
├── Shared/
│   └── Construction/
│       ├── ConstructionWorkManager.cs     (existing; add held-work seam)
│       └── README.md                      (existing)
└── ConstructionDrones/                    (NEW)
    ├── README.md                          (this plan)
    ├── ConstructionDroneMasterConfig.cs   # Inspector settings for count, speed, orbit, beam, caps
    ├── DefaultConstructionDroneConfig.tres
    ├── ConstructionDroneManager.cs        # one per battlefield; allocates jobs + selects Citadel
    ├── ConstructionDrone.cs               # individual world-space drone movement/state
    ├── ConstructionDrone.tscn              # minimal scene; no CollisionObject gameplay blocking
    ├── Visuals/
    │   ├── ConstructionDrone.svg          # small cyan robotic drone sprite, reusable
    │   ├── ConstructionBeam.cs            # projected cone/glow effect on target
    │   └── README.md
    └── Tests/
        └── README.md                      # manual/automated verification notes

TESTS/Towers/
├── ConstructionDroneChecks.cs            # state, timer gate, multiprocess safety
└── ConstructionDroneChecks.tscn           # if a dedicated runner adds value
```

**Folder choice:** This belongs under `TOWERS/ConstructionDrones/` because it is a game-wide *tower construction* mechanic, not a per-tower specialty. Reuse `TOWERS/Shared/Construction/` for timing; `DRONES/Transport/` remains ammunition logistics. If more drone types appear later, keep shared movement utilities in `DRONES/Shared/` without moving the specific construction feature unnecessarily.

## Recommended phases

### CD1 — hold/release work gate and deterministic dispatch (first coding pass)

1. On successful funded tower placement, reserve the construction job as today but **hold the work clock** until its assigned builder confirms arrival. Do not suspend the whole `ConstructionWorkManager` for other independent active jobs; an explicit per-job `AwaitingDrone`/hold flag is safer. Keep compatibility with tests and direct scene instantiation, where no builder subsystem exists. Pick a way to opt in **only for paid, nonzero-duration initial construction** before its first work tick; do not change zero-second or paid upgrades.
2. A single `ConstructionDroneManager`, mounted under the shared battlefield/runtime (one per gameplay session), listens to successful `TowerPlaced` and assigns the job. Be careful: the current placement already calls `StartConstruction` before emitting `TowerPlaced`, so a newly placed site's clock must be gated atomically **before the first physics tick**, not retroactively after elapsed work. Detect and reject duplicate notifications.
3. Pick a *deterministic available Citadel* for each job: nearest available by straight-line distance is a reasonable policy, with stable tie-breaking; do not select destroyed/unregistered Citadels. Drone launch and return use fixed logical points derived from the chosen `CitadelDestination.Root`'s transform (optionally a dedicated `DroneLaunch` Marker2D when authored, fallback to Citadel center). Multi-Citadel selection and destruction during travel require coverage.
4. Choose initial capacity semantics explicitly: simplest prototype is **one visual drone per simultaneous active build**, with a global bounded cap and FIFO pending queue; waiting sites remain paid/occupied/not operational. Do **not** silently start unattended work because the visual cap is hit. If the cap is reduced in the Inspector, never lose a job; only new assignments wait. Optionally allow unlimited within a safe small budget for debugging. Do not have a fixed pool of permanent idle-moving nodes by default.
5. Flight uses simple interpolated/accelerated `Node2D` movement through open sky, **not enemy ground A* or collisions**; towers, lava, maze walls and enemy pathing do not block an airborne builder. Distance/speed produce travel time; use a short ease-out on arrival, no teleport except an explicit failsafe. No expensive per-frame path searches or per-drone scene allocations.
6. When arrival reaches a small threshold, start `Beaming` then release the *specific* tower's held work clock once. Associate exactly one reservation/token with the destination tower; ignored/repeated callbacks cannot start twice.
7. Never cause deadlock: if there is no valid Citadel at placement, an invalid drone scene, failed creation, or a source Citadel disappears en route, use a documented **fallback** policy. Recommended default: log one warning and begin construction immediately with no builder, not permanently strand paid structures; expose `RequireDroneToBuild` as a strict debug/testing switch (default false). This is an exceptional safety fallback, not how normal builds should progress.

### CD2 — construction VFX and return flight

1. While beaming, the drone floats a little around the **tower's footprint center**; broad circles should not cross into adjacent towers. Use a tiny idle orbit or position bob and draw the cyan cone anchored from drone underside to the unfinished tower. Top-down projection can be an additive cyan translucent polygon/shader with soft edges: brighter narrow origin, broader softer footprint, and a few flickering lattice/particle lines. Do **not** require dozens of particle nodes per beam.
2. Brightness should be **legible without hiding the tower's staged construction artwork**. Suggested starting palette: bright icy cyan on the drone nozzle, soft cyan interior cone, concentrated feathered edges and faint holographic sweep lines. The cone should follow the drone while the target point stays on the site; its visual state must not modify the tower's targetability, shield visuals or mouse input.
3. When `WorkCompleted` fires for **initial construction**, stop beam and launch the drone home. Reuse the assigned launch position and don't demand an available Citadel to complete tower work already underway. If the source is gone, prefer choosing a surviving home Citadel or despawning the cosmetic drone safely after work is done. Return should not delay tower operation or release of tower occupancy. On home arrival return to the pool/free it; no idle per-drone processing.
4. If the tower dies/is removed while the drone is flying or beaming, cancel its assignment, hide the cone, release/clean up the drone, and free capacity for pending builds. Only the *actual* work manager is authoritative for job completion; neither VFX nor drone movement should increment work time themselves.
5. Support all tower footprints: 1×1 combat towers, 2×2 Depot/resource structures, 3×2 Factory and larger future constructions. Orbit radius, beam target and initial spawn clearance must derive from footprint size (or bounded config), never assume a Battle Tower-sized grid cell.

### CD3 — HUD, polish, tests and future extensions

- Inspector explicitly distinguishes **WAITING FOR DRONE / CONSTRUCTING / OPERATIONAL**; remaining seconds must not tick before arrival. Optionally show estimated arrival (purely informational, no gameplay effects) if useful. Keep existing work bar at 0 until construction starts.
- Separate toggles: `Enabled` for entire feature, `ShowVisuals` only for cosmetic graphics, `RequireDroneToBuild` for strict build dependency. **Turning off visuals must never alter gameplay timing** when the feature remains enabled; turning the feature off should permit immediate normal work progression for compatibility/tests.
- Automated checks: success-only launch; credit charge once; 0-second and direct-instantiated tower remain compatible; no elapsed time during flight (including large frame delta); exactly-once release at arrival; multiple simultaneous active+queued builds; upgrading an operational tower still works; pause/resume; source Citadel fallback and multiple Citadels; tower destroyed/removed; scene teardown; absent drone resource fallback; fast zero-distance placement; bounded maximum concurrent visuals; no added enemy ground pathfinding; and HUD progress + stage art. Preserve all existing C1–C4 fixture assumptions through an opt-in dependency or explicit test helpers where needed.
- Desktop verify flight animation, soundless beam, stage texture transitions and overlapping new construction jobs at **1920×1080** and **960×540**. Profile 20 simultaneous builds, ensure active-only processing, no per-frame allocations or extra expensive shaders. Run actual Godot .NET build/headless fixtures before claiming test success.
- Update this README with actual file names, state transitions, config presets, captured test results and known limitations in **one coherent commit per coding job**, per project preferences.

## Suggested starter values (playtest defaults; not fixed balance)

| Setting | Initial suggestion |
| --- | --- |
| Max active builder drones | 8 |
| Drone travel speed | 350–500 world px/s |
| Arrival radius | 12 px |
| Beam deployment | 0.2–0.35 seconds |
| Hover/orbit radius | 12–20 px near the build center |
| Beam opacity | 0.22–0.40 fill, brighter rim/nozzle |
| Drone return | Same speed or slightly faster; purely cosmetic |
| Disabled drone mode | Preserve original immediate timer start |
| Missing home Citadel | Fail-safe starts work; warn once |

The values are subject to actual scale/zoom and performance review. Keep them in `DefaultConstructionDroneConfig.tres`, not hardcoded repeatedly.

## Open game-design options (do not assume in initial implementation)

- Should the player build drone hangar modules or research more simultaneous drones? Could be an upgrade later; don't implement today.
- Should a drone be destructible, interceptable or targetable by hostile gunner drones? **No for the first pass.** Drone should be a non-combat construction visual, independent of enemy/ground collisions.
- Should a fixed number of reusable drones queue jobs, or should each job spawn its own visual with a cap? Prototype a bounded assignment queue first, without new in-game currencies.
- Should *upgrading* towers eventually dispatch the same drone? **Defer**. Existing upgrades remain active and should not unexpectedly stop working.
- Should Citadel destruction cancel, reroute or strand existing construction? Use a nonblocking fallback first; a strict gameplay penalty may be designed later.

## User acceptance target

On Level020, place a Battle Tower: site and foundation appear; **no build progress yet**; cyan builder launches from nearest available Citadel, flies over terrain, hovers and projects a visible holographic beam; the usual ~3-second Battle construction starts **only when the beam activates**; the tower becomes operational at regular completion; drone returns home. Build several towers at once and each assignment is distinct. Other towers should participate through the same shared foundation when their work duration is positive, without having bespoke scripts.

**No implementation is authorized by this documentation pass; Codex should tackle CD1 before CD2 and CD3.**
