# Debug enemy spawning

EnemySpawnMenu.cs owns the F2 panel and one bounded pending batch. LevelTools/LevelDebugController attaches it to each live battlefield. Catalog choices come from the actual EnemyCatalog; no duplicate enemy list or balance table. Amount and gap are captured at Start. Zero gap is throttled by MaxSpawnsPerTick; positive gaps are physics-tick limited and do not catch up in bursts. Pause freezes the batch. Closing the menu leaves it running, Cancel removes pending ships only, failed delivery stops with a reason. Existing production active-enemy limits remain.

SpawnManager.SpawnRandomPortal uses its shared reservoir sampler and production portal validation, with independent debug RNG and no wave-owned creation event. Portal-only means no fallback to map edges. Debug deaths/breaches retain normal gameplay consequences. No debug waves or mission statistics are fabricated.

LegacyCatalogSpawner.cs parks the old catalog-order cycling logic without a key handler or scene attachment. It remains callable for old fixtures; F2 no longer calls it. Tests: TESTS/Debug/EnemySpawnMenuChecks.tscn.
