# Rectangular building footprints — F1–F5 verification

Build with the matching Godot .NET SDK, then run:

```sh
dotnet build
godot --headless --path . res://TESTS/Towers/FootprintChecks.tscn
godot --headless --path . res://TESTS/Economy/AmmunitionLoopChecks.tscn
```

## Focused coverage

Small grouped checks use private in-memory terrain and real production placement/wallet/navigation scenes. No authored maps or global ammunition balance are saved/changed.

- Definition/grid claims: 1×1, 2×2, 3×2 and 2×3; zero/negative/extreme dimensions, right/bottom/outside bounds, non-anchor unbuildable/reserved/occupied cells, exact all-or-none claims/releases and neighbouring claim safety.
- Structural rules: both mixed-category directions for 3×2, 2×2, 2×3; actionable uniform-foundation failure before floods, payment or occupancy. Different artwork within one category remains allowed.
- Real Factory 3×2 and Depot 2×2: six/four indexed tiles, one paid building each, correct whole-rectangle root, any-cell selection of the same instance, actual Inspector dimensions, centered 192×128 ghost, red/green preview, cancellation and 100 preview refreshes without a navigation flood.
- Failures: bad scene root, insufficient Credits, overlapping building and injected late dictionary insertion failure after occupancy; no charged Credits/orphan nodes/partial index entries or neighbour release. EconomyChecks explicitly injects stale index state now that normal removal cleans it automatically; its original rollback assertion remains active.
- Navigation: each individual cell leaves a two-lane passage open, but the combined 3×2 rectangle seals it and rejects in one proposal flood. Live-enemy trap with no active portal sources and nonzero enemy-radius edge clipping also reject.
- Wall placement/removal: six occupied/selectable cells with zero navigation revision, A* point edits, full rebuilds, floods or queued path work. Passable placement/removal: one batch/revision each, complete six-cell occupancy visible to callbacks, six incremental A* edits, no full rebuild.
- 500-enemy stress: one reusable proposal flood per click, not per footprint cell. Six-cell blocking coalesces into one validation for each of 500 affected indexed routes; subsequent validation/A* count budgets remain strict and removal queues no extra validation.
- Existing ammunition suite purchases the larger endpoints and verifies centered Factory/drone departure, production, exact 1,000-round shipments, 10,000 Depot storage, both physical drone legs and return, reservations, 900 actual weapon shots, refill, Inspector updates, interruptions and conservation.

## Actual execution — 2026-10-16

Godot 4.7.2 Mono / .NET 8.0.31 headless:

- C# compilation passed with the installed Roslyn 4.11 compiler, matching Godot source generator and reference assemblies, no diagnostics. Ordinary `dotnet build --no-restore` remains blocked before compilation by the environment's missing `NuGet.Protocol, Version=6.11.2.1` SDK dependency.
- FootprintChecks and AmmunitionLoopChecks passed with process exit 0. MenuTowerFlowChecks, PauseControlsChecks and ResultsChecks passed with exit 0. CheckpointChecks, RoutePreviewChecks, CorridorNavigationChecks and PlacementPerformanceChecks also passed with exit 0.
- Representative final footprint measurement: **0.456 ms mean per 3×2 validation with 500 stationary enemies**, 20 measured clicks after cache warm-up, one flood/click. Earlier repeats were 0.442–0.478 ms. These are host-specific headless CPU timings, not rendered FPS or a comparison against the old one-cell workload.
- The unchanged full EnemyIntegrationChecks stops at the preexisting Level020 painted-terrain assertion (“The western open ground and eastern maze are fully painted”). To verify downstream systems, a separate diagnostic build temporarily omitted only `Level020Checks.Run()` and restored the tracked source immediately. After fixing the repeated-build Inspector regression and explicitly injecting the economy rollback fixture's stale index, the diagnostic run exited 0 through all remaining 31 enemy/navigation/combat integration checks, including beams, shields, AoE, shockwaves, spatial queries, explicit levels/ranks, waves, wallet and selective repathing. This is not claimed as an unmodified full-suite pass.
- One repeated ammunition stress run printed PASS but then exited 139 during Mono/native shutdown, so that run was not counted as a pass. The ammunition runner now uses the existing test-only freed-fixture finalization barrier before native teardown, as the navigation/results suites already do. There is no forced GC or new processing in gameplay.

No desktop visual review was available. Headless checks validate footprint/ghost geometry, selection and drone motion; visual scale/presentation can be reviewed in the editor. Rotation, irregular footprints, upgrade-time resizing, selling UI and saving remain out of scope. Factory/Depot prices, inventory, production, cargo and offensive tower combat values are unchanged.
