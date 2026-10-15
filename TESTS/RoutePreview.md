# F3 route preview — usage and verification

Expanded 2026-10-15 on GitHub base `9e0cb0f`, Godot 4.7.2 Mono / .NET 8. Rebased before push onto `0b0adc0` (economy planning-doc changes only; runtime source unchanged). Checkpoints remain deferred.

## Try it

Load a campaign level normally (or F6 Main/MainLevel002). Press **F3**. Choose one portal or **ALL PORTALS**, a catalog enemy type, **1–50 paths per portal**, and Final Refined / Raw A*. Click **Simulate Paths**. White shows unweighted shortest routing; colored portal families show sample paths. The scrollable legend reports profile, distance overhead, duplicate samples and unique routes per portal. Clear Paths or F3 cancels/removes everything. Isolated map-only F6 scenes without world services have no panel.

### Preview-only settings

- Enable Path Diversity
- Diversity Strength: 0–3
- Weighted Profiles: 1–16
- Reset to Game Settings

These controls edit a private EnemyMasterConfig copy. Each Simulate creates one detached private NavigationGrid pool (up to 16 weighted grids plus neutral) for the whole batch, never per path, portal or enemy. The pool reads authoritative WorldGrid solidity and a frozen copy of the current crowd snapshot, but it never changes live enemy weights/configuration or scans the enemy registry. Closing/resetting/clearing releases preview native grids explicitly; level exit also disposes the settings copy. No .cs/.tres files are saved, and no Apply-to-Game action exists. The legend prints tested values for manual transfer into normal configuration.

The normal game's profile limit also rises to 16; default remains 4. One sample uses only its selected profile. More raw samples can repeat profile routes. Per-type diversity/offset/corridor permissions still apply. Offset/corridor settings are copied from the game; this pass adds UI controls for Path Diversity only.

ALL uses **50 per portal**, not 50 spread across the map: five portals can show 250 paths. All jobs together share the existing debug default of one A* query / soft 1 ms per physics step. Single native queries and refinement are indivisible, so the time cap is soft. For five portals/four goals/50 samples, 1,020 queries require at least 1,020 physics steps (about 17 seconds at 60Hz), with results filling in as available. Nothing recalculates after completion. Inputs/map/settings changes discard stale output for explicit resimulation. Closing F3 stops preview processing.

## Verification

- Direct Roslyn C# compilation + Godot source generators: passed, no diagnostics. Standard MSBuild remains unavailable in this container due to the previously verified missing NuGet.Protocol dependency.
- `RoutePreviewChecks.tscn`: passed on both campaign levels. Real panel/button/key callbacks; Raw/Refined reproducibility; all eight type permissions; neutral baseline; unsnapped exits; invalid/no-goal handling; stale-input cancellation; duplicate reporting; 50 paths per portal; ALL; 16-profile grid ownership/selection; preview-only on/off/strength/count edits; reset; live-config identity/value/pool isolation; unchanged assignment counters and live queues; F3 close/Debug disable cleanup. Production AssignProfile is separately checked for two complete 0–15 cycles. ALL/50/16 also runs with 500 living enemies.
- `CorridorNavigationChecks.tscn`: exit 0, all existing geometry, MovementPermissionChecks and twelve 100/500 × straight/winding × 0/50/100% cases.
- `MenuTowerFlowChecks.tscn`: exit 0, Boot/Main/Campaign, tower build and paid upgrade flow.
- `PlacementPerformanceChecks.tscn`: exit 0 for 100/500.
- Unmodified `EnemyIntegrationChecks.tscn`: fails at the existing `Level002Checks` painted-tile-count assertion. This was independently reproduced on untouched `3470c61` during the first overlay pass; subsequent GitHub changes before this pass are planning docs only. No map or map-test assertions are changed here.
- Diagnostic run with only the `Level002Checks.Run()` invocation temporarily skipped: exit 0; all remaining combat, spatial, terrain, waves, ranks and navigation checks passed (final 31 integration checks, including SwarmNavigationChecks/RepathMovementChecks/SmoothRepathChecks). Original source call was restored immediately after diagnostic compilation and the final production binary rebuilt. This is not a clean unmodified full-suite pass.
- `git diff --check`: passed.

No manual graphical playtest is claimed. Expected existing missing-catalog fallback warnings remain.

## Recorded expanded-pass timings

Headless CPU totals, distributed over bounded preview steps, including private-grid setup and debug output. Other suites ran concurrently; not FPS or a controlled speedup measurement.

| Scenario | Samples | Queries | Total CPU work |
| --- | --- | --- | --- |
| Level001, ALL | 2 portals × 50 | 102 | 124.14 ms |
| Level002, ALL | 5 portals × 50 | 1,020 | 1,657.02 ms |
| Level001, ALL with 500 live enemies | 2 portals × 50 | 102 | 122.45 ms |
| Level002, ALL with 500 live enemies | 5 portals × 50 | 1,020 | 1,494.24 ms |

Live path queues/calculation counters remain unchanged during these runs. Weighted grid preparation happens on Simulate; no private grids are constructed per sample. Legend/draw caches update when a sample finishes, rather than after every goal query. Large batches still have noticeable total debug work and can overrun the soft limit on individual searches; they are deliberately spread across frames.
