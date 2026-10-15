# Battle Tower — explicit levels and guided Kinetic fire

Ctrl → Battle Tower → place on route-safe terrain. First targets the shortest remaining path; Closest targets nearest. Shared turret aiming rotates the child visual toward the target, leaving the grid root fixed. Fire requires a living in-range aligned lock and elapsed cooldown; zero rate disables firing. Guided shots still originate at the center and retain launch-time damage/penetration.

## Level data

**Edit `BattleTowerLevels.cs`**, the sole authored source of all level-dependent numbers and credit prices. Each labeled typed row is complete; adding Level 4 means adding another consecutive row. Changing Level 2 never changes Level 3.

| Level | Build/upgrade price | Damage | Shots/s | Range | Projectile speed | Turn | Aim | Search | Penetration |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 200 | 12 | 1 | 256px | 600px/s | 180°/s | 12° | .15s | 5% |
| 2 | 150 | 16 | 1.2 | 288px | 600px/s | 180°/s | 12° | .15s | 5% |
| 3 | 250 | 22 | 1.5 | 320px | 600px/s | 180°/s | 12° | .15s | 5% |

DefaultBattleTowerConfig.tres retains Inspector identity, target priority and RotateWhileTracking; scene/definition retain artwork and registration. Combat stats/prices are no longer numeric .tres exports. Each placed tower gets private runtime rows/recipes. C# Level 1 convenience accessors edit row 1 only; use SetLevelStats(level, snapshot) for another row.

## Purchases and cached stats

Select a placed tower to see its level, actual stats, next-row differences, price and Upgrade button. Level 3 displays Maximum level reached. Build/upgrade payments use the existing shared wallet and ResourceCost recipe; terrain, occupancy, route-safety and affordability failures never charge. Starting funds remain in level configs. The Inspector and range ring read the same cached FinalStats used by combat.

The eight-stage pipeline selects the complete purchased row at Stage 02. Later rank/research/map/effect stages remain neutral. Config changes or purchases invalidate once; physics never resolves per tick. Upgrading preserves targeting, aim, cooldown and navigation.

Kinetic damage and 5% penetration retain the shared defense resolver contract: 12 base against 100 shield/60 hull leaves 97.15 shield/59.4 hull, or deals 12 to exposed hull. Fired bullets preserve their launch snapshot. No research, saving, selling, armour, new artwork or weapon mechanics were introduced.

TowerStatChecks asserts exact stats/prices and level independence; TowerUpgradeChecks drives real HUD purchases/preview/max-level/atomic costs; DamageTypeChecks retains real projectile penetration coverage. See TESTS/README.md for actual compilation and headless results.
