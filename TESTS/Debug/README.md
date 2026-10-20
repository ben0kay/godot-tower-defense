## PF2 debug update (2026-10-20)

Optional subsystem timing hooks feed the existing F4 panel (Shift+F4 toggles profiling; F5 saves a report). Old GridDebugOverlay/Info labels are retired; F2/F3 remain. See `DEBUG/README.md` for inclusive timing semantics, fixed sample storage and validation details. Historical notes below remain as reference.

# Performance overlay / debug input checks

Build C# with Godot .NET 4.7.2, then run `res://TESTS/Debug/PerformanceOverlayChecks.tscn` headless. Tests run actual Main, MainLevel002 and MainLevel020 scenes; verify service counts without new route queries, all overlay Controls ignoring mouse events, clearance below HUD/old Info and above lower HUD at reduced viewport sizes, F4/echo handling, pause-safe reopening and stopped hidden sampling. The repaired Level002 controller is exercised with F2 presses and F3 service initialization, alongside the existing maps. These are behavior/geometry checks, not screenshots or a rendered performance benchmark.

C# build passes with zero warnings/errors. The final runner passed on all three battlefields, including F2 spawn/echo and F3 initialization. Existing Level001 emits an unrelated missing configured EnemyPortal2 warning but has a usable portal; level art/config/terrain data are not edited by the fixture. Runtime release-export gating is implemented through OS.IsDebugBuild; an exported release executable is not exercised by this runner.
