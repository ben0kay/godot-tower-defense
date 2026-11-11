## STG0 — Class-based directional impact stagger (revised plan 2026-11-10)

[Implementation specification](ImpactStagger/IMPLEMENTATION_PLAN.md). Blaster staggers Light and Standard ships. Rocket staggers Light ships. Surviving hits that reduce hull or shield briefly slow eligible ships. Visible ships without hull-mounted weapons also get an outward hull and thruster shove. The right-side **Impact Stagger** switch controls both effects and starts from `DefaultFeedbackConfig.tres`.

---

## CW1 — weapon-neutral recoil (planned 2026-11-06)

**Plan only.** Extract the existing tower recoil's visual motion into shared `FEEDBACK/Weapons/Recoil/` ownership with backwards-compatible tower wiring, then opt in Gunner/Heavy Gunner and four Capital gun mounts on accepted shots only. Active-only, bounded and visually cosmetic; no recoil code moved yet. [Codex handoff](../COMBAT/SHARED_WEAPON_CAPABILITIES_PLAN.md).

---

## Disabled tower feedback and stable NoPower presentation — 2026-11-05

**Source Implemented / Awaiting Owner Validation.** Placed disabled towers now show a small amber outlined exclamation triangle above the upper-right footprint corner and a subtle red art tint. `FEEDBACK/Disabled/DefaultTowerDisabledStyle.tres` tunes tint, badge color/size/offset. The shared TowerBase condition notification attaches one lazy `TowerDisabledVisual` only after native artwork initialization and only when needed. Initial construction sites hide the warning; completed disabled structures and paid upgrades show it. Work/disable/death events update presentation. No polling, bobbing animation, timer, active shader or per-frame redraw; active/never-disabled towers allocate no helper.

The helper caches Sprite2D descendants in scriptless art branches once, excluding scripted feedback/effect and GUI subtrees. Sprite SelfModulate multiplies a captured authored tint, leaving selection Modulate intact; split art, recoil and texture swaps keep their existing owners. All captured colors restore exactly on recovery/removal; overlapping reasons never compound the tint or clear it prematurely. Custom future art under a scripted container will need an explicit presenter extension rather than tinting arbitrary effects.

Inspector no longer emits a duplicate `NO POWER` entry in the Main warning row; other reasons retain their counts. The shared disabled header and ENERGY bar remain. PowerDetails keeps its three explicit lines at a fixed 48px minimum with wrapping disabled/clipping enabled, so changing connection/recovery text cannot add rows or grow the panel. Full power explanations remain in the existing tooltip. No power arithmetic, disable ownership, upgrade behavior, save data, gameplay balance or selection semantics change.

Focused HeatInspectorChecks expectations now omit duplicate NoPower and include dormant badge lifecycle. Source/scene review only; no Godot/.NET runtime or desktop layout test run. Owner: select an empty/recovering Battle, watch the panel during charge/depletion, inspect unselected disabled towers, EMP plus NoPower/heat overlaps, recovery, selection changes, upgrade art, 2×2/3×2 buildings and a fresh Save/Load. Verify that icons stay at the fixed upper-right corner and other gameplay effects retain their normal colors.

---

## HI1 — hull-only kinetic impact sparks (2026-11-04, source implemented)

Confirmed **hull HP loss** from Battle, Machine Gun and Sniper shots now requests tiny white-hot/yellow/orange streaks from one pooled, capped `World/Effects/HullHitSparks` renderer. Shield-only contacts, missed shots and zero effective hull damage emit **nothing**; an actual penetrating hull hit can spark even if shield absorption also occurred. No `GPUParticles2D`, per-impact nodes, new hit collisions or modified damage resolution. One sleeping CanvasItem manages short-lived line fragments with a default maximum of 320 simultaneously active flecks. Per-weapon burst sizes: Battle 4, Machine Gun 2, Sniper 6. See [Hull Hit Sparks](HullHitSparks/README.md) for Inspector controls and the two hit paths. In-game visual/F4 FPS acceptance pending.

---

## Selected tower presentation — aqua/red overlay (2026-11-03)

`Selection/` contains one reusable selected-tower hologram and stronger aqua/red screen grid/tint + a world-anchored name caption to the tower's right. The current `TowerPlacementController` selection and disabled/work events drive it, so normal states use aqua and disabled/upgrading states use soft red. No additional per-tower nodes or gameplay change. Configure colours, grid, pulse, text and opacity in [`Selection/DefaultTowerSelectionStyle.tres`](Selection/DefaultTowerSelectionStyle.tres). Source implemented; owner in-game review pending.

## Shield ricochet — SR1–SR4 (2026-10-21)

`Ricochet/` owns a bounded world-space cosmetic streak buffer with angle-aware shield reflections. Only opted Kinetic projectiles with confirmed shield absorption request it. Master toggle ShieldRicochetsEnabled; shared DefaultRicochetStyle controls chance/motion/cap. No per-round nodes, collisions or gameplay effects. See Ricochet/README.md for tuning, checks and pending desktop review.

## Stealth opacity — SE1 (2026-10-20 UTC)

`Stealth/` adds configurable event-driven hidden/revealed opacity, composing with existing status tint and dimming shield/defense feedback. It owns no targeting rules or reveal membership.

# FEEDBACK — Combat and World Response

Reusable visual feedback systems. **DefenseBars and DeathExplosion are now active on Enemy001**, while DamageNumbers and CameraEffects remain reserved.

- `FeedbackMasterConfig.cs` + `DefaultFeedbackConfig.tres`: shared feedback visibility toggles.
- `DeathExplosion/`: reusable automatically-sized kill smoke and orange flash using GPU particles.
- `Shared/`: reusable particle textures and process-material presets. The scene/effect that uses them owns its own `GpuParticles2D` nodes, lifetime and position; currently the shared beam renderer references small energy-contact sparks.
- `Shields/`: one shared, tinted, auto-scaled shield aura and hit/break tween component; lazily attached only to currently shielded enemies. Intended to be reusable by future damageable towers without implementing tower shields or regeneration.
- `DefenseBars/`: event-driven combined **hull + shield** indicators used by inherited enemy scenes and reusable for future damageable towers. The shield meter renders as an extra row within the *same* `DefenseBar` node only while the owner has shield points. Configure its colors, capped logarithmic sizing and offset in `DefenseBars/DefaultDefenseBarConfig.tres`.
- `WorldCallouts/`: reusable world-positioned text for tower progression and future statuses. See [World Callouts](WorldCallouts/README.md).
- `DamageNumbers/`: future hit numbers and damage aggregation.
- `CameraEffects/`: future impact shakes (normal pan/zoom remains in `CAMERA/`).

Keep feedback separate from combat simulation. Health values come from owners: bars never poll entity state or register per-frame processing. Rotating ships update a visible bar's counter-rotation during their existing movement callback. See `DefenseBars/README.md`.

## Shared DeathExplosion (implemented)

`DeathExplosion/` owns a reusable, **one-shot `GpuParticles2D`** orange flash + smoky grey/orange burst, with soft SVG particle artwork. This is an independent world effect, not a child of an enemy or tower being freed. `World/Effects` is the shared canvas layer for effects. `DeathExplosion.Spawn(layer, worldPosition, visualSizePixels, multiplier)` works for enemy kills now and future destroyed towers. The effect scales automatically from the source `Sprite2D.GetRect()` and the lengths of its global transform axes; per-type `DeathExplosionMultiplier=1.0` is an **optional** override. Edit `DeathExplosion/DefaultDeathExplosionConfig.tres` for shared scale, duration, particle count and active-effect cap; edit particle materials/colors in `DeathExplosion.tscn`. The feedback master config can disable effects. Arrival/removal without death does not explode, and effects self-delete after finishing. See `DeathExplosion/README.md`.

## Shared shockwave presentation — Phase4E

Shockwave/ owns one reusable ShockwaveEffect scene and Inspector-editable ShockwaveStyle. Blaster supplies its own orange impact style and uses autonomous cosmetic expansion after instant damage. The new Shockwave Tower supplies a purple style and drives the same visual with its actual gameplay wavefront radius. The visual has no enemies or damage code. The former AreaBlast/BlastFlash was removed; AreaBlast/README.md points to the replacement. DeathExplosion remains a separate kill-only visual.

## Shared particle resources — beam visual foundation

[`Shared/README.md`](Shared/README.md) describes the reusable monochrome spark texture and gentle energy-contact material. The `TOWERS/Shared/Weapons/Beams/BeamRenderer.tscn` scene owns its impact/muzzle GPU particle emitters and toggles them only while the beam is active. The shared folder owns **resource presets**, not runtime emitters or a new manager. Other weapon effects can instance or reference these assets independently.

## Shield visual presentation (SV1)

[`Shields/README.md`](Shields/README.md) documents a single reddish baked SVG aura that scales to the owner's sprite and remains idle between shield-change events. Enemy shields already exist in `COMBAT/Damage` and `ENEMIES/Shared`; visual nodes contain **no gameplay shield state**. No new regeneration is implemented. `Shared/` continues to hold cross-effect resources rather than another copy of the shield system.

