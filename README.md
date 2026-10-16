# Godot Tower Defense — C#/.NET

A 2D top-down sci-fi tower-defense game in Godot 4.7.x. Editable Level001 and five-portal Level002 maps, enemy spawning and A* routing, finite manual waves, Battle/Laser/Blaster combat, shared instant area damage, wallet/kill credits, Citadel HP and the eight-stage tower-stat foundation are implemented.

## Launch

Build C# in the Godot .NET editor, then press **F5**. The project starts at **Boot**, where you create/select a local profile. Select **Campaign** in Main Menu, then **Play Level 001** or **Play Level 002** to enter the selected battlefield. Profiles persist to Godot `user://Profiles/profiles.json` (versioned JSON with backup recovery). **Campaign/gameplay saves are not yet implemented**: each deployment starts a fresh level session.

For rapid map or combat testing, open `Main.tscn` (Level001) or `MainLevel002.tscn` (Level002) and press **F6** (Run Current Scene). This bypasses profile/menus and remains valid for headless integration checks.

## Game controls

- WASD/arrows: move camera; middle mouse: pan; mouse wheel: zoom.
- Ctrl: toggle tower build tray. Choose Battle, Laser or Blaster Cannon, left-click valid cells to place, right-click/Escape to cancel; select towers to inspect them.
- Escape: cancel active placement first; otherwise open/resume Pause. Save/Load/Options are disabled placeholders.
- F11: toggle fullscreen everywhere, restoring windowed/maximized mode. Shared shortcuts live in `CONTROLS/`.
- F2: cycle the enemy catalog through the level's debug system.
- F3: open Route Preview; choose portal/enemy/path samples and click Simulate Paths (debug only).
- Start/Next Wave: use the top-right wave button.

## Configuration and structure

- `UI/DefaultUIConfig.tres`: HUD layout, palette and common button colors.
- `UI/Shared/UIButtonFactory.cs`: consistent button styling across menus, HUD and dialogs.
- `UI/Menus/`: Boot, Main, Campaign and level-owned Pause.
- `SYSTEMS/Profiles/`: local profile records and persistence.
- `SYSTEMS/Saving/`: planned campaign persistence boundaries (not implemented yet).
- `LEVELS/Level001/`: editable terrain, portal/objective configuration and finite waves.
- `WORLD/`, `ENEMIES/`, `TOWERS/`, `ECONOMY/`: reusable gameplay systems.

Use feature READMEs for detailed implementation and development plans, especially `TOWERS/README.md`, `LEVELS/README.md`, `UI/Menus/README.md`, `SYSTEMS/Saving/README.md` and `NOTES/CodePreferences.md`. Run `TESTS/EnemyIntegrationChecks.tscn` in the Godot .NET headless runner for gameplay regression checks.

## Long-term vision — optional multi-genre game (planning only)

**Current priority: build, polish and complete Tower Defense as a self-contained, fully playable game.** Do **not** reorganize or prematurely extract its systems for this vision. All TD code, gameplay, saving work, assets and **UI** can remain local to this repository and be developed normally. This is a future direction, **not** a request to implement Idle Clicker, Space Shooter or cross-genre infrastructure now.

The eventual concept is **one persistent sci-fi campaign / empire** with three connected gameplay modes:

- **Idle Clicker / Empire Management (eventual main hub):** mine/process resources, accumulate Credits, run factories, manufacture towers/ships, and progress research. Offline progress is computed from saved state and elapsed time when the campaign is reopened (with production/storage constraints), without running the game continuously in the background.
- **Tower Defense (this project, the present focus):** defend levels using manufactured equipment and research bonuses from the future empire. For example, manufacturing five Battle Towers puts five in persistent inventory; **successfully deploying** one deducts one from that available inventory. Failed/cancelled placement must not consume stock. Tower losses, mission rewards and other persistence rules will be decided when integration is actually designed.
- **Space Shooter (future):** an optional action-oriented mode for exploring space, mining resources directly and fighting enemies. Camera/genre (2D top-down versus first-person/3D) can be decided later. Its rewards would feed the same campaign economy.

### Development and future integration principles

1. **Do Tower Defense first.** Continue building towers, enemies, levels, movement, interactive terrain, construction, ammunition, upgrades, UI, performance and testing without waiting for the other genres. The present game must stay runnable independently for development/testing.
2. **Keep the current UI here.** Menus, HUD, Inspector, build tray, visual theme and shared UI helpers should continue to evolve inside TD. **Only later**, if/when other genres exist and consistent presentation is desirable, extract or share suitable UI components/theme resources across modes. Do not move or refactor UI into a cross-game framework today.
3. **Preserve sensible boundaries, not speculative abstractions.** Continue using stable tower/resource IDs, separate authored definitions from per-instance state, reuse the existing transaction/economy and stat-pipeline ownership, and avoid tightly coupling tower combat to menus or a hypothetical Idle system. No new general-purpose cross-genre managers, interfaces or folder hierarchy are required now.
4. **Integrate through a shared campaign state later.** When development actually reaches the hybrid phase, the persistent campaign should own resource balances, manufactured inventory, research, progression and cross-mode rewards. Each mode should consume/return explicit campaign data rather than maintaining conflicting copies. TD can retain a **standalone/test mode** with its current starting resources and placement, alongside a future campaign-connected mode.
5. **Saving comes in stages.** Current local profiles are not campaign saves; follow \`SYSTEMS/Saving/README.md\` when implementing TD saves. Design persistence with stable IDs and clean level/session boundaries so eventual offline progression and cross-genre inventories can be added without serializing live scene nodes or cached derived stats. Do not build an offline simulator until the Idle mode exists.
6. **Avoid a late three-game merge.** If the larger vision is pursued, build one modest end-to-end connection at a time: resource production → tower inventory → TD deployment → mission results; add research and Shooter integration afterward. The TD implementation should not be rewritten wholesale to achieve this.

**Bottom line:** Treat this repository as the complete Tower Defense game **today**, with the *option* to become the Tower Defense module within a broader multi-genre game **later**. This vision must not block or complicate current TD work.
