# Campaign Research Room

## Battle Tower Weapons I extension — 2026-10-17

The Research Room now contains **three real nodes**, including battle_weapons_1 / Battle Tower Weapons I (cost1 RP, prerequisite research_core, axial -1,0). It permanently grants +5% Battle Tower damage in future deployments. The stored purchased ID uses existing progression v2; previous saves and Citadel research stay compatible.

TowerDamagePercent effects carry a stable TargetTowerId (battle for this node). ResearchDeployment resolves purchased IDs once at mission initialization; CitadelSession owns this immutable snapshot. Paid placement/build previews take their type's bonuses from it; TowerBase retains those values for rank/config/paid-level rebuilds. Stage04 performs arithmetic only and never loads profiles or Resources. Live missions retain their captured bonuses even if selection changes externally; a fresh mission captures current research.

Integer projectile damage rounds nearest/ties-to-even after Stage03 rank rounding: standard-rank Battle levels1/2/3 are32/35/38 rather than30/33/36. Authored source rows, costs, ammo and heat are not modified. Build Inspector and next-tier previews use the same researched values. Only Battle's node is authored; additional approved effects/nodes remain future work. Mission XP scoring/replays/once-only rewards remain deferred.

Graph reset/initial resize now fits the authored nodes and balances long names across two lines. Test/authoring details: TESTS/Research/BATTLE_RESEARCH.md. Original notes below remain for history; statements that tower research is neutral/future-only are superseded by this extension.

---

ResearchMenu.tscn is a normal out-of-gameplay Control scene. Entry: Campaign → RESEARCH. Exit: BACK TO CAMPAIGN. It does not appear in Results, Pause, Main or gameplay. A profileless direct launch returns to Boot.

ResearchGraph is one clipped, event-driven custom drawing surface (not a node per hex). It draws real catalog nodes and prerequisite connectors from axial coordinates, using the same graph/screen transforms and hex geometry for drawing and hit tests. Drag background or middle-drag to pan; wheel zooms around the cursor (0.45–1.8); arrows browse nodes, Tab reaches Purchase/Back, Home or RESET VIEW restores the initial view. Selection never spends RP.

The right scrolling information column shows name, description, exact effect, prerequisites, cost/balance and LOCKED / AVAILABLE / INSUFFICIENT RP / RESEARCHED. Purchase requests go through ResearchService's active-scene/eligibility checks; saves commit before balances/ownership refresh. Colors, explicit text and selected outlines distinguish states. Catalog/load/save errors are readable and Back remains usable. No debug-grant button in this room; enable the default-off DEBUG XP grant in Main or Campaign to test with a disposable profile.

Graph source: SYSTEMS/Progression/Research/DefaultResearchCatalog.tres. Research Core is virtual/free; Citadel Master I is the first purchased node. Future nodes use catalog data and explicit mechanics handlers. Deployment resolves purchased bonuses; Controls never change authored level/tower resources or calculate live combat stats.

Headless geometry checks cover 1280×720, 960×600 and 800×600; real desktop appearance still needs review. See TESTS/Research/README.md.
