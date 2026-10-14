# TOWERS — All Player-Placeable Towers

Umbrella folder for **every player-buildable tower**, not just weapons: offensive defenses, support, economy/production and utility. The UI can call all of them *towers*. A tower's primary role determines its folder, **not** its only possible capability: for example, `Economy/SolarTower/` may eventually gain a weak defensive laser upgrade.

## Current state (October 2026)

**Implemented:** 1×1 grid-snapped Battle Tower placement/selection, tile-aware placement restrictions, a saved `BattleTowerConfig` Resource, First/Closest target selection, cooldown-based automatic shooting and guided test projectiles that damage Enemy001. The shared `FEEDBACK/DefenseBars/` component currently works on enemies and can be reused when towers become damageable.

**Not yet implemented:** the Battle Tower sprite **does not rotate toward its target**; shots launch from the center regardless of visual aim. `BuildCost` is a display-only integer with **no credit deduction**. Upgrades, resource inventory, upgrade buttons, tower health/damage, and tower selling gameplay are not implemented. `TowerMasterConfig.AllowUpgrades`, `AllowSelling` and sell refund values are future rules, not proof those mechanics work.

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
│   │   ├── TowerCostEntry.cs                # Planned: resource identifier + amount
│   │   └── README.md
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

## Phase 1 — Make the Battle Tower rotate toward its target

**This is the immediate next task**; implement it before costs or upgrades.

Today `BattleTower.cs` acquires targets and fires bullets but doesn't turn the art. Add a lightweight reusable turret-aiming helper in `Shared/Weapons/` that **only weapon-capable towers opt into**. Never require a weapon or targeting loop on Repair, Radar, Solar or other passive towers.

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

## Phase 2 — Shared multi-resource construction costs (data foundation first)

Replace the long-term single-resource `BattleTowerConfig.BuildCost` model with an **Inspector-editable collection of cost entries** shared by *all* towers. Credits should be the default resource; extra materials should be optional.

Conceptual example:

```text
Battle Tower — ConstructionCosts
  [0] Resource: Credits    Amount: 100
  [1] Resource: Alloy      Amount: 5       # Optional future recipe

Solar Tower — ConstructionCosts
  [0] Resource: Credits    Amount: 125
  [1] Resource: EnergyCell Amount: 2       # Optional example
```

- Proposed `TowerCostEntry : Resource` includes a stable resource identifier (or future reference to a reusable `ResourceDefinition`) and non-negative integer amount. Prefer a typed reusable Resource asset/reference as the economy system matures; avoid scattered magic strings and resource-name comparisons in gameplay code.
- Export `Godot.Collections.Array<TowerCostEntry>` (or an equally Inspector-friendly collection) in the tower's definition/config. A tower may require only Credits or multiple resource types.
- Use the **same cost entry shape** for build costs **and upgrade costs**. The UI should eventually show missing resources and affordability clearly.
- **Migration:** preserve today's 100-credit Battle Tower price when converting from `BuildCost`; don't accidentally charge both the old integer and the new cost array. Decide the one authoritative source; keep temporary compatibility only if needed.
- Later, after terrain/route validation, the shared economy service should **validate and deduct the complete cost atomically** before committing a tower/upgrade. On any failure, no partial deduction or partially placed tower. Successful sale/refund should use recorded paid costs and the existing shared sell refund rules where appropriate.
- **Do not implement a whole economy or block current free-build debugging in this phase.** First establish Resource data, Inspector editing and display compatibility, then connect the economy when ready.

## Phase 3 — Generic, data-driven tower upgrades

Every tower type should **be able to upgrade**, including support/economy towers; an individual design may have fewer upgrade tiers or temporarily disable upgrades.

- `TowerBase` (or similarly small reusable common component) owns stable tower identity, current upgrade level, active configuration and optional lifecycle/selection hooks; it must **not** force all towers to run targeting/firing code.
- Define upgrade steps as Inspector-editable Resources, e.g. `TowerUpgradeTier`: target tier/level, shared `TowerCostEntry` array, descriptive label, and **explicit relevant improvements / optional unlocked capabilities**. Avoid a monolithic list of weapon-only stats or fragile reflection-based generic property patches.
- Begin with a simple **linear upgrade sequence**: initial level 1 → level 2 → level 3, etc. Allow an upgrade path to evolve into multiple branches later without hard-coding all towers into BattleTower.
- A weapon upgrade may affect range, shots/sec, damage, projectile speed, turning speed, barrel count, or unlock a new firing mode. A support upgrade may increase repair/buff strength. An economy upgrade may increase output or add a small laser. **Upgrades must not require every tower to have a weapon**.
- Applying an upgrade must update the **specific placed instance**, not unintentionally mutate the shared default `.tres` or other towers. Existing `BattleTowerConfig` duplication is the intended pattern.
- Eventually connect the bottom-right gameplay **Inspector panel**: show current level, current stats, next upgrade's changes and price; offer an Upgrade button only when a valid next upgrade is available/affordable. No upgrade UI needs to be created during Phase 1.
- Future sell/repair/damage systems can reuse the tower base and `FEEDBACK/DefenseBars/` once tower health exists.

Illustrative Battle Tower progression (**NOT approved final balance**):

| Tower level | Damage | Shots/sec | Upgrade price |
| --- | ---: | ---: | --- |
| 1 | 12 | 1.0 | Construction: 100 Credits |
| 2 | 16 | 1.2 | 150 Credits |
| 3 | 22 | 1.5 | 250 Credits + 10 Alloy |
| 4 | 30 | 1.8 | 400 Credits + 25 Alloy |

Keep actual progression per tower editable in Resources, not in a global switch or hard-coded upgrade table.

## Implementation order for Codex

1. **Do Phase 1 first:** reusable turret rotation, correct art-facing angle, aim tolerance and Inspector settings. Verify firing against Enemy001.
2. **Then Phase 2:** define shared typed construction cost entries with optional secondary resources; migrate the Battle Tower's displayed cost without enabling real economy spending.
3. **Then Phase 3:** establish generic upgrade/tier Resources and instance state, later adding build/upgrade economy transactions and HUD upgrades in a separate focused pass.
4. Update corresponding per-feature README files when each feature actually exists, and add focused tests for aiming tolerance/cooldown, cost migration, instance isolation and upgrade level changes.

**Keep performance and compatibility:** C#/.NET only, no duplicate targeting system, no always-on weapon ticks for non-weapon towers, no unnecessary per-frame allocations, no per-tower independent economy service. Preserve `WorldGrid` occupancy, route-preserving placement, current navigation/tower targeting, HUD and Enemy001/DefenseBar behavior. Follow the project's code style and use organized Inspector `[ExportGroup]` settings.

## Current live Inspector files

- `Offensive/BattleTower/DefaultBattleTowerConfig.tres`: name/description, current legacy BuildCost (display-only), range, shots/sec, damage, projectile speed, target priority and target search interval.
- `DefaultTowerConfig.tres`: shared placement preview/selection range toggles and future upgrade/sell switches.
- `FEEDBACK/DefenseBars/DefaultDefenseBarConfig.tres`: reusable health bar appearance when towers become damageable.

**This README is the implementation specification, not evidence that Phases 1–3 have been coded.**
