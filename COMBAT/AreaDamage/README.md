# Shared Area Damage — Phase 4D implemented

`AreaDamage.cs` owns instantaneous radial recipient selection, linear distance falloff in base-damage units, and forwarding immutable hits. It knows nothing about tower IDs, weapon balances, projectile travel, firing cadence, research or visuals. Blaster Cannon is its first consumer; other instant explosions can call the same API.

## API and math

- `Validate(radius, edgeFraction)` rejects nonfinite/negative radius and nonfinite/out-of-range edge retention before mutation.
- `AtDistance(sourceHit, distance, radius, edgeFraction)` returns a new `DamageInfo` with a scaled amount and the unchanged type, optional absolute shield/hull overrides and penetration. Distance must be finite/nonnegative. Outside distance returns a zero-amount payload.
- `ApplyInstant(impactWorldPosition, radius, sourceHit, edgeFraction, enemyRegistry)` dispatches one hit to every eligible enemy center at inclusive distance `<= radius`. The return value counts dispatched recipients, including valid zero-amount edge hits. Invalid impact coordinates or registry reject before mutation.

For radius r > 0, base damage D, center distance d and weapon-supplied edge fraction e:

`damage = D * (1 - (1 - e) * clamp(d / r, 0, 1))`

Center retains 100%; the exact edge retains e. Zero edge retention has no hidden minimum; e=1 gives flat damage. **The shared helper has no default edge fraction or other weapon balance.** Radius zero hits only an enemy centered exactly at the impact point, at full base damage. Geometry uses world enemy centers, not body overlap or target radius. Nonfinite enemy positions are skipped.

| Example: 60 base / 80 px / 25% edge | Base damage before defenses |
| --- | ---: |
| Center | 60 |
| Half radius (40 px) | 37.5 |
| Exact edge (80 px) | 15 |
| Outside (greater than 80 px) | 0 |

`Enemy.ApplyDamage(DamageInfo)` remains the only recipient entry point; existing `DamageResolver` applies inherited or explicitly overridden shield/hull responses and penetration exactly once. Explosive defaults remain 0.25x shield / 1.25x hull. There is no second damage-type table, global splash multiplier or direct-hit bonus.

## Snapshot and performance contract

Each impact scans its per-world registry once, collecting eligible recipients and their distance-scaled payloads before applying anything. Immediate kill/unregister callbacks cannot invalidate enumeration. Registry membership uses a HashSet alongside the existing living list: constant-time uniqueness/membership lets the application pass skip recipients killed, freed, queued or unregistered by an earlier callback without rescanning the list. Native validity, living state and tree membership are rechecked before each hit.

Temporary recipient storage is local to the impact, making nested impacts independent. There are no persistent helper nodes, physics shapes, timers, per-tower area polling or per-frame area scans. Existing list removal/death feedback still belongs to the enemy lifecycle. The headless suite includes one 500-recipient blast with no scene-node allocation by this helper; timing is reported as a measurement, not a hardware-independent FPS promise.

## Delivery and visual ownership

`TOWERS/Shared/Projectiles/SplashProjectile.cs` tracks a target/last-known point and dispatches this helper **once on contact**, using its launch snapshot. It never also deals a direct hit. Tower range is a launch constraint, not an impact constraint. `FEEDBACK/AreaBlast/BlastFlash.cs` adds a short cosmetic flash after damage is complete. Its expanding/fading ring cannot damage enemies.

## Regression coverage and status

`TESTS/AreaDamageChecks.cs` exercises center/half/edge/outside geometry, zero radius, 0/25/75/100% retention, invalid inputs, finite extreme amounts, unique recipients, mutation during a kill callback, queued/freed/unregistered targets, isolated rewards and 500 recipients. Defense fixtures cover Explosive shield-only, hull-only and break spillover plus absolute overrides and penetration. `BlasterCannonChecks` exercises actual fired delivery, launch snapshots, falloff tuning, paid construction/upgrades, caches/Inspector, rates and cleanup. See `TESTS/README.md` for actual build/headless results.

## Future Phase 4E — expanding wavefront remains deferred

An instant blast damages a full disc at one moment. A true shockwave must instead damage when a swept advancing annulus crosses an enemy, with a per-emission hit-once set and previous/current radii so long frames cannot skip targets. It may reuse `AtDistance` at encounter distance, but must **not** invoke `ApplyInstant` on the already-expanded disc each tick. Visual thickness is independent of damage geometry.

The complete future contract remains in `TOWERS/Shared/Weapons/Shockwaves/README.md`. No shockwave runtime, research, saving, armour, Missile Tower or unrelated mechanics were added in 4D.
