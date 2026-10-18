# UI — Shared menus and live gameplay HUD

## Research-first extension — 2026-10-17

Menus/Campaign/Research now supplies Campaign-only persistent Research purchases and a hex graph. No gameplay/Results access. Results shows actual paid placement, construction completion, upgrade purchases and unpaused duration via LEVELS/Session/LevelRunStats. Mission XP rewards remain deferred; Main/Campaign XP displays remain profile-owned.

---

`UIMasterConfig.cs` / `DefaultUIConfig.tres` provide the Inspector-editable common HUD palette/layout. `Shared/UIButtonFactory.cs` centralizes styling and creation of all interface buttons; `Shared/MenuLayout.cs` provides consistent menu panels.

- `Menus/Boot/` is the active F5 startup for creating/selecting locally persistent profiles.
- `Menus/Main/` shows the active profile and Campaign / Change Profile / Exit Game.
- `Menus/Campaign/` offers **Play Level 001 / Play Level 020**; later it will lead to a Levels selection screen.
- `Menus/MenuNavigation.cs` owns scene switching; `Menus/Pause/` supplies the level-owned Escape pause overlay with Resume, Quit to Main Menu and Exit Game; Save/Load/Options are disabled placeholders.
- `GameplayHUD/` remains the actual combat interface under `Main.tscn/UI`: credits, health, waves, Ctrl tower tray and bottom-right inspector. Existing .tscn button nodes receive shared factory styles without recreating signals or changing gameplay.
- `Alerts/` is the shared event-driven major-alert + toast foundation ported from the old isometric 2D game. `GameplayHUD.tscn` instantiates its separate overlay (no gameplay triggers yet); configure it through `Alerts/NotificationSettings.tres`, and press F7 in a DEBUG game for a harmless visual preview. See [Alerts README](Alerts/README.md).

The gameplay world is intentionally not loaded by Boot/Main/Campaign menus. Open `Main.tscn` and run current scene (F6) for direct editor testing; F5 launches the Boot flow. Profile persistence lives in `SYSTEMS/Profiles/`; **campaign/game saves are not implemented** (planned in `SYSTEMS/Saving/`).
