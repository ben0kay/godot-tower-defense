# Decorative Tile Overlays

Grid-aligned scratches, panel numbers, caution stripes, bolts, lights, grime and other art overlays belong on the level's `Decorations` TileMapLayer. They should **never change** tower placement or ship navigation.

Giant nebulae, background planets and moving starfields are better as independent scene sprites/parallax under `WORLD/BACKGROUNDS/`.

## VE3 — Industrial accents

Select `Terrain/Decorations` in the level editor and paint from `DecorationTerrain.tres`. The atlas has transparent 64×64 cells with **cyan on row 0** and **amber on row 1**. Source **1** is static and source **2** uses a slow sweep. Both use the same SVG and coordinates. Source **0**, cell `(5, 0)`, is the legacy decoration.

| Column | Shape / connections |
| --- | --- |
| 0, 1 | Horizontal, vertical |
| 2–5 | Corner up/right, right/down, down/left, left/up |
| 6–9 | T-junction opening up, right, down, left (third branch) |
| 10 | Four-way crossing |
| 11–14 | Foundation strip at top, right, bottom, left |
| 15 | Status indicator |

Paint over existing floor or wall artwork. For source 2, edit `IndustrialPulseCyan.tres` or `IndustrialPulseAmber.tres` to tune the tint, baseline, strength, frequency, coverage and spatial phase. Set **Enabled** off for the same static pixels in place. Settings are shared by the animated tiles of that colour. The TileData material uses the atlas alpha and a continuous world-space sweep, so it follows connected lines without shifting UVs.

The Godot 4.6.3 scratch check loaded and placed all 64 new tile choices plus the legacy tile and verified their material assignments. Desktop appearance and seam checks remain pending.
