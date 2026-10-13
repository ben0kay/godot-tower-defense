# Godot Tower Defense: Interactive Battlefield + HUD

Fully 2D top-down space tower defense in **Godot C#/.NET**. This stage includes a large camera-navigable grid map and the first persistent gameplay HUD foundation. The first **Battle Tower** can be placed and inspected. Enemies, projectiles, waves and economy have **not** been implemented yet.

## Open and run
Open in the Godot **.NET** editor (project SDK 4.7.x), build C# and press **F5**. The configured `Main.tscn` loads directly into the battlefield.

## Current controls
- **WASD / arrow keys:** pan.
- **Middle mouse drag:** pan directly.
- **Mouse wheel:** zoom toward the pointer.
- **Ctrl:** toggle tower build tray.
- **Click BATTLE:** begin grid-snapped tower placement. **Left-click:** place on a free cell; **right-click / Escape:** cancel. After canceling, left-click a placed tower to inspect it.
- **X in Inspector:** close inspector independently of build tray.

## Settings
- `CAMERA/DefaultCameraConfig.tres`: pan speed, smoothing, zoom and boundaries.
- `WORLD/DefaultWorldConfig.tres`: map dimensions, grid, colors.
- `UI/DefaultUIConfig.tres`: HUD dimensions, tray animation, colors and initial visibility.
- `TOWERS/DefaultTowerConfig.tres`: shared range/preview switches; `TOWERS/Offensive/BattleTower/DefaultBattleTowerConfig.tres`: proposed baseline stats.

## Folder plans
- `UI/GameplayHUD/` — TopBar, TowerBar, Inspector and their individual READMEs.
- `UI/Menus/` — reserved Boot, Main and Pause.
- `UI/Shared/` — future reused UI helpers.
- `TOWERS/Placement/` — Battle Tower build ghost, occupied-cell checks and tower selection.
- `FEEDBACK/` — future defense bars, damage numbers and camera effects.

The world grid renders only visible lines using one `Node2D`. The HUD roots ignore mouse input outside visible panels. Status labels show `--` because actual gameplay systems do not exist.

See `NOTES/CodePreferences.md`, `NOTES/NAVIGATION_PERFORMANCE_PLAN.md` and `UI/GameplayHUD/README.md`. In-engine compilation and gameplay verification are still needed locally.
