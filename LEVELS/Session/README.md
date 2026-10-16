# Level session / mission outcome — R1 implemented

`LevelSessionController.cs` is a small level-owned coordinator shared by `Main.tscn` and `MainLevel002.tscn`. State is Playing, Victory or Defeat. It caches sibling services once and uses events; there are no per-frame outcome checks or scene searches.

## Outcome arbitration

Victory is considered only after WaveManager completes the final authored wave (nonzero TotalWaves, AllWavesComplete), then resolved deferred with a fresh positive-health check. CitadelHealth.Destroyed immediately latches Defeat, cancels placement and freezes the tree. Defeat beats a queued victory, including the final enemy's lethal arrival. Its snapshot/presentation is deferred until the synchronous registry/Citadel/wave notifications finish, so the lethal breach and completed final wave are recorded. Neither deferred path runs after the controller unloads. An empty/disabled wave set is not a win.

One final `LevelResultSnapshot` is immutable after capture. Basic counters use an event-based set of actual wave spawns: SpawnManager.WaveEnemyCreated fires before Initialize (covering entry-hazard deaths), registry kill/arrival removes the entry and increments once; TreeExiting forgets manual removals without inventing kills. F2/unowned enemies do not affect official kill/breach totals. WaveManager.CompletedWaves and CitadelHealth supply the remaining actual values. No enemy or wave engine owns mission outcome logic.

## Pause, results and navigation

The controller closes the build tray/cancels its ghost, transfers pause ownership from PauseMenu, binds Escape/Ctrl at results priority 200 and pauses gameplay. LevelResults processes Always above the battlefield. Escape skips reveal/dismisses errors; Ctrl cannot build; F11 remains global. Navigation is unavailable before reveal finishes/Skip. Unload removes callbacks and bindings and releases results-owned pause.

`PlayableLevelCatalog.cs` centralizes explicit launcher registration and strict three-digit level IDs. Level001 launches Main.tscn; Level002 launches MainLevel002.tscn. Only an existing registered immediate next battlefield with the shared service shape is eligible; map existence alone is insufficient. Extend the Entries table when a real new launcher is ready. Missing-next keeps the primary action disabled. Retry reloads the current launcher; shared MenuNavigation handles menu/profileless Boot redirection and pause restoration on failed scene changes. Exit Game releases pause before quitting.

## Scope and future work

R1 does not track successful tower construction, elapsed unpaused time or Credits earned/spent: those nullable fields display `—`. It does not change Credits, waves, navigation, tower ranks, player progression or save schemas. R2 is presentation polish, R3 adds actual event-driven run statistics and future DeathSpawn ownership, R4 defines player XP/rewards/persistence separately from tower rank XP. See `UI/Menus/Results/README.md` for the full remaining checklist and `TESTS/UI/Results.md` for verification.
