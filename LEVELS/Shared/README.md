# Shared map authoring — LM1

**Source Implemented / Awaiting Owner Validation — 2026-11-05.**

`BaseLevelMap.tscn` owns the WorldLevel root, background categories/default starfield, dormant overridable atmosphere, Terrain/Checkpoints/Portals/Objectives containers, and existing shared Debug controller plus CheckpointOverlay. It has no Settings, painted cells, portal/Citadel instances, waves, world services or extra processing manager.

**Hybrid chosen:** each child owns its four local TileMapLayer nodes and references the existing category TileSets. This keeps ordinary painting/serialization local; it does not depend on the editor overriding inherited TileMapLayer data. Level001/002/003/011/020/100 and LevelTemplate inherit the base. Their original node unique IDs, scene UIDs, local terrain headers/properties/data, objects, Settings and meaningful background/debug overrides remain. Missing atmosphere/checkpoint organizers gain safe dormant/empty defaults. Level001 explicitly keeps its previously enabled atmosphere; others retain disabled behavior. Level020 keeps its 3px floor offset and authored object positions.

To start a blank map, create an inherited scene from BaseLevelMap, copy the four **local layers only** from LevelTemplate into Terrain, and assign independent Settings. Do not add a second Terrain/Debug/Portals/Objectives. The base's shared defaults remain overridable in the child. [Full authoring workflow](../AUTHORING.md).

## Minimal verification and limits

A single headless **Godot 4.6.3 native** probe tested the hybrid before migration: inherited base, two child maps with four locally painted layers, pack/save/reopen, shared base appearance edit without erasing child cells, editable local objects in Portals/Objectives, and an untouched empty base. Saved scenes retained their PackedScene base reference. The probe used native/script-free stand-ins: **the C# game was not built or launched**, and manual Godot .NET editor painting/import/Campaign checks remain with the owner. This proves the native hybrid scene/data workflow, not full gameplay acceptance or fully inherited TileMapLayer editing.

`TESTS/Levels/LM1_MAP_INVENTORY.json` records the original scene UIDs, node paths/properties, canonical resource paths, exact serialized terrain hashes and object positions. Migration compared the effective inherited properties against this inventory, preserving original terrain blocks exactly. Runtime/Battlefield/WorldMapLoader, configs, waves, mission catalog, gameplay systems and save schema were not edited. A shared dependency change can still trip the existing checkpoint fingerprint guard; use a fresh checkpoint after pulling.

`TESTS/Levels/LM1AuthoringProbe.tscn` is an unregistered owner-facing editor sample with one local cell in each layer and two local object instances. Duplicate it for painting experiments; do not register it as a campaign mission or edit shared TileSets just to change individual map paint. Full intended acceptance: edit/reopen two copies in the Godot .NET editor, inspect all five Campaign maps and F2, and verify a fresh retry/pause/checkpoint. No broad regression, long simulation or performance benchmark ran.

## Existing larger dimensions — preserved independent work

Level011 and `LevelConfig.WorldConfig` were added independently while LM1 was in progress. The existing map loader now applies an optional per-map WorldMasterConfig before Grid/navigation initialize, keeping the common 64px cell size and shared defaults for other maps. LM1 preserves that work without editing the loader, configs, catalog or Level011 scene. Level011 was subsequently migrated to BaseLevelMap on owner request, preserving its four local layers, object positions and independent 160×120 settings. Future maps should inherit BaseLevelMap. Do not enlarge DefaultWorldConfig globally for one mission. LM1 adds no dimensions, Level011, streaming or procedural generation.

