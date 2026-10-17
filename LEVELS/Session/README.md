## Mission XP connection — implemented 2026-10-17

User approved a provisional, tunable mission-to-profile XP loop before finalized scoring. **This amendment supersedes older statements that missions never grant XP; preserve the original P1–P5/R4 design below as future scope.**

- **Delivered remaining P1/P2/R4 foundation:** each level exposes VictoryBaseXP / DefeatBaseXP (500 / 0 initially); deterministic itemized scoring; captured deployment profile/settings; persisted once-only mission numbers/receipt; atomic XP + level-up RP + award marker; honest failed-save UI and retry. v1/v2 progression migrates to v3 without resetting XP, research, RP or stored curve.
- **Delivered Results/Campaign loop:** saved XP itemization, actual level changes/RP and final within-level bar; show/Skip/reopen never grants. RETURN TO CAMPAIGN opens the existing hub. Research remains Campaign-only. Profileless F6/preview says NOT SAVED.
- **Intentionally deferred original scoring:** first-clear/replay history, replay reduction/anti-farming policy, ETA/time modifiers, kill/tower/performance bonuses and deductions. Replays currently earn the same baseline; signed line-item data is ready for later reward deductions floored at zero, without removing earned levels.
- **Still open:** separate P3 Levels area, mission unlock progression, detailed XP count-up/sounds/R2 polish, Credits telemetry, campaign/battlefield Save–Load/slots and full P5 regressions. Do not mark the combined P1–P5/R4 long-term brief fully complete.
- **Verification:** .NET SDK build (0 warnings/errors), MissionXPChecks, PlayerProgressionChecks, ResultsChecks, ResearchFoundationChecks, ResearchMenuChecks and BattleResearchChecks passed. Results-frame geometry verified at 1920×1080, 1366×768 and 960×540; desktop visual review remains. Existing unrelated historical full-suite blockers remain recorded below.
- **Failure boundary:** leaving a mission whose XP could not save does not persist a recoverable pending battlefield/reward; use RETRY XP SAVE before leaving. Monotonic watermarks assume the existing single active battlefield/application flow.

Authoring/ownership: SYSTEMS/Progression/Missions/README.md. Validation: TESTS/Progression/MissionXP.md.

---

# Level session / mission outcome — R1 implemented

## Real run statistics extension — 2026-10-17

LevelSession now owns one LevelRunStats child: successful paid tower placements, completed first constructions, purchased tower upgrades and unpaused mission time. It subscribes to placement and placed-tower work/purchase events; no scene scans/per-tower polling. Instant builds count immediately; unfinished/canceled sites count as placed but not constructed. Upgrade completion is not a second construction. Historical totals survive tower removal.

The controller freezes stats immediately when an outcome latches, then copies them into the immutable result snapshot with existing official wave-owned kills/breaches/waves/integrity. Final lethal-breach callback arbitration remains R1-owned. Credits earned/spent remain null. Older R1 statements below about untracked duration/construction are superseded by this extension.

Research affects only deployment initialization: CitadelSession resolves authored max HP plus selected-profile purchased bonuses before Initialize/HUD notification; Results uses the same effective denominator. There is no Research UI/purchase access in the battlefield or Results. Mission rewards remain unconfigured/unawarded.

---

`LevelSessionController.cs` is a small level-owned coordinator shared by `Main.tscn` and `MainLevel002.tscn`. State is Playing, Victory or Defeat. It caches sibling services once and uses events; there are no per-frame outcome checks or scene searches.

## Outcome arbitration

Victory is considered only after WaveManager completes the final authored wave (nonzero TotalWaves, AllWavesComplete), then resolved deferred with a fresh positive-health check. CitadelHealth.Destroyed immediately latches Defeat, cancels placement and freezes the tree. Defeat beats a queued victory, including the final enemy's lethal arrival. Its snapshot/presentation is deferred until the synchronous registry/Citadel/wave notifications finish, so the lethal breach and completed final wave are recorded. Neither deferred path runs after the controller unloads. An empty/disabled wave set is not a win.

One final `LevelResultSnapshot` is immutable after capture. Basic counters use an event-based set of actual wave spawns: SpawnManager.WaveEnemyCreated fires before Initialize (covering entry-hazard deaths), registry kill/arrival removes the entry and increments once; TreeExiting forgets manual removals without inventing kills. F2/unowned enemies do not affect official kill/breach totals. WaveManager.CompletedWaves and CitadelHealth supply the remaining actual values. No enemy or wave engine owns mission outcome logic.

## Pause, results and navigation

The controller closes the build tray/cancels its ghost, transfers pause ownership from PauseMenu, binds Escape/Ctrl at results priority 200 and pauses gameplay. LevelResults processes Always above the battlefield. Escape skips reveal/dismisses errors; Ctrl cannot build; F11 remains global. Navigation is unavailable before reveal finishes/Skip. Unload removes callbacks and bindings and releases results-owned pause.

`PlayableLevelCatalog.cs` centralizes explicit launcher registration and strict three-digit level IDs. Level001 launches Main.tscn; Level002 launches MainLevel002.tscn. Only an existing registered immediate next battlefield with the shared service shape is eligible; map existence alone is insufficient. Extend the Entries table when a real new launcher is ready. Missing-next keeps the primary action disabled. Retry reloads the current launcher; shared MenuNavigation handles menu/profileless Boot redirection and pause restoration on failed scene changes. Exit Game releases pause before quitting.

## Scope and future work

R1 does not track successful tower construction, elapsed unpaused time or Credits earned/spent: those nullable fields display `—`. It does not change Credits, waves, navigation, tower ranks, player progression or save schemas. R2 is presentation polish, R3 adds actual event-driven run statistics and additional scoring inputs, R4 defines player XP/rewards/persistence separately from tower rank XP. See `UI/Menus/Results/README.md` for the full remaining checklist and `TESTS/UI/Results.md` for verification.

E3A offspring now publish wave provenance before initialization through the same spawning event. R1 includes real wave-child kills/breaches, excludes F2 descendants, and cannot win while WaveManager has pending death releases. No new presentation/scoring/persistence layer was added.

## Planned mission XP and time telemetry (NOT IMPLEMENTED)

P1/P2 of [the Player XP/Research pass](../../SYSTEMS/Progression/PLAYER_XP_RESEARCH_FOUNDATION_PASS.md) add an authored XP baseline per level, optional target completion time, lightweight **unpaused mission duration**, deterministic bonuses/penalties, and exactly-once profile-wide XP awarding from the **existing** frozen mission outcome. Preserve wave provenance, E3A offspring and existing R1 arbitration. No reward logic belongs in the Results UI.
