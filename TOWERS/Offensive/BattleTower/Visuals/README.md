## Battle recoil preset

BattleRecoilStyle.tres opts Battle into shared recoil: 2px kick over45ms,150ms return,4px repeated-shot cap. Edit Enabled, KickDistancePixels, KickSeconds, ReturnSeconds and MaximumOffsetPixels in the Inspector. This is presentation data; recoil state belongs to each controller. Fixed base and gun SVGs share64×64 registration. Composite construction/custom tier artwork remains supported, with split recoil suspended while that overlay is visible. Build/playtest verification is pending; see [checks](../../../../TESTS/Towers/TowerRecoil.md).

# BattleTower visual presets

Edit these `.tres` resources in Godot and assign them under the tower config's Weapon Visuals group. Current defaults: 18px amber tracer and stronger golden starburst; actual muzzle is `(0, -28.5)` under `TurretPivot/Muzzles/MuzzleA`. No combat stats are stored here. Shapes, snapshot/fallback behavior and texture axes: [shared guide](../../../Shared/Weapons/Visuals/README.md).
