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
