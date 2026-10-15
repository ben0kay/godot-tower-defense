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
