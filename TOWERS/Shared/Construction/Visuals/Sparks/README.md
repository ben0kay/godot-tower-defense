## Two-layer welding sparks — 2026-11-03

Construction and timed upgrade work now uses **two GPU particle layers from one shared emitter scene**:

- **Soft background layer:** The original warm, rounded amber/orange flecks, unmodified. Defaults remain 8 flecks/site, 0.48s lifetime, 55% tower-footprint coverage. It continues using `FEEDBACK/Shared/Particles/EnergySpark.svg`.
- **Hot welding layer:** A separate `GPUParticles2D` child named `WeldingSparks`, with a sharply defined [white-hot streak SVG](Artwork/WeldingSpark.svg). The small streak aligns with travel direction, sprays across wider angles with faster random velocities, random tangential motion, short bursts, and falls/fades through bright white, pale yellow and brief orange. Defaults: 8 streaks/site, 0.34s lifetime, 38% footprint coverage. No CPU particle movement.

Tune either via the same `DefaultConstructionSparkConfig.tres`: `WeldingEnabled`, `WeldingParticlesPerSite`, `WeldingLifetime`, `WeldingFootprintCoverage`, `WeldingTint` are **new** independent controls. Speed, spread, gravity, damping, size, colour ramp and burst randomness live in `ConstructionSparkEmitter.tscn`. The controller changed the original emitter tint from inherited `Modulate` to `SelfModulate`, **so the orange background never dyes the hot-white child**.

Both layers begin only when construction or upgrade time actually starts (after any drone hold), and both stop/free on work completion, cancellation, unload or destruction. One scene/active work site, still capped at 24. No new CPU processing/timers/scans or impact on credits, drones, construction times, stage sprites, saving or combat. Default peak budget is **24 sites × (8 soft + 8 welding) = 384 GPU particle slots**; welding can be disabled while preserving the orange layer.

**Desktop acceptance pending:** Compare before/after at normal camera zoom, confirm crisp brighter erratic strokes above faint amber, repeat for 1×1 and large buildings, toggled off, paused/finished, and 3/10/24 simultaneous tower projects. No renderer/FPS benchmark has been run.

---

# Shared GPU fabrication sparks

Reusable warm **GPUParticles2D** sparks for paid **construction and upgrade work** on all tower types. The emitter scene uses the already-shared `FEEDBACK/Shared/Particles/EnergySpark.svg` texture. One dormant listener under `World/Effects/ConstructionSparks` uses existing `ConstructionWorkManager.JobRegistered/JobUnregistered` events, including saved/restored jobs; it never polls tower state or runs `_Process`.

**Files:** `ConstructionSparkController.cs` owns the active site mapping and clean-up; `ConstructionSparkEmitter.tscn` contains motion/gradient/texture; `ConstructionSparkConfig.cs` and `DefaultConstructionSparkConfig.tres` provide Inspector tuning (Enabled, cap 24 sites, 8 particles/site, .48s lifetime, 55% footprint coverage and amber tint). One emitter is temporarily parented under each actively registered tower. Each active emitter gets its own one-time process-material copy *solely* to size the source box to the tower's rectangular footprint; the shared artwork is reused.

**Timing:** emitters are dormant while waiting for a construction/upgrade drone. The existing `WorkProgressChanged` hold-release event starts sparks when paid work actually begins. Without a required drone, timed work emits immediately. Completing, cancelling, destroying, failing placement and world teardown all retire the effect. Direct/zero-duration/idle buildings have no emitters. Spark cap omits effects only; it never changes work, credits, progress, drone holds or gameplay. Pause is inherited from the tower scene tree.

**Rendering/performance:** small soft orange flecks lift in random directions, fall and fade. GPU particles own their motion. Maximum default budget is 24 × 8 particle slots, with no extra CPU frame callbacks, timer nodes, active list scans, texture allocation or visual work when idle. Existing cyan construction-site holograms, beam and staged artwork are unchanged. Editing `Enabled` prevents new instances; active jobs retire normally (no idle polling for resource changes).

**Godot acceptance:** inspect 1×1 Battle, 2×2 Depot and 3×2 Factory, construction and upgrade with and without a drone, pause/save/reload, and 3/10/24 overlapping sites. Confirm spark placement, brightness, lifetimes and FPS in F4. Source-only validation cannot verify GPU appearance.
