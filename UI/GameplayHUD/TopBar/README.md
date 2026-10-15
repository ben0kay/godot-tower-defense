# Top Bar — Credits / Citadel Health / Wave

This is the **permanent thin top section** of `UI/GameplayHUD/` in the existing CanvasLayer. **Target order and alignment**:

- **Left:** current `CREDITS` balance, initialized from per-level `StartingCredits` when a runtime economy is implemented.
- **True screen center:** `CITADEL` current/max health **plus a compact horizontal HP progress bar**, colored by health fraction.
- **Right:** `WAVE` number, optionally with total waves when known. Until wave mechanics exist, show `WAVE --`.

Keep the middle block *actually centered on the viewport* independently of the amount of credit or wave text. Fit comfortably at different window widths and keep the center readable. A small Ctrl/Towers hint can move elsewhere rather than crowding this area.

**Currently implemented:** TopBar/Status has independent Credits and Wave labels and a centered Citadel VBox containing Health and Bar. GameplayHUD updates the three reserved zones on resize, keeping the objective centered as numbers grow. Narrow screens scale text and truncate overflow. The Ctrl shortcut remains in the bottom build tray.

Main/Economy seeds and pushes live Credits through SetCredits; SetWave updates text; SetCitadelHealth updates current/max, clamped fill and configured threshold colors. SetCoreHealth forwards for compatibility. Unknown Citadel/Wave state remains -- with no health fill. Layout and palette settings live in UIMasterConfig / DefaultUIConfig.tres.

**Later integration:** `LEVELS/LevelConfig.cs` will eventually specify starting credits per level; a central economy/session service owns the changing balance; Citadel durability is owned by `WORLD/OBJECTIVES/Citadel/`; future `LEVELS/Waves/` owns wave state. Bind event-driven updates to the HUD. Don't simulate those mechanics here.

See **`UI/GameplayHUD/README.md`** for the complete layout, semantics, proposed colors/thresholds, future cost/resource considerations and Codex acceptance tests.

Main/Waves now supplies wave status through SetWaveControls and owns manual progression. The StartWave button is below the right zone (not within the center block); active waves show a disabled remaining count and blocked/setup states have explanatory tooltips. Credits still come from Main/Economy and Citadel remains unknown.

## Live Citadel durability

The central health number/bar now reflects **actual Citadel HP**. The active level's `LevelConfig.CitadelMaxHealth` initializes the reusable `WORLD/OBJECTIVES/Citadel/CitadelHealth`; `Main/CitadelSession` listens to `HealthChanged` and calls the already-existing `GameplayHUD.SetCitadelHealth(current, maximum)`. Arriving ships subtract their configured `CitadelDamage`; killed ships do not. The HUD remains display-only and never owns health or defeat rules. Zero HP emits the model's `Destroyed` event but does not yet show game over.

The Start/Next Wave `Button` now receives `UIButtonFactory.Apply(..., UIButtonStyle.Action)`. The wave scheduler still owns whether it is enabled and the existing HUD still owns its responsive position and click signal. This is styling only.

## Top-bar animation pass (implemented)

The left balance now reads `Credits: 250`, followed immediately by inline `(+10)` or `(-200)` feedback. The existing `CreditGainIndicator.cs` owns both earned and spent displays. Quick transactions of the same type stack within `CreditRewardStackSeconds` (default 0.8 s). Every new event restarts a brief scale/brightness pulse and faint color outline; after inactivity it drifts slightly and fades. Switching from a gain to a spend starts a fresh tally.

The **actual** wallet balance never animates: `ECONOMY/Wallet/EconomyManager` still commits amounts synchronously and broadcasts `BalanceChanged`. `GameplayHUD.SetCredits` performs a display-only count-up/down from the previous visible number to the latest authoritative target. A level initialization snaps immediately and clears old feedback. `TargetCredits` exposes the committed target independently of the moving label.

A successful Credits `Grant` calls `ShowCreditReward`; successful `TryPurchase`/`TrySpend` calls `ShowCreditExpense` **only after commit**. Zero/failed transactions and starting balances create no popup. Future sources can use these same wallet APIs without special HUD code.

The center Citadel **text** stays immediately authoritative, while its progress bar animates on same-session damage and the health number briefly flashes red. New-level/max-health resets snap instead. The wave number briefly enlarges/brightens on **actual forward wave changes**, not on every WaveManager status notification. The Citadel remains anchored to the true viewport center at all window widths.

In `UI/DefaultUIConfig.tres`, adjust **Credit Balance Animation**, **Credit Reward Feedback**, and **Top Bar Reactions** for countdown/pulse/fade/glow/spending colors, Citadel bar transition/flash and wave pulse. Existing gameplay ownership and other HUD panels are unchanged. `TESTS/HudTowerChecks.cs`, `TESTS/EnemyKillRewardChecks.cs`, `TESTS/EconomyChecks.cs`, and `TESTS/CitadelHealthChecks.cs` cover the new visual contract and unchanged economy/health rules.
