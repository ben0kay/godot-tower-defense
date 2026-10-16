# Tower tests and architecture fixture

Tower catalog, per-level stats, ranks, upgrades, Laser/Blaster and Shield Emitter regression checks live here. They are invoked by the integration runner.

PassiveTestTower.tscn is a test-only nonweapon TowerBase scene with a simple square visual. It accepts shared TowerConfig, has no turret nodes, combat-stat snapshot, range, upgrade tiers or process/physics work. TowerCatalogChecks creates/registers its typed definition at test time, charges its 75-Credit recipe, places/selects it beside a real Battle Tower and verifies private data/independent progression. It is deliberately absent from DefaultTowerCatalog and introduces no new gameplay producer/support system.

`FootprintChecks.tscn` is a standalone paid-placement/navigation runner using small check groups and FootprintFixture. See [Footprints.md](Footprints.md) for multi-cell scenarios, commands and actual verification limits.

## Sniper/Machine Gun and barrel sequencing

`MultiBarrelTowerChecks.cs` runs in the full `EnemyIntegrationChecks` suite. It validates catalog entries and production scenes, sniper barrel-origin lethal shot, machine-gun A→B→A muzzle alternation, total bullet cadence, simultaneous two-bullet volley, atomic ammo shortage behavior, private authored snapshots, rank XP and future N-muzzle selection. The new towers do not require any direct GameplayHUD code changes. Godot/.NET test execution still needs local verification.

`ConstructionChecks.tscn` covers C1/C2 work clocks, pause/cleanup, both real paid launchers, all six weapons, production/logistics gates, structural navigation and500-idle/100-active benchmarks. Existing unrelated subsystem checks explicitly finish initial paid work through the real clock before asserting finished-building behavior. See [Construction.md](Construction.md).

`ConstructionChecks.tscn` now includes `TimedUpgradeChecks` (C3): real paid1→2→3 timing, mid-work promotion, old-tier combat/deliveries, stock/capacity preservation, selected work UI and close/reselect, pause/results, overlap, zero/invalid/stale/free and missing-manager cases. Older finished-weapon helpers wait through real paid work using ConstructionTestClock.PurchaseAndFinish; they do not set authored times to zero. See [TimedUpgrades.md](TimedUpgrades.md).
