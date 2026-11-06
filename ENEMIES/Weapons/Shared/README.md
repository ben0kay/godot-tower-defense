## Shared weapon terrain vision — 2026-11-06

**Source Implemented / Awaiting Owner Validation.** Mountains now block enemy weapon acquisition, aiming and firing as well as friendly tower targeting. `WorldGrid.HasWeaponLineOfSight(from, to)` is the neutral shared API, using the existing allocation-free cached terrain traversal. `HasTowerLineOfSight` remains a compatibility wrapper; the existing `blocks_tower_sight` tile metadata, blocker count/cache, corner rules and no-blocker fast path are preserved. Friendly target queries and the selected red/visible range overlay call the neutral API; no second visibility cache, terrain scan, physics ray system or global fog system.

Enemy `TowerRegistry` exposes its already-cached owning grid. `EnemyTowerTargeting.Valid` and `Closest` use that same terrain test, with an optional sight origin independent of range origin. Enemy mounts pass their **own muzzle** for sight while retaining pivot-based range/closest scoring and stable registration-order ties. Only candidate score contenders traverse terrain, so a hidden nearest tower cannot suppress a visible fallback. Existing staggered acquisition and lock-hold intervals remain. A newly obstructed lock is cleared before aiming/firing; if aiming/countering hull rotation moves an aligned muzzle, sight is revalidated before emission. The redundant identical pre-aim lock check was removed. All four Capital guns and both Gunner variants inherit this behavior through `EnemyWeaponMount`; no per-ship branches or new processing loops.

Projectile flight/collision, already-emitted bullets, damage, splash/pulses, repair, stealth rules, navigation, tile flags, weapon balance and saves are unchanged. This pass concerns weapon line of sight only.

**Minimal validation:** focused source/diff checks only; .NET unavailable. Existing MountainSightChecks was extended with neutral/legacy API parity, reverse blockage, hostile hidden-target rejection/visible fallback, independent offset muzzle visibility, and a real Heavy Gunner mount refusing shots until terrain removal. C# checks are authored but **not executed** here; no build, gameplay or performance claim. Owner: rebuild, place a mountain between a tower and Gunner/Capital, verify neither side targets/fires through it, move around the mountain and inspect each Capital muzzle independently. Already-fired projectile collision remains unchanged by design.


---

## Separate locked/idle searches — 2026-10-20

EnemyWeaponConfig now captures TargetSearchSeconds(default0.2) and LockedTargetSearchSeconds(default1). A successful query starts the full locked interval. Cached locks are still validated each tick; invalid locks clear immediately and reduce the next query delay to the idle interval. No all-tower scan, extra timer, navigation modification or per-tick allocation is added. TargetHold in EnemyWeaponChecks passes with a closer challenger, a full hold, range loss and destruction.
---

## EG2 delivery update — 2026-10-20

EnemyShotRequest now lives in this Shared folder and optionally carries a lifetime and immutable ProjectileVisualSnapshot. Actual accepted minigun requests go directly to the world-owned EnemyBulletSystem; ShotRequested observes accepted launches rather than acting as the delivery subscriber. Source/target references are absent from bullet state. Navigation and mount acquisition remain unchanged. See ../../Projectiles/README.md. Earlier EG1-only delivery-seam wording below is historical.

# Optional enemy weapon mount and targeting — EG1

EnemyWeaponMount is a Node2D capability added only to armed enemy scenes. Host can be an explicit ancestor reference or is found once from ancestry. PivotPath defaults to TurretPivot; MuzzlePath to TurretPivot/GunSprite/Muzzle. Both must resolve inside the mount, with the muzzle beneath the pivot. Use a Node2D grouping parent so transforms follow the hull. The gun holds world heading independently when the hull turns and reuses TurretAiming.Track; movement/checkpoints/Citadel assignment remain untouched.

EnemyWeaponConfig groups targeting, aiming and future bullet delivery inputs. TryCapture rejects invalid/nonfinite values; each mount stores a readonly EnemyWeaponSettings value. Editor/source changes do not mutate running state until Config is assigned or RefreshConfiguration is called. Enabled controls only this mount. Missing/invalid configuration, owner, pivot, muzzle or world service leaves it sleeping.

Initialized restarts private state and a stable stagger derived from mount identity. Closest-center searches happen at configured intervals (default0.2s) through World/TowerRegistry; scratch storage is retained. Cached locks are rechecked each active physics tick. No-target ticks only count down to the next search, and target loss never forces a per-frame rescan. Exact distance ties use registry insertion order. Constructing and disabled living towers can be targeted. Pause freezes clocks; Freeze stops this optional weapon while Cryo Slow affects movement alone. Removed/ExitTree stop work and release lifecycle subscriptions.

OnAimReady is the extension point. EG1 applies no damage and never creates bullets. Minigun/ adds burst requests for the future central EG2 delivery system. The default plain Enemy scene and all current catalog types remain unchanged.

Verification: TESTS/EnemyWeapons/README.md. Source reviewed only; build/runtime tests pending.
