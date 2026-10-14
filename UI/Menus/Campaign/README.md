# Campaign — Level001 Gateway (implemented foundation)

`CampaignMenu.tscn` / `CampaignMenu.cs` display **Level 001 — Defend the Citadel**, a **Play Level 001** button and **Back to Main Menu**. It requires a selected local profile. Play opens the existing `res://Main.tscn` battlefield without touching its scene or level state.

**No campaign save is created or restored in this pass.** This button starts a fresh Level001 session. Do not enable misleading Continue, Load, stage completion or level unlock buttons before `SYSTEMS/Saving` and progression actually support them.

Later replace this single-level gateway with **Campaign → Levels screen** where the player selects unlocked levels. Keep the interface scene-independent and use `MenuNavigation` for transitions; `LEVELS/` owns map definitions/identities and `SYSTEMS/Saving` owns progression state.
