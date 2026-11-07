## Idle draw tuning — 2026-11-06

Battle now uses 0.10 energy/s while idle. All other existing idle profiles doubled to 0.20–0.40 energy/s. Five idle Battle Towers draw 0.50 energy/s total once operational. Active draw still replaces idle draw. The rollout table below records the earlier settings.

---

## Power resource ordering hotfix — 2026-11-06

**Source fixed; in-game confirmation pending.** User evidence showed Battle and Corruptor with live ENERGY meters, while Sniper, Blaster Cannon and Laser had no meter and a disconnected Sniper fired freely. The cause is the order of properties in eight authored `Default*Config.tres` resources: `Power = ExtResource("power")` was serialized **before** `script = ExtResource("1")`. The only two working offensive towers, Battle and Corruptor, had `script` first. Without a recognized `Power` on the loaded scripted resource, `TowerBase.SynchronizePower` constructs no `TowerPowerConsumer`, and the shared `TryFundProjectileAttack`/beam funding fallback treats the weapon as unpowered.

**Correction:** moved `script` to the first property of `[resource]` in Sniper, Machine Gun, Rocket, Laser, Cryo, Stasis, Shockwave and Blaster Cannon default configs. `Power` references and numerical parameters were **not changed**; Battle, Corruptor and Restoration were already correctly ordered and are untouched. No shared power algorithm, combat code, save code or UI code changed.

**Resource-authoring rule:** for scripted Godot `.tres` files, put `script = ExtResource(...)` first under `[resource]`, **before custom exported fields**, so Godot has the script while applying those fields. When reviewing future authored power rollouts, verify the runtime Inspector ENERGY meter appears rather than merely checking the text `Power =` in the source resource.

**Light verification:** checked those eight `.tres` files all now put `script` before `Power`, have a matching power `ext_resource`, and retain their remaining properties byte-for-byte. No Godot runtime/build test claimed. Owner: pull `dev`, reimport/rebuild and deploy **fresh** Sniper, Blaster and Laser towers; verify ENERGY 0/capacity until connected, no free disconnected shots/beams, and normal recharge/combat when connected. Verify Machine Gun, Rocket, Cryo, Stasis, Shockwave too. Restart the map rather than using a pre-fix checkpoint.

---

## Consumer drain verification — 2026-11-05

Confirmed all nine offensive defaults and Repair reference their per-type Power resources and use the shared attack/work funding paths. Eight non-Battle offensive types plus Repair already have nonzero idle and active draw; no duplicate implementation or balance change was needed. Battle intentionally retains 0 idle / 12 active as the unchanged reference. Idle drains only while operational outside a paid active window; active replaces idle. Incoming recharge may keep stored energy full despite consumption. Inspect the existing selected **Use** and **Refill** rates, or remove supply to see depletion. Source checks only; owner gameplay validation pending.

---

## Offensive and Repair power rollout — 2026-11-05

**Source Implemented / Awaiting Owner Validation.** All nine implemented offensive types plus Restoration Tower now use the existing `TowerPowerConsumer`, `PowerCapacitor`, `PowerGridManager` and owned `NoPower` handle. No additional power clock, per-tower polling, source, relay or priority slider is added. Existing selected ENERGY bar and PW4-D actual refill/limit/use/distance/status details automatically apply to every consumer. Earlier Battle-only phase notes below are historical.

| Tower | Capacity | Nominal recharge/s | Idle/s | Total active/s |
| --- | ---: | ---: | ---: | ---: |
| Battle (unchanged reference) | 100 | 10 | 0 | 12 |
| Machine Gun | 100 | 10 | 0.15 | 10 |
| Sniper | 100 | 10 | 0.10 | 8 |
| Cryo | 100 | 10 | 0.10 | 8 |
| Stasis | 100 | 10 | 0.10 | 8 |
| Blaster Cannon | 120 | 12 | 0.15 | 14 |
| Rocket | 120 | 12 | 0.15 | 14 |
| Laser | 100 | 10 | 0.20 | 12 |
| Shockwave | 120 | 12 | 0.20 | 14 |
| Restoration / Repair | 100 | 10 | 0.15 | 8 |


These are modest provisional rates, not final balance. Each type's `Default*Config.tres → Power` references its own `*Power.tres` beside the config; Battle retains `BattlePower.tres`. All start empty with 15% restart. Config/private-instance cloning already preserves independent profiles; capacity, recharge, idle/active draw, creation fill and restart remain Inspector-editable. Rates stay separate from combat/rank stats and currently remain the same across paid levels.

### Payment and lifecycle

- Projectiles and instant-target shots prepay total active draw × firing interval before launch. Shared multibarrel weapons fund the whole volley once using `count / ShotsPerSecond`, with atomic ammunition consumption and power rollback on rejection. No per-barrel duplicate energy payment; heat, recoil, audio, cooldown and damage retain their existing owners.
- Shockwave pays once per emitted pulse using its interval. Its existing automatic emission even without targets is unchanged; travelling fronts/splash victims never pay again. Already-launched attacks continue independently of later source disable/destruction.
- Laser calls the shared `FundPoweredWork` from its existing damage tick: heat-limited duration is capped by available energy **before** beam ammunition is bought. Only the resulting funded duration deals damage/records heat. Repair uses the same seam capped by delta and actual missing hull / repair rate; seeking, aligning and a full target incur no active payment. No new repair heat/ammo behavior is introduced.
- The existing paid active-time remainder suppresses idle draw during paid work windows, so active replaces idle rather than adding to it. Only operational time outside paid windows incurs idle draw. Payment is recorded once in existing service samples. No global wave-active drain is used.
- Power depletion is published after the paid attack/work is committed, so NoPower callbacks cannot clear a lock before delivery. Existing hysteretic recovery and independent heat/EMP/work disables remain authoritative. Timed construction/upgrades preserve charge and neither charge nor spend; their clocks remain unchanged. Built disabled towers can recharge without idle draw.
- The existing shared finite distance-first allocator, cached conductive distance/efficiency, pause rules, event cleanup, capacity-preserving config edits/upgrades and save state (including paid remainder) are reused. No save schema or compatibility bypass. The existing content fingerprint can reject pre-change checkpoints; validate with a fresh save.

### Minimal validation / owner handoff

Focused source/diff and resource-wiring checks only; .NET is unavailable here. No build, Godot gameplay, visual acceptance or benchmark is claimed. Owner: rebuild in Godot .NET; select a projectile, Laser, Shockwave and Repair tower, inspect charging and active/idle Use, then let stored energy deplete/recover. Check a full repair target, simultaneous volley/ammo starvation, source loss, heat/EMP, construction/upgrade and a fresh between-wave Save/Load. Preserve broad testing for owner playtesting.


---

## PW4-D — selected power feedback — 2026-11-05

**Source Implemented / Awaiting Owner Validation.** The existing right-column ENERGY current/capacity bar remains. A compact three-line **PowerDetails** label in the Inspector Main column shows **actual Refill +X/s**, **distance-limited Limit +Y/s**, **actual Use −Z/s**, conductive distance, efficiency and connection state. Tooltips explain measured versus potential rates, authored idle/total-active draw and the recovery threshold. Full, charging, awaiting refill, supply-limited, connected/starved, disconnected/no-live-source, recharge-disabled and work-paused are distinct. Recovery is shown separately; no incoming supply does not erase stored energy.

`TowerPowerInspection.cs` owns a typed derived snapshot and service samples; `PowerInspectionText.cs` owns formatting. Successful funded attack payments and settled idle energy losses are counted once; rejected ammunition and config capacity clamps are not consumption. Rates use the existing shared game-time service (default 0.25s), not an instantaneous frame estimate. Full/work/disconnected states suppress stale incoming rates; one final zero sample after activity ends lets the existing shared service return to sleep. Restore/detach resets derived samples, with no save-schema change. Pause freezes both power and its displayed samples.

UI uses the existing selected-only/coalesced RuntimeChanged path and hides for build previews/non-capacitor towers/closed inspection. No new UI or per-tower processing loop, source scan, stat-grid reconstruction or allocation change. PowerPriority remains inactive with no slider; PW5 relays and additional consumers are not implemented. Existing Inspector dimensions/fonts and targeting controls are preserved; the new Main label can wrap, so desktop geometry at small resolutions remains an owner check.

Focused PowerAllocationChecks now covers typed starved/full/disconnected samples, actual-versus-ceiling formatting, paid consumption and zero-rate dormancy. Authored but not executed here. Source/scene checks only; Godot/.NET unavailable, no layout/runtime/benchmark claim. Owner: inspect a selected Battle Tower charging, firing, full, behind other near consumers under shortage, without a source, during work/pause, then switch/close/reselect and check a fresh Save/Load. Check Heat/Ammo/ENERGY and the new detail lines at normal and small window sizes.

---

## PW4-C — distance-first scarce-supply allocation — 2026-11-05

**Source Implemented / Awaiting Owner Validation.** The existing shared capacitor service now allocates finite island generation in ascending cached conductive-path distance, breaking ties by ordinal Tower SaveId. Every request is capped by eligible elapsed recharge at its PW4-B ceiling and remaining capacitor headroom. Unused energy goes immediately to later requests; zero supply/route/ineligible state delivers zero. Stored energy, shot spending and NoPower recovery remain independent. No priority slider or bias, new consumer, relay, balance change or save schema is introduced.

Allocation order is cached across service ticks. Membership, consumer remapping or delivery revisions invalidate it; a single reused list sorts only when dirty, with no delivery/BFS work inside comparisons. Sparse per-island budgets, request snapshots and list capacities are reused. Existing source/topology/config flush boundaries are retained; work eligibility transitions also flush the prior interval before changing charge state. Pause advances neither the shared clock nor allocation. Credits finish before lifecycle/publication callbacks run.

`TowerPowerConsumer.LastDeliveredRefillPerSecond` reports actual credit divided by the **latest completed service interval**, independently of `RechargeCeilingPerSecond`. It is derived, resets on restore/detach, and is not saved or presented as new Inspector UI in C. It is a historical service sample (can remain nonzero when the service sleeps after filling), not an instantaneous generation promise. PW4-D owns final live rate/status presentation. `AllocationOrderRebuildCount` permits a focused cache check without adding a debug UI poll.

Equal-distance ties are deterministic, not fair-sharing: an earlier SaveId can consume all remaining supply while it keeps requesting; later equal-distance towers wait until output/headroom permits. A bounded equal-distance fair policy remains a future decision if playtesting demonstrates a problem. Farther groups do not jump ahead.

Focused `PowerAllocationChecks` is called by the existing optional PowerNetworkChecks fixture: scarcity/abundance, leftover headroom, finite conservation, dormant priority values, stable saved identity, actual-versus-ceiling, cache reuse, source loss/restoration, disconnected stored-energy spending and pause. Authored but not executed here. Source review only; .NET/Godot unavailable, no benchmark or broad tests. Owner: rebuild, test a low-output source with near/far Battle Towers, let near buffers fill, then inspect source removal and a fresh Save/Load. Existing content-fingerprint compatibility guard remains unchanged.

---

## PW4-C decision — proximity governs scarce electricity (2026-11-05)

**PW4-C source implemented / owner validation pending.** A capacitor's maximum refill is already reduced by PW4-B conductive distance. When aggregate demand exceeds actual producer output, PW4-C allocates the finite supply **nearer first**, using the shared cached conductive-path distance and a stable tie-break. A reachable distant tower may get zero while nearer eligible batteries charge; once nearby buffers fill, remaining power reaches farther towers. Any already-stored energy remains usable and NoPower still depends on depletion/recovery, not instantaneous allocation. Keep PW4-C inside the existing shared manager, not new per-tower processing.

The saved `PowerPriority` value (default 50) remains **inactive**. A future player slider may provide only a **small bounded preference** if later approved, not a hard override. PW4-D should show *actual received* versus *potential distance-capped recharge* without pretending a priority slider already changes supply. [Authoritative design](../../Economy/Power/IMPLEMENTATION_PLAN.md). Earlier priority-first / slider requirements below are historical.

---

## PW4-B consumer delivery update — 2026-11-05

**Source Implemented / Awaiting Owner Validation.** Capacitor consumers expose cached `ConductiveDistanceCells`, `DeliveryEfficiency` and `RechargeCeilingPerSecond`. PrepareRefill now uses nominal recharge × distance efficiency × eligible elapsed time, capped by capacity headroom and the existing finite shared island allocation. Distance never reduces capacity or attack damage/cadence, increases drain, or erases stored energy. NoPower continues to derive only from energy sufficiency/restart hysteresis, so outage/isolation does not immediately disable a charged tower.

The shared world config defaults to full efficiency through 4 cells, soft half-efficiency 12 cells beyond that, 10% floor **only for reachable routes**. No source/invalid footprint means zero. Consumers cache footprint distance at delivery revision/placement boundaries; power-only topology queries do not invoke navigation. Constructor/attack notifications do not force the shared dirty field to rebuild during source batches. The modest existing service refreshes cached delivery; public explicit delivery queries can warm a pending shared field once. Source/curve changes flush old elapsed state first. Initial/load state does not receive free energy.

No new saved delivery fields or opted-in tower types; Battle remains the baseline. Restore warms source-derived mappings before unpause. Priority scheduling remains PW4-C, rate/efficiency/priority Inspector controls remain later D, relays remain PW5. The compact Energy bar is preserved. See [shared implementation](../../Economy/Power/Shared/README.md). Source checks only; owner Godot build/runtime validation pending. Earlier dated notes below are historical and superseded for full-efficiency-only delivery.

---

# Tower capacitors — PW4-A

**Source Implemented / Awaiting Owner Validation · 2026-11-05.** Optional `TowerConfig.Power` is a private per-instance copy of `TowerPowerConfig`. Only Battle Tower currently supports funded attacks and its default config opts in through `BattlePower.tres`: capacity **100**, recharge **10 energy/s**, idle **0**, active **12 energy/s**, initial fill **0**, restart **15%**. Other tower types remain unchanged; attaching a profile to an unadapted weapon fails definition preflight rather than allowing unpaid attacks.

## Ownership and spending

- `TowerPowerConfig.cs`: Inspector-editable non-level capacity/rates, initial fill and restart fraction. Capacity must be positive; omit the optional profile for a non-power tower. Zero idle and active draw never applies NoPower.
- `PowerCapacitor.cs`: plain energy arithmetic; current energy is runtime state, never shared authored data. Config/tier/rank changes preserve absolute energy and clamp only to capacity; initial fill applies once.
- `TowerPowerConsumer.cs`: event subscriptions, cached island/state, priority (0–100, initially 50), owned NoPower handle and save seam. No per-tower node or processing callback.
- `TowerBase.Power.cs`: optional capability and reusable funded-projectile seam. Battle prepays active draw × nominal shot interval before launch. Ammo rejection rolls that reservation back. It publishes depletion after launch so target cleanup cannot invalidate the paid projectile.

Active draw is the **total** active rate, not idle plus active: each successful shot prepays one firing interval and suppresses idle expenditure for that interval. Outside paid attack time, only an operational tower incurs idle draw. Seeking, cooldown, ammo failure and a wave-active flag never create active spending. Unfinished/upgrading towers neither recharge nor spend; work clocks are untouched and stored charge is preserved. Built EMP/heat/NoPower-disabled towers may recharge and incur no idle draw. Emitted attacks remain independent.

Insufficient attack energy or an empty energy-using buffer applies one existing `TowerDisableReason.NoPower` handle. Restart requires max(capacity × restart fraction, last refused attack cost); refill continues while disabled. Only that handle is removed, preserving EMP/heat/other NoPower owners. Zero active draw still permits free attacks if otherwise operational; zero capacity is invalid. A shot costing more than capacity cannot fire until its authored settings change.

## Shared refill and saves

The existing [PowerGridManager](../../Economy/Power/Shared/README.md) runs a modest shared time-integrated service (default 0.25s) only while work is needed. Valid same-island towers use full efficiency in A; stored energy remains usable after source death/isolation. The provisional proportional allocator limits total credited refill to island production × elapsed game time and each consumer's recharge ceiling/headroom. No island storage, distance lookup, relay or priority scheduling yet. F4's older requested-demand totals remain diagnostics for that API; they are not capacitor delivery or authoritative brownout state.

Checkpoint state includes energy, priority, restart latch/minimum and unexpired paid active time. Authored capacity/rates remain in resources. Only power's exact owned NoPower handle is omitted; external NoPower conditions still persist. Restore derives the owned handle while the battlefield is paused. Missing optional power data restores empty; strict content-fingerprint compatibility remains unchanged and can reject pre-change checkpoints. Never edit an old JSON fingerprint to bypass it.

## Compact energy bar delivered (UI amendment, 2026-11-05)

The existing selected-tower Inspector now displays capacitor **ENERGY current / capacity** with its own thin bar, styled red→yellow→green by charge ratio, independent of the Heat and Ammo meters. It is event-updated from `RuntimeChanged`; towers with no capacitor and build previews show no energy bar. Inline energy quantity was removed from the runtime-status footer to avoid duplication. The Combat Rank bonus-percentages line was removed to free the vertical space; its combat effects and rank XP remain unchanged. [Inspector notes](../../../UI/GameplayHUD/Inspector/README.md).

This delivers only the **charge meter** requested ahead of full PW4-D; recharge/distance efficiency, drain details and Power Priority control remain **future phases**. The power service, save format, attack funding and capacity calculations are unchanged. Source check only; in-editor playback and layout QA pending.

---

## Remaining phases

**PW4-B:** conductive-distance refill ceiling. **PW4-C:** deterministic priority allocation replacing A's provisional sharing. **PW4-D:** extend the now-live selected Stored Energy / Capacity bar with live rates/efficiency and priority control. A adds only compact energy text to the existing runtime label. **PW5:** relays. See the [authoritative plan](../../Economy/Power/IMPLEMENTATION_PLAN.md).

Validation: focused source/resource/diff review only; no .NET toolchain is available. No runtime or performance acceptance is claimed. Owner: rebuild in Godot .NET, let a completed Battle Tower charge, sustain fire to depletion/restart, remove its source and check stored-energy use, then check a new between-wave Save/Load and existing heat/EMP gates.


