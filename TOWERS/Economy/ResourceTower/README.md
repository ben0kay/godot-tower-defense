## Tower durability rebalance — 2026-10-20 (source updated; Godot runtime tests pending)

**Current authoritative defaults supersede older HP references below:** Base/max hull health for every available player-built tower/structure is **10× the previous level-1 value**. All **nine offensive towers** (Battle, Laser, Blaster Cannon, Shockwave, Machine Gun, Sniper, Rocket, Cryo, Stasis) have explicit, independent paid level snapshots: **1,000 → 1,050 → 1,102.5 HP** (two-level towers stop at 1,050). **Every purchased upgrade adds 5% of the previous tier's Max HP**, compounded, not 5 percentage points of the original at every tier; the values are authored as complete absolute tier stats, not runtime additive bonuses.

**Other buildings:** Ammunition Factory and Depot **1,000 HP** each (one build tier/no paid upgrades yet); Resource Tower and Adaptable Resource Generator **1,500 → 1,575 → 1,653.75 HP** (new tier progression replaces their former 150/200/250 HP schedule); Stealth Beacon **1,800 HP** (one tier). Generic passive config fallback is 1,000 HP. These are **hull**, not shields. Enemy damage, friendly firing stats, costs, unlocks, construction timing and Citadel HP are unchanged.

**Pipeline and HP semantics:** Stage02 still selects one authored tier, then Stage03 applies earned **rank HP bonuses independently**. The existing `TowerBase.Durability` synchronizes max capacity while **preserving missing absolute HP**; upgrades do not fully heal already-damaged towers. The current Inspector's `Max HP` comparison automatically shows the extra `(+50)` at level2 and `(+52.5)` at level3 for a standard-rank 1,000-HP offensive tower. No duplicate HP system, per-frame calculations, or tower-specific damage handlers were added.

**Verification still needed:** Compile Godot .NET, run `TESTS/Towers/TowerHealthChecks.tscn` and the existing integration scene that executes `TowerStatChecks`/`TowerUpgradeChecks`, plus `TESTS/Economy/CreditTowerChecks.tscn`. Inspect upgraded/damaged towers at runtime, including the resource producer `.tres` serialized tiers. Written test assertions were updated, but **not executed here**.

---

# ResourceTower — implemented

## CP2 + CP3 economy towers — implemented 2026-10-19

Both towers are registered in the Ctrl build tray's **Economy** category, with muted dollar-bill greens and distinct original SVGs. They use the existing placement/construction/health/paid-upgrade/Inspector flow. Select one and press **COLLECT … CREDITS** in its Inspector; world clicks still select towers normally. Wallet grants remain centralized, all-or-nothing and once-only. Failed/full-wallet collection leaves storage intact.

| Purchased tier | Resource Tower price | Credits / 5 active seconds | ARG price | Credits / eligible kill | ARG range | Storage (both) | HP (both) |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 350 | 10 | 300 | 1 | 256px | 100 | 150 |
| 2 | 250 | 15 | 250 | 2 | 288px | 150 | 200 |
| 3 | 350 | 20 | 350 | 3 | 320px | 200 | 250 |

Prices for tiers2/3 are upgrade purchases, not cumulative costs. All work takes2 seconds initially. Resource Tower footprint **2×2**, ARG **1×1**. Balance is provisional. Edit each `Default…Config.tres` → **Credit Production / Production Levels** for absolute tier prices, capacity, payout, interval, range, hull and work duration before deployment. The nested tier resources are cloned per placed tower; existing instances retain private snapshots. Tier capacity must not decrease. Identity/art/footprint remain in the standard tower Definition. ActiveWavesOnly defaults true on both; turning it off permits between-wave income while the mission is Playing.

**Resource Tower:** shared CreditProductionTimer runs only while operational, not busy/disabled/paused, mission Playing, and (by default) waves Spawning/SpawnBlocked/WaitingForClear. Fractional progress freezes between waves/while disabled. Full storage accumulates no backlog. Each purchased upgrade preserves stored Credits, increases capacity and starts a fresh periodic clock with the new rate; no retroactive payout.

**ARG:** listens to the authoritative registry's new AttributedKill event, published before ordinary EnemyKilled listeners can finish the wave. Enemy records only the actual lethal DamageSource, not last nearby/last nonlethal attacker. The weak owner token must resolve to a live, nonqueued, nondestroyed Offensive tower in the same world. Range is measured inclusively to that **killing tower's position at kill time**, never the victim. Every eligible overlapping ARG earns independently. Legacy/environmental/unattributed kills and arrivals do not qualify. Already-emitted hits can still belong to a disabled living offensive source; disabled/incomplete/upgrading/destroyed ARGs do not earn. The final wave kill earns normally. Existing global kill Credits, finite damage/kill XP and carrier lifecycles stay separate. The immutable source token also supports future delayed/DoT payloads; no DoT tower was added.

**Shared foundation:** `TOWERS/Economy/Shared/CreditProduction/` adapts editable producer tiers into optional immutable Production snapshots, without pretending producers have a weapon/ammo/rank progression. Inspector upgrade comparison shows storage/payout/interval/source range, rather than projectile rows. CreditProducerTower owns collection/lifecycle/timer; ARG is event-only with no physics tick when periodic income is absent. Resource Tower has one simple gated timer callback. No per-frame tower/enemy scans, extra currency wallet or per-tower UI buttons are created. CreditBuffer gained safe capacity growth for upgrades; CP1 behavior is retained.

**CP1 status:** user reported successful manual playtesting on2026-10-18; automated CitadelCreditChecks and CitadelCollectionChecks also pass in this job. The legacy pending-status sections below are historical.

**Deferred:** Generator.2 passive ARG research (+20/10s) and New Interest Citadel research (+25%) remain planning, not silently purchased/unlocked. No research ownership/effects or campaign save format was changed. CP3's passive tower is implemented; its optional research portion remains future work. Existing maps/waves/combat stats were not edited. See `TESTS/Economy/CreditTowers.md` for validation and limits. Original plans below are retained.

---

# Resource Tower (Planned)

Economy tower that produces credits at intervals. Older GameMaker reference included a 2×2 resource tower.

Future balancing: price, cell footprint, payout amount/interval and upgrades. No implementation yet.


## Shared credit producer architecture (PLANNED)

Use [`ECONOMY/Production/README.md`](../../../ECONOMY/Production/README.md) for the reusable buffer/production design shared with the Citadel and future Adaptable Resource Generator; this passive Resource Tower's balance and trigger remain a separate future implementation choice. The old GameMaker `Resource Tower` was tiered periodic income; the childhood notes also propose a distinct kill-driven Adaptable Resource Generator. Neither is implemented here yet.
