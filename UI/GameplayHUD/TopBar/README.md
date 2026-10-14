# Top Bar — Credits / Citadel Health / Wave

This is the **permanent thin top section** of `UI/GameplayHUD/` in the existing CanvasLayer. **Target order and alignment**:

- **Left:** current `CREDITS` balance, initialized from per-level `StartingCredits` when a runtime economy is implemented.
- **True screen center:** `CITADEL` current/max health **plus a compact horizontal HP progress bar**, colored by health fraction.
- **Right:** `WAVE` number, optionally with total waves when known. Until wave mechanics exist, show `WAVE --`.

Keep the middle block *actually centered on the viewport* independently of the amount of credit or wave text. Fit comfortably at different window widths and keep the center readable. A small Ctrl/Towers hint can move elsewhere rather than crowding this area.

**Currently implemented:** TopBar/Status has independent Credits and Wave labels and a centered Citadel VBox containing Health and Bar. GameplayHUD updates the three reserved zones on resize, keeping the objective centered as numbers grow. Narrow screens scale text and truncate overflow. The Ctrl shortcut remains in the bottom build tray.

Main/Economy seeds and pushes live Credits through SetCredits; SetWave updates text; SetCitadelHealth updates current/max, clamped fill and configured threshold colors. SetCoreHealth forwards for compatibility. Unknown Citadel/Wave state remains -- with no health fill. Layout and palette settings live in UIMasterConfig / DefaultUIConfig.tres.

**Later integration:** `WORLD/LEVELS/LevelConfig.cs` will eventually specify starting credits per level; a central economy/session service owns the changing balance; Citadel durability is owned by `WORLD/OBJECTIVES/Citadel/`; future `ENEMIES/Waves/` owns wave state. Bind event-driven updates to the HUD. Don't simulate those mechanics here.

See **`UI/GameplayHUD/README.md`** for the complete layout, semantics, proposed colors/thresholds, future cost/resource considerations and Codex acceptance tests.

Main/Waves now supplies wave status through SetWaveControls and owns manual progression. The StartWave button is below the right zone (not within the center block); active waves show a disabled remaining count and blocked/setup states have explanatory tooltips. Credits still come from Main/Economy and Citadel remains unknown.
