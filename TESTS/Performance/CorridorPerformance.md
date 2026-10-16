# N4 regression and CPU measurements

Measured on 2026-10-14 in the execution container using Godot4.7.2 Mono, .NET8.0.31, a direct Roslyn/source-generator build and real headless scene runs. Runs were sequential. No rendered GPU/frame pacing or hardware60FPS claim is made. Cold/JIT/GC/scheduler spikes remain included.

## Commands and outcome

- `Godot --headless --path . TESTS/Integration/EnemyIntegrationChecks.tscn`: passed, including catalog/abilities, waves/shields/rewards, tower gameplay/stat/upgrades, SwarmNavigationChecks, RepathMovementChecks and SmoothRepathChecks.
- `Godot --headless --path . TESTS/Navigation/CorridorNavigationChecks.tscn`: passed all12 straight/winding ×100/500 ×0/50/100% cases, plus focused correctness.
- `Godot --headless --path . TESTS/Performance/PlacementPerformanceChecks.tscn`: passed. Validation/grid/next-physics maximums were1.64/0.54/1.57ms at100 ships and0.73/0.07/1.47ms at500.

Existing authored SVG UID warnings resolve through text paths; no engine errors occurred. Standard editor SDK/MSBuild was unavailable here.

## N4 fixture

The fixture duplicates config/level Resources and carves runtime-only terrain. Weighted diversity, congestion and tiny offsets are disabled for timing isolation; normal gameplay defaults are unchanged. The path manager uses8 calculations/frame and its existing2ms soft budget. Each assignment includes N4 refinement before the manager checks elapsed time again; one noninterruptible request can exceed that time budget.

Build time measures a direct accepted-route rebuild per ship. Path-frame time measures the centralized manager while draining actual spawned requests. Movement time measures a normal manual60Hz physics frame for the full population; audits are staggered, and no unchanged-route A* requests are observed in that interval. The dynamic-block phase then adds a tower cell, checks radius safety on every moving tick, enforces request limits, and verifies every ship safely passes the obstruction without permanent waiting.

| Corridor | Ships | Selection | Selected | Build mean/max ms | Path frame p95/max ms | Movement frame p95/max ms |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| straight | 100 | 0% | 0 | 0.031/0.262 | 1.61/1.61 | 0.23/0.55 |
| straight | 100 | 50% | 44 | 0.762/2.067 | 3.92/4.16 | 0.29/0.55 |
| straight | 100 | 100% | 100 | 2.034/4.328 | 4.08/4.78 | 0.34/0.49 |
| straight | 500 | 0% | 0 | 0.068/20.436 | 0.81/0.97 | 1.27/1.83 |
| straight | 500 | 50% | 257 | 0.985/19.400 | 4.06/4.78 | 1.34/2.02 |
| straight | 500 | 100% | 500 | 1.868/4.614 | 4.04/13.22 | 1.43/2.08 |
| winding | 100 | 0% | 0 | 0.034/0.198 | 8.02/8.02 | 0.27/1.40 |
| winding | 100 | 50% | 44 | 0.902/2.406 | 2.59/4.12 | 0.35/0.49 |
| winding | 100 | 100% | 100 | 2.032/2.617 | 4.29/8.29 | 0.28/0.42 |
| winding | 500 | 0% | 0 | 0.031/0.115 | 0.78/0.83 | 1.05/1.57 |
| winding | 500 | 50% | 257 | 1.008/2.668 | 3.98/4.41 | 1.01/1.70 |
| winding | 500 | 100% | 500 | 2.061/6.016 | 4.23/13.18 | 1.17/1.72 |

N4 adds roughly1.9–2.1ms per selected route build in these maps; at50% selection the mean across all ships is lower. Ordinary500-ship movement maximums were around1.6–2.1ms here. Path-frame outliers reached13.2ms with100% selection. The unselected direct-build batch also had a20.4ms outlier, so this is a measured sample, not a bound. More selected ships increase queued assignment latency; retain the master-off default until visual/hardware playtesting.

## Correctness evidence and remaining visual check

The deterministic10,000-spawn50% sample selected5,018 ships. Independent96-seed three-cell geometry produced42 upper/25 lower/29 center choices, with50.6px peak lateral displacement. Endpoints and forward progress remain bounded; narrow paths fall back; physical suffix distance and First ordering use the actual route. Explicit grazing-corner coverage reproduces a case missed by legacy samples and verifies the strict N4 connection rejects it and the ship progresses after handoff.

Disabled/unselected builds retain exact baseline waypoints and zero corridor sampling. Toggle/percentage lifetime behavior and forced-repath seed/geometry stability are covered. Shared A* weights/profile architecture are unchanged.

A native moving-window visual check was unavailable (no display server). In Godot, enable Corridor Path Variation in EnemyConfig, compare0/50/100% in a three-cell straight passage and several turns, and place towers near selected ships. Inspect lateral spread, smooth turning and forward handoff. Test on target hardware with rendering enabled before treating500 ships at60FPS as achieved.
