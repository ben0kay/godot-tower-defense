# Enemy spawning

`SpawnManager.cs` is a world service under World/EnemySystems/Spawning. Physical Portal scenes contain only visuals/SpawnExit; this service selects positions and creates requested scenes. Waves call SpawnWave(scene, random, optionalRequiredPortal), while level debug calls Spawn(scene, portalOnly: true).

LevelConfig supplies SpawnMode (Portals / MapEdges / Both), allowed edges, ActivePortals and CitadelPath. Entries and goal-connectivity are cached by navigation revision. Invalid, blocked, unreachable or insufficient-clearance sources are skipped. Map-edge entries are cell centers on allowed map borders. Portal-only debug respects permissions, so edge-only levels need PortalOnly unchecked on their Debug node to test edge spawning.

Placement checks run on a build click: reject a cell overlapping an active enemy's clearance, sealing any currently usable authored entry, or trapping a living enemy. In edge mode every currently reachable permitted boundary entry is preserved. The hover preview remains a cheap terrain/occupancy check; a route rejection is explained in the Inspector when clicked. RebuildTerrain after runtime changes to level source/goal settings.

Main/Waves schedules manual finite waves. SpawnWave selects uniform random eligible portals, prefers them in Both mode, and uses permitted map edges if no portal is eligible. A required registered portal never falls back. F2 Spawn(scene, portalOnly) retains round-robin behavior. EnemyMasterConfig controls the active ship safety limit and path budget. Sources already unreachable because of authored map errors do not become valid through this check; debug spawning reports the failure safely.

Connectivity storage now reuses buffers: NavigationGrid returns borrowed flood scratch space, copied into SpawnManager's reusable current-connectivity cache. Placement validation exposes LastPlacementValidationMilliseconds. The pre/post connectivity checks remain mandatory; route checking has not been removed to hide freezes.
