# Tower Inspector

`InspectorPanel.tscn` is a modular panel at the **lower right** of the HUD. It can be shown by build-tray selection or, later, by clicking an already placed tower.

Contains name, tower role/category, formatted stats, description and independent X close button. Keep the Inspector visible even if the Ctrl build tray closes. Avoid creating separate Inspectors for build-preview vs placed towers; feed both into `GameplayHUD.ShowTowerDetails` initially.

Paid upgrade controls are active; sell/targeting controls remain future work. Any `TBD` values shown by prototype buttons are deliberate placeholders.

## Paid tower upgrades
The existing panel now includes UpgradeInfo and UpgradeButton. The placement controller supplies next-tier effective changes, shared resource costs, affordability and max-tier state; the HUD emits TowerUpgradeRequested and never spends currency or owns progression. Build previews/prototype selections hide upgrade controls. The panel grows upward for the preview while the bottom tray retains its configured height and independent Ctrl animation. Closing the panel remains independent.

The existing Inspector X uses `UIButtonStyle.Compact` through the common factory; it remains an Inspector-owned close action. Upgrade uses UIButtonStyle.Action through the same factory; Sell remains future work.
