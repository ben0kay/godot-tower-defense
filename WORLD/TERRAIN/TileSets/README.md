# Terrain palettes

Each level's matching TileMapLayer selects a category resource:

- PassableTerrain.tres: Metal atlas(2,0)/(4,0), open floors.
- ImpassableTerrain.tres: Metal atlas(0,0)/(3,0), structural wall/rock.
- InteractiveTerrain.tres: source0 Metal atlas(1,0) raised Foundation; source1 atlas(0,0) molten Lava SVG.
- DecorationTerrain.tres: cosmetic Metal atlas(5,0).

All use64px tile regions; WorldMasterConfig.CellSize must match. Gameplay palettes expose Boolean can_build and blocks_enemies. Interactive also exposes String interaction (foundation/lava), independent of art. Lava overrides buildable floors but cannot override structural blockers or object reservations. Foundation is buildable/solid; Lava is nonbuildable/walkable.

Level001's74 Foundation records were moved without changing coordinates, source/atlas IDs, art or effective rules before the old Foundation entry was removed from the current Impassable palette. Template exposes the same new layer. WorldTerrain.tres remains an unchanged legacy mixed palette; migrate other old maps' Impassable(1,0) cells explicitly before switching them to these current palettes. See TESTS/Terrain/InteractiveTerrainMigration.md and ../Interactive/README.md.
