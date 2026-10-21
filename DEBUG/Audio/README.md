# Audio Debug Tester

`AudioTester.cs` is attached beneath the autoloaded `AUDIO/AudioManager.tscn`. It creates temporary PCM tones **only in DEBUG builds**; no imported sound files are required. It does not alter `DefaultAudioCatalog.tres`.

Press **F9** with `Scenario = CycleExamples` to hear a UI beep and left/right positional beeps, then toggle a looping hum. Use the `AudioManager.tscn` Inspector to select `PriorityBurst`, `Intermittent` or `VoiceQueue` scenarios. Positional scenarios require `Main.tscn` (camera); F5 opens the Boot menu and supports non-positional checks.

Remove or disable the tester when debugging is no longer useful; it has no production gameplay integration.
