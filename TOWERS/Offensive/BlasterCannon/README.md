# Blaster Cannon — Phase 4D implemented

A real 1x1 offensive tower registered as `blaster_cannon` in `TOWERS/DefaultTowerCatalog.tres`. Ctrl → Blaster Cannon → place on route-safe buildable terrain. Generic placement, wallet, dynamic build bar and selected Inspector service it without tower-ID branches.

## Authored playtest values

| Stat | Default |
| --- | ---: |
| Construction | 300 Credits |
| Damage type | Explosive |
| Base damage per shot | 60 |
| Blast radius | 80 px |
| Edge damage fraction | 0.25 (25%) |
| Fire rate | 0.5 shots/sec |
| Projectile speed | 500 px/sec |
| Target range | 256 px |
| Shield penetration | 0% |
| Level 2 purchase | 200 Credits: 66 base damage, 88 px blast radius and 264 px targeting range |

Level 2 resolves 66 base damage / 88 px radius / unchanged 25% edge retention. Type defaults inherit Explosive 0.25x shield / 1.25x hull. Independent opt-in coefficient overrides in each complete level row can replace either response; zero is explicit. Existing shield penetration behavior is supported and launch-snapshotted. These defaults are editable playtest balance, not resolver constants.

Edit `BlasterCannonLevels.cs` for all numeric balance and credit prices. Its complete typed rows include aiming/search, penetration, damage response and area stats. Changing row 1 never changes row 2. Add another consecutive row to extend levels. DefaultBlasterCannonConfig.tres retains only non-level identity, target priority and tracking policy; scene/style resources retain visual settings. Placed towers receive private row/recipe copies. SetLevelStats replaces any absolute runtime row and invalidates its cache; Level 1 C# accessors edit row 1 only. The selected gameplay Inspector shows actual cached base damage, radius, edge percentage, penetration, type, rate and range. Upgrade preview shows 60 → 66 damage, 80 → 88 px radius and the 200-Credit price; purchase uses the existing atomic wallet transaction.

## Stats and future research ownership

`AreaBlastStats` is an optional immutable capability on the existing TowerStatBlock/FinalStats. Blaster's type-aware adapter enters the same eight stages as Battle/Laser. Stage 02 selects the complete row with no cumulative math; Stage 07 bounds radius to finite nonnegative values and edge retention to finite 0..1; Stage 08 publishes the capability. Numeric config edits emit Changed and rebuild the selected cache/Inspector once; ordinary physics ticks read cached stats.

Stage 04 permanent research remains neutral. Future Blaster research can raise edge retention through this capability and invalidate eligible new/existing tower caches when persistence is implemented. **The authored purchased tier never raises falloff retention.** There is no research manager, fake progression or save data in this pass. Battle and Laser have no artificial AoE capability; passive towers still need no weapon fields or combat snapshot.

## Instant-impact delivery

The fixed placement root never rotates; TurretPivot holds the up-facing placeholder cannon. Shared TowerTargeting and TurretAiming provide staggered First/Closest searches and alignment gating. Firing uses the cached rate/cooldown and creates a lightweight visible SplashProjectile.

Each projectile snapshots base hit/type/overrides/penetration plus radius/edge retention at launch, keeping no mutable tower/config reference. It tracks a living target's position; death/arrival/external free captures the last valid target point and leaves the projectile traveling toward it. Scene unload cancels delivery and releases subscriptions; timeout expires without an impact. A target leaving firing range after launch does not cancel the shot. Contact is the actual destination point.

At impact the projectile dispatches one `AreaDamage.ApplyInstant` call. Enemy centers at distance <= radius are included; the primary receives only the area hit, with no additional direct damage. For 60 base / 80 px / 25% edge, center/half/edge base damage is 60 / 37.5 / 15 **before** shield/hull defenses. The helper owns math/selection, the projectile owns travel/timing, and `FEEDBACK/Shockwave/ShockwaveEffect.cs` owns the optional post-impact visual under World/Effects. Its ring animation never repeats or delays damage.

## Implemented files

- `BlasterCannon.cs`, `BlasterCannonConfig.cs`: weapon behavior and Inspector inputs.
- `BlasterCannon.tscn`, `Artwork/blaster_cannon.svg`: pivot scene and replaceable vector art.
- `BlasterCannonLevels.cs`: all actual stats and construction/upgrade prices, one row per level.
- `DefaultBlasterCannonConfig.tres`, `BlasterCannonDefinition.tres`: non-level policy/identity and registration.
- `TOWERS/Shared/Projectiles/SplashProjectile.cs`: reusable travel/last-known-point/one-shot delivery.
- `TOWERS/Shared/Stats/AreaBlastStats.cs`: optional capability in the existing pipeline.
- `COMBAT/AreaDamage/AreaDamage.cs`: stateless recipient selection and weapon-input falloff.
- `FEEDBACK/Shockwave/ShockwaveEffect.cs`: damage-independent cosmetic flash.

## Verification and scope

`AreaDamageChecks` and `BlasterCannonChecks` run in the full headless integration suite: real catalog/action/payment/selection and private data; paid preview parity; cache/Inspector invalidation; 30/60/120 Hz travel/contact; one hit per primary/nearby/edge enemy; outside exclusion; target death/free/arrival; launch snapshots after source edits; coefficient overrides/penetration; edited falloff in newly fired attacks; range-after-launch; timeout/unload cleanup; visual independence and 500-recipient impact behavior. Existing Battle/Laser, upgrades, shield emitter, waves, economy, navigation and menu suites remain active. See TESTS/README.md for actual results.

The headless run cannot verify artistic quality; the projectile and flash need a live-window visual review. No timed damage wavefront is used by this weapon. Terrain impacts, line of sight, pooling, Missile Tower, armour, permanent research and saving remain deferred. Implemented Phase4E travelling damage is defined separately in `TOWERS/Shared/Weapons/Shockwaves/README.md`.

## Phase4E visual migration

Effects/BlasterShockwaveStyle.tres now owns the orange plasma ring/glow, .22s expansion and fade. DefaultBlasterCannonConfig references it through ImpactStyle. Config copies isolate the style; SplashProjectile snapshots it at launch and passes it to the shared ShockwaveEffect after instant damage. Style duration/radius/colour edits cannot delay or repeat AoE. Shared feedback contains no hardcoded Blaster colours. The separate purple Shockwave Tower uses the same visual in externally driven mode, with damage timing owned by ShockwaveEmitter. The former BlastFlash script/UID were removed.

## Earned Rank and Experience

`BlasterCannonRanks.cs` sits beside `BlasterCannonLevels.cs` and defines all five named ranks in compact explicit rows: Standard0, Veteran1000, Enhanced3000, Superior7000 and Legendary15000 total XP. The selected rank's total bonuses are0/2/4/6/8% damage and0/1/2/3/4% range; future HP columns are inactive. Rank XP is independent of purchased level/prices and is preserved by upgrades. Stage03 modifies the selected complete Level snapshot, never accumulates earlier Rank bonuses. Integer projectile damage retains nearest/ties-to-even rounding; float beams/waves retain fractional damage.

Attacks carry the tower's weak DamageSource. Actual shield/hull loss earns a bounded share of XP derived from each enemy's KillRewardCredits; the lethal source receives kill XP. Shared TowerMasterConfig multipliers default to2× damage pool and10× kill reward. Debug/manual kills and arrivals grant no tower XP. Already-fired payloads retain combat values/source after promotion or upgrade; removed source towers do not receive XP. The bottom-right selected Inspector shows Rank, within-rank/total XP, progress bar, active bonuses and MAX RANK automatically through events. See TOWERS/Shared/Ranks/README.md for exact rules and regression results.

## Live ammunition supply

Current complete BlasterCannonLevels.cs rows include 600 reserve / 3 per projectile launch (not each impact recipient). Defaults are provisional except Battle1000/1. InitialAmmoFraction starts full by default; empty reserves stop unpaid attacks, and paid levels/ranks never refill them. The existing Inspector displays current ammo/delivery state and changed next-level ammo values. Factory/depot drones deliver physical cargo only on arrival. See `TOWERS/Shared/Ammunition/README.md` and `TESTS/Economy/Ammunition.md`.

## Initial construction (C1/C2)

Paid placement occupies its footprint immediately and takes **5 seconds**, authored in the Level1 WorkSeconds column of this tower's `*Levels.cs`. It cannot acquire targets/fire/consume ammunition or request deliveries until complete. Completion preserves stats, level, rank and stock. Direct scene fixtures remain operational by default. Current final artwork stays visible; stage SVGs/VFX (C4), timed upgrades and a full Inspector progress display (C3) remain deferred. Upgrades are blocked while constructing, then still apply instantly. See `TOWERS/Shared/Construction/README.md`.
