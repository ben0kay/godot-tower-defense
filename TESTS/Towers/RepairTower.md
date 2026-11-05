## Restoration Tower paid levels 2–3 (2026-11-05)

The focused `RepairTowerChecks.cs` now expects three absolute support tiers, checks L2/L3 price/work and 20/24 hull HP/s, 344/368 px range, 1,050/1,102.5 hull cap, and confirms the Repair/Range Inspector descriptions. These are lightweight authored checks **not executed** in this connector session. Owner should visually confirm finished SVG split layers, rotating gun on each tier, staged 35%/70% upgrade overlays, and saved/restored progression.

---

# RT1 Restoration Tower — focused owner check

**Source/resource review completed; fixture authored but not run.** Execution environment unavailable. No tooling installation, broad regression suite, gameplay/visual acceptance or benchmark. Rebuild C# and import new SVGs in Godot .NET; optionally run `RepairTowerChecks.tscn` with F6 once.

The small production-world fixture checks the Support catalog/type and single level settings, independent RepairStats and labels, 16 HP/s actual hull gain, physical muzzle origin, disabled healer vs disabled target, pause, exact partial/full healing, immediate out-of-range beam shutdown, nonfinite-rate clamp/sleep, private config isolation, unfinished/completed work and ordinary checkpoint input restoration without a target/beam save schema. Existing HudCategoryChecks now expects the populated Support category; no larger test suite was run.

Owner: place **Support → Restoration Tower** on a valid tile (350 Credits / 5s / 1000 HP / 320px). Damage a nearby finished tower and watch its hull rise at 16 HP/s while aligned. Check split head vs fixed base, aqua beam from barrel tip, full hull, source/target work, range exit/re-entry, EMP on healer/target, pause/resume and new Save/Load. No self/Citadel/enemy/shield repair. RT1 has **no heat or power cost**; actual continuous capacitor funding and heat are RT2. No heal XP, purchased tiers or priority selector is expected yet. Old checkpoints may fail the existing fingerprint guard after catalog/source changes.
