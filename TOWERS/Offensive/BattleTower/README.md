# Battle Tower — first automatic weapon

F5 -> Ctrl -> BATTLE -> place on buildable terrain. Press F2 repeatedly to spawn Enemy001 from the Portal. Towers fire at enemies within range. First targets the shortest remaining path to the Citadel; Closest targets nearest to the tower. Defaults: range 256px, 1 shot/s, 12 damage, bullet speed 600px/s, search every 0.15s. Enemy001 takes five baseline hits to kill.

## Inspector tuning

- Select `DefaultBattleTowerConfig.tres` to change defaults for newly built towers, or select TowerPlacement in Main.tscn and expand BattleConfig.
- Open BattleTower.tscn, select its root and expand Config to view the same defaults.
- Each placed tower receives a separate Config copy. During play, switch Scene to Remote, expand Battlefield/World/Towers, select a BattleTower and expand Config to tune only that tower's range, fire rate, damage, projectile speed and targeting. Runtime edits are temporary; edit the saved resource for permanent defaults.
- Fire rate zero disables shooting. BuildCost is displayed but not charged; economy is future work.

The one-piece SVG is unchanged; bullets fire from its center. No rotating turret, line-of-sight/terrain-blocked bullets, upgrades, rewards, saving or objective-health/waves yet. Tower placement prevents sealing currently usable entries or trapping existing ships. Click a tower to inspect active stats and its range ring.
