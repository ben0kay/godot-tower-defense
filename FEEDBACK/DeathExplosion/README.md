# DeathExplosion — Reusable Smoke and Orange Flash

**Implemented:** A single shared, independent one-shot GPU particle effect for killed enemies. Future destroyed towers can call the same `DeathExplosion.Spawn` API. `World/Effects` holds the active puffs (not the disappearing enemy/tower). Enemy death calls the API once from its guarded `Finish(false)` path; normal arrival or world cleanup does not. The effect is entirely visual: no damage, collisions, sound or wallet changes.

## Files
- `DeathExplosion.tscn`: reusable effect with **Smoke** + **Flash** nodes of Godot type `GPUParticles2D` (Godot C# type **`GpuParticles2D`**). Both emit once with a radial soft texture and orange→grey fading ramps. Open this scene in the Inspector to tweak colors, spread and particle velocity.
- `DeathExplosion.cs`: scene cache, size calculation, spawning, active-effect cap and automatic cleanup; no per-frame Process.
- `DeathExplosionConfig.cs` / `DefaultDeathExplosionConfig.tres`: Inspector settings for the default 32-pixel reference size, size multipliers/caps, particle amounts, smoke and flash lifetime, maximum concurrent effects.
- `Artwork/smoke_puff.svg`: 48px white radial alpha sprite tinted by GPU particle colors; imported by Godot.
- `FEEDBACK/DefaultFeedbackConfig.tres`: master `DeathExplosionsEnabled` switch (enabled by default).

## Auto scaling — no per-enemy setup needed

The shared `EnemyDefinition` exports **Death Feedback / Death Explosion Multiplier = 1.0** with a default. `Enemy001.tres` needs no override. Whenever an enemy dies, the system reads its `Sprite2D.GetRect().Size` and its world transform basis lengths (actual scale, including parent scaling); rotation does not inflate the explosion. If the sprite is absent, fall back to `EnemyDefinition.RadiusPixels * 2`.

```text
applied scale = clamp(
    measured_visual_size_px / ReferenceVisualSizePixels
    × GlobalSizeMultiplier × enemy.DeathExplosionMultiplier,
    MinimumScale, MaximumScale)
```

The default 32px Enemy001 sprite produces scale 1. Doubling its visual scale produces a puff ~2× bigger. A large boss can use `DeathExplosionMultiplier=1.5` for exaggerated feedback; leave every other enemy at 1.0. A multiplier of 0 disables the effect on that type. Minimum/maximum explosion size are shared settings, not per-enemy properties.

## Reuse on towers later

A tower can call `DeathExplosion.Spawn(effectsLayer, tower.GlobalPosition, DeathExplosion.MeasureVisualSize(towerSprite), optionalMultiplier)` **only when destroyed**. Towers do not need to reference `Enemy.cs` or `EnemyDefinition`; the API accepts any world layer and world-size number. The effect has its own lifetime and cleans itself up even after the owner has been queued for deletion.

## Performance and verification

At most `MaxConcurrentExplosions` active scenes (64 by default); additional off-screen/mass deaths can safely skip only the visual, never gameplay effects. GPU materials are defined in the shared scene, while PackedScene/config resources are cached lazily, and each effect automatically frees after its longest particle lifetime plus a short buffer. `TESTS/Combat/DeathExplosionChecks.cs` covers kill-only, duplicate-hit and arrival behavior, default sizing, scaled sprite sizing and multiplier overrides; the heavy headless test fixture turns effects off outside its focused checks.

**Test in Godot (visual):** build C#, F5 → F2 spawn Enemy001 → kill with Battle Tower; expect a short warm orange spark/flash and expanding grey-orange smoke at the death location, independent of moving ship / credits UI. For a larger ship, enlarge its Sprite2D scale and confirm the puff grows without entering any manual explosion size. Godot particle appearance may differ between renderers; tune shared settings/art if needed. No Godot runtime is available in this editing environment, so visual confirmation must be done in the editor.
