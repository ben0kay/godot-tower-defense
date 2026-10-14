# LEVELS — Maps Painted in Godot

- `Level001/Level001.tscn`: the live terrain/placement test map, mounted as `World/Level`.
- `LevelTemplate.tscn`: empty paint-ready scene with a Citadel and shared TileSet; defaults to MapEdges and requires no Portal.
- `WorldLevel.cs`: typed scene root exposing terrain layers, level settings and destination.
- `LevelConfig.cs`: Inspector-editable Resource with Starting Resources / StartingCredits, `SpawnMode` (Portals / MapEdges / Both), `AllowedEdges`, root-relative `ActivePortals` NodePaths and `CitadelPath`.
- `DefaultLevelConfig.tres`: template defaults. Duplicate/make unique the settings resource for each new level.

Open a level in the 2D editor and paint Ground/Walls/Decorations. Save and press F5 to test the game. To choose another map, change the `Level` scene instance in `WORLD/World.tscn`, keeping the node name `Level`. Do not copy the camera/HUD/tower controller into individual maps.

`WorldMasterConfig` remains the single source for map dimensions and cell size: default 100×80, 64 px. Paint within those bounds. The shared TileSet is 64 px; its size must match the configured grid. Keep terrain layers aligned to the world grid without arbitrary scaling/rotation.

Physical Portals belong under `Portals`; the Citadel belongs under `Objectives`. Position object roots at cell centers. A Citadel root identifies the upper-left cell center of its footprint, rather than the overall footprint center. Place `SpawnExit` and `ApproachPoints` in unblocked cells. Paths in LevelConfig are relative to the level root, not Main.

Spawning and waves are future ENEMIES systems; level resources only declare permitted sources. Background sprites are independent of the grid. Empty space is unbuildable and passable. See `Level001/README.md` for controls and the atlas palette.

## Starting resources
Select the active level settings .tres and edit Starting Resources / Starting Credits. Level001Config.tres starts at 1,000; DefaultLevelConfig.tres starts at 0. Main/Economy loads this value into a fresh session wallet and pushes it to the HUD. Changing settings during play does not refill the wallet; restart the level to apply saved starting values. Credits are currently the only starting resource; optional tower material costs already use shared ResourceCost entries.
