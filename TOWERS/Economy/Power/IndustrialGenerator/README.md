# Industrial Generator — PW3

**Source Implemented / Awaiting Owner Godot Validation — 2026-11-04.** Available in **Ctrl → Economy** as `industrial_generator`. Uses the shared `../Shared/PowerGenerator.cs` and `PowerGeneratorConfig.cs`; later Solar/Fusion definitions can use the same implementation with their own resources/scenes/IDs.

| Provisional setting | Value |
| --- | --- |
| Output | 250 power |
| Construction cost | 300 Credits |
| Construction work | 5 seconds (after the existing builder gate) |
| Footprint | 2×2 cells |
| Hull | 1,200 HP |
| Purchased upgrades / fuel | None in this pass |

Edit **DefaultIndustrialGeneratorConfig.tres** for Output, Construction Credits, Construction Seconds and Max Health. The registered definition owns Footprint; the scene owns one stationary 112×112 Sprite2D and the standard event-driven construction presenter. Initial assembly uses 0/35/70% SVG stages, restoring finished artwork at completion. The normal build ghost, shared construction drones/hologram, selection and hull bar are reused; no particle system, animation, weapon or per-frame generator callback was added.

Generation starts only when the building is alive, fully constructed and CanOperate. Shared disable reasons (e.g. EMP), upgrading if later supported, destruction and external removal withdraw its one stable producer registration; recovery/completion reinstates it. Generation does not depend on wave activity or require fuel. It is supply capacity, not accumulating energy. All footprint cells must be on one conducting island; unassigned sources contribute zero and are counted in F4. It never provides power to another isolated island without future relays.

Tower roots are footprint centers: registration converts the current root position to its first cell center in grid-local space (distinct from Citadel top-left roots). Config, health, work, disable, terrain and spatial events update output; unchanged accounting inputs are no-ops. Private placed configs retain normal recipe copying. Edit Output on a placed instance to change only that source; edit the authored config before starting a level to change future builds. Replacing config references requires restart.

## Saving and selected Inspector

Existing TowerSaveData and placement restoration preserve the ordinary type ID, SaveId, cell, work, hull and disable state. Power registration uses `generator:` + SaveId, is rebuilt from config/state, and is removed on exit. An unfinished restored building supplies zero until completion; no second purchase, energy stock or checkpoint schema is added. Old source/resource checkpoints may still reject through the unchanged content fingerprint.

Selected-generator runtime details show **Generation: current / configured power · Island**; the configuration card shows output/cost/footprint. The requested **selected-tower power bar beside heat/ammo and priority slider** is explicitly scheduled in PW4 with actual consumers, rather than displaying a consumption bar before demand/allocation exists.

## Brief owner check

Rebuild C#, place one from Economy and inspect its construction stages/work/drones. On the same island as one normally grounded Citadel, F4 supply should rise from 100 to 350 only after completion. An isolated island with the generator has 250 local supply. Check a fresh Save/Load while unfinished and after completion; there should be no duplicate output. Disable/destroy/remove it and verify supply falls. No runtime tests or benchmark were run here; one focused optional fixture is `TESTS/Power/PowerGeneratorChecks.tscn`.
