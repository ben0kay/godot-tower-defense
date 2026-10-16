# Navigation placement CPU measurements

Measured before/after this change using the same `PlacementPerformanceChecks.tscn` fixture on the execution machine: open in-memory terrain, four weighted profiles, 100/500 existing ships and five tower placements. Values are maximum milliseconds per stage, not averages or combined rendered frame times. Before used the repository's original full profile refresh and global revision repathing; after uses incremental updates, selective invalidation and the new default offsets/audits.

| Population | Stage | Before (ms) | After (ms) |
| --- | --- | ---: | ---: |
| 100 | Placement connectivity/clearance validation | 14.29 | 1.55 |
| 100 | Occupancy event + navigation synchronization | 3.35 | 0.88 |
| 100 | Following manually simulated physics frame | 1.02 | 2.93 |
| 500 | Placement connectivity/clearance validation | 14.88 | 0.74 |
| 500 | Occupancy event + navigation synchronization | 3.28 | 0.06 |
| 500 | Following manually simulated physics frame | 0.77 | 1.05 |

The main placement costs dropped substantially. The following-frame work now includes movement refinement and staggered audits, so that stage is not uniformly faster. Cold/JIT costs particularly affect the first 100-ship samples. Do not add the maximum stage values and describe the sum as one measured frame: the maxima can come from different samples.

The separate 500-ship correctness fixture measured weighted path-work frames at p95 2.62ms / maximum 3.80ms and movement plus extra test clearance assertions at maximum 3.83ms. Path time limits are soft: one native search and its route refinement/index registration may overrun the configured 2ms, then work yields. Count limits remain strict. Validation has its own count/time limits.

The selective-repath test gives 500 ships the same route, blocks one cell, and drains validity/repath work across many frames with deliberately tiny count budgets. It asserts that an unaffected ship keeps moving, affected replacement routes avoid the cell, and no ship enters a blocker while waiting. It also checks valid audits, no-op removal, offset tapering, urgency/fairness, duplicate suppression and tiny-radius diagonal safety.

These are headless CPU measurements, not a rendered 60 FPS benchmark or a guarantee for the user's hardware. Synchronous creation of 500 nodes can still spike (about 32ms in the final batch test); a future wave system should spread creation across frames. No pooling, waves or rendering changes are introduced here.

Run after a normal Godot C# build:

```sh
godot --headless --path . TESTS/Performance/PlacementPerformanceChecks.tscn
godot --headless --path . TESTS/Integration/EnemyIntegrationChecks.tscn
```

Run timing fixtures separately from other heavy tests for a meaningful comparison. Timing diagnostics in Level001's Debug HUD show placement validation, last grid update, native A* queries/time, total path work and validation work. Lifetime path/validation peaks remain visible between the HUD's throttled refreshes; restart the playtest to reset them.
