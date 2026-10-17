# Cryo Tower — CT1–CT3

Catalog ID `cryo`, Offensive, 1×1. Single rotating muzzle fires a cyan guided Energy projectile. Contact deals damage exactly once and applies permanent 33% movement slowdown if the target survives and is not immune. Shields absorb normal Energy damage but do not block Slow. Repeated Slow never compounds; shared status policy retains the strongest application.

**Targeting:** only unslowed, Slow-susceptible enemies qualify. Uses existing First priority (physical remaining route distance) by default, with the existing spatial query and staggered search interval. If none qualify, Cryo waits without consuming ammunition. Shots already in flight may hit an enemy another Cryo shot has since slowed; they deal their launched damage but cannot stack the slow. No pending-shot reservation is introduced.

## Explicit purchased levels

`CryoTowerLevels.cs` is the sole authoritative numeric source. Each row is independent; ranks are separate in `CryoTowerRanks.cs` (0/1k/3k/7k/15k XP), modifying damage/range without increasing Slow strength.

| Level | Credits | Work seconds | Damage | Shots/s | Range | Projectile speed | Slow | Ammo capacity / shot |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| 1 | 250 | 4 | 10 | 0.50 | 208 | 650 | 33% | 600 / 1 |
| 2 | 175 | 5 | 11 | 0.52 | 216 | 650 | 33% | 600 / 1 |
| 3 | 250 | 6 | 12 | 0.54 | 224 | 650 | 33% | 600 / 1 |

Original provisional damage/rate/range retained. Added construction/upgrade times 4/5/6 seconds to match current timed-work conventions. Energy uses existing physical ammo logistics, not a new power economy. New construction cannot fire; paid upgrades keep current-tier combat until completion. XP and rank survive purchases. Initial construction uses existing default visuals; no new staged construction artwork in this pass.

`SlowStats` is an optional eight-stage capability, with safety clamps and identical live/preview resolution. Inspector shows permanent Slow, Energy damage and eligibility. Launch captures resolved damage/overrides/penetration/Slow values; upgrades or source removal cannot change that projectile. Runtime config copies keep level rows private.

Shared status runtime: `ENEMIES/StatusEffects/README.md`. Stasis, timed effects, DoT, research and save integration remain deferred. Focused tests: `TESTS/Towers/CryoTowerChecks.tscn`; actual verification outcomes are in `TESTS/Towers/CryoTower.md`.

## Projectile presentation — PV1/PV2

Implemented 16px icy cyan tracer and pale cyan crystalline starburst; the existing Energy damage and permanent Slow payload are unchanged. Tower-owned presets live in `Visuals/` and are assigned in `DefaultCryoTowerConfig.tres` (Weapon Visuals). Presentation never enters level/rank stat tables. Shared behavior and authoring: [Visuals](../../Shared/Weapons/Visuals/README.md). Regression evidence: [ProjectileVisuals](../../../TESTS/Towers/ProjectileVisuals.md). Manual appearance review at normal gameplay zoom remains pending.
