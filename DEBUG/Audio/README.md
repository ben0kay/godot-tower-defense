# Audio Debug Tester — retired

DBG1 removed the tester from AudioManager and retired F9. The archived DEBUG-only `AudioTester.cs` can run synthetic playback through `RunSample()` when explicitly mounted in a dedicated debug fixture. Normal game launch no longer registers its synthetic tones.
