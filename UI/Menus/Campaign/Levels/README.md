## Lightweight data-driven levels — implemented 2026-10-19

Shared `LEVELS/Runtime/Battlefield.tscn` + `WORLD/World.tscn` replace per-level service copies. Existing Main launchers and World variants are thin compatibility shortcuts; F6 paths stay valid. Each level now has a MissionDefinition resource and the Campaign roster is `LEVELS/Catalog/DefaultMissionCatalog.tres`. Menu number/title/description/map are data, with a bounded scrollable mission list. New missions deploy/retry directly from their resource; no per-level launcher/world or menu/catalog code is required. Next advances only to the immediate enabled number.

LevelConfig directly references an Inspector-assignable C# WaveSetSource; the three existing wave recipes keep identical Create() content and now implement Build(). WaveCatalog's old central switch is removed; string-ID compatibility resolves via the mission roster. Painting, map/config ownership, portal/Citadel placement, credits/HP/XP values and progression identities are preserved. Explicit off-tree preparation supports the existing multi-Citadel fixture. Citadel destination cleanup is now idempotent across objective/world teardown.

Authoring steps: `LEVELS/AUTHORING.md`; ownership: `LEVELS/Catalog/README.md`, `LEVELS/Runtime/README.md`; validation: `TESTS/Integration/LightweightLevels.md`. This completes shared-launcher/catalog convenience, not a visual wave editor, campaign unlock/save system or hot map switching. Earlier duplicated-launcher instructions below are historical.

---

## Level002 tutorial waves — 2026-10-18 (implemented)

The newly painted Level002 is now playable through **Campaign → Levels → PLAY LEVEL 002**, `MainLevel002.tscn` (F6), and Level001's Next Level result action. Its dedicated `WORLD/WorldLevel002.tscn` mounts the existing user-authored map unchanged. Level020 remains separately playable and unchanged. Level002 Next stays disabled because there is no playable Level003; it never skips to Level020.

`LEVELS/Level002/Level002Waves.cs` defines four modest, manually started waves. Enemy “1–2–3” means the three introductory plain roles: Standard Drone (`basic_mk1`), Swift Interceptor (`scout_mk1`) and Bulwark Drone (`medium_mk1`, asset Enemy004). There is no Enemy003 scene. No shields, support ships, carriers, bosses or extra mechanics are introduced.

| Wave | Standard | Swift | Bulwark | Total | Introductory purpose |
| --- | ---: | ---: | ---: | ---: | --- |
| 1 | 12 | 0 | 0 | 12 | Baseline shooting/building |
| 2 | 10 | 6 | 0 | 16 | Fast, fragile targets |
| 3 | 12 | 4 | 4 | 20 | Slower, tougher targets |
| 4 | 16 | 4 | 4 | 24 | Light mixed finale |

Spawn intervals are 0.65–1.4 seconds, with 3-second gaps between batches. Wave4 uses exact weighted composition; all other batches use one type. Existing Start/Next Wave controls allow unlimited preparation time. Local settings: **1,000 starting Credits**, **1,000 Citadel HP**, existing provisional **500 victory XP / 0 defeat XP**. Both authored portals (`EnemyPortal1`, `EnemyPortal2`) are permitted; map-edge spawning is replaced by portal mode for this level. Counts/timings remain C#-authored for AI-assisted tuning as requested. No map tile, Portal/Citadel coordinate, shared scheduler or enemy/tower balance changes.

C# build and `Level002TutorialChecks.tscn` passed: actual map/scheduler spawned all 72 unshielded enemies and produced victory only after wave4. `ResultsChecks.tscn` passed actual Level001→Level002 navigation, retry, menu and missing-next behavior. The isolated tutorial test kills enemies to verify scheduling/ownership; desktop tower balance/playfeel remains provisional. Earlier unplayable-template notes below are historical and superseded by this section.

---

## Level002 moved to Level020; fresh Level002 design map — 2026-10-18

The Five Gates is now `LEVELS/Level020/Level020.tscn`, with `Level020Config.tres`, `Level020Waves.cs` and WaveSetId `level020`. Its terrain, object layout, wave composition and balance are preserved. Its standalone launcher/world variant are `MainLevel020.tscn` / `WORLD/WorldLevel020.tscn`; Campaign labels/launches Level020 and PlayableLevelCatalog registers numeric ID20. Scene/script UID sidecars are retained across renames. Tests and current documentation reference the moved map.

Fresh `LEVELS/Level002/Level002.tscn` is copied from the current LevelTemplate with a new root name and independent `Level002Config.tres` copied from DefaultLevelConfig. Empty terrain layers and template background/Portal/Citadel/checkpoint overlay are retained. The template itself is unchanged. Open this scene in Godot's 2D editor to design it. It has no authored waves or playable launcher yet and is not offered as a finished Campaign mission. Add those when the map is ready. Results cannot skip the missing playable Level002 to reach Level020; Level001's Next is disabled for now. Level020 remains available directly from Campaign and F6 on MainLevel020.

Earlier Level002/Five Gates references in historical notes below refer to the map now called Level020. No old design plan or map content was removed. Verification: see `TESTS/Integration/LevelReorganization.md`.

---

# Campaign Levels

`LevelSelectionMenu.cs/.tscn` owns the mission-selection screen reached from Campaign → LEVELS. It requires the selected local profile, reuses MenuLayout/UIButtonFactory, and launches the same Level001 / Level020 battlefield scenes through MenuNavigation. Back returns to the Campaign hub; missing profiles redirect to Boot.

This is the existing two-mission chooser moved out of the hub. Both missions remain available. No completion stars, locked missions, campaign slots, Continue/Load or unlock progression are invented. Maps and mission settings remain in LEVELS; profile XP/research persistence remains in SYSTEMS/Progression. Research is accessed through the hub between deployments.
