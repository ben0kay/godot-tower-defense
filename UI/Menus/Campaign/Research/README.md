# Campaign Research Room

ResearchMenu.tscn is a normal out-of-gameplay Control scene. Entry: Campaign → RESEARCH. Exit: BACK TO CAMPAIGN. It does not appear in Results, Pause, Main or gameplay. A profileless direct launch returns to Boot.

ResearchGraph is one clipped, event-driven custom drawing surface (not a node per hex). It draws real catalog nodes and prerequisite connectors from axial coordinates, using the same graph/screen transforms and hex geometry for drawing and hit tests. Drag background or middle-drag to pan; wheel zooms around the cursor (0.45–1.8); arrows browse nodes, Tab reaches Purchase/Back, Home or RESET VIEW restores the initial view. Selection never spends RP.

The right scrolling information column shows name, description, exact effect, prerequisites, cost/balance and LOCKED / AVAILABLE / INSUFFICIENT RP / RESEARCHED. Purchase requests go through ResearchService's active-scene/eligibility checks; saves commit before balances/ownership refresh. Colors, explicit text and selected outlines distinguish states. Catalog/load/save errors are readable and Back remains usable. No debug-grant button in this room; enable the default-off DEBUG XP grant in Main or Campaign to test with a disposable profile.

Graph source: SYSTEMS/Progression/Research/DefaultResearchCatalog.tres. Research Core is virtual/free; Citadel Master I is the first purchased node. Future nodes use catalog data and explicit mechanics handlers. Deployment resolves purchased bonuses; Controls never change authored level/tower resources or calculate live combat stats.

Headless geometry checks cover 1280×720, 960×600 and 800×600; real desktop appearance still needs review. See TESTS/Research/README.md.
