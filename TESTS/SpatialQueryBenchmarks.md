# Shared enemy spatial queries — measured results (2026-10-15)

Same-machine paired baselines against main `e9c9c07`, using its original targeting/instant-blast scans and test-only `BruteForceShockwave`. Production uses one registry-owned spatial index for all three consumers. Timings below come from the final implementation, not estimates.

## Reproduce and interpret

Compile the project, then run:

```sh
godot --headless --path . TESTS/SpatialQueryBenchmarks.tscn
```

Godot4.7.2 Mono, .NET8.0.31, direct Roslyn4.11 compile with the Godot source generator/API, same container and compiler options for both paths. Each pair rebuilds the same deterministic positions, routes, centers and inputs. Sparse enemies span4096×4096px, dense enemies span128×128px. Cell size128px; circle ranges256px for targeting and120px for impacts. Every living enemy moves0.75/0.25px per simulated tick. Original fixtures disable spatial transform notifications; indexed fixtures include point/motion maintenance. Production navigation/physics ticks and cosmetic death explosions are disabled during this isolated benchmark.

Each tick forces50/100/200 mixed First/Closest acquisitions, eight instant impacts and up to eight independent200px/250px/s fronts. Waves are replaced every50ticks, with7.73 active on average. Their initialization, movement maintenance, query, crossing, snapshot and dispatch work is included. Actual towers retain their staggered0.15s searches; this intentionally measures an acquisition burst rather than pretending every real tower searches every frame. Payload amount0 isolates delivery/selection cost without killing the fixture; real nonzero defense/XP/health behavior is covered separately by regression tests.

There are60 warmup ticks then120 measured ticks per side. Allocations use GC.GetAllocatedBytesForCurrentThread; results include amortized new-wave/history costs. Candidates count enemies examined after cell pruning (before exact radius/crossing), summed across all calls in a tick; baseline counts the corresponding full registry examinations. Swept entries are deduplicated per wave query. Results are one paired run, subject to JIT/GC/scheduling noise; no statistical confidence interval or rendered FPS claim is made.

## Total simulation work

All times are milliseconds; movement and index maintenance are included in total.

| Enemies | Acquisition calls | Layout | Path | Average | p95 | Maximum | Candidates/tick | Managed bytes/tick |
| ---: | ---: | --- | --- | ---: | ---: | ---: | ---: | ---: |
| 500 | 50 | sparse | scan | 4.297 | 5.984 | 8.110 | 32,867 | 6,625 |
| 500 | 50 | sparse | index | 1.580 | 2.232 | 3.477 | 573 | 577 |
| 500 | 100 | sparse | scan | 5.859 | 8.154 | 9.094 | 57,867 | 8,625 |
| 500 | 100 | sparse | index | 1.506 | 2.044 | 2.861 | 1,114 | 577 |
| 500 | 200 | sparse | scan | 8.781 | 10.188 | 12.813 | 107,867 | 12,625 |
| 500 | 200 | sparse | index | 1.614 | 1.879 | 2.730 | 2,159 | 577 |
| 1000 | 50 | sparse | scan | 8.462 | 9.819 | 16.515 | 65,733 | 9,181 |
| 1000 | 50 | sparse | index | 2.468 | 3.208 | 5.504 | 1,133 | 809 |
| 1000 | 100 | sparse | scan | 11.323 | 12.348 | 14.773 | 115,733 | 11,181 |
| 1000 | 100 | sparse | index | 2.497 | 2.832 | 3.624 | 2,211 | 740 |
| 1000 | 200 | sparse | scan | 19.895 | 29.050 | 40.151 | 215,733 | 15,181 |
| 1000 | 200 | sparse | index | 2.919 | 3.500 | 4.048 | 4,337 | 697 |
| 500 | 50 | dense | scan | 5.245 | 8.514 | 11.516 | 32,867 | 339,180 |
| 500 | 50 | dense | index | 4.366 | 5.682 | 6.259 | 31,657 | 5,799 |
| 500 | 100 | dense | scan | 6.662 | 8.039 | 15.010 | 57,867 | 341,155 |
| 500 | 100 | dense | index | 5.628 | 6.985 | 10.329 | 56,657 | 5,765 |
| 500 | 200 | dense | scan | 9.659 | 11.114 | 15.260 | 107,867 | 345,157 |
| 500 | 200 | dense | index | 8.260 | 10.611 | 15.432 | 106,657 | 5,765 |
| 1000 | 50 | dense | scan | 10.241 | 12.523 | 16.043 | 65,733 | 679,592 |
| 1000 | 50 | dense | index | 9.925 | 12.120 | 17.921 | 63,321 | 11,781 |
| 1000 | 100 | dense | scan | 14.641 | 17.147 | 20.269 | 115,733 | 681,592 |
| 1000 | 100 | dense | index | 12.754 | 17.914 | 22.418 | 113,321 | 11,644 |
| 1000 | 200 | dense | scan | 20.736 | 25.013 | 29.203 | 215,733 | 685,592 |
| 1000 | 200 | dense | index | 17.889 | 24.623 | 29.948 | 213,321 | 11,644 |

## Component averages

Maintenance includes actual position changes and periodic wave initialization. Wave time includes candidate query, history retirement, precise solver and zero-amount dispatch.

| Enemies | Calls | Layout | Path | Maintenance ms | Target ms | Eight blasts ms | Wavefronts ms |
| ---: | ---: | --- | --- | ---: | ---: | ---: | ---: |
| 500 | 50 | sparse | scan | 0.205 | 1.663 | 0.680 | 1.750 |
| 500 | 50 | sparse | index | 1.128 | 0.207 | 0.028 | 0.216 |
| 500 | 100 | sparse | scan | 0.208 | 3.304 | 0.695 | 1.652 |
| 500 | 100 | sparse | index | 1.019 | 0.333 | 0.018 | 0.136 |
| 500 | 200 | sparse | scan | 0.244 | 6.309 | 0.681 | 1.547 |
| 500 | 200 | sparse | index | 0.944 | 0.539 | 0.014 | 0.117 |
| 1000 | 50 | sparse | scan | 0.454 | 3.250 | 1.377 | 3.380 |
| 1000 | 50 | sparse | index | 1.998 | 0.199 | 0.026 | 0.245 |
| 1000 | 100 | sparse | scan | 0.535 | 6.344 | 1.298 | 3.147 |
| 1000 | 100 | sparse | index | 1.890 | 0.361 | 0.023 | 0.225 |
| 1000 | 200 | sparse | scan | 0.841 | 13.815 | 1.562 | 3.677 |
| 1000 | 200 | sparse | index | 1.947 | 0.712 | 0.023 | 0.237 |
| 500 | 50 | dense | scan | 0.296 | 1.841 | 1.699 | 1.408 |
| 500 | 50 | dense | index | 1.017 | 1.216 | 1.592 | 0.541 |
| 500 | 100 | dense | scan | 0.262 | 3.456 | 1.683 | 1.261 |
| 500 | 100 | dense | index | 1.086 | 2.429 | 1.583 | 0.530 |
| 500 | 200 | dense | scan | 0.309 | 6.585 | 1.539 | 1.226 |
| 500 | 200 | dense | index | 1.167 | 4.835 | 1.704 | 0.554 |
| 1000 | 50 | dense | scan | 0.623 | 3.554 | 3.253 | 2.812 |
| 1000 | 50 | dense | index | 2.556 | 2.527 | 3.542 | 1.301 |
| 1000 | 100 | dense | scan | 0.841 | 7.402 | 3.443 | 2.955 |
| 1000 | 100 | dense | index | 2.635 | 5.182 | 3.644 | 1.292 |
| 1000 | 200 | dense | scan | 0.785 | 13.803 | 3.256 | 2.892 |
| 1000 | 200 | dense | index | 2.916 | 9.878 | 3.755 | 1.340 |

Sparse mean totals improve **2.72–6.82×**; dense means improve **1.03–1.20×** in this run. For1,000 enemies/200 acquisitions in sparse placement, mean **19.895→2.919ms**, p95 **29.050→3.500ms**; candidates **215,733→4,337** (98.0% fewer). Index maintenance is a real extra cost (**0.841→1.947ms**) and is included in the net gain. Dense counts barely shrink because almost everyone is nearby; direct cached-position scoring, scratch reuse and cheaper wavefront queries supply the smaller net benefit. Dense instant blasts can be individually slower because recipient ordering/snapshots remain necessary. Some dense p95/max samples regress despite better means; the table reports these rather than claiming universally faster tails.

## Pathological teleport trade-off

A separate stress case moves every enemy2,000,000px across the origin each tick, with eight independent sample cursors and the original solver. All entries exceed the64-cell swept-storage threshold and conservatively enter the overflow set. It runs20 warmup+80 measured ticks. Every crossing is asserted, including both endpoints outside the front. This deliberately measures selection/motion/solver, without damage dispatch, and is not directly comparable to the acquisition-burst table.

| Enemies | Path | Mean ms | p95 ms | Max ms | Candidates/tick |
| ---: | --- | ---: | ---: | ---: | ---: |
| 500 | scan | 0.706 | 0.841 | 6.684 | 4,000 |
| 500 | index | 2.587 | 3.151 | 4.278 | 4,000 |
| 1000 | scan | 1.252 | 1.427 | 1.598 | 8,000 |
| 1000 | index | 5.852 | 6.753 | 9.735 | 8,000 |

This case is slower with the index: there is no candidate reduction, and motion bookkeeping is additional work. It preserves correctness without imposing an invented maximum speed or allocating an enormous grid. Ordinary movement/sparse attacks benefit; arbitrarily large teleports and long-lived unsampled cursors conservatively retain more candidates/history. That limit is explicit rather than hidden by skipping fast enemies.

## Correctness and build results

- Direct C# compilation of all project sources with Roslyn4.11, Godot source generation and GodotSharp references: passed, no compiler diagnostics.
- EnemyIntegrationChecks: passed, including SpatialQueryChecks and existing shields, damage types, areas, projectiles, beams, wavefronts, ranks/XP, prices/previews, waves, economy, spawning, movement and navigation. The focused checks cover0/1/500/1,000 circles, invalid/extreme input, negative/cell/range boundaries, stable priorities, stale routes, direct movement, external frees, isolated-world unload, sparse blast defense parity, nested snapshot dispatch and original-vs-indexed moving fronts30/60/120Hz.
- MenuTowerFlowChecks: passed (Boot→Main→Campaign→Level001, build/cancel and paid upgrades).
- CorridorNavigationChecks: passed (straight/winding geometry, live repaths,0/50/100% variation,100/500 enemies).
- PlacementPerformanceChecks: exited0 with100/500-enemy validation/grid/next-physics measurements.
- SpatialQueryBenchmarks: exited0; all12 paired normal configurations and500/1,000-enemy overflow correctness assertions passed.
- Standard dotnet build could not start: this container's incomplete SDK lacks NuGet.Protocol6.11.2.1. Godot editor import additionally reports missing SDK/MSBuild components. This is an environment limitation; the direct compiled assembly was used successfully by all actual headless runtime suites. A clean SDK/MSBuild editor build is not claimed.

Level rows, balances, targeting schedules, navigation algorithms and visual delivery were not changed. No rendered profiling was performed, so rendering bottlenecks or rendered60FPS cannot be inferred; actual sprite/material/particle costs still require a graphical profiling session.
