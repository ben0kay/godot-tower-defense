## F2 sprite preview — 2026-10-21

EnemySpawnMenuChecks now exercises every catalog selection, native hull texture/scale, separate Gunner hull/gun layers, no attached preview scripts or live enemy registration, idle process flags, and oversized artwork preserving authored scale. Existing independent batches and shield/stealth controls remain covered.

## Concurrent batches and flags — 2026-10-21

EnemySpawnMenuChecks now verifies two simultaneous enemy types with different gaps and captured shield/stealth flags, a combined-flags submission through the actual checkbox/button controls, round-robin fairness under a one-spawn global budget, and individual row cancellation without stopping the other batch. Existing single-batch exact-count/pause/close/random-portal/wave-isolation checks remain. This supersedes the old one-batch rejection expectation.

Final checks: C# build/import, EnemySpawnMenuChecks, PerformanceOverlayChecks and PlayerProgressionChecks pass. GunnerDroneChecks stops at its pre-existing tracer/flash-style assertion; the identical failure reproduces on unchanged6aab989 before this debug reorganization. Its legacy catalog-cycle section is adapted, but no complete GunnerDrone suite pass is claimed.

## F2 batch menu — 2026-10-21

EnemySpawnMenuChecks.tscn verifies F2 opens without spawning, echo suppression, Escape, exact bounded zero-gap batches, positive gaps, pause, close/continue, cancellation, rejected inputs, disabled-controller cleanup, independent wave ownership, random distribution across two seeded portal sources, and no map-edge fallback. PerformanceOverlayChecks now explicitly submits a menu batch instead of expecting keypress spawning. GunnerDroneChecks uses the parked legacy catalog helper for its catalog cycle. Existing broader integration fixtures retain previously documented failures unrelated to this change.

## PF2 debug update (2026-10-20)

Optional subsystem timing hooks feed the existing F4 panel (Shift+F4 toggles profiling; F5 saves a report). Old GridDebugOverlay/Info labels are retired; F2/F3 remain. See `DEBUG/README.md` for inclusive timing semantics, fixed sample storage and validation details. Historical notes below remain as reference.

# Performance overlay / debug input checks

Build C# with Godot .NET 4.7.2, then run `res://TESTS/Debug/PerformanceOverlayChecks.tscn` headless. Tests run actual Main, MainLevel002 and MainLevel020 scenes; verify service counts without new route queries, all overlay Controls ignoring mouse events, clearance below HUD/old Info and above lower HUD at reduced viewport sizes, F4/echo handling, pause-safe reopening and stopped hidden sampling. The repaired Level002 controller is exercised with F2 presses and F3 service initialization, alongside the existing maps. These are behavior/geometry checks, not screenshots or a rendered performance benchmark.

C# build passes with zero warnings/errors. The final runner passed on all three battlefields, including F2 spawn/echo and F3 initialization. Existing Level001 emits an unrelated missing configured EnemyPortal2 warning but has a usable portal; level art/config/terrain data are not edited by the fixture. Runtime release-export gating is implemented through OS.IsDebugBuild; an exported release executable is not exercised by this runner.
