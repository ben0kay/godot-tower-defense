## Planned CP1: clickable Citadel credit meters (NOT IMPLEMENTED)

The current top-centre health panels are **not yet Credit collection buttons**. CP1 will add a small separate green stored-Credits bar/amount to **each existing Citadel health cell** and allow clicking that cell to collect Credits from its own Citadel, matching clicking the corresponding world Citadel. No second top bar, no shared/pooled Credit buffer, no health/status binding changes, and no overlapping Wave/Credits hitboxes. All visual bindings come from one per-Citadel `CreditBuffer`; collection uses the same authoritative wallet-grant action for either click source. Preserve one-row responsiveness/labels and test multiple objectives and small displays. See [ECONOMY/Production/README.md](../../../ECONOMY/Production/README.md#cp1-interaction-decision--collect-from-world-citadel-or-its-top-centre-hud-cell-2026-10-18-planned).

---

## MC3 HUD + F3 — implemented 2026-10-18

Multiple Citadels now share **one horizontal row** in the top-center HUD: equal-width cells for two, four or more objectives, never stacked vertically. A one-Citadel level keeps the original nodes/layout/SetCitadelHealth behavior. Each multi meter shows its stable CITADEL number, independent current/max HP, threshold-colored fill and damage-only animation/flash. Zero values and maximum resets are supported; rebind/unload disconnect model events and stop old tweens. The full Citadel number/HP is available in each cell's tooltip. No HUD health polling or gameplay authority is introduced.

UIMasterConfig exposes MultiCitadelBlockWidth (560 default) and CitadelCellGap (6), alongside existing bar height/palette/timing settings. The center remains viewport-centered and capped to the existing responsive center zone (46% of width); Credits and Wave retain separate side zones. Short cells truncate text rather than wrap into extra rows. Very large counts/HP values can require the tooltip for full text. Existing Inspector/build tray geometry is unchanged.

F3 now passes each portal's **actual cached live Citadel identity** into RoutePreviewJob. Raw/refined paths, neutral comparisons and required checkpoint chains end at that assigned objective, not the primary Citadel. Legend/source labels show portal → CITADEL N; destination approach markers and one label per objective use the same stable identity colors/numbers as the HUD. ALL PORTALS keeps the existing shared preview query/time budget and private profile pool. Missing assignments appear as UNASSIGNED diagnostics with no invented fallback route. All destination goals/availability and cached assignments participate in invalidation, including secondary objectives. Closing/Clear releases private preview resources only.

Explicit TerrainRebuilt now refreshes source assignments during the authoring rebuild itself, so F3 reads current cached ownership without triggering a live assignment calculation. Ordinary tower occupancy still cannot reassign portals, and preview settings/simulation never spawn enemies or alter live route queues/profile pools/random streams.

Validation: compiled with the installed .NET SDK/MSBuild; MultiCitadelHudChecks passed one/two/four objective presentation at 1920×1080, 1366×768 and 960×540, independent HP/colors/zero/maximum reset, rebind, actual assigned F3 endpoints, secondary-goal invalidation and live-state isolation. Existing RoutePreviewChecks (both maps/raw/refined/checkpoints/50 samples/500 enemies), MultiCitadelChecks, CheckpointChecks, ResultsChecks, MissionXPChecks, PlacementHudChecks and CompactInspectorChecks passed. See TESTS/UI/MultiCitadelHud.md. Desktop rendered appearance/playfeel still needs user review; headless geometry checks are not a screenshot or full-suite/FPS claim. Existing gameplay maps/waves were not changed. This amendment supersedes earlier MC3-pending/primary-only-preview statements without deleting the original pass plan.

---

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

In `UI/DefaultUIConfig.tres`, adjust **Credit Balance Animation**, **Credit Reward Feedback**, and **Top Bar Reactions** for countdown/pulse/fade/glow/spending colors, Citadel bar transition/flash and wave pulse. Existing gameplay ownership and other HUD panels are unchanged. `TESTS/UI/HudTowerChecks.cs`, `TESTS/Economy/EnemyKillRewardChecks.cs`, `TESTS/Economy/EconomyChecks.cs`, and `TESTS/Integration/CitadelHealthChecks.cs` cover the new visual contract and unchanged economy/health rules.
