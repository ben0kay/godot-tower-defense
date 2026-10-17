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

Rank promotions/purchases retain absolute heat. Capacity reductions clamp and may overheat; increases retain the existing latch until the new restart threshold permits recovery. Removing the heat capability clears only its own latch and resets heat. Heat/timer updates never rebuild FinalStats. `CurrentHeat`, `IsOverheated`, `FinalStats.Heat` and `HeatChanged` are ready for HD3. Shots publish heat changes; cooling notices are limited to about10Hz, plus threshold/state changes.

## Remaining work

**HD3 is deferred:** no heat bar, disable-reason label, heat upgrade-comparison rows, cooling artwork or Inspector layout changes. No save persistence, EMP attack, electricity, tower HP/shields or abilities were introduced. Explicit clock stepping through `AdvanceConditions` supports tests; normal gameplay uses the shared pause-aware physics clock.

Verification: [TowerConditions.md](../../../TESTS/Towers/TowerConditions.md).
