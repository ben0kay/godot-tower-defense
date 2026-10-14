# Pause Menu — Future phase

Not implemented yet. The Ctrl build tray in gameplay is **not** a pause menu. The future Pause menu will allow Resume, Save (only after real campaign persistence exists), Return to Main Menu and Quit using `UIButtonFactory` for shared styling.

Set a Pause UI branch to process while the world is paused, avoid stuck paused trees on scene exit, and route scenes via `MenuNavigation`. Preserve the direct `Main.tscn` playtest path without requiring profile/save setup. Saving must call `SYSTEMS/Saving/CampaignSession` rather than serializing Godot nodes or placing write logic in the UI.
