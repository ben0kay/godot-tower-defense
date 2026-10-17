# Enemy ability regressions

`DeathSpawnChecks.tscn` is the standalone E3A runner, grouped into lifecycle/results, safety, route inheritance and timing/stress checks. It uses DeathSpawnFixture's private settings/maps with real Enemy/SpawnManager/WaveManager/LevelSessionController. See [DeathSpawn.md](DeathSpawn.md) for commands, actual results and limitations. Existing mixed-enemy/ShieldEmitter integration checks stay under Integration/. No production scenes launch these test runners.

`GenericEnemyChecks.tscn` verifies all registered inherited scenes, shared-default propagation, definitions/artwork, exact paths/node counts and catalog-driven movement, then reuses catalog, shields, ShieldEmitter, waves/portal, Citadel, area and weapon regression helpers. Optional pre-migration scene fixtures enable 100/500-instance construction benchmarks. See [GenericEnemy.md](GenericEnemy.md).


`TESTS/Integration/EnemyCatalogChecks.cs` now also verifies Big mk.1 (Enemy005): original-inspired balance, real catalog and portal spawn, 96×72 art, no abilities, extra shared DefenseBar clearance, unshielded debug state, optional 200-point wave shields, shield-only hits and normal 10-Credit kill. Godot runtime verification remains to be run.
