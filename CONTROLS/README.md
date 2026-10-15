# CONTROLS — Global Input and Context-Aware Shortcuts

**Status: PLANNED / NOT IMPLEMENTED.** This directory is reserved for the game's input coordination foundation. Currently this README is the **only** file here: do not treat `GameInput.cs`, key mapping or pause functionality as existing. Gameplay is unchanged by this planning note.

## Purpose

Keep global and context-dependent **keyboard shortcut routing** easy to find as the project grows beyond tower defense. This is **not** a player-character controller and must not become a giant script that directly implements camera motion, tower placement, menus, combat or other systems.

Suggested future files:

```text
CONTROLS/
├── README.md            # This plan; actual key assignments and ownership
├── GameInput.cs         # Planned: one global shortcut/router Autoload
└── InputContext.cs      # Optional later: minimal context/registration helper if actually needed
```

Reuse Godot's **Input Map** for named actions/keybindings where practical. Avoid hardcoded key checks spread across unrelated game systems.

## Current controls (already implemented elsewhere)

| Key / input | Current owner | Current action |
| --- | --- | --- |
| Ctrl (press once) | `UI/GameplayHUD/GameplayHUD.cs` | Toggle tower build tray |
| Escape during placement | `TOWERS/Placement/TowerPlacementController.cs` | Cancel build placement |
| Right mouse button during placement | `TowerPlacementController.cs` | Cancel build placement |
| WASD / arrows, middle mouse, wheel | `CAMERA/CameraController.cs` | Pan, drag and zoom camera |
| F2 | `LEVELS/Debug/LevelDebugController.cs` | Cycle/spawn registered debug enemies |
| F11 | Not implemented | Planned fullscreen toggle |
| Escape during ordinary gameplay | Not implemented | Planned pause menu |

`UI/Menus/Pause/README.md` already contains the separate future Pause Menu plan. Do not claim it exists or enable unimplemented Save functionality.

## Proposed input model

### 1. Global shortcuts

- **F11 — Toggle fullscreen/windowed.** Available in Boot, Main Menu, Campaign Menu, all battlefield levels and future screens, even when gameplay is paused.
- Add one small `CONTROLS/GameInput.cs` `Node` as a **Godot Autoload**, registered in `project.godot` under `[autoload]` alongside existing `GameAudio`. It lives under `/root/GameInput`, **not** inside `Main.tscn`, each map scene, or each menu. Never duplicate it across level scenes.
- Set the global router to an appropriate process mode so it can receive F11 even while the scene tree is paused. Ignore key-release events and OS auto-repeat.
- On F11, remember the previous window mode (windowed or maximized) before switching to fullscreen; toggle back to that previous mode. Handle an already-fullscreen startup and window mode changes gracefully.
- Initially register **only F11**. Existing shortcuts must remain unaffected; the Input Map/context foundation should not consume shortcuts that are still handled by their current owners.

### 2. Contextual shortcuts

A context is the **currently active screen/game mode**, not a string guessed from the scene's filename or a repeated search for nodes.

Examples:

| Active context | Ctrl | Escape | F11 |
| --- | --- | --- | --- |
| Boot | No action | No action for now | Fullscreen |
| Main Menu | No action | Future back/quit confirmation | Fullscreen |
| Campaign Menu | No action | Future return to Main | Fullscreen |
| Tower Defense gameplay | Toggle tower build tray | Cancel placement **or** open Pause | Fullscreen |
| Tower Defense pause overlay | No gameplay Ctrl action | Resume | Fullscreen |
| Future space shooter / empire game mode | May have completely different Ctrl action | Context's pause/back action | Fullscreen |

**No active binding = do nothing.** The global router must **not** attempt to call gameplay HUD/placement methods when a Boot/Main/Campaign menu is active; no null-reference exceptions, warnings or invisible build tray actions.

Prefer a **small scoped action-registration system**: the active screen/controller registers actions it supports when entering and unregisters them when leaving. A stack or explicit priority layer allows modal UI and Pause to override underlying gameplay shortcuts. Avoid retaining stale scene references after changes. No scene-tree polling or per-frame discovery is needed.

**Escape priority in Tower Defense:**
1. A modal/dialog or higher-priority Settings screen gets Escape first.
2. Pause Menu open → close Pause / resume.
3. Tower placement active → cancel placement without also opening Pause.
4. Otherwise, normal gameplay → open Pause Menu (when implemented).

Ctrl must be blocked from toggling the build tray behind Pause or another modal. If another game mode maps Ctrl to something different, that mode's active context defines its action. Context bindings should not leak across scene changes.

### 3. System responsibilities (keep separate)

- `GameInput.cs`: recognizes a global/contextual input and dispatches it to the **current owner**, with proper priority and input consumption.
- `GameplayHUD.cs`: owns actual tower tray animation/open/close (the router does **not** reimplement it).
- `TowerPlacementController.cs`: owns placement state, click handling and cancellation.
- `CameraController.cs`: owns camera movement/drag/zoom; leave camera axes and mouse handling local unless there is a clear benefit to a reusable Input Map action.
- `LEVELS/Debug/LevelDebugController.cs`: owns debugging/F2; don't make that a normal global gameplay action.
- `UI/Menus/Pause/`: owns future pause menu UI; only resume/transition orchestration belongs in the shortcut/context layer.

**Important migration rule:** When moving Ctrl/Escape from an existing owner into `GameInput`, remove or disable the old key listener *in the same change*. Otherwise one keypress may fire twice. Keep the existing event-handling priority and RMB/Escape cancellation semantics.

## Suggested Codex implementation phases

1. **Foundation / F11 only:** Create a minimal `GameInput.cs`, register one global Autoload in `project.godot`, document F11, verify window-mode restoration and that F11 works in menus and gameplay. Leave existing Ctrl/Escape/F2/camera/mouse listeners untouched. Do not add an empty `InputContext.cs` or an unused generic input framework merely for future-proofing.
2. **Context routing:** When actually migrating Ctrl/Escape, add the smallest scoped registration/priority mechanism that fulfills the table above. Adapt `GameplayHUD` and `TowerPlacementController` once, with safe registration/unregistration across scene transitions and modals.
3. **Pause Menu:** Implement `UI/Menus/Pause` as an independent pass. Ensure its UI processes while the world is paused, pausing/resuming never leaves a scene stuck, and returning to menus uses `MenuNavigation`. Saving stays disabled until a real campaign save implementation exists.
4. **Future:** Extend named Input Map actions, rebindable controls, contextual shortcuts for other game modes and optional control settings only when needed.

## Acceptance checks

- F11 works from Boot, Main Menu, Campaign, Level001 and Level002; repeated F11 restores the prior window mode; no shortcut double-fires.
- With only F11 implemented, Ctrl still opens/closes the Tower Defense build tray, Escape still cancels placement, F2 still debugs enemy spawning, and camera/mouse controls are unaffected.
- Once contexts are implemented: Ctrl does absolutely nothing in Boot/Main/Campaign unless intentionally mapped there, never calls unloaded HUD nodes, and is blocked behind Pause; global F11 keeps working.
- One Escape press performs **only one** highest-priority action (cancel placement, resume, dialog back, or pause) appropriate to current context.
- Changing scenes automatically retires old contextual bindings; direct `Main.tscn` / `MainLevel002.tscn` playtesting does not require a profile or campaign launcher.
- No per-frame input-target searches, duplicate Autoload nodes, giant controllers or speculative system rewrites. Test Godot .NET builds and the relevant UI/menu/placement integration suites; report actual results.

**Current request:** only create this folder/README for Codex. No `GameInput.cs`, Autoload setting, F11 behavior, control migration or Pause Menu has been implemented yet.
