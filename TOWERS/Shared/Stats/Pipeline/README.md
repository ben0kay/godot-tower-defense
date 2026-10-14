# Ordered stat stages

TowerStatPipeline.cs is the single coordinator. Each stage is an ordinary static C# helper, not a Godot Node or service. Numbered filenames provide visible order without numeric C# type names.

| File | Current behavior |
| --- | --- |
| 01_BaseStats.cs | Copy the resolution context's authored snapshot |
| 02_TowerUpgrades.cs | Active; cumulative purchased tier operations |
| 03_TowerRanks.cs | Neutral; kill milestones later |
| 04_PermanentResearch.cs | Neutral; persistent player research later |
| 05_LevelModifiers.cs | Neutral; current-map conditions later |
| 06_ActiveEffects.cs | Neutral; runtime buffs/debuffs later |
| 07_SafetyClamps.cs | Safe finite values and supported zero-rate firing |
| 08_FinalStats.cs | Fresh immutable final snapshot |

Future stages receive their own read-only inputs through TowerStatContext and return a new TowerStatBlock. Keep stage arithmetic/order explicit, rebuild from original inputs and use this same resolver for upgrade previews. Stage files must not spend money, own progression saves, attribute kills or schedule timed effects.


Optional BeamStats now propagates through all eight stages. Stage 02 applies TowerStatId.BeamDps with existing arithmetic, Stage 07 clamps DPS, Stage 08 includes the immutable capability. Original Battle fields/adapter/constructors and neutral future stages remain supported; no parallel Laser pipeline exists.
