# Shared shield aura — visuals only (SV1)

This folder owns the reusable **aura visual**, not the shield damage mechanic or the shield **meter**. The slim shield bar above hull health lives in [`FEEDBACK/DefenseBars`](../DefenseBars/README.md) and is drawn in the already-inherited `DefenseBar` node when shields are present. It can be attached to **any** shield-owning gameplay entity (enemies now, optional towers later).

## Architecture: keep combat separate from feedback

- **Gameplay shield state:** `Enemy.CurrentShield` and `EnemyDefinition.MaxShield` remain in `ENEMIES/Shared/`. `COMBAT/Damage/DamageResolver.cs` still owns typed shield-first absorption, spillover and penetration. Do not duplicate them here.
- **Rendering:** `FEEDBACK/Shields/ShieldVisual.tscn` owns its own `Sprite2D` and short triggered tweens. `ShieldVisual.cs` accepts only `SetVisualSize(Vector2)`, `SetShield(previous, current, animate)` and `ResetVisual()`; it does not reference `Enemy`, `TowerBase`, `DamageInfo`, damage types or regeneration.
- **Reusable assets:** The single baked-looking `Artwork/ShieldAura.svg` belongs here because it is shield-specific. `FEEDBACK/Shared/` is for assets such as the energy spark materials used by multiple unrelated effects. Put future broadly reusable material/textures there, not the shield controller.
- **Per-instance styling:** `DefaultShieldVisualStyle.tres` (Inspector) sets warm reddish-orange tint, **1.65× auto-sizing**, **0.82 idle alpha**, hit/break peak opacity of **1.0** and short fade durations (0.28s / 0.38s). Changing style cannot affect actual shielding or combat.

## Current runtime behavior

1. **Unshielded spawn / zero shield capacity:** no `ShieldVisual` node is created; no `_Process`, extra draw calls or additional setup.
2. **Shielded spawn:** `Enemy.Initialize` lazily adds exactly one `ShieldVisual` child, placed behind the ship by `z_index=-1`. Its sprite scales once from the owner's authored Sprite2D rectangle and scale (or physical radius fallback). Full/current shield does not pulse or animate while idle.
3. **Shield damage:** `Enemy.CommitDamage` sends old/new points to the visual after the authoritative resolver commits. The same tinted aura flashes, then smoothly returns to idle. **Repeated damage during an active hit tween does not restart it**, bounding animation churn for a continuously firing laser.
4. **Shield break:** the aura briefly brightens and fades to hidden when the shield reaches zero. The burst contains no per-tick particles or shaders.
5. **Existing ShieldEmitter restores shields:** the same recipient visual becomes visible again if the shield rises from zero. It remains a single child; restoration is **not natural regeneration**.
6. **Death, arrival, scene unload:** shield visuals stay owned by the enemy and are released with it. No detached effect persists after the owner is removed.

The baked monochrome SVG has a **lightly translucent filled centre**, a **much brighter and thicker radial-gradient perimeter**, three soft wide outer ring layers, a strong crisp edge, and several **angular electrical filaments with bright contact nodes**. All details are embedded in one SVG and share the Inspector tint. No custom image file is required for each enemy. The effect uses zero `GpuParticles2D` for now; if the design later needs a light impact spark/break burst, own the emitter under `ShieldVisual.tscn` and reference broadly reusable assets from `FEEDBACK/Shared/`.

## Files

```text
FEEDBACK/
  Shared/                              # reusable cross-effect assets, unchanged
  Shields/
    README.md
    ShieldVisual.cs                    # visual lifecycle only; no combat data
    ShieldVisual.tscn
    ShieldVisualStyle.cs               # Inspector-visible visual knobs
    DefaultShieldVisualStyle.tres
    Artwork/ShieldAura.svg             # one shared art asset; scaled and tinted
```

## Reusing it on future towers

After a tower health/shield capability actually exists, that separate durability owner can instance `ShieldVisual.tscn`, set its position/size, then call `SetShield(oldShield, newShield)` only after actual shield state changes. The visual requires no enemy-specific calls and **does not imply that tower health, tower shields, tower-targeting enemies or natural regeneration are implemented**. Future damageable towers should reuse the existing typed damage resolver rather than storing authoritative shield state in the visual.

## Testing and performance boundaries

`TESTS/Combat/EnemyShieldChecks.cs` now asserts that shielded instances lazily own a visible shield aura, ordinary zero-shield enemies own none, shield-only hits flash without hull loss, shield breaks trigger visual fade, and an explicit restoration reuses the existing visual. Existing ShieldEmitter/typed damage tests still exercise source and gameplay isolation.

**Latest visual revision (2026-10-17):** replaced the formerly barely visible outline with a filled red-orange shield and baked electrical zaps, raised initial opacity, increased size, and explicitly enabled linear texture sampling for smooth edges. No GPU particles/shaders were introduced. **Not yet executed in Godot/.NET in this GitHub-only pass.** Visually check small Enemy001 shields, Swift Interceptors and wide Transporters, hit/break/restoration, top-down draw order, paused tweens, and 500-enemy wave frame times. No shader/geometry animation, per-frame shield polling, new regeneration, tower durability or HUD shield bars were added.
