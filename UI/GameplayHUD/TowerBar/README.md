# Tower Build Tray

`TowerBar.tscn` is the row of selectable tower tiles, toggled with Ctrl by `GameplayHUD.cs`. The tray starts hidden and can open/close without altering the bottom-right Inspector.

Six buttons demonstrate categories under `TOWERS/`: **Battle** (placeable), Laser, Missile, Repair, Solar and Resource (Inspector-only placeholders). The Solar example intentionally supports a future economy + defensive laser hybrid.

Clicking a tile displays its planned information and emits `TowerChoiceRequested(id)`. The placement system subscribes to `"battle"`, previews a grid-snapped ghost, rejects occupied cells, and builds a tower on left-click. Other buttons remain informational.

All six existing `Button` nodes now use `UIButtonFactory.Apply(button, UIButtonStyle.TowerTile, Config)` for visual consistency. Their scene hierarchy, prototype data, signals and tower placement behavior are unchanged.
