# Tower Placement

`TowerPlacementController.cs` wires the GameplayHUD `TowerChoiceRequested("battle")` event to grid-aligned creation and selection of `BattleTower.tscn` instances.

**Ctrl** opens/closes the tower menu. Click **BATTLE** to enter build mode; a green translucent SVG follows a free cell, red signals occupancy. Left-click to place, repeating as needed. **Right-click or Esc** cancels. Outside build mode, click an existing tower to inspect and highlight it (including a proposed range circle); click empty space to deselect. Tower bar and Inspector remain independent.

The controller uses a `Dictionary<Vector2I, BattleTower>` for constant-time occupancy checks; instances are under `World/Towers`. No collider or per-tower `_Process` is created. World mouse input is handled only when not consumed by UI controls.

**Not yet implemented:** cost checks/deduction, guns/projectiles, target acquisition, enemy navigation, build restrictions other than bounds/occupancy, saves and upgrades. Future tower placement must validate portal-to-goal connectivity when building can block ship paths (`NOTES/NAVIGATION_PERFORMANCE_PLAN.md`).

Battle Tower config: `TOWERS/Offensive/BattleTower/DefaultBattleTowerConfig.tres`. Preview/range switches: `TOWERS/DefaultTowerConfig.tres`.
