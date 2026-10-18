# Terrain palettes

Each level's matching TileMapLayer selects a category resource:

- PassableTerrain.tres: Metal atlas(2,0)/(4,0), open floors.
- ImpassableTerrain.tres: Metal atlas(0,0)/(3,0), structural wall/rock.
- InteractiveTerrain.tres: source0 Metal atlas(1,0) raised Foundation; source1 atlas(0,0) molten Lava SVG.
- DecorationTerrain.tres: cosmetic Metal atlas(5,0).

All use64px tile regions; WorldMasterConfig.CellSize must match. Gameplay palettes expose Boolean can_build and blocks_enemies. Interactive also exposes String interaction (foundation/lava), independent of art. Lava overrides buildable floors but cannot override structural blockers or object reservations. Foundation is buildable/solid; Lava is nonbuildable/walkable.

Level001's74 Foundation records were moved without changing coordinates, source/atlas IDs, art or effective rules before the old Foundation entry was removed from the current Impassable palette. Template exposes the same new layer. WorldTerrain.tres remains an unchanged legacy mixed palette; migrate other old maps' Impassable(1,0) cells explicitly before switching them to these current palettes. See TESTS/Terrain/InteractiveTerrainMigration.md and ../Interactive/README.md.

## New industrial tiles (additive, 2026-10-18)

Both **PassableTerrain.tres** and **ImpassableTerrain.tres** preserve their original `source 0` prototype atlas and every existing painted cell. New `source 1` references the same optimized 256×64 WebP atlas, `Tiles/Metal/new_metal_terrain_atlas.webp`, composed left-to-right from the four supplied images. Each atlas cell is exactly 64×64 (matching `WorldMasterConfig.CellSize`). No level scene or legacy `WorldTerrain.tres` was changed.

| Tile | TileMapLayer / palette | Source ID | Atlas cell | can_build | blocks_enemies |
| --- | --- | ---: | --- | --- | --- |
| Plain bolted wall | Impassable | 1 | (0,0) | true | true |
| Cracked wall | Impassable | 1 | (1,0) | false | true |
| Decorative metal walkway | Passable | 1 | (2,0) | false | false |
| Cyan walkable foundation | Passable | 1 | (3,0) | true | false |

Paint a wall using the level's **Terrain/Impassable** layer, and a walkable floor using **Terrain/Passable**. The newly named walkable foundation is **not** the original raised, blocked Foundation in `InteractiveTerrain.tres` (source 0, atlas 1,0); both retain their independent rules. Empty walkable foundation becomes blocked after tower occupancy, subject to the existing route-safety check. The decorative metal floor is gameplay terrain, not a neutral overlay in `Terrain/Decorations`.

The provided artwork was resized from 256×256 source images for the current 64-pixel grid, using a high-quality downsample and a compact WebP atlas. Only the gameplay-resolution atlas is committed; the 256px originals remain the separate source attachments. No atlas IDs or custom-data definitions were renumbered.
