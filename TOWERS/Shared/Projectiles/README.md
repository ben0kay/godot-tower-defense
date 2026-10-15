# Basic projectiles

`BasicProjectile.cs` is a visible guided bullet with speed/damage initialized by its weapon. It tracks its selected living target, hits using travel distance plus target radius and expires after five seconds or when its target dies/arrives. Bullets do not collide with terrain in this first combat test. They can finish travel after an enemy leaves tower range; range constrains target acquisition and firing.

BasicProjectile has no rigid bodies, splash damage, armor model, pooling or elaborate VFX. Fired from the existing tower sprite center; separate rotating gun and muzzle art can follow later.


Phase 4C-3: BasicProjectile stores an immutable DamageInfo supplied by its weapon. Battle snapshots its final Kinetic damage and5% penetration at launch. Initialize(target,speed,float) remains compatible and defaults to Kinetic with zero penetration and no overrides. Contact forwards unmodified base units to Enemy.ApplyDamage; COMBAT/Damage owns all coefficient selection, bypass and base-unit shield spillover. Battle12 against100 shield/60 hull leaves97.15/59.4, while an unshielded target still loses12 hull. Delivery/travel/lifetime/reward behavior is unchanged.

## SplashProjectile — implemented Phase 4D

A separate guided delivery helper for Blaster and future instant-explosion weapons; BasicProjectile is unchanged. Initialize supplies target/registry/last-known world point/speed, immutable DamageInfo and AreaBlastStats, optional Effects parent and a default 5-second lifetime. Inputs reject invalid settings before activation. The projectile retains no tower/config reference.

It updates a valid target destination, captures its final point on death/arrival/TreeExiting, and keeps traveling when that target disappears. Contact reaches the destination center and dispatches `COMBAT/AreaDamage.ApplyInstant` once; it never applies an extra direct hit. Target acquisition range is not rechecked during flight. Travel performs no area scans or recipient allocations. HasDetonated/AppliedHits expose the completed impact result for diagnostics/tests.

The completion flag is set before damage callbacks, so repeated ticks cannot hit again. Timeout cancels without detonation; scene exit releases target subscriptions and prevents later effects. `FEEDBACK/Shockwave/ShockwaveEffect` renders a short damage-independent flash under the supplied world Effects layer. Missing visuals do not suppress combat. Zero blast radius hits only exact-center recipients, as defined by AreaDamage.

See BlasterCannon/README.md and TESTS/README.md for launch snapshots, rates, target loss, overrides/penetration, one-hit primary/nearby recipients and cleanup regressions. Phase4E travelling wavefronts use the separate ShockwaveEmitter and never repeatedly call this instant-disc mechanic.

Phase4E adds a trailing optional ShockwaveStyle argument to Initialize, preserving existing effects/lifetime call sites. Blaster supplies its own orange style; the projectile duplicates it at launch. On contact it first completes ApplyInstant, then spawns the shared autonomous visual with the snapshotted radius/style. No BlastFlash class remains. Source/effect style edits cannot change damage or trigger another contact.
