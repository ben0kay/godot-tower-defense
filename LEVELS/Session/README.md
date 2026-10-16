# Level session / mission outcome (FOUNDATION ONLY)

`LevelResultSnapshot.cs` is a small result-data contract for `UI/Menus/Results/LevelResults.cs`. No runtime session controller has been added or attached to a map yet. A snapshot contains the real outcome, wave/enemy/Citadel counters where supplied, and **nullable** future metrics: null means **not tracked**, never fake zero.

**Next integration (R1):** implement a small level-owned controller under this folder, shared by both `Main.tscn` and `MainLevel002.tscn`; use `WaveManager.WaveCompleted` / `AllWavesComplete` for success and `CitadelHealth.Destroyed` for immediate defeat. Check Citadel health again at victory, and latch the first final outcome (defeat wins if both occur on the last arrival). Pause world gameplay but allow the results UI to animate. Snapshot the run once; do not let debug spawns pollute official statistics.

**Stats (R1/R3):** `EnemyRegistry` owns killed/arrived totals; `WaveManager` owns wave completion; `CitadelHealth` owns integrity. Count actual successful tower placements, transactions and elapsed *unpaused* mission time using event-driven changes and a single session clock, not scanning all towers/enemies every frame. Keep player XP distinct from already-implemented tower rank XP.

No persistence, awards, combat, save format, or wave behavior is changed by the foundation commit. For the UI and full passes see `UI/Menus/Results/README.md`.
