## HD4 additional heat preset coverage — 2026-10-26

`TowerConditionsChecks` now validates shipped MG/Laser/Blaster/Rocket/Shockwave opt-in resource timing on all paid levels, shot versus damaging-beam-second versus wave-interval resolution, opt-out/private-copy isolation, explicit Heat row precedence and Sniper/Cryo/Stasis remaining heatless. This was added as focused source regression coverage; **execution still pending** (no local Godot/.NET runtime for this authoring session). See `TOWERS/Shared/Conditions/README.md` for balance defaults.

---

## CD3 construction hologram presentation — 2026-10-25

`ConstructionDroneChecks.tscn` now also asserts beam animation time, exactly one site-anchored completion flash after paid initial construction, independence from the returning/pooled drone, short-lived idle renderer cleanup, and suppression when visuals are off or a paid upgrade completes. CD1/CD2 hold/payment/return tests remain intact. Actual editor/GPU appearance testing is separate and not claimed here.

---

## CD1/CD2 builder verification — 2026-10-21

Run ConstructionDroneChecks.tscn for real paid gates/beam/return, independent clocks, FIFO/cap/token/duplicate safety, pause/visual-off, failures/strict fallback, multi-Citadel/marker/source loss,2×2/3×2, destruction/teardown, mission Results freeze and20-build CPU/allocation stress. It opts in explicitly; ordinary AmmoLoopFixture tests disable builders for their unrelated timer contracts. The fixture placement search now includes all painted layers, including solid foundations. Saved maps are never changed.

Dedicated builder/FootprintChecks/PerformanceOverlayChecks pass. Existing combined ConstructionChecks passes timers/Battle art then hits the pre-existing ammo_factory map-space purchase failure, reproduced on baseline production code. Exact timings, known limits and pending desktop review: TOWERS/ConstructionDrones/README.md.

## Tower durability rebalance — 2026-10-20 (source updated; Godot runtime tests pending)

**Current authoritative defaults supersede older HP references below:** Base/max hull health for every available player-built tower/structure is **10× the previous level-1 value**. All **nine offensive towers** (Battle, Laser, Blaster Cannon, Shockwave, Machine Gun, Sniper, Rocket, Cryo, Stasis) have explicit, independent paid level snapshots: **1,000 → 1,050 → 1,102.5 HP** (two-level towers stop at 1,050). **Every purchased upgrade adds 5% of the previous tier's Max HP**, compounded, not 5 percentage points of the original at every tier; the values are authored as complete absolute tier stats, not runtime additive bonuses.

**Other buildings:** Ammunition Factory and Depot **1,000 HP** each (one build tier/no paid upgrades yet); Resource Tower and Adaptable Resource Generator **1,500 → 1,575 → 1,653.75 HP** (new tier progression replaces their former 150/200/250 HP schedule); Stealth Beacon **1,800 HP** (one tier). Generic passive config fallback is 1,000 HP. These are **hull**, not shields. Enemy damage, friendly firing stats, costs, unlocks, construction timing and Citadel HP are unchanged.

**Pipeline and HP semantics:** Stage02 still selects one authored tier, then Stage03 applies earned **rank HP bonuses independently**. The existing `TowerBase.Durability` synchronizes max capacity while **preserving missing absolute HP**; upgrades do not fully heal already-damaged towers. The current Inspector's `Max HP` comparison automatically shows the extra `(+50)` at level2 and `(+52.5)` at level3 for a standard-rank 1,000-HP offensive tower. No duplicate HP system, per-frame calculations, or tower-specific damage handlers were added.

**Verification still needed:** Compile Godot .NET, run `TESTS/Towers/TowerHealthChecks.tscn` and the existing integration scene that executes `TowerStatChecks`/`TowerUpgradeChecks`, plus `TESTS/Economy/CreditTowerChecks.tscn`. Inspect upgraded/damaged towers at runtime, including the resource producer `.tres` serialized tiers. Written test assertions were updated, but **not executed here**.

---

## Offensive range rebalance regression notes — 2026-10-17

All seven offensive types now author approximately 15% more range at **every** level in their own `*Levels.cs`. Existing exact-row assertions were updated in `TowerStatChecks`, `TowerUpgradeChecks`, `LaserTowerChecks`, `CryoTowerChecks`, `MultiBarrelTowerChecks`, `ShockwaveChecks`, and `TowerRankChecks` (including Shockwave's rank-scaled radius). These preserve the existing stat pipeline and distinguish Blaster **target range** from unchanged **area radius**. Godot headless/desktop tests could not run in this environment because neither Godot nor .NET is installed; rerun test scenes and visually review range rings and targeting at max range.

---

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

## Shared beam ripple and particle presentation — 2026-10-17

`LaserTowerChecks` now asserts travelling ripple geometry (more than two points while enabled), an exactly anchored muzzle, last visual point within the configured target wander radius, unchanged gameplay damage/stats across visual edits, the two scene-owned `GpuParticles2D` emitters with shared motion material, independent particle switches and a two-point idle/straight-line fallback when all effects are off. Particle textures/materials live in `FEEDBACK/Shared/`; implementation notes are in `TOWERS/Shared/Weapons/Beams/README.md`. **Source changed; the modified fixture has not yet been run in Godot here.** Headless checks alone will not verify rendered glow/spark quality.

`BattleHeatTimingChecks.tscn` exercises the enabled authored Battle preset with3/3.5 minutes, all three selected level rows, and30/60/120Hz clocks. It checks cold-start duration, nine-second recovery, passive cooling, private live config changes and unchanged cached FinalStats. Actual outcomes: BattleHeatTiming.md.
