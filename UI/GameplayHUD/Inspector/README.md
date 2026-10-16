# Tower Inspector

`InspectorPanel.tscn` is a modular panel at the **lower right** of the HUD. It is now392px wide ×360px high by default, with a vertical ScrollContainer at Insets. Long details scroll within that fixed height rather than growing over the battlefield. It can be shown by build-tray selection or by clicking an already placed tower.

Contains name, tower role/category, formatted stats, description and independent X close button. Keep the Inspector visible even if the Ctrl build tray closes. Avoid creating separate Inspectors for build-preview vs placed towers; feed both into `GameplayHUD.ShowTowerDetails` initially.

Paid upgrade controls are active; sell/targeting controls remain future work. Any `TBD` values shown by prototype buttons are deliberate placeholders.

## Paid tower upgrades
The existing panel now includes UpgradeInfo and UpgradeButton. The placement controller supplies next-tier effective changes, shared resource costs, affordability and max-tier state; the HUD emits TowerUpgradeRequested and never spends currency or owns progression. Build previews/prototype selections hide upgrade controls. The panel retains its bounded height for previews; all upgrade information/actions remain reachable in its scroll area, while the bottom tray retains its configured height and independent Ctrl animation. Closing the panel remains independent.

The existing Inspector X uses `UIButtonStyle.Compact` through the common factory; it remains an Inspector-owned close action. Upgrade uses UIButtonStyle.Action through the same factory; Sell remains future work.

## Earned Rank / XP

RankSection is inside this same panel, above the existing stats. Rank displays Standard/Veteran/Enhanced/Superior/Legendary; ExperienceBar and ExperienceText display within-rank progress/required XP, next named rank and total XP. RankBonuses shows only active Damage/Range totals. Legendary displays MAX RANK and a full bar. The existing stats retain Tower level and the same paid upgrade controls.

TowerPlacementController subscribes/unsubscribes ExperienceChanged alongside StatsChanged for the actual selected instance. XP-only events queue one coalesced deferred rank-section update rather than formatting/laying out the panel hundreds of times for one AoE. Promotions refresh cached stats/current upgrade preview and the rank section; previews never spend XP or Credits. Build previews hide rank data, selection switches to the new tower, and closing the panel remains independent of Ctrl. Colours/style use UIMasterConfig, matching existing HUD panels. No new rank window/button or per-frame progression polling exists.

InspectorWidth/InspectorHeight and BottomPanelOpacity live in UI/DefaultUIConfig.tres through UIMasterConfig. Height reduces to fit shorter viewports. Background opacity defaults to90%; foreground text/controls remain opaque. Existing Insets/Body label/button paths and event-driven rank/runtime updates are preserved. New selections reset scroll to the top; selected-only runtime updates do not reset it. Category filtering preserves placed-tower inspection.

## Work progress (C3)

The shared WorkSection shows CONSTRUCTING, UPGRADING to Level N or OPERATIONAL. Busy buildings show a compact progress bar, elapsed/total and remaining seconds. TowerPlacement observes only the selected visible building's work events (approximately10Hz); progress changes update these controls without touching combat caches, stat labels, upgrade previews or scroll position. Closing, changing selection and unloading detach listeners; reselecting reads the current state. Timed paid upgrades retain old active stats, show free target differences and “Cost already paid”, and disable the duplicate purchase button. The392×360 scrolling Inspector and subtle background remain unchanged. C4 construction artwork/particles are separate future presentation.
