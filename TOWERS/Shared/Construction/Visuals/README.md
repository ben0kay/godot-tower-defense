## Circular construction telemetry polish — 2026-11-06

**Source Implemented / Awaiting Owner Visual Validation.** The previous rectangular site readout is replaced only for **CONSTRUCTING / UPGRADING** by a compact cyan circular vector meter outside the footprint. The actual paid work percentage counts up in the centre; one pale outer curved arc and its 20 tiny ion dots move **anti-clockwise**, while two inner curves rotate **clockwise**. A small caption identifies the active job. The ring has only a subtle round tint; no black rectangle. **DRONE INBOUND, SITE LOCKED and SYSTEM ONLINE** remain short unboxed text, with the existing completion fade.

This reuses the active-only shared `ConstructionSiteHologram` canvas, 24Hz redraw, eight bounded non-overlap lanes, pause behaviour, completion cap and original work/restore sources. No new site nodes, timers, shader, particle emitters or save fields. The original rectangular footprint hologram remains unchanged. Adjust ring radius, the three rotation speeds and count-up speed through `DefaultConstructionSiteVisualConfig.tres`. Owner Godot visual acceptance pending: inspect both build/upgrade, queued/locked/completed states, nearby towers, pause, checkpoint restore and cancellation.

---

## DRONE2 site telemetry and acquisition

**Source Implemented / Awaiting Owner Validation.** The same capped ConstructionSiteHologram canvas now draws one tiny panel outside the full footprint: DRONE INBOUND while held; SITE LOCKED during acquisition/deployment; real CONSTRUCTING/UPGRADING N% after release; SYSTEM ONLINE for1s only on successful WorkCompleted. No-builder work uses the authoritative work phase/progress immediately. No GUI scene, timer or process per site/drone. Existing work events cache strings, with the bounded redraw cadence recovering live visual toggles. Completion readouts cap32 (hard64), clear on removal/unload and are not restored/replayed. One new paid job replaces that tower's earlier completion panel.

Panels use fixed retained placement bounds and at most eight vertical lanes to avoid overlapping nearby readouts; excess crowded cosmetics may skip rather than overlap or expand unbounded work. Existing active-site cap remains128 (hard256), cadence24Hz and camera culling. Renderer sleeps after the last site/completion readout. Cancelled/destroyed/rolled-back work never shows SYSTEM ONLINE.

During the manager's new0.20s Acquiring phase, existing footprint brackets brighten once and four thin cross strokes converge on its center. This does not advance work or deploy the beam. The original0.25s deployment follows and remains the release gate. AcquisitionCueEnabled/tint and telemetry enabled/font/size/colors/background/width/offset/completion-duration/cap are Inspector settings in DefaultConstructionSiteVisualConfig.tres. The original blueprint, fabrication sparks and purchased-tier art stay unchanged. Runtime visual/performance acceptance remains pending.

---

## GPU sparks now use explicit perimeter points — November 3, 2026

Both orange and white-hot construction/upgrade `GPUParticles2D` layers are now sourced from **64 explicit outer-footprint positions** supplied in a small float emission texture on paid work registration. This replaces prior ring/sphere spawn settings that still looked centre-biased in user screenshots. All resulting sparks travel radially outward; no additional processing loops or render emitters. Read [Sparks](Sparks/README.md) for the authoritative source geometry and source-level regression check. In-game visual/FPS acceptance remains pending.

---

## Corrected edge-origin construction sparks — 2026-11-03

Screenshot feedback revealed the previous orange sparks still emerged from a central rectangular area, even after the crisp layer's ring radius increased. **Both** orange and sharp-white layers now use true hollow `ParticleProcessMaterial` rings close to the footprint perimeter (92%/94% of the relevant tower dimensions), with radial velocities that carry particles beyond the building. On rectangular sites, the ring stretches into an ellipse. The original texture, colour palette and separate work-driven lifecycle remain intact. See [Sparks/README.md](Sparks/README.md) for implementation details and tuning. No per-frame CPU updates were introduced; in-game visual verification is pending.

---

## Two-layer fabrication sparks — 2026-11-03

The existing soft orange `GPUParticles2D` flecks are preserved. A new, brighter **white-hot/yellow welding streak** layer is added above them inside the same shared emitter scene. It has its own GPU motion material, velocity alignment and independently configurable density/lifetime/tint/enable flag. Both layers share the existing paid construction/upgrade start, drone hold-release and teardown notifications; no extra script update loops. Read the [Sparks guide](Sparks/README.md) and tune `Sparks/DefaultConstructionSparkConfig.tres` or `Sparks/ConstructionSparkEmitter.tscn`.

---

## Shared construction / upgrade fabrication sparks — 2026-11-03

A new capped, visual-only `World/Effects/ConstructionSparks` controller now adds small amber GPU sparks during any paid timed **initial construction or upgrade**. It subscribes to the shared work manager's existing events and adds one temporary `GPUParticles2D` child to an active tower, matching 1×1/2×2/3×2 footprints. Sparks **wait until the drone finishes deploying its beam** (or start immediately for work without a drone), then stop on the work-unregistration event. There is no extra animation/physics processing in the C# controller. Config, reusable scene and tuning: [Sparks](Sparks/README.md).

The cyan CV1 footprint hologram, builder beam/flight, stage artwork, build clock and save format are unchanged. The effect is GPU-driven and defaults to a cap of 24 sites with eight visible particle slots per active site. Desktop visual and F4 profiling remain with the owner.

---

## CV1 extension — same site overlay during upgrades (2026-10-27)

The existing **square cyan construction-site hologram** now appears on **all positive-duration paid upgrades** as well as initial builds, without replacing the tower sprite or restarting its initial build art. It is visible immediately after the purchase is accepted, while the upgrade drone is queued/flying/deploying, through beam time and both Battle Tower upgrade-art milestones, then disappears on the existing work-unregistration event at completion or removal. **The tower and its upgrade images remain visible underneath**: only the transparent footprint blueprint/corner brackets/perimeter dot/engineering rings are drawn over it. The same 1×1/2×2/3×2 footprint rules and waiting/building opacities apply.

This uses the **same** `World/Effects/ConstructionSiteHolograms` renderer, `ConstructionWorkManager.JobRegistered/JobUnregistered` events and capped active-only 24Hz canvas. No new nodes, texture assets, timers, per-tower processing or new save fields. A timed upgrade **without** a drone still shows the overlay; zero-time upgrades never register work and do not show it. Loaded jobs work through the same restore-time registration. In the shared source, only the work-phase filter changed from `Constructing` to all current busy phases (`Constructing` and `Upgrading`).

**Validation:** focused source/regression assertions updated to cover waiting, beaming, staged artwork, completion cleanup and upgrades with the drone opt-out. Godot/.NET and rendered playtest not run here.

---

# CV1 — Shared Construction Site Holograms (2026-10-26)

**Implemented in the shared runtime:** `World/Effects/ConstructionSiteHolograms` is one active-only `Node2D` renderer for all **paid initial construction and timed upgrade** jobs. It listens to `ConstructionWorkManager.JobRegistered/JobUnregistered` (not `ConstructionDroneManager`) so it is visible immediately after a successful paid placement or upgrade, including when the drone is outbound or queued behind the default eight-drone cap. The same registration path runs during S2/S3 checkpoint restoration. Direct/zero-duration/operational towers never register a hologram; paid timed upgrades now use the same site renderer.

Visuals scale to `TowerBase.Footprint × WorldGrid.Config.CellSize`: faint transparent blueprint and cross-lines, glowing perimeter/corner brackets, a travelling perimeter dot, and two concentric animated broken circular engineering rings. The **rectangle remains fixed** for 1×1/2×2/3×2/other footprints; the circular scanner rotates *inside* it. While `AwaitingConstructionDrone` is true (even during beam deployment), opacity uses the softer waiting preset; upon arrival the existing work hold releases and the same drawing brightens automatically. The hologram coexists with tower construction-stage SVGs and the separate drone projection beam, and disappears as soon as the work manager unregisters the completed, removed or cancelled job. Existing CD3 completion flash still fires from the drone's own completion event.

**Performance:** one shared canvas, no per-tower effect scene, shaders, texture allocations, particles, timers, signals on every tower, work loop, or render-loop when empty. The renderer throttles updates to 24 Hz while any construction or upgrade site is present, draws only the registered towers, and caps tracked visuals (default 128, max 256) without touching any game jobs. The rendering parent is `World/Effects` at z=7, over the foundations and under the drone at z=8. Pausing the battlefield pauses the shared animation; exiting a map unsubscribes from work events. Cosmetic event listeners cannot fail a purchase/clock transaction.

**Inspector tuning:** `DefaultConstructionSiteVisualConfig.tres` edits enabled, refresh Hz, max site visuals, cyan tint, waiting/building opacity, blueprint opacity, inset, corner bracket length, scanner rotation, moving-dot speed, and pulse depth/frequency. Disabled and empty renderers sleep; swapping the resource or calling `RefreshProcessing()` re-evaluates runtime presentation when editing settings.

**Desktop check pending:** visually inspect Battle/Machine Gun before drone launch and while waiting in an >8-build queue, the stronger hologram when the drone releases work, removal on completion, 2×2 Depot and 3×2 Factory bounds, and save/load between waves. Compare the game at 1920×1080 and 960×540, including performance with many simultaneous sites. Source checking is not a visual/GPU test.
