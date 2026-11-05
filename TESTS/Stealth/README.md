## Stealth Beacon paid tiers (2026-11-05)

The focused `StealthChecks.cs` source now asserts three paid scanner levels, Level 1's 4.5-second revolution and Level 2/3 80°/90° coverage and 280/304px ranges. Earlier SE2 timing assertions were adjusted for the slower revolution without changing the expected in-place heading refresh or independent reveal handles. These checks are **authored but not executed** in this tool environment; the owner handles Godot validation.

---

# SE2 focused verification — 2026-11-05

**Source checks completed; fixture updated but not executed.** .NET is unavailable here; no installation, large suite or benchmark. After rebuilding C# in Godot .NET, optionally run `StealthChecks.tscn` once.

Directional assertions replace SE1's radial assumptions: default up-facing 70° cone, front vs side/behind (including close sideways targets), inclusive 256px edge, continuous head movement and one bounded polygon, stationary enemies revealed/re-hidden by rotation, retained source identity without transition flicker, independent overlapping source departure, construction/EMP/death/removal, paused heading/membership, live cone/range/revolution/refresh/opacity edits, source teleport with absent-member cleanup, and index subtree unload sleeping scanners/releasing handles. Existing wave flags, shield/stealth visuals, status composition, six policies, private specialist detection and aimed-weapon gates remain checked. A cached Laser lock is stepped after the last scan rotates away and must not damage or consume ammo.

Only private weapon fixture configs omit Power so the specialist Battle assertion tests detection independently of PW4 charge. No shipped config, map, wave, price or combat stat is changed. Source review confirms the manager has no frame callback and source heading refresh does not register/unregister or rebuild source buckets. Detection is sampled (default 0.1s), not a per-enemy frame loop.

Owner visual check: build/place Utility → Stealth Beacon, inspect split fixed base/clockwise head and faint sector, watch stationary stealth ships in front vs side/behind, overlap two sources, pause/resume, and disable/remove a beacon. Check construction art hides the sector and normal zoom keeps it subtle. The scan phase is transient presentation state and starts fresh on checkpoint reconstruction, like other non-durable target/presentation state.

---

## Historical SE1 validation record (superseded for directional assertions)

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
