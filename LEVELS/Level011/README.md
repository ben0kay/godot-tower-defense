# Level011 — large authoring map

**Source implemented / awaiting owner validation (2026-11-05).** Now inherits `LEVELS/Shared/BaseLevelMap.tscn`, with four local editable terrain layers and independent objects/settings. The shared base owns common structure/background/debug. A focused before/after property comparison preserved every original node property, terrain layer and instance reference; no runtime test was run for this migration.

- **192 × 120 cells at 64px** = **12,288 × 7,680 world pixels**. `Level011WorldConfig.tres` is used only by this mission through `Level011Config.WorldConfig`. Existing levels still use `WORLD/DefaultWorldConfig.tres` (100 × 80).
- **Local and paintable:** Passable, Impassable, Interactive and Decorations remain independently editable. The initially blank map now contains owner-authored terrain. All original TileSet references, background hierarchy, optional atmosphere, checkpoints and F2/F3 debug tools from the template are present.
- **One movable portal** `Portals/EnemyPortal1` at (2080,3872) and **one movable Citadel** `Objectives/Citadel` at (8224,3872). These are initial positions, not a prescribed map design. Portal/Citadel footprints and approach markers come from their existing shared scenes.
- **Dedicated settings:** `Level011Config.tres` with 2,000 starting Credits, 1,000 Citadel HP, a single active portal and unique `level011` wave ID. Edit this file for settings, portal NodePaths and new CitadelPaths. Do not edit the common World config when tuning Level011's bounds.
- **Editable placeholder wave:** `Level011Waves.tres` has one short 10-Standard-Drone batch purely to let you test a painted route. Customize it in the existing Wave Editor; no other mission's waves are changed.
- **Campaign:** `Level011Mission.tres` is enabled as mission 11. Launch via Campaign list; it does not become Level003's Next because missions 004–010 do not exist. No new launcher/world scene needed.

## Right-edge bounds correction — 2026-11-05

Level011's authored terrain now reaches cell X=175 and its Citadel is at X=10336px, beyond the old 160-cell/10240px limit. Its local WorldConfig is now **192×120** (12288×7680px), with room around the current right edge. Camera, navigation, power and placement all reuse these bounds; no camera code, scene paint, objects or global defaults changed. Restart/redeploy the mission to rebuild its grids. Brief source/coordinate comparison only; gameplay not run.

## Painting

1. Open `res://LEVELS/Level011/Level011.tscn` in the Godot 2D editor.
2. Paint connected Passable cells between the portal's spawn exit and the Citadel's approach points. Add Impassable/Interactive/Decorations as desired. Empty terrain is intentionally unbuildable and cannot route enemies.
3. Move/add Portals or Citadels freely, updating `Level011Config.tres` references as required. Keep objects and their approach/spawn markers on the 64px grid.
4. Launch from Campaign and run the short wave after you have a valid route.

**Design note:** `LevelConfig.WorldConfig` is optional. `WorldMapLoader` applies it to the existing shared `World/Grid` before navigation/grid initialization. It does not introduce new gameplay services, extra processing or any map streaming. The 192×120 grid has 2.88× the cell count of existing maps; check navigation, power and camera performance in Godot before enlarging it again.

**Validation:** source/resource shape reviewed only; Godot import/playback and desktop performance remain owner-side.

