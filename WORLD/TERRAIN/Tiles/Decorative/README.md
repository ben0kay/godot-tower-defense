# Decorative Tile Overlays

Grid-aligned scratches, panel numbers, caution stripes, bolts, lights, grime and other art overlays belong on the level's `Decorations` TileMapLayer. They should **never change** tower placement or ship navigation.

Giant nebulae, background planets and moving starfields are better as independent scene sprites/parallax under `WORLD/BACKGROUNDS/`.

## VE3 — paintable industrial accents (planned, not implemented)

The approved [VE3 Codex handoff](../../../BACKGROUNDS/README.md#ve3--industrial-metal-and-corridor-details-codex-handoff-planned) will extend **the existing** `WORLD/TERRAIN/TileSets/DecorationTerrain.tres` with transparent 64×64 cyan/amber conduit, junction, foundation-edge and indicator tiles. These are placed by hand on `Terrain/Decorations`, above Passable/Impassable artwork; no gameplay tiles or map painting change automatically. Animation, where used, must be isolated to opted-in accent artwork, never the entire Decorations TileMap. Current source `0`, coordinate `5:0/0` must be preserved. Assets/shader not implemented yet.

