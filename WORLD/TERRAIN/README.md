# TERRAIN — Reusable Tile Artwork and Gameplay Metadata

Create and maintain reusable terrain tiles here; do **not** store the hand-painted layout of individual levels here. Level scenes live in `WORLD/LEVELS/`.

## Organization

- `TileSets/`: future shared Godot `TileSet` (`.tres`) resources and atlas definitions used by `TileMapLayer` nodes.
- `Tiles/Metal/`: futuristic 64px metal floor/platform/wall artwork and variants.
- `Tiles/Dirt/`: dusty, rocky and other natural ground artwork.
- `Tiles/Decorative/`: non-gameplay-altering decals, scratches, grime, small lights and surface details.
- `Rules/`: mapping between painted tiles and the runtime logical build/navigation grid.

The visual material (**metal**, **dirt**) is separate from the two intended gameplay attributes:
`can_build` and `blocks_enemies`. A visually identical metal tile may be either a hard wall, a buildable hard platform or a buildable passable surface. Use TileSet custom data rather than inferring behavior from texture colors, scene names or atlas cell coordinates.

The current `WORLD/GRID/WorldGrid.cs` debug grid is **not** a TileMapLayer or a terrain-rule source. Codex will implement tile resources, caching and integration later. See `WORLD/README.md` for the agreed tile matrix and implementation checklist.
