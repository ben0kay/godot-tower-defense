# Catalog build tray — Phase 4B

TowerBar.tscn contains a header and horizontal ScrollContainer/Buttons row. GameplayHUD.BindCatalog creates UIButtonFactory TowerTile buttons from validated available definitions; no hardcoded IDs, names, costs, stats or prototype controls live in this scene. DefaultTowerCatalog currently exposes Battle Tower only.

Ctrl toggles the existing slide/fade independently from the Inspector. The scroll container clips large rosters inside the configured tray width; buttons keep focus/navigation support. Available=false entries are hidden and rejected by placement. An empty catalog displays a safe message. Registration edits require Catalog.Rebuild followed by BindCatalog, rather than polling/rebuilding every frame. GetBuildButton(typeId) is the stable action lookup.
