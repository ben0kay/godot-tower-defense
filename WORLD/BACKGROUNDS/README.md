## VE2 implementation — 2026-10-21

Shared WORLD/VISUALS/Shaders/EnergyField.gdshader now animates portal inner swirl and Citadel teal reactor, independently per scene with low-cost shader time. Successful portal spawns briefly pulse the matching visual via a tween; no failed attempts or map edge spawns trigger it. The built-in bloom experiment was removed. The rest of VE3–VE7 remains planned; live Godot appearance testing is pending.

---

## VE1 implementation — 2026-10-21

VE1 now has a reusable opt-in LevelAtmosphereController, six Inspector preset resources, StarTwinkle and NebulaDrift shaders and the modular WORLD/VISUALS folders. Level001 uses CalmSpace behind terrain; LevelTemplate includes the disabled-by-visibility scaffold. Existing star SVG and painted maps are preserved. See WORLD/VISUALS/README.md. VE2–VE7 remain planned; desktop appearance/GPU checks remain.

---

# BACKGROUNDS — Cosmetic Space Scenery

Purely visual **non-tilemap** art intended to appear behind a level's physical terrain. Keep the reusable assets/scene components here; place instances in each level's `SpaceBackground` section.

- `Nebulae/`: large, positionable world-anchored `Sprite2D` images, optionally animated by shaders later.
- `Stars/`: starfield layers, optionally `Parallax2D` for depth.
- `Planets/`: distant planets, moons, silhouettes, other large decorative objects.

A nebula is **not a wall tile, an occupancy cell or a gameplay obstacle**. Leaving terrain cells empty allows it to show through. No collision, buildability, pathfinding or save-state requirements.

The live Level001 includes a world-anchored SVG placeholder starfield/nebula under SpaceBackground/Stars. WorldGrid now draws transparent lines/diagnostics rather than an opaque fill. Add or replace sprites without changing gameplay rules. Keep SpaceBackground at z=-20, behind terrain.

---

# Proposed level atmosphere, lighting and combat ambience — VE1–VE7 (PLANNED)

**Status: staged design brief for Codex, not implemented by this plan.** This plan is a proposal based on the live level scenes and existing assets reviewed 2026-10-19. No shaders, visual presets, nodes or gameplay changes have been added by this document. Start with a small opt-in visual prototype and preserve all currently painted maps.

## What already exists

- The playable map scenes are `LEVELS/Level001/Level001.tscn`, `Level002/Level002.tscn` and `Level020/Level020.tscn`; new levels typically copy `LEVELS/LevelTemplate.tscn`.
- Each scene already exposes `SpaceBackground/{Stars,Nebulae,Planets}` and a scaled `Stars/Space` Sprite2D. **All three currently use the same static** `Stars/level001_space.svg` (a baked starfield and faint nebula); `Nebulae` and `Planets` are empty. Keep the backdrop world-anchored and visually behind playable terrain (currently `SpaceBackground.z_index = -20`).
- Terrain is painted into independent `TileMapLayer` nodes: `Terrain/Passable`, `Impassable`, `Interactive`, `Decorations`. Metal, Foundation, dirt and newer Mars dust/rock are actual authored tile assets, with gameplay rules maintained independently by `WorldGrid`.
- The original 2026-10-19 review saw polygon placeholders for portals and the Citadel; these references are historical and must be rechecked against newer SVG scene updates. They are suitable for **separate opt-in visual-only overlays**, not a replacement of their actual geometry or mechanics.
- At the time of the original 2026-10-19 review, there were **no live `.gdshader` or `.gdshaderinc` shader assets** in the reviewed repository; that snapshot is historical. `WORLD/TERRAIN/Edges/` contains a separate **disabled scaffold** and unfinished masking/rendering plan. `FEEDBACK/CameraEffects/` is planned only. Recheck GitHub status when implementing: other jobs may land first.
- Level001 is a compact single-portal combat map; Level002 is a relatively simple two-portal tutorial; Level020, **The Five Gates**, is a large five-portal maze of metal/Foundation walls and corridors. All should stay readable at ordinary gameplay zoom.

## Desired look / guiding principles

Subtle animated sci-fi ambience: **dark blue/teal empty space**, slow drifting nebula haze, rare star twinkles, carefully placed cyan/amber industrial details, living energy portals, and a softly pulsing Citadel reactor. Keep the battlefield high-contrast so dark vector enemies, path markers, tower range rings, attacks and projectiles remain obvious. Avoid making the *entire level* pulse. The initial implementation should feel like restrained background ambience, not a full-screen post-processing filter.

**Visual-only contract:** no changes to tile IDs, `can_build`, `blocks_enemies`, reservations, portal spawn positions, assigned Citadels, navigation, wave balance, gameplay light/damage calculations, collision shapes, save data or gameplay input. Shaders and materials should never become authoritative game state.

## Proposed reusable file structure (create only when implementing)

```text
WORLD/
  BACKGROUNDS/
    README.md                        <- source design brief for VE1–VE4
    Stars/                           <- existing static backdrop and future twinkle layer
    Nebulae/                         <- future positionable clouds
    Planets/                         <- future decorative celestial sprites
  VISUALS/                            <- proposed shared, opt-in visual library
    README.md
    LevelAtmosphereConfig.cs         <- optional per-level Resource with toggles/intensities
    LevelAtmosphereController.cs     <- binds visuals per level; no enemy/tile polling
    Shaders/
      StarTwinkle.gdshader
      NebulaDrift.gdshader
      MetalPulse.gdshader
      EnergyField.gdshader
      Shared/
        Noise.gdshaderinc
        PulseMath.gdshaderinc
    Presets/
      CalmSpace.tres
      IndustrialBlue.tres
      IndustrialAmber.tres
```

Treat those names as **suggestions**, not files that exist. Prefer a small `[GlobalClass]` `Resource` with Inspector-editable `Enabled`, colors, intensity, drift/twinkle/pulse speed and optional layer switches, plus actual Godot `ShaderMaterial` preset resources with parameter overrides. Avoid a mandatory singleton, per-cell/particle nodes, or an oversized manager. Keep per-instance materials independent where level-specific uniforms differ; otherwise share resources intentionally. Only create `.gdshaderinc` functions if two or more shaders actually reuse them (`#include` from Godot shaders).

## VE1 — Space atmosphere (start here, low risk)

**Scope:** reusable star twinkle and nebula drift, configurable per playable level/template.

- [ ] Preserve the existing opaque baked SVG as the static fallback. **Do not claim that its individual baked stars can be separately animated** by simply assigning a generic uniform opacity shader to the whole sprite. Instead add a sparse star-point/twinkle overlay, star-mask texture or procedurally sampled star layer. Animate only rare stars at subtly different phases. Keep brightness low; don't wash out enemies over empty space.
- [ ] Optional `NebulaDrift.gdshader`: extremely slow UV distortion/translation on a **separate** transparent nebula sprite or generated mask, with soft falloff and no wrapping seams. Reuse `SpaceBackground/Nebulae`; the existing `Stars/Space` asset already contains a static nebula and can remain the base.
- [ ] Optional depth via `Parallax2D` for far stars only; preserve separately authored world-anchored nebulae for clouds tied to certain map regions. Verify camera pan, scroll/zoom, different aspect ratios and Level020's 6,400×5,120 world edges; never leave blank strips at map boundaries.
- [ ] Define restrained default `CalmSpace` with Inspector controls for star density, twinkle amplitude/frequency, cloud opacity/drift speed and tint. Default-off for already-authored levels until reviewed. Full disable must reproduce the current background without affecting map rendering.
- [ ] Verify the library works with Level001/002/020 and new copies of `LevelTemplate`, without hand-copying shader code to each level.

## VE2 — Energy structures (high impact, low/moderate risk)

**Scope:** visually enhance existing portals and the Citadel without changing their roots, hit tests or health mechanics.

- [ ] One shared **`EnergyField.gdshader`** (with separate preset/material parameters for different structure styles), or simple `Sprite2D/Polygon2D` visual children if the shader adds no value. Portal: gently rotating or rippling inner field, small cyan rim pulse, mild distortion. Use an appropriate UV/mask/overlay geometry; a raw Polygon2D may require explicitly authored UVs or its own overlay sprite.
- [ ] Subtle **spawn pulse**: trigger once from the existing successful portal spawn event (verify the right lifecycle before wiring), no new recurring scans or gameplay timer. Idle movement should remain much quieter than a real spawn.
- [ ] Citadel: slow teal reactor breathing effect with subtle outer glow; possibly a thin greenish ring pulse when its individual Credit buffer becomes ready to collect. Optional later damage-state amber/red shifts must use authoritative per-Citadel health events and respect multi-Citadel independence.
- [ ] Don't alter `WorldObjectFootprint`, clickable Credit hit areas, HUD overlays, independent Citadel buffers/health, portal assignments or F2/wave spawns. Different visuals must not interfere with top-centre Credits/HP UI.
- [ ] Use restrained alpha/additive overlay effects where needed. **Real fullscreen HDR bloom is not guaranteed** just by calling a material "glow"; don't require a new global post-process for this pass.

## VE3 — Industrial metal and corridor details (Codex handoff, planned)

**Status: APPROVED IMPLEMENTATION PLAN — NOT IMPLEMENTED.** The authoring model is now decided: **manually paint purely visual, transparent accent tiles onto the existing `Terrain/Decorations` TileMapLayer, over existing Passable or Impassable terrain**. Do not treat VE3 as a new terrain type, power-network subsystem, map auto-populator, or replacement for the E1–E3 terrain edge renderer.

### 1. Preserve current terrain and Decorations ownership

- Read `NOTES/CodePreferences.md`, root README, `WORLD/TERRAIN/Tiles/Decorative/README.md`, `WORLD/TERRAIN/TileSets/DecorationTerrain.tres`, `WORLD/TERRAIN/Edges/IMPLEMENTATION_PLAN.md`, `WORLD/VISUALS/README.md` and this plan first; inspect the existing level scene and TileSet resources before editing.
- **Verified starting point:** `WORLD/TERRAIN/TileSets/DecorationTerrain.tres` is a **64×64** TileSet with one `TileSetAtlasSource` at `sources/0`, using `Tiles/Metal/prototype_atlas.svg` and the existing painted coordinate `5:0/0`. Preserve source ID **0**, its atlas coordinates, that tile's appearance and all existing painted level references.
- Extend this **same TileSet resource** with new, dedicated industrial accent atlas source(s) (use the first confirmed unused source ID; never recycle existing IDs). No changes to `Terrain/Passable`, `Impassable`, `Interactive`, TileSet custom-data gameplay layers, `WorldGrid`, collision, construction, enemy navigation or terrain masks. A cosmetic overlay must not secretly become a blocking/buildable terrain source.
- All new accent art must have **transparent 64×64 cells with no opaque background or hidden solid fill**, aligned to the map's 64px tile grid. They overlay the existing floor/wall pixels instead of hiding their material. Keep trim thin enough not to resemble a buildable wall, path boundary or enemy.

### 2. Paintable atlas contents (small first pass)

Create a **small, editor-visible set** of industrial accents under `WORLD/TERRAIN/Tiles/Decorative/` (e.g. `IndustrialAccentsStatic.svg` and, only if warranted for shader isolation, `IndustrialAccentsAnimated.svg`), then register their `TileSetAtlasSource` entries in `DecorationTerrain.tres`. Prefer a compact grid of transparent 64px cells; document the exact atlas layout and source IDs after implementation.

| Paintable shape | Cyan variant | Amber variant | Intended placement |
| --- | --- | --- | --- |
| Narrow straight conduit (horizontal / vertical) | Yes | Yes | Metal corridors |
| Conduit 90° bend / corner | Yes | Yes | Corridor turns |
| Conduit T-junction / crossing | Yes | Yes | Corridor intersections |
| Foundation edge / inset illuminated strip | Yes | Yes | Selected Foundation boundaries |
| Tiny status light / warning indicator | Yes | Yes | Machinery or selected walls |

- Equivalent rotated variants may use stable atlas coordinates or Godot-supported tile transforms, whichever the editor authoring actually supports reliably. Verify orientation and no neighbour seams. Keep a static fallback for every essential decorative shape; animated variants are opt-in, not compulsory.
- **Do not** add a new background or fill-only metal tile; only subtle accents. Do **not** repaint any existing level in this pass, even Level020. The owner decides where to place each accent using the existing Decorations painting tools.

### 3. Reusable, isolated shader/material

- Implement `WORLD/VISUALS/Shaders/MetalPulse.gdshader` for optional soft local emissive-looking movement **on only the authored animated accent pixels**: a faint passing highlight or slow glow along a conduit, restrained indicator blink, or a short rare energy sweep. Set low-energy defaults rather than an always-flashing arcade effect.
- Expose editable **enabled/static fallback**, accent tint (cyan/amber), baseline brightness, pulse strength, speed/frequency, mask/coverage and optional phase variation. Provide reusable low-intensity preset materials for industrial cyan and amber. The existing `IndustrialBlue.tres`/`IndustrialAmber.tres` are **VE1 level-atmosphere presets**, not automatic per-tile glow materials; keep their identity/values intact.
- **Material isolation is a hard acceptance requirement:** never apply an animation shader blindly to the entire `Terrain/Decorations` layer or the existing `prototype_atlas.svg` source; this would animate unrelated decorations and may conflict with E2 masks or the wrong atlas UVs. First verify actual Godot 4 tile/atlas material behaviour against this project's assets. Prefer a dedicated *animated-accent atlas source* or genuinely per-tile material if verified to isolate rendering; if Godot cannot isolate animated cells in the existing Decoration TileMap safely, do **not** silently create per-cell nodes or repaint/migrate the layer. Document the limitation and propose a **separate strictly cosmetic overlay layer** as an explicit fallback for owner approval.
- Shader geometry must use correct local texture coordinates/masks so the pulse does not wrap across atlas cells, bleed at tile edges or illuminate the underlying terrain. Normal unlit/non-animated accents remain static, and disabling animation preserves the same artwork and placement.
- Do **not** require VE5 real 2D lights, shadow occluders, HDR or global bloom. Any visible glow is a restrained local color/alpha effect only. Shared shader code and shared immutable art are preferable; use independent materials only when instance-specific uniform changes truly require them.

### 4. Performance and separation from gameplay

- **No `Node2D`, `Timer`, `Tween` or C# `_Process` per tile.** Prefer the TileMap's existing batched drawing and shader `TIME` for gentle movement; avoid per-cell scene-tree entities, screen reads, heavy noise loops, CPU image generation or a new terrain-neighbour cache.
- Avoid full-map animated transparent overdraw. Limit accents to manually selected areas; start with a few junctions/strips, not a complete blinking labyrinth. Use a master off/static fallback or preset intensity 0 so VE3 can be compared with original appearance.
- **Do not connect glow to `PowerGridManager` in VE3.** The word "powered" describes the visual look; electrical outages, light failures and other gameplay reactions are possible later opt-ins, not part of this pass. VE4 regional dust, VE5 physical lighting and VE6 steam/lava are separate phases.
- All scenes, painted maps, mission behaviour, checkpoint formats and enemy/tower readability stay unchanged. Draw order must keep subtle accents behind ships, bullets, health bars, targeting lines and the UI.

### 5. Minimal Codex delivery / owner acceptance

- Limit this job to the two small accent atlas resources (or a verified simpler single-source layout), their **new TileSet source IDs**, the shared `MetalPulse.gdshader`/isolated presets, and a concise atlas-coordinate README. Do **not** automatically place accents in Level001/002/011/020, modify E2 masks, or start VE4/5/6/7.
- Check (lightly): existing Decorations art at `sources/0` and existing level TileMap serialized IDs are unchanged; all new tiles are visible and paintable in the Godot TileSet editor; texture cells/rotation line up at normal and zoomed-out scales; disabled animation leaves static art; only opted-in accent pixels animate; E2 and other existing layer shaders are not overwritten. No new gameplay data or processing loops.
- Owner checks in editor: on a **scratch map or non-committed test placement**, paint cyan/amber straights, corners and junctions over both Passable and Impassable; ensure underlying terrain remains visible, no changes in placement/LOS/navigation, no full-layer glow or shader bleed, and stable FPS at ordinary zoom.
- Keep source checks minimal per CodePreferences; update a short entry in `NOTES/WORK.md` and the decorative tile atlas README with the actual source ID/coordinates after coding; commit/push to `dev` only. Status becomes **Source Implemented / Awaiting Owner Validation** only when the assets and shader are actually delivered.

## VE4 — Regional ambience and atmospheric accents (optional)

- [ ] Low-frequency, low-opacity Martian dust wisps over selected broad regions, with region mask/authoring positions and sparse updates. A few batched instances or one region shader is preferable to thousands of moving particles.
- [ ] Occasional industrial lamp/fault flicker or tiny decorative sparks in authored spots only. Use resource presets and draw-order discipline: no effects obscuring enemy hulls, projectile muzzle/tracers, tower targeting, damage bars or path previews.
- [ ] Coordinate with `WORLD/TERRAIN/Edges/IMPLEMENTATION_PLAN.md`: Mars rounded silhouettes/real masked edges remain **E1–E3 terrain work**, not VE4. **Do not overlay pretty organic lines while the original square opaque tile remains visible.** Avoid a second independent terrain-neighbour cache.
- [ ] `FEEDBACK/CameraEffects/` can later provide optional impact shake; it is a separate gameplay feedback feature, not required for this atmosphere pass.

## Per-level atmosphere suggestions (not current scene settings)

| Level | Visual identity | Starting preset / accents |
| --- | --- | --- |
| **Level001** | Calm combat test in open space | `CalmSpace`: sparse white/cyan twinkles, faint slow blue haze; gentle Citadel reactor |
| **Level002** | Tutorial with two entry portals | `IndustrialBlue`: cooler teal ambience, two individual portal ripples, only a few glowing floor details |
| **Level020 — Five Gates** | Large multi-entry industrial labyrinth | `IndustrialAmber` or mix: restrained amber wall accents, intermittent powered conduits at chosen junctions, quieter dark-blue space haze behind the maze |
| **LevelTemplate** | Safe starting point for new maps | Opt-in atmosphere config with all nonessential effects disabled until designed; no compulsory art overrides |

## VE5 — Environmental lighting and emissive surfaces (new, opt-in)

**Goal:** give the top-down battlefield a cohesive lighting identity, not blanket bloom or thousands of lights.

- [ ] Investigate Godot 4 **`CanvasModulate`** for global ambient/tint darkness and carefully placed **`PointLight2D`** for a few localized cyan/amber industrial lights. Add `DirectionalLight2D` only where it contributes to readability. Never assume a 3D `WorldEnvironment` automatically supplies the desired 2D lighting.
- [ ] Define lighting presets and Inspector controls for ambient tint, baseline illumination, light color/energy/texture/radius and optional authored region accents. Keep weapon and enemy readability at standard zoom; avoid over-darkening red shield effects or HUD.
- [ ] Study optional `LightOccluder2D` for selected large structural shapes, and optional normal maps for special metal sprites/tiles. **Do not build occluders for every tile, enemy, or tower**; prove usefulness and measure the overhead before extending.
- [ ] Add decorative "emissive" floor strips, industrial lamps, warning indicators and reactor glow as baked sprites/masked overlay shaders when an actual light would be unnecessary. Document that visually glowing sprite coloring is distinct from actual 2D illumination and fullscreen HDR bloom.
- [ ] Keep post-process/HDR glow optional and renderer-verified; make an easy toggle and avoid conflicting with existing canvas shaders / terrain edge masks. Confirm the selected Godot renderer actually supports the intended glow path.
- [ ] Suggested `WORLD/VISUALS/Lighting/` ownership for shared resources/presets and placement examples, with region/level-specific *data* rather than a new script per light. The actual placed light belongs to each level's visual subtree.
- [ ] Ensure changing/toggling lights never changes navigation, gameplay detection/stealth, collision, building or attack calculations. **VE5 is aesthetic lighting only.**

## VE6 — Localized terrain atmosphere and environmental shaders (new)

- [ ] Extend VE4's authored regional ambience with **lava surface motion/glowing seams and optional subtle heat shimmer**, limited to actual applicable lava/decorative regions (check existing `WORLD/TERRAIN/Tiles/Interactive/Lava`). Do not assign an atlas-wide material that unintentionally animates other tiles.
- [ ] Create a reusable, gentle **steam/vent plume**, sparse smoke, damaged machinery sparks or small dust wisps for author-placed zones. Use a small number of shared `GpuParticles2D` emitters or masked shader regions; no individual process callback for each decoration.
- [ ] Support occasional **beacon blinks, hazard strobes and electrical panel flickers** around metal corridors with editable color, timing and falloff. Use deterministic or staggered shader phases where appropriate rather than CPU timers on every tile.
- [ ] Keep presets modular: `IndustrialNight`, `MarsDusk`, `DeepSpace` are suggested visual identities, not new level mechanics. Combine with VE1–VE5 without requiring every level to enable every effect.
- [ ] Coordinate with existing active E1/E2 terrain edge rendering and its opt-in material masking. Never replace or overwrite tile materials/cached mask state on a shared `TileMapLayer` without explicitly preserving all existing visual behavior.

## VE7 — Battle-responsive floating dust, debris and intensity (new)

**Visual direction:** quiet maps show rare, slow, tiny drifting motes. As a fight develops, slightly more particles become visible, with subtle lateral drift, occasional brighter sparks/ash and a touch of localized haze. Once combat subsides, density **eases back down** rather than shutting off abruptly. This is atmosphere, not a physical projectile or a game mechanic. Keep ships, shields, tracers and the build/Inspector UI clear. Dust must be visible but restrained, not a snowstorm.

### Ownership and suggested architecture

- [ ] Put the **world ambience** subsystem under `WORLD/VISUALS/Atmosphere/` (for example `BattleAtmosphereController.cs`, `BattleAtmosphereStyle.cs`, `BattleAtmosphere.tscn` and `DefaultBattleAtmosphereStyle.tres`). Keep visual impact bursts under existing `FEEDBACK/` if they represent specific hits/explosions. Do **not** duplicate the separate `FEEDBACK/Ricochet` or `DeathExplosion` systems.
- [ ] Use **one or a very small bounded set of `GpuParticles2D`** on world/viewport-sized visual planes and shared `ParticleProcessMaterial`/textures; do **not** instance an emitter per enemy, bullet, tower or collision. Use C# type casing exactly `GpuParticles2D` (Godot editor node `GPUParticles2D`).
- [ ] Prefer texture/particle resources prepared and cached ahead of time, GPU particle simulation, and infrequent event-driven intensity updates. **No per-frame C# dust particle positions, per-battle image generation, or mass scene-tree children.** Choose world-anchored vs camera-following carefully: camera-local screen dust may maintain coverage during pans/zooms but must not visibly jump or convey false world-relative motion.
- [ ] Build a small `CombatIntensityTracker` (or an equally simple component) from **already-available events/counters** such as shots fired, valid impacts, active enemy/tower engagement and wave state. First inspect the actual event/counter APIs; **do not add a scanning loop over all enemies/towers** or count every emitter as a new combat event. Consider rate-limiting or aggregated event sampling to avoid excessive signal traffic during dense battles.
- [ ] Define an intensity target clamped to **0..1**. Rise quickly during engagements, and decay slowly after engagement. Allow a low quiet-time dust baseline. Use time-based easing and hysteresis so intensity doesn't flicker between adjacent waves or on intermittent shots. Pausing freezes intensity progression and particle simulation; wave-end does not force immediate zero.
- [ ] Intensity should alter **emission density / relative amount, visibility or intensity and gentle particle speed** through a bounded smooth transition. Avoid repeatedly changing `Amount` or rebuilding a particle system every frame, as this may reset/resize particles; prefer emission controls that can vary smoothly, or coarsely update only when supported by Godot without resets. Never scale particles to explosion-like sizes just because combat is active.
- [ ] Keep a soft, limited spatial emphasis around the **currently active viewport/combat region** when feasible, not a million-pixel world-sized GPU emitter. No dependence on hidden enemies, per-ship particle collisions, or per-frame global node lookups. If global scene ambience would make dust appear through walls inappropriately, use explicit per-region layer masks/placement rather than collision simulation.
- [ ] Suggested **initial test only**: quiet baseline density 10–20% of battle maximum, rise time ~1.5–3 seconds, decay ~5–10 seconds, 1–2 subtle layers (fine cool grey motes and rare warm particles), max overall opacity low. All values Inspector-editable; start low and tune visually. Make the intensity algorithm reusable rather than hardcoding assumptions about Level020.

### Inspector and configuration

- [ ] Add **master atmosphere enable** and independent toggles for stars, nebulae, lighting, ambient weather/dust, battle response, regional particles and optional post-processing. The existing `FeedbackMasterConfig` controls combat feedback, while these *world* switches belong in `WORLD/VISUALS` or an appropriately scoped atmosphere config; don't conflate the two.
- [ ] Add an optional per-level `LevelAtmosphereConfig` resource (reference from the existing level configuration or a designated visual scene owner; follow the repo's data-driven authoring conventions). Support color/density/size/lifetime/drift speed, rise/decay, maximum particle budget and update cadence. Do not bake combat intensity settings into each painted tile.
- [ ] Expose a simple **debug preview override** (0..1 intensity) for visual testing and performance comparisons, with OFF restoring real event-driven intensity. Do not add a new unrelated debug hotkey if existing tools already have assigned keys.
- [ ] Ensure global OFF means the relevant emitters stop and resources are not needlessly instantiated, with safe cleanup on map unload and no lingering references across missions.

### Verification specifically for VE7

- [ ] Idle/fight/cooldown/second-wave cases: verify smooth buildup, bounded ceiling, natural fade and re-intensification, all with no sudden reset.
- [ ] Pause/resume, restart, level change, extreme camera pan/zoom, desktop resize and high-speed large waves; ensure no flashing particle sheet edges or unwanted background/UI occlusion.
- [ ] Test **500+ enemies and heavily firing towers** with VE7 ON vs OFF, measuring CPU physics, GPU/frame time and particle counts. If `GpuParticles2D` is too costly on low-end rendering targets, permit a fallback to pre-baked batched drifting sprites or a small shader overlay—still no per-particle C# updates.
- [ ] Regression: combat, kills/XP, explosion toggles, shields/ricochets, tile masks, F2 debug, HUD input and paths remain unchanged.

## Updated rollout strategy (same VE plan, gradual batches)

**One VE pass per Codex job**, with one commit including its README/tests. The complete VE1–VE7 plan is the shared multi-session backlog; **do not implement all seven in one giant untested commit**. Recommended dependency order: VE1 (backgrounds) → VE5 (basic lighting) → VE2 (energy accents) → VE3 (industrial strips) → VE4/VE6 (regional ambience) → VE7 (battle-responsive effects). VE7 can be prototyped sooner with one opt-in test scene and intensity debug override if desired. Stop between passes to evaluate real F5 screenshots and performance.

## Performance, reliability and testing contract

- **500+ enemies remains the performance target.** Use GPU `canvas_item` shaders for localized visual motion when appropriate, shared immutable shader code, cached materials and event-driven state changes. Do not add per-frame CPU texture regeneration, full-world scans, individual gameplay-time updates per decorative tile or effects linked to every enemy.
- Inspector-friendly *master config + preset resource* controls intensity, saturation, speed and an on/off switch for **each** effect. The disabled path returns to original pixels/layout. Document any uniform/ShaderMaterial copying needed to avoid accidental shared resource edits across levels.
- Maintain existing draw hierarchy: static/deep space `z=-20` behind terrain; surface-only accents do not accidentally cover towers/portals/Citadel; VFX must not obscure combat; foreground effects are narrowly authored.
- Honor visibility/culling and viewport zoom. Prefer capped very slow animation, limited overdraw, and avoid stacking translucent full-map quads or heavy screen reads. Compare GPU/frame time with VE effects on/off on Level020 (most demanding), especially at zoomed-out and 500+ enemies; don't promise performance without measuring it.
- Headless/integration tests: resources and presets load; shaders compile (Godot renderer/device confirmation may require a real graphical run); a new level inherits safe defaults; material binding doesn't alter cell counts/metadata/nav state; per-level overrides don't leak to other levels; disable produces original scene visuals; unload/reload unsubscribes and frees all runtime instances. Manually verify 1920×1080, 1366×768 and 960×540 displays at near/far zoom.
- Follow `NOTES/CodePreferences.md`: concise classes, explanatory script headers, `// =========================================================` above methods, grouped inspector exports, major-folder README, reuse common helpers. **One atomic GitHub commit per VE job**; update docs/tests and explicitly mark real test results. Do not combine VE1–VE4 into one massive untested visual rewrite.

**Earlier VE1–VE4 order (superseded by the VE1–VE7 rollout above):** begin with standalone background shaders/presets and review appearance after each pass. Do not treat this historical note as limiting the expanded plan.

