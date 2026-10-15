# Named tower ranks and combat XP — implemented

Purchased **Level** and earned **Rank** are independent. TowerBase owns its UpgradeLevel and private TowerRankProgress. Buying a level never resets total XP or named rank, and earning a rank never buys a level or spends Credits. Standard defaults reproduce all previous baseline stats.

| Rank | Total XP | XP from previous | Damage bonus | Range bonus | Future HP bonus (inactive) |
| --- | ---: | ---: | ---: | ---: | ---: |
| Standard | 0 | — | 0% | 0% | 0% |
| Veteran | 100 | 100 | 2% | 1% | 2% |
| Enhanced | 300 | 200 | 4% | 2% | 4% |
| Superior | 700 | 400 | 6% | 3% | 6% |
| Legendary | 1500 | 800 | 8% | 4% | 8% |

These are complete **current-rank totals**, never accumulated bonuses from earlier ranks. Incremental XP requirements double (100/200/400/800). Edit each type's compact one-row-per-rank `BattleTowerRanks.cs`, `LaserTowerRanks.cs`, `BlasterCannonRanks.cs` or `ShockwaveTowerRanks.cs` beside its Levels file. Tables are immutable typed C# data, independently customizable per type. TowerRankController.Define validates ordered names, increasing finite thresholds and finite nonnegative bonuses once when each table initializes. The passive test config has only a neutral Standard row; it has no invented weapon/rank loop.

## Awarding and bounded damage XP

TowerMasterConfig / DefaultTowerConfig.tres exposes shared Inspector **Tower Experience** multipliers: KillXpMultiplier=10 and DamageXpMultiplier=2. Placement passes its actual session Settings into the tower; standalone production scenes fall back to the same default resource. Nonfinite/negative multipliers resolve to zero XP. There are no per-enemy authored XP rewards or extra wallet transactions.

An enemy snapshots its KillRewardCredits and starting hull+spawned shield on Initialize. Its finite damage budget is that starting durability. Each committed hit consumes up to the remaining budget using actual shield loss plus actual hull loss, after defense multipliers/penetration and overkill clamping. Unattributed damage also consumes budget. Restoring shields or any future healing does not refill it; shields added later do not enlarge the spawn-time budget.

At its first actual hit with a valid live source, snapshot shared XP multipliers for that enemy:

- Damage pool = snapshotted KillRewardCredits × DamageXpMultiplier.
- Damage XP for this hit = pool × eligible actual damage / starting durability.
- A lethal attributed hit additionally awards KillRewardCredits × KillXpMultiplier, once through the existing guarded death lifecycle.

A shieldless60-HP/3-credit enemy has6 damage XP and30 kill XP:12 actual damage gives1.2 XP, an overkill finishing the remaining48 gives4.8 damage XP+30 kill XP. A100-shield/60-hull spawn divides the same6-XP pool across160 starting points. Repeated restoration cannot create more than6 damage XP. The first40 actual shield loss gives1.5 XP. Independent shield/hull coefficients and penetration remain in COMBAT/Damage; XP counts their resulting point loss, not raw attempted damage. Zero-credit enemies grant zero XP. Rules/reward edits affect new instances/encounters rather than retroactively expanding an already captured budget.

The existing player Credits economy and EnemyKilled registry event remain unchanged. Arrivals, manual/debug kills, misses, blocked hits and dead/queued/unregistered targets do not grant tower XP. No last-attacker fallback is used: only the source on the actual lethal hit receives kill XP. Freed/unloaded sources receive no XP, though already-fired attacks retain their combat delivery.

## Cache, events and representation

TowerRankProgress uses double XP with compensated fractional summation. Positive finite awards can cross several thresholds; a large lethal award can reach Legendary at once. Total XP is retained at maximum rank. No XP/rank processing runs per frame.

Stage02 selects the complete explicit Level row. Active Stage03 then applies only the current Rank's damage/range percentages. Projectile integer damage rounds once to nearest/ties-to-even (Level3 Battle22×1.02 rounds22; Legendary22×1.08 rounds24). Beam DPS and shockwave damage remain float. Range scales acquisition range and Shockwave's maximum radius; Blaster blast radius/falloff, speed, intervals, coefficients and penetration do not gain unrequested rank bonuses. HP bonus is reserved row metadata; no tower health/ability system exists. The named rank identity/data can later key unique abilities without introducing an unused ability engine now.

Ordinary XP awards emit ExperienceChanged only. Promotions rebuild cached FinalStats once and emit StatsChanged, then ExperienceChanged. Paid previews call the same ranked resolver as application. Normal combat ticks reuse FinalStats. No pipeline stage spends money or owns attribution.

The existing bottom-right Inspector shows named Rank, within-rank XP progress/bar, total XP and active Damage/Range bonuses. At Legendary it shows MAX RANK. A Veteran with120 total XP shows20 /200 XP to Enhanced. Selection subscribes to that actual tower's events; changing selection/unloading unsubscribes. Ordinary XP updates are deferred/coalesced per selected frame and preserve upgrade details. Promotions update combat/preview immediately. Build previews hide rank details. No new window, polling loop or Rank Up button was added.

TowerRankChecks runs in EnemyIntegrationChecks and covers actual/fractional XP, all four production attack deliveries, competing attackers, splash recipients, shields/restoration, overkill, invalid/zero/manual/arrival cases, weak-source lifetime, named thresholds/multiple promotions, type-specific tables, Stage03/order/rounding, explicit level/prices, purchases, Inspector events/layout/MAX and500 recipients. See TESTS/README.md for executed results. Research, saving, tower HP and rank abilities remain deferred.
