## Continuous default fire — 2026-10-20

DefaultEnemyMinigun now explicitly sets UseBursts=false:6 shots/s continuously while a valid target is aligned. BurstSeconds/RestSeconds remain available only through explicit UseBursts opt-in. Bounded fractional cooldown carry preserves cadence and never emits catch-up volleys. The default also sets LockedTargetSearchSeconds=1 and TargetSearchSeconds=0.2. Visuals/EnemyMinigunTracer.tres now opts into9px tapered energy streaks; bullets remain unguided Kinetic rounds. The previous burst/rest description below is retained as history. Build and focused enemy checks passed; desktop appearance still needs review.
---

## EG3 authored minigun and flash — 2026-10-20

DefaultEnemyMinigun.tres is now assigned to the actual Gunner Drone. Visuals/EnemyMinigunTracer.tres and EnemyMinigunMuzzleFlash.tres provide dedicated7×1.3px muted-red tracers and5×3px65ms warm flashes. EnemyMinigunConfig.MuzzleFlash is optional and captured per initialize/config assignment alongside ProjectileVisual. A WeaponVisuals child uses the existing bounded WeaponVisualRenderer; only an accepted central shot flashes. No renderer child is added automatically to plain enemies or synthetic mounts without one. Stop/reinitialize/disable clears flash slots, not world bullets. Presets remain read-only; no random spread is added. The new scene/art/F2 registration is EG3 source-complete; build/runtime/visual acceptance is pending. Older type/flash-deferred text below is historical.

## EG2 delivery — source implemented (2026-10-20)

EnemyMinigun caches World/EnemyBullets on initialization/config capture, then submits one real shot attempt per cadence opportunity. Accepted launches increment ShotRequests and publish ShotRequested as an observation event. Overflow never queues delayed catch-up bullets. Shots remain owned by the world after this gun/host exits. Missing delivery service cannot create a fake hit.

EnemyMinigunConfig adds optional ProjectileVisual and BulletLifetimeSeconds(default2.5); both are captured per initialization/config assignment. Null visual uses the world's generic7×1.3px muted-red fallback. No spread or muzzle flash is added in EG2; dedicated Gunner Drone visual presets and F2 scene remain EG3. Existing enemies/waves stay unchanged. Earlier seam-only statements below are historical. Build/runtime/visual verification remains pending.

# Enemy minigun timing seam — EG1

EnemyMinigun extends EnemyWeaponMount with a private burst/rest/cooldown clock. Assign EnemyMinigunConfig: default range260px,240deg/s turn,6deg aim tolerance,0.2s searches,6 requests/s,1.5s burst,1s rest,2 damage and1200px/s future bullet speed. Settings are tentative and Inspector editable; no default .tres or existing armed enemy is introduced until EG3.

ShotRequested publishes readonly EnemyShotRequest values: actual muzzle world origin, forward direction, damage and speed. One request maximum per tick prevents hitch catch-up floods; no subscriber means no emitted/countable requests. Whole-world bullet ownership and genuine Kinetic hits are EG2. The event is not a direct-hit mechanism and contains no retained enemy/tower references. Losing target/alignment, disable, reinitialize or removal clears cadence; pause holds it. Requests already consumed by a future world manager must remain independent of source lifetime.

No projectile nodes, tracer/flash assets, spread, catalog/F2 entry or wave changes are part of EG1. Small muted-red visual presets remain EG3. Build/runtime verification remains pending; see TESTS/EnemyWeapons/README.md.
