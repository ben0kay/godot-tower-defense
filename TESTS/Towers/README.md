# Tower tests and architecture fixture

Tower catalog, per-level stats, ranks, upgrades, Laser/Blaster and Shield Emitter regression checks live here. They are invoked by the integration runner.

PassiveTestTower.tscn is a test-only nonweapon TowerBase scene with a simple square visual. It accepts shared TowerConfig, has no turret nodes, combat-stat snapshot, range, upgrade tiers or process/physics work. TowerCatalogChecks creates/registers its typed definition at test time, charges its 75-Credit recipe, places/selects it beside a real Battle Tower and verifies private data/independent progression. It is deliberately absent from DefaultTowerCatalog and introduces no new gameplay producer/support system.

`FootprintChecks.tscn` is a standalone paid-placement/navigation runner using small check groups and FootprintFixture. See [Footprints.md](Footprints.md) for multi-cell scenarios, commands and actual verification limits.
