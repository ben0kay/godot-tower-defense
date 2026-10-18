## Tutorial follow-up — 2026-10-18

Level002 has since been painted and registered as a playable four-wave tutorial. Current migration assertions now verify its own terrain, two portals, wave set and Level001→002 registration while continuing to preserve Level020 independently. The empty/unplayable-map expectations below describe the earlier rename pass. See Level002TutorialChecks for the real four-wave fixture.

---

# Level002 → Level020 reorganization — 2026-10-18

Verified with Godot .NET 4.7.2 and .NET 8:

- C# build: zero warnings/errors. Editor import completed.
- Source comparison against base `88f3a96`: Five Gates scene, settings, waves, world variant and launcher are byte-for-byte equivalent after substituting the renamed Level020 references. Painted tile data and object coordinates are preserved. Incoming template edits at `a860beba` are preserved unchanged and included in the fresh Level002 copy.
- `LevelReorganizationChecks.tscn` passes: fresh Level002 has empty terrain layers, retained template objects and independent settings; the preserved Level020 launches with numeric identity020, all five active portals, the four-wave set and successful real portal spawning. The raw Level002 map is not treated as a playable battlefield; Next does not skip the gap.
- `TESTS/UI/ResultsChecks.tscn` passes: outcomes, retry, Campaign return, renamed Level020 direct navigation, missing-next safety and strict consecutive-ID formatting. Navigation fixtures now expect Level001 Next to remain disabled until a playable Level002 launcher is registered.

The renamed historical `Level020Checks` retains old fixed terrain-count/layout assertions. Its “western open ground and eastern maze are fully painted” assertion does not match the current authored map at base88f3a96; the migration check uses source equivalence rather than rewriting that old map or claiming that historical fixture passes. No gameplay content was changed to satisfy a test.

Design the new map at `LEVELS/Level002/Level002.tscn`; configure its own `Level002Config.tres`, then add waves/launcher/catalog/menu entry when ready. Five Gates remains at `LEVELS/Level020/` with `MainLevel020.tscn` for direct F6 play.
