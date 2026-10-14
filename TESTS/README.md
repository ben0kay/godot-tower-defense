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

SmoothRepathChecks also runs in the integration scene. It deliberately delays replacement processing, checks continued safe-prefix movement and stopping before the wall, verifies a near-wall elbow handoff without center retreat, and tests forward handoffs with offsets from both fresh and old snapshots. Targeting fixtures now use clear test space so the handoff safety checks do not accept synthetic routes originating inside painted walls.
## Defense bar regression checks

The integration fixture now also asserts that Enemy001 has a hidden full-health bar, a damaging hit reveals the correct fractional fill, rotation stays counter-aligned, max-health scaling grows to but not beyond its width cap, and full/zero health hide the component. It doesn't replace visual inspection in F5 for pixel alignment and scale.

## Construction economy and HUD checks

EconomyChecks runs in the same Main-based fixture: per-level starting-credit/reset behavior, live HUD values/events, duplicate IDs and multi-resource costs, invalid/negative/overflow recipes and grants, recursive transaction rejection, creation failures before and after occupancy, successful 200-credit purchases, insufficient funds, repeated occupied-cell clicks and deep config/cost isolation. Actual secondary-material construction is exercised by temporary in-memory costs; no saved resource is changed. The route-sealing fixture asserts payment remains unchanged.

HudTowerChecks now expects initialized live Credits, with Citadel/Wave still unknown. It checks the independent status APIs, true center/non-overlap at 1920/1366/640 widths, health thresholds/clamps, tray/Inspector independence and shortest-angle/speed-capped turret aiming. Weapon checks also verify no firing before alignment and a fixed placement root.

Synthetic swarm/repath fixtures explicitly select the one portal they reposition, so additional authored portals do not alter controlled start positions or route lengths. The initial live-level checks retain all configured portals.

## Wave regressions
WaveChecks runs against Main's WaveManager/SpawnManager. It asserts immediate first spawns, exact 10/5/20 counts and 2s/5s pauses, overlapping batch ships, ignored final post-delay, owned-only completion and idempotent arrival/death, manual next/final state, quota rounding/shuffle seeds, setup validation, random approximately balanced portals, unchanged F2 round-robin, blocked and named exits, map-edge-only spawning, cap retry backoff, reset/unsubscription and bounded zero-interval/lag spawning. Pure composition checks use synthetic IDs without loading or introducing new enemy assets. Live waves remain Enemy001-only.

## Kill rewards

`EnemyKillRewardChecks` runs in the production-scene integration fixture after WaveChecks. It verifies 3 Credits from Enemy001 kills, event-driven HUD updates, one payout per kill, zero payouts on arrival, configurable zero-reward enemies, and identical payout for WaveManager-owned ships. Wallet and authored waves are restored afterward.

## Death explosion feedback

`DeathExplosionChecks` runs once in the Main scene integration suite. It tests that a killed Enemy001 creates exactly one shared puff, actual Sprite2D scale changes explosion size, optional per-enemy multiplier overrides the result, and arriving ships or repeated damage never trigger explosions. It checks active-effect cleanup and turns GPU particles off for the mass-kill navigation/wave suites. The test validates spawn/sizing logic; the rendered orange/grey smoke still needs an F5 visual check in Godot .NET.

## Terrain authoring cleanup
TerrainCategoryChecks verifies the three filtered palettes, independent movement/build permissions, empty/decorative space, decals over floor/solid cells, overlap fallback and object reservations. Fixtures use WorldLevel.Passable/Impassable. TerrainMigration.md records the exact before/after comparison of all 176 existing authored tiles.

## Tower stat pipeline
TowerStatChecks verifies all eight stage calls/order, neutral baseline parity, deterministic resolution, safe finite bounds, source/cache independence, numeric Resource change notifications, Config replacement/exit cleanup, unchanged cached values across combat ticks, live zero-rate gating/re-enabling, and selected Inspector updates. Existing aiming, damage/cooldown, payment and range targeting checks continue to exercise the production BattleTower.

## Citadel durability and per-level settings

`TESTS/CitadelHealthChecks.cs` runs in the existing Main-scene integration suite. It checks the level-specific initial maximum, real Enemy001 arrivals and configured CitadelDamage, instantaneous health HUD/bar updates, duplicate-arrival prevention, killed-enemy exclusion, zero clamping, once-per-destruction event, and reset without modifying any saved level Resource. `HudTowerChecks` now expects a live objective on startup and restores the authoritative display after synthetic status setter tests. Defeat/menu flow is intentionally not part of this pass.

## Purchased tier regressions
TowerUpgradeChecks drives actual HUD upgrade actions and validates levels 1→2→3, preview/apply parity, shared/base/other-instance isolation, atomic credit/material prices, insufficient funds, disabled rules, stale and same-frame repeat requests, max tier, invalid tier/operation data, arithmetic, unchanged navigation and no repeated compounding. It waits for GUI layout to verify Inspector screen bounds and tray height/independence.

## Menu / profile foundation

`MenuFoundationChecks.cs` is included in the existing Main-scene headless suite. It checks loadable Boot, Main and Campaign scene resources, the unchanged gameplay destination, factory-created button callbacks, styled existing HUD buttons and profile name validation. The automated check **does not touch the user's real profiles**. Manual Boot smoke test: create/select a profile, enter Campaign → Play Level001, exit/restart and confirm the same profile remains selectable; try multiple profiles, invalid names and returning to Boot with Change Profile. Test the normal F5 startup and direct gameplay `Main.tscn` F6 separately. This does not test campaign persistence because that system is still planned.

## Phase 4A/4B tower regression coverage

EnemyIntegrationChecks now runs TowerCatalogChecks (validation, mixed paid placement/selection, passive capability absence, config isolation and independent Battle progression) and TowerBuildBarChecks (40 registrations, availability, canonical prices/icons, generated actions, focus, scrolling, empty/rebound catalogs and independent panels). TESTS/Towers/PassiveTestTower is a test-only nonweapon scene, excluded from the default catalog. Existing economy failure-after-occupancy rollback, route sealing, combat, stat/upgrades and 500-ship checks are retained.

Run `Godot --headless --path . TESTS/MenuTowerFlowChecks.tscn` separately to exercise real Boot profile selection → Main → Campaign → Level001 buttons, dynamic Battle entry, construction and paid upgrade. The fixture injects a memory-only ProfileStore list in that standalone process; it does not read or write user profiles. Existing EnemyIntegrationChecks loads Main.tscn directly without a save/profile dependency. Headless tests validate behavior and bounds, not rendered appearance or hardware FPS.

## Enemy shields — optional wave spawn flag

`TESTS/EnemyShieldChecks.cs` runs after `WaveChecks` in `EnemyIntegrationChecks`. It checks the shared `MaxShield` default and Enemy001's authored capacity, exact weighted composition of same-ID entries with distinct shield flags, real WaveManager → SpawnManager flag propagation, full and zero starting shields, shield-first damage, exact shield break, overflow into hull, existing hull DefenseBar updates, unaffected debug/F2 spawning and per-instance independence. Test-created ships are removed and the original level wave set restored without changing authored files. Shield rendering and regeneration are not part of this pass.


DamageTypeChecks covers typed coefficients, exact shield break,20/30/4.25 base spillover, zero absorption blocking, invalid payloads/profiles, overflow safety, isolated enemy state, legacy behavior and idempotent kill/arrival rewards. The independent 4C-1 checkpoint runs before Laser implementation.


LaserTowerChecks runs after the independent 4C-1 damage/shield fixture: real catalog placement/recipe/upgrade actions, private source data, shared DPS operations/clamps,30/60/120 Hz parity, typed hull/shield spillover, First/Closest/retargeting, range/alignment/disable/zero-DPS/arrival/death/unload gates, endpoints/visual independence and no per-tick node creation. TowerBuildBarChecks now expects both playable default entries. Full regression and standalone menu flow are exercised with the Godot assembly compiled via Roslyn/source generators; standard editor SDK/MSBuild is unavailable here and no live rendered visual-quality check is claimed.


EnemyCatalogChecks validates canonical scene registration, invalid assets/IDs/balance, two-type exact weighted waves/required portals, weaker/faster Enemy002 navigation/targeting/shield isolation, distinct kill rewards/Citadel arrivals and safe oversized rejection. E2A is compiled/headless-verified separately before ability work.


ShieldEmitterChecks adds real registered support scenes to test staggered per-instance cooldowns, capped restoration, hull-bar neutrality, self/eligibility/range, bounded candidate/recipient work, no catch-up bursts, private configs, disabled/null/invalid components and kill/arrival/unload/queued cleanup. Ordinary Enemy001/Enemy002 remain ability-free. All original tower/combat/navigation/wave/shield tests stay active. MenuTowerFlowChecks still independently verifies startup-to-Level001 flow without save writes.
