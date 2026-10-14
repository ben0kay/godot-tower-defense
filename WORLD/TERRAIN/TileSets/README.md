# Terrain TileSets

Select a scene layer to get its own palette, without manually switching resources:

| Resource | Layer | Source 0 atlas coordinates |
| --- | --- | --- |
| PassableTerrain.tres | Terrain/Passable | (2,0), (4,0) |
| ImpassableTerrain.tres | Terrain/Impassable | (0,0), (1,0), (3,0) |
| DecorationTerrain.tres | Terrain/Decorations | (5,0) |

All use 64px regions of the same prototype_atlas.svg. Custom data is Boolean can_build and blocks_enemies; keep both movement/build combinations independent. Gameplay layers are read into WorldGrid; Decorations is always ignored. Only tiles defined in each atlas source can be selected/painted, although Godot may show the full shared texture in the atlas preview.

WorldTerrain.tres is retained as the original legacy atlas; the current level/template have no references to it. Add future art and rules to the correct category resource. Changing a shared TileSet affects every level using it; duplicate a resource for a level-specific variant when needed.
