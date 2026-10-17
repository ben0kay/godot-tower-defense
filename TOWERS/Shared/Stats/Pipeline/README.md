# Ordered stat stages — explicit purchased levels

`TowerStatPipeline.cs` remains the single eight-stage coordinator. Each stage is a plain static C# helper, not a Node or service.

| File | Current behavior |
| --- | --- |
| 01_BaseStats.cs | Copy the tower's complete Level 1 snapshot |
| 02_TowerUpgrades.cs | Replace with the selected level's complete absolute snapshot |
| 03_TowerRanks.cs | Active; current named rank’s explicit total damage/range bonuses |
| 04_PermanentResearch.cs | Neutral; persistent research deferred |
| 05_LevelModifiers.cs | Neutral; map conditions deferred |
| 06_ActiveEffects.cs | Neutral; temporary effects deferred |
| 07_SafetyClamps.cs | Finite bounds, optional beam/area clamps, supported zero-rate firing |
| 08_FinalStats.cs | Publish an immutable cached snapshot, including weapon response |

Stage 02 does **no additive, percentage or cumulative upgrade arithmetic**. Level 3 reads row 3 directly, regardless of row 2's damage, rate, range, speed, aiming or penetration. Every type authors its own compact typed table: `BattleTowerLevels.cs`, `LaserTowerLevels.cs`, `BlasterCannonLevels.cs`. Construction/upgrade prices are outside combat math and enter the existing wallet as ResourceCost recipes.

`TowerStatContext(BaseStats, SelectedLevel)` carries immutable values. Optional BeamStats, AreaBlastStats and WeaponDamageStats preserve the capabilities present on that weapon; passive fixtures need no combat snapshot. Stage 02 selects all of them together. Stage03 modifies the selected row using the current earned Rank. Research/map/effect stages remain neutral; no generic modifier engine is introduced.

Preview and paid application call the same resolver. Resolve on Ready, config replacement/Changed or explicit RebuildStats, never each physics tick. `SetLevelStats(level, snapshot)` replaces one runtime row and emits Changed only when values differ. `TowerStatTrace` records all eight intermediate values on request. Stages never spend money, own saves, schedule effects or scan scene nodes.

## Phase4E optional travelling-wave capability

ShockwaveStats(DamagePerWave, ExpansionSpeedPixelsPerSecond, IntervalSeconds, EdgeDamageFraction) appends optionally to TowerStatBlock/FinalStats; existing constructors/towers remain unchanged. Maximum radius uses shared RangePixels, avoiding a second radius authority. ShockwaveTowerLevels.cs supplies complete independent rows and prices. Stage02 selects the whole row; Stage03 applies earned Rank; Stages04–06 remain neutral; Stage07 clamps finite damage >=0, speed >=1, interval >=.05s and edge0–1; Stage08 publishes the same cache used by attacks/Inspector/previews. Future research can modify these capability values through its existing stage and cache invalidation; it is not implemented. Style resources never enter combat math.

## Earned rank context

TowerStatContext includes an optional immutable TowerRankDefinition. Placed TowerBase.ResolveRankedLevel supplies its actual current rank for both application and purchase previews; raw Config.ResolveStats remains a neutral-rank build preview. Stage03 scales float range, integer projectile damage (nearest/ties-to-even), optional beam DPS and optional shockwave damage. It does not compound earlier ranks, alter level rows/costs or activate the reserved HP bonus. Ordinary XP awards update progress only; promotion rebuilds the per-instance FinalStats once. See TOWERS/Shared/Ranks/README.md for ownership/XP policy. Permanent research, map modifiers and active effects remain neutral.

The optional AmmunitionStats group follows the same eight stages: complete selected-level values enter Stage02; future research/effects may alter them afterward; Stage07 clamps capacity/attack/rate minima and Stage08 includes them in FinalStats. No cumulative ammo upgrade modifiers or second pipeline exists. Runtime stock and prepaid beam fractions belong to TowerAmmunition and never enter stat snapshots.

Construction C1/C2: `TowerLevel.WorkSeconds` is purchase/work metadata outside combat math. Initial construction preserves the cached Level1 snapshot but prevents capabilities from operating; completion does not rebuild the stat pipeline or award rank XP. C3 keeps the old purchased tier active during timed upgrades, then resolves the one pending tier with current rank at completion. Work progress does not resolve/rebuild combat stats.

## Cryo Slow capability

Optional SlowStats travels through the existing eight stages without a parallel resolver. Stage02 selects the complete row, Stage03 leaves Slow unchanged, Stage07 sanitizes its fraction, Stage08 publishes it. Cryo snapshots final Slow at projectile launch and displays it in Inspector/previews. No cumulative purchased modifier is introduced.

HD2: nullable Heat capability passes through the same eight stages. Complete selected levels supply it, Stage07 sanitizes invalid inputs, Stage08 publishes it. Current heat/disable timers live in TowerConditions and never feed back into stat resolution. Current authored Heat defaults are null; future research can modify the selected capability afterward. HD3 heat preview presentation is not implemented.
