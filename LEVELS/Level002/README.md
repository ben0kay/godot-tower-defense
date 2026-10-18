# Level002 — fresh map-design scene

Open `res://LEVELS/Level002/Level002.tscn` in the 2D editor to paint the next map. This is a fresh copy of `LEVELS/LevelTemplate.tscn`, with its own root name, scene identity and `Level002Config.tres`. Terrain layers start empty; template Portal/Citadel/checkpoint overlay and background are retained. The shared template and its default config are unchanged.

Edit this local config without affecting other maps. Defaults retain template map-edge spawning, zero starting Credits and no wave set. Configure terrain, sources, routes, rewards/waves and playable launcher before adding it to Campaign/PlayableLevelCatalog. It is intentionally not registered as a completed playable mission yet; use the existing map-authoring workflow in LEVELS/README.md.

The former Level002 / Five Gates map is preserved at `LEVELS/Level020/Level020.tscn`, with its renamed config/waves, `WORLD/WorldLevel020.tscn` and `MainLevel020.tscn`. Campaign now labels that map Level020. Do not put old Five Gates content back into this folder.
