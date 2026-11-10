# STG0 — Class-Based Directional Enemy Impact Stagger

**Repository:** `ben0kay/godot-tower-defense` · **Branch:** `dev`  
**Updated:** 2026-11-10  
**Status:** **Implementation plan ONLY — no stagger effects or gameplay code implemented.**  
**Idea reference:** `NOTES/Ideas.md` → M17  
**Supersedes:** the prior STG0 mass-ratio/per-enemy-impact-mass proposal in this same file.

## Goal

Heavy-feeling weapon impacts should briefly jolt **eligible lighter enemies** while they continue **toward their Citadel**, never leaving their actual path. A qualifying splash hit causes:

1. A very short **forward movement slowdown** (small genuine gameplay effect), then automatic recovery.
2. A small **visual-only hull shove** away from the blast centre and smooth return to its normal local pose.

**No physical sideways knockback, root displacement, collision change, path recalculation, sustained stun or new combat status category.** The difference between gameplay and visuals is essential: enemy root and path stay authoritative; only forward speed dips. Ships keep moving to their assigned Citadel.

### Owner-selected initial weapon/class rules (exact scope)

The live class enum is `ENEMIES/Shared/EnemyClass.cs`, not inferred from sprite scale:

| Enemy class | Blaster Cannon | Rocket Tower | Other towers |
| --- | --- | --- | --- |
| **Tiny** | No — not specified yet | No — not specified yet | No |
| **Light** | **Yes (strong)** | **Yes (mild)** | No |
| **Standard** | **Yes (mild)** | No | No |
| **Heavy** | No | No | No |
| **Superheavy** | No | No | No |
| **Capital** | No | No | No |
| **Titan** | No | No | No |
| **Colossal** | No | No | No |

**Tiny note:** `Tiny` is an existing real class but the owner explicitly specified **Light and Standard** for Blaster and **Light** for Rocket. Do **not** opt Tiny in automatically: it needs a later owner decision. The last two classes have non-numeric display order in their enum: do not rely on `(int)EnemyClass` comparisons or assume sequential ordinals.

All other towers, enemy weapons, drones, beams, projectile types and damage effects remain **not stagger-capable by default**. A tower dealing explosive damage does not automatically qualify; this is a specific **weapon-owned opt-in**.

## 1. Simpler authoring model: class resistance and weapon impact power

**Remove the old proposal for a mass field on each `EnemyDefinition.tres`.** No per-enemy masses, mass ratios, velocity/momentum physics, dynamic Rigidbody interaction, per-ship authored stagger values, or independent movement/path systems.

Instead define a tiny central **class resistance table** with explicit enum entries plus a nonnegative **weapon impact power**.

Proposed initial numeric tiers:

| Weapon | `StaggerImpactPower` (proposed) |
| --- | ---: |
| **Blaster Cannon** | **2** |
| **Rocket Tower** | **1** |
| Every other weapon | **0 (disabled)** |

| EnemyClass | `StaggerResistance` / class eligibility |
| --- | --- |
| Light | 1 |
| Standard | 2 |
| Tiny, Heavy, Superheavy, Capital, Titan, Colossal | **Immune / not eligible in STG0** |

Acceptance: valid target class, positive impact power, class resistance defined, and `power >= resistance`. **Use an explicit `switch` over EnemyClass** (or equivalent validated class-policy resource), not integer-enum ordering; `Tiny=0`, `Titan=6`, `Colossal=7` are already serialized. Missing/new future classes must default to immune.

A light ship is visibly affected more by Blaster (2 vs 1) than by Rocket (1 vs 1); Standard only reacts to Blaster. For optional intensity, use a bounded fixed step per tier, without division or simulated physics:

```text
if (!enabled || impactPower == 0 || classResistance == Immune || impactPower < classResistance)
    return;                // zero work beyond inexpensive guards for ineligible hits

tier = impactPower - classResistance + 1   // eligible: 1 (mild) or 2 (strong)
strength = clamp(tier * 0.5, 0, 1)       // 0.5 mild, 1 strong
strength *= blastProximity              // [0..1], using existing AoE recipient distance
```

This is an explicit game-balance mechanic, **not physical mass**. If later towers need different curves, extend the shared effect policy deliberately rather than implementing separate status engines. A very modest static method or tiny configuration Resource is preferred.

### Important config ownership and inheritance

- Put `StaggerImpactPower` on a suitable weapon-owned **Inspector-editable** resource/config, default `0` globally.
- **Blaster Cannon** is `2` explicitly; **Rocket Tower** is `1` explicitly. Rocket's `RocketTowerConfig` **inherits** from `BlasterCannonConfig` today: explicitly set the correct value on **both separate weapon default resources** (or a typed per-constructor override with clear ownership), and verify the copied runtime config preserves the authored value. A single inherited shared default `2` would incorrectly make Rocket stagger Standard.
- Add **no new `EnemyDefinition` mass or resistance field**. Read the existing `Enemy.Definition.Class` and map it to a resistance via one shared resolver. Per-class intensity is a central config/policy, not a per-enemy override.
- Capture the impact power **once at projectile launch** as part of its immutable snapshot, not by asking the tower's config for every splash recipient. Runtime upgrades must not change shots already in flight.

## 2. Existing implementation seams (read before coding)

| Source | Required reuse / constraint |
| --- | --- |
| `ENEMIES/Shared/EnemyClass.cs` | Canonical Class values. Do not change, reorder or renumber the enum. |
| `ENEMIES/Shared/EnemyDefinition.cs` | Already declares `EnemyClass Class`. No new mass fields needed. |
| `TOWERS/Offensive/BlasterCannon/BlasterCannon.cs` | Launches `SplashProjectile`; authored stagger power 2. |
| `TOWERS/Offensive/RocketTower/RocketTower.cs` | Also launches `SplashProjectile`; authored stagger power 1. |
| `TOWERS/Offensive/RocketTower/RocketTowerConfig.cs` | Inherits `BlasterCannonConfig`; avoid accidental copy/inheritance of power 2. |
| `TOWERS/Shared/Projectiles/SplashProjectile.cs` | Has actual travelled direction and detonation position; on successful detonation, already calls instant area damage. Preserve weapon cadence, shot handling, TTL and hit-once rules. |
| `COMBAT/AreaDamage/AreaDamage.cs` | Owns a **single spatial query and snapshotted recipient pass**; transport an optional impact context through the same pass. Do not re-query for stagger. |
| `COMBAT/Damage/DamageInfo.cs` | Shared immutable damage payload; avoid putting general physics or class policy inside the damage resolver. |
| `ENEMIES/Shared/Enemy.cs` + `ENEMIES/Movement/EnemyMovement.cs` | Compute `EffectiveMoveSpeed` once from authored speed and existing Slow/Freeze multiplier; compose a finite transient stagger multiplier into that value. Do not move the root sideways or rebuild routes. |
| `ENEMIES/Shared/GenericEnemy.tscn` | Hull Sprite, Thruster, Movement and DefenseBar are separate children. Visual reaction must not break mount points, thruster attachment, shield aura, DefenseBar or existing `GetNode("Sprite")` lookups. |
| `FEEDBACK/FeedbackMasterConfig.cs` + `FEEDBACK/DefaultFeedbackConfig.tres` | Add `ImpactStaggerEnabled` default **true**. Toggling off disables **both** speed dip and visual shove while preserving damage and all other feedback. |

## 3. Directional reaction, timing and defaults

Direction should be visually intuitive at a glance:

1. For each genuinely affected target, compute a direction **radially away from the explosion centre** using `enemy world centre - blast world centre`.
2. If target position is exactly at the blast centre, use the projectile's last valid **world travel direction** captured before detonation.
3. Convert the world impulse to the target's **local visual space** to account for the ship's current rotation, then move the appropriate artwork only.
4. Return the visual offset smoothly and exactly to its authored baseline.

**Illustrative starter tuning**, all clamped and shared (not per enemy) in a small Inspector configuration:

| Parameter | Suggested value |
| --- | --- |
| Mild strength | 0.5 |
| Strong strength | 1.0 |
| Maximum visual shove (strong, centre hit) | 5 px |
| Visual return time | 0.2 seconds |
| Maximum forward-speed dip (strong, centre hit) | 20% |
| Maximum speed dip duration | 0.12 seconds |
| Per-enemy re-hit cooldown | 0.18 seconds |
| Concurrent **visible** recovery animation cap | 64 |
| Blast edge scaling | Use existing distance/radius; weaker at edge |

The slowdown is a *single positive multiplier* composed **after** existing Slow/Freeze; frozen ships remain frozen. No slow stacking, no permanent zero speed, no reverse travel and no status-colour takeover. A simultaneous stronger impact may refresh a reaction within a fixed cap, while weaker hits within cooldown are ignored.

**Damage eligibility:** Check actual valid live/registered hit recipients and real applied damage/shield absorption. An absorbed-by-shield hit can jolt the ship; a zero-damage hit cannot. If damage kills/removes a target, skip its later stagger animation. Do not grant extra XP, Credits or wave changes.

**Visual compatibility:** Keep the enemy root, collision and all route/terrain/registry coordinates unchanged. Small ships with only a hull sprite are the simplest first pilot. Preserve gun muzzle positions/rotating mounts, active thruster flames, glow, shield centre, damage textures, stealth and defense bars; if there is no straightforward reusable moving visual-group hook for more complex ships, do **not** rearrange every enemy scene in STG0.

## 4. Configuration switch: complete disable

Add `ImpactStaggerEnabled` to **`FEEDBACK/FeedbackMasterConfig.cs`** (default `true`), reachable through **`FEEDBACK/DefaultFeedbackConfig.tres`**.

- `true`: only the exact initial weapon/class matrix can trigger the effect.
- `false`: **no brief movement dip and no artwork offset**. Ordinary shell hit, blast damage, defense layers, kill XP, wave progression and shockwave visuals must still work.
- This particular toggle controls a small gameplay effect as well as feedback, unlike typical cosmetic-only master flags. A value set before deployment must be honored; optional live mid-wave retuning is not part of STG0.

## 5. Performance and save requirements

- **No second spatial query**: existing `AreaDamage.ApplyInstant` is the single hit-recipient owner. The `0` impact-power case does nearly nothing and never schedules stagger.
- No physics impulses, Rigidbody2D nodes, extra colliders, per-enemy classes/fields, extra pathfinding, saved offsets, or event spam.
- No per-hit Tween/Timer/node allocations. Reuse existing enemy movement physics updates or one bounded active-only scheduler for a few floats and a cosmetic return; keep all idle enemies asleep. Only visible victims receive animated shove, and the shared cosmetic budget is finite.
- Avoid creating full-size visual nodes or per-ship material instances; cache the baseline local visual transforms and restore precisely after finishing, death, arrival, new spawn, pause/restore or unload.
- Use **one clipped finite reaction per enemy**. Coalesce or ignore overlapping Blaster/Rocket explosions. Off-screen ships may get the same brief gameplay speed dip but should skip cosmetic offset work.
- Check 300–500+ enemies during a congested splash wave for budget and correctness. Visual cap must never change gameplay eligibility. Keep movement, spatial indexing, fog and lighting performance the same when no stagger is active.
- Between-wave checkpoint: transient stagger not persisted or reconstructed; normal saved transforms/HP/wave results unaffected. Do not modify campaign profiles.

## 6. Suggested implementation sequence — STG0 only

**STG0-A — Opt-in data & guard:** add one zero-default `StaggerImpactPower` to the shared splash weapon policy, author Blaster=2 and Rocket=1 in their respective default resources, leave other weapon families at 0; central enum-class resistance table and the master toggle.

**STG0-B — Hit delivery & temporary gameplay dip:** snapshot power per launch, reuse `AreaDamage` recipient pass, compute allowed tier/proximity, apply very short temporary speed scalar using the enemy's existing movement path, keep full regular damage/XP semantics.

**STG0-C — Visual-only recoil:** world-to-local hull offset, return and cooldown, bounded visuals, safe cleanup on all exits; add small focused test/sanity expectations for Tiny/nonordinal classes, Blaster Standard, Rocket Light and Rocket Standard rejection, shield hits, config-disabled mode and multiple hits.

**Do not** implement masses, per-enemy physical data, support for Laser/other towers, directional enemy steering, shoving through walls, new status immunities, new XP mechanics or a multi-wave upgrade balance pass.

## 7. Owner acceptance checklist

- Blaster hits **Light** and **Standard**; Light takes the stronger reaction. Blaster does **not** stagger Heavy or above.
- Rocket hits **Light only** (milder); **Standard** does not react to Rocket.
- **Tiny** does not react in STG0 (explicitly not authorized yet); `Titan` and `Colossal` are not accidentally selected because of enum values.
- All remaining towers and projectile families produce **no stagger**.
- Each valid recipient keeps normal movement toward Citadel, briefly moves more slowly, shows a small outward visual jolt and recovers completely. Root position never jumps, route doesn't recalculate.
- An explosion at a distance yields a weaker reaction than a centre hit. Shield impacts may count when they absorb positive damage; harmless or missed rounds do not.
- `FEEDBACK/DefaultFeedbackConfig.tres` with `ImpactStaggerEnabled=false` disables **both** results while preserving damage, kill reward, sounds and shockwave presentation.
- Numerous overlapping shots neither stack slowdowns nor cause lasting offsets. Save/reload and no-stagger waves work as before.
- As usual per `CodePreferences.md`: focused source/diff inspection and at most one basic build if readily available; owner performs visual/gameplay/FPS acceptance.

### Codex-ready handoff

> Implement **STG0 only — Class-Based Directional Impact Stagger** from `FEEDBACK/ImpactStagger/IMPLEMENTATION_PLAN.md`, repo `ben0kay/godot-tower-defense`, branch `dev`. Read `CodePreferences.md`, `project.md` and the relevant combat/enemy/tower docs first. Replace the prior mass concept with **weapon impact power and shared EnemyClass resistance**: Blaster=2 (Light strong, Standard mild), Rocket=1 (Light mild), everything else zero; do not opt in Tiny. Use only existing splash recipients, never physics knockback or route rework. Add a short real speed dip plus a visual-only directional hull recoil and `ImpactStaggerEnabled` master switch to disable both. Keep performance bounded and save/damage behaviour intact. Do focused verification, update `NOTES/WORK.md`, commit/push to `dev`.
