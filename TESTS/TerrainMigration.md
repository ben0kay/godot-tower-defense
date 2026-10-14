# Category palette migration verification

Baseline: c2fc537, LEVELS/Level001/Level001.tscn.

A headless Godot scene snapshot recorded every painted coordinate, source ID, atlas coordinate, alternative tile, can_build and blocks_enemies before conversion. Old Ground had 102 dirt-floor cells (4,0); old Walls had 74 raised-platform cells (1,0); Decorations had zero cells. There were no gameplay overlaps, transparent gameplay tiles or ambiguous layer assignments.

The conversion decoded/repartitioned serialized cell records and preserved their IDs. A second Godot snapshot of Passable/Impassable was compared to the complete former Ground-then-Walls effective tile dictionary: all 176 entries and their metadata matched exactly. Decoration lists also matched. Source/atlas coordinates reference the same unmodified SVG artwork. Scene diffs preserve node unique IDs, all backgrounds, Portal/Citadel/marker positions and level settings; only category layer names, palette references and associated cell partitioning changed.

Legacy WorldTerrain.tres is retained. The new TileSets define only category-appropriate coordinates and both Level001/template reference them.

TerrainCategoryChecks tests palette membership and independent gameplay rules with temporary in-memory cells/resources, restoring authored cells and TileSets afterward. Existing integration fixtures now use Passable/Impassable and preserve route, wave, placement, economy and combat checks. This document records a one-time migration comparison; it does not freeze future designer edits to this map.
