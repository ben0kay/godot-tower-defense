# SE1 verification — 2026-10-20 UTC

Run `res://TESTS/Stealth/StealthChecks.tscn` after building C#.

Focused coverage uses the real catalog/HUD/wallet/placement/construction and wave scheduler in a private open-terrain fixture:
- Wave spawn combines shield:true and stealth:true; normal spawns stay ordinary.
- Ship, shield aura and DefenseBar alpha is0.30 hidden /1.0 revealed; Cryo+Stasis SelfModulate tint survives reveal changes.
- Six priorities and Laser shield preference exclude hidden ships before scoring. Specialist query can acquire hidden ships without globally revealing them.
-250-Credit paid1×1 beacon has no effect during its2s construction, then reveals stationary enemies within256px. Exact boundary, exit/re-entry, newly registered stealth spawn, source overlap, EMP/reactivation, destruction, removal, radius changes and source teleport are checked.
- Pause retains range-only revelation. Enemy death/reinitialization clears source membership and original appearance. Removing last beacon leaves no active indexed sources.
- Every aimed type (Battle, Blaster, Rocket, Laser, Sniper, Machine Gun, Cryo, Stasis) refuses to spend ammo or fire at a hidden-only target. A cached Laser lock stops damage/ammo; a private specialist Battle config actually fires and does not change the shared default.

Build and StealthChecks passed. Existing ShieldTargetingChecks (including checkpoint/path and500-enemy sharing) and CreditTowerChecks also passed. The broader StasisTowerChecks fails at its initial “Could not purchase stasis” authored-map fixture; the same failure was reproduced on unchanged parent f5133ec in a separate worktree. Existing HudCategoryChecks also fails “Could not purchase battle”, and PlacementHudChecks fails “Sequence contains no matching element”; both failures were reproduced on that same unchanged parent. These authored-map fixtures are not claimed passing or fixed. The Utility-category assertion was updated for the now-populated category; new StealthChecks verifies actual Utility selection and paid placement on its private terrain. The focused stealth test covers Stasis targeting and concurrent Slow/Freeze color without editing user maps.

Beacon SVG was rendered and visually inspected; Godot headless import succeeded. Desktop gameplay/readability at varied zoom, crowded enemies and HUD widths is still a manual review, not an FPS/rendering claim. No existing wave or map was given stealth automatically. Example authoring: `Enemy("basic_mk1", stealth: true)`.
