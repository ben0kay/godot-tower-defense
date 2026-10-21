# WORLD/VISUALS

VE1 implemented as an optional level-local star twinkle and nebula drift overlay. VE2–VE7 remain planned in WORLD/BACKGROUNDS/README.md.

LevelAtmosphereConfig.cs provides Inspector flags, tints, opacity, density, drift, twinkle and extent. LevelAtmosphereController.cs instantiates at most two ColorRect shader quads on _Ready, creates a separate material per level, and disables C# process callbacks. Shaders/ owns the two GPU shaders; Presets/ owns six VE1 visual palettes. Lighting/, Atmosphere/, Materials/ and Shaders/Shared/ reserve later VE duties.

Level001 opts in with CalmSpace under SpaceBackground/VisualAtmosphere. Switch off through VisualAtmosphere.visible=false for immediate comparison (reload); template includes same component initially hidden. Existing static SVG and painted terrain are untouched. The procedural stars form an independent layer, not animated dots inside the SVG. The default overlay covers 6400x5120 pixels from origin; adjust CoveragePixels for other map sizes. Renderer-dependent appearance and GPU throughput need Godot desktop verification.
