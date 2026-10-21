## Independent F2 batches and spawn flags — 2026-10-21

F2 now accepts new batches while previous ones are still spawning. Every click captures its own enemy, amount, gap, Shielded and Stealthed options. Both flags default off and can be combined; shields use the enemy definition's existing MaxShield (zero capacity stays zero), and stealth uses the existing targeting/visual state. No new shield/stealth mechanic or authored resource values are introduced.

A scrollable active-batch list shows captured settings and progress, with a Cancel button per row. Cancel All stops all pending batches; closing F2/Escape leaves them running. Completed/cancelled/failed rows retire. A failure stops only that batch and reports its reason. Aggregate Spawned counts successes since the current group began (reset on starting from idle). Batches retain independent timers and rotate fairly under one shared MaxSpawnsPerTick budget (default16 across all batches, not16 each); positive gaps remain at most one delivery per batch per physics tick, with no catch-up bursts. Pause freezes all pending timers; idle processing stops. Independent debug RNG, random valid portals, active-enemy limit, wave isolation and existing gameplay consequences remain.

Build/import and focused menu tests passed: concurrent distinct types/intervals, shield-only/stealth-only/combined flags, actual checkbox/button submission, global budget/fairness, per-row cancellation, prior pause/close/random-portal checks. Previous one-batch descriptions below are historical and superseded. Actual tools stay under DEBUG; checks stay under TESTS.

---

# Debug enemy spawning

EnemySpawnMenu.cs owns the F2 panel and one bounded pending batch. LevelTools/LevelDebugController attaches it to each live battlefield. Catalog choices come from the actual EnemyCatalog; no duplicate enemy list or balance table. Amount and gap are captured at Start. Zero gap is throttled by MaxSpawnsPerTick; positive gaps are physics-tick limited and do not catch up in bursts. Pause freezes the batch. Closing the menu leaves it running, Cancel removes pending ships only, failed delivery stops with a reason. Existing production active-enemy limits remain.

SpawnManager.SpawnRandomPortal uses its shared reservoir sampler and production portal validation, with independent debug RNG and no wave-owned creation event. Portal-only means no fallback to map edges. Debug deaths/breaches retain normal gameplay consequences. No debug waves or mission statistics are fabricated.

LegacyCatalogSpawner.cs parks the old catalog-order cycling logic without a key handler or scene attachment. It remains callable for old fixtures; F2 no longer calls it. Tests: TESTS/Debug/EnemySpawnMenuChecks.tscn.
