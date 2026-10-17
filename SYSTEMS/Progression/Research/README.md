# Permanent Research — first playable foundation

**Implemented:** free research_core at axial (0,0), citadel_master_1 at (1,0), cost 1 RP, permanent +10% Citadel maximum/current starting health on new deployments. Campaign is the only Research entry; Results/gameplay have none.

## Ownership and authoring

- ResearchDefinition.cs: Inspector-authored stable ID, name, description, RP cost, prerequisite IDs, axial coordinate and explicit typed effect/percentage.
- DefaultResearchCatalog.tres: the two real node definitions. No fake 100-node tree.
- ResearchCatalog.cs: startup/load validation, immutable runtime nodes, unique IDs/positions, single free root, known dependencies and acyclic graph. Paid nodes require positive cost and a real supported effect.
- ResearchRules.cs: pure state/payment and Citadel health calculation. Core is always researched/free, never a paid saved ID. Multiple Citadel percentage effects add against the authored baseline and resolve once; they never repeatedly multiply current health.
- ResearchService.cs: cached validated catalog, active-room purchase guard and deployment effect accessor. Only the actual current ResearchMenu scene can issue purchases. ProgressionService commits RP plus owned IDs atomically before notifying UIs.

Persistent data belongs to SYSTEMS/Progression, not Controls. Store IDs only; recompute effects from current catalog on deployment. Existing v1 progression migrates to v2 with empty purchases and no XP/RP reset. UI/load/save failures remain visible; irrecoverable files are preserved.

## Adding real upgrades later

Add one ResearchDefinition resource to the catalog with a unique stable ID, axial coordinate and prerequisite IDs. Six neighbor directions are (1,0), (1,-1), (0,-1), (-1,0), (-1,1), (0,1); positions and connectors are derived from data, never hardcoded per-node screen coordinates. Description/cost/state panels and graph paint/hit tests automatically use catalog entries.

Reuse supported effect kinds for repeated mechanics. For a genuinely new mechanic add an explicit effect resolver and its tests. **Battle Tower damage +5% is an example for a later node, not implemented here.** Tower research must use the neutral TOWERS/Shared/Stats/Pipeline/04_PermanentResearch stage with cached profile-effect snapshot invalidation, preserving authored *Levels.cs rows. Do not use callbacks/function pointers in saved JSON, per-frame profile loads or a giant reflection framework.

Catalog is frozen per application process; restart after authoring. Shipped catalog edits affecting saved IDs require a deliberate migration/balance decision. No refunds/respec, equipped doctrines, research timers, tower unlocks or mission rewards in this pass.

Verification: TESTS/Research/README.md. Revised sequence: ../PLAYER_XP_RESEARCH_FOUNDATION_PASS.md.
