# Main Menu — Implemented

`MainMenu.tscn` / `MainMenu.cs` display the selected local profile and provide **Campaign**, **Change Profile** and **Exit Game**. A missing profile redirects to Boot. `MenuNavigation.cs` owns destinations.

Campaign opens the currently implemented `UI/Menus/Campaign/CampaignMenu.tscn`, not the gameplay scene directly. Campaign's **Play Level 001** button starts `Main.tscn`. Change Profile clears only the in-memory choice and returns to Boot; saved identities remain intact.

Future: continue/load a saved campaign, options, artwork and richer layouts without moving profile/storage logic into this class. Buttons come from `UIButtonFactory` rather than hardcoded style overrides.

## Player progression foundation (2026-10-17)

Separate per-profile XP/levels/Research Points now live in SYSTEMS/Progression (see its README). Main and Campaign show the selected profile's level, XP bar and RP. Missing progression starts at Level 1; saved grants survive restart and profile switching. Identity/index format is unchanged. The optional menu DEBUG grant is Inspector-controlled and off by default. Mission rewards/Results XP, replay rules, research purchasing and full campaign/battlefield saving remain deferred; this does not complete P1–P5 or Results R4.
