# Terrain — Category-filtered authoring

Each level has Terrain/Passable, Terrain/Impassable and Terrain/Decorations TileMapLayers. Their respective TileSets are PassableTerrain.tres, ImpassableTerrain.tres and DecorationTerrain.tres under TileSets/. They share prototype_atlas.svg, but define only the matching selectable tile coordinates.

Passable contains open metal/dirt floors; Impassable contains walls, rocks and buildable raised platforms; Decorations contains cosmetic decals. Movement and tower permission are independent Boolean custom data (can_build, blocks_enemies), not inferred from art. Empty space is passable/unbuildable; decals never change these rules.

WorldGrid caches Passable then Impassable, which wins on accidental overlap. Erase the old gameplay cell before changing its category. Static rebuilds diagnose overlaps, category errors and invalid setup; ordinary tower placement retains incremental occupancy updates. No per-frame tile scans or physics bodies are added.

WorldTerrain.tres remains a legacy mixed palette. Current Level001/template use the separate palettes. See LEVELS/README.md and LEVELS/Level001/README.md for editor workflow and TESTS/TerrainMigration.md for preservation checks.

## Planned next pass — Interactive Terrain (Foundation + Lava)

**Documentation only, NOT implemented.** After the separate tower-targeting/area-attack spatial optimization pass finishes, implement the plan in [`Interactive/README.md`](Interactive/README.md). This adds a dedicated editor paint category for gameplay-aware tiles while preserving existing Passable, Impassable and Decorations rules, navigation and tower placement.

- **Foundation:** buildable terrain; preserve the existing raised foundation/platform tile's `can_build=true` and `blocks_enemies=true` semantics when moving its editor category. Existing Level001/template painted terrain must be migrated compatibly, not silently remapped or removed.
- **Lava:** passable, **nonbuildable**; **immediate damage when an enemy enters Lava, followed by configurable periodic damage while it remains on Lava**, including when stationary. Adjacent Lava cells are **one continuous hazard** (no repeated entry hits/timer resets), and exit/re-entry resets exposure. Swept movement catches fast crossings. `LavaTileConfig` owns entry toggle, damage amount, damage interval and existing damage type; no per-tile physics/timers.
- **Art direction:** Lava is **predominantly vivid molten orange**, with only tiny sparse molten flecks, highlights and very little dark crust; legible, contiguous 64x64 tiles, not a mostly black cracked-rock texture.

Use one lightweight `InteractiveTileManager`, a new `Terrain/Interactive` TileMapLayer and editor TileSet, and Inspector-editable Lava entry/periodic damage settings. The manager must not scan all painted tiles or every enemy per tick. The full migration, test requirements and Codex-ready one-pass prompt are in `Interactive/README.md`. Do not confuse these notes with a completed gameplay feature.
