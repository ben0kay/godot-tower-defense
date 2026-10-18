# Cryo Tower — Codex Implementation Pass (CT1–CT3)

**Status: CT1–CT3 implemented. See README.md and TESTS/Towers/CryoTower.md for actual verification and remaining manual checks.**
**Target:** Godot 4.7 C# tower-defense project. This file is an implementation brief, not a claim that the code exists.
**Scope:** Implement the **Cryo Tower** plus an extensible, performance-conscious **enemy status-effect foundation**. **Do not implement the Stasis Tower yet.** Design the foundation so Stasis and many other effects can be added without another enemy-movement rewrite.

## Before coding

1. Read `README.md`, `NOTES/CodePreferences.md`, `TOWERS/README.md`, `TOWERS/Offensive/README.md` and the relevant shared tower/enemy/combat READMEs. Inspect the **current source**, not just documentation; some top-level READMEs lag recent implementation.
2. Inspect `ENEMIES/Shared/Enemy.cs`, `EnemyDefinition.cs`, `ENEMIES/Movement/EnemyMovement.cs`, `TOWERS/Shared/Targeting/TowerTargeting.cs`, `TOWERS/Shared/Projectiles/BasicProjectile.cs`, `TOWERS/Shared/Weapons/MultiBarrel/ProjectileTurretBase.cs` and `ProjectileTurretConfig.cs`, plus the shared stat pipeline, rank/upgrade contracts, ammo logistics, tower construction/operational state and catalog registration.
3. **Architectural design checkpoint:** Answer the questions under **"Codex: choose the future-proof status architecture"**, record your chosen approach/trade-offs in this file or a linked status-system README, and then implement it. You are empowered to choose a better internal design than the sketches below, provided it satisfies the acceptance criteria and remains easy to extend.
4. Other work may be landing on `main` (especially build HUD changes). **Do not edit UI layout/CTRL tray code, campaign menus, navigation algorithms, or unrelated towers.** Re-read upstream before committing; preserve concurrent work.

## Historical GameMaker reference (behavior to preserve)

Source: the user's older `SPACE-SHOOTER-TD-IDLE-CLICKER-HYBRID` repository, especially `scripts/BUILDING_DATA/BUILDING_DATA.gml`, `scripts/td_tower_bullet_collisions/td_tower_bullet_collisions.gml`, `scripts/td_tower_fire_processes/td_tower_fire_processes.gml` and `objects/o_enemy_par/Alarm_1.gml`.

- **Cryo:** Energy-using projectile; one hit applies **33% permanent movement slowdown**. Normal movement speed × 0.67. Repeated Cryo hits do **not** stack more slow. The old tower avoided already-slowed targets and did not slow bosses.
- **Stasis (future only):** Temporarily freezes movement at 0, then restores the already-slowed speed when its duration ends. Old level-one freeze lasted **180 frames at 60 FPS = 3 seconds**; higher tiers eventually reached 4 and 5 seconds. Also avoided already-frozen targets and exempted bosses.
- Old Cryo's collision handler called damage and then inherited a parent handler that also applied damage. **Do not recreate possible double-damage behavior.**
- Old GameMaker frame-based projectile speeds are **not directly comparable** to Godot pixel/second speeds. Use reasonable Godot speeds and measure playfeel.
- The user wants **Cryo and Stasis to use the existing Energy damage type**; Slow and Freeze are **status effects**, *not* additional damage types.

## Required Cryo gameplay

1. Player can build `Cryo Tower` from the existing catalog-driven **Offensive** build category, place it normally, select it, see stats and purchase upgrades.
2. A single turret barrel rotates/tracks and fires a visible guided **Energy** projectile. Reuse shared targeting/aiming/ammunition/stat machinery; avoid copying `BattleTower.cs` wholesale or creating a second projectile framework.
3. Successful projectile contact applies **Energy damage exactly once** through `COMBAT/Damage`, then applies a **permanent 33% slow** if the target survives and is eligible. Shields absorb damage per the existing global Energy coefficients, but do **not** block the slow by default. Document this explicitly.
4. Slow is **per enemy instance**, independent of `EnemyDefinition.MoveSpeed` and its saved resource. Existing slow stays active if the source tower is sold/destroyed/unloaded. A second equal/weaker slow must not compound; if later stronger slows are supported, the **strongest slow wins** (e.g. 20% then 33% => 33%, not 46.4%). No permanent-effect timer is needed.
5. Prefer **in-range targets without Cryo**. Follow existing `First` targeting preference among eligible enemies, then wait without firing if no eligible target exists. User clarification: never target already-slowed enemies, including weaker slows; no damage-only fallback. Ensure targeting remains bounded/cached like the present staggered/spatial-index system; no full-enemy scan each frame or new scene-tree searches. Do not alter other towers' default targeting semantics.
6. Build immunity as **data** on enemy definitions (e.g. flags/tags/policy) rather than `EnemyId` comparisons or `IsBoss` hardcoding. Initial nonimmune types can be slowed; there is presently no generic boss-identity property, so avoid inventing boss behavior. Provide a test fixture/type configured immune to Slow.
7. Statuses cannot create extra credits or XP on application alone. Regular actual Energy damage and kills keep their normal `DamageSource` rank attribution. Cryo shots consume ammunition via the existing system **once per launched projectile**.
8. Clearly show affected enemies through inexpensive visual feedback (e.g. subtle cyan tint/indicator), respecting existing sprites/DefenseBar. Do not create ongoing particle emitters or per-effect Godot nodes for every afflicted enemy. Visuals must not drive simulation.

### Provisional Cryo level-one balance and upgrades

These are **starting playtest values**, not sacred constants; preserve one complete absolute row per purchased level and keep small incremental upgrades. If changing the table, explain why.

| Level | Cost (build / upgrade) | Base Energy damage / hit | Shots / sec | Range | Permanent slow |
|---|---:|---:|---:|---:|---:|
| 1 | 250 CR | 10 | 0.50 | 208 px | 33% |
| 2 | 175 CR | 11 | 0.52 | 216 px | 33% |
| 3 | 250 CR | 12 | 0.54 | 224 px | 33% |

- Suggested projectile speed: **650 px/s** at all levels (Godot units; tweak after gameplay testing).
- Suggested local ammunition reserve: **600**, consumption **1 per projectile**. Use existing shared physical ammo logistics; `DamageType.Energy` does **not** imply a new energy-ammo resource. Do **not** add a second energy/power economy here.
- Earned rank totals: Standard **0**, Veteran **1,000**, Enhanced **3,000**, Superior **7,000**, Legendary **15,000** total XP, matching other current offensive towers; keep rank bonuses in separate `CryoTowerRanks.cs`.
- Reuse the cached eight-stage stat/paid-level pipeline. If slowdown strength is upgradable in future, ensure its authoritative authored level value, effective value, and upgrade preview cannot diverge. Do not make silent bypasses around the stat pipeline.
- Do not add freezing, damage-over-time, project-wide damage-type changes, tower HP, or new economy resources in this pass.

## Codex: choose the future-proof status architecture

**This is an explicit design request, not just a prescribed class name.** Expect **many effects** in future: permanent Slow, timed Freeze/Stasis, Burn/Corrosion/DoT, stuns, debuffs, vulnerabilities, buffs, immunities, perhaps effects from enemies/terrain/support towers. The user wants these to be easy to add without a giant `Enemy.cs`, an ever-growing hardcoded switch, or expensive Godot nodes.

**Before implementing, please evaluate and answer:**

1. Would you choose (a) dedicated fields like `SlowMultiplier` / `FreezeSeconds`, (b) a compact collection of active typed status instances with a small status registry/handlers, or (c) another approach? **What is best when we eventually have dozens of effect definitions, yet most enemies have zero to two at a time?**
2. Should identity use an enum, ID/string/hash, bit flags, resources, or a combination? Which layer owns **stacking policies, strength, durations, refresh, immunity, and effect-specific behavior**? How do we extend the system without modifying a giant conditional every time?
3. What is the smallest **allocation-conscious**, maintainable implementation that supports **500–1,000+ simultaneous enemies**? Prefer no extra nodes or timers per effect, no per-frame allocations/LINQ, no global scans, no unnecessary per-enemy dictionary for every unmodified enemy. Lazy/compact active storage is welcome if justified; **do not preemptively build a huge plugin/factory framework**.
4. How will a projectile carry an **immutable, launch-time effect payload** separately from its `DamageInfo` so status values, damage values, and source attribution are correct even if the firing tower upgrades/disappears before impact? Can `BasicProjectile` support optional typed hit effects without breaking existing Kinetic turrets? Is a specialized projectile or a small reusable impact hook cleaner?
5. What happens when **Slow + Stasis** coexist? Define deterministic ordering: effective speed 0 while frozen, afterward the strongest permanent slow remains. Freeze duration expires while immobilized, does not require a fresh A* request, and never overwrites an earlier slow. Define stronger/weaker reapplication and refresh semantics for future use.
6. How do future **DoT** effects get tick scheduling, attacker/XP attribution, and cleanup without requiring every enemy to check dozens of inactive statuses each physics frame? Don't implement DoT yet, but ensure the chosen contract won't obstruct it.
7. How do definitions declare immunity (including later bosses)? Should effect applicability respect shields, target types, or tags? Keep these distinctions explicit and testable instead of embedding arbitrary tower-specific rules.

**Default direction (you may improve it):**
- `COMBAT/StatusEffects/` for small shared **status identity + immutable effect payload / stacking policy contracts**, separate from `DamageType`.
- `ENEMIES/StatusEffects/` for a **lightweight per-enemy runtime status holder** and optional data-driven immunity flags/config. It should offer queries such as `HasStatus(...)` and `GetEffectiveMoveSpeed(baseSpeed)`. If allocation is required, allocate only for affected enemies where practical.
- `ENEMIES/Movement/EnemyMovement.cs` reads the **effective** movement speed; do **not** mutate `EnemyDefinition.MoveSpeed`, waypoint caches, A* weights, or checkpoint requirements.
- A **pluggable or data-driven resolution policy** for future effects rather than per-type runtime Godot components. Not every effect must be generic: a small number of explicitly implemented effect families is fine. Explain the extension procedure for adding a new status type.
- If time-varying effects need ticking, integrate with an **existing bounded per-enemy/world update path**; avoid one independent SceneTreeTimer/Godot Timer per effect. Permanent slow should have almost zero continuing cost.

**Deliverable:** In a new `ENEMIES/StatusEffects/README.md` (or similarly clear design note), record the selected architecture, the rejected alternatives, memory/update complexity, status stacking/immunity rules, and **a worked example of adding Stasis later** without implementing its tower today.

## Implementation passes

### CT1 — Shared status-effect foundation

- Implement Codex's selected status architecture (only the concrete Slow behavior required now).
- Add a safe enemy API to apply/query effects and resolve effective movement speed.
- Make enemy slowdown/immune state per instance; status clearing happens on death, arrival and unload.
- Integrate effective speed into `EnemyMovement` without breaking corridor-aware routes, checkpoint chains, collision clearance, swept terrain/Lava timing or registry spatial history.
- Preserve original normal-speed behavior for enemies with no effects.
- Define the **future Stasis/Freeze interaction contract** in docs/tests. Implement a generic timed-effect facility now only if it is demonstrably small and useful; **do not build Stasis Tower or its artwork**.

### CT2 — Cryo weapon and catalog integration

- Add `TOWERS/Offensive/CryoTower/` with `CryoTower.cs`, `CryoTowerConfig.cs`, `CryoTowerLevels.cs`, `CryoTowerRanks.cs`, `CryoTower.tscn`, `CryoTowerDefinition.tres`, `DefaultCryoTowerConfig.tres`, `Artwork/cryo_tower.svg` and `README.md`.
- Give Cryo a recognizable top-down vector sci-fi ice/cyan SVG, one visible muzzle and a low-overhead slowed-enemy indicator. Exact node structure can reuse the current shared projectile turret contract.
- Prefer a **shared type-capable projectile/config** or thin appropriate extension over duplicating the `ProjectileTurretBase` firing/aiming logic. Existing Battle, Sniper, Machine Gun, Laser, Blaster and Shockwave must remain behaviorally unchanged.
- Register Cryo `TowerId = "cryo"` and category `Offensive` in `TOWERS/DefaultTowerCatalog.tres`. The dynamic CTRL build HUD will pick it up; **do not edit HUD scene/scripts**.
- Respect the tower's operational/construction state, ammo, selection/inspector, upgrades, level stats, rank XP and existing targeting/aiming conventions.

### CT3 — Automated tests, documentation and verification

Add focused regression coverage and link it into the existing relevant headless test suite; avoid changing unrelated fixtures except necessary expectations.

**Required tests:**
- Baseline `Enemy001` (100 px/s) becomes **67 px/s** when slowed; remains at 67 on repeat hit; stronger/weaker slow interaction is deterministic.
- Same authored `EnemyDefinition` used for multiple enemy instances: only the struck one slows; original `MoveSpeed` remains 100.
- Energy projectile deals normal shield/hull damage **once**, awards normal XP/kill rewards once; slow applies after a surviving shielded impact and no slow is added after a killing hit.
- Slow immunity is configurable in a test definition; damage still works when slow application is rejected.
- Cryo preferentially targets unslowed enemies, does not waste the first opportunity on an already-slowed enemy when an eligible target is in range; fallback policy is tested.
- A slowed enemy continues **its existing** route/checkpoint progress; slowdown and eventual timed-freeze model cannot trigger unnecessary A* recalculation or corrupt Lava traversal timing.
- Full level 1–3 stats, affordable upgrades, independent rank progression (1k/3k/7k/15k), catalog registration, UI build choice and private config copies.
- Enemies without statuses preserve the prior movement and CPU behavior; 500+ enemy smoke/performance check, no extra timer nodes/per-tick allocations on the common path.
- Effects/source remain valid at projectile impact if the firing tower is upgraded or removed; no gameplay crashes or double effects.

**Verify:** compile the Godot .NET project, run the project's headless regression scenes if environment allows, and manually inspect a Cryo hit in play. If tools are unavailable, say so clearly rather than claiming a runtime pass. Update corresponding READMEs/tests with what's actually implemented.

## Acceptance criteria

- Cryo can be selected and placed through the existing offensive build catalog, shoots Energy projectiles, does exactly one damage resolution and applies **one permanent nonstacking 33% slow** to eligible living enemies.
- Default enemy speed/path behavior is unchanged without status; repeated hits do not cause additional speed loss or recompute paths.
- Status infrastructure has a documented **credible extension path to Stasis and dozens of other effects**, without a god-class or unbounded per-frame overhead.
- The status API and immunity data belong to enemy/combat shared systems, **not** in `CryoTower.cs` or a mutable `EnemyDefinition`, and are free of tower-specific enemy type-name exceptions.
- Purchased levels, rank experience, power/construction behavior, existing ammo logistics, damage attribution and default build HUD continue to work.
- The Stasis Tower remains **unimplemented** (design notes only).

## Implemented design checkpoint

Chosen: lazy compact typed status storage with a cached movement multiplier; immutable launch payload separate from DamageInfo; enum identity and Inspector immunity array. Permanent Slow has no ticking/scheduler. Strongest Slow wins; shields do not block it. Cryo reuses ProjectileTurretBase and the existing spatial visitor, without altering other weapons. Detailed alternatives, complexity, Freeze coexistence and future DoT attribution/scheduling contracts: `ENEMIES/StatusEffects/README.md`. No timed effects or Stasis behavior is claimed implemented.

## Codex completion report

When done, please provide:
1. **Architecture choice:** what you chose for the future effects system, alternatives considered and why, including the estimated runtime cost for hundreds of enemies.
2. **Files changed** and a short overview of how to add Stasis or another new effect later.
3. **Test results:** which commands/scenes ran, pass/fail evidence and any unverified items.
4. **Known limitations / deferred work** (e.g. DoT scheduling, save serialization, shader polish, bosses).


## Follow-up ST1 — 2026-10-18

The earlier instruction to defer Stasis applied to CT1–CT3. User-authorized ST1 now implements it separately; Cryo retains permanent 33% Slow, Energy damage and unslowed/nonimmune-only targeting. Freeze coexists with Slow, temporarily gates movement, and expiry restores Slow. See ../StasisTower/README.md and ../../../ENEMIES/StatusEffects/README.md. Original pass instructions are preserved above.
