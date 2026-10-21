# Progression debug action

PlayerProgressionPanel.Debug.cs contains the optional development XP-grant button. It extends the existing partial menu panel, keeping the attachment hook and normal display in UI/Menus. Main/Campaign Inspector opt-in stays off by default. It grants real saved XP and remains absent in release builds. This is the existing behavior, moved to the debug folder.
