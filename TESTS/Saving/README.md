# Saving checks — S1–S3

**Authorship validation:** diff whitespace and basic C# delimiter/source review completed.
No .NET/Godot executable is installed here: neither compilation nor fixture/runtime execution is
claimed. No broad suite, performance profiling or render loop was run.

Build in the Godot .NET editor, then run `CampaignStoreChecks.tscn` once. Its disposable-directory
checks cover storage/recovery/ownership/format boundaries plus typed checkpoint serialization
with frozen research, paid work, real cargo, empty return flights, RNG and telemetry. It also
rejects a missing nested required field and a reward ticket from another profile. The fixture
never changes actual user profiles. This is not a real battlefield round-trip test.

## Brief local gameplay smoke check

1. Use a test profile and deploy Level001. Confirm Save is available before wave one; start a wave
   and check the disabled Save control displays exactly **Unavailable during combat**. F2 live ships
   and surviving hostile/friendly projectiles must also block Save.
2. Between waves, note Credits, each Citadel's HP/stored income, tower HP/tier/XP/targeting/ammo.
   Place an unfinished building and arrange a loaded supply drone, then Esc → Save.
3. Load directly, then restart and use Campaign → Continue. Verify the next wave is manual, those
   values match, the original research bonuses remain, construction resumes at its saved progress,
   cargo arrives only once and returning drones keep their source slots until they reach home.
4. Buy a paid upgrade before another checkpoint and confirm its pending tier/remaining time survive.
   Verify the mission reward ticket is unchanged and completion awards XP once. The completed
   checkpoint should subsequently show a retired-ticket explanation instead of granting another award.
5. Switch profiles: Continue/Load must select only that profile's slot. Confirm profileless F6 still
   plays normally without saving. For corrupt/incompatible-file review use a copy/test profile only:
   original files and the currently running battlefield must survive a failed load.

Extended combinations (multiple objectives, conditions, missing endpoints, crash interruption and
export remapping) remain local acceptance work, not claimed passes. Dependency fingerprints currently
require authored resource files; verify packaged exports separately.
