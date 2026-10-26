## CD3 visual polish — 2026-10-25

The existing footprint-sized four-vertex cyan cone now gains manager-driven scrolling scan bands (four by default), a subtle adjustable brightness pulse, brighter nozzle glow, and a faint site-centered contact halo. All are drawn with lightweight canvas primitives using retained arrays. The **same single manager** passes hover age during deployment/beaming; there is no additional per-drone processing, GPU particle system, shader, physics, or combat logic.

`ConstructionCompletionFlash.cs` is one shared world-space Node2D created under `ConstructionDrones/CompletionFlashes`. It draws a quick cyan ring and core at each successfully completed **initial** construction site. Its fixed 64-slot value buffer drops excess flashes only; it processes for the short effect lifetime, then sleeps. Completion remains instantaneous from the work manager, the beam hides as before, and the drone returns independently. No flash occurs for a cancelled site, failed placement, upgrade, missing builder fallback, or `ShowVisuals=false`.

Edit `DefaultConstructionDroneConfig.tres` for `BeamScanBands` (0 hides bands), `BeamScanCyclesPerSecond`, `BeamPulseHz`, `BeamPulseDepth`, `CompletionFlashEnabled`, `CompletionFlashSeconds` and `CompletionFlashOpacity`. The old `BeamColor`/`BeamOpacity` still set the base hologram. All visuals remain disabled by the existing `ShowVisuals=false` switch; `Enabled=false` preserves its original work fallback. Travel speed remains 420 px/s; no research, ETA, audio, completion timing or UI changes.

**Desktop acceptance still pending:** check 1×1 towers and larger 2×2/3×2 sites at 1920×1080 and 960×540, pause, overlaps, visual toggle, and the GPU cost of 20 active beams. Source/headless checks cannot establish on-screen glow quality.

---

# Construction drone visuals

`ConstructionDrone.svg` is the shared imported36×28 dark/cyan builder, separate from ammunition courier artwork. The scene reuses one texture; no runtime artwork generation or loading occurs during flight. Its nozzle is at local(0,7).

`ConstructionBeam.cs` draws the footprint-sized cyan hologram from the drone nozzle toward the site's fixed world center. A retained four-vertex polygon/color array and thin lattice/edge lines avoid particles, shaders, per-frame arrays and input/collision nodes. The manager supplies pose/deployment strength, and hides the beam immediately on work completion/cancellation. No construction time or combat logic lives here. Desktop appearance/GPU review remains pending; see ../README.md.
