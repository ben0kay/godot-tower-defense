# LEVELS — Maps Designed Manually in the Godot Editor

Unlike `TERRAIN/` (shared source tiles), this folder contains each **hand-authored battlefield layout**.

- `LevelTemplate.tscn`: empty reference scene with the intended level node structure.
- `Level001/Level001.tscn`: separate, editable first level scene with the same foundation.
- `Level001/README.md`: notes about this map and its planned setup.

## Painting your map

1. Ask Codex to create a **64×64 TileSet** in `WORLD/TERRAIN/TileSets/` and assign it to the `Ground`, `Walls` and `Decorations` layers.
2. In Godot, open `Level001/Level001.tscn` (**not** the global `World.tscn`) in the 2D editor.
3. Select the `Ground` TileMapLayer, choose a tile from the TileSet palette, and paint your base, floors, small disconnected platforms or dirt.
4. Select `Walls` and paint hard barriers or buildable solid positions; leave gaps for corridors and ship travel.
5. Select `Decorations` for grime/markings/lighting that do not affect gameplay.
6. Add individual sprites such as nebulae under `SpaceBackground/Nebulae`, or reusable cosmetic scenes from `WORLD/BACKGROUNDS/`.
7. Position `GameplayMarkers/SpawnPoints` and `Objectives` for future spawners/goals.
8. Save the scene. Codex will wire the level scene into the existing `World.tscn` runtime afterward.

You can leave huge areas of the tilemap *unpainted*. They reveal the independent space background and default to enemy-passable, unbuildable space.

The provided scenes **do not yet have a TileSet resource assigned**, and `Main.tscn` still uses the old debug world. These files are editor organization scaffolding, not playable levels. Avoid duplicating the whole terrain/grid logic across each level.
