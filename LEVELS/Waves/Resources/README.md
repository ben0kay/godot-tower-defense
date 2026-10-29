# Editor-authored wave resources (2026-10-29 pilot)

`WaveSetResource` is an alternative `WaveSetSource` for native Godot `.tres` files. Data hierarchy: **WaveSetResource → WaveResource[] → WaveBatchResource[] → WaveEnemyResource[]**. These are `[Tool, GlobalClass]` C# `Resource` types so the Godot editor can serialize them; they own **data only**, not spawning state, RNG, enemy instances or per-frame work.

The builder constructs fresh immutable `WaveDefinition` / `WaveBatch` / `WaveEnemyEntry` objects when `WaveSource.Build()` is called by the existing `WaveCatalog`. It preserves the usual C# `WaveBuilder.Wave/Batch/Enemy` contract, including optional per-wave presentation, weighted mixtures, exact timings, required portal selectors, starting shields and stealth flags. Runtime validation remains in `WaveCatalog.Validate`.

The `addons/wave_editor/` main-screen plugin is the recommended authoring UI. Existing `LevelXXXWaves.cs` subclasses coexist; only Level003 is switched to a `.tres` pilot. `Level003Waves.cs` remains a comparison fixture. See [editor instructions](../../../addons/wave_editor/README.md). No new game runtime update loops.
