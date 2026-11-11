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
| 4 | Projectile rendering | P4-A and optional P4-B implemented in source. The middle-right Textured Tracers toggle compares cached textured quads with the existing renderer. Shared friendly and hostile renderers cull complete visual footprints. Standalone Basic/Splash shots retain cached commands and use native viewport culling with explicit bounds. Projectile light leases release offscreen and suspend in wholly concealed areas. Runtime measurements and camera/fog checks pending. |

### P2 verification

Owner screenshot before P2, with FOG-PERF1 in use and mixed visibility: 314 enemies, 35 towers, 75 FPS, 20.4 ms displayed frame time, 31.1 ms peak, 7.46 ms engine process, 13.73 ms physics, 630 draw calls and 3,873 render objects. The owner reports that this run rarely fell below 60 FPS. This single view is a reference, not a matched comparison. Post-P2 runtime measurements are pending. No batch prototype was added because individual-bar overhead has not been isolated.

Use F2 for the current Performance overlay, Shift+F2 to profile, and F3 to save the report. For P2, compare 200–500 ships in fully concealed, fully revealed and mixed combat views. For a separate shield rendering comparison, temporarily suppress only the aura quad in a local profiling build and leave shield state and bars active.

### Verification

Use the same level, camera, zoom, resolution and comparable fleet mix. Change one visual layer at a time. Record frame time and spikes alongside live enemies, draw calls and render objects. Use the rendering profiler for GPU measurements where available. If attribution is still unclear, add counters for active exhaust emitters, on-screen fog contacts, visible shield effects and defense bars.

Check fog transitions, camera edges and crowded combat after each change. Preserve enemy count, pathing, targeting, projectile collisions and damage. Keep [navigation work](NOTES/NAVIGATION_PERFORMANCE_PLAN.md) separate from these rendering passes.

### P4-A verification

Source implementation retains tracer artwork and the shared RevealEntities shader. Shared renderers reject complete screen-space footprints, with a two-pixel antialiasing margin. Standalone drawing stays cached for native CanvasItem culling, using the same tracer footprint plus a conservative zoom margin. Fog remains a pixel shader operation for bullets. Only the capped light pool tests reveal-disk overlap.

No crowded-battle FPS or GPU comparison was practical without the Godot .NET runtime. Compare the same scene with dense friendly and hostile fire, fully offscreen shots, tails/glows/tips crossing every edge, fog crossings and F6, and camera pan/zoom/rotation. Include buffered overflow or UseBufferedTracers=false and Splash rounds. Record F2 / Shift+F2 / F3 reports with draw calls and render objects. Native standalone command caching is retained. Common hull batching and simplified tracer rendering remain separate work.

### P4-B verification

The permanent debug panel at middle-right switches tracer rendering live. Original rendering remains the default. Compare both modes at the same camera/zoom with dense friendly and hostile volleys, then disable UseBufferedTracers to exercise BasicProjectile fallback. Check switching with rounds in flight and while paused, texture colors and dimensions, tapered shots, fog edges, F6, and viewport clipping. Account for the first bake/upload of each style before recording steady-state results. Resize the window and open/close F1 to check the independent panel.

No matched runtime FPS or GPU measurements are available in the current environment. The 64-style cache is bounded and preserves original-renderer fallback for additional styles. The textured mode remains optional pending owner comparison.
