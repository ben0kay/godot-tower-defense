## Automatic next waves and upper-right countdown — 2026-11-06

**Source Implemented / Awaiting Owner Validation.** The first authored wave remains manual. After any non-final wave fully clears (all scheduled ships, owned live ships and reserved death children resolved), the next wave starts automatically after 30 seconds by default. NEXT WAVE remains enabled throughout so the player can start early. Automatic/manual starts share StartWave and preserve once-only banners, portal activation, RNG/ownership and spawn budgets; zero delay starts on the next scheduler tick rather than recursively inside a death callback.

- LevelConfig.DefaultNextWaveDelaySeconds sets the level-wide default (30).
- WaveResource.NextWaveDelaySeconds / immutable WaveDefinition.NextWaveDelaySeconds overrides the wait **after that wave**; -1 inherits the level default, 0 skips the wait, and the final wave ignores the value.
- Wave Editor → Selected wave → Next wave delay exposes this setting; duplicate/build/save preserve it. Existing .tres resources and C# recipes inherit automatically. C# recipes can use `Wave(...) with { NextWaveDelaySeconds = 10 }`.
- The upper-right label beside NEXT WAVE shows AUTO START · mm:ss and updates only when its displayed second changes. Narration clears the complete control row.
- Countdown pauses with gameplay, cancels on early start/reconfigure/outcome/unload, and never starts Wave 1 while preparing a new battlefield.
- Optional checkpoint NextWaveAutoStartSeconds stores exact remaining seconds. Continue restores it; old checkpoints with completed waves and no field use the current default/override. Wave-zero checkpoints retain manual start.

No additional per-wave timers/Nodes or scene scans: the existing scheduler only ticks while spawning or counting down, and sleeps otherwise. Focused source and lifecycle review only; .NET unavailable. New owner-run `TESTS/Integration/AutomaticWaveChecks.tscn` covers defaults/custom/zero, pause, early start, last-wave cancellation, restore, resource copying and the live Results hold. Not executed here; owner gameplay/UI acceptance pending.

---

# Editor-authored wave resources (2026-10-29 pilot)

`WaveSetResource` is an alternative `WaveSetSource` for native Godot `.tres` files. Data hierarchy: **WaveSetResource → WaveResource[] → WaveBatchResource[] → WaveEnemyResource[]**. These are `[Tool, GlobalClass]` C# `Resource` types so the Godot editor can serialize them; they own **data only**, not spawning state, RNG, enemy instances or per-frame work.

The builder constructs fresh immutable `WaveDefinition` / `WaveBatch` / `WaveEnemyEntry` objects when `WaveSource.Build()` is called by the existing `WaveCatalog`. It preserves the usual C# `WaveBuilder.Wave/Batch/Enemy` contract, including optional per-wave presentation, weighted mixtures, exact timings, required portal selectors, starting shields and stealth flags. Runtime validation remains in `WaveCatalog.Validate`.

The `addons/wave_editor/` main-screen plugin is the recommended authoring UI. Existing `LevelXXXWaves.cs` subclasses coexist; only Level003 is switched to a `.tres` pilot. `Level003Waves.cs` remains a comparison fixture. See [editor instructions](../../../addons/wave_editor/README.md). No new game runtime update loops.
