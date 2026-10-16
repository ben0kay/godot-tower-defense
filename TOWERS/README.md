# TOWERS — Player-placeable structures

This folder holds all buildable towers: offensive weapons, Ammunition Factory/Depot economy structures, and future support/utility structures. A tower's category describes its primary purpose; it does **not** require a weapon, ammo system, targeting loop or combat stats.

This README is an up-to-date overview and forward plan. Completed phase-by-phase implementation notes have been removed because the feature-specific READMEs and actual code are authoritative.

> **Important:** Unimplemented items under **Tower operations** remain planning notes, not permission to build unrelated systems. Ammunition is explicitly marked completed. Current gameplay must stay unchanged until a separate implementation pass.

## Current implementation (October 2026)

- **Playable:** Battle Tower, Laser Tower, Blaster Cannon, Shockwave Tower. Ammunition Factory and Ammunition Depot are also registered Economy buildings. All six types use `DefaultTowerCatalog.tres`; other type folders may contain plans only.
- **Purchased levels:** `Offensive/<Tower>/*Levels.cs` contains one **complete, absolute row per level**, with construction price in Level 1 and upgrade price in later rows. October 2026 rebalance: Battle Level 1 deals 30 Kinetic hull damage (two hits versus 60-HP unshielded Enemy001), Battle projectile speed is 800 px/s at each level, and bought levels get gentler stat/range increases. Level 3 never accumulates the stat values from Level 2. Never restore cumulative purchased-level modifiers or scattered per-level `.tres` values.
- **Earned ranks:** `*Ranks.cs` beside `*Levels.cs` defines Standard → Veteran → Enhanced → Superior → Legendary, at 0 / 1,000 / 3,000 / 7,000 / 15,000 cumulative XP from actual shield/hull damage and kills. XP award multipliers are unchanged. Rank bonuses are independent of purchased levels; the bottom-right Inspector shows Rank, XP and bonuses. See [Shared/Ranks/README.md](Shared/Ranks/README.md).
- **Foundation:** `Shared/TowerBase.cs`, `Shared/TowerConfig.cs`, `Shared/Upgrades/` and `Shared/Stats/Pipeline/` own per-instance progression, independent config copies, payments/previews and cached effective stats.
- **Catalog and placement:** `Shared/TowerDefinition.cs`, `Shared/TowerCatalog.cs` and `Placement/` supply generic registered build choices, rectangular placement, atomic occupancy/navigation checks, selection and an existing Inspector. `ECONOMY/Wallet/` is the one wallet/payment authority.
- **Combat:** Battle fires guided Kinetic projectiles; Laser delivers Energy beam DPS; Blaster deals instant area damage; Shockwave uses a travelling wavefront. Shared combat and damage attribution live under `COMBAT/`.
- **Ammunition:** optional explicit per-level reserves/consumption, full configurable starting stock, factory production, depot storage and visible cargo drones on both legs are implemented. See [Shared/Ammunition](Shared/Ammunition/README.md) and [Economy/Ammunition](Economy/Ammunition/README.md).
- **Not currently implemented:** tower HP or tower shields, magazines/reloading, accuracy simulation, power consumption, maintenance/wear or selling.

### Where to find the code

| Path | Responsibility |
| --- | --- |
| `Offensive/BattleTower/BattleTowerLevels.cs` and the equivalent files for Laser/Blaster/Shockwave | Complete, explicitly authored numeric levels and construction/upgrade prices |
| `Offensive/<Tower>/*Ranks.cs` | Named rank thresholds and total rank bonuses |
| `Offensive/<Tower>/Default*Config.tres` | Non-level settings: identity, targeting/operation policy, visuals where relevant |
| `Shared/Stats/Pipeline/TowerStatPipeline.cs` | Single eight-stage stat coordinator |
| `Shared/Ranks/`, `Shared/Upgrades/` | XP/ranks and paid upgrade machinery |
| `Shared/Weapons/`, `Shared/Projectiles/`, `Shared/Targeting/` | Optional shared weapon components |
| `DefaultTowerCatalog.tres`, `Shared/TowerDefinition.cs`, `Placement/` | Tower registration and placement |
| `TowerMasterConfig.cs`, `DefaultTowerConfig.tres` | Global tower policies / XP multipliers, not per-level combat stats |

**Completed F1–F5:** [Generic rectangular tower footprints](Placement/MULTI_CELL_FOOTPRINT_PASS.md) — Ammunition Factory **3×2**, Depot **2×2**, existing offensive towers **1×1**. Whole rectangles require uniform buildable PASSABLE or IMPASSABLE foundations; mixed terrain rejects. Wall occupancy performs no navigation work, while passable footprints use combined route safety and incremental batches. [Verification](../TESTS/Towers/Footprints.md).

Detailed existing behavior belongs in [Shared/Stats/README.md](Shared/Stats/README.md), [Pipeline/README.md](Shared/Stats/Pipeline/README.md), [Shared/Upgrades/README.md](Shared/Upgrades/README.md), [Shared/Ranks/README.md](Shared/Ranks/README.md), [Placement/README.md](Placement/README.md), [COMBAT/Damage/README.md](../COMBAT/Damage/README.md), and the individual tower READMEs.

## Existing stat pipeline — preserve its stage ownership

| Stage | Status | Responsibility |
| --- | --- | --- |
| 01 Base Stats | Active | Original complete Level 1 source |
| 02 Tower Upgrades | Active | Substitute the selected purchased level's **absolute** stat row |
| 03 Tower Ranks | Active | Apply the currently earned named rank's **total** bonuses |
| 04 Permanent Research | Future / neutral | Player/profile-wide persistent research, across maps and reloads |
| 05 Level Modifiers | Future / neutral | Conditions owned by the current level/session |
| 06 Active Effects | Future / neutral | Runtime modifiers/conditions with explicit owners and lifetimes |
| 07 Safety Clamps | Active | Validate/clamp effective capability stats |
| 08 Final Stats | Active | Cached, immutable values consumed by gameplay |

Keep authored values, purchased level, rank/XP, future research, level rules and runtime effects **separate** from final calculated results. Rebuild `FinalStats` only when inputs change, not per combat frame. Purchase previews resolve through the same pipeline as real purchases. Existing rank data reserves a future HP bonus, but tower health does not yet exist.

Permanent research must belong to future persistent player/profile state, **not** the research-center building or LevelConfig. Current-level modifiers disappear when switching maps. Buffs, debuffs and operational conditions belong to the affected runtime instance/source. Do not add a new stat pipeline or an unused generic modifier engine in advance.

## Tower operations — completed ammunition and future extensions

**Direction:** More mechanical, infrastructure-driven tower defense. Towers should need time to build, survive damage, hold supplies and eventually depend on electricity and maintenance. Keep numeric progression in the compact one-row-per-level C# authoring style, but make systems optional by tower type.

### A. Explicit per-level parameters and future additions

These values may vary **independently** for Level 1, Level 2, Level 3, etc., just as Damage/Rate/Range do now. Field names and actual balance values are tentative.

| Parameter | Intent |
| --- | --- |
| **Construction/upgrade time** (`WorkSeconds` suggested) | Level 1 specifies **build time**; each later row specifies how long that particular **upgrade** takes. Purchased upgrades will no longer finish instantly. |
| **Maximum HP** (`MaxHealth`) | Structural durability of that tower at that level. Current HP is stored separately for each placed instance. |
| **Maximum Shield** (`MaxShield`) | Maximum tower shield capacity, separately from HP. Current shield, shield recharge and damage rules are future operational behavior. |
| **Ammunition Capacity** (`AmmunitionCapacity`) | Maximum **local reserve storage**, Implemented as AmmunitionStats.Capacity; Battle currently holds **1,000** at each authored level. Generic optional capability for Battle, Laser and other suitable tower types—not a Battle-only field. |
| **Ammunition per shot** (`AmmunitionPerShot`) | Units taken from available ammunition for each attack, usually **1** at first. Implemented consumption: Battle1/launch, Laser2/firing-second, Blaster3/launch and Shockwave5/pulse. |
| **Magazine size** (`MagazineCapacity`) | Optional additional value required for towers that use a magazine. Magazine rounds and total stored reserve are different quantities. |
| **Reload time** (`ReloadSeconds`) | Delay when a magazine reloads **from its local reserve**. Towers without magazines must not be forced to reload. |
| **Accuracy** | A level-dependent weapon handling value. Decide whether it means spread, leading/tracking, or hit probability *before* implementation; avoid meaningless per-tick miss rolls for continuous beams or area waves. |

**Do not implement these stats as cumulative level bonuses.** Every row must explicitly contain its own values, with the same clear one-row-per-level layout as `BattleTowerLevels.cs`. If rows become too wide, prefer small typed optional groups such as `StructureStats`, `AmmunitionStats` and `WeaponHandlingStats` within each row, rather than making every future tower own irrelevant stats.

**Operational state is not level data:** `CurrentHealth`, `CurrentShield`, `CurrentAmmo`, rounds currently loaded, construction progress and current reload time belong to the individual placed tower. Maximum capacity and rates are authored level data. No system should regenerate ammunition just because a magazine reload completes.

#### Construction/upgrade decisions to resolve during implementation

Before coding delayed work, define (and test) when the grid cell is reserved/occupied; whether an unfinished or upgrading tower can aim, fire, be damaged, block paths or be targeted; when Credits/materials are charged; cancellation/refund rules; pause/interruption behavior; and future save/load restoration. Progress should be visible in the relevant build/placed-tower Inspector. Keep existing paid placement, occupancy and route-validation guarantees intact.

#### Ammunition: completed first loop, future extensions

The shared optional reserve capability is implemented for all four weapons: Battle1/launch, Laser2/firing-second, Blaster3/launch and Shockwave5/pulse. Each complete C# level row owns capacity/consumption. New weapons start full unless InitialAmmoFraction is edited; promotions/upgrades preserve actual stock and never refill. Both factory→depot and depot→tower use visible drones with real cargo. Magazines, reload timers, differentiated ammo types, research and saving remain future work. See the ammunition READMEs for lifecycle/loss policies and verification.

#### HP, shields and accuracy

Tower HP and tower shields are new; the existing enemy health/shield system is not already a tower defense system. Define initial values, damage spillover, repairs/recharge, tower destruction and UI handling when implementing them. Only then should rank HP bonuses become active.

Accuracy must affect meaningful outcomes (for example projectile spread against moving enemies) and should be optional by weapon/delivery type. Keep existing guided shots, beams and area damage unchanged until an accuracy pass is requested.

### B. Fixed/non-level-upgradable operating rules — deferred

These are **not** additional per-level columns for now:

- **Electricity consumption:** A fixed baseline per tower type, eventually editable in non-level config (potentially idle draw and active firing draw). Purchased Level 2/3 should **not** automatically change it. In the future, **Permanent Research (Stage 04)** may lower effective electricity consumption. Generation, energy storage, distribution, power outages/brownouts and operational shutdown will require their own separate system. **Not implemented yet.**
- **Maintenance / wear:** After enough operating/play time, a particular tower may degrade (illustrative: **5%** reduced effectiveness). Keep this as **per-instance condition/state**, not an absolute `*Levels.cs` row or rank bonus. Later apply the penalty through an appropriate sourced stat-pipeline condition (potentially Stage 06 if treated as an ongoing effect), and decide what restores it. Research and levels must not be overwritten. **Not implemented yet.**

The intent of "fixed" is **not increased by buying a tower level**; research and other separately designed future systems can still affect effective power usage.

### C. Suggested future implementation passes

1. **Timed construction/upgrades C1–C3 implemented; visual stages C4 remain planned.** Initial placement blocks weapons/production/logistics until complete; paid upgrades keep the old tier operating and the Inspector shows work progress/time. **Codex pass C1–C4:** [Shared/Construction/TIMED_CONSTRUCTION_PASS.md](Shared/Construction/TIMED_CONSTRUCTION_PASS.md), including future authored SVG build stages and optional sparks/VFX. See the construction README for current durations and behavior.
2. **Tower HP and optional shields**, damage/repair foundations and Inspector readouts.
3. **Completed:** generic local ammunition storage/consumption and the first physical two-leg supply loop. Magazines/reloads remain future work.
4. **Optional magazines/reload timers**, for applicable weapons only.
5. **Accuracy mechanics**, selected by delivery type.
6. **Later:** electricity generation/distribution/consumption, expanded logistics, permanent research efficiency and maintenance/wear.

These are planning priorities, **not** permission to implement everything in one large rewrite. Keep each pass narrow, reusable and tested.

## Architecture, performance and future-proofing

Shared spatial targeting and area queries are implemented behind the world EnemyRegistry. Battle/Laser/Blaster acquisition uses nearby circular candidates; Blaster impacts use the same service, and timed Shockwaves query motion-aware swept bounds while retaining their original crossing solver. See `Offensive/Shared/README.md` for the completed optimization and `../TESTS/Performance/SpatialQueryBenchmarks.md` for measured sparse/dense gains, maintenance overhead and limitations.

- Optional capabilities: a Solar Tower may produce electricity, a Repair Tower may restore HP, a Laser may have no magazine, and a passive economic structure may have no weapon at all. Do not put mandatory attack/ammo/physics loops in `TowerBase`.
- Keep one authoritative economy and one placement/navigation owner. Construction time must not bypass payment rollback or path validation.
- Favor cached per-tower state, event-driven updates and budgeted/staggered operations; avoid one scene-tree search, resupply route solve or stat recalculation for **every** tower **every** frame. Target 500+ enemies without unnecessary per-frame allocation.
- Future save/load should persist source data (type, position, purchased level, XP/rank inputs, health/shield/ammo, construction/reload progress, as needed), then rebuild derived final stats. Do not serialize `FinalStats` as a replacement for actual state.
- Other separate future decisions: selling, rotated/irregular footprints, additional support/production tower mechanics, line-of-sight, special rank abilities and player research/active effects.
- Read `NOTES/CodePreferences.md` plus each feature's current README before working. Follow compact C#, labeled rows, comment separators, realistic Godot/compile tests and precise documentation. Never claim tests ran if they did not.

## Reserved future infrastructure folders — README-only

The [Construction](Shared/Construction/README.md), [Ammunition](Shared/Ammunition/README.md) and [Power](Shared/Power/README.md) folders document optional per-tower capabilities. [Economy infrastructure](Economy/README.md) covers production, depots and generators; [Power Relay](Utility/PowerRelay/README.md) is a utility building. Economy-owned network and delivery services are described under [POWER](Economy/Power/README.md) and [LOGISTICS](Economy/Ammunition/Shared/README.md). **All are future plans, not implemented or permission to refactor TD today.**

## Future shared generator and drone umbrellas — README-only

[Economy/Power](Economy/Power/README.md) groups Solar, Industrial and Fusion as **different generator types sharing the same future generation implementation**; avoid copying the same generator code three times. [DRONES](../DRONES/README.md) is the top-level home for reusable moving drones: [Transport](../DRONES/Transport/README.md) for future factory → depot and depot → tower physical deliveries, and [Combat](../DRONES/Combat/README.md) for a possible player-launched drone ability following enemy corridors. The isolated ammunition inventory, factory timer/buffer, depot stock and global 10% policy helpers are already coded but **not attached to live gameplay**. The required integration uses physical drones on both delivery legs; see [NOTES/WORK.md](../NOTES/WORK.md). Keep combat and tower stats unchanged until Codex's explicit integration work.
