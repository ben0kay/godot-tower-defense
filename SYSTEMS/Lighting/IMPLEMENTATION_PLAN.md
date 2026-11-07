# LGT0 — Real 2D Lighting Foundation (Codex implementation plan)

**Repository:** `ben0kay/godot-tower-defense`  
**Branch:** `dev` only  
**Status:** **LGT0-A source implemented, awaiting owner validation. LGT0-B planned.**
**Priority:** Implement this real-world-lighting pilot before VE4 regional atmosphere and the remaining VE5/VE6 passes. Do not restore reverted VE5C bloom.  
**System ownership:** `SYSTEMS/Lighting/` (the existing repository folder is plural `SYSTEMS`, not `SYSTEM`).

## Outcome and scope

Deliver genuinely lit/dark 2D battlefields using **Godot `CanvasModulate` + native `PointLight2D`**. A Citadel and completed Battle Towers emit soft cool light that falls off smoothly onto nearby metal, Foundation and other compatible ground art. Areas away from those structures remain dimmed by configurable ambient lighting. **Basic real shadows now belong in LGT0-B:** all completed placed towers and painted procedural mountains cast directional shadows where qualifying lights reach them. **No new gameplay visibility**: darkness/shadows must not affect AI, targeting/LOS, stealth, pathfinding, collision or damage. No normal-map rewrite, postprocessing/bloom or gameplay light-detection logic in LGT0.

Reference: [Godot 4.6 2D lighting and shadows](https://docs.godotengine.org/en/4.6/tutorials/2d/2d_lights_and_shadows.html), [PointLight2D](https://docs.godotengine.org/en/stable/classes/class_pointlight2d.html), [Light2D](https://docs.godotengine.org/en/stable/classes/class_light2d.html) and [GPU optimization](https://docs.godotengine.org/en/stable/tutorials/performance/gpu_optimization.html).

### Facts verified in the repository

- `LEVELS/Runtime/Battlefield.tscn` contains `World` (`WORLD/World.tscn`) and separate `UI` CanvasLayer; towers are under `World/Towers`, with `TowerRegistry` already listening to child arrival/removal. Reuse production lifecycle; no new full-scene scans every frame.
- The Citadel artwork in `WORLD/OBJECTIVES/Citadel/Citadel.tscn` is **offset** from its root. Position its light at the actual artwork/reactor centre; Battle Tower lights should follow their stationary base, not the rotating/recoiling gun.
- **F3 is already the route preview** in `DEBUG/LevelTools/LevelDebugController.cs`; **F4** is the existing performance panel, and **F5** dumps a profile. Keep all three bindings.
- `WORLD/VISUALS/LevelAtmosphereController` owns VE1 stars/nebulae, not physical 2D lighting. Existing VE2 portal/Citadel glow and VE3 decal shaders are emissive-looking visuals; **they do not illuminate neighbouring tiles**. Don't replace their materials.
- `WORLD/VISUALS/Lighting/README.md` confirms the old VE5C HDR/bloom test was reverted. Never re-enable it incidentally.

## Implementation milestone LGT0 (implement ONLY this, then stop)

### A. Shared system in SYSTEMS/Lighting

1. Add `WorldLightingController.cs`, a **world-scoped** lighting coordinator mounted in the shared `WORLD/World.tscn`, **not** a permanent autoload. Centralize active lighting state, `CanvasModulate`, light source registrations, budgets and cleanup in one place.
2. Add `LightingMasterConfig.cs` and `DefaultLightingConfig.tres` with Inspector-editable `Enabled`, ambient tint, default state, light/source budget, optional viewport culling settings, **`ShadowsEnabled` (default true)**, shadow quality/tint, a separate maximum shadow-enabled-light budget and debug comparison permission. Lighting OFF must restore the original undimmed baseline.
3. Add `LightSourceConfig.cs` plus reusable per-source preset resources, initially **Citadel** and **Battle Tower**. Expose Enabled, tint, energy/brightness, radius/texture scale and local offset. Use one cached light texture and native lights, not per-source procedurally generated textures.
4. Create a small shared soft radial gradient texture (e.g. 256px) whose outer border fades fully transparent. True light intensity/falloff comes from the native PointLight2D, not an additive sprite pretending to cut a hole in darkness. Radius must be calibrated against the actual light texture/scale and 64px tile grid.
5. Keep this plan and add a short `SYSTEMS/Lighting/README.md` when the implementation is delivered. World/scene-specific nodes may live under their natural scene parent, but the controller, configs, presets and shared helper code stay here.

### B. Baseline visual values and placements

- Prototype **ambient `#65727e`** (gentle industrial dusk, not pitch black). Inspector changes can later support deep-night or planet-specific palettes. Do not hardcode a night mode onto every existing level.
- Citadel preset: cool cyan/teal light, approximately **4–6 map cells** of visible radius (256–384 world px), softly fading. Multi-Citadel maps get separately anchored lights.
- Battle Tower preset: pale cyan/white, approximately **2.5–4 cells** (160–256px), soft edge. Start with one tower then several, not all tower types.
- Use normal additive light blending. **LGT0-A** first establishes real lighting without shadows for a baseline; **LGT0-B** enables basic native `LightOccluder2D`/`OccluderPolygon2D` shadows from towers and mountain clusters. No `DirectionalLight2D`, per-tile normal maps, bloom or screen-space glow pass.
- Apply ambient to the **world**, not the HUD/menu CanvasLayer. Test whether existing shaders marked `unshaded` opt out of light/ambient; those may intentionally remain emissive. Preserve the terrain E2 mask material behavior and don't blanket-replace TileMapLayer materials.
- Placement ghosts, off-tree preview scenes and in-progress construction do **not** emit light. When a real tower finishes construction its light may enable. Destroying/selling/unloading unregisters and frees it. Check save/Continue restore and upgrade/disable paths without changing save data.
- Future tower types opt in as light emitters via typed source presets/attachments, not tower-ID switches. **Only Citadel and Battle Tower emit light in LGT0, but ALL finished placed tower types cast simple shadows.**

### B2. Basic tower and mountain shadows — LGT0-B (approved)

Use **Godot native `LightOccluder2D` + `OccluderPolygon2D`** with `PointLight2D.ShadowEnabled` and matching light/occluder shadow masks. The resulting 2D shadow extends away from **each actual light position**, not from a global assumed sun direction. It only affects terrain/sprites in lit regions; it is visual-only and does not modify path blocking or shot-time mountain LOS.

- **Which objects cast?** **ALL real completed placed tower types** (whether they emit light or not) plus **only impassable tiles tagged `mountain_visual` for procedural mountains**. No ordinary walls/Foundation, Citadel hull, enemy ships, drones, bullets or VE3 decal tiles as new occluders. Light emitters remain Citadel and Battle Tower in LGT0.
- **Tower occluders:** use one reusable *low-vertex* silhouette per operational tower, based on its authored `TowerDefinition.Footprint` and stationary root (a simple inset rectangle/octagon or small per-type override where needed). Reuse the existing `TowerRegistry` membership and tower `IsOperational`/work-state/destruction lifecycle. Unfinished construction/preview ghosts do not cast; upgrading tower may retain its silhouette. Remove/update on sale, destruction, transform change, and checkpoint reconstruction. **No geometry per barrel/recoiling gun and no per-tower shadow `_Process` loop.**
- **Tower light self-shadowing:** a point light placed inside its own occluder may black out the light pool or create a black ring. Explicitly test Citadel→Battle Tower and Battle Tower→nearby tower shadows. Keep illumination clear around the emitter itself using a practical mask/offset/footprint solution that *still allows tower-to-tower shadows*. Do not spend one mask bit per individual tower or weaken the light system just to avoid the issue.
- **Mountain occluders:** `WORLD/TERRAIN/Mountains/MountainTerrainVisual.cs` already consumes `mountain_visual` TileData and caches raised rock/shadow meshes when `WorldGrid.TerrainRebuilt` occurs. Derive the 2D occlusion outlines from that **same marked-cell information/rebuild lifecycle**, not a second terrain-neighbour poll or the separate `blocks_tower_sight` flag. Prefer **coalesced connected mountain-group outlines** (bounded/chunked outlines for huge or complex regions), not a `LightOccluder2D` per painted cell or per mesh triangle. Use conservative projected silhouettes matching actual mountain artwork, cache static polygons, and free/rebuild only when terrain changes.
- **Existing mountain cosmetic shadow:** the current mountain renderer already draws a static colored shadow mesh. `MountainRock.gdshader` is `unshaded`, so mountain facets may remain intentionally baked-lit while the *surrounding* terrain receives real light and cast shadow. Do not replace its shader, E2 terrain-edge material or authoring metadata. Compare for obvious double-darkening/offset outlines; leave existing art untouched unless an observed visual bug warrants a separate correction.
- **Inspector quality and budget:** master `ShadowsEnabled` defaults **true** for LGT0-B, separately switchable without turning off lights. Start with **hard shadow filter = None** for speed; optionally allow PCF5 as a measured softer setting, but **no PCF13** in this pass. Use moderate shadow tint/opacity instead of pure black. Initially allow at most **6 shadow-enabled lights** (separate from 16 total lit sources) and prioritize consistently: visible Citadel/important sources first, no frame-to-frame flicker. The cap counts shadow-processing *lights*, not all occluder polygons. Culling of distant casters should be coarse/bounded, not a per-frame full-scene scan.
- **Performance and scene lifetime:** one simple caster node per completed tower is acceptable; **never one per tile**. Cache mountain occluders per connected area, no extra terrain scan each frame; create/free with map and tower events. Compare light/shadow GPU costs on the user's GTX 1080 during heavy battles. Shader shadow masks must not darken the HUD or UI.

**Visual acceptance:** a non-emitting tower must cast a directional shadow when lit by a Citadel or neighbouring Battle Tower; a contiguous painted mountain cluster must cast a credible, continuous directional shadow. Light sources on opposite sides produce correspondingly different shadows. The tower that emits light must not cast an unnatural self-shadow ring over its own base. **F6 OFF removes ambient darkness, real lights and their shadows**; the separate `ShadowsEnabled=false` switch keeps illumination but removes cast shadows. Source geometry never changes gameplay blockage.

### C. User requested debug on/off comparison

- **F6 toggles lighting ON/OFF**, debug builds only, in playable battlefield scenes. Preserve plain F3 route preview and its UI, F4 performance overlay, F5 profiler. Ignore key release/repeat; use appropriate existing debug owner or scene-scoped input binding, no global duplicate listener.
- Godot Editor uses F6 to Run Current Scene when the **editor** has focus; that shortcut does not consume F6 inside the running game window. Keep this distinction clear in testing.
- Controller exposes one method `SetLightingEnabled(bool)` or equivalent; all managed lights and the ambient system obey this state. OFF means **white/unmodulated ambient AND all managed PointLight2D disabled, including their shadows**; disabling lights alone while leaving a dark world is incorrect for this comparison. ON restores configured ambient, eligible sources and approved shadows. A separate Inspector `ShadowsEnabled=false` must leave ambient and real illumination active for an accurate lighting-without-shadows comparison.
- A tiny debug toast/readout `LIGHTING ON` / `LIGHTING OFF` is optional; a cheap status line on the existing F4 overlay is acceptable, but don't expand it into a new window.
- Debug override is runtime-only (not saved in campaign checkpoints), resets when a new battlefield opens and cannot strand menus/Results in darkness. Inspect pause/unpause, route-preview coexistence and teardown.

### D. Performance and lifecycle contract

- Initial **provisional budget: 16 simultaneously enabled PointLight2D sources** at most, with a **separate maximum 6 simultaneously shadow-enabled lights** in LGT0-B (both Inspector adjustable). Begin with 1 Citadel + 1–6 towers, comparing 0/2/4/6 shadow-enabled light sources. If several sources overlap, use stable deterministic priority; prefer visible Citadel and nearby relevant structures, no flicker when exceeding cap.
- Large lights illuminate more pixels; many overlapping lights and especially soft shadow maps can be costly. Benchmark **LGT0-A shadowless first, then LGT0-B with shadows enabled**, not assuming either is free on GTX 1080.
- Reuse a shared gradient texture. No per-light or per-tower C# `_Process`, `Timer` or animation callback; static light transform updates only on needed source movement or registration.
- Prefer membership events (existing `Towers` child hooks and Citadel map ownership) plus a small cached visible-source list. Offscreen candidates should be disabled using a bounded/low-frequency camera visibility refresh (e.g. 0.25–0.5s when needed) or one practical engine visibility callback, **not** scanning every enemy/tower per physics frame.
- Do not allocate a light per VE3 glowing decal, per enemy, projectile or short-lived muzzle flash. Do not add power-grid calculations, fog-of-war, visibility queries or new save fields.
- **Performance goal, not measurement:** strive for roughly **≤1ms incremental GPU cost** at common light counts on a busy 1920×1080 battlefield while maintaining the wider 60FPS goal. No FPS guarantees without the owner's hardware benchmark; F4 CPU timings alone are not GPU timings.

### E. Deliverables and validation (lightweight, owner-led)

Implement in **two small sequential milestones**: **LGT0-A**, native ambient + Citadel/Battle Tower lights + F6 and source lifetime; **LGT0-B**, ONLY basic tower/mountain shadow occluders on the same foundation. Deliver both before declaring the owner's lighting-and-basic-shadows scope complete. Use one focused commit per milestone if practical, log briefly in `NOTES/WORK.md`, push `dev` and do not expand into LGT1–LGT4.

Focused source review / one practical build if available, then leave desktop appearance/performance acceptance to owner:

1. Same camera position/zoom with ON vs OFF, confirming real light falls on underlying floor art and OFF restores original colors without darkening HUD, F3 routes or menus.
2. 1, 4, 8, 16 lights with and without overlap; separately compare **shadow OFF and 2/4/6 shadow-enabled sources** using 1×1 and larger-footprint towers, isolated and connected mountains, and lamps on opposite sides of an occluder. Use a heavy battle with **50/200/500 enemies** for realistic comparison. Record FPS, CPU frame, **GPU frame time where measurable**, renderer, resolution and zoom. If GPU time is unavailable state so, don't invent it.
3. Creation/finished construction, sale/destruction, upgrades, checkpoint restoration, multiple Citadels, terrain rebuild, camera pan/zoom, pause, retry and level transitions. Check no detached occluder nodes and no black self-shadow ring around emitters. No ghost lights, input conflicts, leaked nodes or altered gameplay.
4. Test existing shader materials and optional E2 mask against the light; no unrelated Decorative/Mars atlas lighting or shared material edits.

## Later opt-in steps — NOT PART OF LGT0

- **LGT1:** opt more towers, manually placed industrial lamps/VE3 small decal fixtures into shared source preset system. Never make a native light for every lit atlas cell. Optional power-loss responses only using authoritative source state, later.
- **LGT2:** later **advanced shadow polish only**: detailed hand-authored hulls, optional additional large obstacle casters and measured softer PCF filtering. Basic tower/mountain shadows now belong in LGT0-B, not LGT2. Terrain shot-time LOS remains independent.
- **LGT3:** authored day/night darkness presets, selected slow light pulses/failed-lamp flicker and per-level identity, event-driven and capped.
- **LGT4:** actual light-driven stealth/reveal/fog-of-war is a **separate gameplay feature** requiring explicit design; it is not provided by visual darkness.
- **VE5C bloom:** reverted and out of scope. No global HDR/glow restoration.

**Decision:** Real Godot 2D lights, followed by **basic all-tower and procedural mountain shadows**, are the approved first implementation. If shadows cost too much, reduce shadowed-source count, filter quality, caster geometry, radius and overlap. Do not silently omit LGT0-B or claim all basic shadows are complete after LGT0-A.
