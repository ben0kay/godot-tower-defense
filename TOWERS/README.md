# TOWERS — All Player-Placeable Towers

Umbrella folder for **every player-buildable tower**, not just weapons: offensive defenses, support, economy/production and utility. The UI can call all of them *towers*. A tower's primary role determines its folder, **not** its only possible capability: for example, `Economy/SolarTower/` may eventually gain a weak defensive laser upgrade.

## Current implementation (October 2026)

**Implemented today:** 1×1 Battle Tower placement/selection; terrain and connectivity validation; First/Closest target search, turret rotation, guided projectiles and enemy damage; atomic 200-Credit construction; per-instance Config copies; eight-stage cached stats (Phase 3A); and **Phase 3B paid upgrades**. Battle Tower upgrades level 1→2 (150 Credits) and 2→3 (250 Credits), with the selected Inspector showing real next-tier stat differences, cost and an Upgrade button. `TowerBase`, `Shared/Upgrades/`, `Shared/Stats/` and `TowerUpgradeChecks` exist.

**Phase 4A/4B implemented:** `TowerConfig`, `TowerDefinition`, `TowerCatalog` and `TowerPresentation` are the shared foundation. `TowerBase` additionally owns stable type identity, definition, grid anchor/footprint, config compatibility/application, placement/selection hooks and optional range inspection. Placement indexes `TowerBase` and resolves catalog IDs without Battle-specific branches or scene-node assumptions. The HUD creates available catalog entries in a horizontal scrolling tray, using `UIButtonFactory`. Construction identity/prices stay on the canonical Config, referenced by definitions; no second price or stat list exists.

**Default playable roster:** Battle Tower, Laser Tower and Blaster Cannon. The passive scene under `TESTS/Towers/` is a regression fixture, not a promised Repair/Solar tower or a default menu entry. Registering a valid additional definition creates its build choice without editing placement or HUD. Only 1×1 footprints are supported; other footprints fail registration.

**Phase 4C-1/4C-2 implemented (see completed4C-3 below for current balance):** COMBAT/Damage owns typed base hits and a cached canonical shield/hull response profile. Battle bullets deliver Kinetic; legacy TakeDamage keeps 1:1 semantics. Laser Tower sustains Energy DPS through the same resolver, with a separate endpoint-only BeamRenderer and Inspector-editable copied BeamStyle. Its genuine float BeamStats capability uses the same eight stages and paid upgrades. Shared targeting/aiming are reused; delivery remains type-specific rather than introducing a universal weapon switch. Ranks/research/level modifiers/effects, selling, tower HP, shield visuals and multi-cell placement remain future work.

**Codex:** read `NOTES/CodePreferences.md`, this README, `tower_ideas.md`, `Shared/README.md`, `Shared/Stats/README.md`, `Shared/Upgrades/README.md`, `Placement/README.md` and `Offensive/BattleTower/README.md` before editing. Work against latest HEAD, preserve the newly implemented upgrade work, and run existing tests.

## Folder map — existing versus proposed

| Path | Status | Responsibility |
| --- | --- | --- |
| `Shared/TowerBase.cs` | EXISTS | Lightweight per-instance levels/stats/event; minimal extension in Phase 4 |
| `Shared/Stats/` and `Shared/Stats/Pipeline/` | EXISTS | Eight ordered stages; Base, Upgrades, Safety and Final active |
| `Shared/Upgrades/` | EXISTS | Paid tiers, stat operations, validation, atomic wallet purchase |
| `Shared/Targeting/`, `Shared/Weapons/`, `Shared/Projectiles/` | EXISTS | Common target query, turret aiming and guided projectile |
| `Shared/TowerDefinition.cs` and `Shared/TowerCatalog.cs` | EXISTS (Phase 4A) | Type metadata, scene/config registry, dynamic build choices |
| `Placement/TowerPlacementController.cs` | EXISTS (generic TowerBase) | Generalize placement/selection/ghost, keep grid/economy ownership |
| `Offensive/BattleTower/` and `Offensive/BattleTower/Upgrades/` | EXISTS | First working offensive type; Level2/Level3 assets |
| `Offensive/LaserTower/` | EXISTS (4C-2) | Sustained Energy DPS, muzzle beam renderer and paid tier |
| `Offensive/MissileTower/` | PLANNED | Future explosive weapon |
| `Offensive/BlasterCannon/` | IMPLEMENTED Phase 4D | Instant AoE projectile tower; owns radius and falloff values |
| `Shared/Weapons/Shockwaves/` | FUTURE Phase 4E; README only | Timed expanding edge-only wavefront delivery |
| `COMBAT/Damage/` | EXISTS (4C-1) | Shared typed base damage and canonical shield/hull responses |
| `Support/`, `Economy/`, `Utility/` | PLANNED | Passive and hybrid tower categories, never mandatory weapon logic |
| `UI/GameplayHUD/` | EXISTS (dynamic catalog entries) | Replace build choice inputs with catalog data |
| `ECONOMY/` | EXISTS | The **only** wallet, ResourceCost and purchase authority |

The current `BattleTowerConfig.ConstructionCosts` and `DefaultBattleTowerConfig.tres` own construction price/base stats, with external `Level2.tres`/`Level3.tres` for per-type upgrades. Phase 4 must not create a second conflicting catalog price. Keep source data Inspector-editable. No other gameplay service belongs under `TOWERS/`.

## Phase 1 — Battle Tower rotation (already implemented)

**Already implemented:** `BattleTower.cs` uses `Shared/Weapons/TurretAiming.cs` with a rotating visual `TurretPivot`, Inspector settings and aim tolerance. The following notes document the intended behavior; **Codex must not implement a duplicate rotation system.** Weapon behavior remains optional for non-offensive towers. Never require a weapon or targeting loop on Repair, Radar, Solar or other passive towers.

- Preserve existing `TowerTargeting` (First/Closest), range checks, staggered searches, `ShotsPerSecond`, damage and projectile speed.
- Add Inspector-exported **`RotationSpeedDegrees`** (initially **180 degrees/sec**), **`AimToleranceDegrees`** (initially **12 degrees**) and `RotateWhileTracking` (default true). Optionally provide an idle-rotation policy, default disabled.
- Once a target is acquired, turn toward its current position using shortest-angle interpolation **capped by configured degrees/sec × delta**. Do not teleport/snap orientation, and don't waste targeting searches on every physics frame just to rotate.
- **Only fire when the target is alive/in range and the barrel is within the aim tolerance**, in addition to its existing cooldown. Keep the existing guided test projectile behavior for now; real ballistic/muzzle visuals can follow.
- **Current artwork is ONE sprite containing both base and barrel.** Rotating the whole image is acceptable *for this prototype*, but rotate a visual `TurretPivot` / child sprite rather than rotating the tower's placement root, grid identity, or range effect. The SVG's barrel points **upwards at rest**, so explicitly account for this artwork's local forward direction when calculating target angles.
- Future scene layout (not required to split artwork immediately):

```text
BattleTower (root: fixed grid/world position)
├── BaseSprite (future stationary visual)
├── TurretPivot (Node2D, rotates toward the target)
│   ├── Sprite (existing one-piece SVG for now; future turret-only art)
│   └── Muzzle (Marker2D, future accurate projectile origin)
└── DefenseBar (future, when tower health exists)
```

- Keep the current **center-spawned** bullets initially if a muzzle marker is not ready; no recoil, multi-barrel art, line of sight, or new enemy combat behaviors needed for this pass.
- Configure defaults in `Offensive/BattleTower/DefaultBattleTowerConfig.tres`. Every placed Battle Tower already receives a separate duplicated Config for individual tuning; retain that behavior.

**Acceptance:** F5 → Ctrl → build Battle Tower → F2 spawns Enemy001 → tower visibly and smoothly turns toward moving targets at the set speed; fires only after achieving its allowed aim; retains existing targeting/damage/cooldown and scene Inspector controls. No C# compile errors or unintentional sprite/grid rotations.

## Phase 2 — Shared construction costs and live economy (implemented)

The standalone **ECONOMY/** folder now owns the session wallet and shared transactions. Read **ECONOMY/README.md** for its current API and boundaries. TOWERS/Economy continues to describe producer tower types, not player balances.

BattleTowerConfig.ConstructionCosts is an Inspector-editable Array<ResourceCost>. The shared entry lives at ECONOMY/Transactions/ResourceCost.cs and references ResourceDefinition plus Amount. DefaultBattleTowerConfig.tres requires **200 Credits**; BuildCost has been retired to avoid competing prices/double charges. Optional additional resources and future upgrades reuse this same entry type.

LevelConfig exposes Starting Resources / Starting Credits. Level001 starts at **1,000**, configurable in Level001Config.tres; default/template starts at zero. One Main/Economy wallet initializes each session, clears balances on level initialization and updates the HUD through BalanceChanged events.

Terrain/occupancy and route connectivity validation run before purchase. TryPurchase validates the complete cost, runs the placement callback, and deducts only when tower creation succeeds. The callback rolls back scene/index/occupancy on exceptions. Insufficient funds, failed builds, route-sealing placement and repeat clicks on an occupied cell never charge. There is no free-build bypass enabled.

Prices are deeply duplicated with each placed tower's config. The build preview shows costs and affordability; the placed Inspector describes paid construction. Weapon rotation, targeting, navigation and combat remain unchanged.

## Phase 3 — Tower upgrades and a layered stat pipeline (3A/3B implemented)

Every tower **can optionally support upgrades**, including future support/economy types. Zero upgrade tiers is valid, and passive towers must not inherit targeting, projectile data or weapon loops.

**Already implemented:** `TowerBase` owns purchased `UpgradeLevel` and effective stats; `TowerUpgradeTier`/`TowerUpgradeModifier` hold Inspector-editable levels, typed stat operations and `ResourceCost` entries; `TowerUpgradeController.TryUpgrade` validates the tier, resolves next-level stats, pays atomically with `EconomyManager.TryPurchase`, and publishes state after payment. Battle Tower copies nested tier resources per placed instance. The selected Inspector displays price, live/next stats, affordability and max-tier state.

**Future:** activate rank, research, level rule and active-effect stat stages only with real owners; support type-specific stats (repair strength/production output) without giving a Solar Tower fictitious damage/fire rate. The existing Upgrade button and stats pipeline should be reused for new tower types. Optional upgrade-unlocked behaviors may require dedicated typed capability data later; do not implement a giant monolithic weapon-specific array or a second stat pipeline.

### Required stat pipeline

Purchased tower upgrades are only one source of progression. Future kill-based tower ranks, permanent player research, level rules and active abilities must coexist without overwriting one another:

**Base stats → Tower upgrade tier → Tower rank → Permanent player research → Level modifiers → Active buffs/debuffs → Safety clamps → Final stats**

| Stage | Owner and lifetime | Responsibility |
| --- | --- | --- |
| Base stats | Shared tower definition; immutable authored inputs | Original weapon/support/production values before progression. |
| Tower upgrade tier | This placed tower; purchased progression | Apply the current tier's explicit improvements or unlocked capabilities. |
| Tower rank | This placed tower; earned progression separate from purchased tier | Rank up after configured kill-count milestones; apply rank bonuses. Kill attribution/thresholds are future work. |
| Permanent player research | Persistent player/profile progression, outside individual towers and levels | Research unlocked through the future research center **follows the player across every level**, survives level restart/change and is restored on save/load. Apply only to eligible tower types/capabilities. |
| Level modifiers | Current level/session, declared separately in level settings | Mission-wide conditions such as reduced range or improved production; replace them when changing maps. They are not temporary-effect entries or permanent research. |
| Active buffs/debuffs | Runtime effect instances on eligible towers | Enemy abilities, player abilities and other active effects with their own source, duration/removal and stacking policy. |
| Safety clamps | Shared stat resolution, with capability-specific bounds | Prevent invalid/negative range, damage or production, nonfinite values and invalid cooldown/rotation values; retain explicitly supported zero-rate disabled firing. |
| Final stats | Derived per-instance cache | Values actually consumed by targeting, firing, rotation, repair or production. Never the authoritative progression/save input. |

A research center is a gameplay interface/source of unlocks, **not the owner of permanent research state**. Deleting the center or unloading a map must not erase research. Use a player/profile progression service when that system is implemented; no particular save-folder/service implementation is assumed to exist today.

Tower tier and tower rank remain separate: buying level 2 does not erase earned rank, and ranking up does not silently buy an upgrade. Per-tower kill counts must be attributed to the responsible tower once, not inferred from the global enemy registry's total. Tower progression lasts with that instance; carrying towers across maps is a separate future decision.

### Modifier rules and final-stat calculation

- Keep **stored inputs** (base definition, purchased tier, rank/kills, player research, current level rules, active effect instances) distinct from **calculated final stats**.
- Every modifier needs an explicit stat/capability, stable source identity, operation and applicable scope. For temporary effects, also define duration/removal and whether another application refreshes, replaces or stacks.
- Use clear operations: flat additions (+5 damage), additive percentage bonuses (+20% range), and explicit multipliers (×0.5 fire rate). Do not treat these as interchangeable.
- Recommended initial arithmetic **within each stage**: `output = (input + sum(flat additions)) × (1 + sum(additive percentage bonuses)) × product(explicit multipliers)`. Percentages are relative to that stage's incoming value after flat additions, not ambiguously to original base stats.
- Two +20% bonuses in the same stage combine to +40%; two explicit ×1.2 multipliers combine to ×1.44. Bonuses from different stages apply in pipeline order. Document intentional exceptions, tier stat replacements and any future override operation rather than relying on collection iteration order.
- Rebuild from original inputs whenever an input changes. Never add bonuses repeatedly to the existing final cache or undo an expired debuff by dividing/subtracting its old effect; this avoids drift and restores the correct value even when another stage changed meanwhile.
- **Recalculate on changes**, not every physics frame: purchase, rank gain, research unlock/reset/load, level initialization/rule change, or effect application/expiry/removal. Research changes invalidate only eligible placed towers; newly built towers receive current research, level conditions and applicable effects immediately.
- Existing combat loops read final effective stats. Purchasing a tier must not reset unrelated active effects, targeting priority or cooldown behavior accidentally. Fields such as price/identity are separate from combat-stat modifiers unless explicitly designed otherwise.
- Preview an upgrade by running the same resolver with the proposed tier and all other current inputs unchanged; the Inspector should show actual final-stat differences and price, rather than a misleading base-only increase.
- Save progression inputs and persistent research, then reconstruct final stats on load. Persisting active timed effects or per-level tower instances is a later save-system decision; research must not live in LevelConfig or reset from StartingCredits.

Examples (illustrative, not balance): a permanent range-research bonus applies on both Level001 and Level002. Level001's −15% range condition disappears when leaving that map. An enemy's −30% fire-rate debuff expires independently; removing it preserves the tower's purchased tier, earned rank, research and current level rules.

### Current code layout — eight implemented stage files

All numbered stage files, context, block, immutable final snapshot and optional trace already exist. The **same** shared pipeline should continue resolving a tower's effective stats; Phase 4 needs type-aware configuration adaptation and may add capability-specific typed fields when an actual second consumer requires them.

```text
TOWERS/
├── Shared/
│   ├── TowerBase.cs                        # EXISTS: current upgrades/final stats
│   ├── Stats/
│   │   ├── TowerStatBlock.cs               # EXISTS: typed numeric inputs
│   │   ├── TowerStatContext.cs             # EXISTS: read-only resolution context
│   │   ├── TowerStatModifier.cs            # EXISTS: stat/operation/amount
│   │   ├── TowerFinalStats.cs               # EXISTS: immutable result
│   │   ├── TowerStatTrace.cs                # EXISTS: optional debug trace
│   │   └── Pipeline/
│   │       ├── TowerStatPipeline.cs         # EXISTS: one explicit coordinator
│   │       ├── 01_BaseStats.cs              # ACTIVE
│   │       ├── 02_TowerUpgrades.cs          # ACTIVE: cumulative purchased tiers
│   │       ├── 03_TowerRanks.cs             # NEUTRAL (future)
│   │       ├── 04_PermanentResearch.cs      # NEUTRAL (future)
│   │       ├── 05_LevelModifiers.cs         # NEUTRAL (future)
│   │       ├── 06_ActiveEffects.cs          # NEUTRAL (future)
│   │       ├── 07_SafetyClamps.cs           # ACTIVE
│   │       └── 08_FinalStats.cs             # ACTIVE
│   ├── Upgrades/
│   │   ├── TowerUpgradeTier.cs             # EXISTS
│   │   ├── TowerUpgradeModifier.cs         # EXISTS
│   │   └── TowerUpgradeController.cs       # EXISTS
│   ├── Targeting/                          # EXISTS
│   ├── Weapons/                            # EXISTS
│   └── Projectiles/                        # EXISTS
├── Offensive/BattleTower/
│   ├── BattleTower.cs                      # EXISTS: cached FinalStats consumer
│   ├── BattleTowerConfig.cs                # EXISTS: config/tiers/copy
│   ├── DefaultBattleTowerConfig.tres
│   └── Upgrades/Level2.tres, Level3.tres
└── Placement/TowerPlacementController.cs   # EXISTS: currently Battle-only

LEVELS/                                    # Future level modifier ownership
PROGRESSION/Research/                      # Proposed possible future research owner
```

**Important:** `SYSTEMS/Profiles/` currently persists local profile **identities**, not research unlocks or full campaign saves. Permanent research state must eventually be owned by the appropriate persistent progression system, independent of level scene lifetime. `TowerStatPipeline.Resolve(BattleTowerConfig, level)` is an adapter at the source edge; `Resolve(TowerStatContext)` is already reusable. Do not re-implement the pipeline or create another resolver per tower type.

#### Explicit pipeline orchestration — one file shows the complete order

`TowerStatPipeline.cs` should be immediately comprehensible when opened: the ordered stages are invoked **explicitly in one place**, without hidden ordering by class discovery, reflection, enum values or file-system iteration.

**Simplified shape of the current implementation:**

```csharp
public static TowerFinalStats Resolve(TowerStatContext context)
{
    TowerStatBlock stats = BaseStatsStage.Apply(context);
    stats = TowerUpgradesStage.Apply(stats, context);
    stats = TowerRanksStage.Apply(stats, context);
    stats = PermanentResearchStage.Apply(stats, context);
    stats = LevelModifiersStage.Apply(stats, context);
    stats = ActiveEffectsStage.Apply(stats, context);
    stats = SafetyClampsStage.Apply(stats, context);
    return FinalStatsStage.Apply(stats, context);
}
```

Each file owns **the rules for applying its stage**, not the source data, level lookup, runtime timers or purchases. A stage operates on typed inputs and returns a new value/snapshot (or updates an isolated mutable working copy) instead of overwriting shared base Resources or last frame's `FinalStats`. `01_BaseStats.cs` is the start of every calculation, **not a permanent value being mutated by later steps**. `08_FinalStats.cs` turns the validated working values into an immutable/read-only final result and applies explicit presentation/gameplay conversion/rounding where required.

**No per-stat files or separate pipelines per tower type.** One typed pipeline may contain only the capabilities present on the tower (e.g. optional weapon, repair, production groups). Don't make a passive Solar or Repair tower carry fictitious damage, projectile or firing fields. If future capability-specific rules genuinely differ, use small typed helpers without duplicating these eight stages. Keep construction prices/ResourceCost, targeting priority, identity and other non-stat metadata separate unless explicitly designed as modifiers.

#### Stage-by-stage source ownership

| File / stage | Data originates from | Lifetime and expected role |
| --- | --- | --- |
| `01_BaseStats.cs` | Tower type's immutable authored base definition / `.tres` (initially existing `BattleTowerConfig`) | Establish original stats for the specific placed instance, without upgrades or bonuses. |
| `02_TowerUpgrades.cs` | `TOWERS/Shared/Upgrades/` and `Offensive/BattleTower/Upgrades/` etc. | Applies the current **purchased** tier for this tower instance only; payment handled elsewhere. |
| `03_TowerRanks.cs` | `TOWERS/Shared/Ranks/` and this tower's attributed kill count / earned rank | Adds kill-based progression independently of purchased tiers; rank milestones and attribution are separate logic. |
| `04_PermanentResearch.cs` | Future **player/profile-owned** `PROGRESSION/Research/` state | Applies eligible permanent unlocks across levels and after reload; never owned or reset by a level, tower or research-center building. |
| `05_LevelModifiers.cs` | **Current** `LEVELS/` level's authored rule set, supplied through level/session context | Applies that map's conditions only; changing maps replaces these inputs, leaving research untouched. |
| `06_ActiveEffects.cs` | `TOWERS/Shared/Modifiers/` active runtime effects, e.g. enemy slow, aura, player ability | Applies stack/refresh/expire rules from the effect owners; no level-specific conditions in this collection. |
| `07_SafetyClamps.cs` | Shared typed stat validity/bound rules | Clamp nonfinite, invalid or out-of-bounds values. Preserve deliberately valid **0 shots/sec = disabled firing**. |
| `08_FinalStats.cs` | The preceding validated working stat block | Publishes the per-instance **effective stats** used by combat/support/economy systems, not authoritative saved inputs. |

`TowerStatContext` should expose the minimum read-only inputs needed to resolve a specific tower (tower definition/base stats, purchased tier, earned rank, eligible persistent research snapshot, active level rules, active effects and relevant capability metadata). Gather these from the proper owners **before** resolution; do not let stages search arbitrary scene nodes or load `Level001` directly. Future research unlocks use the existing/current profile state; new towers must immediately resolve against that state and their active level.

**Distinguish two kinds of modifier concepts:** `TowerStatModifier.cs` is the small reusable **math operation** (target stat, flat/percent/multiplier) shared by stages, while `TOWERS/Shared/Modifiers/` owns the **runtime temporary-effect lifecycle** (source identity, durations, refresh/stack/remove). Level conditions and permanent research can reuse the same math operation without being misclassified as temporary buffs.

#### Debugging and intermediate values — essential for balancing

Offer an **optional, explicitly enabled stage trace** so inspecting one tower can show how each step changed its values. For example, an illustrative Damage calculation could display:

| Snapshot | Damage (example only) |
| --- | ---: |
| Base | 12 |
| Purchased tier | 16 |
| Earned rank | 18 |
| Permanent research | 21.6 |
| Current level modifiers | 21.6 |
| Temporary debuff | 17.28 |
| Safety validation / final rounded value | 17 |

A `TowerStatTrace` can store a **stage-name + copied stat snapshot** after each explicit stage (including unchanged stages). Build/display that trace **only when debugging or inspecting a tower**, not every frame for every tower. Use the **same real pipeline** for the gameplay result and the trace; never maintain competing calculation implementations or falsify the intermediate values. Keep fractional precision during calculations and define rounding **once at the final boundary** for integer gameplay values (e.g. damage); choose and document rounding behavior. Avoid recalculating snapshots when a tooltip simply redraws.

#### Recalculation, caching and cross-system contracts

- **One authoritative `TowerFinalStats` per placed instance**, computed from source inputs and cached. The Battle Tower runtime already reads the cached `FinalStats` for damage, range, fire rate, aiming and projectile speed; do not regress it to direct Config reads. The authored Config remains the source input. Aim speed/turning, repair rate or economy output should follow the same pattern as capabilities are introduced.
- Mark final stats **dirty** on relevant events: tier purchase, individual rank gain, research change/load, level-rule replacement, new effect, expired/removed effect. Recalculate on change or the next safe access, **not on every physics frame**; avoid per-frame allocations or scanning all research/levels for every tower. Research invalidation should target eligible towers when possible.
- The **same resolver** should calculate both the currently applied result and a proposed upgrade preview; for preview only the hypothetical tier changes, while current rank, research, level rules and temporary effects stay identical. Preview must never spend Credits or mutate the placed tower.
- Apply flat/additive-percent/explicit-multiplier operations using the existing Phase 3 arithmetic rules **within each stage**, in the fixed stage order. Identical inputs and seed-free stat calculations must produce identical outputs; never add upgrades to the old final cache or reverse expired debuffs by subtracting/dividing previous results.
- On save/load, restore authoritative progression inputs (purchased tier, earned rank and profile research), then resolve final stats. Do not serialize the derived final result as a substitute for those inputs. Level rules should be **replaced** when switching levels; persistent research must survive. Do not implement the future profile-save system just to implement Battle Tower Phase 3.
- Keep existing `ECONOMY/Wallet/EconomyManager`/`ResourceCost` as the **only** authoritative payment service and existing `TowerPlacementController` as the placement owner. A stat-stage file must never charge resources, attribute kills, schedule timed effects, own a research-center UI, or directly alter enemy navigation.
- Match project style: brief header comments, `// =========================================================` comments above methods, sensible `#region` groups, compact code and README files in newly created folders. Avoid empty services and excessive abstraction; neutral stage methods can remain lightweight until their feature is built.

**Acceptance for this architecture:** a developer can open **`TOWERS/Shared/Stats/Pipeline/TowerStatPipeline.cs` and see the entire actual execution sequence at a glance**, open any numbered file to find that step's implementation, inspect optional stage-by-stage values, and follow each source back to its natural owning feature folder. No other class should contain a competing full copy of the pipeline.

### Phase 3A — Stats foundation (COMPLETED)

This phase added the eight ordered files, typed `TowerStatContext`/`TowerStatBlock`, immutable `TowerFinalStats`, optional `TowerStatTrace`, safe clamping, runtime cache, and event-driven re-resolution on Config changes. `TESTS/TowerStatChecks.cs` covers deterministic stage order, unchanged base values, independent per-tower snapshots, the disabled-firing case and live Inspector updates. It does **not** need to be implemented again.

### Phase 3B — Paid upgrades (COMPLETED)

`TowerBase`, shared upgrade data, transaction control, Inspector preview/button and Stage 02 tier modifiers are implemented. Each placed tower's mutable Config/tier Resources are copied without changing shared `.tres` inputs. Levels start at 1, purchased tiers apply cumulatively, the resolver rebuilds without compounding into old final values, and payments are atomic.

| Battle Tower level | Damage/shot | Shots/sec | Range | Price |
| --- | ---: | ---: | ---: | --- |
| Level 1 | 12 | 1.0 | 256px | Build: 200 Credits |
| Level 2 | 16 | 1.2 | 288px | Upgrade: 150 Credits |
| Level 3 | 22 | 1.5 | 320px | Upgrade: 250 Credits |

These are editable **playtest values**, defined in `Offensive/BattleTower/Upgrades/Level2.tres` and `Level3.tres`; no Alloy cost is currently part of the default Level 3 upgrade. `TESTS/TowerUpgradeChecks.cs` covers payment, secondary-resource cases, tier progression, max level, preview parity, resource isolation, invalid and duplicate requests and Inspector behavior.

| Pipeline stage | Current runtime status |
| --- | --- |
| 01 Base Stats | **ACTIVE** |
| 02 Purchased Tower Upgrades | **ACTIVE** |
| 03 Tower Ranks | **NEUTRAL / FUTURE** |
| 04 Permanent Research | **NEUTRAL / FUTURE** |
| 05 Level Modifiers | **NEUTRAL / FUTURE** |
| 06 Active Effects | **NEUTRAL / FUTURE** |
| 07 Safety Clamps | **ACTIVE** |
| 08 Final Stats | **ACTIVE** |

Do not redo Phase 3A/3B in Phase 4. Maintain stable Source Config values, `TowerUpgradeController`, per-instance `UpgradeLevel`, active Stage 02, `TowerFinalStats`, combat/invalidation caches, wallet events and existing test coverage. Phase 3C+ (ranks, research, level rules, buffs/debuffs) stays separate future work.

## Phase 4 — Scalable tower catalog and shared placement (4A/4B implemented)

**Objective:** Support **30+ different offensive tower types** alongside optional Support/Economy/Utility towers without creating 30 copies of `BattleTower`, the same placement logic or the same hardcoded build-bar UI. **Phase 4A and 4B are implemented and verified; Phase 4C is the next approved design, split into 4C-1 typed combat and 4C-2 a real Laser Tower.** The 4A/4B requirements below record the accepted implementation; do not redo them or construct dozens of placeholder towers.

### 4A — Tower definitions, minimal common contract, generic placement (implemented)

1. **Extend current `TOWERS/Shared/TowerBase.cs` minimally**, preserving `UpgradeLevel`, `NextUpgrade`, `FinalStats`, `StatsChanged`, `ResolveLevel` and the paid-upgrade contract. Add only reusable placed-tower needs: stable **tower-type ID**, grid anchor/footprint (currently 1×1), selection/inspection entry point and an optional type/config accessor. Instance identity can come when saving needs it. **No mandatory `_PhysicsProcess`, weapon, aiming pivot, damage, recoil, projectile node or combat stats for passive towers.**
2. **Introduce `TOWERS/Shared/TowerDefinition.cs`** as an Inspector-editable typed Resource for stable ID, display name, description, category, preview/build icon, `PackedScene`, type-appropriate default Config and declared footprint. **Introduce `TowerCatalog.cs`** as the one authoritative registry for registered/available types, with ID lookup and validation; use authored Resources/a small exported list, not 30 hardcoded switches. Reject blank/duplicate IDs, invalid scene/config references and unsupported footprints early.
3. **Keep canonical balance input ownership.** Today's `BattleTowerConfig.ConstructionCosts` (200 Credits) and `DefaultBattleTowerConfig.tres` remain authoritative for the Battle Tower, and purchased tiers remain separate per-type Resource data. `TowerDefinition` references those inputs instead of independently duplicating their price. Use a small common typed config interface/base or validated adapter where helpful to support multiple types; **never make the generic contract depend on `BattleTowerConfig`** or require every tower to expose `DamagePerShot`/`ShotsPerSecond`. Copy per-instance mutable source/tier/cost data safely, preserving existing Godot `.tres` and Inspector editing. Avoid duplicate base stat or pricing data across catalog/config/HUD.
4. **Refactor `TOWERS/Placement/TowerPlacementController.cs`** to find the selected definition by tower ID and to track `TowerBase` instances, **not** `Dictionary<Vector2I, BattleTower>` and a `BattleTower _selected`. Replace exported `BattleTowerScene`, `BattleConfig`, `BattleArtwork` with catalog-driven inputs while preserving/transitioning existing Main.tscn references safely. Use each type's scene and art for preview; do **not** reject non-Battle scenes because `TurretPivot/Sprite` is missing. Restrict actual placement to 1×1 until correct multi-cell grid, occupancy and navigation validation is separately implemented.
5. **Preserve atomic placement and connectivity:** `WorldGrid` validates terrain/occupancy/reserved cells; `SpawnManager` rejects a tower that seals all valid routes; `EconomyManager.TryPurchase` alone charges costs, after a successful callback. Roll back nodes, occupancy and selections on any failure. No failed or repeated placement charges money; no parallel wallet, occupancy cache or pathfinder.
6. **Preserve upgrade functionality for generic towers.** Placed instances retain per-instance Config, purchased levels, next-tier data, real `ResolveLevel` previews, `StatsChanged` event subscriptions and `TowerUpgradeController.TryUpgrade` transactions. The Inspector must be fed by the **selected actual tower**, never `BattleConfig` on the controller. A type with no upgrades displays a safe disabled/max/no-upgrades state, not a broken button. Keep preview and successful payment calculations identical.

**Phase 4A acceptance:** the existing Battle Tower places, selects, targets, fires, damages enemies, buys levels 2/3 at the same prices and uses the same stat pipeline. A minimal **second test tower definition and scene** (even a non-firing/passive one) can be registered, paid for, placed and selected **without editing controller per-type logic**. No forced turret scene nodes, weapon ticks or nonsensical weapon stats. Test invalid catalog entries, rollback, different costs, independent upgrade state and navigation route sealing.

### 4B — Dynamic build bar and generic Inspector (implemented)

- **Replace `GameplayHUD.SampleTowers`**, six hardcoded prototype button IDs, and duplicated stat descriptions with catalog-backed buttons. The catalog determines visible playable entries, name, art, category, build costs and authorable default config. Don't display nonfunctional Laser/Missile/Repair/etc. as though usable.
- **Design for arbitrary registered tower count** with scrollable/wrapping/grouped build entries (future category filters: Offensive / Support / Economy / Utility). Read metadata to populate UI, not by instantiating all tower scenes. Keep the current Ctrl toggle, close/selection behavior, keyboard focus and Inspector positioning.
- Use existing `UI/Shared/UIButtonFactory` for generated tower buttons, wave/upgrade/close buttons. Preserve the existing `TowerChoiceRequested(towerId)` and `TowerUpgradeRequested` signals or deliberately migrate with regression tests. The HUD emits user actions; the catalog provides data; the placement controller decides whether placement/upgrade succeeds.
- The Inspector shows the **actual selected tower's** identity, config, `FinalStats`, level, available next tier, real change preview and cost. Do not duplicate price/math. Show range rings **only if the tower has that capability**; passive support/production towers shouldn't require an attack radius.

**Phase 4B acceptance:** registering a new valid type makes a build-bar entry appear without a new hardcoded name/button/branch. Clicking it uses that type's scene, construction rules, icon and Inspector. Single Battle Tower gameplay and its Upgrade button remain unchanged. Test many catalog entries, disabled/unimplemented entries, save-free direct `Main.tscn` testing and the F5 Boot→Main→Campaign→Level001 flow.

### Phase 4C — Typed damage and the first continuous-beam weapon (implemented)

**Implement in strict order: 4C-1 first, verify it independently, then 4C-2.** Phase 4A/4B catalog/placement/UI, Phase 3A/3B stats/upgrades, and the opt-in enemy `MaxShield`/`CurrentShield` groundwork already exist. Do not create a new tower registry, wallet, targeting service, replacement stat pipeline, or all-purpose weapon superclass.

**Core architectural rule:** `TowerBase` is *tower identity, instance progression, placement and optional inspection*, not a damage type or mandatory weapon. The **weapon/config** supplies the damage type, the **shared combat resolver** interprets that type against the victim's shield/hull, and the **beam renderer** draws only visuals. A future hybrid tower may have a kinetic gun and an energy beam with different damage types. `Offensive` is a tower category; `Kinetic/Energy/Explosive` are damage types; `Projectile/Beam/Explosion` are delivery mechanisms—keep those three concepts independent.

#### 4C-1 — Shared typed damage and shield/hull response (implemented and independently verified first)

**Goal:** Introduce typed hits while preserving every currently working projectile/health behavior and the just-implemented opt-in shield spawn feature. Put shared attack/defense math outside `TOWERS/`, because enemy attacks and other sources may eventually use it.

**Implemented shared folder:**

```text
COMBAT/
├── README.md
└── Damage/
    ├── DamageType.cs                 # Kinetic, Energy, Explosive (extensible enum)
    ├── DamageInfo.cs                 # Value-only payload: BASE amount + DamageType
    ├── DamageResolver.cs             # Single authoritative shield/hull calculation
    ├── DamageResponseProfile.cs      # Godot Resource with editable per-type multipliers
    ├── DefaultDamageProfile.tres      # Shared authored default balance
    └── README.md

ENEMIES/Shared/
├── Enemy.cs                          # Adds typed entry point, keeps TakeDamage(float)
└── EnemyDefinition.cs                # MaxShield already exists; optional defenses LATER

TOWERS/Shared/Projectiles/
└── BasicProjectile.cs                # Supplies typed Kinetic damage on contact
```

**Data ownership and APIs:**

1. Use a small `DamageType` enum (`Kinetic`, `Energy`, `Explosive`) and an immutable/readonly `DamageInfo` containing a **finite, nonnegative *base* amount**, its type, and **no preapplied defense multiplier**. An optional source/attacker can be added only if needed later for kill attribution. Avoid effects, critical hits, armor penetration, damage-over-time, tags or status engines in this pass.
2. `DamageResponseProfile` must expose **separate shield and hull multipliers per damage type** as Inspector-editable `[ExportGroup]` fields. One canonical `DefaultDamageProfile.tres` supplies the initial table. Suggested provisional balance (tune in Inspector; not final):

   | Damage type | Shield efficiency | Hull efficiency | Example weapon |
   | --- | ---: | ---: | --- |
   | Kinetic | **0.50×** | **1.00×** | Battle Tower bullet |
   | Energy | **2.00×** | **0.85×** | Laser beam |
   | Explosive | **0.25×** | **1.25×** | Future Missile/Mortar |

   The resolver loads/references the profile **once**, not on every hit. Provide validation for negative/nonfinite multipliers and unknown damage types. An enemy-specific resistance/weakness override can be added **later** as an optional profile reference in `EnemyDefinition`; do not duplicate multipliers onto every enemy type now.
3. `DamageResolver` is one small, deterministic calculation for **a single hit**. Take base damage `D`, current shield `S`, shield multiplier `Ms` and hull multiplier `Mh`. With shields present and `Ms > 0`: spend `min(D, S / Ms)` **base damage** against shields; subtract that consumed base amount × `Ms` from shields; apply the **remaining base damage × `Mh`** to hull. Without shield, apply `D × Mh` to hull. Clamp state at zero, preserve fractional values until used, and avoid division by zero. For this first pass, **`Ms = 0` means existing shields block that type without taking damage; it does not implicitly bypass shields**. Explicit shield-bypass would be a separate future trait.
4. **Critical spillover example:** Energy `D = 20`, `Ms = 2`, `Mh = 0.85`, `S = 30` uses 15 **base** damage to remove the 30 shield points and sends the remaining 5 base damage × 0.85 = **4.25 hull damage**. Do **not** transfer 10 excess *amplified shield damage* to hull or apply the hull multiplier to the already amplified shield amount. When `S = 100`, this same hit subtracts 40 shield points and 0 hull.
5. Add a typed `Enemy.ApplyDamage(DamageInfo)` (naming flexible) that delegates math to the resolver, changes **only that enemy instance's** `CurrentShield` and `Health`, updates the existing hull `DefenseBar` on a hull hit and triggers `Finish(false)` only on hull death. The previously implemented `Enemy.TakeDamage(float)` **must remain** for tests/debug/legacy callers and preserve its original **1:1 shield-first, spillover-to-hull** semantics (no hidden default energy/kinetic multiplier). Funnel shared lifecycle/state updating through one internal helper if useful, but do not double-apply resistance.
6. Update **`BasicProjectile`** to deliver an explicitly **Kinetic typed** hit when it contacts an enemy, with the existing `BattleTower` default damage value unchanged. This intentionally lowers its damage **to shields** compared with the earlier untyped projectile, but its **unshielded hull damage, hit timing, lifetime, effects and kill payouts must remain identical**. Keep projectile delivery and damage calculation decoupled so a later projectile weapon can specify Energy/Explosive without duplicating the resolver.
7. Shield capacity/starting state remain as shipped: `EnemyDefinition.MaxShield` (Enemy001 test value **100**) is capacity only; `CurrentShield = 0` on F2 and ordinary waves; **only** `WaveBuilder.Enemy(..., shield: true)` enables a full starting shield. No automatic recharge, blue defense bar, typed incoming Citadel damage, active `Armor`, boss modifiers or save system.

**Tests and independent 4C-1 checkpoint:**

- Add focused `TESTS/DamageTypeChecks.cs` (name flexible) and integrate it into the headless suite. Cover 0/partial/full shield, shield-break exact boundary, amplified shield-only damage, shield-to-hull spillover (including the 20/30/4.25 example), low-efficiency Explosive vs shield, unshielded hull multipliers, zero multiplier/no implicit bypass, invalid/zero amounts, and no negative/nonfinite health/shield values.
- Verify typed damage changes **only the target instance**; repeated damage on killed/arrived ships produces no rewards/events; a shield-only hit leaves the existing hull bar unchanged. Legacy `TakeDamage(float)` remains 1:1.
- Verify the live Battle Tower still fires guided projectiles, costs **200 Credits**, deals **12** base damage to unshielded Enemy001 at level 1, supports levels 2/3 (150/250 Credits), and retains targeting, rotation, 3-Credit kill reward, Citadel arrival damage, the 100-shield opt-in wave and F2 behavior. Preserve exact route-safe placement, wave scheduling and all existing regression tests.
- **Stop after 4C-1 and confirm compilation and headless tests before implementing Laser Tower.** Update `COMBAT/Damage/README.md`, `ENEMIES/Shared/README.md`, projectile README and `TESTS/README.md` to describe actual behavior.

#### 4C-2 — Laser Tower with sustained Energy beam and separate visual component (implemented after 4C-1 verification)

**Goal:** Add a **real, catalog-registered Laser Tower** that holds a visible beam onto its target and deals **Energy damage per second**; it is *not* a fast invisible projectile, repeated hitscan spawn, series of discrete shots, or special-case inside `BattleTower.cs`. It should demonstrate the second actual offensive weapon family and be easily restyled in the Inspector without editing its combat logic.

**Implemented files/responsibilities (the following contract remains authoritative):**

```text
TOWERS/
├── Shared/
│   ├── Weapons/
│   │   ├── TurretAiming.cs                # EXISTS: reuse if Laser has tracking pivot
│   │   └── Beams/
│   │       ├── BeamRenderer.cs            # NEW: visual endpoints/active state only
│   │       ├── BeamRenderer.tscn           # NEW: reusable Line2D/CanvasItem presentation
│   │       ├── BeamStyle.cs               # NEW: typed visual style Resource
│   │       └── README.md
│   └── Stats/                              # EXISTS: keep same 01–08 resolver
└── Offensive/
    └── LaserTower/
        ├── LaserTower.cs                   # NEW: targeting/tracking + sustained DPS
        ├── LaserTowerConfig.cs             # NEW: canonical typed weapon stats/costs
        ├── LaserTower.tscn                  # NEW: TowerBase-derived root, optional pivot/muzzle
        ├── LaserTowerDefinition.tres        # NEW: TowerCatalog registration
        ├── DefaultLaserTowerConfig.tres     # NEW: editable type config
        ├── Beam/
        │   └── DefaultLaserBeamStyle.tres   # NEW: tower-specific visual tuning
        ├── Artwork/                         # NEW: simple placeholder SVG
        └── README.md

TOWERS/DefaultTowerCatalog.tres              # Add the real Laser definition
```

**Laser runtime contract:**

1. `LaserTower : TowerBase` implements the existing typed `TowerConfig`/definition/copy/upgrade/selection/Inspector contract. Register stable ID `laser` via `LaserTowerDefinition.tres` and **add it to `DefaultTowerCatalog.tres`**. The existing generic HUD and placement controller must display/build/select it **without any new per-type branches**. Use the same `ResourceCost`/`EconomyManager.TryPurchase` flow and route-safe 1×1 occupancy; leave its initial build cost/balance Inspector-editable.
2. Author `LaserTowerConfig : TowerConfig` with **RangePixels**, **DamagePerSecond** (e.g. 20 base DPS for playtesting), `DamageType = Energy`, target priority/periodic search interval, and optional rotation/aim tolerance/rotate-while-tracking if the tower has a pivot. **Damage type belongs to this weapon config, not `TowerBase`**; hybrids may carry different types in different weapon modules. Never hardcode the global Energy damage multipliers in Laser config. Reuse existing `TowerTargeting` and `TurretAiming`, including staggered search and cheap per-frame cached-target checks. Don't search all enemies on every physics tick.
3. **Sustained lock:** with a living target in range and turret aligned (if aiming enabled), activate a single beam and damage that target using `effectiveDamagePerSecond × physicsDelta` as **base Energy damage** passed to the new typed damage API. Continuous visuals follow tower/muzzle and target each active frame. Damage stops immediately on loss of alignment, range, valid target, disabling, scene exit or enemy removal/death. Reacquire on the existing periodic schedule; no aim snapping, firing cooldown, bullet nodes or new beam scene per tick. Choose a clear first-version retarget policy and document it.
4. **Separate visual ownership:** `BeamRenderer` owns drawing and visual state **only**: given world-space origin/muzzle, target world endpoint and enabled flag, it renders the beam. It must **not** acquire targets, know what an Enemy is, call damage APIs or change DPS. `BeamStyle.tres` provides Inspector-exported core color, outer/glow color, widths and optional restrained pulse/flicker; a future shader/particles/impact sprite may be swapped in here **without changing target selection, attack damage or upgrade math**. Avoid forcing bloom/shaders into the first gameplay pass or promising them without a 2D render setup.
5. Use one persistent renderer per Laser Tower, with no allocations or new `Line2D`s each physics frame. The tower drives `SetBeam(fromWorld, toWorld, active)` (conceptual API), and the renderer converts coordinates as needed. On target death/unload/switch, clear stale endpoints and hide the beam immediately. A `Marker2D` muzzle is desirable; center emission is an acceptable initial fallback, but visuals must never rotate the grid/placement root.
6. **Stats and upgrades:** Laser Tower requires a genuine **float DamagePerSecond** effective stat, not simulated `DamagePerShot × ShotsPerSecond`. Extend the existing `TowerStatContext` / `TowerStatBlock` / `TowerFinalStats` with an **optional typed beam capability/stat field** and add `TowerStatId.BeamDps` (or a clear equivalent) to the shared modifier system; have **the same eight numbered stages** propagate it, Stage 02 apply tower-specific tier operations, Stage 07 clamp nonfinite/negative DPS, and Stage 08 publish a cached final result. Keep the existing `BattleTowerConfig` adapter, old stat fields, source constructors and all current Battle tiers/tests working. Do not create a parallel Laser stat pipeline or require a passive tower to claim projectile/fire-rate fields. The Laser Inspector must display *effective* Range/DPS, price, purchased level and next-tier preview from the shared resolver; any authored Laser upgrade tiers must preview exactly what purchase applies. If a minimal initial Laser has no tiers, safely show no available upgrade rather than fake values; ideally author one modest playable DPS/range upgrade to prove capability-specific Stage 02 integration.
7. Keep the **attack math deterministic across tick rates**: 20 base DPS produces about 20 base damage over 1 second of valid lock at 30/60/120 physics ticks (with normal float tolerance); against 100 shield and the default Energy 2× coefficient that removes about **40 shield points**, and no hull damage until shield depletion. If a tick breaks the shield, correctly convert the remaining **base** damage to hull at its own multiplier. A beam cannot apply damage to a different target simply because the old target was freed during this tick.
8. Targeting and visuals should be separate from shared `DamageResolver`: the Laser asks *how much base Energy damage to send*, the resolver decides shield/hull losses, and the beam renderer decides *how the attack looks*. Shield absorption is an enemy-defense concern, not a per-laser special case.

**Tests and 4C-2 checkpoint:**

- Test catalog registration, type identity/scene/config validity, actual Laser build from the dynamic bar, paid construction, range, selection, no duplicate grid occupancy and unchanged Battle Tower build/upgrade behavior.
- Test first/closest target acquisition, lock/alignment gating, damage starts/stops at the right time, target death/out-of-range/switch/unload cleanup, and no damage/visible beam without a valid target. Confirm no per-tick projectile/node creation and no persistent beam left when laser is idle.
- Run 30/60/120 tick-rate DPS parity and energy shield-vs-hull tests; verify fractional spillover, no repeated kill credits, no duplicate Citadel damage, no changes to WaveManager's opt-in shield behavior.
- Validate the visual Resource independently: changing beam width/core/glow/pulse in `DefaultLaserBeamStyle.tres` changes its rendered presentation **without altering DPS, targeting, paid upgrades, grid placement or enemy health calculations**. Restore/avoid mutating authored shared Resources in tests. Manually confirm the beam remains anchored to its muzzle and moving target in the live Godot window—headless checks cannot prove visual quality.
- Verify Phase 3A/3B regression tests, generic 4A/4B test-only passive tower, 40-entry build-bar tests, menu flow, economy, navigation, waves, shield tests, and `Main.tscn` direct play. Avoid rewriting working foundation classes more than absolutely necessary.

**Do not implement yet:** Missile Tower, splash/area damage, damage-type-driven critical effects, enemy-specific resistance overrides, shield regeneration/blue UI, armor mitigation, line-of-sight blocking (current guided bullets pass through walls), laser heat/overheating, charged beams, laser sound/VFX polish, research/ranks/active effects, save persistence or extra tower families. They can follow after the initial Laser Tower proves the system.

**Deliverables when implemented:** add short C# header comments, `// =========================================================` method separators, appropriate `#region`s and Inspector ExportGroups, plus accurate README files for `COMBAT/`, `COMBAT/Damage/`, `TOWERS/Shared/Weapons/Beams/`, `TOWERS/Offensive/LaserTower/`, existing enemy/projectile/stats docs and `TESTS/`. Report actual Godot .NET build/headless/manual results, not assumed passes.

**4C finish definition:** Battle Tower still operates as before; a registered Laser Tower draws one restylable continuous beam and deals typed Energy DPS through one global shield/hull resolver; the same eight-stage stat pipeline and shared HUD/upgrade/economy/placement infrastructure service both tower families without Laser/Battle special cases in generic placement.

### Stats pipeline compatibility during Phase 4

- **Do not rewrite the eight numerical stages**: `TowerStatPipeline.Resolve(TowerStatContext)` already supports typed generic resolution. Its `BattleTowerConfig` overload is the adapter to evolve when future types need new inputs; the existing Battle Tower numbers and Level2/Level3 tier arithmetic must remain identical.
- Use a shared authored **type-aware stat input adapter/context** rather than introducing `if (towerId == ...)` branches inside `TowerStatPipeline`. Add optional capability-specific stat groups when needed by a second real tower (e.g. repair/production output), not artificial zero-damage/fire-rate values on every passive tower. Retain explicit numbered order, finite clamps, correct one-time integer rounding and per-instance cache invalidation.
- Construction costs, metadata, targeting priority and tracking policy remain outside combat numeric modifiers unless a deliberate later capability introduces them. Resolve both live and next-level previews through the same pipeline. **Save source inputs** (type ID, grid anchor, purchased level, future rank/research), never the derived `TowerFinalStats`.

### Performance, tests and scope

- **30 types ≠ 30 active towers.** Profile actual *placed instances*, target searches, projectiles and active effects. Cache scene/service refs in `_Ready()`, stagger target searches, validate cached targets cheaply, resolve stats on invalidation (not per physics tick), and disable physics for passive towers. Avoid rebuilding the catalog/list or scanning every enemy for every tower each frame. Pool effects only if measured.
- Run and preserve `TESTS/EnemyIntegrationChecks`, `HudTowerChecks`, `EconomyChecks`, `TowerStatChecks`, `TowerUpgradeChecks` and terrain/navigation/wave regression checks. Add catalog ID/asset tests, a non-Battle passive test type, multi-type placement/selection/upgrade tests, failed purchase rollback tests and dynamic UI entry checks. Verify the live Battle baseline numbers and the Phase 3B upgrade payments/Inspector parity.
- Keep `Main.tscn`, `World/Towers`, existing terrain layers, waves, existing Citadel/portal reservation and navigation, `ECONOMY/` wallet and profile/menu startup working. Do not move unrelated modules or create a generic engine larger than needed. Use compact C#, Inspector `[ExportGroup]` Resources, explicit contracts and feature README updates. **Do not claim local Godot runtime tests passed without actually running them.**
- **4A/4B and 4C-1/4C-2 are implemented and regression-tested.** 4C-1 compiled and passed the headless suite before Laser work started. Ranks, permanent research, level effects, selling and tower HP remain future passes.

**Finish condition:** adding a new tower should require authoring its definition/config/scene/art and registration—**no new tower-specific case in placement or HUD**. Do not implement 30 complete towers as part of this foundation pass.

## Phase 4C-3 completed combat pass

**Implemented:** See [`COMBAT/Damage/README.md`](../COMBAT/Damage/README.md) for the authoritative4C-3 math and authoring contract. DamageInfo supports independent absolute shield/hull overrides and0..1 penetration. Laser now uses 12 base DPS with effective 6.00x shield/0.15x hull responses, giving72 shield/s or1.8 exposed hull/s. Its175-Credit tier adds10 base DPS/32px, yielding22 DPS/288px. Global Energy remains2.00/0.85.

Battle starts with 5% base shield penetration, visible in the placed Inspector at every tier and carried through the existing cached eight-stage pipeline into projectile payloads. Level2/Level3 still improve damage/rate/range without adding penetration. The default config's missing tier references were restored. New hits default to zero penetration; optional weapon overrides do not impose combat behavior on passive towers.

Stage 04 remains neutral. Future permanent ballistic research and saving are deferred, alongside Missile Tower, ranks, level modifiers, active effects, selling and tower HP. Visual beam polish still requires a live-window review. See TESTS/README.md for4C-3 validation results.

## Phase 4D completed — shared instant AoE + Blaster Cannon

`COMBAT/AreaDamage/AreaDamage.cs` performs one mutation-safe registry scan per impact, scales base DamageInfo by weapon-supplied linear distance falloff, and forwards each eligible center hit once through the unchanged defense resolver. Inclusive edge and zero-radius exact-center behavior are documented in [`COMBAT/AreaDamage/README.md`](../COMBAT/AreaDamage/README.md). There is no shared hardcoded falloff, direct-plus-splash duplicate or per-frame area scan.

[`Offensive/BlasterCannon/README.md`](Offensive/BlasterCannon/README.md) describes the registered `blaster_cannon`: 300 Credits, Explosive 60 base damage, 80 px radius, 25% edge retention, 0.5 shots/sec, 500 px/sec projectile, 256 px range and zero penetration. Its 200-Credit Level 2 adds 15 base damage and 16 px radius; edge retention stays 25%. Optional immutable AreaBlastStats flows through the existing eight stages, typed upgrade IDs, safety clamps, cached selected Inspector and real next-tier previews. Stage 04 remains neutral for future falloff research.

SplashProjectile snapshots damage/type/defense overrides/penetration/radius/edge at launch and detonates once at the target or last-known point, even when the target disappears or leaves firing range. BlastFlash under FEEDBACK/AreaBlast is purely visual. No generic placement/HUD changes or Battle/BasicProjectile behavior changes are required. Tests and actual validation are recorded in TESTS/README.md.

[`Shared/Weapons/Shockwaves/README.md`](Shared/Weapons/Shockwaves/README.md) remains the future Phase 4E contract: damage occurs only when an advancing wavefront crosses an enemy. No shockwave runtime, permanent research, saving, Missile Tower or armour is implemented in 4D.

## Current live Inspector files

- `Offensive/BattleTower/DefaultBattleTowerConfig.tres`: name/description, ConstructionCosts (200 Credits), aiming, range, shots/sec, damage, projectile speed, target priority and target search interval.
- `Offensive/BlasterCannon/DefaultBlasterCannonConfig.tres`: 300-Credit recipe, Explosive base hit, area radius/edge fraction, penetration, response overrides and aiming; Upgrades/Level2.tres owns the 200-Credit tier.
- `DefaultTowerConfig.tres`: shared placement/selection preview flags; `AllowUpgrades` is **active**, while selling/refund settings remain future work.
- `FEEDBACK/DefenseBars/DefaultDefenseBarConfig.tres`: reusable health bar appearance when towers become damageable.

**Current status:** rotation (Phase 1), paid construction (Phase 2), stat foundation (3A), paid upgrades (3B), generic catalog/placement (4A) and dynamic build UI (4B) work. Ranks, research, level conditions and timed effects remain planned.


## Implemented Phase 3A verification
All eight stage files and the single coordinator exist. TowerFinalStats is a separate immutable instance snapshot; source Config remains Inspector-editable and numeric changes automatically rebuild. Resource replacement/exit remove old subscriptions. StatsChanged refreshes selected Inspector values/range rings; combat ticks consume cached results. Safety bounds and optional traces are documented in Shared/Stats/README.md. TowerStatChecks validates stage order, baseline parity, clamps, cache/input isolation, per-tower independence, live tuning and disabled/re-enabled firing. That statement describes the earlier 3A-only milestone; tier purchasing and the Upgrade button were subsequently implemented in Phase 3B. Rank/research/condition/effect gameplay remains future work.


## Implemented Phase 3B verification
BattleTower uses shared TowerBase UpgradeLevel (starting at 1) and the existing eight-stage resolver. Editable Level2/Level3 tiers cost 150/250 Credits and produce 16/22 damage, 1.2/1.5 shots/sec and 288/320px range at current base balance. These are playtest values, not finalized balance. Tier/modifier/cost Resources are explicitly copied per instance; base Config remains unchanged.

The selected Inspector displays current level, actual next-tier stat differences, price, affordability and max-tier status. Purchases use EconomyManager.TryPurchase, do not change navigation/occupancy or reset cooldown/aim, and notify UI after payment commits. Build previews/prototype towers have no upgrade action. The larger Inspector grows upward while the Ctrl tray retains its configured height.

TowerUpgradeChecks covers paid HUD actions, stale/same-frame duplicate protection, global AllowUpgrades, insufficient/secondary-resource costs, max tier, source isolation, preview parity, deterministic stage arithmetic, no compounding and layout/independent panels. No rank, research, level/effect gameplay, selling or visual tower upgrade artwork was added.

## Implemented Phase 4A/4B verification and authoring

`DefaultTowerCatalog.tres` references BattleTowerDefinition, LaserTowerDefinition and BlasterCannonDefinition. Add a tower by authoring a compatible `TowerBase` scene/script, typed `TowerConfig`, icon and `TowerDefinition` (ID/category/scene/config/icon/1×1 footprint), then adding the definition to the catalog's Definitions array. Expand DefaultConfig to edit its name, description, construction costs and tiers. Definitions do not independently override prices or names. Rebuild the catalog and rebind the HUD after runtime registration/availability edits; ordinary frames do neither. Available=false hides the build entry and rejects placement; asset/config validation still applies to registered entries. Empty catalogs display a safe message.

Passive scenes need no pivot, projectile node, weapon stats, range ring or processing. `TowerConfig.ResolveStats`/`DescribeStats`/`DescribeChanges` are type-aware adapters; Battle keeps the unchanged eight numerical stages and real upgrade preview. Future passive progression with actual capability stats can supply its own typed data when that consumer exists; the passive fixture deliberately has no upgrades or combat snapshot.

At the 4A checkpoint the real Godot headless integration scene passed catalog/mixed-type tests and all existing combat, stat, upgrade, economy, terrain, wave, feedback and navigation checks. After 4B it passed again, with 40 registered variants testing availability, canonical prices/icons, generated ID actions, focus, scrolling, rebinding and independent panels. `MenuTowerFlowChecks.tscn` separately exercises actual Boot profile-button → Main → Campaign → Level001 buttons, then paid Battle placement/upgrade using an injected memory-only profile (no save writes). The suite includes the existing 500-ship fixture; headless timings are not a rendered-FPS or visual-layout guarantee.

## Historical Phase 4C-1/4C-2 verification (superseded balance)

4C-1 was committed/verified separately before any Laser code: direct C# compilation with Godot source generators and the actual Godot 4.7.2 headless integration scene passed, including DamageTypeChecks, opt-in shield waves and all old Battle/navigation/economy tests. Stage4C-2 then passed the full suite with LaserTowerChecks and the 500-ship fixture. Default Laser price 250 Credits, base20 DPS/256px; its175-Credit level 2 tier resolves 30 DPS/288px.20 Energy base DPS removes 40 shield or17 unshielded hull per second, within float tolerance. Battle's200 construction,150/250 upgrades,12 base hull hit, guided delivery and kill rewards remain unchanged.

The environment's ordinary editor SDK/MSBuild build could not run (missing/incomplete .NET SDK/MSBuild setup). Compilation was performed successfully using Roslyn with the actual Godot API/source generator, then the compiled assembly was exercised by Godot headless. Manual live-window rendered beam quality/moving-target visual review is not claimed; automated endpoint geometry, style independence, cleanup and timing are covered.
