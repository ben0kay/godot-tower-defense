# Raised Foundation

Paint in Terrain/Interactive, source0 atlas(1,0). The existing Metal/prototype_atlas.svg tile is shared rather than duplicated. Foundation remains can_build=true and blocks_enemies=true: a tower can be placed here under existing occupancy, reservations, connectivity and payment checks, while enemies cannot traverse the platform even before construction.

All74 Level001 platforms migrated explicitly; see TESTS/InteractiveTerrainMigration.md. Foundation has no entry effect, timer or per-cell processing. A different future walkable foundation would require its own authored tile; it is not silently substituted here.
