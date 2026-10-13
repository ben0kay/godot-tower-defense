# Tower Build Tray

`TowerBar.tscn` is the row of selectable tower tiles, toggled with Ctrl by `GameplayHUD.cs`. The tray starts hidden and can open/close without altering the bottom-right Inspector.

Six **prototype-only** buttons demonstrate categories under the umbrella `TOWERS/`: Gun, Laser, Missile, Repair, Solar and Resource. The Solar example intentionally supports a future economy + defensive laser hybrid.

Clicking a tile displays its planned information and emits `TowerChoiceRequested(id)`. It does not place a tower yet. Future placement should subscribe to this signal, show a grid-snapped ghost, reject invalid cells and create a tower after click.
