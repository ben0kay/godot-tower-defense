# Shockwave Tower — purple travelling energy pulse

A real1×1 offensive tower registered as `shockwave` in DefaultTowerCatalog. Ctrl → Shockwave Tower → place on route-safe terrain. Generic placement, economy, build tray, selection/range ring, Inspector and paid upgrades service it without type switches.

## Explicit levels

Edit `ShockwaveTowerLevels.cs`: one complete independent typed row per level, no cumulative modifiers. The first row pays construction, subsequent rows pay for that level. Level2 is additional provisional playtest balance to exercise the existing purchase flow.

| Level | Cost | Damage/wave | Max radius | Expansion speed | Interval | Edge retention | Penetration | Type |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | --- |
| 1 | 350 Credits | 40 | 200px | 250px/s | 4s | 25% | 0% | Energy |
| 2 | 225 Credits | 44 | 208px | 260px/s | 3.9s | 25% | 0% | Energy |

Each row also explicitly declares independent shield/hull override flags/coefficients, initially disabled: inherit Energy2x shield/.85x hull. At Level1, exposed center/half/edge hull damage is34/21.25/8.5. At half range,25 base damage removes50 shield points; fractional breaks and penetration remain owned by COMBAT/Damage.

The existing eight stages publish cached RangePixels plus optional ShockwaveStats/WeaponDamageStats and penetration. There is no fictitious projectile/target/aim data in authored wave rows. Max radius, damage, speed, interval and edge fraction remain eligible for future modifiers after selected-level substitution. Stage07 clamps damage/radius >=0, speed >=1px/s, interval >=.05s and edge/penetration0–1. Research, map effects and saving remain deferred; earned ranks are implemented.

## Emission and presentation

The tower emits immediately on its first enabled physics tick, then periodically, including when no enemies are in range. Disabled or zero-damage towers do not emit. There is no target search, muzzle or rotating turret. A long hitch emits at most once and skips missed intervals; normal cadence preserves fractional time. Upgrading does not reset the existing cooldown, and already-emitted waves retain their launch inputs. New emissions use the upgraded row. Removing the source does not cancel attacks already emitted into World/Projectiles; world unload cancels them.

`TOWERS/Shared/Weapons/Shockwaves/ShockwaveEmitter.cs` owns the actual travelling damage. An enemy receives one hit only when its center crosses the moving front; later ticks do not repeatedly damage the interior. Later independent waves may hit it again. Contact uses swept radius plus sampled relative enemy motion and distance falloff at encounter, preserving low-rate delivery. See that folder's README for detailed geometry and limits.

The vector platform/core and shared visual ring are purple/lilac. Edit `Effects/ShockwaveTowerStyle.tres` for ring/glow/opacity/fade/artwork. `DefaultShockwaveTowerConfig.tres` owns identity and that style reference; `Artwork/shockwave_tower.svg` owns tower art. Each placed config/style and each active effect have private copies. Changing visual colour/width/duration/artwork cannot alter combat or invalidate FinalStats. The effect's radius is driven by gameplay, so its independent duration/radius settings do not affect this tower's travelling ring.

## Verification

ShockwaveChecks runs inside EnemyIntegrationChecks: real350/225-credit transactions, independent complete levels, max level, stat/Inspector/preview parity, cache/style isolation, emitted launch snapshots, periodic30/60/120Hz cadence, no hitch bursts, timed contact, one hit per emission, overlap, swept moving targets, terminal ticks, falloff, shields/overrides/penetration, unload/mutation safety and500 enemies. Existing Battle/Laser/Blaster, menu, navigation and placement-performance suites remain active. Headless checks cannot judge final live-window visual quality; purple procedural/vector presentation needs normal playtest review.

## Earned Rank and Experience

`ShockwaveTowerRanks.cs` sits beside `ShockwaveTowerLevels.cs` and defines all five named ranks in compact explicit rows: Standard0, Veteran1000, Enhanced3000, Superior7000 and Legendary15000 total XP. The selected rank's total bonuses are0/2/4/6/8% damage and0/1/2/3/4% range; future HP columns are inactive. Rank XP is independent of purchased level/prices and is preserved by upgrades. Stage03 modifies the selected complete Level snapshot, never accumulates earlier Rank bonuses. Integer projectile damage retains nearest/ties-to-even rounding; float beams/waves retain fractional damage.

Attacks carry the tower's weak DamageSource. Actual shield/hull loss earns a bounded share of XP derived from each enemy's KillRewardCredits; the lethal source receives kill XP. Shared TowerMasterConfig multipliers default to2× damage pool and10× kill reward. Debug/manual kills and arrivals grant no tower XP. Already-fired payloads retain combat values/source after promotion or upgrade; removed source towers do not receive XP. The bottom-right selected Inspector shows Rank, within-rank/total XP, progress bar, active bonuses and MAX RANK automatically through events. See TOWERS/Shared/Ranks/README.md for exact rules and regression results.

## Live ammunition supply

Current complete ShockwaveTowerLevels.cs rows include 400 reserve / 5 per emitted pulse (not each wavefront recipient). Defaults are provisional except Battle1000/1. InitialAmmoFraction starts full by default; empty reserves stop unpaid attacks, and paid levels/ranks never refill them. The existing Inspector displays current ammo/delivery state and changed next-level ammo values. Factory/depot drones deliver physical cargo only on arrival. See `TOWERS/Shared/Ammunition/README.md` and `TESTS/Economy/Ammunition.md`.

## Initial construction (C1/C2)

Paid placement occupies its footprint immediately and takes **5 seconds**, authored in the Level1 WorkSeconds column of this tower's `*Levels.cs`. It cannot acquire targets/fire/consume ammunition or request deliveries until complete. Completion preserves stats, level, rank and stock. Direct scene fixtures remain operational by default. Current final artwork stays visible; stage SVGs/VFX (C4), timed upgrades and a full Inspector progress display (C3) remain deferred. Upgrades are blocked while constructing, then still apply instantly. See `TOWERS/Shared/Construction/README.md`.
