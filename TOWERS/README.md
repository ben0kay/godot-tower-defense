# TOWERS — All Player-Placeable Towers

**Umbrella folder for every tower**, not only weapons. Includes offensive defenses, friendly support, income/energy producers and specialized utility. In the gameplay UI, all of these can simply be called *towers*, as in Bloons TD.

This folder is **foundational organization only**. No tower AI, damage, projectile, economy, upgrade or placement gameplay is implemented here yet.

## Folder map

| Folder | Responsibility |
| --- | --- |
| `Shared/` | Future minimal common tower definition/base, reusable health, upgrading, ownership, etc. |
| `Placement/` | Future placement coordination with the world grid and build menu; one shared place instead of one per tower. |
| `Offensive/` | Towers primarily attacking enemies (gun, laser, missile). |
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

Each placeable tower will eventually need a unique identifier, display name, category for the build menu, scene/art reference, footprint, cost, optional upgrades and suitable capabilities. Start with **one SVG sprite** and **center-spawned projectiles** for the prototype. Later, offensive or hybrid towers can use separate base/turret graphics, rotating pivots, muzzle Marker2D nodes and recoil.

## Next milestone — basic build menu and placement

1. UI exposes one temporary build-menu entry (e.g. Gun Tower).
2. Player selects it, sees a 1×1 grid-aligned preview, and clicks to build.
3. Placement checks cell bounds and occupancy; navigation connectivity checks will be required before placement can block enemy routes.
4. A tower instance appears at the cell center. It does not need to shoot yet.

See `WORLD/GRID/README.md` and `NOTES/NAVIGATION_PERFORMANCE_PLAN.md`. No existing camera, world or UI scene was changed by this folder-structure pass.
