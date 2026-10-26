## HD4 — additional forgiving heat profiles (2026-10-26)

The shared HD1–HD3 condition/overheat clock now serves five more default-enabled offensive presets. **Timing is a target under sustained funded attacks with continuous passive cooling**, not a guaranteed wall-clock shutdown if the target dies, ammo runs out or a tower stops firing.

| Tower | Heat target | Recovery to 35% | Source |
| --- | ---: | ---: | --- |
| Battle | 3 min | 9s | Existing, unchanged |
| Machine Gun | 2 min | 10s | Combined actual bullet rate across both muzzles |
| Laser | 3.5 min | 10s | Actual damaging/funded beam seconds; no arbitrary virtual bullets |
| Blaster Cannon | 4 min | 12s | Actual paid-level projectile cadence |
| Rocket | 4 min | 12s | Inherits Blaster Heat policy, independent Rocket config |
| Shockwave | 3 min | 12s | Actual purchased wave interval; one heat charge per emitted pulse |
| Sniper, Cryo, Stasis | Disabled | — | Existing heatless default |

Edit `HeatEnabled`, `OverheatAfterMinutes`, `OverheatRecoverySeconds` in each `Offensive/<Type>/Default<Type>Config.tres` Inspector resource; the shipped presets enable the five profiles (the Battle resource was already enabled). Private runtime copies propagate changes through the existing cached stat resolver, rank/upgrade and Inspector display. An explicitly authored `HeatStats` level row still wins over timing defaults, and disabling the timing opt-in does not erase an explicit row. The shared `SafetyClamps` stage selects shot cadence, Shockwave's actual interval or continuous beam time; no new attack manager, per-frame config recomputation, per-projectile node, always-running clock or heat mutation during save/load.

`TESTS/Towers/TowerConditionsChecks.cs` now includes focused checks for the shipped resources, selected paid levels, beam/pulse calibration, opt-out/config isolation, explicit row precedence and heatless towers. No comprehensive suite or desktop performance review was done; Godot/.NET execution remains pending in this environment.

---

## Between-wave checkpoint integration — 2026-10-26

S2/S3 now provide source-owned capture/restore seams for this feature through
`SYSTEMS/Saving/CampaignSession.cs` and feature-local saving helpers. Esc Save/Load and Campaign
Continue use the selected profile's checkpoint; combat Save explains **Unavailable during combat**.
Historical saving-unimplemented statements below are superseded. See `SYSTEMS/Saving/README.md`
for field ownership, restored cargo/builders, original reward tickets and compatibility limits.
Source/basic diff review completed; .NET/Godot build/runtime acceptance remains pending locally.

---

# Shared tower disabling and optional weapon heat — HD1/HD2

`TowerBase` owns the public API. `TowerConditions` is one lazily created child clock per affected tower, not a node per debuff, a global tower scan, or a parallel damage system. Ordinary heatless/unaffected towers have no clock node. Cold clocks and exclusively condition-based effects stop processing. Paused scene time freezes all durations/cooling. No enemy or source Node references are retained.

## Applying a generic disable

```csharp
// Independent timed pulse; persists even if its firing enemy later dies.
TowerDisableHandle emp = tower.ApplyDisable(TowerDisableReason.EMP, durationSeconds: 3);

// Optional early removal clears only this application.
tower.RemoveDisable(emp);

// Same nonzero source + reason refreshes this source's duration (replacement, not stacking).
tower.ApplyDisable(TowerDisableReason.EMP, 3, attacker.GetInstanceId());

// Sustained condition; caller removes when power returns or sustained source is destroyed.
TowerDisableHandle power = tower.ApplyDisable(TowerDisableReason.NoPower);
tower.RemoveDisable(power);
// A sustained producer may instead explicitly remove all of its own effects:
tower.RemoveDisablesFromSource(sourceId);
```

Null duration means indefinite until explicitly removed; finite strictly positive durations expire automatically. Undefined reasons/invalid durations reject. Source0 creates independent instances; nonzero source identifies the caller's logical producer and refreshes only the same reason. Use stable unique source IDs; do not reuse one ID for unrelated producers. Foreign/expired handles are no-ops. Tower exit clears its state without retaining callbacks. Source destruction does not automatically cancel a timed pulse; sustained producers own explicit source cleanup. There is no EMP enemy, power grid or source-tree polling in this pass.

`IsDisabled`, `DisableCount`, `GetDisableReason(index)` and `DisableStateChanged` expose the combined state. Reasons are extensible enum entries, not mutually exclusive tower modes. Construction's `IsOperational` retains its original meaning. `CanOperate` adds the independent disable gate; offensive weapons use `CanAttack`, which also rejects pause/unload. Local weapon Enabled flags, ammo and cooldown retain separate ownership. Economy/support production behavior under disabling remains deferred; applying a status there does not silently redefine their production policy.

All seven offensive weapons gate new acquisition/aiming/attacks. Laser clears live beam/target immediately; shared projectile turrets clear Sniper aim immediately. Cooling and expiry keep advancing while attacks are disabled. Existing bullets, Blaster impacts and travelling wavefronts remain independent. Paid upgrades and initial construction retain their existing rules.

## Optional absolute heat stats

`HeatStats(Capacity, PerShot, PerBeamSecond, CoolingPerSecond, RestartBelowFraction)` is a nullable capability on the normal working/final stat snapshots. Stage02 selects the complete row, Stage07 sanitizes invalid inputs, Stage08 publishes it; other stages preserve it and future modifiers can edit it. There is no heat-based damage-type change or separate research pipeline.

Each of the seven `*Levels.cs` row types accepts a final named `Heat` value. For example, append this to one row:

```csharp
Heat: new(100, 12, 0, 8, .35f)
```

Those are illustrative playtest numbers, not installed balance. **All existing authored rows currently retain Heat=null**, because the approved notes leave tower opt-ins and actual balance TBD. No construction/upgrade prices, rates, projectile speeds, DPS, ranges or ammo data were changed. Add complete independent Heat values to each desired row; editing one row must not affect another.

Heat is stored on the placed instance. Successful projectile launches/pulses charge PerShot; Machine Gun charges each bullet in the fully committed volley. Seeking, cooldown and ammo failures generate nothing. Laser caps funded beam time at available thermal headroom, charges PerBeamSecond only when the attack inflicts damage, and stops its beam on overheat. A threshold attack commits once before subsequent attacks are disabled. Fractional beam accumulation snaps within one-millionth of capacity to avoid float stalls immediately below full heat.

Cooling runs at CoolingPerSecond even under other disable causes. At capacity, the heat system installs its own Overheated handle. It removes only that handle at/below the restart fraction, leaving any EMP/no-power applications intact. Zero cooling is valid and requires explicit capability/balance changes to recover. Invalid/zero/nonfinite capacity makes the capability absent; invalid rates become0 and restart fraction clamps0–.99. Invalid clock deltas do nothing.

Rank promotions/purchases retain absolute heat. Capacity reductions clamp and may overheat; increases retain the existing latch until the new restart threshold permits recovery. Removing the heat capability clears only its own latch and resets heat. Heat/timer updates never rebuild FinalStats. `CurrentHeat`, `IsOverheated`, `FinalStats.Heat` and `HeatChanged` drive the selected Inspector in HD3. Shots publish heat changes; cooling notices are limited to about10Hz, plus threshold/state changes.

## Remaining work

**HD3 is implemented:** compact selected heat bar, grouped disable reasons, cached current/next thermal tooltip and lifecycle tests. Cooling artwork remains deferred. No save persistence, EMP attack, electricity, tower HP/shields or abilities were introduced. Explicit clock stepping through `AdvanceConditions` supports tests; normal gameplay uses the shared pause-aware physics clock.

Verification: [TowerConditions.md](../../../TESTS/Towers/TowerConditions.md).

## Battle timing profile follow-up

Battle is now enabled through its authored config: **OverheatAfterMinutes=3**, **OverheatRecoverySeconds=9**. Other offensive defaults remain heatless. The statement above that all defaults are heatless describes the original HD1/HD2 foundation only. Shared HeatTiming resolves once at Stage07 using the selected final shot cadence; complete explicit HeatStats rows take priority. Capacity100 and restart35% are the normalized defaults. For target T seconds, cadence R and cooling C, heat per shot is `(100 + C*T)/(R*T + 1)`; `C=65/recoverySeconds`. The initial-shot contribution and normal inter-shot cooling are included. It is a cold-start approximation bounded by discrete shot/frame scheduling, not a wall-clock disable countdown. No shots means no heat. See BattleTower/README.md for editor tuning and tests.
