## CryoTower recoil preset

CryoTowerRecoil.tres is assigned to the tower scene's Recoil.Style. Select that node or open the preset to tune Enabled, KickDistancePixels, KickSeconds, ReturnSeconds and MaximumOffsetPixels. Motion is local backward +Y on TurretPivot/RecoilRoot; the fixed base and aiming pivot are not displaced. The original composite icon remains unchanged. Per-instance controllers own runtime offsets; presets contain tuning only. Build/visual verification remains pending.

# CryoTower visual presets

Edit these `.tres` resources in Godot and assign them under the tower config's Weapon Visuals group. Current defaults: 16px icy cyan tracer and pale cyan crystalline starburst; the existing Energy damage and permanent Slow payload are unchanged. No combat stats are stored here. Shapes, snapshot/fallback behavior and texture axes: [shared guide](../../../Shared/Weapons/Visuals/README.md).
