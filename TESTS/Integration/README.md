## Level100 stress mission checks — 2026-10-20 (authored, execution pending)

`Level100StressChecks.tscn` checks the new unpainted Level100's scene/config/catalog and composes the actual five wave recipes at deterministic seed to verify scheduled totals **100/200/300/400/500** and exactly **20 Gunner Drones** in each, including the 1,500-ship campaign total. Also rerun `MissionCatalogChecks.tscn` after adding the fourth registered mission. Actual spawn/navigation benchmarks require painting passable ground first; no headless or desktop runs are claimed for this new pass. See `LEVELS/Level100/README.md`.

---

# Integration tests

The main EnemyIntegrationChecks.tscn runner and level/enemy/wave/portal/Citadel checks it orchestrates. It intentionally uses production scenes and resources.

See [the test index](../README.md) for build/run commands and known suite limitations. This folder cleanup does not change test logic.
