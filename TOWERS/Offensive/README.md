# Offensive towers

## Range rebalance — 2026-10-17

Every authored level of the seven current **offensive** towers has approximately **15% more attack range**, rounded to whole pixels; the values remain absolute per level. Battle/Laser/Blaster: 294px level 1, 304px level 2 (Battle level 3: 313px). Cryo/Machine Gun: 239/248/258px; Sniper: 515/524/534px; Shockwave **maximum damaging radius**: 230/239px. All existing earned rank percentages apply afterward through Stage03. The Inspector, upgrade previews and selected range indicator use the unchanged cached `FinalStats.RangePixels` source.

**Not changed:** damage, cadence, aim, projectile speed, credits, work time, ammo, heat, Blaster Cannon splash radius/falloff, enemy/wave properties or future passive/support ranges. This is an authored-balance adjustment, not an added global range modifier.


The default catalog contains BattleTower (guided Kinetic projectile), LaserTower (sustained Energy beam), BlasterCannon (Explosive instantaneous area projectile) and purple ShockwaveTower (timed expanding Energy front). Targeted weapons reuse shared targeting/aiming; all reuse generic catalog-driven placement/HUD, canonical ResourceCost recipes and the same eight-stage cached stats/paid upgrades.

COMBAT/Damage owns shield/hull responses, optional absolute weapon overrides and penetration. COMBAT/AreaDamage owns instant-disc selection and weapon-input distance falloff. SplashProjectile delivers Blaster's launch-snapshotted area payload exactly once at impact; FEEDBACK/Shockwave is cosmetic only. See BlasterCannon/README.md for current values and tests.

Missile Tower, armour, terrain-blocking line of sight and enemy timed status effects remain deferred. **HD1/HD2 shared disabling and opt-in heat are implemented**; Battle now has a3min sustained-fire profile with9s recovery; other defaults remain heatless. See [below](#shared-tower-heat-and-disabling--hd1hd2). The implemented Phase4E ShockwaveEmitter damages only when its advancing front reaches an enemy and shares the visual effect with Blaster. It uses its own explicit C# levels and cached optional ShockwaveStats.


## Sniper and Machine Gun — new projectile specializations

- `SniperTower/`: long-range, slow-firing, high-per-hit Kinetic precision tower (1 muzzle; 80/88/96 damage at levels 1–3). Registered in `DefaultTowerCatalog.tres` as `sniper`.
- `MachineGunTower/`: close-range high-cadence Kinetic tower with **two alternating muzzle markers**; the authored fire rate is **total bullets/s**, not bullets/s per barrel. Registered as `machine_gun`.
- Both reuse `Shared/Weapons/MultiBarrel/ProjectileTurretBase.cs` for targeting/aiming/projectile dispatch, and `BarrelFireController.cs` for `Single`, `Sequential`, and `Simultaneous` firing modes. No new weapon-wide polling or changes to existing offensive tower attack loops; details in `Shared/Weapons/MultiBarrel/README.md`.

**UI integration:** The existing catalog-driven CTRL tray can show these tower definitions automatically, but the live UI redesign belongs to Codex. This implementation adds only the two catalog entries; it does not change GameplayHUD, the build tray scene, or keyboard handling.

## Cryo

[CryoTower](CryoTower/README.md) is playable: Energy projectile plus permanent Slow, using shared single-barrel delivery, levels/ranks/ammunition and construction. Only unslowed, nonimmune enemies are targeted; there is no damage-only fallback.


## Shared tower heat and disabling — HD1/HD2

**Status (2026-10-17): HD1/HD2 IMPLEMENTED; HD3 Inspector presentation is implemented.** Public API, source/expiry policy, optional level authoring and runtime behavior are documented in [Shared/Conditions](../Shared/Conditions/README.md). Battle is enabled through its minutes-based config (3min,9s recovery); other tower heat settings remain TBD. The existing Ammunition Factory **production buffer** is unrelated to weapon heat. The original implementation requirements follow.

### 1. Shared, source-aware tower disabling (HD1)

- Give each placed `TowerBase` one authoritative *runtime* disabled state, independent of levels, earned ranks, current construction work and damage/stats. Expose a readable status such as `IsDisabled` and a combined ability gate such as `CanOperate` / weapon-specific `CanAttack`; implemented as ApplyDisable/RemoveDisable, CanOperate and CanAttack.
- Track **multiple concurrent disable sources**, not one Boolean. Each entry should identify both its reason and its distinct source/instance, so two overlapping enemy EMPs coexist and clearing Overheated never clears another enemy's effect. Reasons can include `Overheated`, `EMP`, `NoPower`, and future special enemy/control effects. Removing/expiring one source only enables operations when **no other reason remains** and existing construction requirements are met.
- Keep existing `IsOperational` construction semantics (it currently means not under initial construction). Do **not** replace it, mutate `FinalStats` to zero, reuse per-weapon `Enabled` flags as a global authority, or stop all physics/timers: cooling and future disable expirations must keep advancing even while attacks are blocked.
- An active disable blocks **new** target acquisition/aiming where appropriate, projectile/volley launch, Laser damage/ammo funding/visible beam, and Shockwave emission. Any projectile or wave **already launched** continues normally. Support and economy buildings should later consult the same gate for their own work, when their disable behavior is explicitly specified.
- Treat timed enemy debuffs and future power outages as *different producers of disable sources*, not independent competing weapon shutdown implementations. Define expiry, source removal/unload and overlapping-effect cleanup without introducing per-effect Godot nodes or per-frame scene-tree scans. Publish state-change events for presentation/UI rather than polling constantly.

### 2. Optional per-tower heat/overheat capability (HD2)

- Allow any offensive weapon to opt into heat; towers without heat remain unchanged but can still be disabled by other sources. Author **absolute per-purchased-level** heat settings in a small optional typed group (analogous to `AmmunitionStats`): `Capacity`, `HeatPerSuccessfulShot` or `HeatPerVolley`, `HeatPerActiveBeamSecond`, `CoolingPerSecond`, and `RestartBelowFraction`. Do not invent a universal heat rate for passive structures. Specific balancing values and whether particular towers opt in are **TBD**.
- Store `CurrentHeat` and an overheat latch on each **placed instance**, not in shared `.tres` resources, the global stat pipeline or static level rows. Upgrades/rank changes must not silently reset heat; when effective capacity changes, clamp documented runtime state rather than refill it. Future research/active modifiers can affect effective heat stats via the existing cached pipeline instead of creating another pipeline.
- Generate heat **only for successfully executed attacks**: Battle/Blaster/Sniper/Cryo on launch; Machine Gun according to actual bullets in a committed volley (not merely number of barrels); Shockwave on emitted pulse; Laser proportional to *funded, damaging beam time*. No heat for seeking/aiming, unfunded ammo, missed cooldown opportunities or idle targets. Resolve an edge case where a final successful attack reaches 100% heat: commit that attack once, then disable subsequent attacks.
- Cool at the defined passive rate while the game runs, including while overheated or disabled by another reason. At **100%**, add this tower's own `Overheated` disable source. Retain it until heat falls to/below the authored restart threshold (illustrative **35%**, not approved balance); then remove **only** the overheat source. Hysteresis prevents firing at nearly full heat and immediately retriggering. Godot pause freezes both heat generation and cooling with ordinary game time.
- Do not tie heat to ammo reserve, Factory stock/buffer, magazine reloads or electricity. Do not mint/refund ammunition or undo attacks when heat trips. Preserve outstanding projectiles, damage attribution, tower Rank XP and current construction/upgrade policy.

### 3. Compact Inspector and verification (HD3)

- Show a modest `HEAT current / capacity` bar and `OVERHEATED / COOLING` state for heat-enabled towers, plus concise active disable reasons (e.g. `EMP + OVERHEATED`). Towers without heat should not show a fake heat meter. Use event-driven or bounded updates, preserve the current **620×330 nominal non-scrollable** Inspector and verify small viewport readability rather than adding a ScrollContainer.
- Test pure heat math (shot, volley, beam, pulse, cooling, threshold, pause, large delta, zero/invalid settings), successful-attack-only charging and all seven currently registered offensive types: Battle, Laser, Blaster, Shockwave, Sniper, Machine Gun, Cryo.
- Regression-test overlapping independent disable sources, source expiry/unload, overheat recovery under continuing EMP/no power, construction and paid upgrades, target/beam/aim cleanup, ammo conservation, rank promotions, existing travelling shots/waves, teardown, direct-scene fixtures and selected-Inspector updates. Run focused Godot/headless checks **and** leave actual desktop visual/playfeel QA explicitly pending until performed.
- Keep logic lightweight: compact per-instance state, no extra all-towers-per-frame scan and no stat-pipeline rebuild per shot or cooling tick. Use existing attack loops and shared helper(s) rather than seven divergent heat implementations.

**Scope boundary:** HD1/HD2 were approved and implemented; HD3 presentation is implemented. No tower HP/shields, enemy EMP attack, electricity network, cooling VFX/audio, save/load, or balancing decisions are included. Shared disabling should be established before those eventual mechanics depend on it. For backlog tracking, see [NOTES/WORK.md](../../NOTES/WORK.md).
