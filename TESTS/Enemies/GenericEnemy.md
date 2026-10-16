# Generic inherited enemy scenes — GE1–GE3

Implemented 2026-10-16. Run `Godot --headless --path . res://TESTS/Enemies/GenericEnemyChecks.tscn` with the project's compiled C# assembly.

## Coverage

The runner checks all five individual catalog scenes, exact common paths/types, absence of redundant Movement/DefenseBar overrides, independently retained Definition/texture, optional abilities only where present, defense-bar fractions, actual catalog spawn and route movement for every type. An isolated `user://` copy changes the real parent bar position and Movement priority, then loads each actual child override against it: both changes propagate while per-type data/components survive. It never writes project scenes. No editor plugin, runtime construction helper or catalog replacement was introduced.

Existing helpers cover mixed catalog validation, wave-only shields, overflow/partial damage, ShieldEmitter interval/budget/lifecycle, wave ownership/timing/RNG, portal spread/size overrides, Citadel arrivals, instant areas, beams, Blaster impact and travelling shockwaves. Fixed shield capacity and portal-offset fixtures exist only in memory and restore authored capacity afterward.

## Actual execution

Godot4.7.2 Mono, .NET8.0.31, Roslyn4.11:

- All production/test C# compiled with the matching Godot source generator and reference assemblies, no diagnostics.
- Ordinary `dotnet build --no-restore` aborts before compilation in this host's SDK CLI: `Win32Exception: Unable to retrieve the specified information about the process or thread` / `Process.GetStat` / `StartTime`. This is not reported as a successful SDK build.
- GenericEnemyChecks: exit0, including Laser, Blaster, Shockwave and area/shield defense calculations.
- DeathSpawnChecks: exit0, including both launchers, final-carrier results, wave reservations, checkpoint handoffs, shield policy, caps, lifecycle and500-active stress.
- CorridorNavigationChecks: exit0, including48 mixed movement-permission combinations, A* budget/clearance/repath and100/500-enemy fixtures.
- CheckpointChecks and ResultsChecks: exit0.
- Unmodified EnemyIntegrationChecks: blocked by the existing Level002 painted-terrain assertion, then native shutdown exit139. This is a failed full-suite run, not a pass. No map assertion or gameplay test was weakened to bypass it.
- Editor import completed for newly pulled artwork. Desktop Inspector editing/visual playfeel was not manually inspected; native inherited scenes and propagation are validated by actual scene loading/instantiation.

## Before/after construction measurements

Before-scene fixtures were copied verbatim from GitHub HEAD `eda8c6c` before migration. To repeat optional benchmarks, place those five `.tscn` files in the Godot user-data directory named `ge-baseline-Enemy001.tscn`, `ge-baseline-Enemy002.tscn`, `ge-baseline-Enemy004.tscn`, `ge-baseline-ShieldGenerator.tscn`, `ge-baseline-Transporter.tscn`. Their existing `res://` script/art/definition dependencies must remain available. Without fixtures, benchmarks explicitly print skipped; correctness tests still run.

Five creation batches per size/type, median milliseconds; instances remain concurrently allocated until each batch completes, outside the scene tree. Timings measure scene construction, not rendered FPS or whole-game memory/frame time. Before/after run on the same host/process; startup, GC and host load still affect results.

| Type | 100 before | 100 inherited | 500 before | 500 inherited | Nodes per ship before/after |
| --- | ---: | ---: | ---: | ---: | ---: |
| Enemy001 | 2.018 | 2.210 | 10.288 | 10.794 | 4 / 4 |
| Enemy002 | 2.038 | 2.107 | 10.392 | 10.679 | 4 / 4 |
| ShieldGenerator | 2.449 | 2.714 | 14.010 | 16.478 | 6 / 6 |
| Enemy004 | 1.807 | 2.009 | 9.962 | 9.975 | 4 / 4 |
| Transporter | 2.335 | 2.367 | 11.116 | 11.932 | 6 / 6 |

Inheritance is an authoring/maintenance improvement, not a runtime speed optimization. Observed creation overhead for500 was0.013–2.468ms per whole batch; there are no new runtime nodes or C# processing paths. Existing navigation benchmarks and500-enemy combat/death-release checks passed. No claim of an FPS increase is made.
