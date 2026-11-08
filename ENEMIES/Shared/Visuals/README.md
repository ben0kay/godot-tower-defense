## VS1 — Visual Scale

Set **Visual Presentation → Visual Scale** on an enemy definition. Default is 1.0, finite values clamp to 0.25–4.0, and nonfinite values resolve to 1.0. The hull transform scales attached guns, recoil and flash renderers; live shots still use the marker's world position and their original projectile preset. Range and terrain-sight checks retain authored mount offsets. F2 applies the same hull transform to its off-tree sprite preview.

The existing thruster, defense bar, shield aura, status feedback and death effects use the scaled framing. Damage stages replace the hull texture only; keep stages registered to the same canvas. Death effects still respect the shared effect's size cap. The enemy root and gameplay indicators such as the Shield Emitter's 192px ring remain in gameplay units.

---

## ET1 flame size + speed contribution — 2026-11-04

ET1's rear flame **width still follows displayed hull height**, but **length now combines overall displayed hull dimensions and the enemy's authored max speed**. A slow Heavy Transporter or Cruiser now receives a substantially longer (and still broad) ignition, while small fast ships remain narrow with longer speed-driven plumes. The optional GPU fleck speed is scaled to the resulting flame length, rather than solely to authored speed, without increasing emitter/particle count. Settings and formulas: [Thrusters README](Thrusters/README.md). Source-only; visually verify in Godot.

---

## ET1 — shared enemy engine ignition (source implemented 2026-11-03)

All inherited enemy hulls now have a rear-centred optional `Thruster` scene. The shared [Thruster folder](Thrusters/README.md) owns a white/yellow/orange SVG flame, tiny GPU exhaust particles, `EnemyThrusterVisual.cs`, a typed `EnemyThrusterConfig` and the default `.tres` resource. **Width depends on actual displayed sprite height; length depends on authored `EnemyDefinition.MoveSpeed`.** Right-facing (+X) sprites exhaust toward -X and follow rotation. Per-ship Inspector `ThrusterEnabled` and `ThrusterMountOffset` are optional; no ship-ID or fleet-family switch.

`Enemy.Initialize` configures each scene once. Slow/Freeze transitions notify it to adjust intensity; existing stealth events dim it along with the ship. Cosmetic only: combat, shields, movement math, rewards, saves and wave definitions remain unchanged.

**Performance:** an inexpensive SVG flame draws on screen. An optional `GPUParticles2D` child has 4 particles/ship and wakes only from `VisibleOnScreenNotifier2D` signals or status changes, capped at 64 simultaneously active GPU emitters. There is **no new `_Process` or `_PhysicsProcess`**; emitters stop offscreen, on Freeze, on death/arrival, and on unload. Beyond the cap, ships retain the flame image without GPU flecks. Local Godot.NET visual/F4 acceptance is still pending.

---

# Shared static enemy hull-damage artwork

This is a **cosmetic, opt-in, event-driven** feature for existing static `Sprite2D` enemies. No animation tree, extra enemy child node, recurring timer or per-frame traversal.

- `EnemyDamageAppearanceProfile.cs`: Inspector-editable resource containing `Stages`, an ordered `Godot.Collections.Array<Texture2D>` with index 0 as pristine and the final index as most damaged.
- `EnemyDamageAppearance.cs`: pure `StageForHealth(health, maxHealth, stageCount)` calculation. For `N` textures it divides the normalized **hull** health range into equal `1/N` bands, using a floor/clamp and returning `-1` for an empty profile. Three textures yield three equal thirds; four give quarters; arbitrary positive counts work without adding any code.
- `EnemyDefinition.DamageAppearance`: optional exported resource reference. The default is null. There are no special case enemy IDs or scene lookups.
- `Enemy.cs`: on initialization, caches the optional resource and existing `Sprite` node, resets its stage, and assigns the texture for current HP. On a successful damage commit it refreshes the art **only when hull HP actually changes** and only writes `Sprite.Texture` when the stage changes. Shield damage, shield restoration and movement do not cause visual updates. No profile means no extra scene node, profile scans or resource loads. The instance clears its references when freed.

**Authoring:** create a profile `.tres` for a type, fill `Stages` with same-sized right-facing artwork (healthy first), and set `Damage Appearance` on that type's `EnemyDefinition.tres`. Keep all textures registered to the same pivot/footprint; changing texture dimensions can affect defense-bar/shield visual framing. If no profile or its stage is empty, the existing scene sprite remains unchanged. Clearing the reference disables the feature. Use `ENEMIES/Types/Enemy001/Visuals/Enemy001DamageAppearance.tres` as the first example.

**Behaviour:** health values are clamped to 0–100% when selecting art, 100% is pristine, and a zero-HP enemy is assigned the last stage before its death/removal. There is currently no hull-healing system, but routing future healing through the same hull change path would update stages in either direction. Shield-only hits do not count as hull damage. Sprite tint (Slow/Freeze/Stealth) and any weapon effects remain owned by their existing systems.

**Scope:** only `Enemy001` is configured. This initial pass does not add particle sparks, debris nodes, new defence modifiers or animation; owner checks artwork readability in Godot at gameplay zoom. Source check only.
