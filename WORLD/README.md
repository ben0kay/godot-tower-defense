# WORLD — Shared Terrain, Navigation, Portals, Citadel and Space Scenery

This folder owns the playable **top-down 2D world** of the space tower-defense game. The long-term direction is **hand-authored maps** painted directly in the Godot 4 editor, with large areas of open space, detailed metal base corridors, disconnected wall/platform islands, dirt/rock formations and independent nebula/star backgrounds.

**Current status (October 2026):** Level001 is now mounted in the live world. Shared 64×64 TileSet, saved editable terrain, cached tile rules, tower build restrictions, space backdrop and visible Portal/Citadel placeholders are implemented. LevelConfig declares portal/edge/both source permissions. Enemy001 spawning, budgeted A* navigation, route-preserving build checks and Battle Tower firing are now active. Waves, economy and Citadel health remain future work. See `LEVELS/Level001/README.md` for exact testing and painting steps.

## Folders and responsibilities

| Location | Responsibility |
| --- | --- |
| `World.tscn` | Live world scene, mounting `Level001` as `Level`, the cached `Grid`, and placed `Towers` container. |
| `WorldMasterConfig.cs` + `DefaultWorldConfig.tres` | Current Inspector-editable world/grid size (default 100 × 80 cells, 64 px each) and debug colors. |
| `GRID/` | Cached terrain rules/object reservations/tower occupancy, grid coordinates and transparent debug overlay. |
| `TERRAIN/` | Shared 64px TileSet, replaceable prototype atlas and tile gameplay rules. |
| Top-level `LEVELS/` | **Authored maps and per-level settings/waves**, stored outside WORLD; World.tscn mounts the selected map at World/Level. |
| `BACKGROUNDS/` | Non-colliding space art: nebulae, stars and distant planets independent of TileMapLayers. |
| `PORTALS/` | **Physical, reusable Portal scene**, manually placed on maps where enemies emerge through portals. No spawning logic here. |
| `OBJECTIVES/Citadel/` | **Physical Citadel scene**, the defended destination with reserved solid 3×3 footprint and approach markers; health remains future work. |

Avoid naming this system `SOLIDS/`: some buildable tiles are passable until built on, while purely visual tiles have no collision at all.

## Tile behavior — separate gameplay from artwork

Every effective terrain cell should provide **two independent Boolean rules** (Godot TileSet custom-data names: `can_build` and `blocks_enemies`):

| Painted terrain | `can_build` | `blocks_enemies` | Game behavior |
| --- | --- | --- | --- |
| Metal, unbuildable wall | false | true | Hard obstacle; never accept a tower |
| Metal, buildable solid platform/wall | true | true | Tower can sit on it; enemy was blocked even before construction |
| Metal, buildable open platform | true | false | Enemies can travel there until tower occupancy blocks the cell |
| Pure background/decorative tile | false | false | Artwork only, no obstacle or tower permission |
| Dirt / rocky ground variants | per tile | per tile | Reuse the same two rules for any natural terrain |

**Empty/unpainted space** should default to **enemy-passable, not buildable**. A tower cannot be placed in plain empty space unless an explicitly buildable tile is painted there.

For cells where multiple terrain layers overlap, choose **one authoritative gameplay tile**: `Impassable` takes precedence over `Passable`; `Decorations` should never override navigation/build permissions. Keep a single, cached logical grid that merges authored tile data and runtime tower occupancy, rather than querying TileMapLayer nodes once per enemy every frame.

With enemy navigation/economy implemented later, tower placement must check, in order: cell inside map, terrain `can_build`, currently unoccupied, sufficient funds when economy exists, and **active spawn-source-to-Citadel route validity** (Portal or map-edge sources) if construction would newly block an enemy-passable cell. Current placement enforces bounds, terrain permission, object reservations and occupancy; route preservation is checked on build clicks; funds remain future work. A tower on a pre-blocked solid platform does not create additional navigation blockage. See `NOTES/NAVIGATION_PERFORMANCE_PLAN.md` for pathfinding and caching plans.

## Enemy entry and destination — world objects vs enemy mechanics

Enemy spawning can come from **Portals**, **map edges**, or **both**, chosen **per level** (with potential wave-level source overrides later).

- `WORLD/PORTALS/EnemyPortal/EnemyPortal.tscn`: a physical scene that you can place in the editor; its `SpawnExit` marker identifies an entry point. **It does not create enemies or run wave timers**.
- **Map-edge spawning:** no physical Portal or tile needed; the SpawnManager computes reachable, valid entry positions on whichever sides the level enables (Top / Bottom / Left / Right).
- `WORLD/OBJECTIVES/Citadel/Citadel.tscn`: the destination object. Enemies should navigate toward **reachable approach locations**, not blindly into the Citadel's future solid footprint.
- `ENEMIES/Spawning/`: **SpawnManager logic** that chooses where to spawn from level-approved sources.
- `LEVELS/Waves/`: **WaveManager logic** that controls what/when/how many ships to spawn.
- `ENEMIES/Movement/`: waypoint following using shared WORLD/NAVIGATION services.
- `LEVELS/`: per-level authored Portal/Citadel positions and future **level spawn parameters** (`Portals`, `MapEdges`, `Both`, allowed edge directions and optionally active Portal references).

`Level001.tscn` now instances reusable EnemyPortal/Citadel objects under Portals/Objectives; legacy GameplayMarkers have been removed. Physical objects use `WorldObjectFootprint.cs` to reserve cells. Edge-only maps need no Portal instance. The empty LevelTemplate defaults to edge sources; Level001 defaults to its single Portal. All NodePaths in LevelConfig are relative to the level root.

## Author levels by painting in Godot, not by writing coordinates

Use Godot 4's **TileMapLayer + TileSet** tools. Each level is its own editable scene, mixing detailed constructed regions with empty open-space areas:

```text
Level001 (Node2D)
├── SpaceBackground (Node2D)       # cosmetics, behind terrain
│   ├── Nebulae (Node2D)           # large non-tilemap nebula sprites
│   ├── Stars (Node2D)             # optional Parallax2D later
│   └── Planets (Node2D)
├── Terrain (Node2D)
│   ├── Passable (TileMapLayer)      # metal / dirt / buildable passable tiles
│   ├── Impassable (TileMapLayer)       # solid walls / buildable solid platforms
│   └── Decorations (TileMapLayer) # only surface art
├── Portals (Node2D)               # zero or more EnemyPortal scene instances
└── Objectives (Node2D)            # Citadel scene instance(s)
```

This is now the actual Level001 arrangement. Passable, Impassable and Decorations use separate category TileSets from `WORLD/TERRAIN/TileSets/`, sharing one atlas texture. Select a layer in Godot's 2D editor and paint the map; save and press F5 to test with camera/HUD/placement. F6 runs only the level scene. Empty unpainted regions reveal space scenery.

`LEVELS/LevelConfig.cs` is the single Inspector-editable per-level spawn Resource: SpawnMode, AllowedEdges, ActivePortals and CitadelPath. These are consumed by ENEMIES/Spawning/SpawnManager.cs. Keep each new level's resource separate; keep WorldMasterConfig as the single source for grid/map dimensions.

## Nebulae and space backgrounds — NOT required to be tiles

Large nebula textures, stars and planets can be ordinary `Sprite2D` nodes, optional particle/shader visuals or `Parallax2D` layers under `SpaceBackground`. They do **not** consume grid cells, contribute to tile occupancy, block ships or accept buildings.

- **World-anchored nebulae** stay behind a chosen sector of the map as the camera pans.
- **Parallax stars** may move at a different rate to suggest distance.
- **Cosmetic lighting/dust** can be added later, without modifying terrain/gameplay data.
- A painted `Decorations` TileMapLayer is for repeated *grid-aligned surface detail*, not giant celestial scenery.

**Rendering:** WorldGrid no longer draws an opaque map fill. SpaceBackground renders at z=-20; terrain at z=0; transparent grid diagnostics at z=1; Portal/Citadel at z=3; towers at z=5; placement ghost at z=10.

## Implemented terrain pass and next work

The shared six-tile atlas includes unbuildable metal walls, buildable solid platforms, buildable open floors, rocky obstacles, open dirt and transparent decals. Passable/Impassable custom data is cached once at startup; Impassable overrides Passable and Decorations are ignored. Tower occupancy is committed through WorldGrid, with navigation revision updated only when occupancy creates a new blocker. Portal/Citadel footprints are reserved against construction.

The starter map is saved as editable TileMapLayer data. No map generator runs in gameplay. To switch the live level, replace World.tscn's Level scene instance, keeping its node name. For runtime terrain edits, call RebuildTerrain explicitly; editor changes take effect on the next game run.

Next gameplay pass: WaveManager, economy and Citadel defeat/victory; active-source route checks, budgeted navigation, SpawnManager, enemy movement/health and Battle Tower firing are now implemented. Spawning belongs under ENEMIES, tower mechanics under TOWERS. Do not infer nav clearance or route validity merely from a visible approach marker.

**Avoid premature complexity:** no custom editor/plugin, procedural world generation, per-cell Godot nodes, physics bodies per tile or always-running path checks. Revisit tile auto-joining and enemy radius/clearance when the base game is functioning.

## Future master configurations

The existing `WorldMasterConfig.cs` is the current single source for grid size/dimensions. When the terrain, level loading and space-background systems are programmed, add their relevant `*MasterConfig.cs` resources within the relevant feature folder **only for values that genuinely need Inspector tuning**; do not duplicate cell-size constants or create unused resource classes just to fill folders.

## Enemy/combat baseline

WORLD/NAVIGATION adapts WorldGrid into budgeted AStarGrid2D; ENEMIES owns registry, health, movement and spawning. World/EnemySystems contains the shared services; World/Enemies and World/Projectiles contain runtime visuals. Level001/Debug contains F2 input and its screen readout. F5 runs the complete test; F6 remains map-only. Tower Config resources expose functional combat stats and First/Closest targeting. See the relevant feature READMEs for limitations and tuning.
