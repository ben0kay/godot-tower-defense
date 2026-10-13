# Gameplay HUD

## Now implemented
- Thin permanent `TopBar/` with placeholder `WAVE --`, `CREDITS --`, and `CORE --`.
- `TowerBar/` is **hidden by default**. Press **Ctrl** once to toggle it; smooth slide/fade on show/hide.
- Six buttons: **Battle**, Laser, Missile, Repair, Solar and Resource Tower. Battle starts real grid placement; other choices still only show sample Inspector details.
- `Inspector/` appears bottom right, can be closed with X, **remains visible if the tower tray closes**, and is intended to display already-placed towers later.
- The HUD uses an existing full-screen CanvasLayer but ignores mouse input outside its visible controls. Clicks on panels do not pass to world placement.

## Configuration
Edit `UI/DefaultUIConfig.tres` in the Godot Inspector. Configure top bar height, bottom tray height, inspector width, margins, tower button width, toggle animation, open-on-launch and colors.

## Connection points for future gameplay
- `GameplayHUD.TowerChoiceRequested(string towerId)` is emitted on tower-button clicks. The placement controller subscribes to `"battle"` and shows a grid ghost; other IDs are still placeholders.
- `GameplayHUD.ShowTowerDetails(name, category, stats, notes)` presents a selected placed tower in the same Inspector.
- `ClearInspection()` closes only the Inspector.
- `SetWave(current, total)`, `SetCredits(amount)` and `SetCoreHealth(current, maximum)` can accept real state once wave/economy/core systems exist.

The labels read `--` until live systems provide numbers. Prototype details use **TBD** for unimplemented balance. Keep this HUD separate from actual tower logic/placement: those will live in `TOWERS/Placement` and their own feature systems.

**Run:** pull, build the C# project in Godot .NET, run `Main.tscn`; press Ctrl, click BATTLE, hover the grid and left-click to place. Right-click or Esc cancels; left-click a placed Battle Tower to inspect it. Camera pan/zoom should still work over the open world.
