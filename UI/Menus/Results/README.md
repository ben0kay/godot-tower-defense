# Mission Debrief / End-of-Level Results — FOUNDATION R0

**Status: R0 UI foundation committed; victory/defeat integration, gameplay statistics and player XP are NOT implemented.** This file is the canonical implementation handoff for ChatGPT or Codex. Follow existing `NOTES/CodePreferences.md` and `UI/Menus/Pause/` conventions.

## Intended experience

An **over-the-battlefield** sci-fi debrief, not a replacement scene. The world freezes beneath a dark translucent backdrop. Large teal **VICTORY** or red **MISSION FAILED** heading. Stat panels reveal from top to bottom with short gaps; XP placeholder fades in after the last statistic; only then do action buttons appear. Provide a **Skip Reveal** action from the start. Presentation runs while gameplay is paused, and navigation must never strand a later scene in the paused state. Reuse `UIMasterConfig`, `MenuLayout` and `UIButtonFactory`; no parallel theme system.

## Files and ownership (R0)

- `UI/Menus/Results/LevelResults.tscn`: reusable `CanvasLayer` at layer 150, `Always` processing. **Not yet instanced in either level**.
- `UI/Menus/Results/LevelResults.cs`: hidden by default; builds styled UI, `ShowResults(LevelResultSnapshot)`, sequential reveals, `SkipReveal()`, `HideResults()`, and a **strictly opt-in** `PreviewOnReady` / `ShowPreview()` with obviously sample numbers. Emits `RetryRequested` and `MenuRequested` only; **not connected yet**.
- `LEVELS/Session/LevelResultSnapshot.cs`: read-only-at-presentation results DTO and `LevelOutcome` enum. Fields that aren't tracked yet are **nullable** and shown as `—`. Player XP bar is **disabled/empty**, and awards/persistence do not exist.
- `LEVELS/Session/README.md`: explains the future session owner and data sources.

**R0 scope boundary:** no changes to `Main.tscn`, `MainLevel002.tscn`, `WaveManager`, `CitadelHealth`, `EnemyRegistry`, tower placement, economy, `GameInput`, persistence or existing tests. The results scene is a reusable foundation, **not yet reachable in normal gameplay**.

## Next implementation passes

### R1 — Real mission lifecycle, safe triggers and basic results (NEXT)

- [ ] Introduce a small **level-owned `LevelSessionController`** in `LEVELS/Session/`, not under `Enemy.cs`, `WaveManager.cs` or the HUD. Instance the controller and **the same** `LevelResults.tscn` in BOTH `Main.tscn` and `MainLevel002.tscn`; retain their existing `World`, `Waves`, `CitadelSession`, `PauseMenu` paths.
- [ ] **Defeat:** subscribe to `CitadelSession.Health.Destroyed` and end the level immediately at 0 Citadel HP, including mid-wave. **Victory:** only after the final `WaveManager.WaveCompleted`/`AllWavesComplete`, *and* only if Citadel health is positive. **Defeat has priority** if final wave and final damaging arrival happen together. Latch once: cannot show both, double-grant awards or respond to subsequent wave/enemy callbacks.
- [ ] Freeze the world (e.g. `SceneTree.Paused=true`) after capturing final state; ensure results UI remains operable via `ProcessMode.Always`. Avoid leaving pause menu open/active, cancel outstanding placement, and suppress game build/pause input while results overlay is visible. `GameInput` needs a higher-priority, scoped results binding for Escape/Ctrl; leave global F11 working. **Do not** simply hide the pause menu after it has paused the world without ownership coordination.
- [ ] Capture one `LevelResultSnapshot` from authoritative sources: `EnemyRegistry.KilledCount/ArrivedCount`, `WaveManager` completed/total, `CitadelHealth.CurrentHealth/MaxHealth`, actual level identity. Distinguish **debug F2 spawns** from real wave-owned enemies for official mission numbers (registry global counts currently include F2); implement minimal proper event-based tally or explicitly label unfiltered totals until ownership filtering is available.
- [ ] Hook `RetryRequested` to reload the active level scene and `MenuRequested` to `MenuNavigation.Open(..., MenuNavigation.MainScene)`. Unpause safely on success/failure, use an error dialog on navigation failure, and unbind input/events on exit. Support directly running either level with F6 and no profile.
- [ ] Regression test: Citadel destroyed while waves remain, final enemy destroys Citadel, final enemy killed with Citadel alive, no active waves/disabled wave set, double callbacks, pausing during result, Retry, Main Menu, F11, absence of dangling subscriptions.

### R2 — Presentation polish

- [ ] Test the current sequence in the real viewport at common resolutions; tune layout/scrolling, enter/slide animations, number count-up, sounds and spacing (several visible rows before XP).
- [ ] Keep all animations **skippable immediately** without changing gameplay/outcome, XP, counters or level state. Any focus/keyboard skip must not also dismiss the whole screen.
- [ ] Defeat and victory share one scene/layout with appropriate color and copy; no new full-screen replacement scene. Optional debug preview should be off in saved level scenes.

### R3 — Additional actual statistics and scoring inputs

- [ ] Add event-driven `LevelRunStats` under `LEVELS/Session/`: successful towers built, upgrades bought, Credits **earned**, Credits **spent**, elapsed *unpaused* mission time, waves survived and properly attributed enemy kills/breaches. Count only successful placements/transactions, not clicks, ghosts or failed purchases. Avoid full-scene/per-frame scans.
- [ ] Add optional damage/tower-type/efficiency breakdown later when real damage or firing telemetry exists. Don't compute metrics from estimated final wallet balance or invent values.
- [ ] Carefully count DeathSpawn transporter offspring in their wave (E3A) and avoid counting F2/unowned test enemies in official stats. `EnemyRegistry` currently reports global kills/arrivals only.
- [ ] Result snapshots must be immutable after ending the mission; events coming from deferred cleanup cannot keep incrementing displayed totals.

### R4 — Player XP, bonuses and persistence (FUTURE)

- [ ] Separate **player/profile experience** from already-implemented **per-tower rank XP**. Calculate a future XP breakdown (mission victory, eligible kills, wave survival, Citadel preservation, zero-breaches; potentially time/efficiency only with anti-farming rules).
- [ ] Show baseline/bonus XP separately; animate the XP bar **after** the stat reveal; support crossing player levels only when actual progression/rank/save behavior is designed.
- [ ] Do not silently persist XP to `ProfileStore` or invent save schema now; integrate later with approved `SYSTEMS/Saving/` and profile progression. A defeat can have distinct reduced XP when explicitly designed.

## Visual / architectural acceptance

- A real results scene exists, shares the project's teal/navy style, starts hidden, has sample preview on request, stat rows appear sequentially, XP clearly says **coming soon**, action intentions are emitted, and there is zero hidden background polling when not shown.
- UI is presentation-only; mission controller determines outcome; data snapshot owns values; source systems remain authoritative for kills/waves/towers/Credits; no new autoload, generic UI manager, or duplicate win/loss state machine.
- **Verify with Godot .NET locally:** build C#, open `LevelResults.tscn`, toggle `PreviewOnReady` temporarily for UI review, and run existing integration checks. This commit was prepared without running Godot; it must not be called runtime-tested.

## Quick handoff

**Coder's next task = R1**, then R2/R3/R4. Start by checking the actual latest scene graph and signals, wiring only real completion/defeat and navigation; preserve all wave spawning, enemy movement and existing pause functionality. Implement each pass completely with explicit tests before moving on.
