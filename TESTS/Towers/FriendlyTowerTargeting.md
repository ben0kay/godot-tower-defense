# RT0 friendly targeting — focused check

**Authored / not executed here (2026-11-05).** Source/diff checks only; .NET is unavailable. No installation, broad test suite or benchmark. After building in Godot .NET, optionally run `FriendlyTowerTargetingChecks.tscn` with F6 once.

The small existing AmmoLoopFixture/production-world setup checks lowest **fraction** rather than absolute hull, deterministic equal-score ties, self/full filtering, 1×1 inclusive root radius, multi-cell broadphase with strict root range, zero/invalid range, disabled-target eligibility and disabled-healer rejection, construction/upgrades, fully repaired/detached/queued/destroyed lock invalidation and cross-world rejection. Scratch contents are cleared while capacity stays reusable. Tests disable Battle's power profile only in private fixture configs to isolate targeting from charge state; shipped configs are unchanged. No profile writes, gameplay map edits or stress simulation.

There is no playable repair tower in RT0. The future caller owns staggered searches, a retained scratch list/target, pause gating and stopping its repair beam immediately when `FriendlyTowerTargeting.Valid` returns false. No repair application, Citadel, shields or mobile-friendly provider is tested or implemented in this pass. Existing enemy selectors and enum values remain byte-for-byte unchanged.
