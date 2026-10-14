# Gameplay HUD — Battlefield Status and Build Controls

The HUD runs in the existing **CanvasLayer** from `Main.tscn`, above the hand-painted level. Its job is to **display gameplay state**, not own credits, Citadel durability, wave scheduling, tower prices, or placement mechanics.

## Current implementation

- A permanent `TopBar/`: Credits left, Citadel current/max and compact health fill at true viewport center, Wave right. Unknown values remain `--`; the Ctrl hint lives in the build tray.
- `TowerBar/` opens/closes with **Ctrl** (toggle, not hold); it starts hidden and uses a slide/fade animation.
- **Battle** opens the working tower placement preview. Laser, Missile, Repair, Solar and Resource buttons are still examples.
- The **bottom-right Inspector** is separate from the Ctrl tray. Clicking a build-menu button or placed tower can display information; closing the tray must not close the Inspector.
- The full-screen HUD root ignores mouse events in unused space; its visible controls consume clicks so map placement and camera interaction continue to work.

`GameplayHUD.cs` already exposes **`SetCredits(int amount)`**, **`SetCoreHealth(int current, int maximum)`**, and **`SetWave(int current, int total = 0)`**. They update their corresponding status zones, including health fill and color; the live session economy supplies Credits; Citadel health is still future work; Main/Waves supplies wave state and manual progression. Preserve these existing entry points while refactoring and provide a better-named `SetCitadelHealth` API if useful, with `SetCoreHealth` forwarding to it until all callers are updated.

## Target: thin, persistent top status bar

**Priority for Codex:** change the top status layout to the following three zones, regardless of whether the actual gameplay systems exist yet:

```text
┌─────────────────────────────────────────────────────────────────────────┐
│  CREDITS   2,500            CITADEL  1,000 / 1,000          WAVE  --   │
│                            [████████████████]                           │
└─────────────────────────────────────────────────────────────────────────┘
      LEFT                       TRUE CENTER                      RIGHT
```

**All numbers above are layout examples, NOT current level settings or initialized gameplay values.**

| Position | Display | Current/future source |
| --- | --- | --- |
| **Top left** | Credits icon/label and current balance, e.g. `CREDITS 2,500` | Live **level/session economy** initialized from each level's own starting credits |
| **Top center** | `CITADEL` + **current / maximum health**, and a narrow color-changing health fill | Future **Citadel durability component** on the level's actual `WORLD/OBJECTIVES/Citadel/` instance |
| **Top right** | `WAVE --` until implemented; later `WAVE 03` or `WAVE 03 / 10` when total is known | Live **ENEMIES/Waves/** WaveManager |

### Layout rules

- Keep the **Citadel block visually centered on the actual viewport**, not just centered between a variable-length credit label and the wave label. Consider three explicitly anchored left/center/right containers, or another responsive arrangement that achieves exact center.
- Top bar should be **thin**, unobtrusive, readable over dark nebulae/terrain and use the existing sci-fi palette. Expose relevant height, gaps, text colors and bar dimensions in `UI/DefaultUIConfig.tres` / `UIMasterConfig.cs` if truly useful.
- Credits and wave strings must remain legible as numbers grow (e.g. `12,345,678` or `WAVE 100`); avoid overlap with centered Citadel health. Test at desktop sizes including 1920×1080 and 1366×768. Use sensible margins and graceful text sizing/truncation on narrow windows.
- **Citadel health is more important than the Ctrl shortcut hint.** If necessary, move the hint into the bottom tray/header, an unobtrusive tooltip or a small secondary area. Don't let it consume the right-side wave slot.
- Keep the top bar **always visible** while the gameplay HUD is active. The bottom tower tray remains optional and the bottom-right Inspector remains independent.
- Maintain mouse-filter behavior: the HUD must not block clicks/scroll over open-world space.

### Credits: per-level starting amount, changing during play

- Each level exposes Inspector-editable `StartingCredits` in `WORLD/LEVELS/LevelConfig.cs` / its per-level `.tres`. Different maps can start with different amounts; **don't hardcode 1,000 or any other balance in the HUD**.
- Once the economy exists, one **runtime balance** (owned by a central economy/session service, not the UI or a particular tower) starts from `StartingCredits` on level start/restart and changes as towers are constructed, upgraded, sold, or income is earned. Changing levels initializes the appropriate new starting amount.
- The HUD **subscribes to a CreditsChanged event/signal**, or is pushed updated values, then calls `SetCredits`; it should never calculate costs, spend currency or poll the balance every frame.
- Credits are the primary currency displayed in the thin top bar. Additional build resources (Alloy, Energy Cells etc.) will be part of shared construction/upgrade costs per `TOWERS/README.md`. When relevant, show them in the **build tray or tower Inspector**, with optional small/expandable resource indicators later, rather than crowding the top bar.
- Until an economy and starting-credits field actually exist, display `CREDITS --` or a clearly documented debug value; do not pretend a placeholder balance is spendable.

### Citadel: central durability display

- Rename player-facing `CORE` to **`CITADEL`**. Our defended objective is the physical `WORLD/OBJECTIVES/Citadel/Citadel.tscn`, not a generic core.
- **Show `current / max` health (e.g. `800 / 1,000`) and a compact horizontal health bar.** A percentage-only readout hides the distinction between maps with different maximum health.
- Clamp fill to `[0, 1]`, safely handle max health = 0 / missing Citadel, and never display negative health. Suggested colors: cyan/green healthy, amber at/below 50%, red at/below 25%. Keep thresholds and colors adjustable in the UI config if needed.
- Update health on **damage/healing events** from the Citadel health component. The HUD must not own health, reduce HP when enemies arrive, or poll the Citadel each frame.
- The top-center objective display is persistent even at full health. This is different from `FEEDBACK/DefenseBars/`, where **small per-enemy/tower bars hide when full**.
- Initially, `CITADEL --` plus a hidden/indeterminate health bar is preferable to inventing live HP. Eventually allow displaying a zero-health / defeated state and connecting proper loss handling through a separate gameplay system.

### Wave number: top right, despite waves not existing yet

- With a wave set connected, show `WAVE 00 / total` before starting; without one, show `WAVE --`. Never imply Wave 1 started merely because the level loaded.
- When implemented, show the current wave number; optionally show `current / total` if the scenario has a fixed total. If endless waves exist, omit total.
- Future wave states may include `Preparing`, `In Progress`, `Intermission`, and `Completed` (subtle caption/tooltip, not necessary to implement now).
- Use wave events/signals to update the HUD at state transitions, not every physics frame.

## Existing bottom gameplay HUD — preserve it

```text
GameplayHUD (Control, CanvasLayer parent)
├── TopBar                   # REFACTOR: left Credits / center Citadel HP / right Wave
└── BottomArea
    ├── TraySlot/TowerBar     # Ctrl toggles tower selection/placement, may be hidden
    └── Inspector             # Bottom right; independently shows selected tower/preview
```

- `GameplayHUD.TowerChoiceRequested(string towerId)` is subscribed to by tower placement (currently `battle`).
- `GameplayHUD.ShowTowerDetails(name, category, stats, notes)` is shared by build previews and placed tower inspection.
- `ClearInspection()` only closes the Inspector, not the tray.
- When tower upgrading is implemented, use this existing Inspector for per-tower level, next upgrade preview, costs and Upgrade/Sell actions. See `TOWERS/README.md`.
- The top bar is not a place for every resource inventory item, tower statistics, or debug performance metrics. Keep debug counters in `Level001/Debug`.

## Codex implementation plan

**HUD layout pass (can be done now, with no wave/economy gameplay):**

1. Read `NOTES/CodePreferences.md`, `UI/README.md`, this README, `TopBar/README.md`, `UI/GameplayHUD/GameplayHUD.cs`, `TopBar/TopBar.tscn`, `WORLD/LEVELS/LevelConfig.cs` and `TOWERS/README.md`.
2. Reorganize **existing** TopBar scene into left Credits / absolutely centered Citadel / right Wave. Change labels to `CREDITS --`, `CITADEL --`, `WAVE --` while no authoritative state exists.
3. Add the compact **Citadel HP progress bar** and consistent text formatting. Keep and adapt current `SetCredits`, `SetWave`, `SetCoreHealth` callers; optional wrapper `SetCitadelHealth`. Never break existing NodePaths without updating `GameplayHUD.cs`.
4. Keep height/colors under the current UI master config. Avoid unnecessary timers, per-frame HUD processing, a second full-screen overlay, or custom shaders.
5. Preserve bottom tray animation, Ctrl key handling, mouse filtering, placement clicking, and the bottom-right Inspector. Update relevant README files.

**Later integration passes (only when the mechanics exist):**

6. Add an Inspector-editable per-level starting-credit parameter and a separate runtime economy service. Bind changes to `SetCredits` and connect shared build/upgrade cost transactions.
7. Implement Citadel health/damage/defeat outside the HUD, and bind its health-change signal to the middle display.
8. Implement WaveManager outside the HUD, and bind wave transitions to the right display.

**Acceptance checks:** the three zones are in the requested positions, the center does not shift as left/right numbers grow, and labels stay readable at multiple resolutions; no false gameplay numbers appear before systems exist; simulated calls to each setter update only the proper zone, including Citadel bar fill/thresholds; the Ctrl build menu, Battle Tower placement, Inspector, camera interactions and Enemy001 combat remain unaffected.

**Status:** the HUD-only pass is implemented. SetCitadelHealth displays clamped current/max health and inclusive warning/critical thresholds; SetCoreHealth forwards to it. Missing maximum hides the bar and restores CITADEL --. Live Credits now come from Main/Economy; manual finite wave progression is active; Citadel HP gameplay remains future work.

TopBar/Status uses independent side labels and a center block. Resize events reserve disjoint zones, scale fonts on narrow windows and truncate overflow without shifting the objective. UIMasterConfig exposes status font/gap, Citadel block/bar dimensions and health colors/thresholds. TESTS/HudTowerChecks exercises placeholders, setters, 1920/1366/640 widths, thresholds and independent tray/Inspector behavior.

## Live Credits integration
EconomyManager initializes from the active LevelConfig.StartingCredits and pushes SetCredits through BalanceChanged events. Level001 starts with an Inspector-editable 1,000 credits and successful Battle Tower construction charges 200 by default. UI owns neither funds nor prices. Citadel/Wave still show -- until authoritative systems exist.

## Manual wave controls
TopBar/Status/StartWave is a compact button below the right Wave zone, leaving the thin bar and true-centered Citadel unchanged. WaveManager calls SetWaveControls on state/count events; the button emits WaveStartRequested without owning timers or enemies. Before starting Level001 displays WAVE 00 / 03. Ready/Completed enable Start/Next; active waves disable it and show remaining planned + owned alive. Blocked/error reasons and detailed counts appear in the tooltip. Final completion disables the button; levels without wave sets hide it.
