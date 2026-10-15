# Area Damage — Phase 4D contract (PLANNED; no C# implementation yet)

## Purpose and ownership

Build one **small, reusable C# area-damage calculation** for the upcoming Blaster Cannon (Phase 4D), with a contract that also suits later Missile/Mortar blasts and expanding shockwaves. This folder owns **which recipients are in an area, distance-based damage scaling, and forwarding a hit**. It must NOT own projectile travel, target acquisition, firing cadence, tower balances, enemy health, shader/particle visuals, or research state.

**Keep the mechanisms distinct:**
- **Instant blast (implement in 4D):** at a single impact time, evaluate everyone currently inside a circle and apply each qualifying hit once immediately. Its visual animation may expand later, but must never delay or repeat damage.
- **Expanding wavefront (future Phase 4E; DO NOT IMPLEMENT in 4D):** a radius travels outward over time; damage occurs when the advancing annulus crosses an enemy, NOT to everyone inside the already-expanded disc. Future runtime ownership belongs under `TOWERS/Shared/Weapons/Shockwaves/`. The wavefront may reuse distance/falloff and the existing `DamageInfo`, but cannot call the instant full-disc attack every frame.

## Shared instant blast API (design, not rigid method names)

Provide a stateless helper (e.g. `AreaDamage.ApplyInstant(...)`) receiving an **impact world position**, **radius in world pixels**, **immutable base `DamageInfo`** (type, overrides, penetration), **weapon-supplied edge damage fraction**, and the living recipient collection / per-world `EnemyRegistry`. Return a small result if needed for tests/impact feedback. The helper must work independently of the Blaster Cannon class so another projectile can use it without branching on TowerId.

**Falloff is input data, never a shared balance constant.** For 4D, a simple *linear* rule is sufficient: with base damage `D`, distance `d`, radius `r > 0`, and edge fraction `e` in `[0,1]`:

`baseDamageAtDistance = D * (1 - (1 - e) * clamp(d / r, 0, 1))`

Center deals 100% of base damage, exactly at the edge deals `e` of base damage; `e = 1` is flat full damage and `e = 0` tapers to zero at the edge. This is **pre-defense base damage**: construct a hit with the same `DamageType`, optional shield/hull absolute overrides and shield penetration as the source, but with the reduced base amount. Then call the existing `Enemy.ApplyDamage(DamageInfo)` **once per recipient**; only `DamageResolver` decides shield/hull loss. Do not multiply global coefficients twice or convert shield damage into hull damage here.

**Required behavior / safety:**
- Use world-coordinate **enemy centers** and inclusive `distance <= radius` as the simple deterministic 4D hit rule (no physics collision body required); document/test radius-zero policy explicitly (recommended: zero radius hits only an enemy centered at the impact). Clamp/reject negative, NaN or infinite inputs before mutation. No arbitrary falloff minimum baked into the shared helper; the weapon owns its number.
- An explosion hits **every eligible living enemy in its radius**, including the initial target if still alive, **at most once**. There is no special direct-hit damage in addition to the AoE. Enemies outside the radius remain untouched. Do not require an enemy to still be inside the Blaster Tower's firing range after the projectile launches.
- Because `Enemy.ApplyDamage` can kill/unregister enemies immediately, **snapshot/collect recipients before applying hits** rather than mutate-enumerating `EnemyRegistry.Living`. Skip invalid, already killed, removed or queued-for-deletion entries at application time. One explosion must not pay a kill or trigger a death twice.
- Performance goal: one bounded registry scan **per impact**, not one scan per active tower every physics frame; no `Area2D` child or physics shape on every enemy/projectile for this first version. Avoid persistent temporary nodes/timers and per-frame allocations. Keep code clear and lightweight for eventual 500+ enemy scenarios.
- An AoE attack with `DamageType.Explosive` naturally uses the existing global Explosive `0.25x` shield/`1.25x` hull defaults unless its weapon opts into absolute per-hit overrides. Do not introduce global `SplashDamageMultiplier` or duplicate the damage-type table.

## Future expanding shockwave contract — Phase 4E only

A timed wavefront has center, previous radius, current radius, maximum radius, expansion speed (pixels/sec), visual ring thickness and a **per-emission hit-once set**. During a tick, detect enemies intersected by the **swept advancing ring** from old to new radius, rather than checking only the instantaneous thin ring at the new radius (which can skip targets on long frames). Later refine relative motion for fast-moving enemies if necessary. Each enemy receives a single `DamageInfo` when the front reaches it, using the weapon's own falloff at encounter distance. Visual ring thickness and animation must not alter combat damage; no repeated hits to enemies left inside the ring. Handle zero-speed, overshoot, dying enemies, scene cleanup and differing physics rates. The future wavefront should *reuse* the distance-falloff helper, not repeatedly invoke `ApplyInstant`.

See `TOWERS/Shared/Weapons/Shockwaves/README.md`. This future behavior is intentionally NOT part of Phase 4D.

## Phase 4D tests and scope

Add deterministic pure-area fixtures plus end-to-end Blaster integration. Cover zero/half/edge/outside distance, nonzero edge percentage, `e=0`, `e=1`, differing weapon falloffs, invalid settings, target killed/removed during the blast, multiple nearby targets, edge-inclusive geometry, one hit per recipient, 0-radius policy, shield-only/hull-only/overflow, overrides and future-proof penetration payloads. Tests should demonstrate that **changing Blaster falloff affects Blaster attacks but never changes shared combat defaults or other weapons**.

Do not build wavefront timers, permanent research, save files, tower ranks, armor or a new damage resolver in 4D. Once Codex implements and verifies Phase 4D, update the status and this README from planned to factual behavior.
