# Stasis Tower — goopy energy firing sound (2026-11-06)

**Source implemented / awaiting owner Godot audio validation.**

- `StasisFire.wav` is a distinct **1.58-second / 32 kHz / mono / signed 16-bit PCM** sci-fi shot, derived from the owner's previously supplied Cryo laser clip. It blends a down-pitched, subtly wobbling energy attack with separately synthesized descending liquid bubbles, a low resonant plop, and short dampened wet echoes. It has a soft ending fade rather than a trailing loop. The sound is rendered once as a WAV asset, so no real-time sound synthesis, effects chain or per-tower audio CPU is required.
- `StasisFire.tres` owns the Inspector-editable one-shot definition: stable `tower_stasis_fire` ID, WorldSfx, priority 35, 0.12-second same-owner cooldown, up to 8 instances, world-position sound, 550px audibility with offscreen culling, -7 dB initial mixer level and normal playback pitch. Tune VolumeDb if several Stasis towers compete with gunfire.
- `DefaultStasisTowerConfig.tres` selects `FireSoundId = "tower_stasis_fire"`, and `AUDIO/DefaultAudioCatalog.tres` registers the definition. Shared `ProjectileTurretBase.FireVolley` already requests one `AudioManager.PlayFrom` per **funded and actually emitted projectile**. Existing positional pools and global sound budget cull playback if needed; audio availability never gates projectile launch, ammo, power or damage.
- No changes to Stasis targeting, freeze duration, projectile shape, muzzle position, cooldown/firing rate, level/rank stats, save data, shared weapon logic or Cryo sound.

**Owner check:** open Godot and let Stasis fire at a valid unfrozen enemy. Confirm the wetter/lower laser attack only sounds on successful shots, compare against Cryo, try several towers together, then check camera distance/offscreen culling. Source/resource/WAV-format review only; no Godot playback test or FPS benchmark run.
