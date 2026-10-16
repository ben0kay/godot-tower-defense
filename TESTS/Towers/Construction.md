# Timed initial construction — C1/C2

Run `Godot --headless --path . res://TESTS/Towers/ConstructionChecks.tscn` after compiling C#.

## Covered contracts

- Independent complete level-row WorkSeconds copies and passive/no-level fallback; invalid negative/NaN/infinite durations rejected. Positive real build times; explicit zero-duration compatibility without an inactive frame.
- Actual active-only manager: exact30/60/120Hz boundary completion, bounded10Hz progress events, no duplicate completion, pause/resume and automatic ticks. Completion callbacks can remove a sibling, enqueue a new job or attempt a reentrant advance without skipped/early work. Queued/free jobs and manager/world unload cannot activate unfinished buildings.
- Actual Mission Results defeat pauses a partially completed job immediately.
- Both real battlefield launchers: paid Factory3×2, Depot2×2 and Battle1×1; all cells occupied/indexed/selectable immediately, Credits charged once, no rank/level/ammo/stat changes on completion. Existing Inspector runtime label shows CONSTRUCTING; upgrades reject unfinished sites and become available afterward.
- Explicit attempts to tick unfinished factories/weapons or register inactive endpoints cannot bypass work. Unfinished factories do not accumulate production time; seeded stocks do not create requests, reservations or shipments. Completion automatically restores the existing Factory→Depot→Tower loop,100-round/2s batches,1000-round factory trips and10000 Depot capacity.
- All six real offensive types (Battle/Laser/Blaster/Shockwave/Sniper/Machine Gun) produce no attacks or ammunition consumption while constructing and consume normally after completion. These stationary capability fixtures use Closest where relevant; path/First priority behavior is covered by existing navigation/targeting suites.
- All-PASSABLE versus all-IMPASSABLE foundations retain existing navigation distinction. Completion never changes grid revision. Free during construction releases every cell and retires work; invalid durations/failed placement do not charge. FootprintChecks additionally verifies malformed scenes, late partial-index rollback, mixed foundations, reservations and insufficient funds; its rollback assertion now includes active-job count.
- Existing TowerUpgradeChecks runs unchanged in the new runner: actual HUD purchase, costs, multi-resource failures, expected-level guards, cached preview parity, independent rows and rank-compatible progression. Upgrades still apply instantly after initial construction; timed upgrades remain C3.

Older subsystem tests explicitly advance initial construction through `ConstructionTestClock` or AmmoLoopFixture.Purchase before testing completed-building behavior. New construction checks purchase with `completeConstruction:false` and assert the real unfinished interval. No production timer bypass, global test flag or disabled gameplay assertion was added. A MenuTowerFlow test-only freed-fixture finalization barrier avoids the host's intermittent Mono/native shutdown race; it does not add GC to gameplay.

## Actual execution — 2026-10-16

Godot4.7.2 Mono, .NET8.0.31/Roslyn4.11:

- All production/test C# compiled successfully with matching Godot source generation/reference assemblies, no diagnostics.
- Standard `dotnet build --no-restore` failed before compilation: host SDK could not load `NuGet.Protocol, Version=6.11.2.1`. This is not reported as a successful SDK build.
- ConstructionChecks (including existing TowerUpgradeChecks), FootprintChecks, AmmunitionLoopChecks, GenericEnemyChecks, DeathSpawnChecks, MenuTowerFlowChecks, HudCategoryChecks, PauseControlsChecks, ResultsChecks and CorridorNavigationChecks: exit0.
- GenericEnemyChecks covers catalog/shields, ShieldEmitter, waves/portals/Citadel, area damage, Laser30/60/120Hz, Blaster instantaneous impacts and Shockwave swept gameplay. DeathSpawn covers both actual launchers, wave/results ownership, checkpoint handoffs and500-active stress.
- Full unmodified EnemyIntegrationChecks still fails the preexisting Level002 map assertion: “The western open ground and eastern maze are fully painted”, followed by native shutdown exit139. No map data/assertions were modified to bypass it.
- An earlier MenuTowerFlow run printed PASS but exited139 during shutdown; not counted as a pass. The final run with the test-only finalization barrier exited0.
- No desktop visual playtest performed. Current finished SVG artwork remains visible while construction runs; C3 progress UI and C4 authored SVG stages/sparks are deliberately not implemented.

## Performance observations

Actual manager benchmark:500 idle passive buildings plus100 active10s jobs,600 explicit60Hz ticks. Final observed mean **0.0209ms/tick**, peak **0.1546ms**; exactly100 active jobs visited per tick, zero idle buildings visited. All100 finish on the same boundary, then the manager disables physics processing and visits0 jobs. World unload with another100 pending jobs completes safely. Buffers/membership are reused rather than allocating per-tick list snapshots.

Separate existing500-enemy3×2 placement benchmark: **0.572ms mean** across20 validations, one flood/click; completion adds no navigation work. These are host-specific headless CPU observations, not rendered FPS guarantees. Existing corridor benchmarks,500-enemy combat and carrier stress also passed.
