## Compact bottom-docked tray and Inspector — 2026-10-19 (code change; runtime verification pending)

The bottom HUD panels now share the **true bottom edge of the gameplay viewport** (0 px exterior bottom offset; side margins remain). Their Inspector-configurable nominal sizes are **224 px build tray** (previously 260) and **252 px Inspector** (previously 330), with unchanged Inspector **560 px** width and unchanged tray/Inspector horizontal division. This trims empty space and exposes more terrain. The Inspector is still a non-scrolling `PanelContainer`: **its natural content minimum may expand it upward for unusually dense upgrade/rank/heat/construction text**, rather than clipping buttons. The left build tray remains an independent fixed-height scroll browser aligned to the same bottom edge.

Build-tile buttons are **56 px** high (previously 65), with exactly two rows and their horizontal scrollbar preserved. The tray inner margins are 8px top/bottom (previously 10), and horizontal `Scroll` reserve is 138px (previously 150). No category buttons, text sizes, scrolling direction, focus behavior or tower stats changed. The existing Ctrl slide/fade, placeholder Inspector shell, hover preview, hide-on-placement and click-to-upgrade paths are unchanged.

`UIMasterConfig.BottomBarHeight` and `InspectorHeight` remain editable in `DefaultUIConfig.tres` Inspector. Scene defaults in `TowerBar.tscn`, `InspectorPanel.tscn` and `GameplayHUD.tscn` match the new runtime arrangement. Geometry assertions now check that **both bottom panel edges** meet the viewport boundary and that the two-row catalog remains vertically contained, for 1920×1080, 1366×768, and 960×540.

**Verification:** The C# and scene changes plus regression expectations are authored but **not Godot-built or run** by this editing environment. Run `PlacementHudChecks.tscn`, `TowerUpgradeChecks.tscn`, `CompactInspectorChecks.tscn`, `HeatInspectorChecks.tscn`, `HudCategoryChecks.tscn`, and inspect Ctrl/hover/heat/upgrade at desktop sizes before marking validated. Previous 560×330 / 260px notes below are historical.

---

# Catalog build tray — Phase 4B

TowerBar.tscn contains a header, horizontally scrollable category tabs and a horizontal ScrollContainer/Buttons row. GameplayHUD.BindCatalog creates UIButtonFactory TowerTile buttons from validated available definitions; no hardcoded IDs, names, costs, stats or prototype controls live in this scene. All available registered towers/buildings appear automatically, grouped by TowerDefinition.Category. All is the initial filter; only populated categories have tabs, with available-building counts. Selected category survives Ctrl toggling in the current level. Switching category cancels pending placement without closing the tray or clearing placed-tower inspection; rebinding removes stale tabs and falls back to All when needed.

Ctrl toggles the existing slide/fade independently from the Inspector. The scroll container clips large rosters inside the configured tray width; buttons keep focus/navigation support. Available=false entries are hidden and rejected by placement. An empty catalog displays a safe message. Registration edits require Catalog.Rebuild followed by BindCatalog, rather than polling/rebuilding every frame. GetBuildButton(typeId) is the stable action lookup.

The lower panel uses UIMasterConfig.BottomPanelOpacity (90% default) on its background only. Text/icons/buttons retain opacity and normal input blocking. BottomBarHeight defaults to176px to accommodate the compact tabs; the roster remains horizontally scrollable. See `TESTS/UI/HudCategories.md`.
