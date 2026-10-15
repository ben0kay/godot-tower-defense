# Offensive towers

The default catalog contains BattleTower (guided Kinetic projectile), LaserTower (sustained Energy beam), BlasterCannon (Explosive instantaneous area projectile) and purple ShockwaveTower (timed expanding Energy front). Targeted weapons reuse shared targeting/aiming; all reuse generic catalog-driven placement/HUD, canonical ResourceCost recipes and the same eight-stage cached stats/paid upgrades.

COMBAT/Damage owns shield/hull responses, optional absolute weapon overrides and penetration. COMBAT/AreaDamage owns instant-disc selection and weapon-input distance falloff. SplashProjectile delivers Blaster's launch-snapshotted area payload exactly once at impact; FEEDBACK/Shockwave is cosmetic only. See BlasterCannon/README.md for current values and tests.

Missile Tower, armour, terrain-blocking line of sight, timed effects and heat remain deferred. The implemented Phase4E ShockwaveEmitter damages only when its advancing front reaches an enemy and shares the visual effect with Blaster. It uses its own explicit C# levels and cached optional ShockwaveStats.
