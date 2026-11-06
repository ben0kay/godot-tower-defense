# DRONE2 lightweight validation

**Source checks completed / owner runtime acceptance pending.** No .NET executable is available; no build, fixture execution, large suite, headless simulation or FPS benchmark was run.

Static checks confirm shared-scene resources/IDs exist, all obsolete TransportDrone runtime references are migrated, builder motion property names are identical through inheritance, phase values0–4 are unchanged with Acquiring appended as5, and ammo stock/reservation/slot logic is unchanged apart from the courier type/save seam. Diff whitespace checks pass.

`Drone2Checks.tscn` is a narrow optional owner runner for original/shared builder curve parity, authored courier speed, outbound/return callback uniqueness, pause, visual opt-out, exact curve round-trip and legacy remaining-flight restore. Existing AmmoLoopFixture arrival helpers use actual remaining curve duration. ConstructionDroneChecks now checks pre-beam acquisition and brief successful completion readouts, then renderer sleep. These fixtures are authored but unrun.

Owner smoke checks:

1. Build and upgrade: outbound ignition/curved flight → SITE LOCKED and converging cross → original0.25s beam deployment → held-work release → real CONSTRUCTING/UPGRADING percent → SYSTEM ONLINE → return ignition/braking.
2. Both ammo routes: same body/curve, small cargo light, real cargo transfer once, surplus return and source slot retained until home. No construction beam/readouts.
3. Hover/rest: tiny engines, no trail; flight trails remain thin and follow the curve. Disable each visual feature without changing outcomes.
4. Several active/queued sites and couriers: independent caps; one panel per site, bounded nonoverlap lanes; dense excess cosmetics may skip.
5. Pause, cancelled/destroyed site, removed/depleted source/destination, large rectangular footprints and fresh saved flying/acquiring/deploying/beaming/returning states. Old deploying records resume deployment without replaying acquisition. No completion label after cancellation or on checkpoint load.
