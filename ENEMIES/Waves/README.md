# Enemy Waves (Future)

The WaveManager will determine **which enemy types spawn, how many and when**. It sends requests to `ENEMIES/Spawning/`, which chooses valid level-approved Portal and/or map-edge sources.

Keep per-level allowed sources in `WORLD/LEVELS/`; wave-specific source overrides can be designed later. Do not embed wave timing or enemy counts in Portal scenes.

No wave system exists yet.
