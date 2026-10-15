# Ordered stat stages — explicit purchased levels

`TowerStatPipeline.cs` remains the single eight-stage coordinator. Each stage is a plain static C# helper, not a Node or service.

| File | Current behavior |
| --- | --- |
| 01_BaseStats.cs | Copy the tower's complete Level 1 snapshot |
| 02_TowerUpgrades.cs | Replace with the selected level's complete absolute snapshot |
| 03_TowerRanks.cs | Neutral; earned ranks deferred |
| 04_PermanentResearch.cs | Neutral; persistent research deferred |
| 05_LevelModifiers.cs | Neutral; map conditions deferred |
| 06_ActiveEffects.cs | Neutral; temporary effects deferred |
| 07_SafetyClamps.cs | Finite bounds, optional beam/area clamps, supported zero-rate firing |
| 08_FinalStats.cs | Publish an immutable cached snapshot, including weapon response |

Stage 02 does **no additive, percentage or cumulative upgrade arithmetic**. Level 3 reads row 3 directly, regardless of row 2's damage, rate, range, speed, aiming or penetration. Every type authors its own compact typed table: `BattleTowerLevels.cs`, `LaserTowerLevels.cs`, `BlasterCannonLevels.cs`. Construction/upgrade prices are outside combat math and enter the existing wallet as ResourceCost recipes.

`TowerStatContext(BaseStats, SelectedLevel)` carries immutable values. Optional BeamStats, AreaBlastStats and WeaponDamageStats preserve the capabilities present on that weapon; passive fixtures need no combat snapshot. Stage 02 selects all of them together. Ranks/research/map/effect stages can later modify the selected row in their existing order; no such systems or generic modifier engine are implemented now.

Preview and paid application call the same resolver. Resolve on Ready, config replacement/Changed or explicit RebuildStats, never each physics tick. `SetLevelStats(level, snapshot)` replaces one runtime row and emits Changed only when values differ. `TowerStatTrace` records all eight intermediate values on request. Stages never spend money, own saves, schedule effects or scan scene nodes.
