# Godot Tower Defense: Interactive Map Prototype

Fully 2D, top-down space tower defense foundation in **Godot C#/.NET**. This first milestone contains a playable view of a large grid map, not yet towers or enemies.

## Open and run

Use the **Godot .NET** editor version matching the project (4.7.x). Build C# scripts in the editor so custom resource classes register, then run with **F5** (`Main.tscn` is already selected as the main scene).

Camera controls: **WASD/Arrows** pan, **middle mouse drag** to pan freely, **mouse wheel** zooms toward the cursor.

## Customize
- `CAMERA/DefaultCameraConfig.tres`: speed, smoothing, zoom and camera boundary limits.
- `WORLD/DefaultWorldConfig.tres`: map cell dimensions (100 x 80 by default), 64-pixel grid, colors and visibility.
- `FEEDBACK/`: placeholders for defense bars, damage numbers and camera effects.

Debug overlay shows hovered cell, zoom, camera coordinates, FPS. World grid is drawn using one Node2D rather than thousands of scene nodes.

See `NOTES/CodePreferences.md` and `NOTES/NAVIGATION_PERFORMANCE_PLAN.md` for coding conventions and next steps. **Runtime compilation and in-engine tests still need to be performed locally.**
