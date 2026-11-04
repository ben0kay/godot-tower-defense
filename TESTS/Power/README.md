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
