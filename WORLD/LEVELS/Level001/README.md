# Level 001 — First Hand-Designed Battlefield

Starter editable scene: `Level001.tscn`. This is deliberately blank until you paint it in Godot once Codex creates a shared TileSet and connects level loading.

Suggested test composition:
- A complex metal defense base on one side, with a few corridors/choke points.
- Several isolated buildable platforms in surrounding open space.
- Both unbuildable solid walls and buildable solid platforms.
- Some **buildable, initially passable** cells to test placing a tower blocking navigation.
- Large untouched open-space sections revealing a nebula behind the map.
- A **Citadel** objective. For testing, choose either Portal scene instances, valid spawn-enabled map edges, or a combination. The current scene still contains marker-only spawn/goal placeholders.

Future Codex task: author the TileSet, install it on these scene layers, cache custom data into the logical grid, test Battle Tower placement restrictions and validate an open spawn-to-goal path. This scene is not currently loaded by the running game.

**Future objects:** `WORLD/PORTALS/EnemyPortal/EnemyPortal.tscn` and `WORLD/OBJECTIVES/Citadel/Citadel.tscn` are now reusable editor-placeable shells, but this map does **not** yet instance them. The current `GameplayMarkers` tree remains as-is to avoid breaking the scene before Codex implements the level loader. Configure Portal / Map Edge / Both on a future per-level settings Resource, not on the Portal object.
