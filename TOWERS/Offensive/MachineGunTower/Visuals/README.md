## MachineGunTower recoil preset

MachineGunTowerRecoil.tres is assigned to the tower scene's Recoil.Style. Select that node or open the preset to tune Enabled, KickDistancePixels, KickSeconds, ReturnSeconds and MaximumOffsetPixels. Motion is local backward +Y on TurretPivot/RecoilRoot; the fixed base and aiming pivot are not displaced. The original composite icon remains unchanged. Per-instance controllers own runtime offsets; presets contain tuning only. Build/visual verification remains pending.

# MachineGunTower visual presets

Edit these `.tres` resources in Godot and assign them under the tower config's Weapon Visuals group. Current defaults: 12px narrow amber tracer and brief orange/white cone at each actual A/B barrel. No combat stats are stored here. Shapes, snapshot/fallback behavior and texture axes: [shared guide](../../../Shared/Weapons/Visuals/README.md).
