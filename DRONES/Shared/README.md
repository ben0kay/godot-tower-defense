# Shared drone flight and body — DRONE2

**Source Implemented / Awaiting Owner Validation.**

`DroneFlightMotion` is plain value-backed cubic/eased flight state, with BeginFlight/Fly, heading slew, velocity, copying and strict finite checkpoint validation. It accepts the job owner's actual speed. ConstructionDroneMotion inherits the same flat fields and keeps only builder-specific orbit, hover, PRNG and validation. No dispatch, callbacks, inventory, construction work or Node processing belongs to the primitive.

`DroneBody.tscn` references the existing builder SVG (kept at its original path for asset compatibility); both role scenes instance it. Sprite heading, rear thrusters at local(±9,6), optional cargo roof light and trail all live in this presenter. Beam coordinates stay on the unrotated builder root. Body has no Process/PhysicsProcess; role updates call Present. Its two circles/short engine strokes pulse for0.28s on each departure, then scale with actual speed. Hover is a tiny steady glow; parked/hidden resets state.

`DefaultDroneVisualConfig.tres` exposes ThrustersEnabled, ignition duration0.20–0.35s, core/halo tint, intensity, scale/ignition expansion, TrailEnabled, color/alpha,1–2px width, min/max length15–35px and speed response. No per-engine Nodes, particles or tweens. A fixed32-point world-position history follows actual curved flight, trims to desired speed-dependent length and fades outward; slow/braking motion shortens it and hover clears it. History and draw loops allocate no managed buffers per update. Effects are cosmetic; toggles never change job callbacks or motion clocks.

Builder max speed remains280px/s and existing builder curve/turn controls are preserved. Ammo speed remains source-authored; its adapter curve/turn controls are independent while using identical mathematics. No shared gameplay concurrency cap.

New courier snapshots restore exact state, old courier records ease from their saved position, and builder field names stay flat/unchanged through inheritance. Cosmetic history/ignition is not persisted. Source-only review; inspect native heading, two flares, actual-path trail, hover/rest and dense flights in Godot before accepting visual/performance quality.
