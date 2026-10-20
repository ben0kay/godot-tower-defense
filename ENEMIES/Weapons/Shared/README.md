## EG2 delivery update — 2026-10-20

EnemyShotRequest now lives in this Shared folder and optionally carries a lifetime and immutable ProjectileVisualSnapshot. Actual accepted minigun requests go directly to the world-owned EnemyBulletSystem; ShotRequested observes accepted launches rather than acting as the delivery subscriber. Source/target references are absent from bullet state. Navigation and mount acquisition remain unchanged. See ../../Projectiles/README.md. Earlier EG1-only delivery-seam wording below is historical.

# Optional enemy weapon mount and targeting — EG1

EnemyWeaponMount is a Node2D capability added only to armed enemy scenes. Host can be an explicit ancestor reference or is found once from ancestry. PivotPath defaults to TurretPivot; MuzzlePath to TurretPivot/GunSprite/Muzzle. Both must resolve inside the mount, with the muzzle beneath the pivot. Use a Node2D grouping parent so transforms follow the hull. The gun holds world heading independently when the hull turns and reuses TurretAiming.Track; movement/checkpoints/Citadel assignment remain untouched.

EnemyWeaponConfig groups targeting, aiming and future bullet delivery inputs. TryCapture rejects invalid/nonfinite values; each mount stores a readonly EnemyWeaponSettings value. Editor/source changes do not mutate running state until Config is assigned or RefreshConfiguration is called. Enabled controls only this mount. Missing/invalid configuration, owner, pivot, muzzle or world service leaves it sleeping.

Initialized restarts private state and a stable stagger derived from mount identity. Closest-center searches happen at configured intervals (default0.2s) through World/TowerRegistry; scratch storage is retained. Cached locks are rechecked each active physics tick. No-target ticks only count down to the next search, and target loss never forces a per-frame rescan. Exact distance ties use registry insertion order. Constructing and disabled living towers can be targeted. Pause freezes clocks; Freeze stops this optional weapon while Cryo Slow affects movement alone. Removed/ExitTree stop work and release lifecycle subscriptions.

OnAimReady is the extension point. EG1 applies no damage and never creates bullets. Minigun/ adds burst requests for the future central EG2 delivery system. The default plain Enemy scene and all current catalog types remain unchanged.

Verification: TESTS/EnemyWeapons/README.md. Source reviewed only; build/runtime tests pending.
