# Basic projectiles

`BasicProjectile.cs` is a visible guided bullet with speed/damage initialized by its weapon. It tracks its selected living target, hits using travel distance plus target radius and expires after five seconds or when its target dies/arrives. Bullets do not collide with terrain in this first combat test. They can finish travel after an enemy leaves tower range; range constrains target acquisition and firing.

No rigid bodies, splash damage, armor model, pooling or elaborate VFX yet. Fired from the existing tower sprite center; separate rotating gun and muzzle art can follow later.


Phase 4C-3: BasicProjectile stores an immutable DamageInfo supplied by its weapon. Battle snapshots its final Kinetic damage and5% penetration at launch. Initialize(target,speed,float) remains compatible and defaults to Kinetic with zero penetration and no overrides. Contact forwards unmodified base units to Enemy.ApplyDamage; COMBAT/Damage owns all coefficient selection, bypass and base-unit shield spillover. Battle12 against100 shield/60 hull leaves94.3/59.4, while an unshielded target still loses12 hull. Delivery/travel/lifetime/reward behavior is unchanged.
