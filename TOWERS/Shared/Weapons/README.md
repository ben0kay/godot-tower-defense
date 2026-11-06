## Tighter aimed-turret firing alignment — 2026-11-05

**Source implemented / Awaiting Owner Gameplay Validation.** Most aimed enemy-firing towers previously accepted **12°** of post-rotation error (Sniper, Cryo and Stasis accepted **8°**), making weapons fire while visually pointing to one side of a target. The **absolute Level 1–3 authored rows** now use narrower values, with no new processing logic, turn-speed edits or shared aiming behavior changes:

| Tower | Previous tolerance | Current tolerance |
| --- | ---: | ---: |
| Battle | 12° | **4°** |
| Machine Gun | 12° | **5°** |
| Sniper | 8° | **3°** |
| Rocket | 12° | **4°** |
| Blaster Cannon | 12° | **4°** |
| Laser | 12° | **4°** |
| Cryo | 8° | **4°** |
| Stasis | 8° | **4°** |

`TurretAiming.Track` already measures **remaining absolute angular error after the turret turns**; its return value gates both projectile volleys and sustained Laser damage. The values above only make the gate stricter. All cooldowns, target searches, aim rotation speeds, fired projectile spread/handling, accuracy, power/heat/ammo costs, visuals, upgrades' other stats and enemy balance remain unchanged. The **Shockwave** is omnidirectional (no alignment gate) and the **Restoration Tower** uses a separate 12° friendly-repair alignment, intentionally not modified.

The current `*TowerLevels.cs` rows remain the authoritative Inspector-editable/config-resolved defaults, and future per-type or per-level tuning stays possible through existing stats. Tightening the gate can delay shots against highly mobile targets; owner should visually check first shots, tracking, rapid retargets and continuous Laser activation in Godot. No local Godot/.NET runtime acceptance was performed.

---

## Planned shared recoil pilot — Battle Tower only (2026-10-20)

**Documentation-only plan, not implemented:** [Recoil/README.md](Recoil/README.md) specifies a reusable, active-only visual recoil controller and Inspector style, to be piloted on the existing Battle Tower only. It preserves fired projectile/muzzle timing, targeting, base rotation, construction sprite paths and other towers; true fixed-hull/split-gun art is a later separate pass. Have Codex implement R1 from that README before enabling it in-game.

---

# Weapon aiming

TurretAiming.Track turns an up-facing visual pivot toward a world target and reports alignment. BattleTower calls it before cooldown-based firing. Rotation speed, tolerance and tracking are held in the tower config; the helper owns no target search, damage or economy.


Phase 4C-2: Laser reuses TurretAiming and TowerTargeting; Beams/ owns reusable endpoint-only visual presentation and BeamStyle. LaserTower owns sustained base DPS delivery, COMBAT/Damage owns defenses. There is no forced weapon node/tick on passive TowerBase instances.

**Implemented delivery distinction (Phase4E):** Blaster uses one projectile-triggered instant disc hit, followed by a cosmetic expanding ring. ShockwaveEmitter instead advances a real damage front, with swept relative crossings and per-emission hit-once state. It reuses AreaDamage.AtDistance and shared defenses, never repeated full-disc damage. Both use FEEDBACK/Shockwave with weapon-owned styles; only gameplay controls timed wave radius. See Shockwaves/README.md.

PV1/PV2 are implemented in [Visuals](Visuals/README.md): launch-snapshotted tracers, bounded muzzle flashes and optional Sniper red aiming. Per-tower presets own appearance; no combat-stat or HUD changes.
