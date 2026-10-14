# Profiles — Implemented local identity foundation

`PlayerProfile.cs` is a serializable data record (`Id`, `Name`, `CreatedUtc`). `ProfileStore.cs` owns the `Selected` profile during this process, the validated list and create/select/clear behavior. `UI/Menus/Boot/BootScreen.cs` is its UI; gameplay and HUD should not open dialogs or manipulate JSON.

## Storage

Godot `user://Profiles/profiles.json`: one versioned JSON index of local profiles. Each profile uses a stable random GUID (`N` format), not a name-derived path. Index writes use `.tmp`, flush to disk and replace with a `.bak` fallback; reads validate versions, IDs, duplicate IDs, names and length. If the index is corrupt and a valid backup exists, it loads the backup and exposes `RecoveryMessage`. When neither is readable, it preserves both files and shows a recoverable error at Boot rather than overwriting them.

`ProfileStore.Create(name)` trims names, rejects blank/control-character/over-32-character and duplicate names, and persists before selecting. `ProfileStore.Select(id)` requires an existing ID; `ClearSelection()` keeps all data. Boot always asks the player to choose/create a profile when launched normally. No deletion/renaming or online accounts in this phase.

**Profiles are persisted; campaigns are not yet persisted.** Do not imply selecting a profile restores a battlefield. See `SYSTEMS/Saving/README.md` for the planned campaign boundary.

## Acceptance

After F5: create a profile, choose Campaign, quit and relaunch; the existing name remains listed. Create two profiles and switch between them without losing either. Test invalid/duplicate names and an unreadable index with/without backup. `Main.tscn` remains directly runnable for gameplay tests without a selected profile.
