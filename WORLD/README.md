# WORLD — Battlefield, Hand-Built Levels, Terrain and Space Scenery

This folder owns the playable **top-down 2D world** of the space tower-defense game. The long-term direction is **hand-authored maps** painted directly in the Godot 4 editor, with large areas of open space, detailed metal base corridors, disconnected wall/platform islands, dirt/rock formations and independent nebula/star backgrounds.

**Important status (October 2026):** The existing `World.tscn`, `GRID/WorldGrid.cs` and Battle Tower placement prototype still run as before. The folders and starter level scenes added in this pass are **authoring scaffolding only**. No actual 64×64 TileSet atlas, custom tile data, tilemap loading, navigation integration, or level switching is implemented. `Main.tscn` is deliberately unchanged.

## Folders and responsibilities

| Location | Responsibility |
| --- | --- |
| `World.tscn` | Current live world scene, holding the visual `Grid` and placed `Towers` container. |
| `WorldMasterConfig.cs` + `DefaultWorldConfig.tres` | Current Inspector-editable world/grid size (default 100 × 80 cells, 64 px each) and debug colors. |
| `GRID/` | Logical grid coordinates and the current lightweight, visible-line editor/gameplay overlay. |
| `TERRAIN/` | Reusable metal, dirt and decorative tiles; future Godot `TileSet` assets and tile gameplay rules. |
| `LEVELS/` | **Maps hand-painted by the designer**, including a reusable scene template and the starter `Level001` scene. |
| `BACKGROUNDS/` | Non-colliding space art: nebulae, stars and distant planets independent of TileMapLayers. |

Avoid naming this system `SOLIDS/`: some buildable tiles are passable until built on, while purely visual tiles have no collision at all.

## Tile behavior — separate gameplay from artwork

Every effective terrain cell should provide **two independent Boolean rules** (planned Godot TileSet custom-data names: `can_build` and `blocks_enemies`):

| Painted terrain | `can_build` | `blocks_enemies` | Game behavior |
| --- | --- | --- | --- |
| Metal, unbuildable wall | false | true | Hard obstacle; never accept a tower |
| Metal, buildable solid platform/wall | true | true | Tower can sit on it; enemy was blocked even before construction |
| Metal, buildable open platform | true | false | Enemies can travel there until tower occupancy blocks the cell |
| Pure background/decorative tile | false | false | Artwork only, no obstacle or tower permission |
| Dirt / rocky ground variants | per tile | per tile | Reuse the same two rules for any natural terrain |

**Empty/unpainted space** should default to **enemy-passable, not buildable**. A tower cannot be placed in plain empty space unless an explicitly buildable tile is painted there.

For cells where multiple terrain layers overlap, choose **one authoritative gameplay tile**: `Walls` should take precedence over `Ground`; `Decorations` should never override navigation/build permissions. Keep a single, cached logical grid that merges authored tile data and runtime tower occupancy, rather than querying TileMapLayer nodes once per enemy every frame.

Tower placement must check, in order: cell inside map, terrain `can_build`, currently unoccupied, sufficient funds when economy exists, and **portal-to-goal route validity** if construction would newly block an enemy-passable cell. A tower on a pre-blocked solid platform does not create additional navigation blockage. See `NOTES/NAVIGATION_PERFORMANCE_PLAN.md` for pathfinding and caching plans.

## Author levels by painting in Godot, not by writing coordinates

Use Godot 4's **TileMapLayer + TileSet** tools. Each level is its own editable scene:

```text
Level001 (Node2D)
├── SpaceBackground (Node2D)        # cosmetics, behind terrain
│   ├── Nebulae (Node2D)            # large anchored nebula Sprite2Ds
│   ├── Stars (Node2D)              # optional Parallax2D later
│   └── Planets (Node2D)            # decorative distant art
├── Terrain (Node2D)
│   ├── Ground (TileMapLayer)       # metal platforms, dirt, open buildable tiles
│   ├── Walls (TileMapLayer)        # solid blocking walls/platform edges
│   └── Decorations (TileMapLayer)  # scratches, lights, surface detailing only
└── GameplayMarkers (Node2D)
    ├── SpawnPoints (Node2D)        # Marker2D enemy-entry points
    └── Objectives (Node2D)        # Marker2D goals / defended core locations
```

For now `LEVELS/LevelTemplate.tscn` and `LEVELS/Level001/Level001.tscn` contain these **empty nodes**, not actual tiles. After Codex creates/assigns the 64px TileSet, open `Level001.tscn`, select `Ground` or `Walls`, and paint directly in the 2D editor. Leave some sections completely blank for open space. Place isolated base pieces, barriers and platforms wherever you want. Do not fill the entire map just to create a background.

## Nebulae and space backgrounds — NOT required to be tiles

Large nebula textures, stars and planets can be ordinary `Sprite2D` nodes, optional particle/shader visuals or `Parallax2D` layers under `SpaceBackground`. They do **not** consume grid cells, contribute to tile occupancy, block ships or accept buildings.

- **World-anchored nebulae** stay behind a chosen sector of the map as the camera pans.
- **Parallax stars** may move at a different rate to suggest distance.
- **Cosmetic lighting/dust** can be added later, without modifying terrain/gameplay data.
- A painted `Decorations` TileMapLayer is for repeated *grid-aligned surface detail*, not giant celestial scenery.

**Rendering integration warning:** the current `GRID/WorldGrid.cs` draws an opaque map rectangle via `DrawRect(map, Config.MapColor)`. Codex must adjust that when wiring the space background, otherwise the dark fill or node draw order may conceal nebula art. Keep the optional visible grid lines/cell diagnostics, but draw the nebula **behind** terrain and choose a transparent grid background or a dedicated space-colored background layer.

## Suggested Codex implementation order

1. Create a shared **64 × 64-pixel Godot TileSet atlas** in `TERRAIN/TileSets/`, using initial placeholder metal/ground tiles. Keep final artwork replaceable.
2. Add TileSet **custom data layers** `can_build: bool` and `blocks_enemies: bool` and configure the five tile behaviors above; document which layers supply gameplay rules.
3. Assign TileSet resources to `Ground`, `Walls` and `Decorations` in the starter level, then confirm the tiles can be painted using the **Godot editor**.
4. Mount the selected level scene inside the existing live world without breaking `Main.tscn`, camera controls, HUD, the world-size config or the Battle Tower prototype.
5. Populate one compact authoritative grid from authored tile properties. Empty cells should be open to enemies but not buildable. Respect Walls-over-Ground and ignore decorations for gameplay.
6. Update `TOWERS/Placement/TowerPlacementController.cs` to check `can_build`, bounds and occupancy, and later route connectivity for towers that block travel. Keep tower art and scene ownership in `TOWERS/`.
7. Allow large world-anchored nebula `Sprite2D` assets and (optionally) `Parallax2D` stars to show behind the painted tilemap. Preserve rendering order, mouse-to-cell snapping and camera bounds.
8. Smoke-test: an unbuildable solid wall rejects tower placement; a solid buildable platform allows it but blocks ships; an open buildable tile permits ships until occupied; empty space/decoration rejects building but allows ships. Verify at least one spawn-to-goal path remains after player building.

**Avoid premature complexity:** no custom editor/plugin, procedural tile/world generation, per-cell Godot nodes, physics bodies per tile or always-running path checks. Revisit more advanced tile auto-joining and enemy radius/clearance when the base game is functioning.

## Future master configurations

The existing `WorldMasterConfig.cs` is the current single source for grid size/dimensions. When the terrain, level loading and space-background systems are programmed, add their relevant `*MasterConfig.cs` resources within the relevant feature folder **only for values that genuinely need Inspector tuning**; do not duplicate cell-size constants or create unused resource classes just to fill folders.
