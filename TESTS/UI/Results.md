# Mission Debrief R1 verification

Build C# with a matching Godot .NET SDK, then run:

```sh
dotnet build
godot --headless --path . res://TESTS/UI/ResultsChecks.tscn
godot --headless --path . res://TESTS/UI/ResultsExitChecks.tscn
godot --headless --path . res://TESTS/UI/ResultsExitChecks.tscn -- --victory
```

ResultsChecks runs MissionOutcomeChecks and ResultsNavigationChecks against actual production launchers, not mock result signals. It tests final kills, lethal final/mid-wave breaches, destruction after queued victory, exactly-once outcomes, empty/unfinished authored waves, actual wave-only stats (including spawn-entry Lava), frozen snapshots, untracked dashes/empty XP, placement/ghost cancellation, Pause takeover, Ctrl/Escape/F11, natural reveal while paused, immediate Skip, disabled Details and missing-next, actual Retry on both levels, Next to MainLevel002, Main Menu/profileless Boot, navigation failure recovery and input cleanup. ResultsExitChecks verifies the real Exit Game handler in separate defeat/victory processes with a failure timeout.

The navigation fixture injects an in-memory profile list solely to exercise menu flow; it does not call profile saving. Test terrain/configuration mutations are restored and no authored map is saved. The unload-before-deferred-result fixture verifies stale callbacks cannot reopen results or strand pause.

## Executed 2026-10-16

Godot 4.7.2 Mono headless / .NET 8.0.31:

- **C# compilation passed**, using Roslyn 4.11 plus the installed Godot source generator and reference assemblies, with no diagnostics. The ordinary `dotnet build --no-restore` CLI fails before compilation because the environment's SDK is missing `NuGet.Protocol, Version=6.11.2.1`; this is not a successful ordinary SDK build.
- **ResultsChecks passed with process exit 0**, repeated; **ResultsExitChecks passed with exit 0 in both outcomes**.
- **PauseControlsChecks, MenuTowerFlowChecks, AmmunitionLoopChecks, CheckpointChecks, RoutePreviewChecks, CorridorNavigationChecks and PlacementPerformanceChecks passed with exit 0**. Existing authored missing-catalog warnings still exercise the automatic default-catalog fallback.
- Early stress runs printed PASS but exited 139 during native Mono shutdown. Those runs were not counted as passes. The affected runners now explicitly finalize freed test-world wrappers before native engine teardown; final R1/checkpoint/preview/placement runs exited 0. The barrier is test-only and introduces no gameplay GC or per-frame work. Older subsystem fixtures detach session/results before deliberately altering waves/HP; all original assertions remain.
- **The unmodified monolithic EnemyIntegrationChecks remains blocked** by the existing Level002 painted-terrain assertion: “The western open ground and eastern maze are fully painted.” Both authored map files are unchanged by R1. A separate diagnostic build temporarily omitted only `Level002Checks.Run()` to reach the remainder; that run exited 0, including all 31 enemy/navigation/combat integration checks, shields/AoE/Shockwave/tower ranks/levels and existing wave tests. The omission was restored immediately, is not committed, and is not reported as a full-suite pass.
- No interactive desktop/visual polish review was performed. Headless checks verify state/navigation/reveal behavior, not artwork/layout at all resolutions; that remains R2.

No player XP, mission rewards, persistent progression, duration/economy estimates or advanced statistics were added. See the Results and Session READMEs for R2/R3/R4 scope.
