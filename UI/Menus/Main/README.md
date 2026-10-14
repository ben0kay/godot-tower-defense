# Main Menu — Implemented

`MainMenu.tscn` / `MainMenu.cs` display the selected local profile and provide **Campaign**, **Change Profile** and **Exit Game**. A missing profile redirects to Boot. `MenuNavigation.cs` owns destinations.

Campaign opens the currently implemented `UI/Menus/Campaign/CampaignMenu.tscn`, not the gameplay scene directly. Campaign's **Play Level 001** button starts `Main.tscn`. Change Profile clears only the in-memory choice and returns to Boot; saved identities remain intact.

Future: continue/load a saved campaign, options, artwork and richer layouts without moving profile/storage logic into this class. Buttons come from `UIButtonFactory` rather than hardcoded style overrides.
