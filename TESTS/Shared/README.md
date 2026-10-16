# Shared tests

Test-only reference implementations and fixtures; never registered as gameplay services. BruteForceShockwave preserves the original scan-based wavefront benchmark baseline. MissionFixture loads genuine battlefield services/controller with automatic ticks stopped for deterministic outcomes. SubsystemBattlefield removes mission orchestration/presentation before older subsystem fixtures deliberately mutate waves/HP; production scenes retain R1. Its teardown barrier finalizes freed managed fixture wrappers before native engine shutdown, avoiding headless Mono teardown faults. No forced GC occurs in gameplay.

See [the test index](../README.md) and [R1 verification](../UI/Results.md).
