# Fighter drones
**FD1-B, FD2 and FD4: source implemented / awaiting owner validation. FD3 patrol remains deferred.**

Build a [Drone Station](../../TOWERS/Utility/DroneStation/README.md), buy a fighter, choose its slot and LAUNCH. Fighters prioritize the shortest remaining journey to the enemy's assigned Citadel using the existing shared budgeted targeting. RECALL returns to their own dock; no free HP/ammo refill.

**Fixed twin-gun fighter refactor:** one hull sprite includes both forward barrels. `Body/Sprite/LeftMuzzle` and `RightMuzzle` alternate successful shots; there is no rotating gun sprite. The hull stages a run, aligns and fires, flies past the enemy, extends away and makes alternating return lobes for a figure-eight pattern. Fire is restricted to an attack run and an **8° forward cone**, with slight lead correction, finite range and shared terrain LOS. Target loss returns to home loiter. Recall/load safely rebuilds the next pass; launch/docking remain shared finite flight.

Defaults: **9 shots/s total** (was 5), 4 Kinetic damage, 275px range, 1200px/s bullet speed, 200 rounds. Actual shared physical projectiles retain the cyan tapered tracer/flash and fighter damage attribution; ordinary kill Credits remain, no rank/fighter XP. Ammo parity preserves left/right alternation across checkpoint restore without another saved state field.

Tune `FighterDroneConfig`: flight speed, acceleration/braking, hull turn rate (150°/s), attack run-up distance (220px), pass exit distance (220px), return-loop radius (150px), manoeuvre speed (260px/s), aim tolerance and fire rate. Shared `DroneBody` now has optional cosmetic engine-length/acceleration-flare settings; the fighter preset uses 3.2× exhaust length plus acceleration flare. Other drone presets keep the original defaults.

Existing snapshots retain ID/slot/HP/ammo/pose, finite flight, velocity/phase and weapon cooldown; targets and attack legs are reacquired rather than storing runtime enemy references. Content-hash checks still apply. Station sale/destruction cleans up its roster.

No FD3 manual patrol UI, FD5 resupply/repair or hostile anti-air added. Source/reference review and native SVG render only; .NET unavailable, owner gameplay/visual acceptance pending. [Roadmap](FIGHTER_DRONES_IMPLEMENTATION_PLAN.md).
