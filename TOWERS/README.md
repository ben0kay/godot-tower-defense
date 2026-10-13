# TOWERS — All Player-Placeable Towers

**Umbrella folder for every tower**, not only weapons. Includes offensive defenses, friendly support, income/energy producers and specialized utility. In the gameplay UI, all of these can simply be called *towers*, as in Bloons TD.

This folder is **first-playable-tower foundation**. The balanced Battle Tower can be placed and inspected; shooting, enemies, economy and upgrades are not implemented yet.

## Folder map

| Folder | Responsibility |
| --- | --- |
| `Shared/` | Future minimal common tower definition/base, reusable health, upgrading, ownership, etc. |
| `Placement/` | Future placement coordination with the world grid and build menu; one shared place instead of one per tower. |
| `Offensive/` | Towers primarily attacking enemies (battle, laser, missile). |
| `Support/` | Towers primarily helping other towers (repair, radar, buffs). |
| `Economy/` | Credits, energy and resource processing (resource, solar, refinery). |
| `Utility/` | Special designs that don't neatly fit the above. |

**Folder categories are for organization, not exclusive capabilities.** A solar tower can start as an energy producer and gain a weak laser through an upgrade while remaining in `Economy/SolarTower/`. The future implementation should allow optional behaviors without forcing all towers to have weapon code.

## Master configuration

- `TowerMasterConfig.cs` is a small **C# Godot Resource scaffold** for shared options, following the project's master-config convention.
- `DefaultTowerConfig.tres` is the Inspector-editable default resource.
- Its settings are **not connected to gameplay** yet and have no effect until implemented.
- Keep **per-tower** costs, fire rate, damage, repair output, energy production, upgrade paths and artwork in future tower-specific definitions, **not** in the global master config.

## Future common data

Each placeable tower will eventually need a unique identifier, display name, category for the build menu, scene/art reference, footprint, cost, optional upgrades and suitable capabilities. Battle Tower begins with **one SVG sprite** and **center-spawned projectiles** for the prototype. Later, offensive or hybrid towers can use separate base/turret graphics, rotating pivots, muzzle Marker2D nodes and recoil.

## First playable tower and placement

- `Offensive/BattleTower/`: 1×1, single-barrel baseline with one SVG, scene, minimal C# and editable proposed stats.
- `Placement/`: build-menu event to snapped ghost, cell occupancy, scene instancing, selection and Inspector.
- Range/fire rate/damage/cost are **planning values**, not active combat or economy systems.
- See `Placement/README.md` and `NOTES/NAVIGATION_PERFORMANCE_PLAN.md` for controls, limitations and later route validation.
