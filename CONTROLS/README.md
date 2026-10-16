# CONTROLS — Global and contextual shortcuts

Implemented: `GameInput.cs` is one always-processing Autoload at `/root/GameInput`, registered in `project.godot`. It routes named Input Map actions without implementing gameplay, menus, camera or combat. Defaults are registered once at startup if an action does not already exist; future authored/rebound actions are preserved.

| Action | Default | Ownership |
| --- | --- | --- |
| `game_fullscreen` | F11 | Global in Boot/Main/Campaign/levels/Pause |
| `game_build` | Ctrl | GameplayHUD toggles the existing tower tray; Pause consumes it |
| `game_back` | Escape | Higher-priority modal → active placement cancellation → Pause/resume |

F11 remembers windowed/maximized state before fullscreen and restores it on the next press. A fullscreen startup returns to windowed. Release and OS repeat are ignored. F11 is handled in `_Input`; contextual actions use `_UnhandledKeyInput` so focused GUI can consume input first.

## Scoped ownership

`Bind(owner, action, callback, priority)` registers a screen-owned callback. Return true to consume an event, false to let a lower-priority binding try. Higher priority wins. Owners explicitly `Unbind(this)` in `_ExitTree`; the router also checks owner validity, tree membership and `CanProcess` before dispatch. No scene-name guessing, per-frame discovery or repeated searches occur.

- GameplayHUD owns tray behavior and its Ctrl binding.
- TowerPlacementController owns placement and priority-10 Escape cancellation; RMB stays local. Old raw Ctrl/Escape listeners were removed in the same migration.
- PauseMenu owns the level's ordinary Escape binding. Its always-processing UI resumes on Escape and blocks Ctrl at priority100 while open. Paused gameplay nodes cannot receive callbacks.
- Future modal screens can register a higher-priority Back binding and retire it when closed. Existing Godot dialogs get GUI input first.
- Boot/Main/Campaign register no gameplay bindings. Ctrl and Escape do nothing there. Changing scenes retires level bindings.

Camera WASD/arrows/middle-mouse/wheel remain in CameraController. Pausing clears its drag latch to handle releases during pause. F2/F3 remain level-owned debug actions and stop with the paused world. No player-character controller or unused context framework was added.

## Scene setup and verification

Both `Main.tscn` and `MainLevel002.tscn` instance `UI/Menus/Pause/PauseMenu.tscn` once. Direct F6 play needs no selected profile. Pause uses real SceneTree pause, layer100 above HUD/debug UI, and `MenuNavigation` for unpaused scene transitions. Save/Load/Options remain disabled placeholders.

Run `TESTS/UI/PauseControlsChecks.tscn` and `TESTS/UI/MenuTowerFlowChecks.tscn` after compiling. See `TESTS/UI/PauseControls.md` for actual results and desktop-only verification limits. No controls settings or rebinding UI is implemented yet.

Mission Debrief R1 uses scoped priority-200 Escape/Ctrl bindings: Escape skips the reveal or closes its navigation error, Ctrl is consumed, and Pause cannot reopen/resume the finished mission. F11 remains global. Session unload unbinds both.
