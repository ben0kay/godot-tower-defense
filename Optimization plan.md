# Optimization plan

## Dense-fleet rendering

**Status:** Proposed. Target smooth play with 300+ live enemies during crowded combat.

### Baseline

In the Level003 captures, the 319-enemy view showed 163 FPS, 355 draw calls and 2,561 render objects. The laggy 296-enemy view showed 57 FPS, 537 draw calls and 3,594 render objects. Movement p95 was similar at 7.19 versus 7.35 ms per sampled physics tick. The slow view had 29 buffered friendly shots and zero hostile shots. The owner reports similar FPS with lighting on and off after the radar-dot fix. These observations prioritize rendering and effects investigation. GPU timing and individual effect costs were unmeasured.

### Proposed passes

| Order | Area | Change to evaluate |
| --- | --- | --- |
| 1 | Thruster exhaust | Compare the same fleet and camera with GPU exhaust on and off using `GpuParticlesEnabled` in `ENEMIES/Shared/Visuals/Thrusters/DefaultEnemyThrusterConfig.tres`. Keep the static flame. The current 64-emitter default cap and on-screen gate do not account for fully fog-hidden ships. Gate exhaust on effective reveal, release slots promptly, and consider a low-zoom cutoff or lower dense-fleet budget if the comparison shows a gain. |
| 2 | Shields and defense bars | Isolate each effect in the same crowded scene. Defense bars already redraw when their values change, but visible bars update their transforms during movement to stay upright. If these layers show measurable cost, try simplified low-zoom bars or batched bars. Preserve shield and damage feedback at ordinary gameplay zoom. |
| 3 | Common hull sprites | Copies already share imported textures. Prototype batched drawing for common drone hulls grouped by texture and material if hull rendering remains a bottleneck. Keep per-ship position, rotation, tint, visual scale and fog reveal. Verify draw ordering and damage textures before extending to layered or armed ships. |
| 4 | Projectile rendering | Friendly and hostile standard shots already use shared buffers. Bring the friendly viewport-culling approach to hostile draws, with padding for the full tracer and glow. Keep simulation and collision active offscreen. If heavy volleys show a measurable cost, compare the current glow/body/core draw passes with a reusable textured tracer. |

### Verification

Use the same level, camera, zoom, resolution and comparable fleet mix. Change one visual layer at a time. Record frame time and spikes alongside live enemies, draw calls and render objects. Use the rendering profiler for GPU measurements where available. If attribution is still unclear, add counters for active exhaust emitters, on-screen fog contacts, visible shield effects and defense bars.

Check fog transitions, camera edges and crowded combat after each change. Preserve enemy count, pathing, targeting, projectile collisions and damage. Keep [navigation work](NOTES/NAVIGATION_PERFORMANCE_PLAN.md) separate from these rendering passes.
