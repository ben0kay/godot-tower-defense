## PW4 revised consumer contract — capacitor + distance-based refill (2026-11-04)

**PLANNED, NOT IMPLEMENTED.** The owner replaced immediate full/on-off power allocation with a **per-tower rechargeable energy buffer**. See the [authoritative PW4-A–D plan](../../Economy/Power/IMPLEMENTATION_PLAN.md). Each opted-in building will have Inspector-editable **Capacity**, **CurrentEnergy**, **RechargeRatePerSecond**, **IdleDrawPerSecond**, **ActiveDrawPerSecond** and per-instance **PowerPriority**. Stored charge can gradually reach 100% even far from a generator, provided positive net idle charging; distance limits **recharge rate only**, not storage capacity or the tower's weapon damage. Combat expenditure can outrun weak refill, eventually causing NoPower until some energy recharges. Generator death does not erase stored charge.

Use existing TowerDisableReason.NoPower with one owned handle and a configurable restart threshold; preserve EMP/heat. Refill is limited by BOTH a cached four-neighbour conductive-path distance efficiency and the same finite island generation budget; priority applies to **allocation of refill**, not instant grid on/off states. Per-instance battery charge and priority need durable checkpoint save/load; derived distribution/NoPower must be reconstructed. Non-power towers should stay unaffected.

**Inspector PW4-D:** primary Heat/Ammo-like bar is **Stored Energy / Capacity** (e.g. ENERGY 64/100), with current recharge, drain, distance efficiency, NoPower/charging state and 0–100 priority nearby. This supersedes the earlier PW4 "full/empty supplied/required" bar below, which is **historical**. No consumer/buffer/priority code is live as of PW3. Do not modify implementation based only on this guide; follow approved subpass requests.

---

## Selected-tower power bar requirement — PW4 (2026-11-04)

The owner requests a **power bar in the selected-tower Inspector**, visually consistent with existing heat and ammo bars. Show supplied versus current required power, readable `POWER 20 / 20` / `NO POWER 0 / 20` text, normal versus shortage colours, and a nearby Power Priority slider (0–100, default 50). Initial allocation is full on/off, so the consumer bar is full/empty; zero requirement needs a neutral no-draw state rather than dividing by zero. Hide it for buildings without the consumer capability. Update only on selection/power/activity changes, not an extra frame loop. Producer output is distinct from consumption; PW3 generators already describe generation in their runtime details. Implement this with PW4 consumers/allocation, not in the generator-only phase.

---

## PW0 audit / PW1 boundary — 2026-11-04

Terrain island detection is source implemented in [Economy Power](../../Economy/Power/IMPLEMENTATION_PLAN.md). Consumer code here remains planned for PW4. Reuse the existing TowerDisableReason.NoPower and owned ApplyDisable/RemoveDisable handles; do not create a second disable mechanism. Demand activity, generator accounting and consumer registration are later work; all current towers remain operational. Save priorities as durable input and rebuild derived NoPower allocation before resume. Full phase contracts and outstanding demand decisions are in the power plan.

---

# Per-building power use — FUTURE / NOT IMPLEMENTED

## Purpose
Optional **consumer-side** power capability for any placed tower/building. [Economy Power](../../Economy/Power/README.md) owns generation, connected terrain islands and priority allocation; an individual tower does not scan power sources or calculate its own grid.

## Parameters
- Fixed **IdlePowerDraw** and **ActivePowerDraw** baseline values in each type's non-level config (not its *Levels.cs rows). Buying Level 2/3 does not automatically increase electricity draw; future permanent research may change **effective** consumption without mutating base settings.
- Per placed instance: is-powered status, current operating mode and **PowerPriority** slider from 0 (lowest) to 100 (highest), starting at 50 in the existing tower Inspector. Priority is a player preference, not purchased level/rank/research.
- During shortages the shared network supplies higher-priority buildings first (initially full on/off operation, deterministic tie-breakers and anti-flicker logic). Define idle/firing behaviour and restoration with tests. Buildings with no power capability remain valid.

Keep power operational state separate from immutable cached combat stats and from the session Credits wallet; avoid required per-tower power nodes/ticks. Until a dedicated power pass, existing towers stay operational as they do now.

**Not implemented. Validate power interruption, priority, upgrades, Inspector/saves and many-tower performance later.**
