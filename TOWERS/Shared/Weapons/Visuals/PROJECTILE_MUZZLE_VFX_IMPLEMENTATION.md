# Projectile Tracers + Tower Muzzle Flashes — Codex Implementation Plan

**Status: PLANNED / NOT IMPLEMENTED BY THIS FILE.**
**Scope:** Add flexible, inexpensive **visual-only** presentation to projectile weapons in the Godot 4.7 C# tower-defense project. **Battle** and **Machine Gun** are the required first implementations. **Sniper** and **Cryo** are useful follow-on presets using the same foundation; the existing Cryo status-effect system is already implemented. This is a Codex brief, not a request to replace combat systems.

## User-facing goal

Projectiles currently look like glowing **round blobs**. Change ordinary gunfire to look more like modern sci-fi shooter **tracers**: a **long, narrow, bright rectangular/linear streak** aligned with the direction of travel, with a subtle colored outer glow and a brighter core/tip. They should read clearly in a top-down vector game at normal zoom.

**Every successfully fired bullet must produce a short-lived muzzle flash at the exact barrel that fired.** Towers will eventually have **individual muzzle-flash shapes, palettes, intensity, size and duration**, regardless of whether they have one, two or several barrels, or fire sequentially/simultaneously. Presets should be edited in the Godot Inspector, not implemented through one tower-specific C# class or enum per tower.

Think crisp and restrained, not heavy particles or full-screen bloom. Combat mechanics stay exactly the same.

## Read current code before making changes

- `NOTES/CodePreferences.md`, `TOWERS/Shared/Weapons/README.md`, `TOWERS/Shared/Projectiles/README.md` and appropriate per-tower READMEs.
- `TOWERS/Shared/Projectiles/BasicProjectile.cs`: the current `_Draw()` draws a **3-pixel circle**; projectile movement is guided and may change direction during flight. Its separate `DamageInfo` and optional `StatusEffectPayload` handling must remain intact.
- `TOWERS/Offensive/BattleTower/BattleTower.cs` + `BattleTower.tscn`: still spawns a bullet from the **tower root/global center**. Has a rotating `TurretPivot/Sprite` but currently **no muzzle Marker2D**. Add one aligned to the actual visible barrel tip.
- `TOWERS/Shared/Weapons/MultiBarrel/ProjectileTurretBase.cs`: already selects `Marker2D` children in `TurretPivot/Muzzles`, spawns a projectile from each selected muzzle, charges ammunition, and supports Single/Sequential/Simultaneous via `BarrelFireController`. The current Machine Gun has `MuzzleA (-9,-25)` and `MuzzleB (9,-25)`; do not replace this selector.
- `TOWERS/Offensive/SniperTower/SniperTower.tscn` and `TOWERS/Offensive/CryoTower/CryoTower.tscn`: both already have one muzzle marker and share the projectile-turret base. Cryo's Energy/status projectile presently draws cyan. **Do not regress its Energy damage or permanent slowdown.**
- Tower configurations live in `*Config.cs` and `Default*Config.tres`; `TowerConfig.CreateInstanceCopy()` uses shallow duplication for shared presentation profiles. Existing beam/shockwave styles demonstrate data-driven style resources.

**Concurrency:** The user may be using Codex concurrently for other passes. Re-read current `main` before editing/pushing. Avoid `UI/`, `ENEMIES/`, `COMBAT/StatusEffects/`, navigation, tower economy and other unrelated files.

## Architectural recommendation: data-driven styles, NOT enums per tower

Create **Godot `Resource` types** (e.g. `[GlobalClass]`) for the two different visual concerns:

### ProjectileVisualStyle (authorable .tres)

Suggested Inspector-editable properties:
- `Enabled` / visible mode; optional old-circle fallback for existing callers with no style.
- `LengthPixels` and `CoreThicknessPixels` (and optional body/glow width multipliers).
- `CoreColor`, `BodyColor`, `GlowColor`, `GlowOpacity` / intensity.
- Optional bright tip, tail-fade/gradient approximation, softness and a **custom Texture2D** path for more complex futuristic ammunition later.
- Lightweight drawn layers (e.g. 2–3 aligned `DrawLine` calls or a small quad/polygon). The bullet length is a visual choice and **must not** change collision reach, speed or damage.
- Keep visuals **direction-aware** for guided/homing shots: orient each projectile's appearance along its latest movement vector; do not merely use static tower rotation or point it backward. Define behavior for zero-motion initial frame and very close target. Avoid allocating new arrays/meshes every frame.

### MuzzleFlashStyle (authorable .tres)

Suggested Inspector-editable properties:
- `Enabled`; `DurationSeconds` (typically **0.04–0.10 s**, visible for more than one frame if appropriate).
- `CoreColor`, `OuterColor`, `Intensity` / opacity, `LengthPixels`, `WidthPixels` / radius, optional spread / point count.
- **Shape variant**: e.g. compact cone, four-point starburst, sharp radial burst, circular ring, **custom texture**. A **small enum for fundamental drawing primitive/shape** is OK; **do not create enums** such as `BattleFlash`, `MachineGunFlash` or one case per future tower.
- Support adjustable rotation and optional small deterministic/pseudorandom variation **without creating an RNG or allocating per draw**.
- Shared style resources should be treated as **read-only authored assets**. Keep fade/progress and other mutable animation state **on the individual flash instance/slot**, not on a shared `.tres`.

### Proposed runtime renderer

- `MuzzleFlashRenderer.cs`: one lightweight renderer **per firing tower/turret pivot**, not a new Node2D/Timer/particle object **per shot**. It can cache a small reusable list/ring buffer of flash records: local muzzle position, orientation if required, elapsed/remaining time, style reference, optional seed.
- Activate its processing only while at least one flash is live; call `QueueRedraw()` only during visible animation. Turn off processing when idle. A fixed/bounded capacity with deterministic overflow policy is fine; ensure rapid alternation and simultaneous volleys never silently miss a flash under ordinary rates.
- Put the renderer in/under `TurretPivot`, or otherwise convert coordinates properly, so flashes track **the correct rotated muzzle position** without double-applying rotations. Draw order should leave the flash visible over the barrel and under/over projectiles as appropriate.
- Firing code calls a small common helper like `ShowMuzzleFlash(marker, style)` **once per actual successfully emitted projectile**, after the launch is funded. Invalid targets, cooldown not met, ammunition exhausted, tower nonoperational/construction: **no flash**. If simultaneous mode fires N bullets, show **N muzzle flashes**, one for each selected marker.
- Consider adding a shared `WeaponVisuals` adapter/helper or a virtual display hook so individual tower types don't duplicate VFX wiring. **Do not** embed visual rules or shape selection into `TowerTargeting`, `DamageResolver` or the existing `BarrelFireController`.
- `BasicProjectile` should accept an optional `ProjectileVisualStyle` along with the existing typed hit/effect payloads, preserving all existing `Initialize` overloads and call sites. It should draw its own tracer directly or delegate to a **static lightweight draw helper**, without an extra scene node per bullet. Missing/disabled style must remain safe.
- For future hitscan/beam/area weapons, use the same style concepts if appropriate, but **do not force every weapon through BasicProjectile or make beams spawn fake projectiles**.

**Codex judgment call:** If a simpler renderer design is materially better for Godot performance, use it and explain the choice. Avoid general-purpose VFX graph systems, dynamically creating materials/shaders per shot, GDScript nodes per projectile, or requiring glow/bloom post-processing for the tracer to be visible.

## Recommended folder structure

The following represents **proposed new files**, not necessarily exact mandated C# types. Preserve existing folder organization and name responsibilities clearly.

```text
TOWERS/
├── Shared/
│   ├── Projectiles/
│   │   ├── BasicProjectile.cs           # MODIFY: optional tracer style + orientation
│   │   └── README.md                    # UPDATE after implementation
│   └── Weapons/
│       ├── Visuals/
│       │   ├── PROJECTILE_MUZZLE_VFX_IMPLEMENTATION.md  # THIS PLAN
│       │   ├── README.md                # Post-implementation guide
│       │   ├── ProjectileVisualStyle.cs
│       │   ├── MuzzleFlashStyle.cs
│       │   ├── MuzzleFlashRenderer.cs
│       │   └── (optional static draw helper/shape enum)
│       ├── MultiBarrel/
│       │   ├── ProjectileTurretBase.cs   # MODIFY: pass bullet style, show flash on fire
│       │   └── README.md                # UPDATE: visual integration contract
│       └── README.md                    # Link to visuals guide
└── Offensive/
    ├── BattleTower/
    │   ├── BattleTower.cs               # MODIFY: use shared style + muzzle renderer
    │   ├── BattleTower.tscn             # MODIFY: add a real barrel-tip Marker2D
    │   ├── BattleTowerConfig.cs         # MODIFY: optional style references
    │   └── Visuals/
    │       ├── BattleTracerStyle.tres
    │       └── BattleMuzzleFlashStyle.tres
    ├── MachineGunTower/
    │   ├── MachineGunTowerConfig.cs      # Reference styles via shared config
    │   └── Visuals/
    │       ├── MachineGunTracerStyle.tres
    │       └── MachineGunMuzzleFlashStyle.tres
    ├── SniperTower/
    │   └── Visuals/                      # Recommended second wave, easy reuse
    │       ├── SniperTracerStyle.tres
    │       └── SniperMuzzleFlashStyle.tres
    └── CryoTower/
        └── Visuals/                      # Optional after verifying existing Cryo work
            ├── CryoProjectileStyle.tres
            └── CryoMuzzleFlashStyle.tres

TESTS/
└── Towers/
    ├── ProjectileVisualChecks.cs        # New visual firing integration tests
    └── README.md                        # Update test coverage notes
```

**Ownership:** Shared `Visuals/` contains behavior and authorable C# types; **each tower** owns its preset `.tres` assets. Configs expose references under an appropriate Godot Inspector export group (e.g. `Projectile / Visuals` and `Muzzle Flash`), ideally using shared config support without polluting passive towers. Style fields are **presentation config**, not part of the eight-stage gameplay stat pipeline. Keep runtime copies and source-resource mutation behavior consistent with the rest of `TowerConfig`.

## Initial artistic defaults (playtest values)

| Tower | Projectile appearance | Muzzle flash |
|---|---|---|
| **Battle** (required) | **16–20 px long**, ~2–3 px core, bright pale yellow/white tip, warm golden/orange body, subtle amber halo | A distinct stronger golden **starburst**; approx. 0.07–0.10 s |
| **Machine Gun** (required) | **10–13 px long**, ~1–2 px core, shorter narrower white/amber streak, soft orange halo | Smaller, sharp **orange/white cone or burst**, approx. 0.04–0.07 s; a flash for *each* left/right shot |
| **Sniper** (recommended) | **24–32 px long** bright sharp precision tracer, minimal halo, more noticeable than Battle | One pronounced angular flash, approx. 0.09 s |
| **Cryo** (optional compatibility check) | Glowing **cyan/light-blue** energy streak with bright icy core, rather than default yellow; carries original Slow impact | Pale cyan crystalline/radial flash; must not touch status simulation |

The goal is distinct silhouettes and palettes, **not** real point lights or volumetric smoke. Procedural layered drawing should still look intentional if WorldEnvironment glow is disabled.

## Implementation stages

### PV1 — Reusable visual resource contracts + tracer bullets

- Build `ProjectileVisualStyle` and make it Inspector-authored with sane numeric validation/fallbacks (no NaNs/negative widths/time).
- Update `BasicProjectile` to render a direction-aligned layered streak using the style at launch. Preserve guided tracking, swept hit detection, 5-second timeout, death/arrival behavior, `DamageInfo`, optional `StatusEffectPayload` and exactly-once impact logic.
- Supply distinct **Battle** and **Machine Gun** bullet styles via their authored tower configs. Preserve every existing actual projectile speed, fire rate, damage type, shield response, target choice, ammo usage and XP.
- Avoid hardcoded `TowerTypeId` checks in projectile rendering. An optional style may still fall back to the original yellow/cyan circle for old callers.

### PV2 — Shared flash renderer + real muzzle origins

- Implement event-driven `MuzzleFlashStyle` and `MuzzleFlashRenderer` with a **small number of flash shape primitives** and optional custom texture rendering for future styles.
- Add a `TurretPivot/Muzzles/MuzzleA` marker to the **Battle Tower** with accurate local barrel-tip coordinates; change its bullet origin to this marker. Keep scene validation/operational construction behavior correct.
- Wire the shared projectile turret base so **Machine Gun** flashes at the selected real marker **A → B → A → B**. **Simultaneous mode** must flash each fired barrel in the same volley. Do not change firing selector, alternate order or shot cadence.
- Bind each tower's flash style in its own resource/preset. Never create per-shot flash nodes, per-shot timer nodes or particle emitters.
- If the renderer needs an extra tower child for lightweight drawing, that is fine. Visual effects must clean up with the tower on removal. **Bullets remain independent** after being fired.

### PV3 — Regression tests, optional presets, docs

- If trivial, add **Sniper** style and/or **Cryo** style using the same plumbing; don't expand scope into status effects or new damage mechanics. Test Cryo separately if included.
- Add focused headless checks and integrate with the appropriate existing test harness. Visual appearance also requires a short manual check at normal gameplay zoom.
- Document how to make an entirely new tower's projectile/muzzle style **by creating two new resources and assigning them**—no new C# visual class or per-tower enum needed.
- Update changed feature READMEs with what is actually completed, and provide compile/test evidence. Do not claim tests passed if Godot/.NET tooling was unavailable.

## Mandatory acceptance tests / no-regression rules

1. **Battle:** one paid/valid shot produces exactly one tracer and one flash from its muzzle marker; not from tower center. Invalid/zero-ammo/nonoperational shots create **none**.
2. **Machine Gun:** ten consecutive shots retain the **A–B–A–B** barrel sequence; each launch flashes at the same barrel the projectile starts from. At default Level 1 the tower remains **12 total bullets/s** across both barrels, not 12 per barrel.
3. **Simultaneous N-barrel:** N successful shots = N markers used, N flashes, N ammo charges. An unfunded volley causes **zero** projectiles or flashes.
4. **Travel alignment:** guided projectile tracers face the real motion heading after the target moves; the tracer has a visible bright tip and a tail behind travel. Near-zero travel must not cause NaN/invalid rotation.
5. **Gameplay parity:** unchanged damage/hit counts, shield penetration, status application, tower XP, target priorities, ammunition accounting, projectile speed, range, paid level/rank stats, construction/upgrade gating, normal death/removal and shot lifetime.
6. **Cryo compatibility:** if style supplied, its bullet stays cyan with the original Energy + 33% permanent slow behavior; if not supplied, its current cyan fallback remains intact. Never turn status bullets yellow due to a missing style.
7. **Resource isolation:** authoring one tower's `.tres` can change its colors/shape without changing another tower; shared references are not mutated per flash, shot, upgrade or selection.
8. **Performance:** no per-shot extra scene nodes; no per-shot material/texture creation; renderer processing idle when no flashes; bounded active-flash state; no new per-frame allocations in ordinary firing/drawing. Test rapid Machine Gun fire and multiple active towers at normal 60-Hz physics and gameplay zoom.
9. **UI independence:** do not change the CTRL build HUD, Inspector layout or build catalog logic for this VFX pass.

## Codex completion report

When complete, report:
1. **Implemented paths** and how resource-style presets are assigned to future towers.
2. **Which visual shapes and fallback behaviors** work; whether custom textures are supported.
3. **Compilation/headless tests** actually executed and any manual visual checks.
4. **Performance approach**, limitations (e.g. faux glow vs real bloom), and remaining optional Sniper/Cryo styling.
