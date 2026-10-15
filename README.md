# Godot Tower Defense — C#/.NET

A 2D top-down sci-fi tower-defense game in Godot 4.7.x. An editable Level001 map, enemy spawning and A* routing, finite manual waves, Battle/Laser/Blaster combat, shared instant area damage, wallet/kill credits, Citadel HP and the eight-stage tower-stat foundation are implemented.

## Launch

Build C# in the Godot .NET editor, then press **F5**. The project starts at **Boot**, where you create/select a local profile. Select **Campaign** in Main Menu, then **Play Level 001** to enter the existing battlefield. Profiles persist to Godot `user://Profiles/profiles.json` (versioned JSON with backup recovery). **Campaign/gameplay saves are not yet implemented**: each deployment starts a fresh level session.

For rapid map or combat testing, open `Main.tscn` and press **F6** (Run Current Scene). This bypasses profile/menus and remains valid for headless integration checks.

## Game controls

- WASD/arrows: move camera; middle mouse: pan; mouse wheel: zoom.
- Ctrl: toggle tower build tray. Choose Battle, Laser or Blaster Cannon, left-click valid cells to place, right-click/Escape to cancel; select towers to inspect them.
- F2: cycle the enemy catalog through the level's debug system.
- Start/Next Wave: use the top-right wave button.

## Configuration and structure

- `UI/DefaultUIConfig.tres`: HUD layout, palette and common button colors.
- `UI/Shared/UIButtonFactory.cs`: consistent button styling across menus, HUD and dialogs.
- `UI/Menus/`: Boot, Main, Campaign and future Pause.
- `SYSTEMS/Profiles/`: local profile records and persistence.
- `SYSTEMS/Saving/`: planned campaign persistence boundaries (not implemented yet).
- `LEVELS/Level001/`: editable terrain, portal/objective configuration and finite waves.
- `WORLD/`, `ENEMIES/`, `TOWERS/`, `ECONOMY/`: reusable gameplay systems.

Use feature READMEs for detailed implementation and development plans, especially `TOWERS/README.md`, `LEVELS/README.md`, `UI/Menus/README.md`, `SYSTEMS/Saving/README.md` and `NOTES/CodePreferences.md`. Run `TESTS/EnemyIntegrationChecks.tscn` in the Godot .NET headless runner for gameplay regression checks.
