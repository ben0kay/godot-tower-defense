## Mission XP foundation — 2026-10-17

LevelConfig now exposes Player XP Rewards: VictoryBaseXP and DefeatBaseXP, provisional500/0 on both playable levels. Session captures these at deployment; reward math/persistence live in SYSTEMS/Progression/Missions, not enemies/waves.

---

# LEVELS — Authored Maps, Settings and Waves

- `Level001/Level001.tscn`: the original terrain/placement test map, mounted as `World/Level` by `WORLD/World.tscn`.
- `Level002/Level002.tscn`: **The Five Gates**, a 100×80 map with five left-side portals, large western open ground and an eastern buildable-Foundation maze. See `Level002/README.md`; playable via Campaign → Play Level 002 or `MainLevel002.tscn`.
- `LevelTemplate.tscn`: empty paint-ready scene with a Citadel and shared TileSet; defaults to MapEdges and requires no Portal.
- `WorldLevel.cs`: typed scene root exposing terrain layers, level settings and destination.
- `LevelConfig.cs`: Inspector-editable Resource with Starting Resources / StartingCredits, `SpawnMode` (Portals / MapEdges / Both), `AllowedEdges`, root-relative `ActivePortals` NodePaths and `CitadelPath`.
- `DefaultLevelConfig.tres`: template defaults. Duplicate/make unique the settings resource for each new level.

Open a level in the 2D editor and paint Passable/Impassable/Decorations. Save and press F5 to test the game. For Level001/Level002 use their existing Campaign buttons or the corresponding `Main.tscn` / `MainLevel002.tscn` launch scenes. The latter uses `WORLD/WorldLevel002.tscn`, keeping the child node named `Level` and leaving the original World scene untouched. A shared loader may replace these launch variants later. Do not copy the camera/HUD/tower controller into individual maps.

`WorldMasterConfig` remains the single source for map dimensions and cell size: default 100×80, 64 px. Paint within those bounds. The shared TileSet is 64 px; its size must match the configured grid. Keep terrain layers aligned to the world grid without arbitrary scaling/rotation.

Physical Portals belong under `Portals`; the Citadel belongs under `Objectives`. Position object roots at cell centers. A Citadel root identifies the upper-left cell center of its footprint, rather than the overall footprint center. Place `SpawnExit` and `ApproachPoints` in unblocked cells. Paths in LevelConfig are relative to the level root, not Main.

Spawning lives in ENEMIES; the shared wave scheduler/catalog live in LEVELS/Waves. Level resources declare permitted sources and WaveSetId. Background sprites are independent of the grid. Empty space is unbuildable and passable. See `Level001/README.md` for controls and the atlas palette.

## Starting resources
Select the active level settings .tres and edit Starting Resources / Starting Credits. Level001Config.tres starts at 1,000; DefaultLevelConfig.tres starts at 0. Main/Economy loads this value into a fresh session wallet and pushes it to the HUD. Changing settings during play does not refill the wallet; restart the level to apply saved starting values. Credits are currently the only starting resource; optional tower material costs already use shared ResourceCost entries.

## Wave sets
LevelConfig / Waves / WaveSetId references a C# wave set in LEVELS/Waves/WaveCatalog.cs. Level001 uses `level001`; Level002 uses `level002`; the empty default disables waves. Wave timing/counts are edited in LEVELS/Level001/Level001Waves.cs, not large Inspector arrays. Unknown set IDs show a setup error; there is no implicit fallback. ActivePortals remains the authoritative allowed-source list.


## Folder ownership

Each level folder is its authoring home: Level001/ contains Level001.tscn, Level001Config.tres and Level001Waves.cs, including matching script .uid files. Future levels follow the same pattern and register their wave set in Waves/WaveCatalog.cs.

Waves/ holds the shared scheduler, immutable data/builder helpers and catalog; Waves/Authoring.md describes batch editing. Debug/ holds shared level debug helpers. WorldLevel.cs, LevelConfig.cs, default settings and LevelTemplate.tscn remain shared at this root.

WORLD/ still owns reusable terrain, grid/navigation, portals, objectives and scenery. The runtime map is still mounted at World/Level by WORLD/World.tscn; moving files does not change scene-tree paths, level-relative portal references or gameplay behavior. Existing Godot .uid values are preserved.

---

## Terrain palette cleanup — implemented

Select the matching TileMapLayer in the scene tree; each has its own category-filtered TileSet:

| Terrain layer | Assigned TileSet | Available atlas tiles | Movement / construction |
| --- | --- | --- | --- |
| Passable | WORLD/TERRAIN/TileSets/PassableTerrain.tres | (2,0) open metal floor; (4,0) open dirt | Passable; each tile independently allows/disallows towers. Both current floors are buildable. |
| Impassable | WORLD/TERRAIN/TileSets/ImpassableTerrain.tres | (0,0) metal wall; (3,0) rocky obstacle | Solid/nonbuildable. |
| Interactive | WORLD/TERRAIN/TileSets/InteractiveTerrain.tres | Source0 (1,0) raised Foundation; source1 (0,0) Lava | Foundation buildable/solid; Lava nonbuildable/passable. |
| Decorations | WORLD/TERRAIN/TileSets/DecorationTerrain.tres | (5,0) transparent decal | Visual-only; never changes movement or construction. |

The original category TileSets share prototype_atlas.svg; Interactive also references its separate Lava SVG. These resources preserve source 0, atlas coordinates, art, 64px cells and Boolean can_build / blocks_enemies metadata. Only category-matching tiles are defined in each atlas source, so they are the only selectable paint choices. The shared texture preview may still show surrounding artwork; undefined atlas regions are not paintable tiles. WorldTerrain.tres is retained as a legacy asset, but neither Level001 nor LevelTemplate uses the mixed palette.

### Painting rules

Passable/Impassable describe **movement**, not material or tower permission. A metal or dirt tile can be in either category according to its gameplay metadata. Buildability is independent: all four movement/build combinations remain supported. Lava now supplies a passable/nonbuildable Interactive overlay without changing ordinary floor defaults.

To turn a solid cell into floor, **erase its Impassable cell first**, then paint Passable. To turn floor into a wall, erase Passable and paint Impassable. Interactive-over-Passable is an intentional overlay; avoid unintended structural overlaps. If both exist, Impassable deterministically overrides Passable—including tower permission—and load/rebuild emits a warning.

Decorations may cover floor, obstacles or empty space without affecting gameplay. SpaceBackground remains separate. Empty/unpainted cells stay passable for ships and unbuildable for towers. Portals and Citadel still reserve construction cells; placed towers remain runtime obstacles on passable floors.

### Migration and diagnostics

Level001's prior Ground/Walls painting was recorded through Godot before conversion. It had 102 dirt-floor cells, 74 raised-platform cells, no decorations and no overlaps. All 176 effective tiles retain their exact coordinates, source/atlas/alternative IDs and gameplay values. No painted map, Portal/Citadel position, background, marker or level settings was reset. Layer unique IDs and existing resource UIDs are retained; the original category migration is recorded in TESTS/Terrain/TerrainMigration.md. The subsequent Interactive migration moves Foundation into its own layer with identical rules/art; both scenes now have four palettes.

WorldLevel exposes Passable, Impassable, Interactive and Decorations. WorldGrid caches Passable then Interactive then Impassable on load/explicit rebuild and never reads cosmetic tiles into gameplay caches. It reports overlapping gameplay cells, category mismatches and missing tiles, and rejects null/wrong-size gameplay TileSets or missing/non-Boolean metadata. Decorations requires the correct tile size but no gameplay metadata. These checks do not scan each frame.

SpawnManager reports missing registered exits, blocked/unreachable Portal SpawnExit cells and missing usable Citadel approaches when source caches refresh. Such warnings identify authored map problems; they do not move objects, repaint tiles or bypass navigation safety. Portal center placement must have sufficient clearance for the enemy radius and a route to an approach.

### Verification

TESTS/Terrain/TerrainCategoryChecks.cs covers category-filtered palettes, all four movement/build combinations, empty space, decals over empty/floor/solid cells, overlap precedence, tower occupancy and Portal/Citadel reservations. Existing F2, random waves, paid placement, combat and navigation fixtures use the new layer API and continue checking blocked routes safely. TESTS/Terrain/TerrainMigration.md records the map-preservation verification.

This was one cleanup pass; no procedural generation, art overhaul, new pathfinding algorithm or economy changes were introduced.

## Per-level Citadel starting health (implemented)

Each level's `LevelConfig` now provides **Destination / Citadel Max Health**. Set `CitadelMaxHealth` in `LEVELS/Level001/Level001Config.tres` (currently **1,000**) or in another LevelXXX config independently. `DefaultLevelConfig.tres` starts at 1,000. The `Main/CitadelSession` integration initializes the matching level `Objectives/Citadel/Health` at full health and uses enemy arrivals to subtract `EnemyDefinition.CitadelDamage`. `HealthChanged` updates the center HUD automatically; `Destroyed` emits once at zero. The defeat UI/level-loss consequences remain future work. There is **no separate per-level Citadel script or duplicate health config**; keep reusable logic under `WORLD/OBJECTIVES/Citadel/`.

## Interactive Terrain

Terrain/Interactive now exposes raised Foundation (source0 atlas1,0) and Lava (source1 atlas0,0). The74 saved Level001 platforms migrated without changing art/buildability/blocking; Template exposes the same palette. Lava is passable/nonbuildable and uses World/InteractiveTiles.LavaConfig for immediate entry plus periodic exposure damage. No Lava was arbitrarily painted into the current playable map. Use WorldGrid.RebuildTerrain after runtime edits; hazard-only changes avoid unnecessary navigation rebuilds. See WORLD/TERRAIN/Interactive/README.md and TESTS/Terrain/InteractiveTerrainMigration.md.
