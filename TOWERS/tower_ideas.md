# Tower Ideas — Architecture and Weapon Families

> **Proposal only — NOT implemented.** This document records a scalable direction for approximately 30 offensive towers, with room for many more, while also supporting passive, support, economy and hybrid towers. Do not treat planned files, features or tower names here as existing code.
>
> Before implementing: read `NOTES/CodePreferences.md`, `TOWERS/README.md`, `TOWERS/Shared/README.md`, `TOWERS/Shared/Stats/README.md`, `TOWERS/Placement/README.md`, and the relevant tower-specific README. Prefer lightweight, reusable, Inspector-editable C# systems and compact scene trees.

## Goals

- Support 30+ **offensive tower types**, with no arbitrary tower-count ceiling.
- Keep one self-contained folder per distinct tower type: artwork, scene, optional custom script, default config and README.
- Reuse common functionality instead of copying the Battle Tower's targeting, cooldowns, placement and stat code into every new tower.
- Keep **weapon behavior optional**: Repair, Solar, Resource, Radar, and hybrid towers must not be forced to fire or track targets.
- Allow shared code and small data-only variants where two towers differ only in stats/art. A distinct tower scene is normal and inexpensive to author.
- Preserve the existing eight-stage stat pipeline and existing economy, grid, connectivity, HUD and navigation boundaries.
- Keep the Godot Inspector useful: readable ExportGroups, typed Resource configurations, clearly separated base values and per-instance effective stats.
- Favor simple, performance-conscious systems over a large inheritance hierarchy or a giant universal weapon script.

## Actual repository state (October 2026)

**Working:**
- `Offensive/BattleTower/BattleTower.tscn` is the only working offensive tower scene. Its root is `Node2D` with `BattleTower.cs`; a `TurretPivot/Sprite` child provides rotating visuals.
- `BattleTowerConfig.cs` and `DefaultBattleTowerConfig.tres` define its Inspector-editable authored stats and costs. Placed instances receive a deep-duplicated config.
- `Shared/Targeting/TowerTargeting.cs` provides First (shortest remaining route to the Citadel) and Closest (world distance to tower).
- `Shared/Weapons/TurretAiming.cs` provides reusable tracking/alignment; `Shared/Projectiles/BasicProjectile.cs` provides a guided test projectile.
- `Shared/Stats/Pipeline/` already has eight ordered stages and immutable effective stat snapshots. Base, safety clamps and final publication are active; upgrades, ranks, research, level modifiers and active effects are currently neutral.
- `Placement/TowerPlacementController.cs` owns the Battle Tower build preview, scene instantiation, occupancy, selection and economy purchase integration.
- `UI/GameplayHUD/GameplayHUD.cs` uses a prototype list with hardcoded tower buttons; non-Battle entries are only samples.

**Not implemented yet:** a common `TowerBase`, tower definition/catalog, generic multi-type placement, dynamic build menu, shared weapon-family implementations, tower upgrades and selling. The existing `TowerMasterConfig` includes future switches; those switches alone do not implement features.

**Main scaling issue:** placement, selection, HUD choices and part of stat input are still coupled to `BattleTower`/`BattleTowerConfig`. Adding 30 towers by copying this setup 30 times would create unnecessary maintenance work.

## Proposed ownership

### Shared tower foundation

Introduce a **small `TowerBase.cs`** (or equivalent common component) for the minimum every placed tower genuinely needs:
- Stable tower type ID, grid anchor/footprint and instance identity.
- Placement/selection hooks and access to its authored config or definition.
- Instance progression state (when upgrades/ranks exist) and a change notification for the Inspector.
- Optional health, selling and other common lifecycle features **only when actually implemented**.

Do **not** place targeting loops, projectiles, cooldowns, rotation or weapon nodes in the mandatory base. Passive towers must remain lightweight.

A **`TowerBase.tscn` is optional**, not a requirement for every type. Use Godot inherited scenes only if they meaningfully share a stable node hierarchy (for example a selectable visual root). Otherwise use separate scenes inheriting the **C#** base class. Avoid brittle long inherited-scene chains.

### Tower definitions and catalog

Propose a typed `TowerDefinition` Resource plus a `TowerCatalog` (names are provisional):
- Stable string ID, name, description, broad category/subcategory.
- `PackedScene`, build-menu icon/art, default Inspector config and declared footprint.
- Construction recipe via the **existing** `ECONOMY/Transactions/ResourceCost.cs`; do not invent a separate wallet or competing price source.
- Optional tags/capabilities (for example Offensive, AntiAir, Beam, Repair, EnergyProducer).
- Registry lookups by ID; the catalog becomes the authoritative list of buildable tower types.

Keep metadata and gameplay-specific numbers in well-defined, nonduplicated places. One authoritative config/definition should own each fact; the HUD should **read** that data rather than maintain another hardcoded copy.

### Generic placement and build HUD

Refactor `TowerPlacementController` so it accepts a tower ID/definition rather than an explicit `BattleTowerScene` and `BattleTowerConfig`.
- Instantiate the selected `PackedScene` as `TowerBase`, store placed towers by grid anchor/ID and show the selected instance's true stats.
- Preserve the current WorldGrid tile/occupancy checks, SpawnManager route-connectivity checks, atomic `EconomyManager.TryPurchase` transaction and rollback guarantees.
- Use definition-driven preview art, costs, shape and validity checks; keep preview processing active only while placing.
- The current Battle Tower is 1×1. **Multi-cell footprints are future work** and must validate/reserve/release every occupied cell and ensure navigation remains valid.
- Populate the build tray from the tower catalog, grouped/filterable by Offensive / Support / Economy / Utility when needed. Avoid 30 hardcoded button names, duplicated display strings or giant switch statements.
- A single world tower container can hold instances of many different scene types. There is no need to create 30 permanent active scenes at startup.

### Data versus special scripts

**Use a separate scene for a visually or mechanically distinct tower.** A 30-tower roster can naturally contain 30 small `.tscn` files, but should not require 30 copies of the same `_PhysicsProcess` combat loop.

- **Data variation only:** different damage, cooldown, range, muzzle offsets, art, colors or targeting priority → reuse the same behavior and scene when sensible, with a different config/definition.
- **Different visual geometry:** separate scene (for example twin-barrel Minigun versus stationary prism) while reusing common weapon code.
- **Genuinely unique mechanic:** a focused tower-specific component/script is fine (for example drone hangar management or a charge-up beam).
- Avoid a growing chain of `if (towerId == ...)` branches in the base class, placement controller or HUD.

## Weapon families and reusable building blocks

A **weapon family** groups towers by broad delivery/attack behavior; it is **not necessarily a C# subclass**. Families can share components, while effects such as freeze or splash can be combined with multiple families.

| Family | Shared behavior to consider | Example tower concepts |
| --- | --- | --- |
| **Kinetic / Projectile** | Target + lead/aim, launch projectiles, hit handling | Battle, Minigun, Sniper, Railgun, Scattergun |
| **Energy / Beam** | Line/hitscan or continuous beam, tick damage, charge/cooldown | Laser, Plasma Lance, Arc Beam, Ion Beam, Prism |
| **Explosive / Indirect** | Projectile flight, explosion radius/falloff, impact timing | Missile, Cannon, Mortar, Cluster Launcher, Bombard |
| **Area / Chain** | Radial pulses, arcs/chains, continuous proximity field | Tesla, Shockwave, Flamethrower, EMP Emitter, Gravity Well |
| **Status / Control** | Shared effect application/expiry/stack rules | Cryo, Stasis, Disruptor, Corrosive, Suppressor |
| **Special / Hybrid** | Optional combination of weapon and special ability modules | AA Rocket, Drone Hangar, Ricochet, Sentry Array, Orbital Beacon |

These are **30 candidate ideas, not 30 promised implementations**. They can be renamed, combined, split, or moved between families as designs evolve. In particular, Status/Control effects can sit on top of projectile, beam or area delivery rather than requiring an entirely separate weapon system.

### Composable attack dimensions

Rather than hardcode a unique system for every tower, consider reusing a small number of behaviors:

1. **Acquisition:** First/Closest (already working); later Strongest, Weakest, flying-only, ground-only, status-aware targeting. Preserve the distinction between path distance and physical proximity.
2. **Aiming:** fixed, tracking turret (existing `TurretAiming`), independent barrels, or no rotation.
3. **Fire timing:** single shot, burst, rapid fire, sustained beam, charge-up, periodic pulse, or triggered attack.
4. **Delivery:** guided projectile, straight projectile, hitscan, beam, arc/chain, ground-targeted strike, or radial wave.
5. **Impact:** direct damage, blast, pierce, falloff, chain, damage-over-time, slow, stun/freeze, armor reduction or mixed effects.
6. **Presentation:** projectile/art scene, muzzle flashes, recoil, beams, explosions, audio, damage numbers and other optional feedback.

The first implementation should remain small: extract only code already shared by at least two working towers. Do not build every listed delivery/effect type up front.

## Proposed folder layout (future; not a claim these files exist)

```text
TOWERS/
├── README.md
├── tower_ideas.md                    # This proposal
├── TowerMasterConfig.cs             # Existing
├── Shared/
│   ├── TowerBase.cs                 # Proposed shared placed-tower API
│   ├── TowerDefinition.cs           # Proposed Inspector-editable type metadata
│   ├── TowerCatalog.cs              # Proposed tower type lookup
│   ├── Stats/                       # Existing eight-stage pipeline
│   ├── Targeting/                   # Existing, extend only as needed
│   ├── Weapons/                     # Existing aiming; future optional attack behaviors
│   ├── Projectiles/                 # Existing BasicProjectile, future delivery types
│   └── Upgrades/                    # Future typed upgrade data/operations
├── Placement/
│   └── TowerPlacementController.cs  # Existing; generalize, do not duplicate
├── Offensive/
│   ├── BattleTower/                 # Existing working scene/script/config/art
│   ├── LaserTower/                  # Planned
│   ├── MissileTower/                # Planned
│   └── ...                          # Other distinct tower types
├── Support/
├── Economy/
└── Utility/
```

A typical specialized offensive tower may have its own `MyTower.tscn`, `DefaultMyTowerConfig.tres`, `Artwork/`, and `README.md`; add `MyTower.cs` **only if** a reusable component cannot express its unique behavior. Keep per-type implementation close to that tower, and common helpers in `Shared/`.

A possible specialized scene:

```text
LaserTower (TowerBase C# script or specialized derived script)
├── VisualRoot
│   ├── BaseSprite
│   └── TurretPivot                 # Optional; not for a fixed emitter
│       ├── TurretSprite
│       └── Muzzle
└── WeaponComponent                # Optional shared controller if useful
```

Do not require these exact node names in every type. Common code should use explicit exported references or clearly defined optional contracts rather than assuming all towers have `TurretPivot/Sprite`.

## Stats, upgrades, economy and effects

- **Keep the existing eight-stage order:** Base Stats → Tower Upgrades → Tower Ranks → Permanent Research → Level Modifiers → Active Effects → Safety Clamps → Final Stats.
- The current `TowerStatPipeline.Resolve(BattleTowerConfig)` is Battle-specific at its input edge. Generalize the input to type-appropriate authored stats/context without discarding the already-working immutable pipeline.
- Don't force weapon-only fields (projectile speed, shots/sec) onto pure support/economy towers as mandatory behavior. Use typed capability-specific stats or small optional stat blocks **when there is a second real use case**.
- Construction costs belong to the shared `ECONOMY` transaction types; prices are not a combat modifier by default. New tower types must continue to pay atomically and roll back on failure.
- Purchased upgrade tiers, earned tower ranks, permanent player research, level modifiers and temporary effects are separate inputs, as specified in `TOWERS/README.md`. Never repeatedly mutate calculated final stats or shared default `.tres` data.
- Each placed instance owns its own runtime progression/config state. A type definition is shared authored data; the final stats snapshot is a recalculated **result**, not the save source.
- Future effects should have explicit source, applicability, duration, stacking and expiry. Do not implement a full generalized effect engine before a tower actually needs it.
- The Inspector should eventually show current effective stats, cost, level/rank and upgrade preview from the **same resolver** used in combat.

## Performance principles for 30+ tower types

Thirty authored types are not inherently a performance problem. The relevant load is **how many tower instances, enemies, shots, effects and searches are active at once**.

- Keep the current staggered target searches; cached targets can be validated cheaply between searches.
- Avoid each tower scanning all enemies every physics frame. Profile before introducing spatial buckets or other indexing.
- Resolve effective stats **on input changes**, not every attack tick. Preserve cached per-instance `FinalStats`.
- Cache scene-node references in `_Ready()`; don't repeatedly traverse scene trees during combat.
- Avoid mandatory `_PhysicsProcess` on passive towers. Choose lightweight shared helpers over complex per-node scene hierarchies.
- Pool high-volume projectile/effect nodes only when measurements demonstrate allocation/GC overhead.
- Keep expensive range previews, trajectory debugging and visual overlays off unless selected/debugging.
- Profile large waves and mixed tower types; verify no regression in movement/navigation and build-time connectivity.

## Suggested implementation order

All phases below are **future work**; this file makes no code changes.

1. **Common identity:** add the minimal `TowerBase` (and scene inheritance only if it helps). Migrate the existing Battle Tower without changing its combat or appearance. Preserve current tests.
2. **Type/catalog + generic placement:** register Battle Tower, replace Battle-only creation/dictionary/UI coupling, retain the same world occupancy, route safety, selection, wallet transaction and refund-on-failure behavior.
3. **Catalog-driven build tray/Inspector:** render build entries and previews from definitions instead of the HUD sample list. Still ship only Battle Tower until stable.
4. **Second distinct weapon family:** implement **Laser Tower** as a contrasting beam/hitscan example. Extract genuinely shared weapon plumbing while keeping laser-specific behavior optional.
5. **Third family and variation:** implement **Missile or Mortar** (explosive splash), then **Minigun** (projectile family using changed cadence/muzzles). Confirm new types do not require editing the placement system.
6. **Progression and effects:** activate the existing stat-pipeline stages as their real owning features arrive; later add upgrades, tower health, sell behavior and more targeting/weapon modules.

### Acceptance criteria for the first architecture pass

- Existing Battle Tower builds, aims, targets, fires, damages enemies and displays the correct Inspector stats as before.
- Construction still costs the correct `ResourceCost` entries; invalid/route-blocking placement does not spend resources.
- Tower scenes do not need to inherit unwanted attack nodes or logic.
- A second tower can be registered and placed **without new hardcoded IDs or per-type branches** in the placement controller.
- Multiple towers can be selected and inspected independently; runtime configs and stats remain per-instance.
- Existing navigation, terrain, combat, economy, HUD and stat-pipeline tests pass; add regression coverage for catalog lookup and multi-type placement when implemented.

## Scope note

This is an expandable design proposal, **not an instruction to refactor everything immediately**. Prefer migrating one working feature at a time, building on the current C#/.NET Godot architecture. Keep `TOWERS/README.md` and feature-specific READMEs authoritative for actual implemented behavior, and update them as phases are completed.
