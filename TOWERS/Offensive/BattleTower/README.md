# Battle Tower

**First placeable tower:** balanced, single-barrel offensive turret between the slower longer-range Sniper and the rapid-firing Minigun.

Open `DefaultBattleTowerConfig.tres` in Godot to tune these **proposed, nonfunctional combat values**:

| Stat | Starting value |
| --- | --- |
| Footprint | 1 x 1 grid cell (64 px square) |
| Barrels | 1 |
| Price | 100 credits (not yet charged) |
| Range | 256 world pixels / four grid cells |
| Fire rate | 1 shot/second |
| Damage | 12 per shot |

The single-piece top-down SVG lives in `Artwork/battle_tower.svg`, used by `BattleTower.tscn`. Future art can separate the fixed base, rotating gun pivot and `Marker2D` muzzle, adding recoil and gunfire without throwing away placement logic.

**Test:** Ctrl -> BATTLE -> hover a grid square -> left-click. Green means buildable; red means already occupied. Build multiple or exit with right-click/Esc. Left-click a placed Battle Tower afterward to inspect its cell, planned stats and range ring.

No aiming, firing, projectiles, enemies, damage, spending money, upgrades, saving or navigation connectivity checks in this pass.
