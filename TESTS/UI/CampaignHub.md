# Campaign hub / Levels split

Updated `MenuTowerFlowChecks.tscn` exercises actual Boot → Main → Campaign → Levels → Back → Campaign → Levels → Level001 buttons, keeps profile identity, verifies both launch choices and continues through existing paid tower/HUD behavior. The runner now isolates progression writes under a temporary directory, since deployment reserves mission-reward tickets.

The same runner checks Campaign and Levels panels at 1920×1080, 1366×768 and 960×540. MenuFoundationChecks includes the new scene. ResearchMenuChecks verifies Campaign ↔ Research and real purchases; ResultsChecks verifies the existing post-mission Campaign return.

Passed: SDK build (zero warnings/errors), MenuTowerFlowChecks (including both menu panels at three viewport sizes), ResearchMenuChecks and ResultsChecks. The broader PauseControlsChecks runner hit a Godot native resource/finalizer crash in this runtime, including on an isolated retry; no complete controls-suite pass is claimed. Its production control behavior is outside this menu-only change. Desktop rendered appearance remains unreviewed. No new unlock/save/mission-completion system is claimed.
