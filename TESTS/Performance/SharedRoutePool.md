# Shared route pool — verification 2026-10-20

Source base: `5990d6c`. Godot4.7.2 Mono/.NET8, headless execution container. `dotnet build --no-restore` passed with zero warnings/errors; headless import completed. These are controlled CPU/correctness fixtures, not the user's rendered Level100 FPS or exact desktop tower layout.

## Paired route-assignment benchmark

Same straight 41-point logical path, radius12, enabled corridor refinement, offset8, seeds0–299, 300 assignments per run. Pool reset for each batch; master off/on order alternates. On includes eight cold templates and292 hits. Per-ship geometry remains private and its construction is included.

| Run | Original per-ship builders ms | Shared pool ms |
|---|---:|---:|
| 1 | 681.497 | 52.820 |
| 2 | 632.228 | 52.556 |
| 3 | 617.679 | 41.083 |

Managed bytes per300 assignments:4,184,824 original versus3,115,912 shared. This measured route-assignment workload benefits substantially; actual waves have different origins/profiles/radii/permissions and may have lower hit rates. No proportional frame-rate gain is claimed. Enabling the cache permits repeated finite variations, so geometry is not expected to be pixel-identical to every original seed.

## Correctness and live simulation

`Godot --headless --path . TESTS/Performance/SharedRoutePoolChecks.tscn` — PASS:

- Native-vs-cached raw geometry/cost, private arrays, off switch, unrelated blocker preservation and affected blocker invalidation.
- Finished tail reuse at a jittered actual origin, safe connections/links, independent points/suffixes, radius and endpoint separation, bounded capacity.
- Twelve actual ships physically completed a required two-checkpoint chain.
- Production100-ship and500-ship swarms reused templates, then ran600 explicit60Hz movement/path ticks with a newly placed blocker. Immediate clearance, scheduler count limits and eventual settled repaths passed. Direct reassignment batches measured17.454ms /51.827ms respectively; this is total batch CPU cost, not a single normal gameplay frame or FPS. Hit counts144/754 include later repaths.

`MovementPermissionChecks` now compares pooled geometry against the defined finite seed/offset policy, retaining all48 mixed-type/global-master/percentage cases. CorridorNavigationChecks separately exercises exact legacy offset parity with the pool disabled only for that oracle, then restores shared routing. Its permission and geometry sections passed.

## Other suites and limitations

MovementRegistryChecks, ProjectileAccuracyChecks and StealthChecks passed after the initial cache integration. Final PerformanceOverlayChecks, SubsystemProfilerChecks, MovementRegistryChecks, ProjectileAccuracyChecks and StealthChecks reruns all passed with the final compiled implementation.

Broader fixtures remain limited by existing issues, reproduced on a separately rebuilt unchanged `5990d6c` worktree:

- MultiCitadelChecks: `Single-objective placement adds no comparisons` (no usable fixture placement).
- RoutePreviewChecks: `Runtime tower obstruction fixture`.
- CheckpointChecks: absent `Portals/EnemyPortal2`, then NullReferenceException before its checks. The new independent checkpoint-chain fixture passes.
- CorridorNavigationChecks: permissions/geometry pass, then later fixture creation fails loading DefaultTowerCatalog; Godot also reports a released GCHandle/finalizer-thread failure. The same failure reproduces unchanged. Its later historical population fixtures are not claimed passing; the new independent100/500 live safety checks replace neither the historical evidence nor its missing execution.
- EnemyIntegrationChecks still fails `No route-safe test cell` at TowerCatalogChecks.Cell, as previously reproduced before this change.

Remaining manual check: trajectory variety/smoothness and Level100 rendered frame behavior. Startup warms on actual requests rather than precomputing all portals/branches synchronously. Pool misses still build routes; ongoing per-ship movement, combat, spawning, effects and GPU costs remain.
