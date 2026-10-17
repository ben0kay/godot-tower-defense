# Tower tests and architecture fixture

Tower catalog, per-level stats, ranks, upgrades, Laser/Blaster and Shield Emitter regression checks live here. They are invoked by the integration runner.

PassiveTestTower.tscn is a test-only nonweapon TowerBase scene with a simple square visual. It accepts shared TowerConfig, has no turret nodes, combat-stat snapshot, range, upgrade tiers or process/physics work. TowerCatalogChecks creates/registers its typed definition at test time, charges its 75-Credit recipe, places/selects it beside a real Battle Tower and verifies private data/independent progression. It is deliberately absent from DefaultTowerCatalog and introduces no new gameplay producer/support system.

`FootprintChecks.tscn` is a standalone paid-placement/navigation runner using small check groups and FootprintFixture. See [Footprints.md](Footprints.md) for multi-cell scenarios, commands and actual verification limits.

## Sniper/Machine Gun and barrel sequencing

`MultiBarrelTowerChecks.cs` runs in the full `EnemyIntegrationChecks` suite. It validates catalog entries and production scenes, sniper barrel-origin lethal shot, machine-gun A→B→A muzzle alternation, total bullet cadence, simultaneous two-bullet volley, atomic ammo shortage behavior, private authored snapshots, rank XP and future N-muzzle selection. The new towers do not require any direct GameplayHUD code changes. Godot/.NET test execution still needs local verification.

`ConstructionChecks.tscn` covers C1/C2 work clocks, pause/cleanup, both real paid launchers, all six weapons, production/logistics gates, structural navigation and500-idle/100-active benchmarks. Existing unrelated subsystem checks explicitly finish initial paid work through the real clock before asserting finished-building behavior. See [Construction.md](Construction.md).

`ConstructionChecks.tscn` now includes `TimedUpgradeChecks` (C3): real paid1→2→3 timing, mid-work promotion, old-tier combat/deliveries, stock/capacity preservation, selected work UI and close/reselect, pause/results, overlap, zero/invalid/stale/free and missing-manager cases. Older finished-weapon helpers wait through real paid work using ConstructionTestClock.PurchaseAndFinish; they do not set authored times to zero. See [TimedUpgrades.md](TimedUpgrades.md).

`ConstructionArtChecks` runs inside ConstructionChecks: actual paid Battle stage references, phase timing/selection/pause/ghost independence, no redundant texture writes or processing, missing/invalid styles, optional intermediate/final upgrade images, zero-time completion, rectangular transform preservation and teardown. See [ConstructionArtwork.md](ConstructionArtwork.md).

## Cryo CT1–CT3

`CryoTowerChecks.tscn` runs focused status/weapon/purchase/HUD/movement/Lava/500-enemy checks plus existing multi-barrel weapons. The Cryo group is also linked into EnemyIntegrationChecks. See [verification](CryoTower.md).

## IT2 upgrade-comparison integration assertions (added, execution pending)

`TowerUpgradeChecks` now checks typed current/next/gain cells, 540×325 defaults, maximum-level cleanup, beam vs projectile rows, ammunition capacity and beneficial shorter shockwave interval. The existing Laser, Blaster, Shockwave and ammunition suites also use the shared `TESTS/UI/UpgradeTableChecks.cs` helper instead of scraping the old freeform upgrade paragraph. **Runtime validation is outstanding** because no Godot .NET runner is available to this authoring session; do not treat the historical green results as verification of this commit.

## IT1–IT4 follow-up verification

The compact two-column Inspector supersedes the earlier540×325 scrolling layout. Existing upgrade/rank/work/ammo assertions follow the new scene paths and current-only MAX LEVEL table. InspectorTargetingChecks runs six-policy instance isolation, geometry checks at three desktop sizes, rank/XP and spatial regressions, plus checkpoint-aware500-enemy sharing. See TESTS/UI/InspectorTargeting.md for executed results.

## PV1/PV2 tracer, muzzle and Sniper aiming checks

`ProjectileVisualChecks.tscn` runs real Battle/Machine Gun/Sniper firing, guided launch snapshots, cleanup, fixed-slot renderer allocation checks, and existing MultiBarrel/Cryo compatibility groups. Its focused `Run` group is also linked into EnemyIntegrationChecks. See [actual verification](ProjectileVisuals.md).

`TowerConditionsChecks.tscn` covers HD1/HD2 source/handle isolation and timing, all seven offensive attack gates/heat charges, threshold volleys, beam30/60/120Hz, cooling/pause, purchases/rank retention, real clock expiry and allocation checks. It is also linked into the integration runner. See [actual verification](TowerConditions.md).
