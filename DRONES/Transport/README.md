# Ammunition couriers — DRONE2

**Source Implemented / Awaiting Owner Validation.** `CargoDrone.cs/.tscn` replaces the obsolete TransportDrone script/scene/procedural artwork. All live scene, code, save and fixture references were migrated before removal. Saves identify source/destination/cargo, not arbitrary courier scene paths, so no obsolete scene alias is needed. Existing dependency fingerprints remain enforced.

The same adapter handles Factory→Depot and Depot→Tower. It instances `DRONES/Shared/DroneBody.tscn`, using the construction drone's actual SVG/silhouette, twin rear thrusters and thin ion history. Loaded cargo has a tiny amber roof indicator; empty returns are dim cyan. No builder beam, site orbit or construction hold exists here.

Outbound and return each use the shared `DroneFlightMotion` cubic curve with smoothstep acceleration/braking and bounded sprite turning. Each source's authored DroneSpeed passes through unchanged; curve/easing naturally changes travel time from the old direct MoveToward path. The adapter's Inspector curve/turn defaults are100px/240° per second. Returning starts along current motion/heading, bends home and brakes. Changed targets begin a new remaining leg; missing destinations report once to the existing logistics callback and return cargo; missing home/external destruction abort once.

**Business owner remains AmmunitionLogistics.** Only its arrival callback unloads physical stock. Factory shipments remain full1000-round batches, Depot→Tower loads remain flexible, and reservations, surplus returns, cargo/loss accounting and source slots through return are unchanged. No combined builder/courier cap, new allocator or pathfinding.

New delivery records include an optional value motion snapshot, restoring exact curve/time/heading. Legacy records without Motion build an eased remaining leg from the saved position; neither path replays arrival, cargo removal or delivery. Existing wallet/ammo/production counts and strict save validation remain authoritative. Trail/ignition are transient and restart cleanly without replaying a restore ignition flare. Idle slots create no courier nodes; completed couriers free through the existing lifecycle.

Owner: both delivery routes, amber surplus return, pause, endpoint removal, actual stock conservation and outbound/return checkpoint reload. Optional narrow adapter runner: `TESTS/Drones/Drone2Checks.tscn`; existing ammo fixture helpers now use actual remaining curve duration. Not compiled/run here (.NET unavailable).
