# Optimization plan

## Dense-fleet rendering

**Status:** Proposed. Target smooth play with 300+ live enemies during crowded combat.

### Baseline

In the Level003 captures, the 319-enemy view showed 163 FPS, 355 draw calls and 2,561 render objects. The laggy 296-enemy view showed 57 FPS, 537 draw calls and 3,594 render objects. Movement p95 was similar at 7.19 versus 7.35 ms per sampled physics tick. The slow view had 29 buffered friendly shots and zero hostile shots. The owner reports similar FPS with lighting on and off after the radar-dot fix. These observations prioritize rendering and effects investigation. GPU timing and individual effect costs were unmeasured.

### Proposed passes

| Order | Area | Change to evaluate |
| --- | --- | --- |
| 1 | Thruster exhaust | Compare the same fleet and camera with GPU exhaust on and off using `GpuParticlesEnabled` in `ENEMIES/Shared/Visuals/Thrusters/DefaultEnemyThrusterConfig.tres`. Keep the static flame. The current 64-emitter default cap and on-screen gate do not account for fully fog-hidden ships. Gate exhaust on effective reveal, release slots promptly, and consider a low-zoom cutoff or lower dense-fleet budget if the comparison shows a gain. |
| 2 | Shields and defense bars | Implemented in source. Full fog concealment suspends shield and bar rendering. Individual bars cache layout and skip unchanged transforms. Profile visible bars before considering batching, since individual bars already cache draw commands. |
| 3 | Common hull sprites | Copies already share imported textures. Prototype batched drawing for common drone hulls grouped by texture and material if hull rendering remains a bottleneck. Keep per-ship position, rotation, tint, visual scale and fog reveal. Verify draw ordering and damage textures before extending to layered or armed ships. |
| 4 | Projectile rendering | Friendly and hostile standard shots already use shared buffers. Bring the friendly viewport-culling approach to hostile draws, with padding for the full tracer and glow. Keep simulation and collision active offscreen. If heavy volleys show a measurable cost, compare the current glow/body/core draw passes with a reusable textured tracer. |

### P2 verification

Owner screenshot before P2, with FOG-PERF1 in use and mixed visibility: 314 enemies, 35 towers, 75 FPS, 20.4 ms displayed frame time, 31.1 ms peak, 7.46 ms engine process, 13.73 ms physics, 630 draw calls and 3,873 render objects. The owner reports that this run rarely fell below 60 FPS. This single view is a reference, not a matched comparison. Post-P2 runtime measurements are pending. No batch prototype was added because individual-bar overhead has not been isolated.

Use F2 for the current Performance overlay, Shift+F2 to profile, and F3 to save the report. For P2, compare 200–500 ships in fully concealed, fully revealed and mixed combat views. For a separate shield rendering comparison, temporarily suppress only the aura quad in a local profiling build and leave shield state and bars active.

### Verification

Use the same level, camera, zoom, resolution and comparable fleet mix. Change one visual layer at a time. Record frame time and spikes alongside live enemies, draw calls and render objects. Use the rendering profiler for GPU measurements where available. If attribution is still unclear, add counters for active exhaust emitters, on-screen fog contacts, visible shield effects and defense bars.

Check fog transitions, camera edges and crowded combat after each change. Preserve enemy count, pathing, targeting, projectile collisions and damage. Keep [navigation work](NOTES/NAVIGATION_PERFORMANCE_PLAN.md) separate from these rendering passes.
