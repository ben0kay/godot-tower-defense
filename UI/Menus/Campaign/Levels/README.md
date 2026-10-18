# Campaign Levels

`LevelSelectionMenu.cs/.tscn` owns the mission-selection screen reached from Campaign → LEVELS. It requires the selected local profile, reuses MenuLayout/UIButtonFactory, and launches the same Level001 / Level002 battlefield scenes through MenuNavigation. Back returns to the Campaign hub; missing profiles redirect to Boot.

This is the existing two-mission chooser moved out of the hub. Both missions remain available. No completion stars, locked missions, campaign slots, Continue/Load or unlock progression are invented. Maps and mission settings remain in LEVELS; profile XP/research persistence remains in SYSTEMS/Progression. Research is accessed through the hub between deployments.
