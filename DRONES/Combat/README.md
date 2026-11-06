# Fighter drones
**FD1-B + FD2: source implemented / awaiting owner validation.**

Complete a [Drone Station](../../TOWERS/Utility/DroneStation/README.md), buy a fighter, select its slot and LAUNCH. Auto-pilot intercepts detectable enemies with the smallest remaining journey to their assigned Citadel, then follows them with target-relative figure-eight, orbit and kiting passes. Comparable threats are distributed between fighters, with hysteresis and urgent breach priority. Between waves they guard near home; RECALL docks without refilling HP/ammo.

`FighterDroneTargeting` belongs to the world's enemy registry: one shared 96-candidate-per-physics-frame budget, top-64 cache refreshed after 0.25s, plus per-fighter staggered search intervals. Only deployed station managers drive it. `FighterDroneMovement` owns continuous steering; shared `DroneFlightMotion` retains finite launch/return and `DroneBody` retains engines/trails. Tune speed, acceleration/braking, turn rate, attack distance, orbit radius, manoeuvre speed, lead and search interval in `DefaultFighterDroneConfig.tres`.

Checkpoints retain ID, slot, HP/ammo, pose, flight leg, velocity and loop phase; targets are reacquired. Existing state enum values and optional snapshot fields remain compatible. The usual content-hash compatibility checks still apply after source changes. Station sale/destruction cleans up fighters and assignments.

No weapons (FD4), patrol controls (FD3), resupply (FD5) or hostile anti-air are implemented. [Roadmap](FIGHTER_DRONES_IMPLEMENTATION_PLAN.md).
