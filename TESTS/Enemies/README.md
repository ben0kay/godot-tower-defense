# Enemy ability regressions

`DeathSpawnChecks.tscn` is the standalone E3A runner, grouped into lifecycle/results, safety, route inheritance and timing/stress checks. It uses DeathSpawnFixture's private settings/maps with real Enemy/SpawnManager/WaveManager/LevelSessionController. See [DeathSpawn.md](DeathSpawn.md) for commands, actual results and limitations. Existing mixed-enemy/ShieldEmitter integration checks stay under Integration/. No production scenes launch these test runners.
