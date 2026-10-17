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
