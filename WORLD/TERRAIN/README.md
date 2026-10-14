# Terrain — Category-filtered authoring

Each level has Terrain/Passable, Terrain/Impassable and Terrain/Decorations TileMapLayers. Their respective TileSets are PassableTerrain.tres, ImpassableTerrain.tres and DecorationTerrain.tres under TileSets/. They share prototype_atlas.svg, but define only the matching selectable tile coordinates.

Passable contains open metal/dirt floors; Impassable contains walls, rocks and buildable raised platforms; Decorations contains cosmetic decals. Movement and tower permission are independent Boolean custom data (can_build, blocks_enemies), not inferred from art. Empty space is passable/unbuildable; decals never change these rules.

WorldGrid caches Passable then Impassable, which wins on accidental overlap. Erase the old gameplay cell before changing its category. Static rebuilds diagnose overlaps, category errors and invalid setup; ordinary tower placement retains incremental occupancy updates. No per-frame tile scans or physics bodies are added.

WorldTerrain.tres remains a legacy mixed palette. Current Level001/template use the separate palettes. See LEVELS/README.md and LEVELS/Level001/README.md for editor workflow and TESTS/TerrainMigration.md for preservation checks.
