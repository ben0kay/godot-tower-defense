# Chained checkpoints verification

Checkpoints expose `NextCheckpoints`: empty routes to the Citadel, one chains to the next region, multiple choose one uniformly. Names are labels; links establish order. Each enemy captures a validated graph and uses deterministic choices independently of wave RNG. Repaths retain the chosen destination. Cycles, broken links and duplicate outgoing links are rejected.

`CheckpointChainChecks` runs inside `CheckpointChecks.tscn`. It covers linear and nested branching, physical ordered entry, 32 mixed branch traversals, eight forced repaths, captured-link persistence, invalid graphs, wave RNG independence, complete preview traces and downstream invalidation. A 10,000-choice four-way sample produced 2447 / 2398 / 2588 / 2567. The 500 six-node graph snapshot benchmark took 13.51 ms total; this is spawn-time work, not per-frame work or a rendered FPS claim.

The shared editor overlay is installed in Level001, Level020 and LevelTemplate. Tests verify absolute Z 4090, circles/arrows, movement-driven redraw without navigation revision changes, unchanged-cache reuse and no hidden runtime processing. F3 enables the same diagram during previews. Actual graphical editor appearance has not been manually verified.

## Results

- Direct Roslyn C# compilation: passed, using the installed Godot assemblies and source generators.
- CheckpointChecks: passed, including the chain checks and existing clearance, swept entry, permission combinations and 500-enemy checks. Initial checkpoint benchmark: 44.74 ms spawn and 193.45 ms total budgeted route work across 5,000 queries.
- RoutePreviewChecks: passed for both levels, ALL portals, 50 samples, private diversity settings and live enemies.
- CorridorNavigationChecks: passed, all twelve scenarios and movement permissions.
- MenuTowerFlowChecks: passed, real menu construction and paid upgrade flow.
- PlacementPerformanceChecks: passed for 100 and 500 enemies.
- Unmodified EnemyIntegrationChecks: blocked by the pre-existing Level020 painted-tile-count assertion. No map data or assertion was changed. A diagnostic build skipping only that check passed the remaining 31 integration checks, including ammunition foundations; the original source was restored before final compilation. This is not a full-suite pass.

Standard SDK/editor compilation remains unavailable in this container: the installed runtime lacks the SDK/MSBuild dependencies (including Microsoft.Build). Headless runtime tests use the directly compiled assembly. Existing catalog fallback and cached resource-UID warnings remain. Headless timings are environment-dependent, not graphical performance measurements.
