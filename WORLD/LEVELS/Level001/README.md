# Level 001 — First Hand-Designed Battlefield

Starter editable scene: `Level001.tscn`. This is deliberately blank until you paint it in Godot once Codex creates a shared TileSet and connects level loading.

Suggested test composition:
- A complex metal defense base on one side, with a few corridors/choke points.
- Several isolated buildable platforms in surrounding open space.
- Both unbuildable solid walls and buildable solid platforms.
- Some **buildable, initially passable** cells to test placing a tower blocking navigation.
- Large untouched open-space sections revealing a nebula behind the map.
- One obvious enemy spawn and one goal (currently just placeholder marker nodes).

Future Codex task: author the TileSet, install it on these scene layers, cache custom data into the logical grid, test Battle Tower placement restrictions and validate an open spawn-to-goal path. This scene is not currently loaded by the running game.
