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

`WORLD/GRID/WorldGrid.cs` reads TileMapLayer cell data **on scene load or explicit `RebuildTerrain()` calls**, caches results in Boolean arrays indexed by grid coordinates, and exposes shared build/block/occupancy queries. Do not scan TileMapLayer/scene-tree state every frame or for each enemy. Route connectivity must be added with enemy navigation before allowing gameplay blockers in a combat level.

`TowerPlacementController.cs` now enforces these terrain/bounds/occupancy rules. Object footprints reserve Portal/Citadel cells. Navigation revision changes when a tower newly blocks an open cell; no actual pathfinding or route checking exists yet. Tiles on Walls replace Ground rules even when their Boolean values are false.
