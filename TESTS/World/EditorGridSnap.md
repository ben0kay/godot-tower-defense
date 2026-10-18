# Portal/Citadel editor snap checks

After building C# and importing assets with Godot .NET 4.7.2:

```sh
godot --headless --editor --path . --script res://TESTS/World/EditorGridSnapChecks.gd
godot --headless --path . --script res://TESTS/World/EditorGridSnapChecks.gd
```

Both commands passed on 2026-10-18. The script uses actual Portal/Citadel scenes and their compiled C# tool script in a disposable map, without saving authored levels. Editor mode verifies deferred transform corrections; runtime mode verifies identical movements are left untouched. Coverage: 64px cell centers, negative positions, opt-out/re-enable, translated/rotated/scaled parents, alternate 80px tile size, duplicate instances, child marker/footprint preservation and standalone scene origins. C# build passed with zero warnings/errors.

The custom headless editor SceneTree emits editor-owned RID/ObjectDB cleanup diagnostics on shutdown after the assertions pass; the runtime command exits cleanly. Desktop drag/undo feel remains a manual review item. No new gameplay/nav behavior was introduced.
