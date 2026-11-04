# Enemy Fleet Roster — names, roles, and stable identities

**Updated 2026-11-04.** This is the canonical human-readable lookup for the **14** types registered in `ENEMIES/DefaultEnemyCatalog.tres`. The GameMaker-era numbered `Enemy001…Enemy006` folders are **asset history**, not an enemy tier, strength progression, wave order, or campaign discovery order. Wave data, spawning and save compatibility use the **stable EnemyId** column.

| Fleet role | Display name | Stable EnemyId (wave key) | Current scene folder |
| --- | --- | --- | --- |
| Drone | **Standard Drone** | `basic_mk1` | `Types/Enemy001/` |
| Drone | **Reinforced Drone** | `reinforced_drone_mk1` | `Types/ReinforcedDrone/` |
| Drone | **Gunner Drone** | `gunner_drone_mk1` | `Types/GunnerDrone/` |
| Interceptor | **Swift Interceptor** | `scout_mk1` | `Types/Enemy002/` |
| Interceptor | **Racer Interceptor** | `racer_mk1` | `Types/Racer/` |
| Bulwark | **Bulwark Drone** | `medium_mk1` | `Types/Enemy004/` |
| Bulwark | **Heavy Bulwark** | `heavy_bulwark_mk1` | `Types/HeavyBulwark/` |
| Shield Ship | **Shield Frigate** | `shield_frigate_mk1` | `Types/ShieldFrigate/` |
| Carrier | **Drone Transporter** | `transporter_mk1` | `Types/Transporter/` |
| Carrier | **Heavy Transporter** | `heavy_transporter_mk1` | `Types/HeavyTransporter/` |
| Support | **Shield Emitter** | `shield_emitter_mk1` | `Types/Enemy003/` |
| Capital Ship | **Heavy Warship** | `big_mk1` | `Types/Enemy005/` |
| Capital Ship | **Cruiser** | `cruiser_mk1` | `Types/Cruiser/` |
| Assault | **Elite Assault Ship** | `elite_mk1` | `Types/Enemy006/` |

## Naming contract

- **DisplayName:** Readable name used in Wave Editor/F2 and UI. Descriptive, role-oriented; no placeholder names such as `Enemy005`, `Big mk.1`, `Elite mk.1` or bare `Racer` in editor-facing menus. New types should use a consistently named variant and role when it helps (e.g. `Racer Interceptor`). This pass renames **only four DisplayNames**: `Big mk.1` → `Heavy Warship`, `Elite mk.1` → `Elite Assault Ship`, `Racer` → `Racer Interceptor`, and `Transporter` → `Drone Transporter`.
- **EnemyId:** Canonical lookup string in waves, spawner, save data, and code (e.g. `medium_mk1`). **Never rename casually**: old authoring resources and checkpoints may refer to these IDs. Naming updates must preserve all 14 current IDs.
- **Family:** Stable serialized `EnemyFamily` enum defines what the unit does, not its difficulty level or campaign order. `EnemyNaming.Role()` renders family labels in menus; **do not renumber enum values**.
- **Folder / .tscn / artwork filename:** Currently a mixture of legacy numbered names and readable newer names. Paths remain intact here to preserve Godot references and imported assets. Editor-visible names are now consistent without migrating these paths. New enemy types should prefer readable PascalCase folder names.
- **Campaign introduction order:** Defined independently in authored `LEVELS/LevelXXX` waves. Not derived from folder number, EnemyId, type family, or catalogue ordering. Level001 introduces Standard then Swift; Level002 can introduce its own concepts independently.

## Optional future asset path migration (not yet implemented)

If the owner wants the Godot FileSystem dock **itself** to use readable names, do a single separate rename pass. Suggested mappings:

| Current folder + resource basename | Proposed folder + resource basename |
| --- | --- |
| `Enemy001` | `StandardDrone` |
| `Enemy002` | `SwiftInterceptor` |
| `Enemy003` | `ShieldEmitter` |
| `Enemy004` | `BulwarkDrone` |
| `Enemy005` | `HeavyWarship` |
| `Enemy006` | `EliteAssaultShip` |
| `Racer` | `RacerInterceptor` |
| `Transporter` | `DroneTransporter` |

This migration must rename scene/resource/artwork paths and update **every** `res://` reference, scene inheritance dependency, script/resource UID path, tests, catalog registration, debug tooling and directly linked docs, preserving `uid://` and stable `EnemyId` values. Migrate via Godot's FileSystem rename where practical (which can update resource references); then inspect imports and test `EnemyCatalog.Rebuild`, spawn menus and levels. Do **not** duplicate or delete assets without proving the old references are repaired. Other six descriptive folders already follow the convention.

## What appears in the tools

- **Godot Wave Editor:** Enemy type picker groups by `Family` and shows **Role · DisplayName (EnemyId)**, with actual ID saved to the wave `.tres`.
- **F2 enemy spawner:** Shows **Role · DisplayName (EnemyId)** and retains the existing catalogue index order (important for its debug and preview behavior).
- **Gameplay:** No changes to health, shields, movement, abilities, wave composition, scene paths, EnemyId, or save data. These are names/menus only.
