# Top Bar — Credits / Citadel Health / Wave

This is the **permanent thin top section** of `UI/GameplayHUD/` in the existing CanvasLayer. **Target order and alignment**:

- **Left:** current `CREDITS` balance, initialized from per-level `StartingCredits` when a runtime economy is implemented.
- **True screen center:** `CITADEL` current/max health **plus a compact horizontal HP progress bar**, colored by health fraction.
- **Right:** `WAVE` number, optionally with total waves when known. Until wave mechanics exist, show `WAVE --`.

Keep the middle block *actually centered on the viewport* independently of the amount of credit or wave text. Fit comfortably at different window widths and keep the center readable. A small Ctrl/Towers hint can move elsewhere rather than crowding this area.

**Currently implemented:** `TopBar.tscn` has plain `Wave`, `Credits`, `Core`, and `Shortcut` labels in a single horizontal container, in the wrong target order; `GameplayHUD.cs` exposes `SetWave`, `SetCredits` and `SetCoreHealth` for future state updates. No real credit balance, Citadel HP or wave system is connected.

**Codex HUD-only pass:** rearrange the TopBar layout (Credits left, Citadel center, Wave right), replace visible `CORE` with `CITADEL`, and add a styled health fill with safe `current/max` formatting. Retain compatibility with the current setters and NodePaths when refactoring. Show `--` for uninitialized state, not fake numbers.

**Later integration:** `WORLD/LEVELS/LevelConfig.cs` will eventually specify starting credits per level; a central economy/session service owns the changing balance; Citadel durability is owned by `WORLD/OBJECTIVES/Citadel/`; future `ENEMIES/Waves/` owns wave state. Bind event-driven updates to the HUD. Don't simulate those mechanics here.

See **`UI/GameplayHUD/README.md`** for the complete layout, semantics, proposed colors/thresholds, future cost/resource considerations and Codex acceptance tests.
