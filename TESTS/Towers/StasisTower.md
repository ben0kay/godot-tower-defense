# Stasis ST1 verification

Run `res://TESTS/Towers/StasisTowerChecks.tscn` in Godot .NET. Uses actual catalog/HUD, private config/stat tiers, live turret acquisition, Energy projectile impact and timed scheduler. Covers finite safety clamps, Freeze/Cryo ordering, maximum remaining refresh, instance isolation, automatic expiration, pause, frozen/immune exclusion, re-eligibility, death/free cleanup, real routed movement, 500 timed enemies and idle scheduler. Calls JourneyTargetingChecks for all six Freeze-filtered modes and First/Last after expiry across two actual portal routes, alongside existing shared route/cache/budget checks.

Validation results are recorded after running these checks. Desktop appearance/playfeel needs user review; 500-enemy checks establish behavior, not an FPS benchmark. Status effect application grants no XP and this pass introduces no DoT, EMP or research unlock.

Verified: .NET build, zero warnings/errors; StasisTowerChecks; CryoTowerChecks; ShieldTargetingChecks; PlacementHudChecks; CompactInspectorChecks. Placement/Inspector checks include Stasis in the catalog-driven previews at 1920×1080, 1366×768 and 960×540, preserving the current 560×330 layout and HP/ammo/rank presentation. Paid build/upgrades, retained rank and actual turret launch-time Freeze snapshot are also checked. Standard fixture warning about loading DefaultTowerCatalog is unchanged.
