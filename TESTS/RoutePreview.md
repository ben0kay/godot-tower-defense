# F3 route preview verification

Implemented/tested 2026-10-15 on GitHub base `3470c61` using Godot 4.7.2 Mono and .NET 8. No checkpoint routing is included. Rebased before push onto `e1c5500` (planning-document changes only; runtime source unchanged).

## Try it

Launch a campaign level normally, or F6 Main/MainLevel002. Press **F3**, choose **Portal**, **Enemy type**, **Path samples (1–12)** and **Final Refined / Raw A***. Click **Simulate Paths**. Toggle the neutral comparison, click **Clear Paths**, or press F3 again to hide/cancel everything. Changes to controls or relevant map/settings clear old results; press Simulate again. No enemy ships are spawned by the tool. F6 on an isolated map without the live world services does not offer this panel.

White = neutral shortest unrefined routing. Colors = deterministic preview sample routes. Legend reports profile, physical distance, distance overhead and duplicates. Samples may overlap. Spawn jitter, enemy timing and future congestion are not simulated; current congestion is a snapshot. A mid-job snapshot change cancels that job rather than mixing costs. Invalid clearance/unreachable routes are reported without unsafe drawn connectors.

## Actual results

- Direct Roslyn C# compilation with Godot source generators: passed, no diagnostics. Final production sources include the original, unchanged Level002Checks call.
- `RoutePreviewChecks.tscn`: exit 0 on final code. Uses real F3 handler, real controls/button callbacks and both Main/Level001 and MainLevel002/Level002 services. Covers hidden defaults; opening/closing; all registered enemy choices; 12-sample raw/refined repeatability; neutral comparison; duplicate labels; all eight type permission combinations in both modes; one-query debug budget; mid-job input/settings/navigation cancellation; no-goal/invalid-start failure; Clear; ignored key repeat; disabling Debug; unsnapped SpawnExit connectors; and unchanged live assignment ordinals, registry, queue/count state. Real preview jobs are also run with 500 living ships.
- `CorridorNavigationChecks.tscn`: exit 0, including MovementPermissionChecks, existing corridor geometry/repath assertions and all twelve straight/winding, 100/500-enemy, 0/50/100% cases.
- `MenuTowerFlowChecks.tscn`: exit 0, real Boot/Main/Campaign/Level001 button flow and placement/upgrades.
- `PlacementPerformanceChecks.tscn`: exit 0 for 100 and 500 enemies.
- Full unmodified `EnemyIntegrationChecks.tscn`: **fails** at `Level002Checks`' pre-existing assertion "The western open ground and eastern maze are fully painted". Reproduced the identical failure on a separate untouched worktree at GitHub base `3470c61`; the overlay does not change Level002 tiles or that test.
- Diagnostic integration run with only the `Level002Checks.Run()` invocation temporarily skipped: exit 0, all remaining combat/terrain/spatial/wave/rank/navigation checks passed (final 31 integration checks), including CorridorAssignmentChecks, SwarmNavigationChecks, RepathMovementChecks and SmoothRepathChecks. The skipped call was restored immediately after compiling the diagnostic binary and is **not** omitted in committed source. This diagnostic run is not claimed as a clean full-suite pass.
- `git diff --check`: passed.

Normal `dotnet build --no-restore` is blocked by this container's missing `NuGet.Protocol, Version=6.11.2.1` dependency before project compilation. Direct C# compilation and actual Godot .NET headless execution succeeded. No manual graphical playtest is claimed. Expected existing missing-catalog fallback warnings remain.

## Performance and isolation

Final preview test, with 500 living enemies (total work distributed across bounded steps):

| Level | Twelve samples plus neutral | Total preview work |
| --- | --- | --- |
| Level001 | 13 A* goal queries | 4.16 ms |
| Level002 | 52 A* goal queries, four Citadel goals | 72.53 ms |

Defaults: one query and soft 1 ms per preview physics step, separate from live gameplay requests. An individual native A* plus safety/refinement step cannot be interrupted and can exceed the soft time cap. Results are cached, with zero path recomputation per frame when finished and no preview processing when closed. Timings include safety/refinement; these are headless CPU measurements, not rendered FPS or a before/after optimization claim.

Preview never calls AssignProfile, AssignCorridorSeed, Spawn, Enemy.Initialize or the live path queues. Catalog definitions are read through packed-scene state without instantiating enemy scenes. Preview seeds/offsets are local deterministic samples; weights/solidity and refinement algorithms are the production ones. Shared profile crowd-cache synchronization is deterministic, and FindPreviewPath restores the live native-A* timing diagnostic. Live routes, Credits, XP and wave RNG are not modified.
