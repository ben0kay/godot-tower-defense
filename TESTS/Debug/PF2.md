# PF2 verification — 2026-10-20

Source base: `015ed7d` (unchanged baseline rebuilt separately). Godot4.7.2 Mono and .NET8 on the execution container. These are headless correctness/diagnostic runs, not the user's desktop Level100 workload.

## Commands and results

Build: `dotnet build --no-restore` — PASS, zero warnings/errors. Godot headless editor import completed.

Run each scene with `Godot --headless --path . TESTS/<scene>.tscn`:

| Scene | Result |
|---|---|
| Debug/SubsystemProfilerChecks | PASS: per-tick aggregation, idle-zero inclusion, rolling eviction, selected-tick stride, nested inclusive timing, foreign-world rejection, pause/stop/unload, default-off and warmed allocation checks; actual registry/targeting/clearance/projectile hooks |
| Debug/PerformanceOverlayChecks | PASS on Main/Level002/Level020: retired Info absent, F2/F3 retained, default-off, Shift+F4 capture, early/late physics samples, expanded-panel resize and click-through, paused hide/reopen freshness, F8 report actually saved |
| Performance/MovementRegistryChecks | PASS: actual movement, synchronous spatial/target queries, live reveal, swept history, ancestor transforms, arrival/free and paired callback diagnostic |
| Towers/ProjectileAccuracyChecks | PASS |
| Towers/TowerConditionsChecks | PASS (weapons/heat/disable/pause) |
| Stealth/StealthChecks | PASS |
| EnemyWeapons/GunnerDroneChecks | FAIL: existing `Dedicated small red tracer and tiny warm muzzle flash share the real launch origin` assertion, Firefight line111 |
| Integration/Level100StressChecks | FAIL: `Template terrain stays entirely blank for the user's painting`, line60 |
| Integration/EnemyIntegrationChecks | FAIL: `No route-safe test cell`, TowerCatalogChecks.Cell line94 |

All three failures reproduce with the same assertions/stack locations on a separately rebuilt unchanged `015ed7d` worktree. No map content, gunner art/cadence or unrelated failing fixtures were changed to force a passing suite.

## Observer measurements and limitations

The new fixture compares 10,000 empty timing scopes after warming the owner cache. First run: OFF0.564ms / ON2.452ms. Final run: OFF1.249ms / ON2.192ms; zero managed bytes for both disabled and warmed enabled loops. These tiny synthetic timings include scheduler/JIT noise; they establish that hooks work and avoid per-call managed allocations after warm-up. They do **not** establish full-game overhead, rendered FPS, an optimization gain or the cause of the user's spike. First-seen nodes allocate one weak owner binding per capture; expired nodes are not strongly retained.

The panel's observer metric covers partial entry/filter/record bookkeeping. Timer reads, nested instrumentation, report/UI refresh, engine callback and other sampling costs are not fully covered. Compare matched desktop OFF/ON runs and record stride/build/settings.

PF1 paired diagnostic (same headless run; 10,000 writes): notification-only callbacks10,000 versus old-extra-call emulation20,000 on each of3 runs. Elapsed pairs in run order:20.368/74.664ms,54.939/95.099ms,82.469/256.118ms (notification-only/extra). These are synthetic setter/subscriber work, not a desktop wave/FPS benchmark.

Remaining: actual Level100 movement-only/combat/spawn-burst captures, matched OFF/ON observer overhead, target-machine CPU/GPU measurements and manual panel readability. Use DEBUG/README.md controls. PF3 bottleneck selection and PF4 optimization remain future work. No performance cause or FPS improvement is claimed here.
