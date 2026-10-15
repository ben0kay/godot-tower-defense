# Shared Area Damage — Phase 4D implemented

`AreaDamage.cs` owns instantaneous radial recipient selection, linear distance falloff in base-damage units, and forwarding immutable hits. It knows nothing about tower IDs, weapon balances, projectile travel, firing cadence, research or visuals. Blaster Cannon is its first consumer; other instant explosions can call the same API.

## API and math

- `Validate(radius, edgeFraction)` rejects nonfinite/negative radius and nonfinite/out-of-range edge retention before mutation.
- `AtDistance(sourceHit, distance, radius, edgeFraction)` returns a new `DamageInfo` with a scaled amount and the unchanged type, optional absolute shield/hull overrides, penetration and optional DamageSource. Distance must be finite/nonnegative. Outside distance returns a zero-amount payload.
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

`TOWERS/Shared/Projectiles/SplashProjectile.cs` tracks a target/last-known point and dispatches this helper **once on contact**, using its launch snapshot. It never also deals a direct hit. Tower range is a launch constraint, not an impact constraint. `FEEDBACK/Shockwave/ShockwaveEffect.cs` adds a short cosmetic flash after damage is complete. Its expanding/fading ring cannot damage enemies.

## Regression coverage and status

`TESTS/AreaDamageChecks.cs` exercises center/half/edge/outside geometry, zero radius, 0/25/75/100% retention, invalid inputs, finite extreme amounts, unique recipients, mutation during a kill callback, queued/freed/unregistered targets, isolated rewards and 500 recipients. Defense fixtures cover Explosive shield-only, hull-only and break spillover plus absolute overrides and penetration. `BlasterCannonChecks` exercises actual fired delivery, launch snapshots, falloff tuning, paid construction/upgrades, caches/Inspector, rates and cleanup. See `TESTS/README.md` for actual build/headless results.

## Phase4E — expanding delivery implemented separately

Instant blast damage remains a full-disc event at contact. ShockwaveEmitter under TOWERS/Shared/Weapons/Shockwaves instead advances a swept moving front, tracks per-emission hit-once IDs and applies damage only on encounter. It reuses AtDistance at contact radius but never repeatedly invokes ApplyInstant. FEEDBACK/Shockwave draws both modes with weapon-specific styles; width/glow/fade do not affect recipient geometry. See the emitter README and ShockwaveChecks for implementation/tests. Research, saving, armour and Missile Tower remain deferred.

Tower XP attribution is carried in DamageInfo.Source and preserved by AtDistance. Each affected enemy independently bounds XP by its spawn-time damage budget; raw AoE amount is never directly converted to XP. A promotion during recipient dispatch does not change the already snapshotted attack, nor does the cosmetic effect grant XP. Existing instant/wave timing and defense math remain unchanged.
