# LEVELS — Authored Maps, Settings and Waves

- `Level001/Level001.tscn`: the live terrain/placement test map, mounted as `World/Level`.
- `LevelTemplate.tscn`: empty paint-ready scene with a Citadel and shared TileSet; defaults to MapEdges and requires no Portal.
- `WorldLevel.cs`: typed scene root exposing terrain layers, level settings and destination.
- `LevelConfig.cs`: Inspector-editable Resource with Starting Resources / StartingCredits, `SpawnMode` (Portals / MapEdges / Both), `AllowedEdges`, root-relative `ActivePortals` NodePaths and `CitadelPath`.
- `DefaultLevelConfig.tres`: template defaults. Duplicate/make unique the settings resource for each new level.

Open a level in the 2D editor and paint Ground/Walls/Decorations. Save and press F5 to test the game. To choose another map, change the `Level` scene instance in `WORLD/World.tscn`, keeping the node name `Level`. Do not copy the camera/HUD/tower controller into individual maps.

`WorldMasterConfig` remains the single source for map dimensions and cell size: default 100×80, 64 px. Paint within those bounds. The shared TileSet is 64 px; its size must match the configured grid. Keep terrain layers aligned to the world grid without arbitrary scaling/rotation.

Physical Portals belong under `Portals`; the Citadel belongs under `Objectives`. Position object roots at cell centers. A Citadel root identifies the upper-left cell center of its footprint, rather than the overall footprint center. Place `SpawnExit` and `ApproachPoints` in unblocked cells. Paths in LevelConfig are relative to the level root, not Main.

Spawning lives in ENEMIES; the shared wave scheduler/catalog live in LEVELS/Waves. Level resources declare permitted sources and WaveSetId. Background sprites are independent of the grid. Empty space is unbuildable and passable. See `Level001/README.md` for controls and the atlas palette.

## Starting resources
Select the active level settings .tres and edit Starting Resources / Starting Credits. Level001Config.tres starts at 1,000; DefaultLevelConfig.tres starts at 0. Main/Economy loads this value into a fresh session wallet and pushes it to the HUD. Changing settings during play does not refill the wallet; restart the level to apply saved starting values. Credits are currently the only starting resource; optional tower material costs already use shared ResourceCost entries.

## Wave sets
LevelConfig / Waves / WaveSetId references a C# wave set in LEVELS/Waves/WaveCatalog.cs. Level001 uses level001; the empty default disables waves. Wave timing/counts are edited in LEVELS/Level001/Level001Waves.cs, not large Inspector arrays. Unknown set IDs show a setup error; there is no implicit fallback. ActivePortals remains the authoritative allowed-source list.


## Folder ownership

Each level folder is its authoring home: Level001/ contains Level001.tscn, Level001Config.tres and Level001Waves.cs, including matching script .uid files. Future levels follow the same pattern and register their wave set in Waves/WaveCatalog.cs.

Waves/ holds the shared scheduler, immutable data/builder helpers and catalog; Waves/Authoring.md describes batch editing. Debug/ holds shared level debug helpers. WorldLevel.cs, LevelConfig.cs, default settings and LevelTemplate.tscn remain shared at this root.

WORLD/ still owns reusable terrain, grid/navigation, portals, objectives and scenery. The runtime map is still mounted at World/Level by WORLD/World.tscn; moving files does not change scene-tree paths, level-relative portal references or gameplay behavior. Existing Godot .uid values are preserved.
