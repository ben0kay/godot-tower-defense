# Projectile guidance, weapon spread and rank/research accuracy — Codex handoff

**Status: PLANNED — documentation only. No gameplay implementation in this pass.**
**Owner:** `TOWERS/Shared/Projectiles/`, cooperating with `TOWERS/Shared/Stats/` and existing offensive weapon launchers.

## Read first

Read the repository `README.md`, `NOTES/CodePreferences.md`, `TOWERS/README.md`, `TOWERS/Shared/Stats/README.md`, `TOWERS/Shared/Stats/Pipeline/README.md`, `TOWERS/Shared/Ranks/README.md`, `TOWERS/Shared/Projectiles/README.md`, relevant weapon READMEs, `SYSTEMS/Progression/Research/README.md`, and the actual current C# source **before editing**. READMEs sometimes retain historical descriptions. Follow the existing comments-above-methods with `// =========================================================`, small regions, strongly typed optional capability groups, clear folder ownership, vertically compact code, no unnecessary scene nodes, performance-minded loops and one GitHub commit for the completed job.

## Goal and decisions

1. **Straight/unguided is the default** for physical projectile delivery. A shot has a launch-time world-space direction (from its **actual muzzle**, not tower center); after launch it does not steer.
2. **Homing/guided remains implemented as an optional capability**, default OFF, for future permanent Research Center unlock(s). Do not build a research UI/tree or invent prices/unlocks as part of this pass. Research unlocking guidance can be a separate subsequent pass.
3. **Spread** is random angular deviation applied **once at launch** to that heading. A `SpreadDegrees = 0` shot is perfectly precise. Nonzero spread is authored **per purchased tower level** and modified separately by **earned rank** and eventually permanent research. It is NOT `AimToleranceDegrees` (permission to fire), `RotationSpeedDegrees` (tracking speed), recoil animation, or a dice-roll chance to hit.
4. **Accuracy is optional by weapon type**. Continuous Laser beam keeps its precise target lock and damage behavior unchanged; Shockwave/other radial delivery, passive structures and nonprojectile generators get no meaningless accuracy stat. Do not globally weaken tracking.
5. Preserve independently authored tower behaviors: e.g. Rocket Tower is **already non-homing** and uses destination/splash delivery, while Blaster is currently guided; Cryo/Stasis carry their status payloads and must still affect enemies on actual hits. Enemy Gunner Drone bullets are a separate enemy-owned straight projectile system; do not change it here.
6. **Rank and research can both improve accuracy**, without blending their ownership. Selected earned rank is a total bonus, not cumulative bonuses from earlier rank tiers. Effective spread is calculated through the existing eight-stage stat pipeline and snapshotted when a bullet launches.
7. A target leaving range, dying or moving after firing must not magically steer, teleport, delete a live unguided bullet or turn misses into hits. Preserve lifetime/scene-cleanup safeguards.
8. No gratuitous refactor of damage resolution, ammo/heat, XP/rewards, firing cadence, turret art/recoil, targeting priorities, towers' footprints, enemy movement or research menus.

## Verified existing code seams (as inspected 2026-10-20)

- `TOWERS/Shared/Projectiles/BasicProjectile.cs` is guided today: it recalculates direction toward its retained target and contacts that specific enemy. This must be changed for real unguided projectiles, **not** just a boolean that freezes rotation while continuing `MoveToward(_target.GlobalPosition)`.
- `TOWERS/Shared/Projectiles/SplashProjectile.cs` already has `trackTarget = true`; Blaster uses its default; Rocket calls `trackTarget: false`. **Important distinction:** current non-tracking Splash moves to a stored destination and detonates there. Decide deliberately how spread changes its initial trajectory and eventual detonation, without accidentally restoring tracking.
- `TOWERS/Offensive/BattleTower/BattleTower.cs` and `TOWERS/Shared/Weapons/MultiBarrel/ProjectileTurretBase.cs` initialize `BasicProjectile` at real muzzle markers via `WeaponVisuals.Present`; the latter serves Machine Gun / Sniper / Cryo / Stasis.
- `TOWERS/Offensive/BlasterCannon/BlasterCannon.cs` and `TOWERS/Offensive/RocketTower/RocketTower.cs` initialize SplashProjectile and launch at muzzle markers.
- `TOWERS/Shared/Stats/TowerStatBlock.cs`, `TowerFinalStats.cs`, `Pipeline/02_TowerUpgrades.cs`, `03_TowerRanks.cs`, `04_PermanentResearch.cs`, `07_SafetyClamps.cs`, `08_FinalStats.cs` are the existing stat path. Permanent research is **already partially active** (e.g. Battle Weapons I damage); do not overwrite it with a neutral implementation because historical README tables say future.
- `Offensive/<Tower>/*Levels.cs` authors absolute per-level values. `*Ranks.cs` supplies the five named rank definitions and explicit total bonuses. Research is captured for deployment and passed into placed towers, not read from the profile during every shot.
- `TOWERS/README.md` currently marks accuracy simulation unimplemented.

## Proposed data contract

Introduce an optional immutable **ProjectileHandlingStats** (or similar clearly named small typed record) in the existing level/final-stat snapshots:
- `SpreadDegrees` = a **half-angle** in degrees (±value about the actual muzzle launch heading), finite and >= 0.
- `GuidanceMode` = `Straight` or `Homing` (default `Straight`) **only if this belongs as a stat rather than a separate per-weapon policy**. Prefer a typed enum to scattered `bool` defaults if it improves clarity. Preserve a single effective guidance authority at launch.
- Only projectile-capable tower types should opt in. An absent handling group means **no spread** and no forced behavior on beams/area-wave systems.
- Do **not** add per-frame accuracy rolls or make an unrelated Inspector-export duplicate of the authoritative numeric level rows. C# level-1 convenience accessors may forward to the authoritative row, like existing settings.
- If guidance is stored as nonlevel weapon policy today but later research overrides it, document the resolution path clearly: authored/default mode → captured deployment research override → immutable launch snapshot. Do not accidentally use current profile state mid-mission.
- If a separate explicit `ProjectileGuidance` launch payload/record simplifies runtime, use it; avoid retaining live tower configs or mutable FinalStats on projectiles.

**Suggested initial spread half-angles (tentative playtest values, not locked balancing):**
| Tower | L1 | L2 | L3 | Notes |
| --- | ---: | ---: | ---: | --- |
| Machine Gun | 5° | 4.5° | 4° | Visible sustained cone from alternating barrel tips |
| Battle | 2.5° | 2° | 1.5° | Moderate precision |
| Sniper | 0.5° | 0.4° | 0.25° | Very accurate; not automatically perfect |
| Blaster Cannon | 1° | 0.75° | 0.5° | Splash near intended impact |
| Rocket | 1.5° | 1.25° | 1° | Already unguided |
| Cryo / Stasis | 1° | 0.75° | 0.5° | Only apply status when a real collision hits |
| Laser / Shockwave | N/A | N/A | N/A | Retain existing beam lock/radial mechanic |

Check actual authored level counts and names: **do not invent absent levels**. Apply sensible values only to existing rows. `0°` must always be valid for future perfectly accurate projectile weapons.

### Rank bonus convention

Add a clearly named **total accuracy/spread-reduction fraction** to each relevant `TowerRankDefinition` (or an optional rank handling group) without changing existing damage/range/HP bonuses. Proposed **total** spread reductions relative to the selected purchased level: Standard 0%, Veteran 5%, Enhanced 10%, Superior 15%, Legendary 20%. These are independently configurable in each tower's `*Ranks.cs`, not hardcoded by projectile runtime. Optional projectile precision can be different or zero for specific towers. Ensure existing positional constructors/callers remain compatible with optional trailing args or explicit named values.

Stage03 computes `spreadAfterRank = purchasedLevelSpread * (1 - currentRankTotalReduction)`; **not** repeated reductions from every lower rank. Rank promotions cause the existing one-time cached stats rebuild; do not calculate per bullet except to read the cached value.

### Research extension (separate future pass)

Reserve a typed captured deployment bonus such as `ProjectileSpreadReductionFraction` and `UnlockHomingProjectiles`, with explicit **scope/compatibility** (global applicable projectile towers or specific tower families). Keep the current Stage04 research bonuses and snapshot semantics. Proposed future Precision Targeting I/II/III totals: 20% / 36% / 52% reduction versus after-rank spread (corresponding to successive ×0.8, ×0.8, ×0.75); **do not implement these research purchases now**. When active: `effectiveSpread = baseSpread * (1 - selectedRankTotalReduction) * (1 - capturedResearchTotalReduction)`. Clamp each fraction 0..1 and resulting spread 0..180 (or a stricter documented practical bound). Homing research merely changes eligible launch guidance mode; spread should **not silently become meaningless** after homing unlock. Specify whether homing preserves launch spread only or applies a homing-turn-rate/lock-on limitation in the **future** research pass; do not invent that mechanic now.

## Collision and travel — critical design acceptance

**BasicProjectile cannot remain target-only on unguided shots.** It needs swept segment contact against eligible living enemies **along actual distance travelled**, with earliest-hit selection, deterministic ties, exactly one contact/damage/status resolution, and no tunnelling at high projectile speed or frame hitches. Reuse existing `EnemyRegistry` spatial/query APIs if they support segment candidates; otherwise add one bounded reusable shared helper. Do not scan every enemy for every bullet every physics tick. Do not create `Area2D` / `RigidBody2D` / new child node per shot. Preserve stealth policy semantics: a bullet physically intersecting an otherwise hidden enemy can deal damage even though targeting cannot lock onto it, per existing SE1 policy. Evaluate whether projectile/environment collision belongs in scope; current bullets have **no terrain collision**, so defer it unless an already-authoritative inexpensive collision helper exists.

**Unguided travel:** launch at real muzzle with a normalized finite direction; advance `position += direction * speed * delta` capped by remaining lifetime; advance and collide even after original target dies/arrives; finish on hit, expiry or world shutdown. Never change heading. Tracer orientation uses actual movement direction. If original enemy moved, another enemy crossing the shot path can be hit; damage/XP credit must still follow existing snapshotted `DamageInfo` / `DamageSource`. Guided mode retains homing behavior, but collision should still use the travelled segment and correct target/lifetime cleanup.

**SplashProjectile:** its damage is **AoE at detonation**, not a direct `BasicProjectile` hit. In unguided mode, capture the target's initial position/launch vector; spread rotates **travel direction once**, so its eventual impact center deviates correspondingly. Explicitly choose and document its detonation rule (e.g. travel fixed original target distance along deviated heading, then detonate); ensure no out-of-range infinite travel or hidden dynamic re-aim. Keep Rocket's existing original-target-point behavior when spread=0, preserve Blast radius/falloff/one-detonation protections, and retain Blaster's default appearance/impact presets. Guided splash, if retained as a capability, must keep its one-shot detonation and lost-target subscriptions.

**Accuracy sampling:** use one random sample per emitted projectile at launch, uniformly distributed over ±SpreadDegrees; no RNG in physics updates, no per-target random misses, and no visual-only deviation. For tests offer deterministic injected/seeded direction or pure angle helper rather than flaky probabilistic assertions. For multiple barrels, use each barrel's world transform and sample independently. Keep random stream choice isolated where practical to avoid reordering unrelated wave/spawn randomness.

## Proposed implementation passes — sequential, one final commit

**PA1 — Types + stats + authoring (no visible behavior flip yet):** define optional handling capability and `Straight/Homing` contract; integrate selected absolute level rows, Stage03 rank totals, Stage04 future-compatible seam without disturbing active research, Stage07 finite clamps, Stage08 cache; add tests for no handling, zero spread, level/rank/preview parity, boundaries, research snapshot compatibility. Do not add unrelated weapon stats.

**PA2 — Shared launch & travel:** implement actual unguided BasicProjectile, swept eligible-enemy contact and launch-time snapshot; keep existing guided mode explicitly selectable. Extend splash to straightforward nontracking trajectories with one-time spread and correct blast delivery. Preserve target loss, high speeds, status, shield/hull damage, XP, source lifetime and efficient cleanup. Establish explicit mode defaults `Straight`.

**PA3 — Wire applicable towers:** Battle and shared multi-barrel ProjectileTurretBase, Blaster and Rocket use cached effective handling, actual barrel markers and unchanged visual FX. Tune the existing authored levels; add each tower's rank bonus values. Laser and Shockwave remain untouched; test both for regression. Avoid changing enemy weapon or visual recoil handling.

**PA4 — Tests + documentation/QA:** run C# build, Godot .NET import/headless focused checks, existing projectile/combat, rank, research, ammunition, heat, stealth and offensive tower suites as available. Inspect gameplay for Machine Gun spread, precision Sniper, missed shots, moving-target crossing, status accuracy, and splash offset. Document commands, actual pass/fail output, known unrelated failures and any unverified visual behavior. Update `TOWERS/README.md`, `Shared/Projectiles/README.md`, `Shared/Stats/README.md`, `Shared/Ranks/README.md`, relevant tower README(s), `TESTS/Towers/` docs, and `NOTES/WORK.md` as appropriate. One final GitHub commit only after the passes.

## Mandatory checks / definition of done

- No research equipped + Standard rank: straight rounds with per-level spread. `SpreadDegrees=0` is a precise straight shot.
- Moving/removed original target never pulls an already-fired straight round; stray bullets may hit the first physically intersected living enemy; missed bullets expire. No frame-step tunnelling.
- Three level rows author **absolute spread**; rank is independent and only its selected **total** bonus applies. Promotion updates FinalStats and Inspector preview consistently; rank never silently upgrades purchased level.
- Research Stage04's **already implemented** Battle damage bonus still passes. Future guidance/precision hooks must not unlock anything by default or alter existing persistent saves.
- Laser stays continuously tracking; Shockwave stays radial; unarmed/passive towers are unaffected; Rocket remains unguided.
- Muzzle launch, tracer orientation, flashes/recoil, ammo spent, heat accrued, typed defenses, status payloads, shield penetration, AoE recipient selection and XP attribution continue to behave correctly.
- High-bullet-count tests confirm no per-shot full-enemy scans or unbounded allocations; reuse query/collision scratch buffers where sensible and avoid extra node trees.
- Tests are deterministic; adjust preexisting tests that assumed all bullets home, **without deleting meaningful coverage**.
- If any proposed design conflicts with current APIs or collision costs, adjust the implementation minimally and **record the exact deviation here**. Do not mislabel unimplemented work as completed.

## Explicitly deferred

Actual Precision Targeting / Homing research tree, RP costs/UI/slots, homing steering upgrades, target leading/prediction, recoil affecting aim, accuracy debuffs/jammers, terrain obstruction, magazines/reloading, different hitboxes per armor component, and any enemy weapon accuracy changes.
