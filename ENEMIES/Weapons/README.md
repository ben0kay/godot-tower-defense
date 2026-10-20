## EG2 — centralized hostile bullets — source implemented (2026-10-20)

World/EnemyBullets now owns a fixed-capacity dense shot buffer and one active-only physics callback; World/EnemyBulletRenderer draws those same live shots at projectile depth. No per-bullet Node2D, physics body, Tween or Timer is created. EnemyMinigun submits real barrel-tip requests directly to this world service; ShotRequested now observes accepted launches. Sources and targets are never retained in bullet state, so emitted rounds survive shooter death/removal.

Straight flight uses captured world direction/speed and a finite lifetime. Every step is clipped to remaining lifetime and swept across spatial tower candidates. Cached world AABBs provide broadphase; exact clipping in grid space resolves full footprints, including transformed grids. First contact wins, with registration-order ties. Each round is consumed before one Kinetic TowerBase.ApplyDamage(DamageInfo) call, with null DamageSource. No enemy/Citadel/wall collision or friendly XP attribution is introduced.

Capacity defaults to4096 active bullets; lifetime defaults to2.5s. DefaultEnemyProjectileConfig.tres contains Inspector-editable budgets and a generic small7×1.3px muted-red tracer fallback. EnemyMinigunConfig optionally captures its own ProjectileVisual and BulletLifetimeSeconds; dedicated Gunner Drone tracer/flash presets and scene/F2 registration remain EG3. Current enemies and authored waves remain unarmed/unchanged. No spread is added; bullets follow actual barrel direction exactly.

Pause freezes launch/travel/lifetime. Invalid launches are rejected; numerically unrepresentable endpoints are invalidated. Lifetime expiry removes missed rounds. Capacity overflow rejects new launches without deleting already-fired shots or queuing later catch-up fire. No offscreen gameplay culling exists. Counters expose ActiveCount, Spawned, Impacts, Expired, Invalidated, Cleared, Rejected, Overflow and CandidateChecks. Clear is for explicit world reset only; source death must not call it. Unload releases buffers and renderer binding.

**Verification:** regression fixtures and static resource/scene/source checks are written/reviewed. No execution environment was available: C# build, Godot headless/runtime checks and desktop visual acceptance remain pending. No bullet-hell throughput/GPU batching claims are made; EG4 remains measured profiling. Earlier EG1-only/documentation-only statements below are retained as history and superseded by this entry. EG3 and EG4 remain planned.

---

## EG1 — source implemented, runtime verification pending (2026-10-20)

EG1 now supplies optional EnemyWeaponMount/EnemyWeaponConfig, an EnemyMinigun burst/request seam, shared closest-tower targeting and one world-scoped TowerRegistry. The registry observes actual World/Towers membership, root transforms and hull destruction; it caches full multi-cell footprint AABBs, deduplicates spatial candidates and uses a stable insertion serial for exact target ties. Startup discovery supports existing direct/preplaced tower instances. Construction sites and disabled living towers remain eligible; destroyed, queued, detached and foreign-world targets do not.

Only mounted enemies use staggered acquisition/aim callbacks; existing enemy scenes, definitions, catalog, F2 and waves are unchanged. Gun pivots aim independently in world heading while hull/navigation remain owned by EnemyMovement. Validated authored settings are captured per initialization/config assignment; assigning Config or calling RefreshConfiguration explicitly recaptures edits. Pause holds clocks; target loss clears the lock without bypassing its search interval; arrival/death/exit stop the component. Freeze suspends the optional weapon while Slow remains a movement modifier.

**EG2–EG4 remain unimplemented.** ShotRequested is a delivery seam only: EG1 creates no hostile bullets, flashes, gunner art/catalog entry or tower damage. An EG2 world-owned manager will consume EnemyShotRequest (origin, actual forward direction, damage, speed); do not add a guided BasicProjectile or direct fake hit here. No consumer means no shot request is counted. Later firing visuals/styles and spread belong to EG2/EG3.

Component authoring: add EnemyWeaponMount (or EnemyMinigun with EnemyMinigunConfig) as a Node2D descendant of the enemy; group under a **Node2D Weapons** if needed to preserve transform inheritance. Its child TurretPivot contains GunSprite/Muzzle Marker2D. PivotPath and MuzzlePath are relative to the mount and must select descendants. No source scene is armed by EG1. See Shared/README.md, Minigun/README.md, ../../TOWERS/Shared/Registry/README.md and ../../TESTS/EnemyWeapons/README.md.

**Validation status:** source/static reference review only. No execution environment was available, so Godot import, C# compilation, focused headless regressions and desktop behavior remain unverified. EG1 acceptance boxes below remain open pending those checks; the earlier documentation-only status/plan is retained as history and superseded by this implementation entry.

---

# Enemy Weapons — EG1–EG4 Gunner Drone and hostile bullet-hell foundation

**STATUS: APPROVED DESIGN / CODEX IMPLEMENTATION PLAN — NOT IMPLEMENTED (2026-10-20).**

The first enemy with a gun should be a **new Gunner Drone**, using a **small forward-firing minigun with narrow, slightly red-tinted tracer rounds**. Later enemies can share the same code for autocannons, multi-gun Cruisers, lasers and different firing patterns. This is a documentation-only handoff: no C# scripts, Godot scenes, resources, wave definitions or tests have been created by this planning pass.

Read the **root `README.md`**, **`NOTES/CodePreferences.md`**, **`ENEMIES/README.md`**, **`ENEMIES/Abilities/README.md`**, **`TOWERS/Shared/Durability/README.md`**, and this document *before* implementing any phase. Review the actual repo HEAD: other work may have advanced. Follow existing structure and simple modular, performance-conscious C# patterns. **One atomic GitHub commit per approved implementation job** and update test/docs status honestly.

## Core gameplay decisions

- The **Gunner Drone follows its normal assigned Citadel path** while its optional minigun independently aims at and attacks nearby **player towers**. It does **not** chase towers, stop to fight, replace checkpoints, change portal assignment, or damage the Citadel with gunshots. Normal arrival-at-Citadel rules remain unchanged.
- The first minigun has **one actual muzzle**, low-damage **unguided straight-line Kinetic bullets**, staggered tower-target checks, firing bursts and a rest interval. Once fired, a bullet travels on its launch direction; **not homing**. Aiming errors and moving source can produce misses. Visual tracers correspond to real fired bullets, not fake hitscan lines.
- The first target preference is **closest eligible placed living tower** within weapon range, with deterministic ties. Optional weapon types later can specify priorities independently of the tower's existing First/Last/etc. system. No enemy-to-enemy friendly fire in this phase.
- Bullets **hit the first valid tower footprint they intersect**, even if that wasn't the originally aimed tower; use swept segment-vs-world footprint collision and apply **one** Kinetic hit, then remove the bullet. Do not let rounds pass through a tower because they crossed it between 60-Hz ticks. **Terrain/wall bullet collision, Citadel bullet damage and player-unit hits are explicitly deferred**, not implicit features. Pathfinding and tile occupancy stay authoritative elsewhere.
- Existing unarmed enemies remain lightweight: **no mandatory weapon child, timers, scans or extra physics callbacks** for all enemy types. Do **not** automatically arm `Enemy001` / `basic_mk1`, other 13 catalog entries, carriers' death-spawn children, or authored waves.
- The new Gunner Drone may be added to the **existing enemy catalog and F2 debug cycling** for manual testing with its own unique stable enemy ID (suggest `gunner_drone_mk1`), while remaining **absent from normal Level001/002/020 waves** until explicit balance approval.
- Tower damage is already implemented. This feature must reuse `TowerBase.ApplyDamage(DamageInfo)` and the shared `COMBAT/Damage/` resolver, including existing HP, death, footprint release, HUD/Inspector and pause. No duplicate enemy-damage calculation or special `TakeTowerDamage` handler.

## Folder ownership — proposed paths, not files that already exist

```text
ENEMIES/
  Weapons/                                    [NEW; shared enemy offensive capabilities]
    README.md                                  [THIS DESIGN DOCUMENT]
    Shared/
      EnemyWeaponMount.cs                     optional component lifecycle, owner/aim/muzzle
      EnemyWeaponConfig.cs                    common Resource: range, damage, speed, turn, cadence
      EnemyTowerTargeting.cs                  shared tower eligibility + staggered spatial queries
    Minigun/
      EnemyMinigun.cs                         firing cadence, burst/rest and single muzzle
      EnemyMinigunConfig.cs                   minigun-specific exported Resource
      DefaultEnemyMinigun.tres                per-weapon parameters
      Visuals/
        EnemyMinigunTracer.tres               SMALL MUTED RED tracer style
        EnemyMinigunMuzzleFlash.tres          small warm red/orange flash style
      README.md                               implementation rules when implemented
  Projectiles/                                [NEW; hostile bullets only]
    EnemyBulletSystem.cs                      one world-owned, physics-ticked bullet simulation
    EnemyBulletData.cs                        compact bullet position/velocity/lifetime/hit payload
    EnemyBulletRenderer.cs                    one drawing surface, no per-bullet scene nodes
    README.md
  Types/GunnerDrone/                          [NEW enemy type; inherits GenericEnemy]
    GunnerDrone.tscn
    GunnerDrone.tres                          normal existing EnemyDefinition resource
    Artwork/
      gunner_drone.svg                        new enemy hull
      gunner_minigun.svg                      separate small weapon sprite if helpful
    README.md

TOWERS/
  Shared/Registry/                            [NEW ONLY IF NOT ALREADY SUPPLIED]
    TowerRegistry.cs                          per-world placed/living tower spatial index
    README.md

WORLD/World.tscn                              runtime composition, optional system nodes
TESTS/EnemyWeapons/                           targeted focused Godot headless tests
```

**Important ownership rules:**

- `ENEMIES/Shared/GenericEnemy.tscn` remains the basic movement/health/sprite/DefenseBar scene. The new Gunner Drone **inherits it** and adds a `Weapons/Minigun` child with `TurretPivot/GunSprite/Muzzle` (`Marker2D`). Its body can turn with path movement while the gun pivot tracks towers independently.
- Enemy weapons are **weapons**, not one-off `ENEMIES/Abilities` scripts. The `Weapons` folder owns aiming/firing, `Projectiles` owns bullet movement/impact, and `Types/GunnerDrone` owns unique art, config and scene. Do not put Minigun simulation in `Enemy.cs`, `EnemyMovement.cs` or every enemy's type folder.
- `TOWERS/Shared/Weapons/MultiBarrel/ProjectileTurretBase.cs` inherits `TowerBase` and targets enemies: **do not subclass or reuse it directly for armed enemies**. Reuse independent helpers when practical: `TurretAiming.Track`, `BarrelFireController` (later multi-muzzle support), `ProjectileVisualStyle`, `MuzzleFlashStyle`, `WeaponVisualMath`, and possibly `WeaponVisualRenderer` if it fits without tower-specific coupling.
- Existing `TOWERS/Shared/Projectiles/BasicProjectile.cs` is a **guided per-bullet Node2D targeting `Enemy`**. It is not suitable unchanged for large volumes of hostile unguided bullets. The proposed hostile bullet manager should call `TowerBase.ApplyDamage` with an immutable `DamageInfo` whose tower-only `DamageSource` is **null**; no tower XP or friendly credit attribution.
- `TowerPlacementController` currently caches tower objects per occupied cell to support selection and removal, but does not expose a general world-scoped nearby-tower registry. Provide **one world-owned index** driven by successful placements and exit/destruction (and existing preplaced towers if supported), including complete multi-cell footprints. Avoid each armed enemy traversing `World/Towers.GetChildren()` on every acquisition or bullet tick. If an equivalent index lands elsewhere first, **reuse it instead of adding a redundant registry**.
- The bullet system should be **world-scoped**, not a gun-owned child that dies with its shooter. Shots already in flight continue after the gunner dies (until impact/lifetime/world teardown). Keep the render layer at the appropriate depth relative to `World/Projectiles`, `Enemies`, `Towers` and `Effects`, without putting every bullet in its own `Node2D`.
- One weapon mount can have one private runtime cooldown, target and burst timer; authoring `.tres` resources are treated as read-only and snapshot/validated for spawned instances. Use exported grouped settings and type-specific style preset references, not hard-coded colors in firing logic.

## First Gunner Drone — tentative weapon defaults

All values are **initial Inspector-editable playtest proposals, not final game balancing**:

| Weapon setting | Initial value |
| --- | --- |
| Target | Closest placed alive tower in range |
| Range | 260 px |
| Damage / hit | 2 Kinetic hull damage (via shared resolver) |
| Total bullets / second | 6 (one muzzle, **not** six per barrel) |
| Bullet speed | 1,200 px/s |
| Burst | 1.5 seconds of firing, then rest |
| Rest between bursts | 1 second |
| Turn speed | 240 degrees / second (tune) |
| Spread | ±3 degrees (small jitter, deterministic/session-safe RNG if needed) |
| Ammunition | Unlimited for this first enemy; not connected to friendly factory/depot logistics |
| Motion | **Keep advancing** through current route while firing |
| Weapon availability | Opt-in on Gunner Drone only, disabled while dead/removed |

One uninterrupted 1.5s burst at 6 shots/s produces approximately **9 low-damage rounds**; the average per-gun threat is intentionally modest at first. The initial Gunner Drone health/speed/credit reward should be defined independently in `GunnerDrone.tres` and tuned after F2 testing, not silently borrowed from `Enemy001`.

## Enemy Minigun visual identity — SMALL + SLIGHTLY RED (USER REQUEST)

**This is essential to the first implementation.** Enemy bullets must be visibly distinguishable from player tower projectiles, but **small and not neon-bright**. Use a reusable **`ProjectileVisualStyle` preset** stored at `ENEMIES/Weapons/Minigun/Visuals/EnemyMinigunTracer.tres`, or a 100%-compatible shared visual snapshot if the centralized renderer requires it. Keep the preset editable from Godot Inspector.

Suggested first look relative to the existing friendly tracer default (**18 px length, 2 px core thickness, 2.5× glow**):

| Visual parameter | Enemy Minigun starting value |
| --- | --- |
| Tracer body | Muted warm/coral red, **`#EF6661`** |
| Bright central core | Soft peach red **`#FFB3AB`** (small, not white-hot) |
| Glow | Deeper red **`#C83232`**, alpha **0.15–0.22**, opt-in |
| Length | **6–7 px** (default 7; visibly shorter than friendly tracers) |
| Core thickness | **1.1–1.4 px** (default 1.3; smaller/slimmer) |
| Glow width multiplier | **2.0×**, faint |
| Tip | Small and understated; default `BrightTip=false` |
| Muzzle flash | **4–6 px** small orange-red spark, ~0.05–0.08 seconds |
| Orientation | Thin tracer points **exactly along the real bullet travel direction** |
| Visibility | Still readable against dark starfields, metal floors and zoomed-out maps |

Tracers should **not** reuse the yellow/white friendly Machine Gun Tower's preset. **Color is presentation only**: it does not change Kinetic damage type, faction, tower HP multipliers, collision or trajectory. Prefer a tiny elongated illuminated line with just a faint soft edge, not thick laser beams, oversized circles, giant red muzzle flares or huge full-screen bloom.

At extreme zoom-out, ensure tiny bullets remain perceptible; make `LengthPixels`, `CoreThicknessPixels` and glow color/opacity Inspector-tweakable. If batching requires a small draw-snapshot struct rather than a Material instance per bullet, capture the preset at launch and never mutate one shared resource dynamically.

## Recommended Codex implementation phases

### EG1 — Enemy weapons and tower target eligibility

- [ ] Add optional scene-mounted `EnemyWeaponMount` and a minimal `EnemyMinigun` owner/cadence or equivalent; wire to `Enemy.Initialized`, `Enemy.Removed` and `_ExitTree`. No changes to plain enemy runtime costs.
- [ ] Add one authoritative world-scoped placed tower lookup (or reuse an equivalent pre-existing index), register actual tower placement/exit, include multi-cell footprints, and exclude destroyed/freed/queued targets. Query at **staggered intervals**, not full tower scans per physics tick per enemy.
- [ ] Reuse the independent shared `TurretAiming.Track` and a real muzzle `Marker2D`; keep enemy navigation on its existing owner.
- [ ] Provide editable config validation, pause/removed/target-loss behavior and sensible no-target idle processing. Do not add new controls to tower Inspector or global targeting priorities.

### EG2 — Centralized hostile bullets + tower damage

- [ ] Install a **single world-owned `EnemyBulletSystem`** and lightweight `EnemyBulletRenderer`, with compact retained bullet state and no per-bullet `Node2D`/per-shot Tween/Timer/PhysicsBody.
- [ ] Spawn from `Muzzle.GlobalPosition` with initial direction determined by actual aim (plus optional bounded spread); travel straight. Swept collision against nearby living tower footprints, earliest hit first; apply one Kinetic `DamageInfo` via `TowerBase.ApplyDamage` and remove. No Citadel or wall hit logic in first pass.
- [ ] Guard finite position/speed/delta/damage, lifetime expiry, target destroyed/moved, multiple bullets hitting a dying tower, source removal, game pause and scene unload. Bullets should not keep enemy/tower Node references alive unnecessarily.
- [ ] Use spatial lookup/broadphase, controlled active-bullet budgets and simple instrumentation (active, spawned, impacts, culled/expired and overflow) that can feed debug F4 later. **Offscreen visual culling must not silently cancel gameplay bullets still capable of hitting a tower**; expire only on real lifetime, world-bound or explicitly tested gameplay rules.

### EG3 — First Gunner Drone + modest red visual preset

- [ ] Create `ENEMIES/Types/GunnerDrone/GunnerDrone.tscn` as inherited `GenericEnemy`; add a single independently rotating gun sprite and **one muzzle marker at the barrel tip**, not at hull center.
- [ ] Add unique `EnemyDefinition` `.tres` and catalog entry (`gunner_drone_mk1` suggested). Enable F2 spawning but **do not add it to any authored wave yet**. All pre-existing enemies stay unarmed.
- [ ] Create `DefaultEnemyMinigun.tres`, **`EnemyMinigunTracer.tres`** (small 7×1.3px red-tinted appearance), **`EnemyMinigunMuzzleFlash.tres`** (small flash), and optional separate gun artwork. Maintain readable forward orientation consistent with actual aiming.
- [ ] Manual visible test: deploy 1–10 Gunner Drones versus nearby friendly towers. Verify moving while aiming, thin muted-red tracers, muzzle alignment, misses, actual tower HP loss and clean destruction/selection/removal.

### EG4 — Bullet-hell performance / multi-gun preparation

- [ ] After EG1–EG3 prove the gameplay, benchmark **50, 100, 250+ armed enemies** and dense tower clusters. Test baseline frames and 100s of active bullets before claiming several thousand is safe; scale based on measured results and the developer's hardware.
- [ ] Keep physics calculations bounded with stable arrays/pools, spatial bucket broadphase and batch drawing where practical. `Node2D._Draw` alone is not proof of automatic GPU batching; verify draw/physics cost separately.
- [ ] Extend mounts to two+ muzzles/independent gun pivots later (Cruiser). Reuse `BarrelFireController` if helpful; do not implement a multi-weapon Cruiser, defensive point defense, new ship AI, tower shields, repair drones or terrain bullet blocking in the first Gunner Drone pass.

## Acceptance checks before marking complete

1. Godot .NET compiles, enemy catalog rebuilds, Gunner Drone F2 spawn works; existing wave definitions, spawn counts, standard enemy movement/stealth/shield/status handling stay unchanged.
2. Gunner follows assigned route while pivot tracks closest tower, fires from real barrel tip, stops fire at loss of range/target, **existing bullets remain independent**.
3. Bullets are **small, thin, slightly red** and clearly unlike friendly Machine Gun Tower tracers at ordinary zoom; scene screenshot/desktop visual approval is still required.
4. Fast bullets crossing a one-cell and a multi-cell tower footprint actually hit once; straight shots can miss; first intersection wins; unaffected towers/enemies take no damage.
5. Tower HP/DefenceBar/Inspector update through current tower APIs; tower death frees occupancy/navigation and cancels construction/ammunition logistics as existing durability already does.
6. Multiple armed Gunner Drones do not trigger repeated scene-wide tower scans, repeated movement path calculations or per-bullet Node2D allocations. All buffers clean up when a world unloads and no stale target survives reuse.
7. Existing pause, wave-clear/debrief, restart and F2 debug spawns retain their normal behaviour, including spawn/kill count ownership and no unapproved wave balance changes.
8. Keep `TESTS/EnemyWeapons/README.md` as implementation verification record (or existing `TESTS/` conventions); record actual run commands/results. **Do not claim tests were run just because authored.**

**Recommended build order: EG1 then EG2 then EG3; keep EG4 a measured follow-up.** An end-to-end EG1–EG3 branch/commit is acceptable only if separately tested and reviewed; otherwise ship one atomic commit per completed job and stop before expanding scope. This document is the agreed design, not a request to create everything at once without review.
