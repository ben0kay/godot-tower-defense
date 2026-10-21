# Lighting — VE5

## VE5C: Cinematic Blue built-in bloom (contained visual test)

**Implemented in an isolated test scene; not enabled in normal gameplay.** Scene: `Tests/CinematicBlueBloomTest.tscn`. Press **F6** with that scene open in Godot, then press **B** to toggle built-in bloom ON/OFF. `BloomTestConfig.cs` exposes Inspector parameters; `Presets/CinematicBlueBloom.tres` contains the preset. `CinematicBloomTest.cs` creates one private Godot `Environment` on a test-only `WorldEnvironment` node and draws representative portal, Citadel, shield and metal highlights. It does not instantiate actual gameplay scenes.

No `project.godot` edits, global HDR 2D switch, autoload, copied per-object material, game level change or new gameplay dependency. This test deliberately uses the project's existing Forward+ renderer and default SDR 2D viewport with a sub-1.0 glow threshold, so even ordinary bright colors can bloom; selectively glowing only specific objects would require HDR 2D or an explicitly isolated bright-pass technique. Don't mistake the low-opacity static blue backdrop halo for bloom: use B to compare actual post-process glow.

**Settings:** Enabled, Intensity, Bloom, Threshold, BackgroundTint, HaloTint, HaloOpacity. Toggle is test scene only. If the full-screen bloom is overly broad, lower Bloom or raise Threshold. Global bloom applies to the whole test viewport; gameplay integration would need review of overlays/HUD and GPU timings first. HDR 2D is intentionally NOT enabled because doing so globally can alter opacity, sprite colors, and text rendering. Desktop GPU appearance/performance is **unverified**.

## Upcoming

VE5A/5B: ambient tint and selected actual 2D lights. VE5D: optional normal maps/occluders. VE5E: profiling and finalized opt-in presets. Read WORLD/BACKGROUNDS/README.md for full VE1–VE7 plan.
