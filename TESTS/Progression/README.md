# Initial player progression verification — 2026-10-17

## Research schema extension — 2026-10-17

Progression v2 adds purchased research IDs and preserves prior XP/RP/curve through v1 migration. The original PlayerProgressionChecks continues passing; Research-specific migration/payment/ownership checks live in TESTS/Research. Historical v1 limitations below are retained; current Research ownership/purchases and deployed Citadel bonuses are implemented. Mission reward idempotency/scoring/replays remain unimplemented.

---

Run res://TESTS/Progression/PlayerProgressionChecks.tscn in a Godot .NET build.
The fixture uses a unique temporary directory and deletes it afterwards; it never writes the player's profile index.

Actual validation in this environment:
- C# compilation with Godot source generators, .NET 8 references and DEBUG: PASS.
- Godot 4.7.2 .NET headless PlayerProgressionChecks: PASS.
- Local isolated-project menu smoke: PASS. Created Main and Campaign with a disposable selected profile, found both progression panels, granted 600 XP through the real service, freed/reopened menus and verified profile switching/isolation. This harness used a separate application name/user directory, not real player data.
- The compilation/test project used an available prior repository checkout with all changed production sources and their current menu/profile dependencies overlaid. This is scoped verification, not a clean full-HEAD build or full gameplay regression run.
- Real viewport/desktop visual review has NOT been performed.

Storage/arithmetic scenarios: fresh Level1/0/500, exact threshold, 600 XP carry, multiple promotions/RP, zero/negative grants, invalid curve, overflow, save/reload, profile isolation, saved-curve stability, backup recovery, repair write preserving valid backup, irrecoverable corruption, invalid GUID and save failure.

Manual editor acceptance:
1. F5, select a disposable profile: Main and Campaign show Level1, 0/500 XP and 0 RP.
2. Enable ShowProgressionDebugGrant on MainMenu or CampaignMenu (default false). In a DEBUG build click the labelled 600 XP button: Level2, 100/750, 1 RP.
3. Switch menus, restart and select the same profile: progress survives. Select a different profile: independent state.
4. Confirm the debug option is off again before committing scene changes. The button is absent from release builds.
5. Inspect both menus at typical resolutions; check wrapping, bar size and keyboard navigation.

Mission reward idempotency, first-clear/replay rules, Results XP and research purchase/activation are later passes and are not claimed tested or implemented.
