# Offensive towers

The default catalog contains BattleTower (guided Kinetic projectile), LaserTower (sustained Energy beam), BlasterCannon (Explosive instantaneous area projectile) and purple ShockwaveTower (timed expanding Energy front). Targeted weapons reuse shared targeting/aiming; all reuse generic catalog-driven placement/HUD, canonical ResourceCost recipes and the same eight-stage cached stats/paid upgrades.

COMBAT/Damage owns shield/hull responses, optional absolute weapon overrides and penetration. COMBAT/AreaDamage owns instant-disc selection and weapon-input distance falloff. SplashProjectile delivers Blaster's launch-snapshotted area payload exactly once at impact; FEEDBACK/Shockwave is cosmetic only. See BlasterCannon/README.md for current values and tests.

Missile Tower, armour, terrain-blocking line of sight, timed effects and heat remain deferred. The implemented Phase4E ShockwaveEmitter damages only when its advancing front reaches an enemy and shares the visual effect with Blaster. It uses its own explicit C# levels and cached optional ShockwaveStats.


## Sniper and Machine Gun — new projectile specializations

- `SniperTower/`: long-range, slow-firing, high-per-hit Kinetic precision tower (1 muzzle; 80/88/96 damage at levels 1–3). Registered in `DefaultTowerCatalog.tres` as `sniper`.
- `MachineGunTower/`: close-range high-cadence Kinetic tower with **two alternating muzzle markers**; the authored fire rate is **total bullets/s**, not bullets/s per barrel. Registered as `machine_gun`.
- Both reuse `Shared/Weapons/MultiBarrel/ProjectileTurretBase.cs` for targeting/aiming/projectile dispatch, and `BarrelFireController.cs` for `Single`, `Sequential`, and `Simultaneous` firing modes. No new weapon-wide polling or changes to existing offensive tower attack loops; details in `Shared/Weapons/MultiBarrel/README.md`.

**UI integration:** The existing catalog-driven CTRL tray can show these tower definitions automatically, but the live UI redesign belongs to Codex. This implementation adds only the two catalog entries; it does not change GameplayHUD, the build tray scene, or keyboard handling.
