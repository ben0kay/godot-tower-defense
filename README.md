## Terrain Edges scaffold — opt-in visual foundation (2026-10-18)

`WORLD/TERRAIN/Edges/` now defines editable edge-style resources, Mars material mappings, an on-demand material cache and a pure 8-neighbour mask. This is a foundation for Codex, **not live corner smoothing**: renderer draws nothing; existing levels and artwork are unchanged. See [the Codex handoff](WORLD/TERRAIN/Edges/IMPLEMENTATION_PLAN.md).

---

## Playable Level002 tutorial — 2026-10-18

Campaign now offers Level001, the newly painted **Level002** (four gentle unshielded waves), and preserved Level020. Open `MainLevel002.tscn` for direct F6 play. Level001 Next advances to002; no level skips to020. See `LEVELS/Level002/README.md` for exact composition/settings. Earlier unfinished-Level002 notes below are historical.

---

## Level002 moved to Level020; fresh Level002 design map — 2026-10-18

The Five Gates is now `LEVELS/Level020/Level020.tscn`, with `Level020Config.tres`, `Level020Waves.cs` and WaveSetId `level020`. Its terrain, object layout, wave composition and balance are preserved. Its standalone launcher/world variant are `MainLevel020.tscn` / `WORLD/WorldLevel020.tscn`; Campaign labels/launches Level020 and PlayableLevelCatalog registers numeric ID20. Scene/script UID sidecars are retained across renames. Tests and current documentation reference the moved map.

Fresh `LEVELS/Level002/Level002.tscn` is copied from the current LevelTemplate with a new root name and independent `Level002Config.tres` copied from DefaultLevelConfig. Empty terrain layers and template background/Portal/Citadel/checkpoint overlay are retained. The template itself is unchanged. Open this scene in Godot's 2D editor to design it. It has no authored waves or playable launcher yet and is not offered as a finished Campaign mission. Add those when the map is ready. Results cannot skip the missing playable Level002 to reach Level020; Level001's Next is disabled for now. Level020 remains available directly from Campaign and F6 on MainLevel020.

Earlier Level002/Five Gates references in historical notes below refer to the map now called Level020. No old design plan or map content was removed. Verification: see `TESTS/Integration/LevelReorganization.md`.

---

## Multi-Citadel HUD and F3 — MC3 (2026-10-18)

Two or more Citadels now divide the top-center HUD into equal-width health meters in one horizontal row. Single-Citadel layout is preserved. F3 previews and labels each portal's cached assigned destination, including secondary Citadels and checkpoint chains. Existing maps remain unchanged. See `UI/GameplayHUD/TopBar/README.md` and `TESTS/UI/MultiCitadelHud.md`; desktop appearance review remains.

---

## Multiple Citadel foundation — MC1/MC2 (2026-10-18)

Levels can author multiple independently damageable Citadels with cached fixed portal destinations. Routing, checkpoints, children, placement protection and any-Citadel defeat respect those identities. Single-Citadel maps bypass destination comparisons. Existing gameplay maps/UI are unchanged; multi-Citadel HUD/F3 presentation remains MC3. See `WORLD/OBJECTIVES/Citadel/README.md`.

---

## Stasis Tower — 2026-10-18

Green Stasis is available in the Offensive build tray: low-rate Energy projectiles temporarily stop unfrozen, nonimmune enemies. Cryo slow persists underneath and returns on expiration. Initial test balance: 350 Credits, 3-second stop, one shot every 5 seconds. See `TOWERS/Offensive/StasisTower/README.md`.

---

## Campaign hub — 2026-10-17

Campaign now displays profile progression with LEVELS and RESEARCH. LEVELS opens the existing two-mission chooser; both mission buttons launch the same battlefields. Results returns to Campaign, and Research remains between missions. See UI/Menus/Campaign/Levels/README.md.

---

## Mission XP foundation — 2026-10-17

Mission outcomes now grant configurable per-level player XP (victory500 / defeat0 initially) once to the deployment profile. Results shows the saved reward/level changes/RP and returns to Campaign for Research. See SYSTEMS/Progression/Missions/README.md; first-clear/replay balancing and battlefield saves remain deferred.

---

# Godot Tower Defense — C#/.NET

## Research-first extension — 2026-10-17

Permanent player research now has a first playable foundation: Campaign → RESEARCH → Research Core / Citadel Master I (1 RP, +10% starting Citadel HP). Ownership and RP persist per profile; v1 XP records migrate without reset. Results now tracks real placements, initial construction completions, paid upgrades and unpaused time alongside kills/breaches/waves/integrity. Mission XP scoring/rewards remain deferred. See SYSTEMS/Progression/Research/README.md and TESTS/Research/README.md.

---

A 2D top-down sci-fi tower-defense game in Godot 4.7.x. Editable Level001 and five-portal Level020 maps, enemy spawning and A* routing, finite manual waves, Battle/Laser/Blaster combat, shared instant area damage, wallet/kill credits, Citadel HP and the eight-stage tower-stat foundation are implemented.

## Launch

Build C# in the Godot .NET editor, then press **F5**. The project starts at **Boot**, where you create/select a local profile. Select **Campaign** in Main Menu, then **Play Level 001** or **Play Level 020** to enter the selected battlefield. Profiles persist to Godot `user://Profiles/profiles.json` (versioned JSON with backup recovery). **Campaign/gameplay saves are not yet implemented**: each deployment starts a fresh level session.

For rapid map or combat testing, open `Main.tscn` (Level001) or `MainLevel020.tscn` (Level020) and press **F6** (Run Current Scene). This bypasses profile/menus and remains valid for headless integration checks.

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

Use feature READMEs for detailed implementation and development plans, especially `TOWERS/README.md`, `LEVELS/README.md`, `UI/Menus/README.md`, `SYSTEMS/Saving/README.md` and `NOTES/CodePreferences.md`. Run `TESTS/Integration/EnemyIntegrationChecks.tscn` in the Godot .NET headless runner for gameplay regression checks.

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

## Ammunition supply

All four offensive weapons consume finite local reserves. Build an Ammunition Factory (200 Credits) and Depot (150 Credits) from the Ctrl tray: factories manufacture automatically, visible drones carry stock to depots, then depot drones supply towers at/below10%. New weapons start full by default; their config InitialAmmoFraction permits quicker low/empty testing. See `TOWERS/Economy/Ammunition/README.md` and `TESTS/Economy/Ammunition.md`. Magazines, power, inputs, research and campaign saving remain deferred.

## Player progression foundation (2026-10-17)

Separate per-profile XP/levels/Research Points now live in SYSTEMS/Progression (see its README). Main and Campaign show the selected profile's level, XP bar and RP. Missing progression starts at Level 1; saved grants survive restart and profile switching. Identity/index format is unchanged. The optional menu DEBUG grant is Inspector-controlled and off by default. Mission rewards/Results XP, replay rules, research purchasing and full campaign/battlefield saving remain deferred; this does not complete P1–P5 or Results R4.
