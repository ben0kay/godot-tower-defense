# Heavy Gunner projectile visual presets

**Refined 2026-11-05:** This heavy-only tracer grew by roughly 33% in both length and width from 11×2.2 to 14.6×2.9. The projectile remains tapered coral red with unchanged head multiplier, glow and fade; no projectile-system or starter-preset edits.

The Heavy Gunner uses separate, inspector-editable `HeavyGunnerTracer.tres` and `HeavyGunnerMuzzleFlash.tres`. Tracer is **14.6px long**, **2.9px core width**, tapered with a 1.3x head and faint deep-red glow; stays visibly red/coral rather than becoming a different damage type. Slightly larger 7×4px 80ms muzzle flash accompanies accepted shots.

The existing `EnemyMinigun` captures visual snapshots per shot; central `EnemyBulletSystem` renders these with no new per-shot nodes. Speed (650px/s), 2.5 shots/s and damage (5 Kinetic) are owned by `../HeavyGunnerMinigun.tres`, **not** by tracer style. Starter Gunner tracer and muzzle flash presets remain unchanged.
