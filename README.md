## More opt-in tower heat profiles — 2026-10-26

Machine Gun (2 min / 10s recovery), Laser (3.5 min / 10s), Blaster Cannon (4 min / 12s), Rocket (4 min / 12s) and Shockwave (3 min / 12s) now enable the existing shared heat capability in their shipped config resources. Laser calibrates heat per damaging beam-second, Shockwave per real pulse interval; shot weapons use their selected paid-level cadence. Battle remains 3 min / 9s; Sniper, Cryo and Stasis stay heatless. Each type exposes Inspector Heat Enabled / Overheat After Minutes / Recovery Seconds. Firing, ammo, price, tower stats and pause/save ownership are unchanged. This is source-implemented; desktop/.NET/Godot verification remains pending. See [heat conditions](TOWERS/Shared/Conditions/README.md).

---

## Organic builders and credit-gain feedback — 2026-10-26

Construction drones now accelerate/brake along curved flights, face travel, hover and reposition
around buildings while beaming, then turn into a smooth return. Motion is checkpointed and stays
inside the active-only shared manager. Credit-gain pulses peak 50% larger and last slightly longer.
See TOWERS/ConstructionDrones/README.md and UI/GameplayHUD/TopBar/README.md. Source/diff review
completed; local .NET/Godot/visual acceptance remains pending.

---

## CV1 shared construction-site holograms — 2026-10-26

Every paid unfinished building now projects a cyan footprint bracket, travelling perimeter indicator and slowly rotating blueprint scanner, including sites **awaiting a queued Citadel builder**. One shared `World/Effects` renderer reads the existing work registration lifecycle, adjusts intensity when the drone releases its hold and sleeps when all construction finishes. Inspector tuning and footprint sizing: [CV1 visuals](TOWERS/Shared/Construction/Visuals/README.md). Desktop visual/performance review remains pending.

---

## Between-wave battlefield saves — S2/S3 (2026-10-26)

Esc Save/Load and profile Campaign Continue now connect to typed battlefield checkpoints. Save
is disabled during combat with **Unavailable during combat**. Towers, work/builders, stock/cargo,
production, wave/RNG progress, frozen research and original reward identity restore into a prepared
paused battlefield. Profileless F6 remains supported. Content changes and retired reward tickets
reject restoration explicitly. Source/diff review completed; **.NET/Godot build/runtime acceptance
remains pending** in the local editor. See [saving](SYSTEMS/Saving/README.md).

---

## Development backlog and documentation audit (2026-10-23)

For current unfinished work **ranked by complexity**, suggested **Codex vs regular ChatGPT** task sizes, additional unimplemented concepts, and a **checklist of outdated Markdown to clean**, see **[NOTES/IMPLEMENTATION_BACKLOG.md](NOTES/IMPLEMENTATION_BACKLOG.md)**. This index distinguishes implemented source, pending QA and historically superseded plan text. Recheck current GitHub HEAD before starting a phase.

---

## Citadel construction drones — CD1/CD2 (2026-10-21)

Paid initial tower/building sites now wait for a cyan builder from the nearest available Citadel. Its beam deploys before ordinary construction time starts; the drone returns after completion. One capped manager/pool handles all towers, with no ground A*. Upgrades/direct/zero-second instances retain their behavior. Settings: TOWERS/ConstructionDrones/DefaultConstructionDroneConfig.tres. Build and dedicated builder/footprint/F4 checks pass; desktop visual/GPU review remains. See TOWERS/ConstructionDrones/README.md for observed tests and existing broad-suite fixture limits.

## Cosmetic shield ricochets — 2026-10-21

Battle/Machine Gun/Sniper shield hits can now leave a short reflected cosmetic streak after real positive shield absorption. One capped world buffer handles all effects; combat remains one hit. Settings: FEEDBACK/DefaultFeedbackConfig.tres and FEEDBACK/Ricochet/DefaultRicochetStyle.tres. Build, focused ricochet and projectile handling checks pass; desktop appearance/GPU review remains. See FEEDBACK/Ricochet/README.md.

## Shared organic terrain edges — E1/E2 (2026-10-21)

Mars atlas clipping/banding is corrected. A reusable profile-driven visual mask now rounds selected terrain boundaries and holes at level load/terrain rebuild, without altering painted tiles, navigation or build rules. The master remains opt-in: WORLD/TERRAIN/Edges/DefaultTerrainEdgeConfig.tres → Enabled=true, then restart the level. Add other tile materials through profiles; no Mars-specific renderer code. See WORLD/TERRAIN/Edges/README.md for settings, tested behavior and remaining desktop visual review.

## F2 enemy artwork preview — 2026-10-21

The wider F2 window now shows the selected enemy's actual-size1:1 sprite on the right, including separate gun artwork. Large artwork scrolls without shrinking. Preview code lives in DEBUG/Spawning and does not spawn gameplay enemies.

## F2 independent batches — 2026-10-21

Start multiple enemy batches concurrently from F2, each with its own amount/gap and Shielded/Stealthed checkboxes. Active rows show progress and individual Cancel controls; Cancel All stops all pending deliveries. All batches share one bounded spawn budget. See DEBUG/Spawning/README.md.

## F2 batch spawner — 2026-10-21

F2 opens a menu to choose enemy, amount and spawn gap in seconds; each ship uses a random valid portal. Close with F2/Escape, or cancel remaining deliveries explicitly. Debug tool implementations now live together under DEBUG (including F3 navigation, audio/alert previews and optional XP grant). Old direct F2 cycling is parked and unbound. See DEBUG/README.md for structure and controls.

## Safety-cache churn correction — 2026-10-20

Active enemies now retain their current segment's safety result, and the shared cache evicts least-recently-used entries. This prevents repeated rebuilding when active segments exceed shared capacity, while immediate navigation invalidation and swept fallbacks remain. See TESTS/Performance/SharedRoutePool.md for the reproduced pressure case and verification.

## Cached movement safety — 2026-10-20

Enemies now share conservative route-segment safety results across waves, avoiding repeated swept wall checks on certified unchanged geometry. Only relevant walkability changes expire certificates; existing staggered repaths and uncertain-geometry fallback remain. Toggle **Enable Route Safety Cache** under the enemy config's Shared Route Pool. See WORLD/NAVIGATION/README.md and TESTS/Performance/SharedRoutePool.md.

## Shared finished routes — 2026-10-20

Compatible enemies now reuse bounded raw paths and finished corridor variations, with selective tower-block invalidation and private movement progress. Toggle **Shared Route Pool** in `ENEMIES/DefaultEnemyConfig.tres`. It warms through ordinary budgeted requests, with no exhaustive startup bake. Trajectories may repeat more often. See `WORLD/NAVIGATION/README.md` and `TESTS/Performance/SharedRoutePool.md` for behavior and measured results.

## PF2 profiling — 2026-10-20

The F4 performance panel now supports optional subsystem CPU timings: Shift+F4 toggles profiling, F5 saves a report. The older top-left grid debug readout is removed; F2/F3 remain. Profiling defaults off. See `DEBUG/README.md` for controls, nested timing meanings and report location, and `TESTS/Debug/PF2.md` for actual verification.

## Projectile handling update (PA1–PA3)

Optional immutable handling now carries launch spread and explicit Straight/Homing guidance through level → selected rank → captured research → safety → final snapshots. Projectile towers default to straight physical shots; splash uses a fixed launch destination. See `TOWERS/Shared/Projectiles/PROJECTILE_GUIDANCE_ACCURACY_PLAN.md` for implementation decisions and retained pass history.

### Gunner Drone handling polish — 2026-10-20

Gunner now holds valid targets for1s, searches every0.2s when idle, fires continuously at6/s, and uses small rounded-front/tapered-tail red energy streaks. All three are data-driven; travelling bullets and Kinetic damage remain intact. Import/build and the three focused enemy-weapon headless checks passed. Desktop visual approval remains pending. See ENEMIES/Weapons/README.md and TESTS/EnemyWeapons/README.md.
---

### EG3 Gunner Drone — source implemented (2026-10-20)

F2/catalog now includes Gunner Drone: a distinct64×48 hull matching medium Enemy004's size, separate rotating gun SVG, small muted-red tracers and tiny orange-red accepted-shot flashes. It keeps routing while shooting nearby towers via EG1/EG2. Normal waves remain unchanged. New scene and balance: [GunnerDrone](ENEMIES/Types/GunnerDrone/README.md). Build/runtime/visual verification remains pending because execution was unavailable; EG4 profiling is still future work.

### EG2 hostile bullet foundation — source implemented (2026-10-20)

Enemy minigun mounts can now launch straight world-owned bullets that sweep against living tower footprints and apply existing Kinetic tower damage. One retained shot buffer and renderer replace per-bullet scene nodes; pause, lifetime, overflow and source-removal handling are included. Generic muted-red tracer appearance and budgets are Inspector editable. Existing enemies/waves remain unarmed; Gunner Drone/F2/art is EG3, benchmarking is EG4. Build/runtime/visual verification remains pending because execution was unavailable. See [enemy bullets](ENEMIES/Projectiles/README.md).

### EG1 enemy weapon foundation — source implemented (2026-10-20)

Optional enemy weapon mounts now acquire nearby living towers through one event-driven spatial TowerRegistry and aim a separate gun pivot while normal routing continues. Minigun burst timing exposes an immutable shot-request seam. Existing enemies remain unarmed; hostile bullets/damage (EG2), Gunner Drone/F2/art (EG3) and profiling (EG4) remain future work. C# build/Godot/runtime tests are pending because execution was unavailable. See [enemy weapons](ENEMIES/Weapons/README.md).

### Offensive projectile recoil rollout — 2026-10-20

Machine Gun, Sniper, Blaster Cannon, Rocket, Cryo and Stasis now join Battle with fixed bases and recoiling gun/muzzle assemblies, each using an editable preset in its Visuals folder. Laser and stationary Shockwave retain their beam/radial effects. Gameplay values and Inspector layout are unchanged. Source/static review completed; Godot import/build/tests and desktop visual verification remain pending. See [recoil notes](TOWERS/Shared/Weapons/Recoil/README.md).

### Battle recoil pilot — R1 + R2 (2026-10-20)

Battle Tower now has a fixed base and a split recoiling gun/muzzle, configured by an optional shared visual-only preset. Construction stages and composite artwork fallbacks remain supported. Source implementation is complete; Godot import/build/runtime and visual validation are pending. See [recoil notes](TOWERS/Shared/Weapons/Recoil/README.md) and [checks](TESTS/Towers/TowerRecoil.md).

## SE1 — stealth enemies and reveal beacon — implemented 2026-10-20 UTC

Waves may now author `Enemy("basic_mk1", stealth: true)` independently of `shield: true`. Default spawns remain ordinary; no existing wave/map was changed. Stealth ships render at30% alpha, revealed ships at100%, with configurable/dimmed defense feedback that composes with Cryo/Stasis. All aimed towers share detection filtering and cached-lock validation; private TowerConfig.CanDetectStealth opts a specialist in without globally revealing enemies. Existing attacks already emitted, splash and untargeted Shockwave remain able to damage hidden ships (stealth is targeting, not immunity).

New Ctrl / Utility Stealth Beacon:250 Credits,1×1,2s build,180 HP,256px range, one paid tier. It reveals enemies inside range for all towers; overlap, exit/re-entry, disable/reactivation, construction, destruction, relocation, enemy removal/reuse and world teardown are source-owned. One lazy spatial reveal index listens to enemy motion only while beacons are active; no per-frame enemy scans or per-beacon processing. See ENEMIES/Shared/Stealth, FEEDBACK/Stealth, TOWERS/Shared/Detection, TOWERS/Utility/StealthBeacon and TESTS/Stealth READMEs. Existing Inspector layout/resize is retained. Earlier blanket “stealth/detection future” notes below are historical; stealth preference scoring, Radar, tower-grant detection beacons and further content remain separate future work.

---

## Lightweight data-driven levels — implemented 2026-10-19

Shared `LEVELS/Runtime/Battlefield.tscn` + `WORLD/World.tscn` replace per-level service copies. Existing Main launchers and World variants are thin compatibility shortcuts; F6 paths stay valid. Each level now has a MissionDefinition resource and the Campaign roster is `LEVELS/Catalog/DefaultMissionCatalog.tres`. Menu number/title/description/map are data, with a bounded scrollable mission list. New missions deploy/retry directly from their resource; no per-level launcher/world or menu/catalog code is required. Next advances only to the immediate enabled number.

LevelConfig directly references an Inspector-assignable C# WaveSetSource; the three existing wave recipes keep identical Create() content and now implement Build(). WaveCatalog's old central switch is removed; string-ID compatibility resolves via the mission roster. Painting, map/config ownership, portal/Citadel placement, credits/HP/XP values and progression identities are preserved. Explicit off-tree preparation supports the existing multi-Citadel fixture. Citadel destination cleanup is now idempotent across objective/world teardown.

Authoring steps: `LEVELS/AUTHORING.md`; ownership: `LEVELS/Catalog/README.md`, `LEVELS/Runtime/README.md`; validation: `TESTS/Integration/LightweightLevels.md`. This completes shared-launcher/catalog convenience, not a visual wave editor, campaign unlock/save system or hot map switching. Earlier duplicated-launcher instructions below are historical.

---

## ARG automatic income correction — 2026-10-19

ARG eligible kills now grant Credits directly to the existing wallet: no collection button, stored income or storage limit. The Resource Tower and Citadels retain manual collection. A single mouse-transparent green floating number beside each ARG combines rapid successful payouts (+1 → +2 → +5), then floats/fades away after a quiet interval (default 0.8s). Popup duration/color are editable on ARG config. Higher tiers still earn 2/3 per eligible kill. Full-wallet rejection displays no false income and is not banked. Existing kill eligibility, pause/construction/upgrade gates and ordinary kill rewards/XP remain intact. ARG previews omit unused storage. Earlier buffered-ARG descriptions below are historical and superseded by this correction.

---

## Resource Tower and Adaptable Resource Generator — 2026-10-19

The Ctrl **Economy** tray now includes two muted-green Credit producers, each with three paid tiers and an Inspector **Collect Credits** action. Resource Tower earns periodically during active waves; ARG earns from credited kills by nearby offensive towers. Existing Citadel income/collection is retained. Balance and tuning: `ECONOMY/Production/README.md`; verification/known baseline test failures: `TESTS/Economy/CreditTowers.md`.

---

## Performance overlay and F2 tools — 2026-10-18

**F4** toggles the new click-through performance panel in debug builds on all playable battlefields. The contained script and settings guide live in `DEBUG/`. Level002/template now include the shared F2 spawn/F3 route-preview tools; use Campaign or a Main launcher for gameplay rather than running the raw map alone. See `DEBUG/LevelTools/README.md`.

---

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
