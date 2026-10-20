# Tower Recoil — shared foundation + Battle-only visual pilot (Codex plan)

> **Status: PLANNED, NOT IMPLEMENTED.** This document is a handoff to Codex. No recoil scripts, scene nodes, style resources, art splits, or animations are implemented by this planning pass. **Only the Battle Tower** should opt into the first implementation. All other towers retain their current visuals until explicitly requested.

## Intent

Add a short, physical-looking gun kick when a **real shot** fires: the moving barrel/turret visibly jolts backward in its local firing direction and quickly settles. It is **cosmetic only**; no accuracy, target tracking, damage, hit timing, projectile trajectory, barrel-origin rules, ammunition consumption, heat, construction, XP, rank, range or upgrade calculations may change.

The user previously liked a GameMaker tower built from a *fixed hull/base* and a *recoiling gun*. Godot 4 C# can do this using ordinary Node2D transforms; **Skeleton2D, AnimationTree and one AnimationPlayer per tower are unnecessary**. Prioritize small, simple, inspector-tweakable and performance-friendly code.

Read before implementation: `NOTES/CodePreferences.md`, `TOWERS/Shared/Weapons/README.md`, `TOWERS/Shared/Weapons/Visuals/README.md`, `TOWERS/Offensive/BattleTower/README.md`, and `TOWERS/Shared/Construction/README.md`.

## Current Battle Tower — verified source paths

```text
BattleTower                           (fixed gameplay root / placed cell)
└── TurretPivot                       (TurretAiming.Track rotates this)
    ├── Sprite                        (existing single 64×64 SVG, base + gun together)
    └── Muzzles
        └── MuzzleA                   (barrel-tip projectile + muzzle-flash origin)
BattleTower/ConstructionVisuals       (SpritePath = ../TurretPivot/Sprite)
```

- `BattleTower.cs` caches `TurretPivot`, `TurretPivot/Sprite`, and `TurretPivot/Muzzles/MuzzleA`; `SetSelected` tints `Sprite.Modulate`.
- A funded shot calls `Ammunition.TryFire()`, creates the existing `BasicProjectile`, then `WeaponVisuals.Present(bullet, _muzzle, Config, _visuals)`, which positions the projectile at the actual `MuzzleA.GlobalPosition` and snapshots a flash.
- `TowerConstructionVisuals` changes **the same Sprite's Texture** through its configured NodePath. The existing SVG includes **both** stationary base and gun. Neither the pivot nor root is a suitable recoil transform.
- Existing beam/projectile/flash components are already shared and must keep working; do not change firing cadence or launch points.

## Proposed files for Codex to create

```text
TOWERS/Shared/Weapons/
└── Recoil/
    ├── README.md                    # this plan; update status after implementing
    ├── TowerRecoilController.cs     # shared local visual kick/return, sleep when idle
    └── TowerRecoilStyle.cs          # optional exported visual-only Resource

TOWERS/Offensive/BattleTower/
├── Visuals/
│   └── BattleRecoilStyle.tres       # Battle-only tuned preset
├── BattleTowerConfig.cs            # optional Recoil style export
├── DefaultBattleTowerConfig.tres   # assigns Battle style
├── BattleTower.cs                  # hooks real funded shot into shared recoil
└── BattleTower.tscn                # only change if needed, preserve existing paths

TESTS/Towers/
└── TowerRecoilChecks.cs            # focused regression checks; wire to suitable runner
```

Do not add recoil properties to every `TowerBase`, require other towers to add nodes, or make new configuration rows in `BattleTowerLevels.cs`. Recoil is **presentation data**, not an upgrade stat.

## R1 — shared controller, only Battle Tower enabled (implement first)

1. Add `[GlobalClass] TowerRecoilStyle : Resource` with Inspector-grouped, bounded/validated settings: `Enabled` (default true only on the Battle preset), `KickDistancePixels`, `KickSpeed` / `ReturnSpeed` or damping/stiffness, `MaximumOffsetPixels`. Suggested **first look**, not a mandate: approximately **2–3 px** backward recoil, **40–60 ms** kick and **120–180 ms** settling. Use reasonable finite-value clamps; avoid wild 64px sprite offsets.
2. Add a reusable `TowerRecoilController` that drives an *explicit* visual `Node2D` target's **local Position**, not tower root/global position or the rotating `TurretPivot`. The Battle SVG points up (local -Y), so visual recoil is local **+Y**. Save and restore the target's original local position, including on cancellation, disable, scene exit or replacement. If no target/style, the feature is inert.
3. Choose a low-cost animation approach: a bounded kick-and-return state or spring impulse with **one active-only process callback on the controller**, sleeping at rest. Combine repeated shots into the existing state with a maximum displacement so high fire rates do not spawn overlapping tweens, timers, or per-shot nodes/resources. No per-frame updates when stationary, no per-shot allocations, and no shared mutable Resource runtime state.
4. In the **Battle Tower only**, create/bind this shared controller once (scene child or lazy attach; pick one and document it). Use an Inspector-assigned `BattleRecoilStyle.tres` in `BattleTowerConfig` or another equally discoverable, optional configuration field. Future weapons can reuse the exact controller but must **not** be modified/opted in during R1.
5. Trigger the visual kick **only after** an actual ammo-funded shot is created and `WeaponVisuals.Present` snapshots its muzzle origin/flash. Never kick on target acquisition, aim alignment, cooldown, empty ammo, paused gameplay, incomplete construction or disabled/overheated weapons. A recoil cannot alter the projectile's launch point or muzzle flash already emitted.
6. **Important initial artwork limitation:** `TurretPivot/Sprite` currently contains the entire tower (including its static base). For the **first visual experiment**, Codex may apply a *subtle* recoil offset to that existing sprite to avoid introducing an art split and breaking construction paths. This is a controlled preview, **not** the final desired split-hull visual. Keep `TurretPivot/Sprite`, `TurretPivot/Muzzles/MuzzleA`, and `ConstructionVisuals.SpritePath` valid. Keep `MuzzleA` fixed for R1 and launch before applying visual recoil; the very small visual kick should make any barrel-marker mismatch unobtrusive. If it looks wrong because the base shifts, move to R2 rather than hiding it with new gameplay adjustments.
7. During initial construction, do not run recoil. During paid upgrades, respect the existing old-level attack/operational behavior rather than suppressing or adding shots. Sprite texture changes, construction progression, selection tint and upgrade artwork must remain intact. Do not change construction/weapon scripts beyond the minimal hookup needed.

### R1 acceptance checklist

- [ ] Battle Tower level 1 can fire continuously and shows obvious but restrained backward recoil while its `TurretPivot` keeps aiming normally.
- [ ] The recoil preset can be enabled/disabled and tuned from the Inspector without recompiling; when disabled, the original sprite position remains unchanged.
- [ ] All purchased Battle levels, ranks and research use the same visual behavior with **unchanged** fire rate, projectile spawn point and damage.
- [ ] Projectile muzzle flash remains at the actual barrel-tip marker **at shot time**, and is still visible; no shot kick occurs when ammo is empty.
- [ ] Repeated bursts recover smoothly without unbounded offset, residual drift, or multiple animations fighting for the same transform.
- [ ] Towers undergoing initial construction never recoil; finished/operational towers do. Tower removal/disabling/reset/scene unload is safe, with no leaked callbacks or processing.
- [ ] Existing `SetSelected` sprite tint, construction stage art, upgraded final artwork and scene validation still work.
- [ ] Sniper, Machine Gun, Rocket, Blaster, Cryo, Stasis, Laser, Shockwave and passive towers are **unchanged**.
- [ ] Add automated regression assertions for true shot versus unfunded/no-shot recoil, bounded repeated impulses, recovery and transform reset; verify the look manually at actual zoom (headless tests cannot judge animation aesthetics).
- [ ] Update this README with actual created files, chosen motion math, test execution and known limitations; **one GitHub commit for this Codex job**, per `NOTES/CodePreferences.md`.

## R2 — fixed base + recoiling gun/barrel art (future, do NOT implement yet)

Once the R1 feel is approved, split **only the Battle Tower** SVG into a stationary base and a separate turret/head/barrel while preserving the existing 64×64 alignment and muzzle direction. A possible final scene layout is:

```text
BattleTower
├── BaseSprite                        (does not aim, does not recoil)
├── TurretPivot                       (aims as before)
│   └── RecoilRoot                    (moves back local +Y)
│       ├── TurretSprite              (gun/turret art only)
│       └── Muzzles/MuzzleA           (moves with physical barrel)
└── ConstructionVisuals              (must be reconciled with split sprites)
```

R2 **must explicitly address** `TowerConstructionVisuals.SpritePath`, `BattleTower.ValidateScene`, `SetSelected`, the existing unfinished construction-stage textures, optional upgrade art, MuzzleA launch timing and `WeaponVisualRenderer` flash placement. Otherwise a casual reparent will silently break stage art and firing. The exact node layout above is illustrative, not an instruction to break paths. Launch/projectile/flash must be snapshotted **before the recoil impulse**. Only after this split should barrel and muzzle travel together through recoil.

R2 is a separate requested visual/art task; do not silently perform it under R1.

## Future reuse — not part of first Codex pass

The shared controller should accept a different style for Sniper (larger/slower), Machine Gun (tiny rapid), Blaster (heavy), dual-barrel Rocket (possible per-barrel recoil only with independent visual targets), and energy weapons (subtle pulses). Laser/continuous beams and radial Shockwave need separate decisions; **do not** wire them blindly to projectile-shot hooks. Optional procedural spring behavior is preferred to heavyweight 2D rigs for this mechanical task.

**Scope summary:** Codex job = shared recoil foundation + one Battle-only working, tweakable preview + tests/docs. Not a game-wide animation rewrite. All other towers remain as they are.
