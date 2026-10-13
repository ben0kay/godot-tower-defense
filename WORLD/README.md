# WORLD

Parent feature area for the battlefield and its future map systems.

- `WorldMasterConfig.cs` defines editable map/grid settings.
- `DefaultWorldConfig.tres` is the Inspector-editable master settings resource.
- `World.tscn` owns the `GRID/WorldGrid.cs` renderer.
- `GRID/` contains coordinate helpers and a small development overlay.

Future subfolders can hold maps, asteroids, objectives and obstacles when implemented. This first pass deliberately has no thousands of per-cell nodes or world simulation.
