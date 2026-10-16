# Tower tests and architecture fixture

Tower catalog, per-level stats, ranks, upgrades, Laser/Blaster and Shield Emitter regression checks live here. They are invoked by the integration runner.

PassiveTestTower.tscn is a test-only nonweapon TowerBase scene with a simple square visual. It accepts shared TowerConfig, has no turret nodes, combat-stat snapshot, range, upgrade tiers or process/physics work. TowerCatalogChecks creates/registers its typed definition at test time, charges its 75-Credit recipe, places/selects it beside a real Battle Tower and verifies private data/independent progression. It is deliberately absent from DefaultTowerCatalog and introduces no new gameplay producer/support system.

`FootprintChecks.tscn` is a standalone paid-placement/navigation runner using small check groups and FootprintFixture. See [Footprints.md](Footprints.md) for multi-cell scenarios, commands and actual verification limits.

## Sniper/Machine Gun and barrel sequencing

`MultiBarrelTowerChecks.cs` runs in the full `EnemyIntegrationChecks` suite. It validates catalog entries and production scenes, sniper barrel-origin lethal shot, machine-gun A→B→A muzzle alternation, total bullet cadence, simultaneous two-bullet volley, atomic ammo shortage behavior, private authored snapshots, rank XP and future N-muzzle selection. The new towers do not require any direct GameplayHUD code changes. Godot/.NET test execution still needs local verification.
