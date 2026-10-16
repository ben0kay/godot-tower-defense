# Enemy spawning

`SpawnManager.cs` is a world service under World/EnemySystems/Spawning. Physical Portal scenes contain only visuals/SpawnExit; this service selects positions and creates requested scenes. Waves call SpawnWave(scene, random, optionalRequiredPortal), while level debug calls Spawn(scene, portalOnly: true).

LevelConfig supplies SpawnMode (Portals / MapEdges / Both), allowed edges, ActivePortals and CitadelPath. Entries and goal-connectivity are cached by navigation revision. Invalid, blocked, unreachable or insufficient-clearance sources are skipped. Map-edge entries are cell centers on allowed map borders. Portal-only debug respects permissions, so edge-only levels need PortalOnly unchecked on their Debug node to test edge spawning.

Placement checks run on a build click: reject a cell overlapping an active enemy's clearance, sealing any currently usable authored entry, or trapping a living enemy. In edge mode every currently reachable permitted boundary entry is preserved. The hover preview remains a cheap terrain/occupancy check; a route rejection is explained in the Inspector when clicked. RebuildTerrain after runtime changes to level source/goal settings.

Main/Waves schedules manual finite waves. SpawnWave selects uniform random eligible portals, prefers them in Both mode, and uses permitted map edges if no portal is eligible. A required registered portal never falls back. F2 Spawn(scene, portalOnly) retains round-robin behavior. EnemyMasterConfig controls the active ship safety limit and path budget. Sources already unreachable because of authored map errors do not become valid through this check; debug spawning reports the failure safely.

Connectivity storage now reuses buffers: NavigationGrid returns borrowed flood scratch space, copied into SpawnManager's reusable current-connectivity cache. Placement validation exposes LastPlacementValidationMilliseconds. The pre/post connectivity checks remain mandatory; route checking has not been removed to hide freezes.

Portal spawning now uses the centered SpawnExit plus a random offset up to PortalSpawnOffsetPixels on each world axis (default 8). Both F2 and waves use the same safety helper; source selection remains round-robin for F2 and random for waves. Up to eight clearance/connectivity/segment-checked samples are tried, falling back to the valid center. Edge positions remain exact. Set the offset to 0 for exact portal-center spawning.

`SpawnWave(scene, random, requiredPortal: "", spawnWithShield: false)` now accepts the wave's optional full-shield flag and passes it to `Enemy.Initialize` only after a valid spawn is found. This does not affect portal selection, retry logic, navigation or costs. `Spawn(scene, portalOnly)` (F2/debug) continues to initialize without shields by default; the per-type `EnemyDefinition.MaxShield` is capacity, not a spawning directive.


E2A: optional exported Catalog defaults to DefaultEnemyCatalog; string-ID Spawn/SpawnWave adapters resolve types while legacy scene APIs/F2 remain intact. WaveManager uses this actual world catalog. Current guaranteed single-cell route support requires radius smaller than half CellSize; oversized types are held/retried with a diagnostic, not squeezed through obstacles. No new A* work or radius grid is created.


Default catalog also registers `shield_emitter_mk1` (the `ShieldGenerator.tscn` asset). Abilities initialize only after a safe physical source, registry and movement are ready. SpawnManager does not own pulse timers or shield-restoration logic; wave starting-shield policy remains unchanged.


Portal offsets now scale by each EnemyPortal.SizeMultiplier and its authored transform. Portal references are cached alongside source metadata; current size is read on each spawn, so changing it needs no A* or navigation rebuild. The base config offset remains the shared tuning control, and all8 bounded safety attempts/center fallback still apply. Map-edge sources are unaffected.

## Portal checkpoint assignment

Both debug and authored wave spawns inspect only the actual selected EnemyPortal's optional CheckpointPath. Its resolved node plus a separate required flag are supplied before initial movement setup, preserving missing-link requirements. No extra wave/spawn RNG draws or changes to reservoir selection, spread, registry/economy or terrain initialization are introduced. Empty links and map-edge sources preserve existing direct routing.

## Mission statistics provenance

`WaveEnemyCreated` identifies successful authored-wave spawns before Enemy.Initialize, so entry-hazard deaths are countable. It is not emitted by manual/F2 spawning. The level session subscribes once for basic official kill/breach tallies; SpawnManager does not own outcomes, rewards or result presentation.
