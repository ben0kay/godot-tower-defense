# UI — Shared menus and live gameplay HUD

`UIMasterConfig.cs` / `DefaultUIConfig.tres` provide the Inspector-editable common HUD palette/layout. `Shared/UIButtonFactory.cs` centralizes styling and creation of all interface buttons; `Shared/MenuLayout.cs` provides consistent menu panels.

- `Menus/Boot/` is the active F5 startup for creating/selecting locally persistent profiles.
- `Menus/Main/` shows the active profile and Campaign / Change Profile / Exit Game.
- `Menus/Campaign/` offers **Play Level 001 / Play Level 002**; later it will lead to a Levels selection screen.
- `Menus/MenuNavigation.cs` owns scene switching; `Menus/Pause/` supplies the level-owned Escape pause overlay with Resume, Quit to Main Menu and Exit Game; Save/Load/Options are disabled placeholders.
- `GameplayHUD/` remains the actual combat interface under `Main.tscn/UI`: credits, health, waves, Ctrl tower tray and bottom-right inspector. Existing .tscn button nodes receive shared factory styles without recreating signals or changing gameplay.

The gameplay world is intentionally not loaded by Boot/Main/Campaign menus. Open `Main.tscn` and run current scene (F6) for direct editor testing; F5 launches the Boot flow. Profile persistence lives in `SYSTEMS/Profiles/`; **campaign/game saves are not implemented** (planned in `SYSTEMS/Saving/`).
