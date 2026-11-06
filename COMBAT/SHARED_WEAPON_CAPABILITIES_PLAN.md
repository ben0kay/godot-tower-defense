# Shared weapon capabilities — CW1 recoil / CW2 hostile splash

**Status:** PLANNED / CODEX HANDOFF (2026-11-06). Work on `ben0kay/godot-tower-defense`, **`dev` only**. Implement CW1 first; CW2 is a **separate later task**. Read `NOTES/CodePreferences.md`, root README, `COMBAT/README.md`, the recoil/AoE docs and current source before editing; check `AGENTS.md` if present.

## Current reusable systems — do not duplicate

- `TOWERS/Shared/Weapons/Recoil/TowerRecoilController.cs` / `TowerRecoilStyle.cs`: real shared, active-only visual kick/settle, but tightly coupled to `TowerBase` disable/destroy lifecycle. Existing tower presets and serialized resource/scene paths must survive unchanged.
- `ENEMIES/Weapons/Shared/EnemyWeaponMount.cs`, `Minigun/EnemyMinigun.cs`: aimed independent mounts. Gunner Drone, Heavy Gunner Drone and the Obsidian Dreadnought's **four independently aimed guns** use `TurretPivot/GunSprite/Muzzle`. The actual accepted-shot seam is `EnemyBulletSystem.TrySpawn(request) == true`.
- `COMBAT/AreaDamage/AreaDamage.cs` is **already the shared Blaster Cannon splash system**, providing `AtDistance` falloff, `DamageInfo` forwarding and enemy-recipient `ApplyInstant` via `EnemyRegistry`. **Extend; don't relocate or rebuild it.**
- `ENEMIES/Projectiles/EnemyBulletSystem.cs` batches hostile shots in compact world-owned slots; existing normal bullets hit one `TowerBase`.

**Ownership:** actual damage/target recipients in `COMBAT/`; visual-only recoil in `FEEDBACK/Weapons/Recoil/`; hostile aiming, firing rate and config remain in `ENEMIES/Weapons/`; hostile bullet movement remains in `ENEMIES/Projectiles/`.

## CW1 — shared weapon recoil for friendlies and enemies (first Codex pass)

1. Extract the tower recoil **motion logic into a small weapon-neutral visual component/style**, with a backward-compatible wrapper/adapter retaining existing `TowerRecoilController`/`TowerRecoilStyle` classes and Godot paths. Do **not** mass-migrate tower scenes/presets, duplicate the motion, or change friendly tower behaviour.
2. Move only the **gun's local visual assembly** backward (+Y for current up-facing art), never the ship hull, tower root, aiming pivot, projectiles or physical firing origin. Snapshot muzzle origin/direction and flash **before** kicking. One bounded accumulated kick/return per mount, exact pose reset, Inspector-editable enable/distance/timing/cap, zero processing at rest; no per-shot tween, timer or node allocation.
3. Opt in Gunner Drone, Heavy Gunner Drone and each of the **four Capital cannons** with separate small recoil presets (illustrative distances: 0.8/1.5/2px). Kick **only after a successful** `EnemyBulletSystem.TrySpawn`; no recoil on rejected, paused, blocked, disabled, frozen or unfunded shots. Stop/cancel on host death/arrival, reinitialize, configuration changes or unload; preserve pause/offscreen policy.
4. Minimal checks: friendly recoil/levels/upgrade sprites unchanged; hostile muzzle flashes and bullets originate from real barrel tip, recoil never changes target/accuracy/trajectory/cadence; four Capital guns independent, no drift after repeat fire, no idle CPU ticks.

## CW2 — optional enemy explosive splash against friendly buildings (later separate pass)

1. Keep existing `AreaDamage.AtDistance` and friendlies' `ApplyInstant(..., EnemyRegistry)` working exactly as now. Add an explicit **`TowerRegistry` recipient path** for hostile splash. Query nearby eligible live towers through existing spatial indexing, snapshot recipients safely before damage/death callbacks and apply `DamageInfo` via `TowerBase.ApplyDamage`. Count each building **once**, regardless of 1×1/4×4 footprint; document nearest-point-on-footprint radius distance.
2. Add Inspector-editable **opt-in** radius, edge retention and blast visual to hostile weapon config/shot request/batched slot. On the **first real impact**, use the actual contact point, consume the bullet, run one AoE hit pass and one bounded visual burst. **No second direct-hit damage** at the center. Normal hostile bullets remain one-target with current speed, capacity, hit behaviour and visuals; ordinary miniguns and all Capital guns remain non-explosive by default.
3. Respect existing terrain/line-of-sight, friendly building shield/hull/disable/death rules, and standard `DamageType`/response coefficients; hostile shots must not claim friendly-tower `DamageSource`. No enemy-on-enemy splash, new projectile node per bullet, full-tower scans, extra manager or hidden changes to missions, waves and rewards. Introduce one **explicitly opted-in explosive enemy gun** only when separately requested.
4. Minimal checks: single/multi-cell recipients, exact edge/center falloff, shield/hull, overlapping explosions, destroy/unregister during callbacks, missing/rejected shots, pooled slot reuse, and Blaster Cannon unchanged.

## Delivery

Do not implement CW2 as part of CW1. Keep each phase to one focused commit on **`dev`**, then update this status, owning README(s) and backlog. Per owner preference, do only short source/resource inspection and at most one readily available build; leave visual and FPS gameplay acceptance to the owner. **This document implements neither mechanic.**
