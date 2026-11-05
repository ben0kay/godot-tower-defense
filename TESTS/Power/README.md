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
