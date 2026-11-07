# LGT0 — Real 2D Lighting Foundation (Codex implementation plan)

**Repository:** `ben0kay/godot-tower-defense`  
**Branch:** `dev` only  
**Status:** **Approved plan, NOT IMPLEMENTED**  
**Priority:** Implement this real-world-lighting pilot before VE4 regional atmosphere and the remaining VE5/VE6 passes. Do not restore reverted VE5C bloom.  
**System ownership:** `SYSTEMS/Lighting/` (the existing repository folder is plural `SYSTEMS`, not `SYSTEM`).

## Outcome and scope

Deliver genuinely lit/dark 2D battlefields using **Godot `CanvasModulate` + native `PointLight2D`**. A Citadel and completed Battle Towers emit soft cool light that falls off smoothly onto nearby metal, Foundation and other compatible ground art. Areas away from those structures remain dimmed by configurable ambient lighting. **No new gameplay visibility**: darkness must not affect enemy AI, targeting/LOS, stealth, pathfinding, collision or damage. No shadow casters, normal-map rewrite, postprocessing/bloom or lighting-derived gameplay logic in LGT0.

Reference: [Godot 2D lighting and shadows](https://docs.godotengine.org/en/stable/tutorials/2d/2d_lights_and_shadows.html), [PointLight2D](https://docs.godotengine.org/en/stable/classes/class_pointlight2d.html), [Light2D](https://docs.godotengine.org/en/stable/classes/class_light2d.html) and [GPU optimization](https://docs.godotengine.org/en/stable/tutorials/performance/gpu_optimization.html).

### Facts verified in the repository

- `LEVELS/Runtime/Battlefield.tscn` contains `World` (`WORLD/World.tscn`) and separate `UI` CanvasLayer; towers are under `World/Towers`, with `TowerRegistry` already listening to child arrival/removal. Reuse production lifecycle; no new full-scene scans every frame.
- The Citadel artwork in `WORLD/OBJECTIVES/Citadel/Citadel.tscn` is **offset** from its root. Position its light at the actual artwork/reactor centre; Battle Tower lights should follow their stationary base, not the rotating/recoiling gun.
- **F3 is already the route preview** in `DEBUG/LevelTools/LevelDebugController.cs`; **F4** is the existing performance panel, and **F5** dumps a profile. Keep all three bindings.
- `WORLD/VISUALS/LevelAtmosphereController` owns VE1 stars/nebulae, not physical 2D lighting. Existing VE2 portal/Citadel glow and VE3 decal shaders are emissive-looking visuals; **they do not illuminate neighbouring tiles**. Don't replace their materials.
- `WORLD/VISUALS/Lighting/README.md` confirms the old VE5C HDR/bloom test was reverted. Never re-enable it incidentally.

## Implementation milestone LGT0 (implement ONLY this, then stop)

### A. Shared system in SYSTEMS/Lighting

1. Add `WorldLightingController.cs`, a **world-scoped** lighting coordinator mounted in the shared `WORLD/World.tscn`, **not** a permanent autoload. Centralize active lighting state, `CanvasModulate`, light source registrations, budgets and cleanup in one place.
2. Add `LightingMasterConfig.cs` and `DefaultLightingConfig.tres` with Inspector-editable `Enabled`, ambient tint, default state, light/source budget, optional viewport culling settings and debug comparison permission. Lighting OFF must restore the original undimmed baseline.
3. Add `LightSourceConfig.cs` plus reusable per-source preset resources, initially **Citadel** and **Battle Tower**. Expose Enabled, tint, energy/brightness, radius/texture scale and local offset. Use one cached light texture and native lights, not per-source procedurally generated textures.
4. Create a small shared soft radial gradient texture (e.g. 256px) whose outer border fades fully transparent. True light intensity/falloff comes from the native PointLight2D, not an additive sprite pretending to cut a hole in darkness. Radius must be calibrated against the actual light texture/scale and 64px tile grid.
5. Keep this plan and add a short `SYSTEMS/Lighting/README.md` when the implementation is delivered. World/scene-specific nodes may live under their natural scene parent, but the controller, configs, presets and shared helper code stay here.

### B. Baseline visual values and placements

- Prototype **ambient `#65727e`** (gentle industrial dusk, not pitch black). Inspector changes can later support deep-night or planet-specific palettes. Do not hardcode a night mode onto every existing level.
- Citadel preset: cool cyan/teal light, approximately **4–6 map cells** of visible radius (256–384 world px), softly fading. Multi-Citadel maps get separately anchored lights.
- Battle Tower preset: pale cyan/white, approximately **2.5–4 cells** (160–256px), soft edge. Start with one tower then several, not all tower types.
- Use normal additive light blending; **shadows OFF** for LGT0. No `LightOccluder2D`, `DirectionalLight2D`, tile-by-tile normal maps, bloom or screen-space glow pass.
- Apply ambient to the **world**, not the HUD/menu CanvasLayer. Test whether existing shaders marked `unshaded` opt out of light/ambient; those may intentionally remain emissive. Preserve the terrain E2 mask material behavior and don't blanket-replace TileMapLayer materials.
- Placement ghosts, off-tree preview scenes and in-progress construction do **not** emit light. When a real tower finishes construction its light may enable. Destroying/selling/unloading unregisters and frees it. Check save/Continue restore and upgrade/disable paths without changing save data.
- Future tower types opt in via a typed source preset/attachment, not a giant switch on tower IDs. **LGT0 only integrates Citadel and Battle Tower.**

### C. User requested debug on/off comparison

- **Shift+F3 toggles lighting ON/OFF**, debug builds only, in playable battlefield scenes. Preserve plain F3 route preview and its UI, F4 performance overlay, F5 profiler. Ignore key release/repeat; use appropriate existing debug owner or scene-scoped input binding, no global duplicate listener.
- Controller exposes one method `SetLightingEnabled(bool)` or equivalent; all managed lights and the ambient system obey this state. OFF means **white/unmodulated ambient AND all managed PointLight2D disabled**; disabling lights alone while leaving a dark world is incorrect for this comparison. ON restores configured ambient plus eligible sources.
- A tiny debug toast/readout `LIGHTING ON` / `LIGHTING OFF` is optional; a cheap status line on the existing F4 overlay is acceptable, but don't expand it into a new window.
- Debug override is runtime-only (not saved in campaign checkpoints), resets when a new battlefield opens and cannot strand menus/Results in darkness. Inspect pause/unpause, route-preview coexistence and teardown.

### D. Performance and lifecycle contract

- Initial **provisional budget: 16 simultaneously enabled PointLight2D sources** at most (Inspector adjustable). Begin tests with 1 Citadel + 1–6 towers. If several sources overlap, use stable deterministic priority; prefer visible Citadel and nearby relevant structures, no flicker when exceeding cap.
- Large lights illuminate more pixels; many overlapping lights and especially soft shadow maps can be costly. Shadowless 2D lights are the first measured route, not proof that dozens are free on GTX 1080.
- Reuse a shared gradient texture. No per-light or per-tower C# `_Process`, `Timer` or animation callback; static light transform updates only on needed source movement or registration.
- Prefer membership events (existing `Towers` child hooks and Citadel map ownership) plus a small cached visible-source list. Offscreen candidates should be disabled using a bounded/low-frequency camera visibility refresh (e.g. 0.25–0.5s when needed) or one practical engine visibility callback, **not** scanning every enemy/tower per physics frame.
- Do not allocate a light per VE3 glowing decal, per enemy, projectile or short-lived muzzle flash. Do not add power-grid calculations, fog-of-war, visibility queries or new save fields.
- **Performance goal, not measurement:** strive for roughly **≤1ms incremental GPU cost** at common light counts on a busy 1920×1080 battlefield while maintaining the wider 60FPS goal. No FPS guarantees without the owner's hardware benchmark; F4 CPU timings alone are not GPU timings.

### E. Deliverables and validation (lightweight, owner-led)

Implement as a **small, standalone job** with shared config, one world controller, two source presets, Citadel/Battle Tower integration and Shift+F3; update a short `NOTES/WORK.md` entry, commit and push `dev`. Do not implement LGT1–LGT4 during this pass.

Focused source review / one practical build if available, then leave desktop appearance/performance acceptance to owner:

1. Same camera position/zoom with ON vs OFF, confirming real light falls on underlying floor art and OFF restores original colors without darkening HUD, F3 routes or menus.
2. 1, 4, 8, 16 lights with and without overlap on normal map zoom; use a heavy battle with **50/200/500 enemies** for realistic comparison. Record FPS, CPU frame, **GPU frame time where measurable**, renderer, resolution and zoom. If GPU time is unavailable state so, don't invent it.
3. Creation/finished construction, sale/destruction, checkpoint restoration, multiple Citadels, camera pan/zoom, pause, retry and level transitions. No ghost lights, input conflicts, leaked nodes or altered gameplay.
4. Test existing shader materials and optional E2 mask against the light; no unrelated Decorative/Mars atlas lighting or shared material edits.

## Later opt-in steps — NOT PART OF LGT0

- **LGT1:** opt more towers, manually placed industrial lamps/VE3 small decal fixtures into shared source preset system. Never make a native light for every lit atlas cell. Optional power-loss responses only using authoritative source state, later.
- **LGT2:** optional actual 2D shadows from a few major hand-authored obstacles; introduce `LightOccluder2D` only after GPU/overlap profiling. Terrain shot-time LOS remains independent of shadows.
- **LGT3:** authored day/night darkness presets, selected slow light pulses/failed-lamp flicker and per-level identity, event-driven and capped.
- **LGT4:** actual light-driven stealth/reveal/fog-of-war is a **separate gameplay feature** requiring explicit design; it is not provided by visual darkness.
- **VE5C bloom:** reverted and out of scope. No global HDR/glow restoration.

**Decision:** Real Godot 2D lights are the first implementation choice. If the measured GPU cost is excessive, reduce count/radius/overlap before considering fake additive accents as a separate performance fallback.
