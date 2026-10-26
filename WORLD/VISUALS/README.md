## Shared space clear color — 2026-10-25

The VE1 organic nebula pass deliberately removed the opaque rectangle from `WORLD/BACKGROUNDS/Stars/level001_space.svg`. All level scenes still reference this translucent SVG, but Godot's unspecified default viewport clear color made the uncovered space appear wrong or effectively blank.

`project.godot` now sets `rendering/environment/defaults/default_clear_color` to **#080E1B** (the SVG's original deep-navy base). This is a single project-wide 2D viewport background; no per-level solid rectangles, scene nodes, runtime polling, or GPU overdraw are required. The existing static SVG, organic nebula fade, opt-in VE1 shader overlays, map terrain and z-order remain unchanged. To retint base space, change the one setting in **Project Settings → Rendering → Environment → Defaults → Default Clear Color**. A future explicitly configured Viewport/Environment background can override it.

**Desktop check still required:** run Level001, Level002, Level020 and Level100, including camera pan/zoom and map edges; verify consistent dark navy behind stars with no rectangular seams. Toggle Level001's `VisualAtmosphere` off and reload to confirm the static starfield still has the correct base color.

---

## VE2 energy structures — 2026-10-21

EnergyFieldVisual.cs, EnergyFieldStyle.cs, Shaders/EnergyField.gdshader, Presets/PortalEnergy.tres, Presets/CitadelReactor.tres. The reusable shader selects a portal-vortex or reactor-core mode, with per-instance material parameters and optional event-only portal spawn flash. Inspector tune each preset. No 2D HDR/bloom setup, gameplay mechanics or new per-frame CPU hooks. VE5C bloom test removed as requested.

## VE1 organic background refinement — 2026-10-21

The early VE1 screenshot showed rectangular cloud bands and a hard-edged rectangle from the original baked backdrop. `Shaders/NebulaDrift.gdshader` now uses four-octave domain-warped clouds shaped by an irregular elliptical fade rather than rectangular edge feathering. The original `WORLD/BACKGROUNDS/Stars/level001_space.svg` opaque solid background rectangle is removed, retaining the static dots and faint pre-baked elliptical nebula. The scene's underlying viewport clear color will show through transparent portions. **This makes the backdrop's base color depend on project/viewport clear settings**, so verify that the out-of-map space remains the intended dark-blue shade at normal zoom. No terrain or other gameplay layer is changed.

If the background becomes unexpectedly black or colored on a particular renderer, configure a shared solid-color backdrop covering the visible camera area *behind the artwork*, rather than reintroducing the old finite opaque SVG rectangle. Smooth organic shapes and draw-coverage edges are separate concerns. Actual desktop verification is pending.

# WORLD/VISUALS

VE1 implemented as an optional level-local star twinkle and nebula drift overlay. VE2–VE7 remain planned in WORLD/BACKGROUNDS/README.md.

LevelAtmosphereConfig.cs provides Inspector flags, tints, opacity, density, drift, twinkle and extent. LevelAtmosphereController.cs instantiates at most two ColorRect shader quads on _Ready, creates a separate material per level, and disables C# process callbacks. Shaders/ owns the two GPU shaders; Presets/ owns six VE1 visual palettes. Lighting/, Atmosphere/, Materials/ and Shaders/Shared/ reserve later VE duties.

Level001 opts in with CalmSpace under SpaceBackground/VisualAtmosphere. Switch off through VisualAtmosphere.visible=false for immediate comparison (reload); template includes same component initially hidden. Existing static SVG and painted terrain are untouched. The procedural stars form an independent layer, not animated dots inside the SVG. The default overlay covers 6400x5120 pixels from origin; adjust CoveragePixels for other map sizes. Renderer-dependent appearance and GPU throughput need Godot desktop verification.
