## Level 1 construction and Levels 2–3 artwork — 2026-11-02

Stasis Tower now uses the existing shared event-driven `TowerConstructionVisuals` presenter and a new `Artwork/Construction/StasisConstructionArt.tres` resource. Initial Level 1 building has foundation (0%), structural frame (35%) and partial emitter (70%) artwork; 100% restores the original fixed base and rotating/recoiling gun. Paid Level 2/3 upgrades have distinct finished split base/gun SVGs plus 35% and 70% assembly-stage composites. The upgraded artwork follows its original green palette, with increasing armour, field stabilizers and emitter detail.

The tower's existing single `MuzzleA` marker remains at **(0, -25)**; damage, temporary freeze, projectile handling, aiming, recoil, ammunition, prices, existing 4/5/6-second work durations, construction drones and gameplay are unchanged. This is cosmetic only; no new per-frame loops. See [construction artwork](Artwork/Construction/README.md) and [upgrade artwork](Artwork/Upgrades/README.md). Godot visual acceptance pending.

---

## PA3 projectile handling

Existing level rows author ±1/.75/.5° launch spread. Selected rank totals reduce spread by 0/5/10/15/20%; reductions are not accumulated across ranks. Each emitted shot snapshots effective handling and samples once at its real muzzle. Straight is the default; see the shared projectile accuracy plan. Existing damage, cadence, ammunition and recoil values are retained.

## Recoil presentation — 2026-10-20

Fixed BaseSprite and rotating TurretPivot/RecoilRoot/Sprite now split the finished artwork; Muzzles travel with that assembly. The original composite SVG remains the catalog icon; the world placement ghost mirrors the finished Level 1 scene sprites. The scene's Recoil node assigns Visuals/StasisTowerRecoil.tres (Enabled, kick distance/duration, return duration, maximum accumulated offset). Each funded shot/volley snapshots its launch and flash before moving the gun. Generic disabling/destruction resets the pose; idle controllers sleep. Selection colors base and gun. Combat balance, upgrades, construction gates and targeting are unchanged. Runtime/build/visual verification is pending. See [shared recoil notes](../../Shared/Weapons/Recoil/README.md).

# Stasis Tower

## Stasis ST1 — implemented 2026-10-18

Green-themed Stasis is now a catalog-registered Offensive Energy projectile tower. It uses the existing shared muzzle/projectile, construction, physical ammo, hull HP, purchased-level, earned-rank, stat pipeline and Inspector paths. No Inspector layout changes. Targets must be unfrozen and Freeze-nonimmune; all six selected modes score only those eligible candidates. If none qualify, wait with no damage-only fallback. Cryo-slowed enemies remain eligible. Existing in-flight shots can refresh Freeze with maximum remaining/new duration, never additive duration.

Freeze is separate from Slow and damage type. A lazy registry-owned TimedStatusScheduler processes only active timed enemies using retained list storage, respects pause, unregisters on death/arrival/exit/reinitialization and sleeps when empty. Cached movement is zero while frozen; expiration restores the retained strongest Slow × base speed. No route reset, new A* request, per-enemy timer or permanent-status ticking. A shared tint resolver composes cyan Slow/green Freeze; effects survive source tower removal without retaining its reference. Status application grants no XP; real Energy damage/kill attribution follows existing projectiles.

Provisional level tuning: build350 / upgrade225 / upgrade300 Credits; damage10/11/12; rate0.20/0.21/0.22 shots/s; Freeze3/3.25/3.5 seconds; range239/248/258px; projectile650px/s; local ammunition600, one per shot; hull100. Edit `TOWERS/Offensive/StasisTower/StasisTowerLevels.cs`. Green SVG tower/icon, tracer and muzzle presets are independent from Cryo. This is a test balance, not final progression tuning.

This entry supersedes older statements that Stasis/timed scheduling are deferred or must not be implemented yet; historical Cryo plans below are retained. DoT/EMP/stealth and research modifiers remain future work. See `TESTS/Towers/StasisTower.md` for validation and limits.

Files: StasisTower.cs supplies only Freeze eligibility/payload hooks; StasisTowerConfig.cs presents effective stats; StasisTowerLevels.cs owns complete paid tiers; StasisTowerRanks.cs owns earned XP thresholds; scene/definition/default config register the tower and one barrel-tip muzzle. Artwork/ and Visuals/ own green presentation. FreezeStats passes through all eight shared stages and launch captures the resolved duration. Future modifiers should alter that capability through the existing pipeline.
