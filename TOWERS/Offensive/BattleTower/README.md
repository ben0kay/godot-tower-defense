# Battle Tower — first automatic weapon

F5 -> Ctrl -> BATTLE -> place on buildable terrain. Press F2 repeatedly to spawn Enemy001 from the Portal. Towers fire at enemies within range. First targets the shortest remaining path to the Citadel; Closest targets nearest to the tower. Defaults: range 256px, 1 shot/s, 12 damage, bullet speed 600px/s, search every 0.15s. Enemy001 takes five baseline hits to kill.

## Inspector tuning

- Select `DefaultBattleTowerConfig.tres` to change defaults for newly built towers, or select TowerPlacement in Main.tscn and expand BattleConfig.
- Open BattleTower.tscn, select its root and expand Config to view the same defaults.
- Each placed tower receives a separate Config copy. During play, switch Scene to Remote, expand Battlefield/World/Towers, select a BattleTower and expand Config to tune only that tower's range, fire rate, damage, projectile speed and targeting. Runtime edits are temporary; edit the saved resource for permanent defaults.
- Fire rate zero disables shooting. ConstructionCosts is the authoritative shared resource recipe; the default costs 200 Credits. Successful placement pays through Main/Economy.

The one-piece SVG is unchanged; bullets fire from its center. The child TurretPivot rotates the one-piece art, leaving placement and selection fixed. No line-of-sight/terrain-blocked bullets, upgrades, rewards, saving or objective-health/waves yet. Tower placement prevents sealing currently usable entries or trapping existing ships. Click a tower to inspect active stats and its range ring.


## Phase 1 aiming
The reusable opt-in `TOWERS/Shared/Weapons/TurretAiming.cs` follows the current target along the shortest angle, accounting for the SVG barrel pointing up. A tower fires only when its living in-range target is within aim tolerance and its cooldown has elapsed. It keeps its last orientation when no target is available.

Tune **Aiming** in DefaultBattleTowerConfig.tres: RotationSpeedDegrees (180), AimToleranceDegrees (12), RotateWhileTracking (true). Zero speed holds orientation; disabling tracking also holds orientation, with aimed-fire gating still enforced. Newly placed towers retain individual resource copies. Shots still originate at the tower center; no muzzle or idle rotation is added.

## Construction economy
Set Construction / Construction Costs in DefaultBattleTowerConfig.tres. Each entry expands to Resource and Amount; the default is 200 Credits. Starting funds live in each level config (Level001 starts with 1,000). Terrain/connectivity and affordability failures charge nothing. The preview shows required costs and insufficient-resource feedback. Paid upgrades are implemented; selling is still future work.

## Effective stat pipeline (Phase 3A)
BattleTower now reads cached FinalStats from the shared eight-stage resolver. Existing numeric Config fields are still editable: Remote Inspector or C# property changes emit Changed and rebuild immediately. Config replacement also rebuilds; RebuildStats() supports future explicit invalidation. The selected Inspector/range ring uses the same final values. Upgrades, ranks, research, level conditions and effects are currently neutral, so valid base balance stays unchanged. Prices, priority and tracking policy remain outside the numeric pipeline. See TOWERS/Shared/Stats/README.md.

## Phase 3B upgrades
Select a placed tower after exiting build mode to see its current level and next-tier differences/price. Upgrade to level 2 for 150 Credits, then level 3 for 250; defaults become 16/22 damage, 1.2/1.5 shots/sec and 288/320px range. Edit Upgrades/Level2.tres and Level3.tres for permanent balance changes. The resolver applies cumulative operations without altering base Config, other towers, aim/cooldown or navigation. External tier Resources are explicitly copied per instance; nested runtime edits require RebuildStats().
