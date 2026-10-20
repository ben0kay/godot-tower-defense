## Hostile energy-streak snapshot options — 2026-10-20

ProjectileVisualStyle/Snapshot append optional Tapered, TailWidthFraction, TailOpacity and HeadWidthMultiplier fields for the centralized hostile renderer. Defaults preserve original straight tracers and existing snapshot constructor call sites. Capture clamps new numeric values; EnemyBulletSystem sanitizes direct requests too. Friendly projectile rendering/configs remain unchanged. Shape affects presentation only. Build and focused enemy checks passed; broader ProjectileVisualChecks retains an unrelated Cryo placement-fixture failure tracked in TESTS/EnemyWeapons/README.md.
---

## EG3 enemy flash reuse — 2026-10-20

WeaponVisualRenderer now also accepts FlashAt(Marker2D, MuzzleFlashSnapshot) so optional enemy miniguns can retain spawn/config-captured appearance. Existing FlashAt(Marker2D, MuzzleFlashStyle) delegates through the same code and keeps tower behavior. ClearFlashes releases transient slots and sleeps without altering aim or counters. Only accepted hostile shots call this renderer; capacity-rejected shots do not flash. No per-shot objects/timers or unrelated tower scene changes. Build/runtime verification remains pending.

# Projectile and muzzle presentation — PV1/PV2

Implemented shared visual-only tracer/flash resources. Weapon damage, targeting, ammo, XP, cadence and the eight-stage combat stat pipeline remain their existing owners. Battle now launches from its real barrel-tip marker rather than its center. Blaster's splash and travelling Shockwave delivery are separate and unchanged.

## Authoring another projectile weapon

Create a `ProjectileVisualStyle` and a `MuzzleFlashStyle` resource in that tower's `Visuals/` folder. Assign them to the tower config's **Weapon Visuals** exports. Set colors, tracer length/thickness/glow, flash duration/size/opacity and primitive. Existing shared projectile turrets need no additional visual C# class or tower-type enum. Battle uses the same adapter with its own firing loop. New custom weapon loops call `WeaponVisuals.Present` only after adding an actually funded bullet.

Flash primitives: Cone, Starburst, Ring and optional Texture (missing texture falls back to Starburst). Tracers support optional artwork too. Artwork points right (+X); the tip is the tracer origin and its tail extends left. Muzzle artwork extends right from its origin. Layered translucent shapes provide inexpensive faux glow without lights, particles, materials or environment bloom.

`BasicProjectile` copies sanitized style values into a value snapshot at launch. Guided travel updates node rotation, keeping cached local draw geometry aligned to motion. Length never changes contact radius, damage or lifetime. Missing style retains the previous yellow circle / cyan status-bullet circle; disabled style hides art without disabling damage. Authored style resources/textures are shared read-only; changing a preset affects subsequent launches, not active snapshot values. Texture assets themselves should not be modified at runtime.

## Flash lifecycle and cost

One `WeaponVisualRenderer` belongs to each rotating turret, with 32 reusable flash slots and one eight-vertex polygon buffer. Each successful launch adds one slot at the exact chosen marker. Sequential A/B and simultaneous volleys retain existing barrel selection and ammo transactions. Cooldown, invalid targets, unfinished construction and unfunded volleys produce no flash. Full capacity overwrites slots round-robin; it never drops gameplay shots. Expiry clears references and stops processing; tower removal frees its renderer while independently owned bullets continue.

There are no per-shot flash nodes, timers, materials or Resource copies. Only live flashes enable renderer processing; aiming is supplied by the existing weapon tick. This is bounded presentation, not a projectile pooling rewrite. The microbenchmark measures flash emission/expiry, not graphics/GPU cost or map FPS.

## Sniper aiming line

Sniper owns `SniperAimLaserStyle.tres`: a subtle 0.7px translucent red barrel-forward line and tiny tip. It follows turret rotation toward the current target distance, capped by weapon range. It appears while tracking a valid target with ammunition, including cooldown, and clears on the next weapon tick after target loss/empty ammo. It adds no raycast, targeting query, damage, accuracy modifier or separate processing loop. Disable/adjust it via the optional Aim Laser config resource; other towers have no assigned aiming style.

Tests and actual limitations: [ProjectileVisuals.md](../../../../TESTS/Towers/ProjectileVisuals.md). Desktop normal-zoom appearance still requires manual review.

## Visibility / barrel-origin follow-up — 2026-10-17

Battle/Machine Gun/Sniper/Cryo launch at their existing Marker2D tips; Blaster now has its own marker `(0,-30)` and an orange launch flash. Laser's continuous beam starts at its existing muzzle; Shockwave intentionally emits a centered circular front. Splash still applies one instant AoE only on arrival, never at launch. Blaster flight time is now measured from its barrel tip rather than its center.

To address hard-to-see flashes, presets are larger and last Battle160ms / Machine Gun90ms / Sniper160ms / Cryo120ms / Blaster180ms. Shared rendering holds full brightness for the first30% then fades linearly, replacing the rapid squared fade. New slots skip their first processing delta so a long first frame cannot expire them before a presentation opportunity. The renderer uses relative Z3 (World/Towers5 →8), above projectiles6 and ordinary effects7. Fixed32-slot capacity, no idle processing and zero per-flash node/resource creation remain. This does not add recoil, lights, damage or change the latest20% rate rebalance. Desktop visual confirmation remains pending.
