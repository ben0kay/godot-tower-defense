# TERRAIN — Shared Artwork and Tile Rules

`TileSets/WorldTerrain.tres` is the shared 64×64 TileSet assigned to each level's Ground, Walls and Decorations layers. `Tiles/Metal/prototype_atlas.svg` contains six temporary metal/natural/decal tiles; the mixed atlas can be replaced with final material-specific artwork later. Logical behavior comes from Boolean custom data `can_build` and `blocks_enemies`, never colors or atlas coordinates.

`WORLD/GRID/WorldGrid.cs` caches Ground, then Walls, on level load. Walls override Ground; Decorations are ignored. Empty cells default to unbuildable/passable. Runtime tower occupancy and object reservations are separate from painted terrain. `RebuildTerrain()` explicitly refreshes static rules after a future runtime terrain edit; ordinary editor edits take effect on the next run.

See `Rules/README.md` for semantics and `LEVELS/Level001/README.md` for the palette. No navigation engine or per-cell physics nodes are created by this terrain pass.
