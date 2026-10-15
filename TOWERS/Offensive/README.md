# Offensive towers

The default catalog contains BattleTower (guided Kinetic projectile), LaserTower (sustained Energy beam) and BlasterCannon (Explosive instantaneous area projectile). All reuse shared targeting/aiming, generic catalog-driven placement/HUD, canonical ResourceCost recipes and the same eight-stage cached stats/paid upgrades.

COMBAT/Damage owns shield/hull responses, optional absolute weapon overrides and penetration. COMBAT/AreaDamage owns instant-disc selection and weapon-input distance falloff. SplashProjectile delivers Blaster's launch-snapshotted area payload exactly once at impact; FEEDBACK/AreaBlast is cosmetic only. See BlasterCannon/README.md for current values and tests.

Missile Tower, armour, terrain-blocking line of sight, timed effects and heat remain deferred. A true expanding shockwave damages only as its wavefront arrives; its Phase 4E design remains under TOWERS/Shared/Weapons/Shockwaves, with no runtime implementation yet.
