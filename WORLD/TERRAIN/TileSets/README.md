# TileSets

`WorldTerrain.tres` is the paint-ready shared 64 px atlas. Source 0 uses `../Tiles/Metal/prototype_atlas.svg` with six tiles. Boolean custom-data layers are `can_build` and `blocks_enemies`.

All levels can reuse this resource. Paint Ground/Walls for gameplay and Decorations for cosmetic detail. Changing a tile's metadata affects all maps that use it. The palette and initial behavior table are documented in `WORLD/LEVELS/Level001/README.md`. No physics/navigation polygons are needed: the logical grid caches tile data for future navigation.
