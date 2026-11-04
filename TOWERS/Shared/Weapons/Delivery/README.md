## HI1 — Sniper instant-hit hull sparks (2026-11-04)

The instant-target weapon helper accepts an **optional** cached `HullHitSparkController`, muzzle position and count; existing callsites with no extra arguments remain valid. Sniper passes its actual muzzle snapshot and requests a small hull-edge burst only after a confirmed decrease in `Enemy.Health`. Its damage, target validation, recoil, XP and status effects are unchanged. Other instant-delivery weapons remain opt-out by default; physical projectile weapons use their swept collision point instead. Source implemented, Godot presentation still to verify.

---

# Shared weapon hit delivery

Reusable **opt-in** delivery for aimed turret shots. `ShotDeliveryMode` has `PhysicalProjectile = 0` (default) and `InstantTarget = 1`. `ProjectileTurretConfig.DeliveryMode` exposes the choice under the Inspector's **Firing** group and is retained when the resource is duplicated for placed towers.

- **PhysicalProjectile (default):** existing `BasicProjectile` creation at the actual muzzle, launch spread, guidance mode, swept collisions, tracer visuals and optional cosmetic shield ricochet. Existing towers without an explicit delivery value behave exactly as before.
- **InstantTarget (opt-in):** the existing shared turret still acquires a visible/detectable target, aims, and pays ammo. A funded shot creates a muzzle flash and recoil, records heat and cooldown, then invokes `WeaponHitDelivery.ApplyInstant` on the locked valid enemy. No travelling projectile, per-frame collision, raycast, or targeting bypass. A target that is already dead/removed cannot take damage. Damage uses the same typed `DamageInfo`, shield/hull resolver, attribution/XP and kill logic; optional status is applied only to a surviving enemy.
- **Cosmetics and configuration:** instant shots do not use tracer style, speed, spread or homing; those settings stay authored for reversible physical mode. Shield ricochet effects require physical projectile contact and are not emitted by instant delivery. Aim laser, muzzle flash and gun recoil remain as configured.

**Currently opted in:** Sniper Tower. **Still physical by default:** Machine Gun, Cryo, Stasis, and every other weapon not explicitly configured otherwise. Other weapon families (e.g. Battle, Rocket, Laser) are unchanged by this addition; a future weapon can explicitly call `WeaponHitDelivery` if it needs the capability, without copying damage math.

The shared turret commits funded shot bookkeeping before instant damage callbacks to be safe when a lethal hit completes a wave. Owner handles in-editor gameplay/visual/FPS acceptance; no lengthy test suite required.
