# Level002 — The Five Gates

A **100 × 80** painted-grid battlefield at **64 px/cell = 6,400 × 5,120 px**, deliberately larger than a 1920×1080 viewport. Normal camera pan/zoom applies. This is a genuine editable `TileMapLayer` map, not a runtime procedural generator.

## Layout

- **Western open staging ground (x=1..43):** predominantly buildable dirt/open-metal terrain, with five spatially separated entry portals at cells **(8,10), (13,24), (7,38), (16,53), (9,69)**. They are active via `Level002Config.tres`; random wave spawning distributes ships among usable entrances.
- **Eastern defensive labyrinth (x≈44..98):** maze walls are *raised Foundation* tiles: **buildable but enemy-impassable**. Walkable corridors use separate Passable-ground tiles. These two categories intentionally do not overlap. The outside edge is a non-buildable hard wall so enemies cannot shortcut around the maze.
- **Routes:** wide **three-cell** horizontal/vertical avenues intermingle with **single-cell** passages, gaps, bends, mid-maze connectors and pockets. Several east–west approaches and north–south cross-connections create loops, not a single forced route. With no player towers, all five portals connect to all four Citadel approach markers through the western field/maze network.
- **Citadel:** eastern courtyard; upper-left footprint root at grid **(90,39)** (a 3×3 reserved/blocked footprint), surrounded by walkable cells, including all four shared approach positions.
- **Art:** existing shared sci-fi terrain tiles and star background; ground and raised Foundation tiles may be repainted in Godot without a generator. Stage-specific custom background art can be added later.

## Gameplay, waves and editing

- `Level002Config.tres`: **1,700 starting Credits**, **1,200 Citadel HP**, five active portals, `WaveSetId="level002"`. Change this resource to rebalance.
- `Level002Waves.cs`: four mixed test waves using `basic_mk1`, `scout_mk1`, `medium_mk1` and `shield_emitter_mk1`. All five portals remain eligible for unpinned wave spawns. These are editable test encounters, not a campaign progression/unlock/save system.
- `Level002.tscn`: edit **Terrain/Passable**, **Terrain/Impassable**, **Terrain/Interactive** (Foundation walls), **Terrain/Decorations**, **Portals** and **Objectives/Citadel** in the 2D editor.
- `res://MainLevel002.tscn` and `WORLD/WorldLevel002.tscn` are deliberately small standalone launch variants for this prototype; Level001's `Main.tscn` and `WORLD/World.tscn` are unmodified. Campaign → **PLAY LEVEL 002** launches the new battlefield. `F6` on `MainLevel002.tscn` bypasses menu/profile. A shared level-loader can later replace these temporary launch variants when many levels exist.
- F2 continues to cycle the same global enemy catalog; normal waves spawn via the wave's stable enemy IDs.

## Navigation / authoring safeguards

- Avoid placing gameplay obstacles across every connection to the Citadel. Normal tower-placement connectivity validation prevents the player from fully sealing active routes.
- The route surface is wide enough for currently supported ship clearance radii (including Enemy004's **22px** medium hull). **Future giant ships larger than half a tile radius require a separate large-unit navigation design**.
- `WorldGrid` treats empty/unpainted space as passable but unbuildable; the deliberately painted border prevents off-maze shortcuts. `Foundation` is buildable but blocks enemies, and does **not** trigger lava damage.
- The map is drawn using normal Godot TileMapLayer serialized cells. No runtime tile scans beyond the game's existing startup cache; no per-tile nodes or new pathfinding systems.
