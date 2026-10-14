# Basic projectiles

`BasicProjectile.cs` is a visible guided bullet with speed/damage initialized by its weapon. It tracks its selected living target, hits using travel distance plus target radius and expires after five seconds or when its target dies/arrives. Bullets do not collide with terrain in this first combat test. They can finish travel after an enemy leaves tower range; range constrains target acquisition and firing.

No rigid bodies, splash damage, armor model, pooling or elaborate VFX yet. Fired from the existing tower sprite center; separate rotating gun and muzzle art can follow later.


Phase 4C-1: BasicProjectile delivers DamageInfo with default Kinetic type. Initialize can alternatively accept a typed payload; damage coefficients belong to COMBAT/Damage, never projectile delivery. Default12 base damage still deals12 to unshielded hull but6 to shield. Travel/contact/lifetime/rewards are unchanged.
