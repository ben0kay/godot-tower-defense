# Heavy Gunner projectile visual presets

The Heavy Gunner uses separate, inspector-editable `HeavyGunnerTracer.tres` and `HeavyGunnerMuzzleFlash.tres`. Tracer is **11px long**, **2.2px core width**, tapered with a 1.3x head and faint deep-red glow; stays visibly red/coral rather than becoming a different damage type. Slightly larger 7×4px 80ms muzzle flash accompanies accepted shots.

The existing `EnemyMinigun` captures visual snapshots per shot; central `EnemyBulletSystem` renders these with no new per-shot nodes. Speed (850px/s) and damage (4 Kinetic) are owned by `../HeavyGunnerMinigun.tres`, **not** by tracer style. Starter Gunner tracer and muzzle flash presets remain unchanged.
