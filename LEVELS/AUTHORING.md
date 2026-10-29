# Creating another playable level

Your design workflow stays the same: duplicate LevelTemplate.tscn into a LevelXXX folder, then paint its tiles and place/snapped Portals and Citadels in Godot. This pass changes the runtime wiring, not map painting.

| File in the level folder | Responsibility |
| --- | --- |
| Level004.tscn | Your painted map and authored world objects |
| Level004Config.tres | Independent starting funds, Citadel HP, portal settings, XP and wave recipe reference |
| Level004Waves.cs **or** Level004Waves.tres | Optional C# recipe or editable Godot waves from the custom Wave Editor |
| Level004Mission.tres | Number/title/menu description and map file path |

1. Duplicate the template into `LEVELS/Level004/Level004.tscn`. Duplicate DefaultLevelConfig into Level004Config.tres and assign it to the map's Settings. Keep each map's config independent.
2. Paint and place objects normally. Set active Portal paths and Citadel references on the config as before. Preview plain maps in the editor; gameplay needs the shared battlefield.
3. **Two supported wave-authoring modes**: (a) for C# waves, copy a LevelXXXWaves.cs recipe, rename its class, preserve WaveSetSource + Build, and assign a recipe resource in Level004Config.WaveSource; (b) for visual waves, enable `addons/wave_editor` under Project Settings → Plugins, click the new **Wave Editor** main tab, choose **New .tres** in Level004's folder, author waves/batches/enemies, Save, then assign that new `WaveSetResource` file to Level004Config.WaveSource. You can duplicate `Level003Waves.tres` as a starting template. Give WaveSetId a unique descriptive ID (e.g. level004). No WaveCatalog switch registration is required. An empty ID and empty WaveSource disables waves for an unfinished map.
4. Create a MissionDefinition resource saved as Level004Mission.tres. Set Number=4, Title, Description and MapScenePath=Level004.tscn. Leave LegacyLauncher empty. Keep Enabled off while the draft is unfinished.
5. Add that resource to `LEVELS/Catalog/DefaultMissionCatalog.tres` / Missions and enable it when ready. Campaign builds its scrollable list from this roster. Deploy to play with the shared services.

No new WORLD/WorldLevel004, MainLevel004, menu button code or C# catalog case is required. Number controls sorting and mission identity; resource-array order is not progression. Next Level only advances to the immediate enabled number: 003 cannot skip a missing004 and jump to020. Retry reloads the same mission with fresh world/wallet/waves. Menu title/description are data too.

The three existing launchers remain F6 shortcuts. An optional new shortcut can inherit `LEVELS/Runtime/Battlefield.tscn` and assign Mission; it contains no duplicate service tree. Wave batches can now be C# or `.tres` based. Only Level003 was migrated for the Wave Editor pilot; other existing maps retain their C# wave recipes. This does not change map tiles, wave scheduling, level completion or campaign save formats.
