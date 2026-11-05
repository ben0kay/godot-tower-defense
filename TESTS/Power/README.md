## PW4-C — distance-first scarce-supply allocation — 2026-11-05

**Source Implemented / Awaiting Owner Validation.** The existing shared capacitor service now allocates finite island generation in ascending cached conductive-path distance, breaking ties by ordinal Tower SaveId. Every request is capped by eligible elapsed recharge at its PW4-B ceiling and remaining capacitor headroom. Unused energy goes immediately to later requests; zero supply/route/ineligible state delivers zero. Stored energy, shot spending and NoPower recovery remain independent. No priority slider or bias, new consumer, relay, balance change or save schema is introduced.

Allocation order is cached across service ticks. Membership, consumer remapping or delivery revisions invalidate it; a single reused list sorts only when dirty, with no delivery/BFS work inside comparisons. Sparse per-island budgets, request snapshots and list capacities are reused. Existing source/topology/config flush boundaries are retained; work eligibility transitions also flush the prior interval before changing charge state. Pause advances neither the shared clock nor allocation. Credits finish before lifecycle/publication callbacks run.

`TowerPowerConsumer.LastDeliveredRefillPerSecond` reports actual credit divided by the **latest completed service interval**, independently of `RechargeCeilingPerSecond`. It is derived, resets on restore/detach, and is not saved or presented as new Inspector UI in C. It is a historical service sample (can remain nonzero when the service sleeps after filling), not an instantaneous generation promise. PW4-D owns final live rate/status presentation. `AllocationOrderRebuildCount` permits a focused cache check without adding a debug UI poll.

Equal-distance ties are deterministic, not fair-sharing: an earlier SaveId can consume all remaining supply while it keeps requesting; later equal-distance towers wait until output/headroom permits. A bounded equal-distance fair policy remains a future decision if playtesting demonstrates a problem. Farther groups do not jump ahead.

Focused `PowerAllocationChecks` is called by the existing optional PowerNetworkChecks fixture: scarcity/abundance, leftover headroom, finite conservation, dormant priority values, stable saved identity, actual-versus-ceiling, cache reuse, source loss/restoration, disconnected stored-energy spending and pause. Authored but not executed here. Source review only; .NET/Godot unavailable, no benchmark or broad tests. Owner: rebuild, test a low-output source with near/far Battle Towers, let near buffers fill, then inspect source removal and a fresh Save/Load. Existing content-fingerprint compatibility guard remains unchanged.

---

## PW4-B focused owner check — 2026-11-05

**Updated fixture authored; not executed here.** Execution environment unavailable; source/resource review only. No SDK installation, large suite or benchmark. After rebuilding in Godot .NET, optionally run `PowerNetworkChecks.tscn` once.

`PowerDistanceChecks.Run` checks a small conductive detour vs straight-line gap, zero-distance whole source footprints, duplicate/multiple anchors, invalid footprint, diagonal isolation, last-source clearing and curve/input bounds. The existing synthetic transformed-grid fixture adds two real Battle capacitors: local vs distance-limited refill, unchanged capacity/stored charge on outage, zero-output source exclusion, consumer relocation, closer-source recovery, batched rebuild count and no BFS for positive output/curve edits. Existing terrain/island/accounting assertions remain. No source maps, waves, shipped balance or profile files are edited by the fixtures.

Owner: compare completed Battle Towers at near/far conductive distances; both can eventually reach full charge while idle with a viable supply. Test a winding route, disconnected island and generator disable/removal/replacement; charge stays usable but refill ceiling updates. Check terrain bridge/Lava changes, multi-cell generator placement and a new between-wave Save/Load. Inspect shared `DefaultPowerDelivery.tres` (4 full cells / 12 falloff / 0.10 floor) and retain current Battle capacity/draw. Public consumer distance/efficiency/ceiling are upper delivery limits; actual refill also shares the finite production budget. No priority scheduling or relay expected yet.

Performance checks here are source-level design checks only: one lazy shared BFS with reused arrays, consumer revision caching, no per-tower search or per-frame map scan. DistanceRebuildCount and LastDistanceRebuildMilliseconds allow owner-side observation if desired; no FPS/timing improvement has been measured or claimed.

---

## PW4-A focused owner check — 2026-11-05

Source/resource/diff review only; no .NET build, runtime suite or performance run here. No tool installation. Rebuild C# in Godot .NET, then briefly check:

1. Complete a Battle Tower on a supplied island: energy starts empty, charges at up to 10/s and resumes at 15/100. Other tower types have no capacitor/gate.
2. Sustained actual shots spend 12 × firing interval each; seeking/cooldown/no ammo never spend active energy. Energy can deplete/recover; no projectile/ammo is emitted/spent on an unfunded shot. NoPower must not cancel the last successfully paid launch.
3. Disconnect/remove the source: stored energy stays usable, refill stops. An isolated island cannot borrow another island's generation. Combined delivered refill remains bounded by available generation; priorities have no effect until C.
4. Heat/EMP retain their independent handles; charging can recover power while these remain disabled. Construction/upgrades keep their old clocks, preserve charge and neither charge nor drain until complete.
5. Make a new between-wave Save/Load with partial charge, also with recovering power and heat/EMP. Compare charge, ammo and work; no free initial fill or stale/duplicated NoPower. Current compatibility fingerprint can reject earlier saves.

The runtime label shows energy quantity. The heat/ammo-style energy bar is deliberately PW4-D. No new per-tower processing/node, distance calculation, priority sorting or relay code belongs to A. Existing PW1 fixtures with **no consumers** should still see a dormant manager. Earlier checks below describe their named historical phases.

---

## PW3 generator checks — 2026-11-04

`PowerGeneratorChecks.tscn` is an optional small fixture using production placement/work/checkpoint seams. It covers the 300-Credit purchase, unfinished zero output, completion to 250, center-based 2×2 membership, removal, shared EMP/recovery, private config output, lethal damage and unfinished/completed restore without repurchase/duplicate generation. It does not write profile saves or run stress simulations. **Authored but not executed here**: Godot/.NET is unavailable. Only focused source/resource checks were performed; no broad suite or benchmark.

Owner: rebuild C#, place Industrial from Economy, inspect 0/35/70% artwork/work, and watch F4 local supply increase only on completion. A normal grounded 100-power Citadel plus one complete Industrial on the same island totals 350; an isolated Industrial supplies only its own island. Inspect selected runtime generation text. Check a fresh Save/Load/Continue with unfinished and completed generators. The selected consumer power bar is PW4 work and is not expected yet.

---

## PW2 additions — 2026-11-04

Focused source checks were extended for stable-ID producer updates without duplicates, requested demand, per-island split/remapping, unassigned supply, idempotent removals and invalid amounts. CitadelPowerConfig emits one Changed signal per actual output change. No fixture/build/runtime tests were executed here (Godot/.NET unavailable), and no benchmark or broad regression run was attempted.

Owner check: rebuild C#, hover the Citadel's island in F4 and expect 100 supply / 0 requested for one normally grounded Citadel. Isolated islands have no supply unless they contain their own Citadel; unassigned source count explains missing/insulating ground. Edit DefaultCitadelPower.tres Output and restart (or edit the bound resource live) to see the change. Check source removal on Citadel death and a fresh Save/Load/Continue for no duplicate output. This is capacity accounting, not stored energy. Demand 0 is expected until PW4. Existing strict checkpoint fingerprint rules remain.

---

# Power foundation checks — PW1

**Source checks authored; Godot/.NET execution pending.** No runtime, timing or FPS result is claimed.
No tooling installation, broad test suite or prolonged simulation is needed for this pass.

After rebuilding C# in Godot .NET, run `PowerNetworkChecks.tscn` once if desired. It exercises the
pure region cache plus one tiny disposable world using production WorldGrid and TileSet resources:

- Four-neighbour connectivity, no diagonal or row-wrap joins, merges, splits, holes, empty maps,
  deterministic snapshot IDs and safe bounds/multi-cell footprint queries.
- Floors, nonbuildable walls, raised Foundation and insulating Lava; normal layer precedence;
  decorations/empty space excluded; transformed grid-to-world coordinates.
- Citadel reservations/tower occupancy never erase land or rebuild power islands. Explicit
  unchanged terrain rebuilds do not recompute topology or invalidate navigation.
- Network change notification and disabled manager frame/physics processing; fixture teardown
  frees the whole world and unsubscribes the topology manager. No gameplay scenes are repainted.

The existing `TESTS/Debug/PerformanceOverlayChecks.cs` now expects the two additional F4 rows;
the old profiling rows retain their indices/behavior. That fixture was updated, not executed here.

## Brief owner check

1. Run a normal Campaign battlefield (or Main launcher F6), press **F4** if hidden. Inspect
   **Power islands / cells / rebuild** and **Power hover** beneath the existing diagnostics.
2. Move the mouse across disconnected islands, walls, Foundation, space and decorations. A positive
   hovered island ID identifies conducting terrain; 0 means none. Diagonal-only contacts stay separate.
3. Place/remove towers normally: the power rebuild counter must remain unchanged and combat must
   work as before. F4 hidden stops its diagnostics; the manager itself never processes per frame.
4. For topology edits, paint a separate test map or use the fixture. A bridge joins islands and
   erasing it splits them; runtime paint changes require `WorldGrid.RebuildTerrain()` as usual.
5. TileSet Inspector → Custom Data exposes Boolean **conducts_power**. Current solid land is true,
   Lava false; existing can_build/blocks_enemies/interaction values and source IDs are unchanged.
   Restart after authored edits; raw map-only F6 intentionally has no gameplay/F4 services.

PW1 neither generates nor consumes electricity. Citadel baseline output is recorded for PW2.
Topology labels are derived and are not saved. The existing content fingerprint may reject older
checkpoints after this source/resource update; save validation itself is unchanged.

