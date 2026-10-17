# Battle Tower Weapons I — test/authoring record

## Delivered behavior

Campaign → RESEARCH → Battle Tower Weapons I. ID battle_weapons_1; cost1 RP; prerequisite research_core; axial(-1,0); effect TowerDamagePercent; TargetTowerId battle; EffectPercent5. It is purchased once using the same RP/ownership atomic save as Citadel Master I. Ownership persists per profile in progression schema2; no migration/schema bump needed for another stable ID.

ResearchDeployment captures a profile's purchased effects once in CitadelSession at mission load. Citadel health and paid tower placements/build previews share that immutable snapshot. A tower retains its type's TowerResearchBonuses when resolving a level/rank or reacting to config edits. Pure Stage04 adds the captured percentage once to the already ranked damage. No store access/Resource loading per tower frame or stat resolve. A new deployment refreshes the snapshot; current missions do not react to later menu/profile changes.

Standard-rank whole damage: Level1 30→32, Level2 33→35, Level3 36→38. Rounding is nearest/ties-to-even, consistent with Stage03 ranks. Purchased levels and ranks are applied before research. Authored rows and unrelated stats remain identical. More approved damage nodes can reuse the targeted percent effect and add their percentages in the deployment snapshot; unusual new mechanics need an explicit effect resolver.

## Automated verification

Run res://TESTS/Research/BattleResearchChecks.tscn. The fixture injects memory-only profiles and a temporary progression directory, then restores the prior in-memory state and removes its files. It covers real Research scene/button purchase, save/reload/reopen, repeat rejection, frozen snapshots, all five ranks × three tiers, both maps, build preview, actual current/next/gain grid, paid tier application, rank promotion, config cache rebuild, real projectile damage/ammo consumption, unaffected Laser stats, profile isolation and neutral F6. Catalog rejects tower effects lacking a stable target ID.

PASS in this environment: Roslyn/Godot source-generator compilation; BattleResearchChecks; ResearchFoundationChecks; ResearchMenuChecks; MenuTowerFlowChecks; BattleHeatTimingChecks; HeatInspectorChecks. Final re-runs after incorporating the latest shield/wave content also PASS: BattleResearchChecks, ResearchFoundationChecks, ResearchMenuChecks and full ResultsChecks (outcomes plus actual Retry/Next/Menu transitions).

The existing Godot/.NET runtime and reference assemblies support source-generator compilation and headless tests. A normal SDK/MSBuild build is still not certified here (previously blocked by the container's SDK/MSBuild setup). Real desktop visual review is not implied by headless geometry/interaction tests.

## Manual acceptance

1. Use a disposable profile with1 RP (the default-off development XP grant is available in Main/Campaign).
2. Open Campaign → Research. Select Battle Tower Weapons I, inspect +5% and1 RP cost, purchase; node/button show RESEARCHED and RP falls once.
3. Start either level. Battle build preview shows32 damage. Place it: current32 / next35 / gain(+3). Buy the next tier:35 damage. Earned ranks continue stacking through Stage03 then04.
4. Restart/select the same profile: upgrade remains owned. Another profile or profileless F6 has original damage. Existing Citadel Master I still applies independently.
5. Review the three-node graph at your normal resolution and confirm initial/reset fit, labels, pan/zoom and keyboard selection.

Mission XP scoring and automatic rewards remain deferred. No extra research nodes, refunds or campaign-save mechanics were added.
