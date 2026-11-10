# STG0 — Mass-Based Enemy Impact Stagger (Blaster Cannon pilot)

**Repository:** `ben0kay/godot-tower-defense` · **Branch:** `dev` only  
**Date:** 2026-11-10  
**Status:** **Approved concept / IMPLEMENTATION PLAN ONLY. No stagger code or balance changes have been implemented by this document.**  
**Related brainstorm:** `NOTES/Ideas.md` → **M17 — Mass-based directional impact stagger**.

## 1. Player-facing behaviour and scope

When an enemy ship is struck by a **projectile explicitly assigned a nonzero impact mass**, compare that projectile's impact mass with the struck ship's authored impact mass.

- A relatively light ship can **jolt a few pixels in the direction of the impact**, then its **visible hull eases back** into normal alignment. This is *only presentation*; do not move the enemy's authoritative `Node2D.GlobalPosition`, collision body, path waypoint, clearance footprint, spatial index or actual route sideways.
- The enemy may temporarily **lose a small percentage of forward movement speed**, then smoothly/automatically resume normal Citadel-bound travel. This is the only gameplay effect; no actual world-space knockback, path deviation, stun/immobilization, wall collision, A* repath or long-duration slowing.
- Bigger/heavier ships resist or entirely ignore light impacts. Strength and eligibility derive from **authored relative masses**, not visual size, class name, weapon damage or hull HP.
- **Initial opt-in: Blaster Cannon's physical shell / splash impact only.** Every other tower/projectile—including Rocket Tower, which derives its config from Blaster—must continue with **zero impact mass** until separately authored. Laser, beams, shockwave rings, damage-over-time and enemy weapons do not accidentally inherit the feature.
- Add a single user-requested Inspector master switch in `FEEDBACK/FeedbackMasterConfig.cs` and `FEEDBACK/DefaultFeedbackConfig.tres`: **`ImpactStaggerEnabled` (default true)**. Setting it **false disables the entire effect** (both momentary movement slowdown and visual shove), not just the animation. This is explicitly a gameplay-affecting exception to the ordinarily cosmetic FEEDBACK toggles. Do not add a Settings menu option or change other feedback flags in STG0.

**Important mental model:** The enemy's real position continues along its pre-existing path; only its *forward speed* dips briefly. The artwork appears to recoil away from the blast, then recovers. The Citadel is still its destination.

## 2. Existing code and integration seams checked on dev

| Current owner | Why it matters |
| --- | --- |
| `TOWERS/Offensive/BlasterCannon/BlasterCannon.cs` + `BlasterCannonConfig.cs` | A funded cannon shot creates `SplashProjectile`; its config/weapon owns opt-in mass. Don't attach impact to the generic `Explosive` damage type. |
| `TOWERS/Offensive/RocketTower/RocketTowerConfig.cs` + `RocketTower.cs` | Rocket config inherits Blaster config and also launches `SplashProjectile`; default inherited impact mass **must remain zero** for Rocket. |
| `TOWERS/Shared/Projectiles/SplashProjectile.cs` | Resolves one actual impact point and calls `AreaDamage.ApplyInstant` exactly once at detonation. Preserve straight/homing, spread, light, lifetime and shockwave presentation. |
| `COMBAT/AreaDamage/AreaDamage.cs` | Already queries nearby enemies once, snapshots recipients and forwards their attenuated `DamageInfo` once. Reuse this recipient pass for mass impacts: **no second area query or per-enemy blast search**. |
| `ENEMIES/Shared/EnemyDefinition.cs` | Add an Inspector-edited impact mass per enemy type. Do not compute it from sprites, RadiusPixels, EnemyClass, HP or Rank. |
| `ENEMIES/Movement/EnemyMovement.cs` + `ENEMIES/Shared/Enemy.cs` | Already calculate `EffectiveMoveSpeed = MoveSpeed × status movement multiplier` and walk cached safe routes. Compose a bounded transient stagger multiplier here; never rewrite routes or set a displaced enemy root. |
| `ENEMIES/Shared/GenericEnemy.tscn` + `Enemy.cs` | The hull Sprite, separate Thruster, Movement and DefenseBar are rooted on the Enemy. Visually offset only compatible presentation children; preserve root position, status tint, damage texture selection, shield center and weapon mounts. |
| `FEEDBACK/FeedbackMasterConfig.cs` + `DefaultFeedbackConfig.tres` | Existing master toggles; cache/load the shared toggle once rather than accessing the resource per hit or per ship frame. |

Nothing in the existing shared damage resolver (`DamageInfo` / `DamageResolver`) or existing Slow/Freeze status effects needs redefining. Do not add a generic stun ability system just for this one effect.

## 3. Authored settings (proposed initial tuning, not fixed balance)

### Weapon impact mass — opt-in, default zero

Add an exported nonnegative finite `ImpactMass` (or equivalent clearer name) under an **Impact Stagger** Inspector group on the appropriate Blaster projectile/config policy. Default **0 = no stagger capability**. Set the **Blaster Cannon default config resource** to approximately **2.0** for the first pilot; keep **Rocket Tower = 0** (including inherited defaults). Capture the mass at *launch* into the projectile's immutable firing context so live config edits/level upgrades never change a round in flight. Don't infer it from damage, projectile speed, projectile visual style, recoil, shield penetration or fire cadence.

If STG0 wiring exposes mass on an inherited config shared with Rocket, explicitly verify that Rocket's effective authored value remains zero. Future projectile types can reuse this typed capability and delivery seam; they don't automatically opt in.

### Enemy impact mass — each actual ship definition

Add an exported finite `ImpactMass` field under **Impact Stagger / Mass Resistance** in `EnemyDefinition`. **0 = disabled/immune/unconfigured** for backwards-compatible definitions; positive values declare physical resistance. Author pilot data on only a few actual lightweight ship `.tres` definitions (for example Standard Drone/Swift Interceptor) and optionally one heavy ship for contrast. **Do not bulk-edit every enemy definition** in this first pass.

Suggested demonstration values (purely relative game weights, not realistic kilograms):

| Ship profile | Suggested impact mass | Blaster mass 2 result |
| --- | ---: | --- |
| Tiny / light drone | 1.0 | Noticeable but restrained jolt |
| Tougher small ship | 1.8 | Milder jolt |
| Medium / armoured ship | 4.0 | Usually no stagger |
| Capital / superheavy | 12.0+ | No stagger |
| Unconfigured / explicitly immune | 0 | Never stagger |

Do not make enemy `ImpactMass` a proxy for class: a future special small but dense ship could legitimately be heavier than an ordinary large ship.

### Shared effect strength and presentation

Centralize finite, Inspector-tunable numeric settings in a small `FEEDBACK/ImpactStagger/` profile/resource, **not one hardcoded amount per weapon or ship**. Illustrative defaults:

| Setting | Proposed starting value |
| --- | --- |
| Mass eligibility | `projectileMass / targetMass > 0.75` |
| Full-strength ratio | `>= 2.0` |
| Visual maximum push | **4–6 world pixels** on a normal-size small ship |
| Visual return time | **0.18–0.25 seconds** |
| Transient forward-speed multiplier | **0.75–1.0** (at strongest, at most 25% slower) |
| Slow duration | **0.08–0.16 seconds** |
| Per-enemy retrigger limit | approximately **0.15–0.25 seconds** |
| Visual concurrency budget | bounded, e.g. **64 simultaneous visible reactions** |

Compute a finite bounded strength from the relative mass, then attenuate it by actual distance from the Blaster's burst center. For example:

```text
relative = projectileImpactMass / enemyImpactMass
massStrength = clamp((relative - minRatio) / (fullRatio - minRatio), 0, 1)
radialStrength = clamp(1 - distanceFromBlastCenter / blastRadius, 0, 1)
strength = massStrength * radialStrength
visualOffsetPixels = maxVisualPush * strength
movementMultiplier = 1 - maxSlowFraction * strength
```

Handle zero blast radius and direct-center impacts explicitly; use a well-defined local/full impulse for a direct hit, and no negative/NaN values. The exact ratio curve is a **game-feel proposal** and should be tuned after owner playtesting. A faint hit near the outer radius should not impart a stronger shove than a direct impact.

## 4. Directional impact and separation of responsibilities

**Source-of-truth direction for Blaster splash:**

1. Snapshot the projectile's **last valid world-space travel direction** at the instant it detonates (away from the shooter toward its impact).
2. For each eligible enemy in the existing splash-recipient pass, calculate **enemy world center − blast center**. If nonzero, normalize it: the enemy's artwork recoils **radially away from the explosion**.
3. If an enemy sits exactly at the center (zero direction), fall back to the actual projectile travel direction; if also invalid, skip directional presentation gracefully.
4. Convert that **world-space impulse direction into enemy-hull local coordinates** before offsetting the visual, because `Enemy` rotates toward its route. This must feel like the shot hits from the side, rear or front as appropriate.

**Gameplay:** Keep `Enemy.GlobalPosition`, `EnemyMovement`'s cached route, `RemainingJourneyDistance`, spatial registry, AoE contacts, collision clearance, checkpoints and arrival checks authoritative and unchanged. Apply the limited, positive transient speed multiplier in the existing `EffectiveMoveSpeed` composition *after* Slow/Freeze so a frozen enemy remains frozen; independent of the Slow status ID/colour/immunity and without generating extra XP, damage, Credits or status notifications. Stagger should never turn a normal enemy backwards.

**Cosmetic:** Use a tiny local hull/recoil offset and optional subtle rocking tilt that restores its original authored pose. Do not touch root/global transform, turret/weapon firing origins, damage-art texture, shield bar, status tint, stealth reveal, targeting, collision or thruster gameplay. Existing `GenericEnemy.tscn` has **separate** hull Sprite/Thruster/DefenseBar siblings; prefer a small reusable presentation adapter that caches the baseline transform and only adjusts the appropriate visible hull components while active. Avoid a scene-wide hierarchy rewrite that breaks `GetNode("Sprite")` references; if mounted guns cannot be moved in unison, keep STG0 visual opt-in to the lightweight unarmed ships.

On re-hit, do **not** create stacking slow effects/tweens. Ignore weaker hits within the cooldown; stronger accepted hits may refresh once up to the same finite max duration/offset. Dead, arriving, unloading or re-initializing ships must restore/remove transient local offsets. Check shield-only **actual** damage as valid impact if the ship survives; empty/zero-damage hits, miss/expiry, dead or registry-excluded recipients must not stagger.

## 5. Implementation boundaries and perf policy

- **STG0 only:** Blaster's launched shell opt-in, lightweight enemy mass profiles, one shared impact resolver, bounded cosmetic hull shove, short forward-speed dip and the `ImpactStaggerEnabled` master toggle. Rocket Tower stays unchanged.
- **Don't** put persistent momentum into pathfinding; no pushed enemy roots, new A* queries, collision rechecks, no knockback into mountains/lava/towers, no change to real movement clearance. Do not conflate existing pathfinding *staggered route audits* with combat *impact stagger*.
- **No new area queries.** `AreaDamage.ApplyInstant` already owns the one recipient pass. Pass optional impact context down that same delivery path. Keep direct hits and impact response separate from `DamageResolver` and from the decorative `ShockwaveEffect`; toggling off visuals cannot remove damage.
- **Only active victims need updates.** Reuse the existing active enemy movement tick or a bounded shared transient scheduler for tiny expire/offset recovery state; no new permanent `_Process`, enemy-by-enemy idle scanning, per-hit Tween/Timer/Node allocation, per-enemy SVG duplication or every-frame config loading. Reuse one shared settings resource. Apply an optional **visible** cosmetic budget and skip just the animation when full, preserving deterministic gameplay eligibility.
- **Heavy waves:** 300–500+ enemies may overlap within an area. No unlimited queue of deferred stagger animations; prefer per-enemy cooldown, capped active visual count and coalesced re-hits. Do not run heavy work for ineligible mass=0 shots.
- **Saving/restore:** Existing checkpoints are only made between waves. Transient sub-second stagger/pose is **not persisted**; on mission unload, reuse or checkpoint restore enemies begin without stagger. No changes to saved enemy positions, profile XP, official wave ownership, enemy class/rank or research.
- **Default feedback master:** `ImpactStaggerEnabled` is the user-requested single enable/disable for **both movement dip and visual animation**. When false, the regular hit, damage, AoE and XP still happen. Runtime live toggle behaviour can be scoped separately; switching it *before* deployment must work. Do not gate the main explosion/shockwave visual.

## 6. Proposed implementation steps (Codex)

1. **STG0-A — Data/seams:** introduce opt-in weapon impact mass (zero by default, nonzero only for Blaster), per-definition enemy mass, lightweight shared settings with safe validation, and the cached `DefaultFeedbackConfig` switch. Ensure Rocket and every other attack still have no stagger. Make the shared splash hit transport optionally carry an immutable mass + direction at launch/detonation without changing damage amounts.
2. **STG0-B — Recipient response:** use the single existing `AreaDamage` recipient loop to determine actual valid surviving hits, relative mass, radius attenuation and outward world direction. Apply one bounded transient speed scalar through the existing `Enemy.EffectiveMoveSpeed`/movement owner; never alter real world X/Y. Integrate cleanly with existing Slow/Freeze and thruster presentation.
3. **STG0-C — Cosmetic & tuning:** apply event-triggered child hull visual shove in world-consistent direction, return the local artwork to its baseline, cap visible concurrency, clear on death/restore and expose only legitimate tune points. Default screenshot/playtest should show small ships briefly flinch under Blaster shells without drifting off the physical route. Update the owning feature docs and short `NOTES/WORK.md` entry when coding is actually delivered.

The three steps may be committed as one focused pass if uncomplicated; don't implement later weapon families under this task.

## 7. Acceptance checklist (owner-led, lightweight developer validation)

- A Blaster shell hits a standard small drone from above/side/rear: its hull nudges **away from the true hit center** and returns within about one quarter-second; its actual route stays intact.
- The actual speed briefly dips then returns, without replaying route construction or adding a Slow-status entry. Combining with Cryo/Freeze behaves sensibly; Freeze remains zero speed.
- A heavy ship and an authored zero-mass ship do not noticeably react to the same shell. A near-edge recipient reacts less than a near-center recipient.
- A Rocket or Laser shot causes **no** stagger by default, even though Rocket uses the same `SplashProjectile` type. An expired/missed Blaster shell does not stagger anything.
- `DefaultFeedbackConfig.tres` → `ImpactStaggerEnabled = false` prevents **both** the slow and hull animation while preserving ordinary blast damage, shield absorption, XP, rewards and visible explosion effect.
- Multiple simultaneous Blaster hits do not permanently reduce speed, stack unbounded effects or leave sprites displaced. Killing a staggered enemy, pausing/unloading, and returning to the campaign reset correctly.
- Source-level checks cover strength clamps, radial/fallback direction, mass zero, recipient filtering and no-impact default. Run only a short focused sanity/build if practical per `CodePreferences.md`; visual/gameplay and large-wave FPS acceptance belongs to the owner.

**Ready-to-paste Codex handoff:**  
“Implement **STG0 ONLY — Mass-Based Enemy Impact Stagger** from `FEEDBACK/ImpactStagger/IMPLEMENTATION_PLAN.md` in `ben0kay/godot-tower-defense` on `dev`. Read `CodePreferences.md`, `project.md` and relevant feature docs first. Give the Blaster Cannon the only nonzero projectile impact mass; assign opt-in mass to lightweight enemy definitions. Use the existing splash recipient pass, a tiny temporary path-speed reduction, and a purely cosmetic directional hull recoil. Keep enemy root/path/collision unchanged, Rocket impact mass zero, and support `ImpactStaggerEnabled` in `DefaultFeedbackConfig.tres` to disable the whole mechanic. Avoid per-ship idle processing and stacking, preserve save/damage/XP, do lightweight checks, update notes, commit and push `dev`.”
