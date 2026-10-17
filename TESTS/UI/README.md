# UI tests

## Placement HUD runner — 2026-10-17

Added PlacementHudChecks.tscn for hover/focus/cost/affordability, rejection signal, hidden lower HUD, RMB/Escape/Ctrl behavior, one-shot placement without selection, independent later inspection and all-type560×330 preview bounds. Existing shared test helpers now explicitly select towers when testing their Inspector; production placement no longer auto-selects. See PlacementHud.md for actual execution and remaining desktop checks.

---

HUD, build tray, menu flow, shared controls/Pause and Mission Debrief. Standalone runners: MenuTowerFlowChecks.tscn, PauseControlsChecks.tscn, ResultsChecks.tscn, ResultsExitChecks.tscn and HudCategoryChecks.tscn.

See [Results verification](Results.md) for R1 scenarios, commands and actual build/suite limitations, and [the test index](../README.md) for other suites.

See [HUD category/layout verification](HudCategories.md) for tabs, compact scrolling Inspector, transparency and the40-entry tray regression.

`CompactInspectorChecks.tscn` is the focused560px non-scrolling Inspector runner. It reuses InspectorTargetingChecks layout coverage, verifies all building/max/busy states at three viewport sizes and asserts content-sized numeric columns on the vector-divider grid. See InspectorTargeting.md for executed results.

`HeatInspectorChecks.tscn` covers HD3 real heat/disable events, threshold cooling, repeated reason counts, heatless towers, selection/close/reselect, pause, paid capacity changes, queued callback unload and560×330 target worst-state geometry. See HeatInspector.md for executed regression results and manual QA limits.

The 560px/relocated Heat update is committed but its Godot headless and desktop visual reruns are **pending**; historical verification records document the earlier 620px geometry.
