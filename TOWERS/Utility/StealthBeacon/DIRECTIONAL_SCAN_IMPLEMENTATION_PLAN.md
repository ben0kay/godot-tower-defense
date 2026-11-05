# SE2 — Stealth Beacon actual rotating cone reveal (Codex implementation handoff)

**Status: directional mechanics NOT implemented.** New split Stealth Beacon art, construction visuals, `TurretPivot` and empty `ScanCone` placeholder are committed separately in this same artwork pass. Codex should implement this plan on **`dev`** without redoing the SVG art.

Read `AGENTS.md` if present, `NOTES/CodePreferences.md`, root README, `TOWERS/Utility/StealthBeacon/README.md`, `ENEMIES/Shared/Stealth/README.md`, `ENEMIES/Shared/Stealth/StealthRevealSystem.cs`, and `TESTS/Stealth/StealthChecks.cs` before editing. Work modularly and keep validation light. Push to `dev`.

## Design contract

- **Option B — genuine directional scan.** While operational, the beacon gradually rotates its top scanner and **only** reveals stealthed enemy centres lying inside a **70-degree full-width cone** extending up to its existing inclusive **256px** range. Nothing outside the cone is revealed. No always-on inner bubble. Reveal source is removed as soon as the enemy leaves the cone, except when an independent reveal source remains.
- Defaults, inspector-editable on `StealthBeaconConfig`: `ScanConeDegrees = 70f`, `ScanRotationSeconds = 3f` (one 360-degree revolution), `ScanUpdateIntervalSeconds = 0.1f`, `ScanConeOpacity = 0.08f`. Validate finite/bounded values. These are new config fields, not new purchased levels.
- Visual `TurretPivot` rotates continuously clockwise while operational. The scanner SVG is oriented along **local up (-Y)**; the query direction in world coordinates is `-pivot.GlobalTransform.Y.Normalized()`. `TurretPivot/ScanCone` should display a faint teal-green transparent filled sector synchronized to the actual scan angle/range. Make it a once-built `Polygon2D` or efficient local draw geometry with ~16–24 segments, parented under the rotating pivot. Do not redraw geometry every physics frame. Its rotation must come from the parent pivot; stop/disable/hide it when inactive or under construction.
- Initial construction (0/35/70%) and the finished split base/head scene are already wired. Leave `Sprite`, `TurretPivot`, `TurretPivot/BeaconHeadSprite`, `ConstructionSprite` and shared presenter paths intact. No new attack, projectile, damage, heat, ammunition, power requirement, rank or tower upgrades.

## Code changes

### 1. `StealthBeaconConfig.cs`

Add the four Inspector exports listed above. DescribeStats should report reveal cone angle, scan revolution time and range, instead of claiming unconditional radial reveal. Keep existing RevealRange/health/price/2-second build unchanged. Provide explicit input validation (angles between 10–180 degrees, revolution >0, refresh >0, opacity 0–0.3; finite everywhere).

### 2. `StealthBeacon.cs`

Cache `TurretPivot` and `TurretPivot/ScanCone` once at `_Ready`. Rotate the pivot using elapsed unpaused time when the beacon is fully built, not disabled, not destroyed, and can operate; use angle speed `Mathf.Tau / ScanRotationSeconds`. Never rotate while inactive. Maintain a short accumulator and notify the existing world `StealthRevealSystem` of heading changes approximately every `ScanUpdateIntervalSeconds`, **not every rendering frame**. On disable/build, call the existing unregister immediately; do not run a timer on inactive beacons. On reactivation, register with the current facing and immediately sample stationary enemies already in the cone. Rotation and reveal must freeze during pause (process mode inherited Pausable); on resume continue from the retained angle. Avoid mutating child textures or allocating nodes during normal ticks.

### 3. `StealthRevealSystem.cs`

Preserve its existing one-world registry index and `Dictionary<StealthBeacon, Area>`, `_cells`, `_memberships`, `Area.Enemies` and `EnemyStealthState` reveal handle identity. Extend `Area` with normalized forward vector and cosine of half the cone's angle. Keep `RadiusSquared` for existing indexed distance broad phase. **Replace the final radial membership test** in both incoming movement/registration events and existing membership expiry with an exact cone test. This is the reusable mathematical predicate:

```csharp
private static bool InsideCone(Vector2 point, Vector2 origin, float radiusSquared,
    Vector2 forward, float cosineHalfAngle)
{
    Vector2 delta = point - origin;
    float d2 = delta.LengthSquared();
    return d2 <= radiusSquared &&
        (d2 <= 0.000001f || delta.Dot(forward) >= Mathf.Sqrt(d2) * cosineHalfAngle);
}
```

The supplied heading must be a finite normalized direction. Compute `cosHalfAngle = Mathf.Cos(Mathf.DegToRad(config.ScanConeDegrees * 0.5f))` **once per source configuration update**, not once per enemy. The small zero-distance case is always in-cone. The distance endpoint is inclusive, as in SE1.

Add a separate `UpdateBeaconDirection(StealthBeacon beacon, Vector2 normalizedHeading)` pathway that updates the **existing** Area without calling `Register` or `Unregister` on each 0.1s scan tick. Use the registry's spatial `GetEnemiesInRadius(area.Center, radius, reusableScratch)` to gather only nearby stealth enemies. For this one Area, diff inclusion of each candidate against `Area.Enemies`; for removed members call `Stealth.RemoveReveal(area)` and update `_memberships`, for newly included members call `AddReveal(area)` and update `_memberships`. Reuse scratch buffers; do not allocate a collection on each scan. Ensure previously revealed enemies not returned by the query are also safely removed. Keep ordinary non-stealth enemies on the fast path. Keep the source identity (`Area` object) stable across heading updates so overlapping beacon reveal handles remain idempotent and no visible flicker occurs.

Initial `Register`, config/radius edits, teleport, disable, destruction, removal and world teardown retain their existing event-driven semantics and source cleanup. A cone orientation update must affect stationary enemies even if they do not emit `EnemyPositionChanged`; the old range-only motion callback is insufficient.

### 4. Visual cone

Build wedge geometry once per config change with a center vertex at `Vector2.Zero`, boundary points spanning `-halfAngle ... +halfAngle` about **local up**, radius equal to `RevealRange`. Assign a faint pale teal-green fill at low alpha (default 0.08). Parent the polygon to `TurretPivot/ScanCone` so it follows the head transform without per-frame redraws. Prefer a single lightweight polygon over `Light2D`, particles, large alpha gradients, or shaders. Make the cone slightly behind the head and avoid drawing it through the construction overlay. The cone must match the *actual* predicate's direction and boundaries.

## Focused acceptance

Update `TESTS/Stealth/StealthChecks.cs` SE1 assumptions: old tests assumed any in-radius direction reveals automatically. Verify **in-cone vs side/behind**, inclusive 256px range, moving into/out of cone, stationary enemy revealed by rotation, enemy re-stealth after rotation, source overlap (one source ending must not hide the other), pickup through source teleport, pause/resume, EMP/disable/destroy/remove, source config changes, stealth-only spawn, and world unload. Verify old beam/cached target locks stop immediately on loss of last reveal. Maintain the old no-extra-enemy-scans-per-frame contract and no new enemy-specific scripts. Do **lightweight source/compile validation only**, no extensive benchmarks; push to `dev`.

## Explicit exclusions

Do not automatically make the scanner a weapon. Do not add a passive reveal radius, LOS walls, target tracking, on-hit effect, power consumption or tower upgrades. Do not alter tower buffs/debuffs or wave generation. Do not treat new artwork's presence as proof that the cone mechanics are implemented.
