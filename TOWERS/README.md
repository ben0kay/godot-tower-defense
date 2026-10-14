# TOWERS — All Player-Placeable Towers

Umbrella folder for **every player-buildable tower**, not just weapons: offensive defenses, support, economy/production and utility. The UI can call all of them *towers*. A tower's primary role determines its folder, **not** its only possible capability: for example, `Economy/SolarTower/` may eventually gain a weak defensive laser upgrade.

## Current state (October 2026)

**Implemented:** 1×1 grid-snapped Battle Tower placement/selection, tile-aware placement restrictions, a saved `BattleTowerConfig` Resource, First/Closest target selection, cooldown-based automatic shooting and guided test projectiles that damage Enemy001. The shared `FEEDBACK/DefenseBars/` component currently works on enemies and can be reused when towers become damageable.

**Already implemented since the original rotation plan:** the Battle Tower has a rotating `TurretPivot`, reusable `TurretAiming` and Inspector-editable turning speed/aim tolerance. **Phase 2 is now implemented:** typed ConstructionCosts and the session wallet charge 200 Credits for Battle Tower. LevelConfig owns StartingCredits and the HUD displays the live balance. Upgrades, resource generation, upgrade buttons, tower health/damage and selling remain future work. `TowerMasterConfig.AllowUpgrades`, `AllowSelling` and sell refund values are future rules, not proof those mechanics work.

**Codex: read `NOTES/CodePreferences.md`, this file, `Offensive/BattleTower/README.md`, `Shared/README.md` and the current code before implementing.** Favor lightweight reusable C# code and Inspector-configurable Resources; do not replace or break the working HUD, navigation, place/select flow, or Enemy001 combat.

## Folder map

| Location | Responsibility |
| --- | --- |
| `Shared/` | Common placeable-tower identity, runtime state and optional capabilities; already has `Targeting/` and `Projectiles/`. |
| `Placement/` | Build selection, ghost, terrain/build validation, occupancy and placed-scene creation; already active. |
| `Offensive/` | Weapon-first towers such as Battle, Laser and Missile. |
| `Support/` | Repair, radar, buffs and other supporting towers. |
| `Economy/` | Resource, solar/energy and refinery towers; may gain optional attacks. |
| `Utility/` | Specialized roles not covered above. |

### Proposed additions — DO NOT assume these files exist yet

```text
TOWERS/
├── TowerMasterConfig.cs                     # Already exists: shared gameplay/preview flags
├── DefaultTowerConfig.tres                  # Already exists
├── README.md
├── Shared/
│   ├── TowerBase.cs                         # Planned: identity, instance state, upgrade level
│   ├── TowerDefinition.cs                   # Planned: shared data for any tower type
│   ├── Costs/
│   │   └── README.md                        # Planned: tower cost usage; shared entries live in ECONOMY/Transactions/
│   ├── Upgrades/
│   │   ├── TowerUpgradeTier.cs              # Planned: next-level cost and effect data
│   │   ├── TowerUpgradeController.cs        # Planned: shared upgrade/validation logic
│   │   └── README.md
│   ├── Targeting/                            # Already exists: TowerTargeting.cs
│   ├── Weapons/
│   │   ├── TurretController.cs              # Planned: optional tracking/aiming
│   │   └── README.md
│   └── Projectiles/                          # Already exists: BasicProjectile.cs
├── Placement/                                # Already exists
├── Offensive/
│   └── BattleTower/
│       ├── BattleTower.cs                   # Already exists
│       ├── BattleTowerConfig.cs             # Already exists; extend for rotation/costs
│       ├── BattleTower.tscn                  # Already exists; change pivot as needed
│       ├── DefaultBattleTowerConfig.tres     # Already exists
│       ├── Upgrades/                         # Future per-type upgrade resources/art
│       └── Artwork/
├── Support/
├── Economy/
└── Utility/
```

Keep the actual economy/inventory service **outside** `TOWERS/`; multiple game systems will eventually spend and award the same resources. The entries in `Shared/Costs/` describe a price, not a wallet.

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

## Phase 3 — Tower upgrades and a layered stat pipeline (planned)

Every tower type should **be able to upgrade**, including support/economy towers; an individual design may have fewer upgrade tiers or temporarily disable upgrades.

- `TowerBase` (or similarly small reusable common component) owns stable tower identity, current upgrade level, active configuration and optional lifecycle/selection hooks; it must **not** force all towers to run targeting/firing code.
- Define upgrade steps as Inspector-editable Resources, e.g. `TowerUpgradeTier`: target tier/level, shared `ResourceCost` array, descriptive label, and **explicit relevant improvements / optional unlocked capabilities**. Avoid a monolithic list of weapon-only stats or fragile reflection-based generic property patches.
- Begin with a simple **linear upgrade sequence**: initial level 1 → level 2 → level 3, etc. Allow an upgrade path to evolve into multiple branches later without hard-coding all towers into BattleTower.
- A weapon upgrade may affect range, shots/sec, damage, projectile speed, turning speed, barrel count, or unlock a new firing mode. A support upgrade may increase repair/buff strength. An economy upgrade may increase output or add a small laser. **Upgrades must not require every tower to have a weapon**.
- Applying an upgrade changes the **specific placed instance's tier/progression inputs**, then rebuilds its final stats through the pipeline below. Never mutate shared default `.tres` assets or compound changes into previously calculated final values. Existing per-instance Config duplication remains useful, but does not replace separation of base inputs from calculated stats.
- Eventually connect the bottom-right gameplay **Inspector panel**: show current level, current stats, next upgrade's changes and price; offer an Upgrade button only when a valid next upgrade is available/affordable. No upgrade UI needs to be created during Phase 1.
- Future sell/repair/damage systems can reuse the tower base and `FEEDBACK/DefenseBars/` once tower health exists.

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

### Required code layout — readable, numbered stat pipeline (planned)

**Codex implementation target:** keep every stat stage clearly visible under **`TOWERS/Shared/Stats/Pipeline/`**, with **one small C# file per stage in the exact order below**. The Stats/Pipeline files are implemented in Phase 3A; the upgrade/rank/research/effect gameplay files shown below remain future work. The leading numbers sort them in VS Code/Godot's FileSystem; ordinary C# class names do **not** start with digits.

```text
TOWERS/
├── Shared/
│   ├── TowerBase.cs                          # Planned: lightweight instance/progression owner
│   ├── Stats/
│   │   ├── README.md                         # Planned: authoring, data ownership and debugging
│   │   ├── TowerStatBlock.cs                 # Typed in-progress stat values/capability groups
│   │   ├── TowerStatContext.cs               # All read-only inputs for one resolution
│   │   ├── TowerStatModifier.cs              # Flat/percent/multiplier operation and target stat
│   │   ├── TowerFinalStats.cs                 # Read-only effective values consumed by gameplay
│   │   ├── TowerStatTrace.cs                 # Optional debug-only snapshots per stage
│   │   └── Pipeline/
│   │       ├── README.md                     # The order and rules, easy to find
│   │       ├── TowerStatPipeline.cs          # SINGLE coordinator, explicit ordered calls
│   │       ├── 01_BaseStats.cs               # BaseStatsStage
│   │       ├── 02_TowerUpgrades.cs           # TowerUpgradesStage
│   │       ├── 03_TowerRanks.cs              # TowerRanksStage
│   │       ├── 04_PermanentResearch.cs       # PermanentResearchStage
│   │       ├── 05_LevelModifiers.cs          # LevelModifiersStage
│   │       ├── 06_ActiveEffects.cs           # ActiveEffectsStage
│   │       ├── 07_SafetyClamps.cs            # SafetyClampsStage
│   │       └── 08_FinalStats.cs              # FinalStatsStage
│   ├── Upgrades/
│   │   ├── TowerUpgradeTier.cs              # Planned: purchased tier definition and ResourceCost
│   │   ├── TowerUpgradeController.cs        # Planned: validated atomic upgrade transaction
│   │   └── README.md
│   ├── Ranks/
│   │   └── README.md                         # Planned: kill milestones/bonuses/ownership
│   ├── Modifiers/
│   │   └── README.md                         # Planned: timed effect source, duration, stacking
│   ├── Targeting/                             # EXISTS: common tower targeting
│   ├── Weapons/                               # EXISTS: TurretAiming
│   └── Projectiles/                           # EXISTS: BasicProjectile
├── Offensive/
│   └── BattleTower/
│       ├── BattleTower.cs                    # EXISTS: must eventually consume FinalStats
│       ├── BattleTowerConfig.cs              # EXISTS: source of Battle Tower base values
│       ├── DefaultBattleTowerConfig.tres     # EXISTS: authored, never mutate during resolution
│       └── Upgrades/                          # Planned: Battle Tower tier Resources/art
├── Support/                                   # Future shared pipeline consumers
├── Economy/                                   # Future shared pipeline consumers
└── Utility/

LEVELS/
├── LevelConfig.cs                             # EXISTS: level settings; add reference later
├── Level001/
│   ├── Level001Config.tres                    # EXISTS
│   ├── Level001Waves.cs                       # EXISTS
│   └── Level001TowerModifiers.cs              # OPTIONAL FUTURE: authored level-only rules
└── Level002/
    └── Level002TowerModifiers.cs              # OPTIONAL FUTURE: separate authored level rules

PROGRESSION/                                   # PROPOSED FUTURE umbrella; do not create now
└── Research/                                  # Persistent player/profile research ownership
    └── README.md
```

`PROGRESSION/Research/` is a **suggested** future home, not an existing service or finalized save design. Level-specific conditions may be authored in a readable per-level C# file **or** a small referenced Resource if that suits Godot; **avoid huge Inspector arrays**. The active level passes its modifiers to the shared resolver; `05_LevelModifiers.cs` must never hardcode `Level001` or read a specific scene file. **Do not move existing `LEVELS/`, `ECONOMY/` or tower files** just to create this layout.

#### Explicit pipeline orchestration — one file shows the complete order

`TowerStatPipeline.cs` should be immediately comprehensible when opened: the ordered stages are invoked **explicitly in one place**, without hidden ordering by class discovery, reflection, enum values or file-system iteration.

**Illustrative C# shape, not runnable code today:**

```csharp
public static TowerFinalStats Resolve(TowerStatContext context)
{
    TowerStatBlock stats = BaseStatsStage.Create(context);
    stats = TowerUpgradesStage.Apply(stats, context);
    stats = TowerRanksStage.Apply(stats, context);
    stats = PermanentResearchStage.Apply(stats, context);
    stats = LevelModifiersStage.Apply(stats, context);
    stats = ActiveEffectsStage.Apply(stats, context);
    stats = SafetyClampsStage.Apply(stats, context);
    return FinalStatsStage.Create(stats, context);
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

- **One authoritative `TowerFinalStats` per placed instance**, computed from source inputs and cached. Existing `BattleTower.cs` currently reads live combat values from `Config.RangePixels`, `Config.DamagePerShot`, `Config.ShotsPerSecond`, etc.; Phase 3 must deliberately switch **runtime consumers** to the resolved final values while keeping authored Config as the base input. Aim speed/turning, repair rate or economy output should follow the same pattern as capabilities are introduced.
- Mark final stats **dirty** on relevant events: tier purchase, individual rank gain, research change/load, level-rule replacement, new effect, expired/removed effect. Recalculate on change or the next safe access, **not on every physics frame**; avoid per-frame allocations or scanning all research/levels for every tower. Research invalidation should target eligible towers when possible.
- The **same resolver** should calculate both the currently applied result and a proposed upgrade preview; for preview only the hypothetical tier changes, while current rank, research, level rules and temporary effects stay identical. Preview must never spend Credits or mutate the placed tower.
- Apply flat/additive-percent/explicit-multiplier operations using the existing Phase 3 arithmetic rules **within each stage**, in the fixed stage order. Identical inputs and seed-free stat calculations must produce identical outputs; never add upgrades to the old final cache or reverse expired debuffs by subtracting/dividing previous results.
- On save/load, restore authoritative progression inputs (purchased tier, earned rank and profile research), then resolve final stats. Do not serialize the derived final result as a substitute for those inputs. Level rules should be **replaced** when switching levels; persistent research must survive. Do not implement the future profile-save system just to implement Battle Tower Phase 3.
- Keep existing `ECONOMY/Wallet/EconomyManager`/`ResourceCost` as the **only** authoritative payment service and existing `TowerPlacementController` as the placement owner. A stat-stage file must never charge resources, attribute kills, schedule timed effects, own a research-center UI, or directly alter enemy navigation.
- Match project style: brief header comments, `// =========================================================` comments above methods, sensible `#region` groups, compact code and README files in newly created folders. Avoid empty services and excessive abstraction; neutral stage methods can remain lightweight until their feature is built.

**Acceptance for this architecture:** a developer can open **`TOWERS/Shared/Stats/Pipeline/TowerStatPipeline.cs` and see the entire actual execution sequence at a glance**, open any numbered file to find that step's implementation, inspect optional stage-by-stage values, and follow each source back to its natural owning feature folder. No other class should contain a competing full copy of the pipeline.

### Phase 3A — Pipeline foundation ONLY (implemented)

**Explicit decision: it is NOT necessary to build purchased upgrades, kill-based ranks, permanent research, level conditions or active buff/debuff systems before the pipeline.** Codex should first implement the **entire numbered pipeline structure** with the currently available **Base Stats → neutral future stages → Safety Clamps → Final Stats**. This is the completed standalone foundation, and must compile and run with *zero research, rank, upgrade or modifier data present*.

| Pipeline file | Phase 3A behavior | When real logic is added |
| --- | --- | --- |
| `01_BaseStats.cs` | **ACTIVE:** copy actual authored Battle Tower values from its current Config/definition into typed working stat values. | Continue reading immutable base data for every future tower. |
| `02_TowerUpgrades.cs` | **NEUTRAL PASS-THROUGH:** return the incoming values unchanged; no tier purchases/UI yet. | Phase 3B: purchased tower tiers. |
| `03_TowerRanks.cs` | **NEUTRAL PASS-THROUGH:** no rank/kill attribution required. | Later: per-instance ranks and kill milestones. |
| `04_PermanentResearch.cs` | **NEUTRAL PASS-THROUGH:** no ResearchManager or player-profile save required. | Later: persistent player research sourced outside levels. |
| `05_LevelModifiers.cs` | **NEUTRAL PASS-THROUGH:** no per-level condition data/files required. | Later: authored `LEVELS/` modifiers supplied by the active level. |
| `06_ActiveEffects.cs` | **NEUTRAL PASS-THROUGH:** no effect managers, stack logic or duration timers required. | Later: timed buffs/debuffs. |
| `07_SafetyClamps.cs` | **ACTIVE:** enforce sensible bounds/finite values; allow intentional zero shots/sec to disable firing. | Extend for new capability-specific stats. |
| `08_FinalStats.cs` | **ACTIVE:** publish a separate, immutable/read-only per-instance `TowerFinalStats`. | All gameplay systems continue reading effective values. |

**Simple neutral-stage implementation (illustrative):**

```csharp
// 04_PermanentResearch.cs — no permanent-research system required yet.
public static class PermanentResearchStage
{
    // =========================================================
    // Neutral until a player/profile research system is implemented.
    public static TowerStatBlock Apply(TowerStatBlock stats, TowerStatContext context)
        => stats;
}
```

**Requirements for Codex in 3A:**
- Create `TOWERS/Shared/Stats/` and its `Pipeline/` folder with the proposed numbered stage files, coordinator, typed source/context and final stats. Use one small, explicitly ordered resolver; no duplicated stage logic in `BattleTower.cs`.
- **Build only the dependencies that exist now.** `TowerStatContext` should accept neutral/empty future inputs without trying to resolve nonexistent managers, services, `.tres` assets, level modifier files or permanent research saves. A neutral stage does not need an empty Godot `Node` or manager; it is a simple C# function.
- Keep initial Battle Tower base values from `BattleTowerConfig` unchanged: range, fire rate, damage, projectile speed, aiming/rotation, targeting intervals as appropriate. Keep IDs, priority, resource costs and other non-stat metadata in their existing owners. Do **not** invent new balance numbers or restructure wave/level folders.
- Wire the **actual Battle Tower runtime** to cached `TowerFinalStats` where a value is a resolvable stat, rather than continuing to bypass the pipeline through `Config`. Build/rebuild on initialization and relevant future changes, never every physics tick. Preserve the existing in-editor/in-game Inspector tuning workflow: changes to an instance's Config need an explicit safe way to invalidate/rebuild stats, and tests should verify the supported workflow.
- With the six neutral/active stages described above, **effective stats must equal the original base values**, except documented safety clamps/rounding. Existing fire rate, damage, projectile motion, turret rotation, targeting, placement, credits and HUD behavior must remain the same. Separate FinalStats instances must not mutate or alias shared `.tres` configuration.
- Intermediate-stage debugging (optional `TowerStatTrace`) may show identical values throughout neutral stages; keep tracing opt-in and allocation-free in normal combat.
- Test the real baseline Battle Tower and stat-resolution determinism, stage order, source-versus-final isolation, zero shots/sec, clamps and per-tower independence. Confirm F5 gameplay and headless regressions pass. Only update docs for features that actually work.

**Acceptance:** Phase 3A is complete when a developer can open `TowerStatPipeline.cs`, visibly follow **all eight numbered steps**, and run the existing Battle Tower with unchanged gameplay—even though upgrade, rank, research, level-modifier and active-effect systems are **not implemented at all**.

### Implementation order after the foundation

1. **Phase 3A (NOW):** implement the ordered pipeline, immutable base/final separation, neutral future stages, runtime FinalStats wiring, safety validation and baseline regression tests only.
2. **Phase 3B (NEXT):** implement purchased upgrade-tier data, atomic `ECONOMY/` payments and an upgrade preview/button; activate only the `02_TowerUpgrades.cs` stage. The later stages stay neutral.
3. **Phase 3C+ (LATER):** implement ranks, player/profile research, authored level conditions and temporary buffs/debuffs in separate focused passes, activating **their existing stage files** without restructuring the resolver or changing prior stages.
4. Add feature-specific tests **when those future systems actually exist**: per-tower kill attribution, research retained across level changes/save/load, level-rule replacement, deterministic effect stacking/removal, preview parity and drift-free recomputation. Do not fabricate placeholder gameplay or future managers to satisfy tests prematurely.

Illustrative Battle Tower progression (**NOT approved final balance**):

| Tower level | Damage | Shots/sec | Upgrade price |
| --- | ---: | ---: | --- |
| 1 | 12 | 1.0 | Construction: 200 Credits |
| 2 | 16 | 1.2 | 150 Credits |
| 3 | 22 | 1.5 | 250 Credits + 10 Alloy |
| 4 | 30 | 1.8 | 400 Credits + 25 Alloy |

Keep actual progression per tower editable in Resources, not in a global switch or hard-coded upgrade table.

## Next implementation pass

Phase 1 rotation, Phase 2 paid construction and **Phase 3A stat foundation are implemented**. Battle Tower combat, selected stats and range displays use immutable cached FinalStats; numeric Config changes emit invalidation events. The eight numbered stages are in TOWERS/Shared/Stats/Pipeline, with 02–06 neutral. See Shared/Stats/README.md for the supported tuning workflow and safety rules.

**Next is Phase 3B:** purchased tier data, atomic ECONOMY payments and an upgrade preview/button, activating only 02_TowerUpgrades. Ranks, persistent research, level conditions and timed effects remain later passes. Do not rebuild the pipeline, add a competing wallet or fabricate future managers.

Tests currently cover rotation/cooldown, atomic payments, paid placement and nested config isolation. Add tier progression and upgrade effects/payment checks when upgrades are implemented.

**Keep performance and compatibility:** C#/.NET only, no duplicate targeting system, no always-on weapon ticks for non-weapon towers, no unnecessary per-frame allocations, no per-tower independent economy service. Preserve `WorldGrid` occupancy, route-preserving placement, current navigation/tower targeting, HUD and Enemy001/DefenseBar behavior. Follow the project's code style and use organized Inspector `[ExportGroup]` settings.

## Current live Inspector files

- `Offensive/BattleTower/DefaultBattleTowerConfig.tres`: name/description, ConstructionCosts (200 Credits), aiming, range, shots/sec, damage, projectile speed, target priority and target search interval.
- `DefaultTowerConfig.tres`: shared placement preview/selection range toggles and future upgrade/sell switches.
- `FEEDBACK/DefenseBars/DefaultDefenseBarConfig.tres`: reusable health bar appearance when towers become damageable.

**Current status:** rotation (Phase 1), paid construction (Phase 2) and neutral stat pipeline (Phase 3A) work. Purchased upgrades (3B), ranks, research, level conditions and timed effects remain planned.


## Implemented Phase 3A verification
All eight stage files and the single coordinator exist. TowerFinalStats is a separate immutable instance snapshot; source Config remains Inspector-editable and numeric changes automatically rebuild. Resource replacement/exit remove old subscriptions. StatsChanged refreshes selected Inspector values/range rings; combat ticks consume cached results. Safety bounds and optional traces are documented in Shared/Stats/README.md. TowerStatChecks validates stage order, baseline parity, clamps, cache/input isolation, per-tower independence, live tuning and disabled/re-enabled firing. No tier purchasing, rank/research/condition/effect gameplay or Upgrade button was added.
