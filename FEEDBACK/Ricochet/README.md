# Shield Ricochet — Codex implementation handoff

**Status: PLANNED / NOT IMPLEMENTED.** This folder is a documentation-only scaffold. No gameplay or effect code has been added.

## Goal

When a **Battle Tower, Machine Gun Tower, or Sniper Tower** bullet strikes and actually damages an enemy's shield, resolve the original bullet's collision and damage **exactly once** and remove it normally. Optionally create a **purely cosmetic imitation projectile** at the shield surface that reflects according to the incoming direction and surface normal, then loses speed, bends gently, fades and expires. It must not perform collisions, deal damage, affect shields/status, consume ammo, award XP, target enemies or trigger another ricochet.

### Prerequisites and conventions

Read root `README.md`, `NOTES/CodePreferences.md`, `FEEDBACK/README.md`, `FEEDBACK/Shields/README.md`, `TOWERS/Shared/Projectiles/README.md`, `TOWERS/Shared/Weapons/Visuals/README.md`, `COMBAT/Damage/README.md`, `TESTS/README.md`, and **current implementations** first. Feature documentation may be historical. Use compact modular C#, region groupings, descriptive top-of-file comments, and the user's `// =========================================================` line plus comment **above methods**. Avoid per-effect scene nodes, duplicate logic and expensive allocations. Make **one GitHub commit for the complete implementation**, including tests and updated READMEs.

## Existing integration points (reviewed 2026-10-21)

- `TOWERS/Shared/Projectiles/BasicProjectile.cs` is straight by default with optional homing. It moves by a launch-snapshotted `TravelDirection`, queries swept circle contact with `ProjectileEnemyContact.First(_registry, from, end, out entry)`, sets position to `from.Lerp(end, entry)`, queues itself for deletion, and invokes `hit.ApplyDamage(_damage)` with optional status afterward.
- `ProjectileEnemyContact.cs` uses a spatial registry visitor, real enemy `Definition.RadiusPixels + 3` collision, and deterministic earliest-entry selection. **This gameplay hit circle is not necessarily the visible shield ellipse.**
- `ENEMIES/Shared/Enemy.cs` owns shield points, `ApplyDamage(DamageInfo)` and `CommitDamage`; `COMBAT/Damage/DamageResolver` owns shield/hull absorption and penetration. Existing shield art is cosmetic and scaled to sprite size via `FEEDBACK/Shields/ShieldVisual.cs`. No ricochet for unshielded or bypass-only shots.
- `ProjectileVisualStyle.Capture()` provides lightweight `ProjectileVisualSnapshot` (length, width, colors, texture). Reuse/simplify these visuals for fake spent rounds; do not load an asset or snapshot a Resource on each impact.
- `FEEDBACK/FeedbackMasterConfig.cs` and `FEEDBACK/DefaultFeedbackConfig.tres` already own global effect toggles; `World/Effects` is used for independent visual effects.

## Responsibilities and proposed files

This folder owns the effect only, not projectile gameplay:

```text
FEEDBACK/Ricochet/
  README.md                       # this implementation handoff
  RicochetController.cs           # future: bounded active effects, spawn/update/cleanup
  RicochetRenderer.cs             # future: one batching/drawing owner, no nodes per particle
  RicochetStyle.cs                # future: Inspector-editable, sanitised visual parameters
  DefaultRicochetStyle.tres       # future: one shared default configuration
```

**Only README.md is created in this scaffold.** Add C# and .tres files when Codex implements the system. Prefer one world-owned controller plus renderer per battlefield (or combine if truly simpler). Never add a `Node2D`, `Area2D`, `RigidBody2D`, or tween per ricochet; prefer a fixed-capacity array/struct pool and one update/draw callback. If no world-owned effect service currently provides safe lifecycle ownership, wire an optional shared effect node under `World/Effects` without altering existing level scenes individually.

## Impact decision — must be reliable

Not all projectile contacts deserve a bounce. Determine actual **positive shield absorption** from the authoritative damage result or an explicit minimal combat event/return value. Do not infer shield damage solely from `CurrentShield > 0` before calling `ApplyDamage`; it might be fully penetrated, zero-effective or invalid. Do not rerun damage resolution for feedback. Guard against an enemy being deleted by a lethal callback; capture necessary position, ship transform/radius, movement direction, damage snapshot and visual snapshot **before** applying damage, then use confirmed outcome. If this needs a new narrow API, document who owns it, preserve the existing `void ApplyDamage` callers, and keep shared combat free of dependencies on FEEDBACK.

Initial weapon eligibility: **Battle, Machine Gun, Sniper** using `BasicProjectile` with Kinetic impact. Other `BasicProjectile` users (Cryo/Stasis, etc.) must not accidentally gain ricochets. Keep eligibility in a typed weapon policy or immutable launch flag from the relevant tower/config (no string tower-ID checks). Later weapons may opt in. Maintain original shot damage, shields, armor, projectile steering/spread, ammo, status, XP, recoil and aim.

## Surface geometry and believable reflection

The goal is **angle-aware mirror reflection**, not a random outgoing direction:

- Let `d` be the **actual normalized incoming travel direction at impact**. For guided bullets use instantaneous direction at contact, not the original muzzle orientation.
- Let `c` be the shield center in **world coordinates** (aligned to its sprite/visual presentation).
- Compute a **visual-only** intersection of the incoming segment/ray against the current shield ellipse, transformed into the shield's local axes. The shield scales independently in X/Y; include scale and rotation as actually shown in the current scene. Do not assume a circle or use the projectile's gameplay collision point as the visible rim.
- At the visual ellipse boundary `p`, the outward normal in ellipse-local coordinates is `normalize((x/a², y/b²))`; transform it back to world space using inverse-transpose for any rotation/non-uniform scaling, then normalize.
- Reflection: `r = normalize(d - 2 * dot(d, n) * n)` (Godot `Bounce(n)` equivalent when `n` is correct and the shot is entering). Test/guard `dot(d,n) < 0`, degeneracy and non-finite inputs.
- **Head-on:** incoming along `-n` reflects almost exactly along `+n`, backwards toward the gun. **Glancing:** outgoing retains a large tangential component and skims away, rather than swinging unrealistically across the bubble. At tangent/near tangent, optional probability skip and slightly outward positional bias prevent a fake shot appearing to travel inside the shield.
- Small optional jitter (default ≤ ±2–3°) only **after** correct reflection, never a wholesale randomized direction. The fake begins at `p + n * smallOffset` to appear just outside the shield.
- If segment is entirely inside the visual ellipse, starts inside, fails to intersect, or orientation/size is invalid, use a deterministic conservative fallback or suppress the visual (no gameplay change). Note: gameplay collision may occur *inside* the drawn shield edge, so the visual intersection may be earlier along the incoming **extended line** than the actual gameplay contact; make the presentation-only reconciliation explicit and test it.
- Use the effective per-enemy visible shield dimensions (currently based on `ShieldVisual.SetVisualSize` and style size multiplier). Prefer exposing a minimal read-only geometry snapshot from that presentation component rather than recalculating guessed dimensions in this effect.

## Cosmetic flight

At spawn capture position, reflected direction, source tracer presentation, and fixed parameters. Suggested tunable values (playtest, not contractual): spawn chance **35%**, retained speed **45%** of incoming speed, lifetime **0.45–0.65 s**, gentle extra turn **0–20°** over its lifetime, fade toward zero, and length/brightness shrinking as speed decays. Use an ease-out speed curve, smoothly integrate direction/position; curvature must begin at the **physically reflected direction**, not be applied as an initial arbitrary deviation. Consider curvature scaled down for direct impacts; never reverse an otherwise plausible reflection. One world-owned update system drives all fake rounds; renderer draws thin streaks oriented to instantaneous visual velocity, not static source rotation. Source bullet has already disappeared. The ricochet remains world-space after the enemy moves/dies. Hard expiry and full cleanup on pause/unload.

## Inspector controls / safety budgets

`RicochetStyle` should export grouped, clamped fields: `Enabled`, `ChancePercent`, `SpeedRetention`, `Deceleration`, `LifetimeSeconds`, `CurveDegrees`, `AngleJitterDegrees`, `LengthMultiplier`, `Opacity`, and `MaxActiveRicochets`. Set sensible bounds, finite checks, defaults suitable for fast machine-gun fire. Add `ShieldRicochetsEnabled` to `FEEDBACK/FeedbackMasterConfig.cs` and specify its default in `DefaultFeedbackConfig.tres`. **Global OFF must skip spawn/drawing work** and not modify gameplay; count/skip overloads without creating queues or reallocations. Make the style resource shared and avoid duplicating it per bullet. No heavy GpuParticles per bullet or collision inquiries for cosmetic rounds.

## Implementation passes for Codex

**SR1 — Infrastructure:** create shared style/resource and bounded controller/renderer; add explicit master toggle; event-driven lifecycle and deterministic spawn API accepting value snapshots. Unit-check independent motion, fade, caps, pause/unload, no gameplay nodes.

**SR2 — Surface reflection:** implement a pure/testable ellipse-ray/segment geometry helper and outward normal/reflection. Verify rotations, nonuniform scaling, head-on, tangent, glancing, degenerate cases; ensure visual point aligns with the **actual shield draw**, not merely `EnemyDefinition.RadiusPixels`.

**SR3 — Actual shield-hit bridge:** only eligible Battle/Machine Gun/Sniper projectile impacts with real positive shield damage call the visual service; keep damage once and original bullet disposal, no double hit/XP. Integrate with actual damage result without introducing feedback dependencies into `COMBAT/Damage`. Do not enable on shields that absorb zero.

**SR4 — Validation/documentation:** test visual first-shot alignment, high bullet rate and 100s of enemies, overlay/profile disabled-versus-enabled, no per-round allocations/nodes/physics, shield fade and destruction/respawn, and ensure separate Laser/Blaster/Rocket/Cryo/Stasis systems are unaffected. Build Godot .NET and run focused headless suites (when available). Update README(s) and `NOTES/WORK.md` with **observed** statuses only, not unverified success. One commit total for SR1–SR4.

## Acceptance

1. Normal impact: bounced tracer returns almost along incoming line, not sideways.
2. Glancing impact: bounced tracer continues primarily tangent to the surface, visibly outward.
3. Launch point is on red shield's **visible ellipse edge**; fake never originates from the hull center.
4. Damage is applied once; real projectile removed once; ricochet deals no damage and earns no XP.
5. Zero absorption, no shield, disabled toggle or budget overflow produces no visual; no change to damage.
6. Bullet style resembles its source, but shortens/slows/fades. Motion can curve gently **after** correct reflection.
7. Batched bounded rendering remains responsive under Machine Gun volleys, with no new nodes/queries per ricochet.

**Out of scope:** actually bouncing live projectiles, new damage types, shield balance, deflection affecting subsequent enemies, a 3D simulation, and adding ricochet behavior to hostile enemy bullets.
