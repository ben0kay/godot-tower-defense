# TOWERS — All Player-Placeable Towers

Umbrella folder for **every player-buildable tower**, not just weapons: offensive defenses, support, economy/production and utility. The UI can call all of them *towers*. A tower's primary role determines its folder, **not** its only possible capability: for example, `Economy/SolarTower/` may eventually gain a weak defensive laser upgrade.

## Current implementation (October 2026)

**Implemented today:** 1×1 Battle Tower placement/selection; terrain and connectivity validation; First/Closest target search, turret rotation, guided projectiles and enemy damage; atomic 200-Credit construction; per-instance Config copies; eight-stage cached stats (Phase 3A); and **Phase 3B paid upgrades**. Battle Tower upgrades level 1→2 (150 Credits) and 2→3 (250 Credits), with the selected Inspector showing real next-tier stat differences, cost and an Upgrade button. `TowerBase`, `Shared/Upgrades/`, `Shared/Stats/` and `TowerUpgradeChecks` exist.

**Phase 4A/4B implemented:** `TowerConfig`, `TowerDefinition`, `TowerCatalog` and `TowerPresentation` are the shared foundation. `TowerBase` additionally owns stable type identity, definition, grid anchor/footprint, config compatibility/application, placement/selection hooks and optional range inspection. Placement indexes `TowerBase` and resolves catalog IDs without Battle-specific branches or scene-node assumptions. The HUD creates available catalog entries in a horizontal scrolling tray, using `UIButtonFactory`. Construction identity/prices stay on the canonical Config, referenced by definitions; no second price or stat list exists.

**Default playable roster:** Battle Tower only. The passive scene under `TESTS/Towers/` is a regression fixture, not a promised Repair/Solar tower or a default menu entry. Registering a valid additional definition creates its build choice without editing placement or HUD. Only 1×1 footprints are supported; other footprints fail registration.

**Still local/future:** Battle Tower keeps its existing targeting/cooldown/guided-projectile loop. **Phase 4C weapon-family extraction is not implemented.** Stage 02 upgrades remains active; ranks/research/level modifiers/effects remain neutral. New weapon families, selling, tower health and multi-cell placement are later work.

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
| `Offensive/LaserTower/`, `Offensive/MissileTower/` | PLANNED | Future beam/explosive example towers, not active |
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

**Objective:** Support **30+ different offensive tower types** alongside optional Support/Economy/Utility towers without creating 30 copies of `BattleTower`, the same placement logic or the same hardcoded build-bar UI. **Phase 4A was implemented and verified before Phase 4B; Phase 4C remains a later distinct weapon pass.** The numbered requirements below record the accepted design. First migrate and verify the existing single working Battle Tower. Add a lightweight second *test* tower to prove extensibility; do not build 30 gameplay towers.

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

### 4C — Later weapon families and incremental extraction (NOT necessary in 4A/4B)

**Only after a second genuine offensive tower exists** (Laser beam/hitscan is a good contrast; Missile/Mortar explosive is another): extract optional shared weapon code that has at least two consumers. Candidate pieces currently inside `BattleTower.cs`: staggered target acquisition/cache checks, fire cooldown and optional delivery/impact interfaces. `TowerTargeting` and `TurretAiming` are already shared; reuse them, don't reimplement them.

Model weapon behavior as optional acquisition, aiming, cadence, delivery, impact/status and visuals. Keep guided bullet details within a projectile family and beam/missile logic in their own modules. Don't build a giant base-class weapon switch or implement all 30 concepts before proving two families. Non-weapon towers must never pay for unnecessary combat processing.

### Stats pipeline compatibility during Phase 4

- **Do not rewrite the eight numerical stages**: `TowerStatPipeline.Resolve(TowerStatContext)` already supports typed generic resolution. Its `BattleTowerConfig` overload is the adapter to evolve when future types need new inputs; the existing Battle Tower numbers and Level2/Level3 tier arithmetic must remain identical.
- Use a shared authored **type-aware stat input adapter/context** rather than introducing `if (towerId == ...)` branches inside `TowerStatPipeline`. Add optional capability-specific stat groups when needed by a second real tower (e.g. repair/production output), not artificial zero-damage/fire-rate values on every passive tower. Retain explicit numbered order, finite clamps, correct one-time integer rounding and per-instance cache invalidation.
- Construction costs, metadata, targeting priority and tracking policy remain outside combat numeric modifiers unless a deliberate later capability introduces them. Resolve both live and next-level previews through the same pipeline. **Save source inputs** (type ID, grid anchor, purchased level, future rank/research), never the derived `TowerFinalStats`.

### Performance, tests and scope

- **30 types ≠ 30 active towers.** Profile actual *placed instances*, target searches, projectiles and active effects. Cache scene/service refs in `_Ready()`, stagger target searches, validate cached targets cheaply, resolve stats on invalidation (not per physics tick), and disable physics for passive towers. Avoid rebuilding the catalog/list or scanning every enemy for every tower each frame. Pool effects only if measured.
- Run and preserve `TESTS/EnemyIntegrationChecks`, `HudTowerChecks`, `EconomyChecks`, `TowerStatChecks`, `TowerUpgradeChecks` and terrain/navigation/wave regression checks. Add catalog ID/asset tests, a non-Battle passive test type, multi-type placement/selection/upgrade tests, failed purchase rollback tests and dynamic UI entry checks. Verify the live Battle baseline numbers and the Phase 3B upgrade payments/Inspector parity.
- Keep `Main.tscn`, `World/Towers`, existing terrain layers, waves, existing Citadel/portal reservation and navigation, `ECONOMY/` wallet and profile/menu startup working. Do not move unrelated modules or create a generic engine larger than needed. Use compact C#, Inspector `[ExportGroup]` Resources, explicit contracts and feature README updates. **Do not claim local Godot runtime tests passed without actually running them.**
- Codex should implement **4A then 4B**, verifying each checkpoint, and update `Shared/README.md`, `Placement/README.md`, `Offensive/BattleTower/README.md` and relevant `UI/GameplayHUD` docs **after implementation**, not before. **4C and ranked/research/effect stages are separate future work.**

**Finish condition:** adding a new tower should require authoring its definition/config/scene/art and registration—**no new tower-specific case in placement or HUD**. Do not implement 30 complete towers as part of this foundation pass.

## Next implementation pass

**Phase 4A+4B are complete.** Next, author a second actual offensive type before extracting shared weapon-family behavior in Phase 4C. The entire game should remain playable with only Battle Tower registered until a second real tower is ready. Ranks, profile research, level modifiers, active effects, selling and tower HP remain future phases.

## Current live Inspector files

- `Offensive/BattleTower/DefaultBattleTowerConfig.tres`: name/description, ConstructionCosts (200 Credits), aiming, range, shots/sec, damage, projectile speed, target priority and target search interval.
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

`DefaultTowerCatalog.tres` references `Offensive/BattleTower/BattleTowerDefinition.tres`. Add a tower by authoring a compatible `TowerBase` scene/script, typed `TowerConfig`, icon and `TowerDefinition` (ID/category/scene/config/icon/1×1 footprint), then adding the definition to the catalog's Definitions array. Expand DefaultConfig to edit its name, description, construction costs and tiers. Definitions do not independently override prices or names. Rebuild the catalog and rebind the HUD after runtime registration/availability edits; ordinary frames do neither. Available=false hides the build entry and rejects placement; asset/config validation still applies to registered entries. Empty catalogs display a safe message.

Passive scenes need no pivot, projectile node, weapon stats, range ring or processing. `TowerConfig.ResolveStats`/`DescribeStats`/`DescribeChanges` are type-aware adapters; Battle keeps the unchanged eight numerical stages and real upgrade preview. Future passive progression with actual capability stats can supply its own typed data when that consumer exists; the passive fixture deliberately has no upgrades or combat snapshot.

At the 4A checkpoint the real Godot headless integration scene passed catalog/mixed-type tests and all existing combat, stat, upgrade, economy, terrain, wave, feedback and navigation checks. After 4B it passed again, with 40 registered variants testing availability, canonical prices/icons, generated ID actions, focus, scrolling, rebinding and independent panels. `MenuTowerFlowChecks.tscn` separately exercises actual Boot profile-button → Main → Campaign → Level001 buttons, then paid Battle placement/upgrade using an injected memory-only profile (no save writes). The suite includes the existing 500-ship fixture; headless timings are not a rendered-FPS or visual-layout guarantee.
