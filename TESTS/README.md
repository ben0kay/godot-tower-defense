# Headless gameplay checks

Build C# first, then run the Godot .NET executable:

```sh
godot --headless --path . TESTS/EnemyIntegrationChecks.tscn
```

Checks instantiate production Main/Level001 and test F2/repeat behavior, portal navigation with swept clearance, 100-ship path budget, First/Closest/range, configured bullet damage/cooldown/death, selective revision validation, map-edge permissions, actual placement route rejection, and occupancy removal. Terrain changes are confined to the in-memory test instance and never saved over Level001. Manual physics ticks keep checks deterministic. These are correctness checks, not a performance benchmark.

The same scene also runs SwarmNavigationChecks: default profiles must use both corridors, routes must have radius clearance, repeated profile searches must be deterministic, disable/zero-strength must restore ordinary paths, and optional congestion must prefer an empty corridor and clear without weight leakage. A 500-ship fixture prints headless spawn/path/movement timings and checks budgets/repath safety; it does not assert hardware-independent 60 FPS.

## Repath/offset regression checks and placement timings

The integration scene now invokes RepathMovementChecks: one tower must produce one incremental update and zero full rebuilds; an unaffected ship must keep moving; 500 affected ships must validate/repath over many count-bounded frames without entering the new blocker. Removal and valid periodic audits must do zero A*. Offsets must vary per ship, remain at most 8px, pass swept clearance and taper in a tight corridor. Tiny soft time budgets must yield after one search; urgent and normal requests must complete without duplication/starvation.

For the dedicated reproducible CPU fixture:

```sh
godot --headless --path . TESTS/PlacementPerformanceChecks.tscn
```

It creates open in-memory terrain, measures five placements with 100 and 500 ships, and reports maximum placement-validation, grid-update and following physics-frame times. It does not save designer terrain or benchmark rendering. See NavigationPerformance.md for recorded before/after values and limitations.

## Defense bar regression checks

The integration fixture now also asserts that Enemy001 has a hidden full-health bar, a damaging hit reveals the correct fractional fill, rotation stays counter-aligned, max-health scaling grows to but not beyond its width cap, and full/zero health hide the component. It doesn't replace visual inspection in F5 for pixel alignment and scale.
