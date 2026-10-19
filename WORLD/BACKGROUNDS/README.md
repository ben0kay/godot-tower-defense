# BACKGROUNDS — Cosmetic Space Scenery

Purely visual **non-tilemap** art intended to appear behind a level's physical terrain. Keep the reusable assets/scene components here; place instances in each level's `SpaceBackground` section.

- `Nebulae/`: large, positionable world-anchored `Sprite2D` images, optionally animated by shaders later.
- `Stars/`: starfield layers, optionally `Parallax2D` for depth.
- `Planets/`: distant planets, moons, silhouettes, other large decorative objects.

A nebula is **not a wall tile, an occupancy cell or a gameplay obstacle**. Leaving terrain cells empty allows it to show through. No collision, buildability, pathfinding or save-state requirements.

The live Level001 includes a world-anchored SVG placeholder starfield/nebula under SpaceBackground/Stars. WorldGrid now draws transparent lines/diagnostics rather than an opaque fill. Add or replace sprites without changing gameplay rules. Keep SpaceBackground at z=-20, behind terrain.

---

# Proposed level atmosphere and shader presets — VE1–VE4 (PLANNED)

**Status: design brief for Codex, not implemented.** This plan is a proposal based on the live level scenes and existing assets reviewed 2026-10-19. No shaders, visual presets, nodes or gameplay changes have been added by this document. Start with a small opt-in visual prototype and preserve all currently painted maps.

## What already exists

- The playable map scenes are `LEVELS/Level001/Level001.tscn`, `Level002/Level002.tscn` and `Level020/Level020.tscn`; new levels typically copy `LEVELS/LevelTemplate.tscn`.
- Each scene already exposes `SpaceBackground/{Stars,Nebulae,Planets}` and a scaled `Stars/Space` Sprite2D. **All three currently use the same static** `Stars/level001_space.svg` (a baked starfield and faint nebula); `Nebulae` and `Planets` are empty. Keep the backdrop world-anchored and visually behind playable terrain (currently `SpaceBackground.z_index = -20`).
- Terrain is painted into independent `TileMapLayer` nodes: `Terrain/Passable`, `Impassable`, `Interactive`, `Decorations`. Metal, Foundation, dirt and newer Mars dust/rock are actual authored tile assets, with gameplay rules maintained independently by `WorldGrid`.
- `WORLD/PORTALS/EnemyPortal/EnemyPortal.tscn` uses straightforward polygon Rim/Core shapes; `WORLD/OBJECTIVES/Citadel/Citadel.tscn` uses polygon Hull/Core shapes. They are suitable for **separate opt-in visual-only overlays**, not a replacement of their actual geometry or mechanics.
- There were **no live `.gdshader` or `.gdshaderinc` shader assets** in the reviewed repository. `WORLD/TERRAIN/Edges/` contains a separate **disabled scaffold** and unfinished masking/rendering plan. `FEEDBACK/CameraEffects/` is planned only. Recheck GitHub status when implementing: other jobs may land first.
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

## VE3 — Industrial metal and corridor details (moderate complexity)

**Scope:** opt-in decorative accent lighting on selected powered metal and Foundation surfaces, especially Level020.

- [ ] Design narrow cyan/amber conduit strips, occasional very subtle brightness pulses, inset indicator lights or a rare short energy sweep. Prefer **dedicated cosmetic overlay/decal tiles, material-masked atlas regions or cached local accent batches**, never a per-tile Node or per-tile timer.
- [ ] Respect the current multiple TileSet atlases and atlas UVs. A shader applied blindly to the whole `TileMapLayer` may animate unrelated tiles or repeat patterns incorrectly in atlas space. Decide whether to render on a selected overlay layer or use a verified UV/mask-aware material. No blanket glow over all floor textures.
- [ ] `MetalPulse.gdshader` should expose parameters for dark/light balance, pulse speed, emissive-looking tint, coverage/mask and per-level variation, with adjustable low-intensity presets `IndustrialBlue` and `IndustrialAmber`. Keep effects in the decoration layer unless visual-only TileSet ownership dictates otherwise.
- [ ] Level020: emphasize a few corridor junctions and Foundation wall edges, not every tile; Level001/002: sparse highlights around selected metal areas. No automatic repainting of user maps or mutation of gameplay TileData.

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

## Performance, reliability and testing contract

- **500+ enemies remains the performance target.** Use GPU `canvas_item` shaders for localized visual motion when appropriate, shared immutable shader code, cached materials and event-driven state changes. Do not add per-frame CPU texture regeneration, full-world scans, individual gameplay-time updates per decorative tile or effects linked to every enemy.
- Inspector-friendly *master config + preset resource* controls intensity, saturation, speed and an on/off switch for **each** effect. The disabled path returns to original pixels/layout. Document any uniform/ShaderMaterial copying needed to avoid accidental shared resource edits across levels.
- Maintain existing draw hierarchy: static/deep space `z=-20` behind terrain; surface-only accents do not accidentally cover towers/portals/Citadel; VFX must not obscure combat; foreground effects are narrowly authored.
- Honor visibility/culling and viewport zoom. Prefer capped very slow animation, limited overdraw, and avoid stacking translucent full-map quads or heavy screen reads. Compare GPU/frame time with VE effects on/off on Level020 (most demanding), especially at zoomed-out and 500+ enemies; don't promise performance without measuring it.
- Headless/integration tests: resources and presets load; shaders compile (Godot renderer/device confirmation may require a real graphical run); a new level inherits safe defaults; material binding doesn't alter cell counts/metadata/nav state; per-level overrides don't leak to other levels; disable produces original scene visuals; unload/reload unsubscribes and frees all runtime instances. Manually verify 1920×1080, 1366×768 and 960×540 displays at near/far zoom.
- Follow `NOTES/CodePreferences.md`: concise classes, explanatory script headers, `// =========================================================` above methods, grouped inspector exports, major-folder README, reuse common helpers. **One atomic GitHub commit per VE job**; update docs/tests and explicitly mark real test results. Do not combine VE1–VE4 into one massive untested visual rewrite.

**Recommended order:** VE1 standalone background shaders/presets → VE2 portal/Citadel animated accents → VE3 selectively powered industrial overlays → VE4 regional ambient effects. Stop after each phase for F5 appearance feedback before increasing intensity.

