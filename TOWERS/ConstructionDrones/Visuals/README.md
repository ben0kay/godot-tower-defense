## Smooth shader projection — 2026-10-27

The construction-drone cone now uses **one cached four-vertex polygon**, rather than drawing three superimposed translucent polygons with distinct sharp boundaries. `ConstructionBeamGradient.gdshader` provides a continuous cyan falloff across its width with fragment-level `smoothstep` and pixel-derivative edge feathering. The nozzle remains brighter, while ground contact keeps a visible (but slightly softer) center. There is no texture render-to-cache, low-resolution viewport, noise, `TIME`, shader animation, or texture lookup.

The shared `ShaderMaterial` is attached to the `Cone` scene node; **no per-drone material copies or per-frame shader parameter writes** are needed. The beam's existing cached per-vertex tint/opacity and CanvasItem deployment alpha still work. Its four-point geometry redraws only on footprint/style changes; hovering moves the same affine transform. Animated scan bands remain a separate aligned child, capped at 24 Hz; ground/nozzle glows remain cached. Construction, drone movement, opt-in ShowVisuals, pooling, pause/save, and the 280 px/s flight default are unchanged.

Performance expectations: this replaces three polygon fills plus edge strokes with one fill and one small shader, but GPU pixels still render each frame; FPS improvement is not guaranteed until measured. The viewport's pixel scaling/zoom can still expose individual pixels at low resolution. Desktop acceptance: compare beams at normal gameplay zoom, 1×1 and 3×2 construction, and three overlapping beam sites; check Godot shader import and F4 timings. Source/test references updated; no extensive runtime suite executed.

---

## Seamless projection polish — 2026-10-27

A user screenshot showed a washed-out/broken middle to the construction projection. The earlier cone ended at just **15% of its source opacity**, while the 24-Hz scan bands were rebuilt in the beam parent and the cone followed the drone through a different CanvasItem transform.

The cone now consists of **three cached, full-length, translucent layers**: wide low-alpha edge feather, broad continuous body, and restrained brighter inner channel. It no longer fades almost to zero at ground contact. Sharp bright edges and the ground ring are softened. The unchanged four-point nozzle-to-site basis uses one affine transform, with no redrawing of the cone for hover, orbit or beam animation.

A small `ConstructionBeamScan` child follows **the exact same transform** as the cone every manager tick, so moving scan bands cannot visually lag behind the beam. Only its scan strokes redraw at a capped **24 Hz**, and no additional _Process/_PhysicsProcess callbacks, shader, particles or new textures are introduced. All layers remain hidden/dormant when not in use. The existing beam settings, 280px/s travel speed, construction clocks, save compatibility and gameplay are unchanged.

Static review and a focused transform/cache regression check added; live Godot visual acceptance and FPS measurement remain for desktop testing (especially three simultaneous beams and large footprints).

---

## Cached cone and scan-only redraw — 2026-10-26

The construction beam now uses a static cached `ConstructionBeamCone` canvas (four-vertex fill and edge lines) and two cached `ConstructionBeamGlow` canvases (site contact and nozzle). These are drawn once per footprint/style assignment. The regular builder physics tick can move/hover the drone, changing only the cached cone's affine transform and the contact's position; **the static cone is not `QueueRedraw()`-ed every frame**. During the short 0.25s deployment, CanvasItem alpha updates without rebuilding geometry. `ConstructionBeam._Draw()` now handles **only the moving scan bands**, refreshing at up to 24 Hz from the same manager-supplied time; 0 configured bands do not animate. No additional per-node processing or physics, new shaders, particle systems, timers or gameplay changes.

The visual change deliberately leaves the static cone/contact/nozzle brightness constant while beaming; the existing pulse remains on scanning lines. Pause, visuals-off, pool reuse, completion and destruction hide the entire beam as before. Focused test checks assert cone geometry remains cached over repeated hover ticks. **FPS savings are unmeasured** and must be assessed in the desktop Godot renderer, comparing three active beams with the prior behavior.

---

## CD3 visual polish — 2026-10-25

The existing footprint-sized four-vertex cyan cone now gains manager-driven scrolling scan bands (four by default), a subtle adjustable brightness pulse, brighter nozzle glow, and a faint site-centered contact halo. All are drawn with lightweight canvas primitives using retained arrays. The **same single manager** passes hover age during deployment/beaming; there is no additional per-drone processing, GPU particle system, shader, physics, or combat logic.

`ConstructionCompletionFlash.cs` is one shared world-space Node2D created under `ConstructionDrones/CompletionFlashes`. It draws a quick cyan ring and core at each successfully completed **initial** construction site. Its fixed 64-slot value buffer drops excess flashes only; it processes for the short effect lifetime, then sleeps. Completion remains instantaneous from the work manager, the beam hides as before, and the drone returns independently. No flash occurs for a cancelled site, failed placement, upgrade, missing builder fallback, or `ShowVisuals=false`.

Edit `DefaultConstructionDroneConfig.tres` for `BeamScanBands` (0 hides bands), `BeamScanCyclesPerSecond`, `BeamPulseHz`, `BeamPulseDepth`, `CompletionFlashEnabled`, `CompletionFlashSeconds` and `CompletionFlashOpacity`. The old `BeamColor`/`BeamOpacity` still set the base hologram. All visuals remain disabled by the existing `ShowVisuals=false` switch; `Enabled=false` preserves its original work fallback. Travel speed uses the shared 280 px/s default; no research, ETA, audio, completion timing or UI changes.

**Desktop acceptance still pending:** check 1×1 towers and larger 2×2/3×2 sites at 1920×1080 and 960×540, pause, overlaps, visual toggle, and the GPU cost of 20 active beams. Source/headless checks cannot establish on-screen glow quality.

---

# Construction drone visuals

`ConstructionDrone.svg` is the shared imported36×28 dark/cyan builder, separate from ammunition courier artwork. The scene reuses one texture; no runtime artwork generation or loading occurs during flight. Its nozzle is at local(0,7).

`ConstructionBeam.cs` draws the footprint-sized cyan hologram from the drone nozzle toward the site's fixed world center. A retained four-vertex polygon/color array and thin lattice/edge lines avoid particles, shaders, per-frame arrays and input/collision nodes. The manager supplies pose/deployment strength, and hides the beam immediately on work completion/cancellation. No construction time or combat logic lives here. Desktop appearance/GPU review remains pending; see ../README.md.
