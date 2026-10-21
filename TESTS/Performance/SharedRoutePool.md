## Cache-pressure regression — 2026-10-20

Reproduced on unchanged 6aab989 production code using the new pressure harness: 600 different active segments, shared capacity512, fixed open terrain, no placements,120 ticks (72,000 movement checks). After warming:10,680 extra builds and61,320 hits,36.137ms. The new assertion fails on that baseline as expected. With the correction:0 additional builds,0 shared dictionary consultations,512 shared entries,12.931ms. The active result adds constant per-route storage and survives global entry eviction. These synthetic times are not overall FPS measurements.

Build passed with zero warnings/errors. SharedRoutePoolChecks passed, including least-recently-used eviction ordering, active-result invalidation by new blockers, uncertain-result retention with original safe-prefix sweeps, removal, off switch, existing selective invalidation/size/bounds/terrain checks, two required checkpoints for12 ships, and100/500-ship dynamic-blocker runs for600 ticks. Existing prior baseline-suite caveats below remain applicable; this is not a complete-suite pass claim.

The 100,000-check warmed straight-segment benchmark measured90.267ms swept versus13.451ms cached on this run. It now includes the local memo path. F5 eviction counters help distinguish normal initial warming from repeat rebuilding. No combat or effects optimization was bundled into this fix.

---

## Movement safety extension — 2026-10-20

Build passed with zero warnings/errors. SharedRoutePoolChecks passed with safety caching enabled: shared certificates, exact-radius separation, immediate affected-block invalidation, unrelated-block retention, off-route swept fallback, blocker removal, storage bound, full terrain reset, and actual placement/removal on a solid buildable tile producing no navigation revision. Existing twelve-ship/two-checkpoint test and 100/500 live-ship runs (600 ticks after obstruction) passed. MovementRegistryChecks, ProjectileAccuracyChecks and PerformanceOverlayChecks also passed. Fixture warnings about the missing secondary portal SpawnExit and catalog fallback remain; no new broader-suite pass claim supersedes the baseline failures documented below.

Isolated warmed 100,000 movement checks: original sweeps 85.379ms versus cached checks 31.891ms on this host. Earlier run: 78.181ms versus30.399ms. These measure a straight open segment, include segment-membership checks/cache lookup, and exclude cold certificate construction. They are not an overall FPS comparison. Conservative certificates use expanded blocked-cell rectangles (including a small numeric margin); uncertain segments keep the original sampled sweep and may see less benefit. Original corner rules remain separate and are memoized per route/navigation revision. Actual route templates/progress and firing rates are unchanged.

The new helper is world-owned, bounded, shared across waves, and warmed on demand on first traversal. No startup bake or extra per-frame enemy scan was added. Navigation events selectively invalidate certificates synchronously before further movement, while existing route validation/replacement work stays queued and budgeted. F5 adds cumulative certificate counters; a cache hit can include an uncertain result and does not by itself prove a sweep was skipped.

---

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
