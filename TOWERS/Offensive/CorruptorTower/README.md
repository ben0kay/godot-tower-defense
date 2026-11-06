# Corruptor Tower — CTX1

**Source Implemented / Awaiting Owner Validation.** Stable catalog ID `corruptor`, Offensive, 1×1. A single violet pulse emitter uses the existing ProjectileTurretBase aiming, muzzle, straight physical projectile, ammunition, recoil, construction, power, heat, ranks, selling and checkpoint paths. Inspired by the childhood FMO role; no second combat engine.

## Provisional balance and authoring

| Purchased level | Build / upgrade Credits | Work (s) | Initial hull HP | DoT hull HP/s | Duration (s) | Total potential hull HP | Shots/s | Range (px) |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 300 | 4 | 8 | 3 | 8 | 32 | 0.60 | 256 |
| 2 | 225 | 5 | 10 | 4 | 9 | 46 | 0.65 | 272 |
| 3 | 325 | 6 | 12 | 5 | 10 | 62 | 0.70 | 288 |

`CorruptorTowerLevels.cs` owns complete absolute tier rows; `DefaultCorruptorTowerConfig.tres` exposes each tier's damage/DPS/duration/rate/range in the Inspector. Common projectile speed is 700px/s, turn 216°/s, aim tolerance 4°, staggered search 0.15s, spread ±0.5°, ammunition 600 / one per shot, and hull 1000/1050/1102.5. These remain editable in level rows. Energy hits use full shield penetration and explicit 1:1 hull response. DoT independently bypasses shields with the shared damage resolver.

Five independent ranks use 0/1000/3000/7000/15000 total XP with current modest offensive damage/range/HP/spread bonuses. Both initial damage and DoT DPS receive the existing selected-rank and captured damage-research stages; duration stays absolute. Launch snapshots preserve those values through upgrades, promotions or source removal.

`CorruptorTowerPower.tres`: capacity100, recharge10/s, idle0.1/s, total active5/s, empty initial stock, 15% restart. Shared timed heat defaults to 3.5 minutes sustained funded shots and 10s recovery to 35%; Inspector heat fields can opt out/tune. DoT ticks consume no second ammunition/power payment and generate no additional attack heat.

## Targeting and Corruption policy

One spatial visitor prefers susceptible, unaffected enemies ahead of affected enemies. Player First/Last/Closest/Farthest/Strongest/Weakest scoring and stable ties apply inside the chosen tier. Normal range/detection/terrain sight remain authoritative. Cached fallback locks remain eligible; fresh targets win at the next ordinary search. Corruption-immune enemies are excluded from acquisition, although stray physical shots may still deal normal hull damage on contact. Cryo/Stasis do not opt into fallback.

One effect per enemy. Reapplication refreshes to the new duration, retains max(old DPS, new DPS), transfers subsequent ticks to the latest successful DamageSource, and preserves accrued tick time. Latest source therefore owns the next tick, including its pending fraction. A weaker refresh never adds a second stack. Removing the source leaves damage active but existing weak ownership prevents stale/replacement XP.

The existing TimedStatusScheduler ticks each active enemy only. Normal ticks pay one second of DPS. Hitches combine due whole seconds into one equivalent DamageInfo; expiry pays any final fractional second, so potential damage is DPS × duration. Clock state commits before lethal callbacks; death/arrival/unload/reinitialization clean up. Freeze and Slow do not stop damage; gameplay pause stops the scheduler.

## Artwork, Inspector and acceptance

Native64×64 graphite metal fixed base and rotating magenta/violet emitter, small violet tracer/muzzle flash, initial0/35/70% build stages and independent L2/L3 finished/35/70% assembly art. Existing ShockwaveEffect supplies a short thin purple impact ring only on successful application. A subtle purple sprite tint composes with Slow/Freeze, damage textures and stealth; no infected-enemy GPU particles, timers or processing script.

The existing Inspector shows direct damage, DoT HP/s, duration and total potential damage with purchased-tier deltas. Normal targeting, HP/ammo/heat/energy/rank and SELL remain shared. Existing bottom-docked panel may grow to its content minimum; no extra panel or width change was added.

Source/resource/SVG XML/diff checks completed; one local SVG render inspected. .NET is unavailable, so no compilation, fixture execution, Godot visual acceptance or FPS claim. `TESTS/Towers/CorruptorTowerChecks.tscn` is a focused optional owner runner. Owner: build, place Offensive → Corruptor, verify all six priorities, hull-only hits/ticks, affected fallback/immune rejection, refresh/source XP, terrain/stealth, tiers, artwork/muzzle, power/ammo/overheat, sale/source death and fresh checkpoint restoration.
