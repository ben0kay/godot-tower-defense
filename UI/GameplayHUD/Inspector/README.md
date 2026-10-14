# Tower Inspector

`InspectorPanel.tscn` is a modular panel at the **lower right** of the HUD. It can be shown by build-tray selection or, later, by clicking an already placed tower.

Contains name, tower role/category, formatted stats, description and independent X close button. Keep the Inspector visible even if the Ctrl build tray closes. Avoid creating separate Inspectors for build-preview vs placed towers; feed both into `GameplayHUD.ShowTowerDetails` initially.

No real upgrade/sell/targeting controls yet. Any `TBD` values shown by prototype buttons are deliberate placeholders.

The existing Inspector X uses `UIButtonStyle.Compact` through the common factory; it remains an Inspector-owned close action. Future Upgrade/Sell controls should reuse `UIButtonFactory` and activate only when their actual gameplay systems are ready.
