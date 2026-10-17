# UI tests

HUD, build tray, menu flow, shared controls/Pause and Mission Debrief. Standalone runners: MenuTowerFlowChecks.tscn, PauseControlsChecks.tscn, ResultsChecks.tscn, ResultsExitChecks.tscn and HudCategoryChecks.tscn.

See [Results verification](Results.md) for R1 scenarios, commands and actual build/suite limitations, and [the test index](../README.md) for other suites.

See [HUD category/layout verification](HudCategories.md) for tabs, compact scrolling Inspector, transparency and the40-entry tray regression.

`CompactInspectorChecks.tscn` is the focused620px non-scrolling Inspector runner. It reuses InspectorTargetingChecks layout coverage, verifies all building/max/busy states at three viewport sizes and asserts content-sized numeric columns on the vector-divider grid. See InspectorTargeting.md for executed results.
