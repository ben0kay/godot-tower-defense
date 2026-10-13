# Spawning Mechanics (Future)

**All decisions about enemy spawning live here**, not in the Portal scene itself.

Planned `SpawnManager` reads per-level spawn settings in `WORLD/LEVELS/`, chooses a permitted spawn position, validates navigability/clearance and instantiates the enemy type requested by `ENEMIES/Waves`.

## Spawn modes, configurable for each level

- **Portals:** use active, level-placed `WORLD/PORTALS/EnemyPortal/EnemyPortal.tscn` instances and their `SpawnExit` markers.
- **Map Edges:** select valid locations from the configured **Top, Bottom, Left and/or Right** map boundaries. This does not require physical Portal objects.
- **Both:** allow an explicit mixture of the above. Later, a wave may override the source selection.

Implemented per-level metadata in `WORLD/LEVELS/LevelConfig.cs`:
`SpawnMode` (Portals / MapEdges / Both), `AllowedEdges`, `ActivePortals` (root-relative NodePaths), and `CitadelPath`. Weights and spawn clearance remain future work. Map edges are derived from map bounds, not extra tile types or hand-authored wall cells.

### Responsibilities and rules

1. The WaveManager says **what/when/how many** enemies spawn.
2. SpawnManager chooses **where** using per-level parameters and the currently valid sources.
3. The Portal instance only identifies its world position, SpawnExit and visual/active state. It does **not** own wave timing or enemy difficulty.
4. Edge spawns must choose a **passable, reachable boundary location**, not a wall or occupied tower cell.
5. All sources must have a viable route to at least one **reachable Citadel approach point**; later build-placement checks must preserve required routes.
6. No source should instantiate enemies at a blocked cell or outside the intended playable area by mistake.

See `MapEdges/README.md` for edge-specific planning and `WORLD/README.md` for the authoring contract.

**No SpawnManager, SpawnMasterConfig, or spawning gameplay exists yet.** This is the folder/Codex handoff only.
