# Typed damage — Phases 4C-1 and 4C-3 implemented

`DamageInfo` is an immutable validated base hit with a damage type, independently optional absolute shield/hull coefficient overrides, and a shield penetration fraction (default 0). Existing two-argument callers remain valid. Null overrides inherit the type default; explicit zero overrides remain meaningful. Negative/nonfinite amounts or overrides, unknown types, and nonfinite/out-of-range penetration are rejected before target state changes.

## Defaults and weapon authoring

`DefaultDamageProfile.tres` remains the shared fallback, cached by DamageResolver:

| Type | Shield coefficient | Hull coefficient |
| --- | --- | --- |
| Kinetic | 0.25 | 1.00 |
| Energy | 2.00 | 0.85 |
| Explosive | 0.25 | 1.25 |

Per-hit overrides **replace**, rather than multiply, the respective fallback. Laser's 6.00x override is exactly 6.00x, independent of Energy's 2.00x fallback. Disabling either Laser opt-in flag independently restores that type default. Future Energy Cryo/Stasis weapons retain the Energy defaults unless they author their own overrides.

LaserTowerLevels.cs contains per-level opt-in flags and finite nonnegative coefficient values; the selected snapshot owns the beam response. Authored Laser balance is 12 base DPS, shield override6.00x, hull override0.15x:72 shield points/s or1.8 exposed hull/s while aligned. Construction remains250 Credits; its existing175-Credit tier adds10 base DPS and32px range, yielding22 DPS/288px. BeamRenderer remains visual-only.

BattleTowerLevels.cs explicitly includes ShieldPenetration=0.05 in every authored row. It flows through the single eight-stage pipeline into cached TowerFinalStats, is clamped to finite0..1 at Stage 07, and is snapshotted in the guided projectile's DamageInfo. Battle's12 damage/shot and12→16→22 tier damage progression remain intact. Its200 construction and150/250 upgrade prices are unchanged. All purchased levels are now complete C# snapshots; obsolete level-specific upgrade assets/references have been removed. All other hit/stat constructors default to zero penetration. Passive towers still need no weapon capability or stats.

## Resolution math

For base damage D, effective hull coefficient H and penetration p:

1. With shields present, split direct-hull base P=D×p from shield-facing base R=D−P.
2. Direct hull damage is P×H. Shields absorb R in base units: consume min(R, shield/shieldCoefficient), then convert unspent base into hull damage using H.
3. A zero shield coefficient blocks all ordinary shield-facing base without damaging the shield. Explicit bypass still damages hull.
4. Without shields, the entire D×H damages hull regardless of penetration. Penetration redistributes the base amount and never grants extra base damage.

Double intermediates prevent overflow; resulting shield/health remain finite and nonnegative. Shield and hull remain separate state. `Enemy.TakeDamage(float)` retains exact legacy1:1 shield-first behavior with no type coefficients or penetration. Hull damage alone drives the existing hull feedback/death/reward lifecycle; repeated hits/arrivals cannot double-pay.

| Fixture (60 starting hull) | Resulting shield | Resulting hull |
| --- | --- | --- |
| Battle12,100 shield,5% penetration | 97.15 | 59.4 |
| Battle12,no shield | 0 | 48 |
| Laser12,100 shield,6.00/0.15 overrides | 28 | 60 |
| Laser12,30 shield,6.00/0.15 overrides | 0 | 58.95 |
| Laser12,no shield,6.00/0.15 overrides | 0 | 58.2 |

## Inspector, ownership and scope

The placed Battle Inspector displays `Shield Penetration: 5%` at Level1/2/3 and refreshes on instance config edits. Laser displays cached effective base DPS and its effective shield/hull coefficients. Upgrade previews use the same resolver as paid application. Config changes invalidate once; ordinary physics ticks reuse cached final stats.

Permanent research is **not implemented**. Stage 04 remains neutral. Future ballistic research belongs to persistent progression, must affect eligible new/existing towers and invalidate their caches; it must not be stored on purchased tower tiers or level configs. Saving, armour, shield regeneration, tower shields/durability, shield HUD bars, enemy-specific resistances and unrelated mechanics remain deferred. Cosmetic hit/break aura for existing enemy shields is separately implemented in [FEEDBACK/Shields](../../FEEDBACK/Shields/README.md); it does not change this resolver.

## Regression coverage

DamageTypeChecks covers unchanged inherited defaults, independent/zero overrides, malformed hits/state, penetration partial/full/exact/no-shield cases, zero shield coefficient, overflow bounds, real guided Battle contact, hull feedback, target isolation and once-only deaths/rewards. LaserTowerChecks covers authored12 DPS,72 shield/s and1.8 hull/s at30/60/120Hz,30-shield spillover, override opt-out, visual independence, targeting/cleanup, construction and22-DPS upgrade parity. TowerStatChecks/TowerUpgradeChecks exercise safety clamps, eight stages, cache/input isolation, selected Inspector updates and unchanged5% penetration through purchased tiers.

See TESTS/README.md for actual compilation/headless results and fixture isolation. Manual rendered beam quality is not asserted by headless tests.

## Phase 4D area consumer

AreaDamage.AtDistance creates a scaled base DamageInfo preserving its type, independent nullable overrides and penetration. The instant helper calls Enemy.ApplyDamage once per snapshotted recipient, so existing shield-first base absorption, bypass, hull feedback and kill/reward isolation remain authoritative here. Blaster defaults to Explosive 0.25/1.25 and zero penetration; optional weapon overrides are launch-snapshotted. No direct-hit bonus or second coefficient application was added. AreaDamageChecks and BlasterCannonChecks exercise both inherited and explicit responses, including zero shield response with penetrating damage.

## Optional owner attribution

DamageInfo additionally carries an optional DamageSource token. It does not affect DamageResolver coefficients, penetration or raw damage math. Production weapon hits supply their originating tower's cached weak token; AreaDamage falloff preserves it for every recipient. Enemy commits actual shield/hull loss and a guarded kill, then credits bounded combat XP through that source. Existing unattributed constructors/TakeDamage keep their old combat/Credits behavior and grant no tower XP. See COMBAT/Attribution/README.md and TOWERS/Shared/Ranks/README.md for budget/lifetime rules.
