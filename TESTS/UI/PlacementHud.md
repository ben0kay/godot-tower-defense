## Compact bottom-docked tray and Inspector — 2026-10-19 (code change; runtime verification pending)

The bottom HUD panels now share the **true bottom edge of the gameplay viewport** (0 px exterior bottom offset; side margins remain). Their Inspector-configurable nominal sizes are **224 px build tray** (previously 260) and **252 px Inspector** (previously 330), with unchanged Inspector **560 px** width and unchanged tray/Inspector horizontal division. This trims empty space and exposes more terrain. The Inspector is still a non-scrolling `PanelContainer`: **its natural content minimum may expand it upward for unusually dense upgrade/rank/heat/construction text**, rather than clipping buttons. The left build tray remains an independent fixed-height scroll browser aligned to the same bottom edge.

Build-tile buttons are **56 px** high (previously 65), with exactly two rows and their horizontal scrollbar preserved. The tray inner margins are 8px top/bottom (previously 10), and horizontal `Scroll` reserve is 138px (previously 150). No category buttons, text sizes, scrolling direction, focus behavior or tower stats changed. The existing Ctrl slide/fade, placeholder Inspector shell, hover preview, hide-on-placement and click-to-upgrade paths are unchanged.

`UIMasterConfig.BottomBarHeight` and `InspectorHeight` remain editable in `DefaultUIConfig.tres` Inspector. Scene defaults in `TowerBar.tscn`, `InspectorPanel.tscn` and `GameplayHUD.tscn` match the new runtime arrangement. Geometry assertions now check that **both bottom panel edges** meet the viewport boundary and that the two-row catalog remains vertically contained, for 1920×1080, 1366×768, and 960×540.

**Verification:** The C# and scene changes plus regression expectations are authored but **not Godot-built or run** by this editing environment. Run `PlacementHudChecks.tscn`, `TowerUpgradeChecks.tscn`, `CompactInspectorChecks.tscn`, `HeatInspectorChecks.tscn`, `HudCategoryChecks.tscn`, and inspect Ctrl/hover/heat/upgrade at desktop sizes before marking validated. Previous 560×330 / 260px notes below are historical.

---

## Stable two-row build browser — 2026-10-17

- Ctrl opening now clears the old tower selection and shows the **empty Inspector shell immediately**. Hover/focus fills the same reserved panel with existing build details/cost/affordability; no tile can be displaced by the panel appearing.
- Tray width **always reserves Inspector width plus the existing gutter**, including while details are hidden. The default 560×330 Inspector scene is unchanged. The tray uses two rows of catalog tiles; further columns scroll horizontally. Category filters recompute columns from visible entries and reset horizontal scroll; keyboard focus follows off-screen tiles.
- The shared BottomBarHeight default is now 260px to fit header, tabs, two 65px tile rows and the horizontal scrollbar. Width remains responsive in UIMasterConfig/GameplayHUD. A compact-width header hides its hint when needed.
- Affordable choice still hides the lower HUD while placing. RMB/Escape/success restore the tray plus blank shell; Ctrl closes. Successful placement remains unselected. IsInspectionVisible continues to mean **active details**, not merely the empty shell, preventing stale tower events from refilling it.
- Verification: SDK build, PlacementHudChecks (both maps plus real Rocket mouse hit/click at 1920×1080,1366×768,960×540; blank shell/stable panel bounds; 40 entries/two-row horizontal overflow/category reflow) CompactInspectorChecks and HudCategoryChecks (including the dynamic 40-type tray) passed. Desktop visual review is still pending.

This amendment supersedes older statements that cancellation must hide the entire Inspector panel. No shield/combat/research/scoring/Inspector-art changes are included.

---

# Placement HUD / Inspector separation — verification

## Contract

Ctrl opens the existing build tray. Hover/focus a tower tile to see its compact definition, cost and affordability in the existing right Inspector. Browsing does not arm/place/pay. Clicking an unaffordable tile leaves these panels visible and emits BuildDenied(towerId, reason), ready for an audio subscriber later. An affordable click hides BottomArea (tray + Inspector), retaining logical tray-open state and permanent top status. Actual world placement still validates resources/occupancy/routes and commits payment once.

RMB/Escape cancel the ghost, clear build selection/preview and restore the tray. Ctrl cancels and closes the menu; reopening does not resume. Successful placement ends placement and restores the tray without auto-selection or Inspector. A later independent click inspects normally. Same-frame follow-up selection and stationary-cursor hover on restored panels cannot reopen it.

No InspectorPanel.tscn, UIMasterConfig.cs or DefaultUIConfig.tres changes. The latest560×330 visual resize, fonts, right column, rank/heat ordering and styles remain. Only build-preview content/affordability wrapping and temporary visibility change.

## Dedicated runner

res://TESTS/UI/PlacementHudChecks.tscn uses actual catalog button signals, wallet, controller, ghost, world placement and scoped controls on both launchers. It covers hover/focus, visible costs/affordability updates, no-payment rejection and future-audio signal, hidden lower HUD, invalid ground attempts, RMB cancellation, Ctrl close/reopen, one-shot successful placement, same-frame selection guard, stationary-hover suppression and separate later inspection. All registered build previews are checked at1920×1080,1366×768 and960×540 against the same560×330 panel bounds. Tests do not write profile or campaign files.

## Actual checks in this environment

C# source-generator compilation uses Roslyn4.11, Godot4.7.2 Mono and net8 reference assemblies. The local checkout was updated to GitHub main including the recent Inspector visual resize before applying/merging placement changes. Passed: PlacementHudChecks, CompactInspectorChecks, HeatInspectorChecks, MenuTowerFlowChecks, PauseControlsChecks. Existing compact/max/busy/heat assertions report width560 and max height330 for all three test viewports.

Passed gameplay regressions: ResultsChecks (outcomes/reveal plus actual Retry/Next/Menu), ConstructionChecks, FootprintChecks, BattleResearchChecks and ResearchFoundationChecks. AmmunitionLoopChecks reported passes for its stock/weapon, conservation, delivery-safety and real-physics production/delivery/pause subchecks, but the final suite completion banner was not observed; no full-suite pass is claimed for that runner. Serial re-runs were used for the timed construction/ammunition runners. Standard SDK build is not claimed; the established source-generator compiler and Godot headless runner are used. Headless interaction/geometry checks do not certify real desktop mouse feel or appearance.

Legacy subsystem fixtures that need an inspected tower now explicitly select after paid placement as a separate test action. They do not restore auto-selection in production. The footprint-preview regression checks the red ghost plus cached PreviewFailure rather than expecting a now-hidden Inspector message.

## Manual review

Open Ctrl, hover/focus different towers and inspect their cost/affordability. Click an affordable tile: lower UI disappears. Move/cancel with RMB and close/reopen with Ctrl. Place once: tray returns, Inspector stays closed, ghost ends. Click the placed tower separately to inspect. Try an unaffordable tile and an invalid world cell; no money/tower state changes on failure. Confirm the recent resized Inspector still looks as expected. Future audio can subscribe to BuildDenied; this pass adds no sounds.
