# Headless gameplay checks

Build C# first, then run the Godot .NET executable:

```sh
godot --headless --path . TESTS/EnemyIntegrationChecks.tscn
```

Checks instantiate production Main/Level001 and test F2/repeat behavior, portal navigation with swept clearance, 100-ship path budget, First/Closest/range, configured bullet damage/cooldown/death, revision repathing, map-edge permissions, actual placement route rejection, and occupancy removal. Terrain changes are confined to the in-memory test instance and never saved over Level001. Manual physics ticks keep checks deterministic. These are correctness checks, not a performance benchmark.
