# Terrain Rules — Authoring Contract for Codex

Use Godot `TileSet` custom-data **Boolean layers** named exactly:
- `can_build`: player may place a tower on this terrain tile (subject to occupancy, economy and future route checks).
- `blocks_enemies`: ships must not traverse this cell.

| Effective terrain | can_build | blocks_enemies |
| --- | --- | --- |
| Unbuildable solid wall | false | true |
| Buildable solid platform | true | true |
| Buildable open surface | true | false |
| Purely decorative tile | false | false |
| Empty/unpainted cell | false | false (passable) |
| Dirt/other terrain | per tile | per tile |

**Layer precedence:** `Walls` overrides `Ground` for gameplay rules if both cover a cell. `Decorations` never contributes gameplay rules. The visual background is entirely nonphysical. Avoid ambiguous authored overlaps in the first pass.

A tower being placed on a passable buildable tile sets a **runtime navigation blocker** until removed; a tower on an already-solid tile doesn't add new blockage. Dynamic occupancy is not equivalent to a painted wall.

Future C# implementation should read TileMapLayer cell data **on scene load or when tiles change**, cache results into an array indexed by grid coordinates, and let placement/pathing query that array. Do not scan TileMapLayer/scene-tree state every frame or for each enemy. Before allowing new blockers, check that required spawn-to-goal routes remain connected.

The folder contains documentation only. The current `TowerPlacementController.cs` still checks only in-bounds/unoccupied cells; it does not enforce these rules until Codex integrates them.
