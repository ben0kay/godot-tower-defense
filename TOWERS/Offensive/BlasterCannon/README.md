# Blaster Cannon — Phase 4D (PLANNED; not implemented yet)

## Goal

Add a real **1x1, catalog-registered offensive Blaster Cannon**. It rotates/aims and fires a visible projectile. On impact, it performs **one immediate circular AoE hit** at the impact world position, dealing damage to all living enemy centers within its configurable radius. It is a different tower from the future Missile Tower. Keep its targeting, weapon config, projectile travel and visuals separate from the reusable math in `COMBAT/AreaDamage/`.

**Do not implement a shockwave as the Blaster attack.** Its blast damage is instantaneous; any expanding ring, flash or particle animation is presentation only. A true expanding wavefront is reserved for Phase 4E.

## Proposed authored playtest defaults (Inspector-editable; not final balancing)

| Source stat | Initial proposal | Meaning |
| --- | ---: | --- |
| Damage type | Explosive | Reuses global Explosive shield 0.25x / hull 1.25x defaults |
| Base damage per shot | 60 | Base damage *before* falloff and `DamageResolver` |
| Blast radius | 80 px | Enemies with center distance <= radius can be hit |
| **Edge damage fraction** | **0.25 (25%)** | Damage remaining at exactly the outer radius; **Blaster-owned**, NOT hardcoded globally |
| Shots per second | 0.5 | One shot every 2 seconds when able |
| Projectile speed | 500 px/s | Lightweight visible blast projectile |
| Tower range | 256 px | Range for acquiring/firing, not for damage after impact |
| Shield penetration | 0% | Separate from falloff; editable/upgrade-ready if needed later |
| Construction cost | 300 Credits | Provisional and owned only by this tower's config |

Example before shield/hull defense: a 60-damage explosion with `e=0.25` deals **60** at center, **37.5** halfway through radius, **15** at the edge. The shared AoE helper accepts `0.25` as input; it must have **no default 25% (or other weapon balance constant) baked into its code**.

## Research and paid upgrades are different

- **Falloff/edge damage fraction is a Blaster weapon stat**, exported in `BlasterCannonConfig.cs` and copied through the **existing eight-stage stat pipeline** to its immutable `TowerFinalStats`. Prefer a small **optional AoE capability** (e.g. `AreaBlastStats` with `RadiusPixels` and `EdgeDamageFraction`), so passive towers and Laser/Battle do not gain fake AoE fields. Add typed stat IDs and Stage 02 handling only for capabilities that exist. Stage 07 bounds `EdgeDamageFraction` to 0..1 and radius to finite nonnegative pixels; Stage 08 publishes both. It must be editable in the Godot Inspector, visible in the selected tower Inspector, and snapshotted at **projectile launch** with base damage and other hit modifiers.
- **Future permanent research can improve Blaster's falloff**, e.g. raising edge retention from 25% to 40% or 60%. This modifier should apply to eligible new/existing Blaster Cannons through **Stage 04 Permanent Research** once that real persistence system exists. **Keep Stage 04 neutral in 4D**; do not attach falloff research to Level 2/3 purchased tower tiers or create pretend save/progression state.
- It is fine to add one **normal paid Level 2 tower upgrade** to prove HUD/preview/purchase integration, such as +15 base damage and +16 px blast radius for 200 Credits (provisional). Do **not** increase edge retention via that tier. Use the existing ResourceCost wallet and canonical catalog price/recipe, not a new payment system. `TowerConfig.DescribeStats/DescribeChanges` should display actual AoE damage, radius, edge damage percent and current/next-tier numbers.

## Proposed file layout — Codex creates actual gameplay files during Phase 4D

```text
TOWERS/Offensive/BlasterCannon/
  README.md                        # this planned source of truth
  BlasterCannon.cs                 # type-specific firing/aiming logic
  BlasterCannonConfig.cs           # Inspector source values and actual prices
  DefaultBlasterCannonConfig.tres  # canonical playtest values
  BlasterCannon.tscn               # tower scene and pivot, simple placeholder art
  BlasterCannonDefinition.tres     # stable catalog ID: blaster_cannon
  Upgrades/Level2.tres             # optional simple paid tier
TOWERS/Shared/Projectiles/
  SplashProjectile.cs              # reusable impact delivery; calls shared instant AoE
TOWERS/Shared/Stats/
  AreaBlastStats.cs                # optional immutable blast capability if practical
COMBAT/AreaDamage/
  AreaDamage.cs                    # reusable one-time radial selection and base falloff
```

Keep `TowerBase`, `TowerTargeting`, `TurretAiming`, `TowerCatalog`, `TowerPlacementController` and `GameplayHUD` generic. Register Blaster in `TOWERS/DefaultTowerCatalog.tres` with its own art/scene/config and existing dynamic build UI. All new C# scripts should follow `NOTES/CodePreferences.md`: short script headers, `// =========================================================` separators above methods, optional regions, concise reusable code, stable Inspector export groups, no overengineering.

## Projectile and impact contract

Reuse Battle's firing/aiming patterns without modifying `BasicProjectile` behavior. Prefer a small new `SplashProjectile` supporting a configured target, speed, last-known impact position, area payload and **one** explosion on impact. If the original target dies/leaves mid-flight, keep traveling to the last-known position and still detonate once; do not disappear or switch to arbitrary targets. Clean up safely if the scene unloads or projectile times out. Whether the target is still alive or already gone, one impact produces one shared AoE event. **No separate direct-hit + splash double damage** to the primary target. No per-frame registry scan while projectiles travel.

A projectile snapshots effective base damage, blast radius, edge fraction, damage type, shield/hull overrides and penetration when fired; upgrading/config editing a tower after firing must not alter a projectile already in flight. Pass appropriately scaled **base** `DamageInfo` to each recipient through `Enemy.ApplyDamage`, preserving enemy shield/hull/death/rewards as they exist.

Add a minimal separate impact visual under the world effects layer if useful, but visual radius/timing must not perform damage. `FEEDBACK/DeathExplosion/` is an **enemy-death visual** only and is not an AoE delivery mechanism.

## Required Phase 4D regression checks

1. Full catalog/scene/config validation; dynamic build bar entry; real buy, route-safe placement, selection/Inspector, private config/tier copies, simple Level 2 preview and payment; no Battle/Laser changes.
2. Actual fired projectile reaches a point and detonates **once**, not while still traveling; primary target hit once, nearby enemies hit once, outside enemy untouched; target dies mid-flight or is removed safely; no extra kill credits; no effects after unload.
3. Changing only the Blaster's edge retention (e.g. 0.25 to 0.75) changes outer damage and its Inspector/cache without changing center damage, radius, inherited Explosive multipliers or any other tower. Include 0%, 100%, halfway, exact radius and falloff input validation.
4. Test Explosive shields/hull, shield-break spillover, optional per-hit coefficient overrides and zero-penetration baseline; verify use of existing `DamageResolver` exactly once per affected enemy.
5. Verify existing Battle 5% penetration, Laser 6.00/0.15 overrides and 12-DPS beam, paid upgrades, waves/shield generator, economy, navigation, 500-enemy integration fixture, and headless menus are unchanged. Area queries must execute only on impact; make 30/60/120 Hz movement/impact tests tolerant and deterministic.

Run real C# compilation and the existing headless tests, including `TESTS/EnemyIntegrationChecks.tscn` and `TESTS/MenuTowerFlowChecks.tscn`; report actual passes/failures and any visual limitations. After working implementation, update related READMEs from PLANNED to IMPLEMENTED with the **actual** chosen values and file paths.

## Scope fence

**Phase 4D = reusable instant AoE foundation + first functioning Blaster Cannon.** Do not implement timed wavefront shockwaves (Phase 4E), future research or persistence, Missile Tower, armor, tower health, terrain impacts, complex projectile pooling, line of sight, damage-over-time, crits or a generic weapon framework.
