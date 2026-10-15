# Headless gameplay checks

**Phase 4D implemented:** AreaDamageChecks and BlasterCannonChecks run in EnemyIntegrationChecks; shared instant AoE and the registered Blaster Cannon are production code. See the completed Phase 4D section below. Phase4E ShockwaveChecks now covers the separate timed wavefront, shared styles and purple Shockwave Tower.

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
TowerUpgradeChecks drives actual HUD upgrade actions and validates levels 1→2→3, preview/apply parity, shared/base/other-instance isolation, atomic credit/material prices, insufficient funds, disabled rules, stale and same-frame repeat requests, max tier, invalid row chains, complete Stage02 substitution, unchanged navigation and no repeated compounding. It waits for GUI layout to verify Inspector screen bounds and tray height/independence.

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


PortalSpawnChecks now verifies independent0.5/3 portal sizes, visual scale/center/footprint/revision preservation, scaled wave and debug offsets, actual expanded spread, clearance/segments, zero-offset mode and invalid-size fallback. Existing baseline 8px checks remain.

## Phase N4 navigation regression

`CorridorAssignmentChecks` runs inside EnemyIntegrationChecks: deterministic percentage boundaries/distribution, config clamping and unchanged seeded wave composition. Run `Godot --headless --path . TESTS/CorridorNavigationChecks.tscn` separately for stable forced-repath preferences, exact disabled/unselected waypoint parity with zero sampling, upper/lower/center spread in three-cell corridors, narrow/open/radius clearance, endpoint/detour/physical-distance checks, swept-corner regression and safe progress through dynamic tower blocks. Its runtime terrain/config copies never modify authored resources.

The fixture exercises straight and winding corridors at 100/500 ships and 0/50/100% selection, records assignment and movement CPU timings, enforces the existing request-count budget, checks no unnecessary A* during unchanged movement, and waits for blocked-route repaths to settle safely. Existing EnemyIntegrationChecks (including SwarmNavigationChecks, RepathMovementChecks and SmoothRepathChecks) and standalone PlacementPerformanceChecks remain required. Results, commands and manual playtest limitations are recorded in CorridorPerformance.md.

## Missing tower catalog startup regression

EnemyIntegrationChecks verifies Main's authored Catalog assignment, clears that export on the in-memory scene before it enters the tree, and checks automatic default recovery plus both Battle/Laser build buttons. The remaining production gameplay checks then exercise the recovered catalog. MenuTowerFlowChecks continues to cover the ordinary Boot/Main/Campaign launch with its authored assignment. No scene or resource is saved by this fixture.

## Phase 4C-3 — completed combat regression pass

`DamageTypeChecks` now verifies independent absolute shield/hull overrides, explicit zero versus inheritance, invalid/nonfinite input/state rejection, penetration at 0/5/100%, exact/partial shield breaks, exposed hull, zero shield coefficient with bypass, finite extreme state, legacy 1:1 behavior, real Battle projectile contact and launch-time payload snapshots, target isolation and once-only kill rewards. The numerical fixtures include Battle 12 → 94.3 shield / 59.4 hull from 100/60 and Laser 12 with 6.00/0.15 overrides → 0 shield / 58.95 hull from 30/60.

`LaserTowerChecks` uses the authored 12 base DPS / 6.00x shield / 0.15x hull balance and 22-DPS paid tier. It asserts 72 shield/s and 1.8 exposed hull/s at 30/60/120 Hz, independent override opt-out/zero, live Inspector coefficient updates, cache invalidation versus tick reuse, and the existing beam endpoints/style/targeting/lifecycle/price/upgrade cases. `TowerStatChecks` and `TowerUpgradeChecks` assert base 5% penetration, all eight stages, finite 0..1 safety clamps, private cache/source isolation, live selected Inspector changes, and unchanged 5% at purchased levels 1/2/3. Passive fixtures still have no combat snapshot.

### Explicit fixture isolation

The current authored Enemy001 resource has zero shield capacity. The integration runner sets a 100-point shield capacity **in memory only** for its numerical shield fixtures and restores the authored value in `finally`; no enemy asset, ID, wave opt-in or restoration mechanic changes. Legacy offset/navigation fixtures temporarily disable the newer N4 corridor geometry and restore its flag in `finally`, so their 8-pixel lateral-only assertions remain meaningful. The dedicated `CorridorNavigationChecks.tscn` independently tests enabled N4 geometry and 0/50/100% variation. Production navigation code/settings remain unchanged.

The default Battle config had lost its Level2/Level3 references before this pass. Those references were restored to the existing, unchanged tier assets so the required paid-upgrade regressions can run.

### Actual validation

- C# compilation: passed using Roslyn 4.11 with .NET 8 reference assemblies, Godot 4.7.2 API and Godot source generators; no compiler errors or warnings. The ordinary `dotnet build --no-restore` command could not initialize because this environment's SDK is missing a compatible `NuGet.Protocol` dependency; that standard build route is not claimed as passing.
- Godot 4.7.2 headless `TESTS/EnemyIntegrationChecks.tscn`: passed, including all new combat fixtures and existing stats, paid upgrades, HUD, economy, waves, shield emitter, feedback, navigation and 500-ship fixtures.
- Godot headless `TESTS/MenuTowerFlowChecks.tscn`: passed actual Boot → Main → Campaign → Level001 button flow plus paid Battle construction/upgrade using a memory-only profile.
- Godot headless `TESTS/CorridorNavigationChecks.tscn`: passed enabled N4 straight/winding geometry, dynamic tower repathing and 100/500-ship 0/50/100% benchmarks.
- Direct headless `Main.tscn` startup: passed. `git diff --check`: passed.

These checks do not assert rendered beam appearance or hardware-independent FPS. Permanent research/stage 04, saving, armour and future tower mechanics remain deferred.

## Phase 4D — shared instant area damage and Blaster Cannon

`AreaDamageChecks` and `BlasterCannonChecks` run after the existing typed-damage/Laser checks in EnemyIntegrationChecks. Pure falloff and actual enemy fixtures cover center/half/edge/outside, inclusive enemy centers, zero-radius exact-center policy, 0/25/75/100% edge retention, invalid/nonfinite settings, extreme finite amounts, preserved type/overrides/penetration, inherited Explosive shield/hull/spillover, unique registration and recipient snapshots. A kill callback kills, frees, queues and unregisters later candidates to verify application-time validity/membership checks and once-only rewards.

The Blaster fixture exercises the real catalog/build-bar action, route-safe 300-Credit placement, private config/tiers, repeated occupancy rejection, selected AoE Inspector values, 200-Credit Level 2 purchase/preview parity (75 damage / 96 px / unchanged 25% edge), all eight stat stages, absolute AoE row selection/independence, finite clamps and event-driven cache/Inspector updates. Fired projectiles are tested at 30/60/120 Hz: no travel-time damage, one instantaneous impact, primary/half/edge hit once, outside untouched, launch snapshots after source damage/radius/falloff/override/penetration edits, and visual expiry without extra damage. Separate production-shot fixtures test explicit zero shield override + hull override + penetration together and edited falloff in newly fired attacks. Target death/free/arrival retains the last-known point; leaving tower firing range does not cancel impact; timeout and unload cannot cause late damage.

One isolated blast also hits 500 living recipients without creating any scene nodes in shared combat. A sample run measured about 1.3–1.8 ms for that impact in this container; this is not a rendered-FPS or hardware-independent performance guarantee. EnemyRegistry now mirrors membership in a HashSet for constant-time uniqueness and removal rechecks; existing living-list consumers, lifecycle and rewards remain unchanged. The existing 500-ship navigation and placement fixtures still run separately.

### Actual Phase 4D validation

- C# compilation passed with Roslyn 4.11, .NET 8 reference assemblies, Godot 4.7.2 API and Godot source generators, with no compiler errors/warnings.
- `TESTS/EnemyIntegrationChecks.tscn` passed: new shared-area/Blaster checks plus existing Battle 5% penetration, Laser 12-DPS/6.00x/0.15x behavior, paid upgrades, UI/HUD, shield emitter, waves, economy, feedback, terrain/navigation and 500-enemy fixtures.
- `TESTS/MenuTowerFlowChecks.tscn` passed Boot → Main → Campaign → Level001, Blaster catalog availability and existing paid Battle construction/upgrade using a memory-only profile.
- `TESTS/CorridorNavigationChecks.tscn` passed enabled N4 geometry, dynamic repathing and 100/500-ship 0/50/100% variation benchmarks.
- `TESTS/PlacementPerformanceChecks.tscn` passed its 100/500-ship placement fixtures.
- Direct headless Main.tscn startup and `git diff --check` passed.

The ordinary `dotnet build --no-restore` route remains blocked before compilation by the environment's missing compatible NuGet.Protocol dependency; the successful compile above uses the actual Godot source generators directly and its resulting assembly was run by Godot. Live-window visual review of the new vector cannon/projectile/flash is outstanding; headless checks establish combat/timing and visual independence, not artistic quality.

That Phase4D milestone added no research, saving, Missile Tower, armour or timed shockwave runtime. Stage04 remains deferred; Phase4E timed wavefronts were subsequently implemented below.

## Explicit C# tower-level refactor (2026-10-15)

TowerStatChecks independently compares every field and credit price at Battle Levels 1–3, Laser 1–2 and Blaster 1–2; tests boundary rejection, Level2/Level3 independence across all projectile stats, immutable authored defaults and expansion by appending one complete row. TowerUpgradeChecks asserts Stage02 replacement before later neutral stages, exact paid preview application, all existing atomic Credits/material costs, insufficient funds, disabled/stale/same-frame requests and maximum level. Laser/Blaster tests additionally prove independent beam/area/response/penetration snapshots and no per-tick cache resolution. Old modifier arithmetic tests were replaced because paid modifiers no longer exist.

Validation: direct Roslyn C# compilation against .NET 8 and GodotSharp with the Godot source generator passed. The ordinary dotnet build command is blocked before compilation by the environment's missing NuGet.Protocol 6.11.2.1 assembly. Actual Godot 4.7.2 .NET headless import, EnemyIntegrationChecks, MenuTowerFlowChecks, CorridorNavigationChecks, PlacementPerformanceChecks and direct Main startup passed. Headless suites retain 500-enemy movement/placement/AoE fixtures and cached combat checks; timing observations are not a rendered-FPS guarantee. Existing terrain-overlap fixture warnings remain. No live-window visual review was performed.

Push integration: rebased onto the newer map/Main/profile commit, retaining KineticShield=0.25 and the optional missing catalog export. Kinetic inherited-response expectations read the authored coefficient; shield-break fixtures scale their starting shield to retain the same base-unit boundary. The integration runner verifies the placement controller and the existing default-catalog recovery rather than requiring an explicit scene binding.

## Phase4E shared visual/wavefront and purple Shockwave Tower (2026-10-15)

ShockwaveChecks runs in EnemyIntegrationChecks. It verifies no instant full-disc damage at emission;30/60/120Hz wave timing, reach/falloff, unchanged behind-front health and inclusive maximum edge; fast relative through-passes with both endpoints outside; earliest-contact falloff; terminal-tick motion clamping; exact zero radius; independent overlapping/subsequent emissions; shield/hull inherited responses, spillover, optional/zero overrides and penetration. Lifecycle cases cover dead/free/arrived targets, mutation/reentrant unload during kill dispatch, registry loss, invalid speed and malformed defense cancellation. Style copies/changes/missing visuals never alter combat; external visual radius exactly tracks gameplay and ignores autonomous clocks/radius. Standalone cosmetic expansion/cleanup remains independently tested.

The real catalog/build-bar/placement fixture pays350 Credits, rejects repeated occupancy, validates all baseline numbers, displays effective damage/range/speed/interval/falloff, then pays225 for independent Level2 and compares the committed cache to its preview. It verifies maximum level, unchanged navigation, source/row/style copies, no tick/style-based stat resolution, launched wave snapshots, new waves using upgraded values,30/60/120Hz4-second cadence and no post-hitch bursts. The500-enemy fixture uses one emitter and zero per-enemy physics/nodes; the observed contact/dispatch pass was about1.7–1.9ms on this headless host, not a rendered FPS claim.

BlasterCannonChecks retains its30/60/120Hz actual instant-impact tests, now asserting orange style snapshots after source edits and that a long/larger shared cosmetic effect never reapplies or delays damage. The default catalog/menu suites require the fourth Shockwave entry. Existing Battle/Laser/Blaster, shields, economy, waves, enemy lifecycle and navigation checks remain active.

Validation: direct Roslyn C# compilation against .NET8/GodotSharp with Godot source generators passed. Godot4.7.2 .NET headless EnemyIntegrationChecks, MenuTowerFlowChecks, CorridorNavigationChecks, PlacementPerformanceChecks and direct Main startup passed. Standard dotnet build remains blocked before compilation by the environment's missing NuGet.Protocol6.11.2.1 assembly; editor import additionally reports the incomplete SDK/MSBuild installation while importing assets. Existing terrain-overlap warnings remain. No live-window visual-quality review was performed.
