# Level 001 — Terrain and Tower Placement Playtest

`Level001.tscn` is loaded by `WORLD/World.tscn` when you run the game (F5). It is a saved, hand-editable TileMapLayer layout, not generated during gameplay. Placeholder artwork is deliberately replaceable.

## Test and adjust

1. Pull the latest commit, let Godot import assets, and build the C# project.
2. Run **F5** to play the main game. The camera starts over the central base. WASD/arrows pan, middle mouse drags, wheel zooms; Ctrl toggles the tower tray.
3. Choose Battle Tower and try the different terrain tiles. Green previews allow building; red previews reject it. Empty space and unbuildable walls reject towers. Buildable open floors and solid platforms accept them. The debug overlay reports current cell rules and occupancy.
4. Stop the game, open **Level001.tscn**, select `Terrain/Ground` or `Terrain/Walls`, and paint using the shared TileSet palette. Save, then press F5 again. F6 runs only the map scene, without the gameplay camera/HUD/placement controller.
5. Select `Terrain/Decorations` for cosmetic tiles only. Erase ground tiles to expose open space. Walls override Ground; Decorations never change cell rules.
6. Move Portal/Citadel instances with grid snapping at 64 px. Object root positions sit at cell centers (32 + 64*n). A Citadel root is the upper-left cell center of its 3×3 footprint.
7. Select the root and edit its `Settings` resource for spawn mode, allowed edges, active Portal paths and Citadel reference. These describe future spawning; they do not create enemies yet.

The main base occupies roughly cells (34,31)–(65,49), with disconnected islands and natural terrain nearby. Portal: (36,39), exit: (37,39). Citadel footprint: (60,39)–(62,41); approach markers lie outside the solid footprint. Keep exits/approaches reachable when repainting.

## Shared atlas palette, left to right

| Atlas coordinate | Tile | Buildable | Blocks enemies |
| --- | --- | --- | --- |
| (0,0) | Dark metal wall, amber marks | No | Yes |
| (1,0) | Raised metal platform, cyan marks | Yes | Yes |
| (2,0) | Open metal floor, cyan dashed square | Yes | No |
| (3,0) | Rocky obstacle | No | Yes |
| (4,0) | Open dirt ground | Yes | No |
| (5,0) | Transparent surface decal | No | No |

Paint decals on Decorations. A decal alone never grants build permission. The atlas is shared by all three layers and other levels; changing tile metadata affects every level using it.

Portal cells are reserved against towers but passable; Citadel cells are reserved and blocked. No enemy spawning, pathfinding, route-preservation checks, firing, economy, Citadel health or win/lose loop is implemented in this pass.
