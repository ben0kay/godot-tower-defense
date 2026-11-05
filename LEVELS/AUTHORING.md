# Creating another playable level

Maps now inherit `LEVELS/Shared/BaseLevelMap.tscn`; common containers/background/debug stay centralized while each child keeps four local editable TileMapLayers. [Architecture and verification limits](Shared/README.md).

| File in the level folder | Responsibility |
| --- | --- |
| Level004.tscn | Your painted map and authored world objects |
| Level004Config.tres | Independent starting funds, Citadel HP, portal settings, XP and wave recipe reference |
| Level004Waves.cs **or** Level004Waves.tres | Optional C# recipe or editable Godot waves from the custom Wave Editor |
| Level004Mission.tres | Number/title/menu description and map file path |

1. Create a **New Inherited Scene** from `LEVELS/Shared/BaseLevelMap.tscn`, saved as `LEVELS/Level004/Level004.tscn`. Copy only the four local TileMapLayers from LevelTemplate into its inherited Terrain container, retaining their shared TileSets. Create independent Level004Config.tres and assign Settings; add local Portal/Citadel instances under the inherited containers.
2. Paint and place objects normally. Set active Portal paths and Citadel references on the config as before. Preview plain maps in the editor; gameplay needs the shared battlefield.
3. **Two supported wave-authoring modes**: (a) for C# waves, copy a LevelXXXWaves.cs recipe, rename its class, preserve WaveSetSource + Build, and assign a recipe resource in Level004Config.WaveSource; (b) for visual waves, enable `addons/wave_editor` under Project Settings → Plugins, click the new **Wave Editor** main tab, choose **New .tres** in Level004's folder, author waves/batches/enemies, Save, then assign that new `WaveSetResource` file to Level004Config.WaveSource. You can duplicate `Level003Waves.tres` as a starting template. Give WaveSetId a unique descriptive ID (e.g. level004). No WaveCatalog switch registration is required. An empty ID and empty WaveSource disables waves for an unfinished map.
4. Create a MissionDefinition resource saved as Level004Mission.tres. Set Number=4, Title, Description and MapScenePath=Level004.tscn. Leave LegacyLauncher empty. Keep Enabled off while the draft is unfinished.
5. Add that resource to `LEVELS/Catalog/DefaultMissionCatalog.tres` / Missions and enable it when ready. Campaign builds its scrollable list from this roster. Deploy to play with the shared services.

No new WORLD/WorldLevel004, MainLevel004, menu button code or C# catalog case is required. Number controls sorting and mission identity; resource-array order is not progression. Next Level only advances to the immediate enabled number: 003 cannot skip a missing004 and jump to020. Retry reloads the same mission with fresh world/wallet/waves. Menu title/description are data too.

The three existing launchers remain F6 shortcuts. An optional new shortcut can inherit `LEVELS/Runtime/Battlefield.tscn` and assign Mission; it contains no duplicate service tree. Wave batches can now be C# or `.tres` based. Only Level003 was migrated for the Wave Editor pilot; other existing maps retain their C# wave recipes. This does not change map tiles, wave scheduling, level completion or campaign save formats.

## Optional per-level map dimensions (Level011)

The common `WORLD/DefaultWorldConfig.tres` remains the default for existing maps (100×80 cells, 64px each). For a larger map, create a dedicated `WorldMasterConfig` resource in its level folder and set `LevelConfig.WorldConfig` to it. The existing `WorldMapLoader` supplies that resource to the same `World/Grid` before `_Ready`/navigation setup. This **does not** change other missions, TileSet cell size, or duplicate world services.

For a paint-ready larger-map example, open `LEVELS/Level011/`. Its blank 160×120 map reuses `LevelTemplate.tscn`, and its resource-based wave is editable in the Wave Editor. Level011 was added independently while LM1 was in progress and remains unchanged as a standalone map. LM1 migrates the five earlier production maps plus LevelTemplate; new maps should use the inherited workflow above. The unregistered `TESTS/Levels/LM1AuthoringProbe.tscn` demonstrates local paint and objects under inherited containers.

