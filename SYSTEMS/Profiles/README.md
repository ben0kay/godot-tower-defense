# Profiles — Implemented local identity foundation

`PlayerProfile.cs` is a serializable data record (`Id`, `Name`, `CreatedUtc`). `ProfileStore.cs` owns the `Selected` profile during this process, the validated list and create/select/clear behavior. `UI/Menus/Boot/BootScreen.cs` is its UI; gameplay and HUD should not open dialogs or manipulate JSON.

## Storage

Godot `user://Profiles/profiles.json`: one versioned JSON index of local profiles. Each profile uses a stable random GUID (`N` format), not a name-derived path. Index writes use `.tmp`, flush to disk and replace with a `.bak` fallback; reads validate versions, IDs, duplicate IDs, names and length. If the index is corrupt and a valid backup exists, it loads the backup and exposes `RecoveryMessage`. When neither is readable, it preserves both files and shows a recoverable error at Boot rather than overwriting them.

`ProfileStore.Create(name)` trims names, rejects blank/control-character/over-32-character and duplicate names, and persists before selecting. `ProfileStore.Select(id)` requires an existing ID; `ClearSelection()` keeps all data. Boot always asks the player to choose/create a profile when launched normally. No deletion/renaming or online accounts in this phase.

**Profiles are persisted; campaigns are not yet persisted.** Do not imply selecting a profile restores a battlefield. See `SYSTEMS/Saving/README.md` for the planned campaign boundary.

## Acceptance

After F5: create a profile, choose Campaign, quit and relaunch; the existing name remains listed. Create two profiles and switch between them without losing either. Test invalid/duplicate names and an unreadable index with/without backup. `Main.tscn` remains directly runnable for gameplay tests without a selected profile.

## Profile-wide progression (initial foundation implemented)

The selected profile GUID now owns separate persistent Player XP/level and Research Point state; research unlocks remain planned in [the P1–P5 progression brief](../Progression/PLAYER_XP_RESEARCH_FOUNDATION_PASS.md). Current `PlayerProfile` / `ProfileStore` still store **identity only**. Do not confuse Player XP with per-tower Rank XP or invent campaign Save/Load.

## Player progression foundation (2026-10-17)

Separate per-profile XP/levels/Research Points now live in SYSTEMS/Progression (see its README). Main and Campaign show the selected profile's level, XP bar and RP. Missing progression starts at Level 1; saved grants survive restart and profile switching. Identity/index format is unchanged. The optional menu DEBUG grant is Inspector-controlled and off by default. Mission rewards/Results XP, replay rules, research purchasing and full campaign/battlefield saving remain deferred; this does not complete P1–P5 or Results R4.
