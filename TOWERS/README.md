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

## Phase 3 — Generic, data-driven tower upgrades

Every tower type should **be able to upgrade**, including support/economy towers; an individual design may have fewer upgrade tiers or temporarily disable upgrades.

- `TowerBase` (or similarly small reusable common component) owns stable tower identity, current upgrade level, active configuration and optional lifecycle/selection hooks; it must **not** force all towers to run targeting/firing code.
- Define upgrade steps as Inspector-editable Resources, e.g. `TowerUpgradeTier`: target tier/level, shared `ResourceCost` array, descriptive label, and **explicit relevant improvements / optional unlocked capabilities**. Avoid a monolithic list of weapon-only stats or fragile reflection-based generic property patches.
- Begin with a simple **linear upgrade sequence**: initial level 1 → level 2 → level 3, etc. Allow an upgrade path to evolve into multiple branches later without hard-coding all towers into BattleTower.
- A weapon upgrade may affect range, shots/sec, damage, projectile speed, turning speed, barrel count, or unlock a new firing mode. A support upgrade may increase repair/buff strength. An economy upgrade may increase output or add a small laser. **Upgrades must not require every tower to have a weapon**.
- Applying an upgrade must update the **specific placed instance**, not unintentionally mutate the shared default `.tres` or other towers. Existing `BattleTowerConfig` duplication is the intended pattern.
- Eventually connect the bottom-right gameplay **Inspector panel**: show current level, current stats, next upgrade's changes and price; offer an Upgrade button only when a valid next upgrade is available/affordable. No upgrade UI needs to be created during Phase 1.
- Future sell/repair/damage systems can reuse the tower base and `FEEDBACK/DefenseBars/` once tower health exists.

Illustrative Battle Tower progression (**NOT approved final balance**):

| Tower level | Damage | Shots/sec | Upgrade price |
| --- | ---: | ---: | --- |
| 1 | 12 | 1.0 | Construction: 200 Credits |
| 2 | 16 | 1.2 | 150 Credits |
| 3 | 22 | 1.5 | 250 Credits + 10 Alloy |
| 4 | 30 | 1.8 | 400 Credits + 25 Alloy |

Keep actual progression per tower editable in Resources, not in a global switch or hard-coded upgrade table.

## Next implementation pass

Phase 1 rotation and Phase 2 wallet/paid construction are implemented. Next is **Phase 3 upgrades**, in a focused pass: define optional tower instance/tier data, reuse ResourceCost and the existing wallet, then connect the Inspector. Do not create another wallet, rebuild aiming or reintroduce BuildCost.

Tests currently cover rotation/cooldown, atomic payments, paid placement and nested config isolation. Add tier progression and upgrade effects/payment checks when upgrades are implemented.

**Keep performance and compatibility:** C#/.NET only, no duplicate targeting system, no always-on weapon ticks for non-weapon towers, no unnecessary per-frame allocations, no per-tower independent economy service. Preserve `WorldGrid` occupancy, route-preserving placement, current navigation/tower targeting, HUD and Enemy001/DefenseBar behavior. Follow the project's code style and use organized Inspector `[ExportGroup]` settings.

## Current live Inspector files

- `Offensive/BattleTower/DefaultBattleTowerConfig.tres`: name/description, ConstructionCosts (200 Credits), aiming, range, shots/sec, damage, projectile speed, target priority and target search interval.
- `DefaultTowerConfig.tres`: shared placement preview/selection range toggles and future upgrade/sell switches.
- `FEEDBACK/DefenseBars/DefaultDefenseBarConfig.tres`: reusable health bar appearance when towers become damageable.

**Current status:** rotation (Phase 1) and paid construction/economy (Phase 2) work. Upgrades (Phase 3) remain planned.

