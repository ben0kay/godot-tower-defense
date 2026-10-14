# Shared UI — Active Button Factory and Menu Layout

`UIButtonFactory.cs` implements `UIButtonStyle` variants: `Menu`, `Action`, `TowerTile`, `Compact`, `Danger`. `Create(text, action, style, height, config)` creates a new Godot Button for dynamic menus; `Apply(existingButton, style, config)` restyles an existing .tscn Button **without replacing its node or binding/calling game actions**. This includes dialog buttons and gameplay-HUD controls.

The factory uses `UI/DefaultUIConfig.tres` / `UIMasterConfig` for the existing palette and consistent normal/hover/pressed/disabled/focus appearance. Leave signal ownership to the screen, HUD or gameplay service. Avoid attaching button actions inside theme helpers. Don't create a second UI theme/color owner.

`MenuLayout.cs` builds a common responsive centered panel and background for Boot, Main and Campaign, with labels/containers that can be extended without altering underlying gameplay.

**HUD migrated:** `UI/GameplayHUD/GameplayHUD.cs` applies `UIButtonStyle.Action` to Start Wave, `TowerTile` to the six existing build buttons, and `Compact` to Inspector X. Existing scene paths, settings-driven dimensions, wave/placement callbacks, inspector and health displays are preserved. Panels and HUD-only status labels retain their own layout rules. New UI buttons should use this factory; non-button controls need not.
